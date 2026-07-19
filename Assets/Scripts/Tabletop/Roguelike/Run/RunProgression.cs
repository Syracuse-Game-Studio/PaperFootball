using System;
using System.Collections.Generic;
using System.Linq;
using PaperFootball.Tabletop.Match;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Presentation;
using PaperFootball.Tabletop.Roguelike.Consumables;
using PaperFootball.Tabletop.Roguelike.Encounters;
using PaperFootball.Tabletop.Roguelike.Modifiers;
using PaperFootball.Tabletop.Roguelike.Opponents;
using PaperFootball.Tabletop.Roguelike.Presentation;
using PaperFootball.Tabletop.Roguelike.Random;
using PaperFootball.Tabletop.Roguelike.Variance;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Roguelike.Run
{
    public enum RunStatus
    {
        NotStarted,
        Active,
        Won,
        Lost,
        Abandoned
    }

    [Serializable]
    public sealed class RunEncounterResult
    {
        public string encounterId;
        public bool succeeded;
        public string resultText;
    }

    [Serializable]
    public sealed class RunStatistics
    {
        public int flicks;
        public int touchdowns;
        public int fieldGoals;
        public int falls;
        public float totalTravelDistance;
        public float highestSpin;
        public float longestFlick;
        public int successfulPrecisionAttempts;
        public int encounterWins;

        public void RecordFlick(FlickResolutionType resolution, FootballPhysicsController physics)
        {
            flicks++;
            if (resolution == FlickResolutionType.Touchdown)
            {
                touchdowns++;
            }
            else if (resolution == FlickResolutionType.FellFromTable)
            {
                falls++;
            }

            if (physics != null)
            {
                totalTravelDistance += physics.LastFlickTravelDistance;
                longestFlick = Mathf.Max(longestFlick, physics.LastFlickTravelDistance);
                highestSpin = Mathf.Max(highestSpin, Mathf.Abs(physics.LastFlickPeakYawVelocity));
            }
        }

        public void RecordFieldGoal(bool successful)
        {
            if (successful)
            {
                fieldGoals++;
            }
        }
    }

    [Serializable]
    public sealed class RunSnapshot
    {
        public int version = 1;
        public int runSeed;
        public int currentEncounterIndex;
        public RunStatus runStatus;
        public List<AppliedUpgradeSnapshot> upgrades = new();
        public List<string> consumableIds = new();
        public List<RunEncounterResult> encounterResults = new();
        public RunStatistics statistics = new();
    }

    [Serializable]
    public sealed class RunState
    {
        public int runSeed;
        public int currentEncounterIndex;
        public RunStatus status = RunStatus.NotStarted;
        public List<GeneratedEncounter> encounters = new();
        public FootballBuild playerBuild = new();
        public List<RunEncounterResult> results = new();
        public RunStatistics statistics = new();

        public GeneratedEncounter CurrentEncounter =>
            currentEncounterIndex >= 0 && currentEncounterIndex < encounters.Count ? encounters[currentEncounterIndex] : null;

        public RunSnapshot ToSnapshot()
        {
            return new RunSnapshot
            {
                runSeed = runSeed,
                currentEncounterIndex = currentEncounterIndex,
                runStatus = status,
                upgrades = playerBuild.UpgradeSnapshots.Select(snapshot => new AppliedUpgradeSnapshot(snapshot.stableId, snapshot.stackCount)).ToList(),
                encounterResults = results.Select(result => new RunEncounterResult
                {
                    encounterId = result.encounterId,
                    succeeded = result.succeeded,
                    resultText = result.resultText
                }).ToList(),
                statistics = statistics
            };
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(ToSnapshot(), true);
        }
    }

    public partial class RunController
    {
        [Header("Catalogs")]
        [SerializeField] private UpgradeCatalog upgradeCatalog;
        [SerializeField] private OpponentCatalog opponentCatalog;
        [SerializeField] private TableSurfaceCatalog surfaceCatalog;
        [SerializeField] private ObstacleLayoutCatalog obstacleCatalog;

        [Header("Scene References")]
        [SerializeField] private MatchController matchController;
        [SerializeField] private FootballPhysicsController footballPhysics;
        [SerializeField] private ShotVarianceController shotVarianceController;
        [SerializeField] private OpponentTurnController opponentTurnController;
        [SerializeField] private TableSurfaceApplier tableSurfaceApplier;
        [SerializeField] private ObstacleLayoutController obstacleLayoutController;
        [SerializeField] private TemporaryPlacementController temporaryPlacementController;
        [SerializeField] private PrecisionTargetZone precisionTargetZone;
        [SerializeField] private RunProgressionUiController runUi;

        [Header("Runtime")]
        [SerializeField] private bool autoOpenRunStartFromLauncher = true;
        [SerializeField] private int randomSeedFallback = 12345;

        private PaperFootballRuleSet baseRules = new();
        private RunState state = new();
        private List<FootballUpgradeDefinition> pendingRewards = new();
        private bool encounterStarted;
        private bool encounterCompletionHandled;
        private int precisionAttemptsUsed;
        private int completedFlicksThisEncounter;
        private bool subscribed;

        public RunState State => state;
        public bool IsRunActive => state.status == RunStatus.Active;
        public UpgradeCatalog UpgradeCatalog => upgradeCatalog;
        public IReadOnlyList<FootballUpgradeDefinition> PendingRewards => pendingRewards;
        public GeneratedEncounter CurrentEncounter => state.CurrentEncounter;

        public event Action<RunState> RunStateChanged;

        public void Configure(
            UpgradeCatalog upgrades,
            OpponentCatalog opponents,
            TableSurfaceCatalog surfaces,
            ObstacleLayoutCatalog obstacles,
            MatchController match,
            FootballPhysicsController physics,
            ShotVarianceController variance,
            OpponentTurnController aiController,
            TableSurfaceApplier surfaceApplier,
            ObstacleLayoutController layoutController,
            TemporaryPlacementController placementController,
            PrecisionTargetZone targetZone,
            RunProgressionUiController ui)
        {
            upgradeCatalog = upgrades;
            opponentCatalog = opponents;
            surfaceCatalog = surfaces;
            obstacleCatalog = obstacles;
            matchController = match;
            footballPhysics = physics;
            shotVarianceController = variance;
            opponentTurnController = aiController;
            tableSurfaceApplier = surfaceApplier;
            obstacleLayoutController = layoutController;
            temporaryPlacementController = placementController;
            precisionTargetZone = targetZone;
            runUi = ui;
            WireUi();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void StartRunWithSeedText(string seedText)
        {
            int seed = ParseSeed(seedText, randomSeedFallback);
            StartRun(seed);
        }

        public void StartRunWithRandomSeed()
        {
            StartRun(Environment.TickCount & 0x7fffffff);
        }

        public void StartRun(int seed)
        {
            baseRules = matchController != null ? matchController.CurrentRules.Clone() : new PaperFootballRuleSet();
            baseRules.Sanitize();

            state = new RunState
            {
                runSeed = seed,
                currentEncounterIndex = 0,
                status = RunStatus.Active,
                encounters = EncounterGenerator.GenerateSixEncounterRun(seed, opponentCatalog, surfaceCatalog, obstacleCatalog),
                playerBuild = new FootballBuild(),
                statistics = new RunStatistics()
            };

            shotVarianceController?.SetRunSeed(seed);
            shotVarianceController?.SetVarianceEnabled(true);
            pendingRewards.Clear();
            LoadCurrentEncounter(showIntro: true);
            PublishState();
        }

        public void ReturnToLocalMatch()
        {
            state.status = RunStatus.Abandoned;
            pendingRewards.Clear();
            encounterStarted = false;
            encounterCompletionHandled = false;
            precisionAttemptsUsed = 0;
            completedFlicksThisEncounter = 0;
            shotVarianceController?.SetVarianceEnabled(false);
            opponentTurnController?.SetAiEnabled(false);
            obstacleLayoutController?.Clear();
            temporaryPlacementController?.ClearTemporaryObjects();
            tableSurfaceApplier?.Apply(null);
            precisionTargetZone?.Hide();
            runUi?.ShowLocalMatchNotice();
            matchController?.SetInputSuppressed(false);
            matchController?.ResetMatchAndBall();
            PublishState();
        }

        public void BeginCurrentEncounter()
        {
            if (state.status != RunStatus.Active || CurrentEncounter == null)
            {
                return;
            }

            encounterStarted = true;
            encounterCompletionHandled = false;
            runUi?.ShowActiveEncounter(state, CurrentEncounter, BuildEvaluation(), pendingRewards);
            matchController?.SetInputSuppressed(false);
            PublishState();
        }

        public void ChooseReward(FootballUpgradeDefinition upgrade)
        {
            if (upgrade == null || pendingRewards.Count == 0)
            {
                return;
            }

            if (state.playerBuild.Apply(upgrade))
            {
                pendingRewards.Clear();
                ApplyBuildToRuntime();
                AdvanceToNextEncounter();
            }
        }

        public string CreateDebugSnapshotJson()
        {
            return state.ToJson();
        }

        private void Awake()
        {
            if (matchController != null)
            {
                baseRules = matchController.CurrentRules.Clone();
            }
        }

        private void OnEnable()
        {
            Subscribe();
            WireUi();
        }

        private void Start()
        {
            if (autoOpenRunStartFromLauncher && PrototypeLaunchOptions.ConsumeStartRunRequested())
            {
                runUi?.ShowRunStart(randomSeedFallback.ToString());
            }
            else
            {
                runUi?.HideRunPanels();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (matchController != null)
            {
                matchController.FlickResolved += OnFlickResolved;
                matchController.FieldGoalResolved += OnFieldGoalResolved;
                matchController.MatchStateRendered += OnMatchStateRendered;
                subscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (matchController != null)
            {
                matchController.FlickResolved -= OnFlickResolved;
                matchController.FieldGoalResolved -= OnFieldGoalResolved;
                matchController.MatchStateRendered -= OnMatchStateRendered;
            }

            subscribed = false;
        }

        private void WireUi()
        {
            if (runUi == null)
            {
                return;
            }

            runUi.Bind(this);
        }

        private void LoadCurrentEncounter(bool showIntro)
        {
            GeneratedEncounter encounter = CurrentEncounter;
            if (encounter == null)
            {
                CompleteRun(true);
                return;
            }

            encounterStarted = false;
            encounterCompletionHandled = false;
            precisionAttemptsUsed = 0;
            completedFlicksThisEncounter = 0;
            ApplyBuildToRuntime();
            ApplyEncounterSetup(encounter);

            if (showIntro)
            {
                runUi?.ShowEncounterIntro(state, encounter, ResolveOpponent(encounter), ResolveSurface(encounter), ResolveLayout(encounter));
                matchController?.SetInputSuppressed(true);
            }
            else
            {
                BeginCurrentEncounter();
            }

            PublishState();
        }

        private void ApplyEncounterSetup(GeneratedEncounter encounter)
        {
            PaperFootballRuleSet encounterRules = BuildEvaluation().ApplyToRules(baseRules);
            encounterRules.targetScore = Mathf.Max(1, encounter.targetScore);
            encounterRules.maximumPossessions = Mathf.Max(0, encounter.maximumPossessions);
            if (encounter.encounterType == EncounterType.EliteMatch)
            {
                shotVarianceController?.SetModifierScales(
                    BuildEvaluation().ForceVarianceScale,
                    BuildEvaluation().DirectionVarianceScale * 1.25f,
                    BuildEvaluation().ContactPointVarianceScale * 1.25f,
                    BuildEvaluation().PreviewAccuracyBonus);
            }

            matchController?.ApplyRuntimeRules(encounterRules);
            matchController?.ResetMatchAndBall();

            TableSurfaceDefinition surface = ResolveSurface(encounter);
            tableSurfaceApplier?.Apply(surface);
            ObstacleLayoutDefinition layout = ResolveLayout(encounter);
            obstacleLayoutController?.Apply(layout);
            temporaryPlacementController?.ClearTemporaryObjects();

            shotVarianceController?.SetEncounterIndex(encounter.stageIndex);
            shotVarianceController?.SetVarianceEnabled(true);

            OpponentProfile opponent = ResolveOpponent(encounter);
            opponentTurnController?.SetOpponent(opponent);
            opponentTurnController?.SetRunContext(
                state.runSeed,
                encounter.stageIndex,
                matchController != null ? matchController.TableBounds : new Bounds(Vector3.zero, new Vector3(8f, 1f, 12f)));
            opponentTurnController?.SetAiEnabled(encounter.encounterType != EncounterType.PrecisionDrill);

            if (encounter.encounterType == EncounterType.PrecisionDrill)
            {
                precisionTargetZone?.Show(encounter.precisionTargetCenter, encounter.precisionTargetSize);
            }
            else
            {
                precisionTargetZone?.Hide();
            }
        }

        private void ApplyBuildToRuntime()
        {
            FootballBuildEvaluation evaluation = BuildEvaluation();
            shotVarianceController?.SetModifierScales(
                evaluation.ForceVarianceScale,
                evaluation.DirectionVarianceScale,
                evaluation.ContactPointVarianceScale,
                evaluation.PreviewAccuracyBonus);
            footballPhysics?.SetCenterOfMassOffset(evaluation.CenterOfMassOffset);
        }

        private FootballBuildEvaluation BuildEvaluation()
        {
            return FootballBuildEvaluator.Evaluate(state.playerBuild, upgradeCatalog);
        }

        private void OnFlickResolved(FlickResolutionType resolution)
        {
            if (!IsRunActive || !encounterStarted || encounterCompletionHandled || CurrentEncounter == null)
            {
                return;
            }

            completedFlicksThisEncounter++;
            state.statistics.RecordFlick(resolution, footballPhysics);

            if (CurrentEncounter.encounterType == EncounterType.BossMatch)
            {
                if (resolution == FlickResolutionType.Touchdown &&
                    footballPhysics != null &&
                    Mathf.Abs(footballPhysics.LastFlickTotalYawDegrees) >= 360f)
                {
                    matchController?.AwardCurrentPlayerBonusTouchdown("Full spin touchdown bonus");
                }

                if (completedFlicksThisEncounter % 3 == 0)
                {
                    ApplyBossDeskShake();
                }
            }

            if (CurrentEncounter.encounterType == EncounterType.PrecisionDrill)
            {
                ResolvePrecisionAttempt(resolution);
            }

            PublishState();
        }

        private void OnFieldGoalResolved(bool successful)
        {
            if (!IsRunActive)
            {
                return;
            }

            state.statistics.RecordFieldGoal(successful);
            PublishState();
        }

        private void OnMatchStateRendered(PaperFootballMatch match)
        {
            if (!IsRunActive || !encounterStarted || encounterCompletionHandled || CurrentEncounter == null || match == null)
            {
                return;
            }

            if (CurrentEncounter.encounterType == EncounterType.PrecisionDrill)
            {
                return;
            }

            if (match.Phase == MatchPhase.MatchComplete)
            {
                bool succeeded = match.Winner == PaperFootballPlayer.PlayerOne;
                CompleteEncounter(succeeded, succeeded ? "Encounter won" : "Encounter lost");
            }
        }

        private void ResolvePrecisionAttempt(FlickResolutionType resolution)
        {
            precisionAttemptsUsed++;
            bool fell = resolution == FlickResolutionType.FellFromTable;
            bool inTarget = !fell &&
                            precisionTargetZone != null &&
                            footballPhysics != null &&
                            precisionTargetZone.Contains(footballPhysics.transform.position);

            if (inTarget)
            {
                state.statistics.successfulPrecisionAttempts++;
                CompleteEncounter(true, "Precision target hit");
                return;
            }

            if (precisionAttemptsUsed >= CurrentEncounter.precisionAttemptLimit)
            {
                CompleteEncounter(false, "Precision attempts exhausted");
                return;
            }

            matchController?.ResetMatchAndBall();
            runUi?.ShowActiveEncounter(state, CurrentEncounter, BuildEvaluation(), pendingRewards);
        }

        private void CompleteEncounter(bool succeeded, string resultText)
        {
            if (encounterCompletionHandled || CurrentEncounter == null)
            {
                return;
            }

            encounterCompletionHandled = true;
            encounterStarted = false;
            opponentTurnController?.SetAiEnabled(false);
            matchController?.SetInputSuppressed(true);
            obstacleLayoutController?.Clear();
            temporaryPlacementController?.ClearTemporaryObjects();
            precisionTargetZone?.Hide();

            state.results.Add(new RunEncounterResult
            {
                encounterId = CurrentEncounter.encounterId,
                succeeded = succeeded,
                resultText = resultText
            });

            if (!succeeded)
            {
                CompleteRun(false);
                return;
            }

            state.statistics.encounterWins++;
            if (CurrentEncounter.isBoss || state.currentEncounterIndex >= state.encounters.Count - 1)
            {
                CompleteRun(true);
                return;
            }

            if (CurrentEncounter.rewardEligible)
            {
                ShowRewardChoices();
            }
            else
            {
                AdvanceToNextEncounter();
            }
        }

        private void ShowRewardChoices()
        {
            int rewardIndex = state.results.Count(result => result.succeeded);
            int seed = StableSeedUtility.DeriveSeed(
                state.runSeed,
                RunRandomStream.RewardGeneration,
                state.currentEncounterIndex,
                stableIdentifier: rewardIndex.ToString());
            UpgradeRarity minimumRarity = CurrentEncounter != null && CurrentEncounter.guaranteedUncommonReward
                ? UpgradeRarity.Uncommon
                : UpgradeRarity.Common;
            pendingRewards = upgradeCatalog != null
                ? upgradeCatalog.GetRewardChoices(state.playerBuild, new DeterministicRunRandom(seed), 3, minimumRarity)
                : new List<FootballUpgradeDefinition>();

            if (pendingRewards.Count == 0)
            {
                AdvanceToNextEncounter();
                return;
            }

            runUi?.ShowRewardSelection(state, CurrentEncounter, pendingRewards, upgradeCatalog);
            PublishState();
        }

        private void AdvanceToNextEncounter()
        {
            state.currentEncounterIndex++;
            if (state.currentEncounterIndex >= state.encounters.Count)
            {
                CompleteRun(true);
                return;
            }

            LoadCurrentEncounter(showIntro: true);
        }

        private void CompleteRun(bool won)
        {
            state.status = won ? RunStatus.Won : RunStatus.Lost;
            shotVarianceController?.SetVarianceEnabled(false);
            opponentTurnController?.SetAiEnabled(false);
            matchController?.SetInputSuppressed(true);
            obstacleLayoutController?.Clear();
            temporaryPlacementController?.ClearTemporaryObjects();
            precisionTargetZone?.Hide();
            runUi?.ShowRunSummary(state, upgradeCatalog);
            PublishState();
        }

        private void ApplyBossDeskShake()
        {
            if (footballPhysics == null)
            {
                return;
            }

            int seed = StableSeedUtility.DeriveSeed(
                state.runSeed,
                RunRandomStream.EncounterGeneration,
                state.currentEncounterIndex,
                stableIdentifier: $"desk_shake_{completedFlicksThisEncounter}");
            IRunRandom random = new DeterministicRunRandom(seed);
            Vector3 impulse = new(random.Range(-0.22f, 0.22f), 0f, random.Range(-0.22f, 0.22f));
            footballPhysics.ApplyExternalImpulse(impulse);
        }

        private OpponentProfile ResolveOpponent(GeneratedEncounter encounter)
        {
            return encounter != null && opponentCatalog != null ? opponentCatalog.GetById(encounter.opponentId) : null;
        }

        private TableSurfaceDefinition ResolveSurface(GeneratedEncounter encounter)
        {
            return encounter != null && surfaceCatalog != null ? surfaceCatalog.GetById(encounter.surfaceId) : null;
        }

        private ObstacleLayoutDefinition ResolveLayout(GeneratedEncounter encounter)
        {
            return encounter != null && obstacleCatalog != null ? obstacleCatalog.GetById(encounter.obstacleLayoutId) : null;
        }

        private void PublishState()
        {
            runUi?.RefreshStatus(state, CurrentEncounter, BuildEvaluation(), pendingRewards, upgradeCatalog);
            RunStateChanged?.Invoke(state);
        }

        private static int ParseSeed(string seedText, int fallback)
        {
            return int.TryParse(seedText, out int parsed) ? parsed : fallback;
        }
    }
}
