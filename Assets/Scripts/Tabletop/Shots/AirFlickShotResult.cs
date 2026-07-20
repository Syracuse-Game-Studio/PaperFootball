using UnityEngine;

namespace PaperFootball.Tabletop.Shots
{
    public readonly struct AirFlickShotResult
    {
        public AirFlickShotResult(
            bool isValid,
            Vector3 direction,
            float forwardImpulse,
            float upwardImpulse,
            Vector3 contactPointWorld,
            bool hasContactPoint,
            float launchAngleDegrees,
            float normalizedPower,
            float predictedMaximumHeight,
            LandingVarianceSample landingVariance,
            ShotExecutionContext context)
        {
            IsValid = isValid;
            Direction = NormalizeTabletop(direction);
            ForwardImpulse = Mathf.Max(0f, forwardImpulse);
            UpwardImpulse = Mathf.Max(0f, upwardImpulse);
            ContactPointWorld = contactPointWorld;
            HasContactPoint = hasContactPoint;
            LaunchAngleDegrees = Mathf.Max(0f, launchAngleDegrees);
            NormalizedPower = Mathf.Clamp01(normalizedPower);
            PredictedMaximumHeight = Mathf.Max(0f, predictedMaximumHeight);
            LandingVariance = landingVariance;
            Context = context;
            TotalImpulse = Direction * ForwardImpulse + Vector3.up * UpwardImpulse;
        }

        public bool IsValid { get; }
        public Vector3 Direction { get; }
        public float ForwardImpulse { get; }
        public float UpwardImpulse { get; }
        public Vector3 ContactPointWorld { get; }
        public bool HasContactPoint { get; }
        public float LaunchAngleDegrees { get; }
        public float NormalizedPower { get; }
        public float PredictedMaximumHeight { get; }
        public LandingVarianceSample LandingVariance { get; }
        public ShotExecutionContext Context { get; }
        public Vector3 TotalImpulse { get; }

        public static AirFlickShotResult Invalid(ShotExecutionContext context = default)
        {
            return new AirFlickShotResult(
                false,
                Vector3.forward,
                0f,
                0f,
                Vector3.zero,
                false,
                0f,
                0f,
                0f,
                LandingVarianceSample.None,
                context);
        }

        private static Vector3 NormalizeTabletop(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude > 0.000001f ? value.normalized : Vector3.forward;
        }
    }
}
