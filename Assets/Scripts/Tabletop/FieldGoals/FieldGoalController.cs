using System;
using PaperFootball.Tabletop.Rules;
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

        public bool AttemptActive => attemptActive;
        public bool ScoredThisAttempt => scoredThisAttempt;
        public PaperFootballPlayer ActivePlayer => activePlayer;

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
            activePlayer = player;
            attemptActive = true;
            scoredThisAttempt = false;
        }

        public void EndAttempt()
        {
            attemptActive = false;
        }

        public Transform GetKickSpot(PaperFootballPlayer player)
        {
            return player == PaperFootballPlayer.PlayerOne ? playerOneKickSpot : playerTwoKickSpot;
        }

        public void ReportGoalMouthEntered(PaperFootballPlayer scoringPlayer, Collider candidateFootball)
        {
            if (!attemptActive || scoredThisAttempt || scoringPlayer != activePlayer)
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
