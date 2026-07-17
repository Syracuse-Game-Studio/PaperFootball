using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.FieldGoals
{
    [RequireComponent(typeof(BoxCollider))]
    public class GoalPostTrigger : MonoBehaviour
    {
        [SerializeField] private PaperFootballPlayer scoringPlayer = PaperFootballPlayer.PlayerOne;
        [SerializeField] private float uprightHalfWidth = 1.1f;
        [SerializeField] private float crossbarWorldY = 0.84f;
        [SerializeField] private Collider footballCollider;

        private FieldGoalController controller;
        private BoxCollider triggerCollider;

        public void Configure(
            FieldGoalController fieldGoalController,
            PaperFootballPlayer player,
            Collider football,
            float halfWidth = 1.1f,
            float crossbarY = 0.84f)
        {
            controller = fieldGoalController;
            scoringPlayer = player;
            footballCollider = football;
            uprightHalfWidth = Mathf.Max(0.05f, halfWidth);
            crossbarWorldY = crossbarY;
            EnsureTrigger();
        }

        private void Awake()
        {
            EnsureTrigger();
        }

        private void OnTriggerEnter(Collider other)
        {
            TryReport(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryReport(other);
        }

        private void TryReport(Collider other)
        {
            if (controller == null)
            {
                return;
            }

            if (footballCollider != null && other != footballCollider)
            {
                return;
            }

            Bounds bounds = other.bounds;
            Vector3 center = bounds.center;
            if (Mathf.Abs(center.x - transform.position.x) > uprightHalfWidth)
            {
                return;
            }

            if (bounds.min.y < crossbarWorldY)
            {
                return;
            }

            controller.ReportGoalMouthEntered(scoringPlayer, other);
        }

        private void EnsureTrigger()
        {
            triggerCollider = GetComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
        }
    }
}
