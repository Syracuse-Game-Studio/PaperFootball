using UnityEngine;

namespace PaperFootball.Tabletop.Shots
{
    [CreateAssetMenu(menuName = "Paper Football/Air Flick Shot Settings", fileName = "AirFlickShotSettings")]
    public class AirFlickShotSettings : ScriptableObject
    {
        [Header("Launch")]
        [SerializeField] private float minimumLaunchAngle = 18f;
        [SerializeField] private float maximumLaunchAngle = 42f;
        [SerializeField] private float minimumUpwardImpulse = 0.35f;
        [SerializeField] private float maximumUpwardImpulse = 0.95f;
        [SerializeField] private float forwardImpulseMultiplier = 0.72f;
        [SerializeField] private float contactLiftInfluence = 0.18f;
        [SerializeField] private float sideContactStabilityPenalty = 0.35f;

        [Header("Variance")]
        [SerializeField] private float forceVarianceMultiplier = 1.25f;
        [SerializeField] private float directionVarianceMultiplier = 1.5f;
        [SerializeField] private float contactVarianceMultiplier = 1.5f;

        [Header("Airborne")]
        [SerializeField] private float minimumAirborneHeight = 0.08f;
        [SerializeField] private float minimumPositiveVerticalVelocity = 0.2f;

        [Header("Landing")]
        [SerializeField] private float landingBounceMultiplierMin = 0.9f;
        [SerializeField] private float landingBounceMultiplierMax = 1.18f;
        [SerializeField] private float landingTangentImpulseRange = 0.18f;
        [SerializeField] private float landingYawImpulseRange = 0.75f;
        [SerializeField] private float landingFrictionMultiplierMin = 0.85f;
        [SerializeField] private float landingFrictionMultiplierMax = 1.15f;
        [SerializeField] private float landingDirectionOffsetDegrees = 8f;
        [SerializeField] private float maximumLandingCorrectionImpulse = 0.3f;
        [SerializeField] private float landingVarianceMultiplier = 1f;

        [Header("Preview")]
        [SerializeField] private float trajectoryPreviewDuration = 1.45f;
        [SerializeField] private int trajectoryPreviewPointCount = 22;
        [SerializeField] private float trajectoryPreviewTimeStep = 0.065f;

        public float MinimumLaunchAngle => Mathf.Clamp(minimumLaunchAngle, 0f, 80f);
        public float MaximumLaunchAngle => Mathf.Clamp(Mathf.Max(minimumLaunchAngle, maximumLaunchAngle), 0f, 80f);
        public float MinimumUpwardImpulse => Mathf.Max(0f, minimumUpwardImpulse);
        public float MaximumUpwardImpulse => Mathf.Max(MinimumUpwardImpulse, maximumUpwardImpulse);
        public float ForwardImpulseMultiplier => Mathf.Clamp(forwardImpulseMultiplier, 0.05f, 2f);
        public float ContactLiftInfluence => Mathf.Clamp(contactLiftInfluence, 0f, 1f);
        public float SideContactStabilityPenalty => Mathf.Clamp01(sideContactStabilityPenalty);
        public float ForceVarianceMultiplier => Mathf.Max(0f, forceVarianceMultiplier);
        public float DirectionVarianceMultiplier => Mathf.Max(0f, directionVarianceMultiplier);
        public float ContactVarianceMultiplier => Mathf.Max(0f, contactVarianceMultiplier);
        public float MinimumAirborneHeight => Mathf.Max(0.005f, minimumAirborneHeight);
        public float MinimumPositiveVerticalVelocity => Mathf.Max(0f, minimumPositiveVerticalVelocity);
        public float LandingBounceMultiplierMin => Mathf.Max(0f, Mathf.Min(landingBounceMultiplierMin, landingBounceMultiplierMax));
        public float LandingBounceMultiplierMax => Mathf.Max(LandingBounceMultiplierMin, landingBounceMultiplierMax);
        public float LandingTangentImpulseRange => Mathf.Max(0f, landingTangentImpulseRange);
        public float LandingYawImpulseRange => Mathf.Max(0f, landingYawImpulseRange);
        public float LandingFrictionMultiplierMin => Mathf.Max(0f, Mathf.Min(landingFrictionMultiplierMin, landingFrictionMultiplierMax));
        public float LandingFrictionMultiplierMax => Mathf.Max(LandingFrictionMultiplierMin, landingFrictionMultiplierMax);
        public float LandingDirectionOffsetDegrees => Mathf.Max(0f, landingDirectionOffsetDegrees);
        public float MaximumLandingCorrectionImpulse => Mathf.Max(0f, maximumLandingCorrectionImpulse);
        public float LandingVarianceMultiplier => Mathf.Max(0f, landingVarianceMultiplier);
        public float TrajectoryPreviewDuration => Mathf.Max(0.1f, trajectoryPreviewDuration);
        public int TrajectoryPreviewPointCount => Mathf.Max(2, trajectoryPreviewPointCount);
        public float TrajectoryPreviewTimeStep => Mathf.Max(0.01f, trajectoryPreviewTimeStep);

        public static AirFlickShotSettings CreateRuntimeDefault()
        {
            AirFlickShotSettings settings = CreateInstance<AirFlickShotSettings>();
            settings.Sanitize();
            return settings;
        }

        public void Sanitize()
        {
            minimumLaunchAngle = MinimumLaunchAngle;
            maximumLaunchAngle = MaximumLaunchAngle;
            minimumUpwardImpulse = MinimumUpwardImpulse;
            maximumUpwardImpulse = MaximumUpwardImpulse;
            forwardImpulseMultiplier = ForwardImpulseMultiplier;
            contactLiftInfluence = ContactLiftInfluence;
            sideContactStabilityPenalty = SideContactStabilityPenalty;
            forceVarianceMultiplier = ForceVarianceMultiplier;
            directionVarianceMultiplier = DirectionVarianceMultiplier;
            contactVarianceMultiplier = ContactVarianceMultiplier;
            minimumAirborneHeight = MinimumAirborneHeight;
            minimumPositiveVerticalVelocity = MinimumPositiveVerticalVelocity;
            landingBounceMultiplierMin = LandingBounceMultiplierMin;
            landingBounceMultiplierMax = LandingBounceMultiplierMax;
            landingTangentImpulseRange = LandingTangentImpulseRange;
            landingYawImpulseRange = LandingYawImpulseRange;
            landingFrictionMultiplierMin = LandingFrictionMultiplierMin;
            landingFrictionMultiplierMax = LandingFrictionMultiplierMax;
            landingDirectionOffsetDegrees = LandingDirectionOffsetDegrees;
            maximumLandingCorrectionImpulse = MaximumLandingCorrectionImpulse;
            landingVarianceMultiplier = LandingVarianceMultiplier;
            trajectoryPreviewDuration = TrajectoryPreviewDuration;
            trajectoryPreviewPointCount = TrajectoryPreviewPointCount;
            trajectoryPreviewTimeStep = TrajectoryPreviewTimeStep;
        }

        public AirFlickShotSettings WithRuntimeMultipliers(
            float forwardMultiplier,
            float upwardMultiplier,
            float launchAngleAdd,
            float landingVarianceScale,
            float bounceScale,
            float landingYawScale,
            float previewAccuracyBonus)
        {
            AirFlickShotSettings copy = Instantiate(this);
            copy.forwardImpulseMultiplier *= Mathf.Max(0.05f, forwardMultiplier);
            copy.minimumUpwardImpulse *= Mathf.Max(0.05f, upwardMultiplier);
            copy.maximumUpwardImpulse *= Mathf.Max(0.05f, upwardMultiplier);
            copy.minimumLaunchAngle += launchAngleAdd;
            copy.maximumLaunchAngle += launchAngleAdd;
            copy.landingVarianceMultiplier *= Mathf.Max(0f, landingVarianceScale);
            copy.landingBounceMultiplierMin *= Mathf.Max(0f, bounceScale);
            copy.landingBounceMultiplierMax *= Mathf.Max(0f, bounceScale);
            copy.landingYawImpulseRange *= Mathf.Max(0f, landingYawScale);
            copy.trajectoryPreviewPointCount += Mathf.RoundToInt(Mathf.Clamp(previewAccuracyBonus, -1f, 1f) * 4f);
            copy.Sanitize();
            return copy;
        }

        private void OnValidate()
        {
            Sanitize();
        }
    }
}
