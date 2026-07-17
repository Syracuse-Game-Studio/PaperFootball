using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Presentation
{
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPreviewRenderer : MonoBehaviour
    {
        [SerializeField] private Rigidbody footballBody;
        [SerializeField] private float launchYOffset = 0.04f;

        private readonly PaperFootballRuleSet fallbackRules = new();
        private PaperFootballRuleSet rules;
        private LineRenderer lineRenderer;
        private Vector3[] points;

        public bool IsVisible => lineRenderer != null && lineRenderer.enabled;
        public Vector3 LastPreviewImpulse { get; private set; }

        public void Configure(Rigidbody body, PaperFootballRuleSet ruleSet)
        {
            footballBody = body;
            rules = ruleSet != null ? ruleSet.Clone() : fallbackRules.Clone();
            rules.Sanitize();
            EnsureLineRenderer();
            EnsurePointBuffer();
            Hide();
        }

        public void Show(FieldGoalKickResult result)
        {
            EnsureLineRenderer();
            EnsurePointBuffer();

            if (!result.IsValid || footballBody == null)
            {
                Hide();
                return;
            }

            LastPreviewImpulse = result.TotalImpulse;
            Vector3 launchPosition = footballBody.position + Vector3.up * launchYOffset;
            int count = TrajectoryPredictionService.Predict(
                launchPosition,
                result.TotalImpulse,
                footballBody.mass,
                rules,
                points);

            count = ApplyCollisionTruncation(count);
            lineRenderer.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                lineRenderer.SetPosition(i, points[i]);
            }

            lineRenderer.startWidth = 0.04f;
            lineRenderer.endWidth = 0.012f;
            lineRenderer.enabled = count > 1;
        }

        public void Hide()
        {
            EnsureLineRenderer();
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
        }

        private void Awake()
        {
            EnsureLineRenderer();
            rules ??= fallbackRules.Clone();
            rules.Sanitize();
            EnsurePointBuffer();
            Hide();
        }

        private void EnsureLineRenderer()
        {
            if (lineRenderer != null)
            {
                return;
            }

            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;
        }

        private void EnsurePointBuffer()
        {
            rules ??= fallbackRules.Clone();
            rules.Sanitize();

            if (points == null || points.Length != rules.trajectoryPointCount)
            {
                points = new Vector3[rules.trajectoryPointCount];
            }
        }

        private int ApplyCollisionTruncation(int count)
        {
            if (rules == null || rules.trajectoryCollisionMask == 0 || count <= 1)
            {
                return count;
            }

            int mask = rules.trajectoryCollisionMask;
            for (int i = 1; i < count; i++)
            {
                Vector3 previous = points[i - 1];
                Vector3 current = points[i];
                Vector3 delta = current - previous;
                float distance = delta.magnitude;
                if (distance <= 0.0001f)
                {
                    continue;
                }

                if (UnityEngine.Physics.Raycast(previous, delta / distance, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
                {
                    points[i] = hit.point;
                    return i + 1;
                }
            }

            return count;
        }
    }
}
