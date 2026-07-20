using System;
using PaperFootball.Tabletop.Rules;
using PaperFootball.Tabletop.Shots;
using UnityEngine;

namespace PaperFootball.Tabletop.FieldGoals
{
    public class FieldGoalController : MonoBehaviour
    {
        [SerializeField] private Transform playerOneKickSpot;
        [SerializeField] private Transform playerTwoKickSpot;
        [SerializeField] private GoalPostTrigger playerOneGoalTrigger;
        [SerializeField] private GoalPostTrigger playerTwoGoalTrigger;
        [SerializeField] private Collider footballCollider;

        private bool attemptActive;
        private bool scoredThisAttempt;
        private PaperFootballPlayer activePlayer;
        private ShotExecutionContext activeShotContext = ShotExecutionContext.None;

        public bool AttemptActive => attemptActive;
        public bool ScoredThisAttempt => scoredThisAttempt;
        public PaperFootballPlayer ActivePlayer => activePlayer;
        public ShotExecutionContext ActiveShotContext => activeShotContext;

        public event Action FieldGoalScored;

        private void Awake()
        {
            WireTriggers();
        }

        public void Configure(
            Transform p1KickSpot,
            Transform p2KickSpot,
            GoalPostTrigger p1GoalTrigger,
            GoalPostTrigger p2GoalTrigger,
            Collider football)
        {
            playerOneKickSpot = p1KickSpot;
            playerTwoKickSpot = p2KickSpot;
            playerOneGoalTrigger = p1GoalTrigger;
            playerTwoGoalTrigger = p2GoalTrigger;
            footballCollider = football;

            WireTriggers();
        }

        private void WireTriggers()
        {
            if (playerOneGoalTrigger != null)
            {
                playerOneGoalTrigger.Configure(this, PaperFootballPlayer.PlayerOne, footballCollider);
            }

            if (playerTwoGoalTrigger != null)
            {
                playerTwoGoalTrigger.Configure(this, PaperFootballPlayer.PlayerTwo, footballCollider);
            }
        }

        public void BeginAttempt(PaperFootballPlayer player)
        {
            BeginAttempt(player, ShotExecutionContext.FieldGoal(player, 0, 0, 0, 0));
        }

        public void BeginAttempt(PaperFootballPlayer player, ShotExecutionContext shotContext)
        {
            activePlayer = player;
            attemptActive = true;
            scoredThisAttempt = false;
            activeShotContext = shotContext.ShotType == FootballShotType.FieldGoalKick
                ? shotContext
                : ShotExecutionContext.FieldGoal(player, shotContext.RunSeed, shotContext.EncounterIndex, shotContext.PossessionNumber, shotContext.ShotSequenceNumber);
        }

        public void EndAttempt()
        {
            attemptActive = false;
            activeShotContext = ShotExecutionContext.None;
        }

        public void SetNonScoringShotContext(ShotExecutionContext shotContext)
        {
            attemptActive = false;
            scoredThisAttempt = false;
            activeShotContext = shotContext;
        }

        public Transform GetKickSpot(PaperFootballPlayer player)
        {
            return player == PaperFootballPlayer.PlayerOne ? playerOneKickSpot : playerTwoKickSpot;
        }

        public void ReportGoalMouthEntered(PaperFootballPlayer scoringPlayer, Collider candidateFootball)
        {
            if (!attemptActive ||
                scoredThisAttempt ||
                scoringPlayer != activePlayer ||
                activeShotContext.ShotType != FootballShotType.FieldGoalKick ||
                !activeShotContext.CanScoreFieldGoal)
            {
                return;
            }

            if (footballCollider != null && candidateFootball != footballCollider)
            {
                return;
            }

            scoredThisAttempt = true;
            FieldGoalScored?.Invoke();
        }
    }
}
