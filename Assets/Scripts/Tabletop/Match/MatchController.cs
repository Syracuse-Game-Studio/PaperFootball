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
        [SerializeField] private FieldGoalController fieldGoalController;
        [SerializeField] private Collider footballCollider;
        [SerializeField] private Transform playerOneStart;
        [SerializeField] private Transform playerTwoStart;

        private PaperFootballRuleSet rules;
        private PaperFootballMatch match;
        private bool fellResolved;

        public PaperFootballMatch Match => match;

        public void Configure(
            PaperFootballConfig rulesConfig,
            FootballPhysicsController physicsController,
            FootballRestDetector detector,
            FlickInputReader reader,
            TableBoundaryDetector boundaryDetector,
            GameHudController hudController,
            FlickAimIndicator indicator,
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
                    ResolveCurrentFlick(FlickResolutionType.FellFromTable);
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
        }

        private void OnDragChanged(FlickCommand command)
        {
            hud?.RenderFlick(command);
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
                return;
            }

            if (match.Phase == MatchPhase.WaitingForFlick)
            {
                BeginNormalFlick(command);
                return;
            }

            if (match.Phase == MatchPhase.FieldGoalSetup)
            {
                BeginFieldGoalAttempt(command);
                return;
            }

            aimIndicator?.Hide();
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

        private void BeginFieldGoalAttempt(FlickCommand command)
        {
            if (!match.TryBeginFieldGoalAttempt())
            {
                aimIndicator?.Hide();
                return;
            }

            fieldGoalController?.BeginAttempt(match.CurrentPlayer);
            fellResolved = false;
            restDetector?.ResetDetector();
            float upwardImpulse = Mathf.Max(2f, command.Force * 0.35f);
            footballPhysics?.Flick(command, upwardImpulse);
            aimIndicator?.Hide();
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
                ResolveStoppedFootball();
            }
            else if (match.Phase == MatchPhase.FieldGoalAttempt)
            {
                ResolveCurrentFieldGoal(fieldGoalController != null && fieldGoalController.ScoredThisAttempt);
            }
        }

        private void ResolveStoppedFootball()
        {
            if (tableBoundary == null || footballCollider == null)
            {
                ResolveCurrentFlick(FlickResolutionType.InvalidState);
                return;
            }

            EdgeOverhangResult overhang = EdgeOverhangCalculator.Calculate(
                tableBoundary.TableBounds,
                footballCollider.bounds,
                match.CurrentPlayer,
                rules);

            bool footballFell = tableBoundary.HasFallen(footballPhysics != null ? footballPhysics.transform : null);
            FlickResolutionType resolution = PaperFootballRules.ResolveStoppedFootball(footballFell, overhang);
            ResolveCurrentFlick(resolution);
        }

        private void ResolveCurrentFlick(FlickResolutionType resolution)
        {
            match.TryBeginResolving();
            match.ApplyResolution(resolution);

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
            ResetBallToCurrentPlayerStart();
            Render();
        }

        private void OnCancelRequested()
        {
            aimIndicator?.Hide();
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
    }
}
