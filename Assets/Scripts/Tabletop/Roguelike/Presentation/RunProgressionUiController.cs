using System.Collections.Generic;
using System.Linq;
using System.Text;
using PaperFootball.Tabletop.Roguelike.Encounters;
using PaperFootball.Tabletop.Roguelike.Modifiers;
using PaperFootball.Tabletop.Roguelike.Opponents;
using PaperFootball.Tabletop.Roguelike.Run;
using UnityEngine;
using UnityEngine.UI;

namespace PaperFootball.Tabletop.Roguelike.Presentation
{
    public static class PrototypeLaunchOptions
    {
        private static bool startRunRequested;

        public static void RequestRoguelikeRun()
        {
            startRunRequested = true;
        }

        public static void RequestLocalMatch()
        {
            startRunRequested = false;
        }

        public static bool ConsumeStartRunRequested()
        {
            bool requested = startRunRequested;
            startRunRequested = false;
            return requested;
        }
    }

    public class RunProgressionUiController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject runStartPanel;
        [SerializeField] private GameObject encounterIntroPanel;
        [SerializeField] private GameObject activeRunPanel;
        [SerializeField] private GameObject rewardPanel;
        [SerializeField] private GameObject summaryPanel;

        [Header("Run Start")]
        [SerializeField] private InputField seedInput;
        [SerializeField] private Button randomSeedButton;
        [SerializeField] private Button startRunButton;
        [SerializeField] private Button returnToLocalButton;

        [Header("Encounter Intro")]
        [SerializeField] private Text introText;
        [SerializeField] private Button continueButton;

        [Header("Active Run")]
        [SerializeField] private Text activeRunText;

        [Header("Rewards")]
        [SerializeField] private Text rewardHeaderText;
        [SerializeField] private Button[] rewardButtons;
        [SerializeField] private Text[] rewardTexts;

        [Header("Summary")]
        [SerializeField] private Text summaryText;
        [SerializeField] private Button restartSameSeedButton;
        [SerializeField] private Button newSeedButton;
        [SerializeField] private Button summaryReturnLocalButton;

        private readonly StringBuilder builder = new();
        private RunController controller;

        public void Configure(
            GameObject startPanel,
            InputField seed,
            Button randomButton,
            Button startButton,
            Button localButton,
            GameObject introPanel,
            Text intro,
            Button continueEncounterButton,
            GameObject activePanel,
            Text active,
            GameObject rewardsPanel,
            Text rewardHeader,
            Button[] rewardChoiceButtons,
            Text[] rewardChoiceTexts,
            GameObject endSummaryPanel,
            Text summary,
            Button restartButton,
            Button newRunButton,
            Button summaryLocalButton)
        {
            runStartPanel = startPanel;
            seedInput = seed;
            randomSeedButton = randomButton;
            startRunButton = startButton;
            returnToLocalButton = localButton;
            encounterIntroPanel = introPanel;
            introText = intro;
            continueButton = continueEncounterButton;
            activeRunPanel = activePanel;
            activeRunText = active;
            rewardPanel = rewardsPanel;
            rewardHeaderText = rewardHeader;
            rewardButtons = rewardChoiceButtons;
            rewardTexts = rewardChoiceTexts;
            summaryPanel = endSummaryPanel;
            summaryText = summary;
            restartSameSeedButton = restartButton;
            newSeedButton = newRunButton;
            summaryReturnLocalButton = summaryLocalButton;
            WireButtons();
        }

        public void Bind(RunController runController)
        {
            controller = runController;
            WireButtons();
        }

        public void ShowRunStart(string seed)
        {
            HideRunPanels();
            SetActive(runStartPanel, true);
            if (seedInput != null)
            {
                seedInput.text = seed;
            }
        }

        public void HideRunPanels()
        {
            SetActive(runStartPanel, false);
            SetActive(encounterIntroPanel, false);
            SetActive(activeRunPanel, false);
            SetActive(rewardPanel, false);
            SetActive(summaryPanel, false);
        }

        public void ShowLocalMatchNotice()
        {
            HideRunPanels();
        }

        public void ShowEncounterIntro(
            RunState state,
            GeneratedEncounter encounter,
            OpponentProfile opponent,
            TableSurfaceDefinition surface,
            ObstacleLayoutDefinition layout)
        {
            HideRunPanels();
            SetActive(encounterIntroPanel, true);
            builder.Clear();
            builder.AppendLine(encounter.displayTitle);
            builder.Append("Type: ").Append(encounter.encounterType).AppendLine();
            builder.Append("Opponent: ").Append(opponent != null ? opponent.DisplayName : "None").AppendLine();
            builder.Append("Surface: ").Append(surface != null ? surface.DisplayName : encounter.surfaceId).AppendLine();
            builder.Append("Obstacles: ").Append(layout != null ? layout.DisplayName : encounter.obstacleLayoutId).AppendLine();
            builder.Append("Objective: ").Append(encounter.encounterType == EncounterType.PrecisionDrill ? "Land in target zone" : "Win the match").AppendLine();
            builder.Append("Special: ").Append(encounter.specialRule).AppendLine();
            builder.Append("Reward: ").Append(encounter.rewardEligible ? (encounter.guaranteedUncommonReward ? "Uncommon or better" : "Upgrade choice") : "Run completion");
            SetText(introText, builder.ToString());
        }

        public void ShowActiveEncounter(
            RunState state,
            GeneratedEncounter encounter,
            FootballBuildEvaluation evaluation,
            IReadOnlyList<FootballUpgradeDefinition> pendingRewards)
        {
            HideRunPanels();
            SetActive(activeRunPanel, true);
            RefreshStatus(state, encounter, evaluation, pendingRewards, controller != null ? controller.UpgradeCatalog : null);
        }

        public void ShowRewardSelection(
            RunState state,
            GeneratedEncounter encounter,
            IReadOnlyList<FootballUpgradeDefinition> choices,
            UpgradeCatalog catalog)
        {
            HideRunPanels();
            SetActive(rewardPanel, true);
            SetText(rewardHeaderText, $"Choose one upgrade\nSeed: {state.runSeed} | After: {encounter.displayTitle}");

            for (int i = 0; i < rewardButtons.Length; i++)
            {
                int index = i;
                bool hasChoice = choices != null && i < choices.Count && choices[i] != null;
                if (rewardButtons[i] != null)
                {
                    rewardButtons[i].gameObject.SetActive(hasChoice);
                    rewardButtons[i].interactable = hasChoice;
                    rewardButtons[i].onClick.RemoveAllListeners();
                    if (hasChoice)
                    {
                        rewardButtons[i].onClick.AddListener(() => controller?.ChooseReward(choices[index]));
                    }
                }

                if (rewardTexts != null && i < rewardTexts.Length && rewardTexts[i] != null)
                {
                    rewardTexts[i].text = hasChoice ? BuildRewardText(choices[i], state, catalog) : string.Empty;
                }
            }
        }

        public void ShowRunSummary(RunState state, UpgradeCatalog catalog)
        {
            HideRunPanels();
            SetActive(summaryPanel, true);
            builder.Clear();
            builder.AppendLine(state.status == RunStatus.Won ? "Run Victory" : "Run Defeat");
            builder.Append("Seed: ").Append(state.runSeed).AppendLine();
            builder.Append("Encounters completed: ").Append(state.results.Count(result => result.succeeded)).Append('/').Append(state.encounters.Count).AppendLine();
            builder.Append("Upgrades: ").Append(state.playerBuild.ToSummary(catalog)).AppendLine();
            builder.Append("Flicks: ").Append(state.statistics.flicks).AppendLine();
            builder.Append("Touchdowns: ").Append(state.statistics.touchdowns).AppendLine();
            builder.Append("Field goals: ").Append(state.statistics.fieldGoals).AppendLine();
            builder.Append("Falls: ").Append(state.statistics.falls).AppendLine();
            builder.Append("Highest spin: ").Append(state.statistics.highestSpin.ToString("0.00")).AppendLine();
            builder.Append("Longest flick: ").Append(state.statistics.longestFlick.ToString("0.00")).AppendLine();
            builder.Append("Precision successes: ").Append(state.statistics.successfulPrecisionAttempts).AppendLine();
            builder.Append("Boss result: ").Append(state.encounters.Count > 0 && state.results.Any(result => result.encounterId == state.encounters[state.encounters.Count - 1].encounterId && result.succeeded) ? "Defeated" : "Not defeated");
            SetText(summaryText, builder.ToString());
        }

        public void RefreshStatus(
            RunState state,
            GeneratedEncounter encounter,
            FootballBuildEvaluation evaluation,
            IReadOnlyList<FootballUpgradeDefinition> pendingRewards,
            UpgradeCatalog catalog)
        {
            if (activeRunText == null || state == null || encounter == null || activeRunPanel == null || !activeRunPanel.activeSelf)
            {
                return;
            }

            builder.Clear();
            builder.Append("Run seed: ").Append(state.runSeed).AppendLine();
            builder.Append("Encounter: ").Append(state.currentEncounterIndex + 1).Append('/').Append(state.encounters.Count).Append(" - ").Append(encounter.displayTitle).AppendLine();
            builder.Append("Status: ").Append(state.status).AppendLine();
            builder.Append("Objective: ").Append(encounter.encounterType == EncounterType.PrecisionDrill ? "Stop inside target" : "Win the short match").AppendLine();
            builder.Append("Special: ").Append(encounter.specialRule).AppendLine();
            builder.Append("Upgrades: ").Append(state.playerBuild.ToSummary(catalog)).AppendLine();
            builder.Append("Variance scales F/D/C: ")
                .Append(evaluation.ForceVarianceScale.ToString("0.00")).Append(" / ")
                .Append(evaluation.DirectionVarianceScale.ToString("0.00")).Append(" / ")
                .Append(evaluation.ContactPointVarianceScale.ToString("0.00")).AppendLine();
            builder.Append("Flicks: ").Append(state.statistics.flicks)
                .Append(" | TD: ").Append(state.statistics.touchdowns)
                .Append(" | FG: ").Append(state.statistics.fieldGoals)
                .Append(" | Falls: ").Append(state.statistics.falls);
            activeRunText.text = builder.ToString();
        }

        private void Awake()
        {
            WireButtons();
        }

        private void WireButtons()
        {
            if (randomSeedButton != null)
            {
                randomSeedButton.onClick.RemoveAllListeners();
                randomSeedButton.onClick.AddListener(() =>
                {
                    if (seedInput != null)
                    {
                        seedInput.text = (System.Environment.TickCount & 0x7fffffff).ToString();
                    }
                });
            }

            if (startRunButton != null)
            {
                startRunButton.onClick.RemoveAllListeners();
                startRunButton.onClick.AddListener(() => controller?.StartRunWithSeedText(seedInput != null ? seedInput.text : string.Empty));
            }

            if (returnToLocalButton != null)
            {
                returnToLocalButton.onClick.RemoveAllListeners();
                returnToLocalButton.onClick.AddListener(() => controller?.ReturnToLocalMatch());
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() => controller?.BeginCurrentEncounter());
            }

            if (restartSameSeedButton != null)
            {
                restartSameSeedButton.onClick.RemoveAllListeners();
                restartSameSeedButton.onClick.AddListener(() =>
                {
                    if (controller != null)
                    {
                        controller.StartRun(controller.State.runSeed);
                    }
                });
            }

            if (newSeedButton != null)
            {
                newSeedButton.onClick.RemoveAllListeners();
                newSeedButton.onClick.AddListener(() => controller?.StartRunWithRandomSeed());
            }

            if (summaryReturnLocalButton != null)
            {
                summaryReturnLocalButton.onClick.RemoveAllListeners();
                summaryReturnLocalButton.onClick.AddListener(() => controller?.ReturnToLocalMatch());
            }
        }

        private static string BuildRewardText(FootballUpgradeDefinition upgrade, RunState state, UpgradeCatalog catalog)
        {
            int stacks = state.playerBuild.GetStackCount(upgrade.StableId);
            return $"{upgrade.DisplayName}\n{upgrade.Rarity}\n{upgrade.Description}\nStack: {stacks}/{upgrade.MaximumStackCount}\n{upgrade.BuildEffectSummary()}";
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
