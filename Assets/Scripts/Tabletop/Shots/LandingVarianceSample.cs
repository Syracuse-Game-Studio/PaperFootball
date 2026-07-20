using PaperFootball.Tabletop.Roguelike.Random;
using UnityEngine;

namespace PaperFootball.Tabletop.Shots
{
    public readonly struct LandingVarianceSample
    {
        public LandingVarianceSample(
            float bounceMultiplier,
            float tangentialImpulse,
            float yawImpulse,
            float frictionMultiplier,
            float directionOffsetDegrees,
            int seed)
        {
            BounceMultiplier = Mathf.Max(0f, bounceMultiplier);
            TangentialImpulse = tangentialImpulse;
            YawImpulse = yawImpulse;
            FrictionMultiplier = Mathf.Max(0f, frictionMultiplier);
            DirectionOffsetDegrees = directionOffsetDegrees;
            Seed = seed;
        }

        public float BounceMultiplier { get; }
        public float TangentialImpulse { get; }
        public float YawImpulse { get; }
        public float FrictionMultiplier { get; }
        public float DirectionOffsetDegrees { get; }
        public int Seed { get; }
        public bool HasVariance => Mathf.Abs(TangentialImpulse) > 0.0001f ||
                                   Mathf.Abs(YawImpulse) > 0.0001f ||
                                   Mathf.Abs(DirectionOffsetDegrees) > 0.0001f ||
                                   !Mathf.Approximately(BounceMultiplier, 1f) ||
                                   !Mathf.Approximately(FrictionMultiplier, 1f);

        public static LandingVarianceSample None => new(1f, 0f, 0f, 1f, 0f, 0);

        public static LandingVarianceSample Generate(AirFlickShotSettings settings, IRunRandom random)
        {
            AirFlickShotSettings runtimeSettings = settings != null ? settings : AirFlickShotSettings.CreateRuntimeDefault();
            IRunRandom runtimeRandom = random ?? new DeterministicRunRandom(0);
            float varianceScale = runtimeSettings.LandingVarianceMultiplier;
            float tangentRange = runtimeSettings.LandingTangentImpulseRange * varianceScale;
            float yawRange = runtimeSettings.LandingYawImpulseRange * varianceScale;
            float directionRange = runtimeSettings.LandingDirectionOffsetDegrees * varianceScale;

            return new LandingVarianceSample(
                runtimeRandom.Range(runtimeSettings.LandingBounceMultiplierMin, runtimeSettings.LandingBounceMultiplierMax),
                runtimeRandom.Range(-tangentRange, tangentRange),
                runtimeRandom.Range(-yawRange, yawRange),
                runtimeRandom.Range(runtimeSettings.LandingFrictionMultiplierMin, runtimeSettings.LandingFrictionMultiplierMax),
                runtimeRandom.Range(-directionRange, directionRange),
                runtimeRandom.Seed);
        }
    }
}
