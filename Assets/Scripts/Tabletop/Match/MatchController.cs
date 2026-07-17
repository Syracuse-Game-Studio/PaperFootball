using System;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Presentation;
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
        [SerializeField] private TableBoundaryDetector tableBoundary;
        [SerializeField] private GameHudController hud;
        [SerializeField] private FlickAimIndicator aimIndicator;
        [SerializeField] private OverhangDebugOverlay overhangDebugOverlay;
        [SerializeField] private TrajectoryPreviewRenderer trajectoryPreview;
        [SerializeField] private FieldGoalController fieldGoalController;
        [SerializeField] private Collider footballCollider;
        [SerializeField] private Transform playerOneStart;
        [SerializeField] private Transform playerTwoStart;

        private PaperFootballRuleSet rules;
        private PaperFootballMatch match;
        private bool fellResolved;
        private OverhangDebugSnapshot? latestOverhangSnapshot;
        private float fieldGoalAttemptTimer;

        public PaperFootballMatch Match => match;
        public OverhangDebugSnapshot? LatestOverhangSnapshot => latestOverhangSnapshot;
        public TrajectoryPreviewRenderer TrajectoryPreview => trajectoryPreview;

        public event Action<OverhangDebugSnapshot> OverhangSnapshotChanged;

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
            Transform p2Start)
        {
            config = rulesConfig;
            footballPhysics = physicsController;
            restDetector = detector;
            inputReader = reader;
            tableBoundary = boundaryDetector;
            hud = hudController;
            aimIndicator = indicator;
            overhangDebugOverlay = debugOverlay;
            trajectoryPreview = trajectoryRenderer;
            fieldGoalController = goalController;
            footballCollider = football;
            playerOneStart = p1Start;
            playerTwoStart = p2Start;
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

            if (inputReader != null)
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

            if (inputReader != null)
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
                return;
            }

            trajectoryPreview?.Hide();
            if (command.IsValid)
            {
                aimIndicator?.Show(command);
            }
            else
            {
                aimIndicator?.Hide();
            }
        }

        private void OnFlickReleased(FlickCommand command)
        {
            if (match == null || !command.IsValid)
            {
                aimIndicator?.Hide();
                trajectoryPreview?.Hide();
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
            FieldGoalKickResult kick = FieldGoalKickCalculator.Calculate(command, rules);
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

            fieldGoalController?.EndAttempt();
            fellResolved = false;
            restDetector?.ResetDetector();
            footballPhysics?.Flick(command);
            aimIndicator?.Hide();
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
            ResetBallToCurrentPlayerStart();
            Render();
        }

        private void OnCancelRequested()
        {
            aimIndicator?.Hide();
            trajectoryPreview?.Hide();
        }

        private void ResetBallToCurrentPlayerStart()
        {
            if (footballPhysics == null || match == null)
            {
                return;
            }

            Transform start = match.CurrentPlayer == PaperFootballPlayer.PlayerOne ? playerOneStart : playerTwoStart;
            Vector3 position = start != null ? start.position : Vector3.zero;
            Quaternion rotation = start != null ? start.rotation : Quaternion.Euler(90f, 0f, 0f);
            footballPhysics.PlaceAt(position, rotation);
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
            restDetector?.ResetDetector();
            fieldGoalController?.EndAttempt();
            fellResolved = false;
        }

        private void Render()
        {
            if (inputReader != null && match != null)
            {
                inputReader.InputEnabled = match.Phase == MatchPhase.WaitingForFlick ||
                                           match.Phase == MatchPhase.FieldGoalSetup;
            }

            hud?.Render(match);
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
