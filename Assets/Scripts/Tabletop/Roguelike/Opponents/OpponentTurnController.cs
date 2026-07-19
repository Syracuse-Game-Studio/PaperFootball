using System.Collections;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Match;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Presentation;
using PaperFootball.Tabletop.Roguelike.Random;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Roguelike.Opponents
{
    public class OpponentTurnController : MonoBehaviour
    {
        [SerializeField] private MatchController matchController;
        [SerializeField] private FootballPhysicsController footballPhysics;
        [SerializeField] private Collider footballCollider;
        [SerializeField] private ContactPointIndicator contactPointIndicator;
        [SerializeField] private FlickAimIndicator aimIndicator;
        [SerializeField] private bool aiEnabled;
        [SerializeField] private bool fastAi;

        private OpponentProfile activeProfile;
        private Bounds tableBounds;
        private int runSeed;
        private int encounterIndex;
        private Coroutine turnRoutine;

        public OpponentProfile ActiveProfile => activeProfile;
        public bool IsThinking => turnRoutine != null;

        public void Configure(
            MatchController match,
            FootballPhysicsController physicsController,
            Collider targetFootball,
            ContactPointIndicator contactIndicator,
            FlickAimIndicator flickIndicator)
        {
            matchController = match;
            footballPhysics = physicsController;
            footballCollider = targetFootball;
            contactPointIndicator = contactIndicator;
            aimIndicator = flickIndicator;
        }

        public void SetRunContext(int seed, int index, Bounds currentTableBounds)
        {
            runSeed = seed;
            encounterIndex = Mathf.Max(0, index);
            tableBounds = currentTableBounds;
        }

        public void SetOpponent(OpponentProfile profile)
        {
            activeProfile = profile;
        }

        public void SetAiEnabled(bool enabled)
        {
            aiEnabled = enabled;
            if (!enabled && turnRoutine != null)
            {
                StopCoroutine(turnRoutine);
                turnRoutine = null;
                matchController?.SetInputSuppressed(false);
            }
        }

        public void SetFastAi(bool enabled)
        {
            fastAi = enabled;
        }

        private void Update()
        {
            if (!aiEnabled || activeProfile == null || matchController == null || matchController.Match == null)
            {
                return;
            }

            PaperFootballMatch match = matchController.Match;
            if (match.Phase != MatchPhase.WaitingForFlick || match.CurrentPlayer != PaperFootballPlayer.PlayerTwo)
            {
                if (turnRoutine == null)
                {
                    matchController.SetInputSuppressed(false);
                }

                return;
            }

            if (turnRoutine == null)
            {
                turnRoutine = StartCoroutine(ExecuteTurn(match.PossessionNumber));
            }
        }

        private IEnumerator ExecuteTurn(int possessionNumber)
        {
            matchController?.SetInputSuppressed(true);
            float delay = fastAi ? 0.05f : activeProfile.DecisionDelay;
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            PaperFootballRuleSet rules = matchController != null ? matchController.CurrentRules : new PaperFootballRuleSet();
            int seed = StableSeedUtility.DeriveSeed(
                runSeed,
                RunRandomStream.OpponentDecisions,
                encounterIndex,
                PaperFootballPlayer.PlayerTwo,
                possessionNumber,
                0,
                activeProfile.StableId);

            OpponentDecisionContext context = new(
                activeProfile,
                footballCollider,
                tableBounds,
                rules,
                PaperFootballPlayer.PlayerTwo,
                possessionNumber,
                encounterIndex);
            OpponentDecision decision = OpponentDecisionService.Decide(context, new DeterministicRunRandom(seed));

            if (decision.IsValid)
            {
                ShowDecision(decision.Command);
                if (!fastAi)
                {
                    yield return new WaitForSeconds(0.35f);
                }

                matchController.TrySubmitFlick(decision.Command, "ai");
            }

            aimIndicator?.Hide();
            turnRoutine = null;
        }

        private void ShowDecision(FlickCommand command)
        {
            if (footballCollider != null && contactPointIndicator != null && command.HasContactPoint)
            {
                SelectedContactPoint contact = new(
                    footballCollider,
                    footballCollider.transform.InverseTransformPoint(command.ContactPointWorld),
                    Vector3.up);
                contactPointIndicator.ShowFlickPreview(contact, command.Direction);
            }

            aimIndicator?.Show(command);
        }
    }
}
