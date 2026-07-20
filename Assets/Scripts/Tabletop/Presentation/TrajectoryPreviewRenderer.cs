using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Rules;
using PaperFootball.Tabletop.Shots;
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

        public void ShowAirFlick(AirFlickShotResult result, AirFlickShotSettings settings)
        {
            EnsureLineRenderer();
            EnsurePointBuffer(settings);

            if (!result.IsValid || footballBody == null)
            {
                Hide();
                return;
            }

            AirFlickShotSettings runtimeSettings = settings != null ? settings : AirFlickShotSettings.CreateRuntimeDefault();
            LastPreviewImpulse = result.TotalImpulse;
            Vector3 launchPosition = footballBody.position + Vector3.up * launchYOffset;
            int count = PredictAirFlick(
                launchPosition,
                result.TotalImpulse,
                footballBody.mass,
                runtimeSettings,
                points);

            count = ApplyCollisionTruncation(count);
            lineRenderer.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                lineRenderer.SetPosition(i, points[i]);
            }

            lineRenderer.startWidth = 0.028f;
            lineRenderer.endWidth = 0.006f;
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

        private void EnsurePointBuffer(AirFlickShotSettings settings)
        {
            AirFlickShotSettings runtimeSettings = settings != null ? settings : AirFlickShotSettings.CreateRuntimeDefault();
            if (points == null || points.Length != runtimeSettings.TrajectoryPreviewPointCount)
            {
                points = new Vector3[runtimeSettings.TrajectoryPreviewPointCount];
            }
        }

        private static int PredictAirFlick(
            Vector3 launchPosition,
            Vector3 launchImpulse,
            float bodyMass,
            AirFlickShotSettings settings,
            Vector3[] targetPoints)
        {
            if (targetPoints == null || targetPoints.Length == 0)
            {
                return 0;
            }

            AirFlickShotSettings runtimeSettings = settings != null ? settings : AirFlickShotSettings.CreateRuntimeDefault();
            int pointCount = Mathf.Min(targetPoints.Length, runtimeSettings.TrajectoryPreviewPointCount);
            float mass = Mathf.Max(0.0001f, bodyMass);
            Vector3 initialVelocity = launchImpulse / mass;
            float maxTime = runtimeSettings.TrajectoryPreviewDuration;

            for (int i = 0; i < pointCount; i++)
            {
                float time = Mathf.Min(i * runtimeSettings.TrajectoryPreviewTimeStep, maxTime);
                targetPoints[i] = launchPosition + initialVelocity * time + 0.5f * UnityEngine.Physics.gravity * time * time;
                if (time >= maxTime)
                {
                    return i + 1;
                }
            }

            return pointCount;
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
