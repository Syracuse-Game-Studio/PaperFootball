using System;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Presentation;
using PaperFootball.Tabletop.Roguelike.Variance;
using PaperFootball.Tabletop.Rules;
using PaperFootball.Tabletop.Scoring;
using UnityEngine;

namespace PaperFootball.Tabletop.Match
{
    public class MatchController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PaperFootballConfig config;

        [Header("References")]
        [SerializeField] private FootballPhysicsController footballPhysics;
        [SerializeField] private FootballRestDetector restDetector;
        [SerializeField] private FlickInputReader inputReader;
        [SerializeField] private FlickInteractionController flickInteraction;
        [SerializeField] private TableBoundaryDetector tableBoundary;
        [SerializeField] private GameHudController hud;
        [SerializeField] private FlickAimIndicator aimIndicator;
        [SerializeField] private OverhangDebugOverlay overhangDebugOverlay;
        [SerializeField] private TrajectoryPreviewRenderer trajectoryPreview;
        [SerializeField] private ShotUncertaintyPreview uncertaintyPreview;
        [SerializeField] private ShotVarianceController shotVarianceController;
        [SerializeField] private FieldGoalController fieldGoalController;
        [SerializeField] private Collider footballCollider;
        [SerializeField] private Transform playerOneStart;
        [SerializeField] private Transform playerTwoStart;

        private PaperFootballRuleSet rules;
        private PaperFootballMatch match;
        private bool fellResolved;
        private bool inputSuppressed;
        private OverhangDebugSnapshot? latestOverhangSnapshot;
        private float fieldGoalAttemptTimer;

        public PaperFootballMatch Match => match;
        public PaperFootballRuleSet CurrentRules => rules != null ? rules.Clone() : new PaperFootballRuleSet();
        public Bounds TableBounds => tableBoundary != null ? tableBoundary.TableBounds : new Bounds(Vector3.zero, Vector3.zero);
        public OverhangDebugSnapshot? LatestOverhangSnapshot => latestOverhangSnapshot;
        public TrajectoryPreviewRenderer TrajectoryPreview => trajectoryPreview;

        public event Action<OverhangDebugSnapshot> OverhangSnapshotChanged;
        public event Action<FlickResolutionType> FlickResolved;
        public event Action<bool> FieldGoalResolved;
        public event Action<PaperFootballMatch> MatchStateRendered;

        public void Configure(
            PaperFootballConfig rulesConfig,
            FootballPhysicsController physicsController,
            FootballRestDetector detector,
            FlickInputReader reader,
            TableBoundaryDetector boundaryDetector,
            GameHudController hudController,
            FlickAimIndicator indicator,
            OverhangDebugOverlay debugOverlay,
            TrajectoryPreviewRenderer trajectoryRenderer,
            FieldGoalController goalController,
            Collider football,
            Transform p1Start,
            Transform p2Start,
            FlickInteractionController interactionController = null,
            ShotVarianceController varianceController = null,
            ShotUncertaintyPreview shotUncertaintyPreview = null)
        {
            config = rulesConfig;
            footballPhysics = physicsController;
            restDetector = detector;
            inputReader = reader;
            flickInteraction = interactionController;
            tableBoundary = boundaryDetector;
            hud = hudController;
            aimIndicator = indicator;
            overhangDebugOverlay = debugOverlay;
            trajectoryPreview = trajectoryRenderer;
            fieldGoalController = goalController;
            footballCollider = football;
            playerOneStart = p1Start;
            playerTwoStart = p2Start;
            shotVarianceController = varianceController;
            uncertaintyPreview = shotUncertaintyPreview;
        }

        private void Awake()
        {
            rules = config != null ? config.CreateRuntimeRules() : new PaperFootballRuleSet();
            rules.Sanitize();
            match = new PaperFootballMatch(rules);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            ApplyRuntimeConfiguration();
            ResetBallToCurrentPlayerStart();
            Render();
        }

        private void Update()
        {
            if (match == null || fellResolved)
            {
                return;
            }

            bool shouldResolveFall = match.Phase == MatchPhase.FootballMoving || match.Phase == MatchPhase.FieldGoalAttempt;
            if (shouldResolveFall && tableBoundary != null && footballPhysics != null && tableBoundary.HasFallen(footballPhysics.transform))
            {
                fellResolved = true;
                if (match.Phase == MatchPhase.FieldGoalAttempt)
                {
                    ResolveCurrentFieldGoal(false);
                }
                else
                {
                    ResolveStoppedFootball(true);
                }
            }

            if (match.Phase == MatchPhase.FieldGoalAttempt)
            {
                fieldGoalAttemptTimer += Time.deltaTime;
                if (fieldGoalAttemptTimer >= rules.fieldGoalTimeLimit)
                {
                    ResolveCurrentFieldGoal(false);
                }
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (match != null)
            {
                match.StateChanged += Render;
            }

            if (flickInteraction != null)
            {
                flickInteraction.DragChanged += OnDragChanged;
                flickInteraction.FlickReleased += OnFlickReleased;
                flickInteraction.ResetBallRequested += OnResetBallRequested;
                flickInteraction.NewMatchRequested += OnNewMatchRequested;
                flickInteraction.CancelRequested += OnCancelRequested;
            }
            else if (inputReader != null)
            {
                inputReader.DragChanged += OnDragChanged;
                inputReader.FlickReleased += OnFlickReleased;
                inputReader.ResetBallRequested += OnResetBallRequested;
                inputReader.NewMatchRequested += OnNewMatchRequested;
                inputReader.CancelRequested += OnCancelRequested;
            }

            if (restDetector != null)
            {
                restDetector.RestDetected += OnRestDetected;
            }

            if (fieldGoalController != null)
            {
                fieldGoalController.FieldGoalScored += OnFieldGoalScored;
            }
        }

        private void Unsubscribe()
        {
            if (match != null)
            {
                match.StateChanged -= Render;
            }

            if (flickInteraction != null)
            {
                flickInteraction.DragChanged -= OnDragChanged;
                flickInteraction.FlickReleased -= OnFlickReleased;
                flickInteraction.ResetBallRequested -= OnResetBallRequested;
                flickInteraction.NewMatchRequested -= OnNewMatchRequested;
                flickInteraction.CancelRequested -= OnCancelRequested;
            }
            else if (inputReader != null)
            {
                inputReader.DragChanged -= OnDragChanged;
                inputReader.FlickReleased -= OnFlickReleased;
                inputReader.ResetBallRequested -= OnResetBallRequested;
                inputReader.NewMatchRequested -= OnNewMatchRequested;
                inputReader.CancelRequested -= OnCancelRequested;
            }

            if (restDetector != null)
            {
                restDetector.RestDetected -= OnRestDetected;
            }

            if (fieldGoalController != null)
            {
                fieldGoalController.FieldGoalScored -= OnFieldGoalScored;
            }
        }

        private void ApplyRuntimeConfiguration()
        {
            footballPhysics?.Configure(rules);
            restDetector?.Configure(rules);
            inputReader?.SetRules(rules);
            flickInteraction?.ApplyMatchState(match);
            overhangDebugOverlay?.Configure(this, null);
            trajectoryPreview?.Configure(footballPhysics != null ? footballPhysics.Rigidbody : null, rules);
        }

        private void OnDragChanged(FlickCommand command)
        {
            hud?.RenderFlick(command);

            if (match != null && match.Phase == MatchPhase.FieldGoalSetup)
            {
                aimIndicator?.Hide();
                PreviewFieldGoalKick(command);
                uncertaintyPreview?.Show(command, GetVarianceTuning());
                return;
            }

            trajectoryPreview?.Hide();
            if (command.IsValid)
            {
                aimIndicator?.Show(command);
                uncertaintyPreview?.Show(command, GetVarianceTuning());
            }
            else
            {
                aimIndicator?.Hide();
                uncertaintyPreview?.HideFlickPreview();
            }
        }

        private void OnFlickReleased(FlickCommand command)
        {
            if (match == null || !command.IsValid)
            {
                aimIndicator?.Hide();
                trajectoryPreview?.Hide();
                uncertaintyPreview?.HideFlickPreview();
                return;
            }

            if (match.Phase == MatchPhase.WaitingForFlick)
            {
                BeginNormalFlick(command);
                return;
            }

            if (match.Phase == MatchPhase.FieldGoalSetup)
            {
                TryLaunchFieldGoalKick(command);
                return;
            }

            aimIndicator?.Hide();
            trajectoryPreview?.Hide();
            uncertaintyPreview?.HideFlickPreview();
        }

        public FieldGoalKickResult PreviewFieldGoalKick(FlickCommand command)
        {
            FieldGoalKickResult kick = FieldGoalKickCalculator.Calculate(command, rules);
            if (match == null || match.Phase != MatchPhase.FieldGoalSetup || !kick.IsValid)
            {
                trajectoryPreview?.Hide();
                return kick;
            }

            trajectoryPreview?.Show(kick);
            return kick;
        }

        public bool TryLaunchFieldGoalKick(FlickCommand command)
        {
            ResolvedFlickParameters resolved = ResolveFlickParameters(command, "field_goal");
            FieldGoalKickResult kick = FieldGoalKickCalculator.Calculate(resolved.ToFlickCommand(), rules);
            if (!kick.IsValid || match == null || match.Phase != MatchPhase.FieldGoalSetup)
            {
                trajectoryPreview?.Hide();
                return false;
            }

            BeginFieldGoalAttempt(kick);
            return true;
        }

        private void BeginNormalFlick(FlickCommand command)
        {
            if (!match.TryBeginFlick())
            {
                aimIndicator?.Hide();
                return;
            }

            ResolvedFlickParameters resolved = ResolveFlickParameters(command, "normal_flick");
            fieldGoalController?.EndAttempt();
            fellResolved = false;
            restDetector?.ResetDetector();
            footballPhysics?.Flick(resolved.ToFlickCommand());
            aimIndicator?.Hide();
            uncertaintyPreview?.HideFlickPreview();
            Render();
        }

        private void BeginFieldGoalAttempt(FieldGoalKickResult kick)
        {
            if (!match.TryBeginFieldGoalAttempt())
            {
                aimIndicator?.Hide();
                trajectoryPreview?.Hide();
                return;
            }

            fieldGoalController?.BeginAttempt(match.CurrentPlayer);
            fellResolved = false;
            fieldGoalAttemptTimer = 0f;
            restDetector?.ResetDetector();
            footballPhysics?.KickFieldGoal(kick);
            aimIndicator?.Hide();
            trajectoryPreview?.Hide();
            Render();
        }

        private void OnRestDetected()
        {
            if (match == null || fellResolved)
            {
                return;
            }

            if (match.Phase == MatchPhase.FootballMoving)
            {
                ResolveStoppedFootball(false);
            }
            else if (match.Phase == MatchPhase.FieldGoalAttempt)
            {
                ResolveCurrentFieldGoal(fieldGoalController != null && fieldGoalController.ScoredThisAttempt);
            }
        }

        private void ResolveStoppedFootball(bool forceFell)
        {
            if (tableBoundary == null || footballCollider == null)
            {
                ResolveCurrentFlick(FlickResolutionType.InvalidState);
                return;
            }

            bool footballFell = forceFell || tableBoundary.HasFallen(footballPhysics != null ? footballPhysics.transform : null);
            OverhangDebugSnapshot snapshot = EdgeOverhangCalculator.CalculateSnapshot(
                tableBoundary.TableBounds,
                footballCollider.bounds,
                match.CurrentPlayer,
                rules,
                footballFell,
                match.CurrentFlickResolved);

            PublishOverhangSnapshot(snapshot);
            FlickResolutionType resolution = PaperFootballRules.ResolveStoppedFootball(footballFell, snapshot.ToOverhangResult());
            ResolveCurrentFlick(resolution);
        }

        private void ResolveCurrentFlick(FlickResolutionType resolution)
        {
            match.TryBeginResolving();
            match.ApplyResolution(resolution);
            MarkLatestOverhangProcessed();
            FlickResolved?.Invoke(resolution);

            if (resolution == FlickResolutionType.FellFromTable || resolution == FlickResolutionType.Touchdown)
            {
                if (match.Phase == MatchPhase.FieldGoalSetup)
                {
                    ResetBallToFieldGoalSpot();
                }
                else
                {
                    ResetBallToCurrentPlayerStart();
                }
            }

            Render();
        }

        private void OnFieldGoalScored()
        {
            if (match == null || match.Phase != MatchPhase.FieldGoalAttempt)
            {
                return;
            }

            ResolveCurrentFieldGoal(true);
        }

        private void ResolveCurrentFieldGoal(bool successful)
        {
            if (match == null)
            {
                return;
            }

            fieldGoalController?.EndAttempt();
            match.ApplyFieldGoalResult(successful);
            FieldGoalResolved?.Invoke(successful);
            fieldGoalAttemptTimer = 0f;
            ResetBallToCurrentPlayerStart();
            Render();
        }

        private void OnResetBallRequested()
        {
            if (match != null && match.IsFieldGoalMode)
            {
                ResetBallToFieldGoalSpot();
            }
            else
            {
                ResetBallToCurrentPlayerStart();
            }

            match?.ResetCurrentBall();
            Render();
        }

        private void OnNewMatchRequested()
        {
            match?.ResetMatch();
            trajectoryPreview?.Hide();
            uncertaintyPreview?.Hide();
            ResetBallToCurrentPlayerStart();
            Render();
        }

        private void OnCancelRequested()
        {
            aimIndicator?.Hide();
            trajectoryPreview?.Hide();
            uncertaintyPreview?.HideFlickPreview();
        }

        private void ResetBallToCurrentPlayerStart()
        {
            if (footballPhysics == null || match == null)
            {
                return;
            }

            Transform start = match.CurrentPlayer == PaperFootballPlayer.PlayerOne ? playerOneStart : playerTwoStart;
            Vector3 position = start != null ? start.position : Vector3.zero;
            Quaternion rotation = start != null ? start.rotation : Quaternion.identity;
            footballPhysics.PlaceAt(position, rotation);
            flickInteraction?.ClearSelection();
            restDetector?.ResetDetector();
            fellResolved = false;
        }

        private void ResetBallToFieldGoalSpot()
        {
            if (footballPhysics == null || match == null)
            {
                return;
            }

            Transform spot = fieldGoalController != null ? fieldGoalController.GetKickSpot(match.CurrentPlayer) : null;
            Transform fallback = match.CurrentPlayer == PaperFootballPlayer.PlayerOne ? playerOneStart : playerTwoStart;
            Transform start = spot != null ? spot : fallback;
            Vector3 position = start != null ? start.position : Vector3.zero;
            Quaternion rotation = start != null ? start.rotation : Quaternion.identity;
            footballPhysics.PlaceAt(position, rotation);
            flickInteraction?.ClearSelection();
            restDetector?.ResetDetector();
            fieldGoalController?.EndAttempt();
            fellResolved = false;
        }

        private void Render()
        {
            if (flickInteraction != null && match != null)
            {
                flickInteraction.ApplyMatchState(match);
                flickInteraction.SetInputSuppressed(inputSuppressed);
            }
            else if (inputReader != null && match != null)
            {
                inputReader.InputEnabled = !inputSuppressed &&
                                           (match.Phase == MatchPhase.WaitingForFlick ||
                                            match.Phase == MatchPhase.FieldGoalSetup);
            }

            hud?.Render(match);
            MatchStateRendered?.Invoke(match);
        }

        public bool TrySubmitFlick(FlickCommand command, string source = "external")
        {
            if (match == null || !command.IsValid)
            {
                return false;
            }

            if (match.Phase == MatchPhase.WaitingForFlick)
            {
                BeginNormalFlick(command);
                return true;
            }

            return match.Phase == MatchPhase.FieldGoalSetup && TryLaunchFieldGoalKick(command);
        }

        public void SetInputSuppressed(bool suppressed)
        {
            inputSuppressed = suppressed;
            Render();
        }

        public void ApplyRuntimeRules(PaperFootballRuleSet newRules)
        {
            if (match != null)
            {
                match.StateChanged -= Render;
            }

            rules = newRules != null ? newRules.Clone() : new PaperFootballRuleSet();
            rules.Sanitize();
            match = new PaperFootballMatch(rules);
            match.StateChanged += Render;
            ApplyRuntimeConfiguration();
            ResetBallToCurrentPlayerStart();
            Render();
        }

        public void ResetMatchAndBall()
        {
            match?.ResetMatch();
            trajectoryPreview?.Hide();
            uncertaintyPreview?.Hide();
            ResetBallToCurrentPlayerStart();
            Render();
        }

        public void AwardCurrentPlayerBonusTouchdown(string reason)
        {
            if (match == null || rules == null)
            {
                return;
            }

            match.AddBonusScore(match.CurrentPlayer, rules.touchdownPoints, reason);
            Render();
        }

        private ResolvedFlickParameters ResolveFlickParameters(FlickCommand command, string stableIdentifier)
        {
            if (shotVarianceController == null || match == null)
            {
                return ResolvedFlickParameters.FromUnmodified(command);
            }

            return shotVarianceController.Resolve(
                command,
                rules,
                match.CurrentPlayer,
                match.PossessionNumber,
                stableIdentifier);
        }

        private ShotVarianceTuning GetVarianceTuning()
        {
            return shotVarianceController != null ? shotVarianceController.CurrentTuning : ShotVarianceTuning.Disabled;
        }

        private void PublishOverhangSnapshot(OverhangDebugSnapshot snapshot)
        {
            latestOverhangSnapshot = snapshot;
            OverhangSnapshotChanged?.Invoke(snapshot);
        }

        private void MarkLatestOverhangProcessed()
        {
            if (!latestOverhangSnapshot.HasValue || match == null)
            {
                return;
            }

            latestOverhangSnapshot = latestOverhangSnapshot.Value.WithScoringEventProcessed(match.CurrentFlickResolved);
            OverhangSnapshotChanged?.Invoke(latestOverhangSnapshot.Value);
        }
    }
}
