using System;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Presentation;
using PaperFootball.Tabletop.Roguelike.Random;
using PaperFootball.Tabletop.Roguelike.Variance;
using PaperFootball.Tabletop.Rules;
using PaperFootball.Tabletop.Scoring;
using PaperFootball.Tabletop.Shots;
using UnityEngine;

namespace PaperFootball.Tabletop.Match
{
    public class MatchController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PaperFootballConfig config;
        [SerializeField] private AirFlickShotSettings airFlickSettings;

        [Header("References")]
        [SerializeField] private FootballPhysicsController footballPhysics;
        [SerializeField] private AirFlickLandingController airFlickLanding;
        [SerializeField] private FootballRestDetector restDetector;
        [SerializeField] private FlickInputReader inputReader;
        [SerializeField] private FlickInteractionController flickInteraction;
        [SerializeField] private ShotSelectionController shotSelection;
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
        private FootballShotType selectedNormalShotType = FootballShotType.FlatTableShot;
        private ShotExecutionContext activeShotContext = ShotExecutionContext.None;
        private AirFlickShotSettings runtimeAirFlickSettings;
        private float airFlickForwardImpulseMultiplier = 1f;
        private float airFlickUpwardImpulseMultiplier = 1f;
        private float airFlickLaunchAngleAdd;
        private float airFlickForceVarianceMultiplier = 1f;
        private float airFlickDirectionVarianceMultiplier = 1f;
        private float airFlickContactVarianceMultiplier = 1f;
        private float airFlickLandingVarianceMultiplier = 1f;
        private float airFlickBounceMultiplier = 1f;
        private float airFlickLandingYawMultiplier = 1f;
        private float airFlickPreviewAccuracyBonus;

        public PaperFootballMatch Match => match;
        public PaperFootballRuleSet CurrentRules => rules != null ? rules.Clone() : new PaperFootballRuleSet();
        public Bounds TableBounds => tableBoundary != null ? tableBoundary.TableBounds : new Bounds(Vector3.zero, Vector3.zero);
        public OverhangDebugSnapshot? LatestOverhangSnapshot => latestOverhangSnapshot;
        public TrajectoryPreviewRenderer TrajectoryPreview => trajectoryPreview;
        public FootballShotType SelectedNormalShotType => selectedNormalShotType;
        public ShotExecutionContext ActiveShotContext => activeShotContext;
        public AirFlickShotSettings CurrentAirFlickSettings => runtimeAirFlickSettings != null ? runtimeAirFlickSettings : airFlickSettings;

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
            ShotUncertaintyPreview shotUncertaintyPreview = null,
            ShotSelectionController shotSelectionController = null,
            AirFlickLandingController landingController = null,
            AirFlickShotSettings airFlickShotSettings = null)
        {
            config = rulesConfig;
            footballPhysics = physicsController;
            airFlickLanding = landingController;
            restDetector = detector;
            inputReader = reader;
            flickInteraction = interactionController;
            shotSelection = shotSelectionController;
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
            airFlickSettings = airFlickShotSettings;
            RebuildRuntimeAirFlickSettings();
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

            if (shotSelection != null)
            {
                shotSelection.NormalShotTypeChanged += OnNormalShotTypeChanged;
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

            if (shotSelection != null)
            {
                shotSelection.NormalShotTypeChanged -= OnNormalShotTypeChanged;
            }
        }

        private void ApplyRuntimeConfiguration()
        {
            EnsureRuntimeReferences();
            RebuildRuntimeAirFlickSettings();
            footballPhysics?.Configure(rules);
            airFlickLanding?.Configure(footballPhysics, tableBoundary != null ? tableBoundary.TableCollider : null, runtimeAirFlickSettings);
            restDetector?.Configure(rules);
            inputReader?.SetRules(rules);
            SetInputShotTypeForCurrentPhase();
            flickInteraction?.ApplyMatchState(match);
            shotSelection?.ApplyMatchState(match, inputSuppressed, flickInteraction != null ? flickInteraction.State : FlickInteractionState.Disabled);
            overhangDebugOverlay?.Configure(this, null);
            trajectoryPreview?.Configure(footballPhysics != null ? footballPhysics.Rigidbody : null, rules);
        }

        private void OnDragChanged(FlickCommand command)
        {
            hud?.RenderFlick(command);

            if (match != null && match.Phase == MatchPhase.FieldGoalSetup)
            {
                aimIndicator?.Hide();
                PreviewFieldGoalKick(command.WithShotType(FootballShotType.FieldGoalKick));
                uncertaintyPreview?.Show(command, GetVarianceTuning());
                return;
            }

            FootballShotType shotType = ResolveNormalShotType(command);
            if (shotType == FootballShotType.AirFlickShot && command.IsValid)
            {
                aimIndicator?.Hide();
                PreviewAirFlickShot(command.WithShotType(FootballShotType.AirFlickShot), generateLandingVariance: false);
                uncertaintyPreview?.Show(command, GetVarianceTuning(FootballShotType.AirFlickShot));
                return;
            }

            trajectoryPreview?.Hide();
            if (command.IsValid)
            {
                aimIndicator?.Show(command);
                uncertaintyPreview?.Show(command, GetVarianceTuning(FootballShotType.FlatTableShot));
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
                TryLaunchFieldGoalKick(command.WithShotType(FootballShotType.FieldGoalKick));
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

        public AirFlickShotResult PreviewAirFlickShot(FlickCommand command, bool generateLandingVariance = false)
        {
            ShotExecutionContext context = ShotExecutionContext.Normal(
                FootballShotType.AirFlickShot,
                match != null ? match.CurrentPlayer : PaperFootballPlayer.PlayerOne,
                shotVarianceController != null ? shotVarianceController.RunSeed : 0,
                shotVarianceController != null ? shotVarianceController.EncounterIndex : 0,
                match != null ? match.PossessionNumber : 0,
                shotVarianceController != null ? shotVarianceController.FlickSequenceNumber + 1 : 0);

            AirFlickShotResult result = AirFlickShotCalculator.Calculate(
                command.WithShotType(FootballShotType.AirFlickShot),
                rules,
                runtimeAirFlickSettings,
                footballCollider,
                null,
                context,
                generateLandingVariance);

            if (match == null || match.Phase != MatchPhase.WaitingForFlick || !result.IsValid)
            {
                trajectoryPreview?.Hide();
                return result;
            }

            trajectoryPreview?.ShowAirFlick(result, runtimeAirFlickSettings);
            return result;
        }

        public bool TryLaunchFieldGoalKick(FlickCommand command)
        {
            ResolvedFlickParameters resolved = ResolveFlickParameters(command.WithShotType(FootballShotType.FieldGoalKick), "field_goal", FootballShotType.FieldGoalKick);
            FieldGoalKickResult kick = FieldGoalKickCalculator.Calculate(resolved.ToFlickCommand().WithShotType(FootballShotType.FieldGoalKick), rules);
            if (!kick.IsValid || match == null || match.Phase != MatchPhase.FieldGoalSetup)
            {
                trajectoryPreview?.Hide();
                return false;
            }

            ShotExecutionContext context = ShotExecutionContext.FieldGoal(
                match.CurrentPlayer,
                shotVarianceController != null ? shotVarianceController.RunSeed : 0,
                shotVarianceController != null ? shotVarianceController.EncounterIndex : 0,
                match.PossessionNumber,
                resolved.FlickSequenceNumber);
            BeginFieldGoalAttempt(kick, context);
            return true;
        }

        private void BeginNormalFlick(FlickCommand command)
        {
            if (!match.TryBeginFlick())
            {
                aimIndicator?.Hide();
                return;
            }

            FootballShotType shotType = ResolveNormalShotType(command);
            ResolvedFlickParameters resolved = ResolveFlickParameters(command.WithShotType(shotType), StableIdentifierForShot(shotType), shotType);
            activeShotContext = ShotExecutionContext.Normal(
                shotType,
                match.CurrentPlayer,
                shotVarianceController != null ? shotVarianceController.RunSeed : 0,
                shotVarianceController != null ? shotVarianceController.EncounterIndex : 0,
                match.PossessionNumber,
                resolved.FlickSequenceNumber);

            fieldGoalController?.EndAttempt();
            fieldGoalController?.SetNonScoringShotContext(activeShotContext);
            fellResolved = false;
            restDetector?.ResetDetector();
            if (shotType == FootballShotType.AirFlickShot)
            {
                LaunchAirFlick(resolved, activeShotContext);
            }
            else
            {
                airFlickLanding?.ResetState();
                footballPhysics?.Flick(resolved.ToFlickCommand().WithShotType(FootballShotType.FlatTableShot));
            }

            aimIndicator?.Hide();
            trajectoryPreview?.Hide();
            uncertaintyPreview?.HideFlickPreview();
            Render();
        }

        private void BeginFieldGoalAttempt(FieldGoalKickResult kick, ShotExecutionContext context)
        {
            if (!match.TryBeginFieldGoalAttempt())
            {
                aimIndicator?.Hide();
                trajectoryPreview?.Hide();
                return;
            }

            activeShotContext = context;
            airFlickLanding?.ResetState();
            fieldGoalController?.BeginAttempt(match.CurrentPlayer, context);
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
                if (activeShotContext.ShotType == FootballShotType.AirFlickShot &&
                    airFlickLanding != null &&
                    airFlickLanding.CurrentState == AirFlickState.Airborne)
                {
                    return;
                }

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
            if (activeShotContext.ShotType == FootballShotType.AirFlickShot)
            {
                airFlickLanding?.MarkResolved();
            }

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

            activeShotContext = ShotExecutionContext.None;
            shotSelection?.ResetNormalShotType();
            selectedNormalShotType = FootballShotType.FlatTableShot;
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
            activeShotContext = ShotExecutionContext.None;
            airFlickLanding?.ResetState();
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
            activeShotContext = ShotExecutionContext.None;
            airFlickLanding?.ResetState();
            Render();
        }

        private void OnNewMatchRequested()
        {
            match?.ResetMatch();
            trajectoryPreview?.Hide();
            uncertaintyPreview?.Hide();
            activeShotContext = ShotExecutionContext.None;
            airFlickLanding?.ResetState();
            selectedNormalShotType = FootballShotType.FlatTableShot;
            shotSelection?.ResetNormalShotType();
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
            airFlickLanding?.ResetState();
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
            airFlickLanding?.ResetState();
            fellResolved = false;
        }

        private void Render()
        {
            SetInputShotTypeForCurrentPhase();
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

            shotSelection?.ApplyMatchState(match, inputSuppressed, flickInteraction != null ? flickInteraction.State : FlickInteractionState.Disabled);
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

            return match.Phase == MatchPhase.FieldGoalSetup && TryLaunchFieldGoalKick(command.WithShotType(FootballShotType.FieldGoalKick));
        }

        public void SetInputSuppressed(bool suppressed)
        {
            if (inputSuppressed == suppressed)
            {
                return;
            }

            inputSuppressed = suppressed;
            Render();
        }

        public void SetAirFlickModifierScales(
            float forwardImpulseMultiplier,
            float upwardImpulseMultiplier,
            float launchAngleAdd,
            float forceVarianceMultiplier,
            float directionVarianceMultiplier,
            float contactVarianceMultiplier,
            float landingVarianceMultiplier,
            float bounceMultiplier,
            float landingYawMultiplier,
            float previewAccuracyBonus)
        {
            airFlickForwardImpulseMultiplier = Mathf.Max(0.05f, forwardImpulseMultiplier);
            airFlickUpwardImpulseMultiplier = Mathf.Max(0.05f, upwardImpulseMultiplier);
            airFlickLaunchAngleAdd = launchAngleAdd;
            airFlickForceVarianceMultiplier = Mathf.Max(0f, forceVarianceMultiplier);
            airFlickDirectionVarianceMultiplier = Mathf.Max(0f, directionVarianceMultiplier);
            airFlickContactVarianceMultiplier = Mathf.Max(0f, contactVarianceMultiplier);
            airFlickLandingVarianceMultiplier = Mathf.Max(0f, landingVarianceMultiplier);
            airFlickBounceMultiplier = Mathf.Max(0f, bounceMultiplier);
            airFlickLandingYawMultiplier = Mathf.Max(0f, landingYawMultiplier);
            airFlickPreviewAccuracyBonus = previewAccuracyBonus;
            RebuildRuntimeAirFlickSettings();
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
            activeShotContext = ShotExecutionContext.None;
            airFlickLanding?.ResetState();
            selectedNormalShotType = FootballShotType.FlatTableShot;
            shotSelection?.ResetNormalShotType();
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

        private void LaunchAirFlick(ResolvedFlickParameters resolved, ShotExecutionContext context)
        {
            int landingSeed = StableSeedUtility.DeriveSeed(
                context.RunSeed,
                RunRandomStream.ShotVariance,
                context.EncounterIndex,
                context.Player,
                context.PossessionNumber,
                context.ShotSequenceNumber,
                "air_flick_landing");

            AirFlickShotResult result = AirFlickShotCalculator.Calculate(
                resolved.ToFlickCommand().WithShotType(FootballShotType.AirFlickShot),
                rules,
                runtimeAirFlickSettings,
                footballCollider,
                new DeterministicRunRandom(landingSeed),
                context);

            if (!result.IsValid)
            {
                airFlickLanding?.ResetState();
                footballPhysics?.Flick(resolved.ToFlickCommand().WithShotType(FootballShotType.FlatTableShot));
                return;
            }

            airFlickLanding?.BeginTracking(result);
            footballPhysics?.AirFlick(result);
        }

        private void OnNormalShotTypeChanged(FootballShotType shotType)
        {
            selectedNormalShotType = shotType == FootballShotType.AirFlickShot
                ? FootballShotType.AirFlickShot
                : FootballShotType.FlatTableShot;
            SetInputShotTypeForCurrentPhase();
        }

        private void SetInputShotTypeForCurrentPhase()
        {
            FootballShotType shotType = GetInputShotTypeForCurrentPhase();
            inputReader?.SetShotType(shotType);
            flickInteraction?.SetShotType(shotType);
        }

        private FootballShotType GetInputShotTypeForCurrentPhase()
        {
            if (match != null && (match.Phase == MatchPhase.FieldGoalSetup || match.Phase == MatchPhase.FieldGoalAttempt))
            {
                return FootballShotType.FieldGoalKick;
            }

            if (match != null && (match.Phase == MatchPhase.FootballMoving || match.Phase == MatchPhase.ResolvingFlick))
            {
                return activeShotContext.ShotType;
            }

            return selectedNormalShotType == FootballShotType.AirFlickShot
                ? FootballShotType.AirFlickShot
                : FootballShotType.FlatTableShot;
        }

        private FootballShotType ResolveNormalShotType(FlickCommand command)
        {
            if (command.ShotType == FootballShotType.AirFlickShot)
            {
                return FootballShotType.AirFlickShot;
            }

            return selectedNormalShotType == FootballShotType.AirFlickShot
                ? FootballShotType.AirFlickShot
                : FootballShotType.FlatTableShot;
        }

        private ResolvedFlickParameters ResolveFlickParameters(FlickCommand command, string stableIdentifier, FootballShotType shotType)
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
                stableIdentifier,
                GetVarianceTuning(shotType));
        }

        private ShotVarianceTuning GetVarianceTuning()
        {
            return GetVarianceTuning(FootballShotType.FlatTableShot);
        }

        private ShotVarianceTuning GetVarianceTuning(FootballShotType shotType)
        {
            ShotVarianceTuning tuning = shotVarianceController != null ? shotVarianceController.CurrentTuning : ShotVarianceTuning.Disabled;
            if (shotType != FootballShotType.AirFlickShot || runtimeAirFlickSettings == null)
            {
                return tuning;
            }

            return tuning.Scaled(
                runtimeAirFlickSettings.ForceVarianceMultiplier * airFlickForceVarianceMultiplier,
                runtimeAirFlickSettings.DirectionVarianceMultiplier * airFlickDirectionVarianceMultiplier,
                runtimeAirFlickSettings.ContactVarianceMultiplier * airFlickContactVarianceMultiplier,
                airFlickPreviewAccuracyBonus);
        }

        private void EnsureRuntimeReferences()
        {
            if (airFlickLanding == null && footballPhysics != null)
            {
                airFlickLanding = footballPhysics.GetComponent<AirFlickLandingController>();
                if (airFlickLanding == null)
                {
                    airFlickLanding = footballPhysics.gameObject.AddComponent<AirFlickLandingController>();
                }
            }

            if (shotSelection == null)
            {
                shotSelection = FindFirstObjectByType<ShotSelectionController>();
            }

            if (shotSelection == null && hud != null)
            {
                shotSelection = ShotSelectionController.CreateRuntimeHud(hud.transform);
            }

            if (shotSelection != null)
            {
                shotSelection.NormalShotTypeChanged -= OnNormalShotTypeChanged;
                shotSelection.NormalShotTypeChanged += OnNormalShotTypeChanged;
            }
        }

        private void RebuildRuntimeAirFlickSettings()
        {
            AirFlickShotSettings source = airFlickSettings != null ? airFlickSettings : AirFlickShotSettings.CreateRuntimeDefault();
            runtimeAirFlickSettings = source.WithRuntimeMultipliers(
                airFlickForwardImpulseMultiplier,
                airFlickUpwardImpulseMultiplier,
                airFlickLaunchAngleAdd,
                airFlickLandingVarianceMultiplier,
                airFlickBounceMultiplier,
                airFlickLandingYawMultiplier,
                airFlickPreviewAccuracyBonus);
        }

        private static string StableIdentifierForShot(FootballShotType shotType)
        {
            return shotType == FootballShotType.AirFlickShot ? "air_flick" : "flat_table_shot";
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
