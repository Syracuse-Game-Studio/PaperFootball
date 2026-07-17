using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.FieldGoals
{
    public static class TrajectoryPredictionService
    {
        public static int Predict(
            Vector3 launchPosition,
            Vector3 launchImpulse,
            float bodyMass,
            PaperFootballRuleSet rules,
            Vector3[] points)
        {
            PaperFootballRuleSet runtimeRules = rules != null ? rules.Clone() : new PaperFootballRuleSet();
            runtimeRules.Sanitize();

            if (points == null || points.Length == 0)
            {
                return 0;
            }

            int pointCount = Mathf.Min(points.Length, runtimeRules.trajectoryPointCount);
            float mass = Mathf.Max(0.0001f, bodyMass);
            Vector3 initialVelocity = launchImpulse / mass;
            float maxTime = runtimeRules.maximumTrajectoryPreviewTime;

            for (int i = 0; i < pointCount; i++)
            {
                float time = Mathf.Min(i * runtimeRules.trajectoryTimeStep, maxTime);
                points[i] = launchPosition + initialVelocity * time + 0.5f * UnityEngine.Physics.gravity * time * time;

                if (time >= maxTime)
                {
                    return i + 1;
                }
            }

            return pointCount;
        }
    }
}
