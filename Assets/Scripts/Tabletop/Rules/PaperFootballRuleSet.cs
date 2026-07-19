using System;

namespace PaperFootball.Tabletop.Rules
{
    [Serializable]
    public class PaperFootballRuleSet
    {
        public int touchdownPoints = 6;
        public int successfulKickPoints = 3;
        public int targetScore = 21;
        public int maximumPossessions = 0;
        public bool touchdownRequiresOverhang = true;
        public float requiredOverhangPercent = 0f;
        public float minimumSupportedPercent = 0.25f;
        public bool fallingFromTableChangesPossession = true;
        public float maximumFlickForce = 4f;
        public float minimumFlickForce = 0.35f;
        public float flickForceResponseExponent = 1.6f;
        public float minimumDragDistance = 0.05f;
        public float maximumDragDistance = 2.5f;
        public float footballStoppingThreshold = 0.08f;
        public float angularStoppingThreshold = 0.25f;
        public float footballAngularDamping = 0.8f;
        public float contactYawTorqueMultiplier = 2.5f;
        public float maximumFootballAngularVelocity = 24f;
        public float requiredStillTime = 0.35f;
        public float fallHeight = -1.2f;
        public float fieldGoalTimeLimit = 6f;
        public float turnTimeLimit = 0f;
        public float kickoffOffsetFromCenter = 3.8f;
        public float minimumFieldGoalForce = 2.5f;
        public float maximumFieldGoalForce = 9f;
        public float minimumFieldGoalLaunchAngle = 28f;
        public float maximumFieldGoalLaunchAngle = 58f;
        public float minimumFieldGoalUpwardForce = 2f;
        public float maximumFieldGoalUpwardForce = 7f;
        public int trajectoryPointCount = 28;
        public float trajectoryTimeStep = 0.075f;
        public float maximumTrajectoryPreviewTime = 2.1f;
        public int trajectoryCollisionMask = 0;

        public void Sanitize()
        {
            touchdownPoints = Math.Max(1, touchdownPoints);
            successfulKickPoints = Math.Max(1, successfulKickPoints);
            targetScore = Math.Max(1, targetScore);
            maximumPossessions = Math.Max(0, maximumPossessions);
            requiredOverhangPercent = Clamp01(requiredOverhangPercent);
            minimumSupportedPercent = Clamp01(minimumSupportedPercent);
            maximumFlickForce = Math.Max(0.01f, maximumFlickForce);
            minimumFlickForce = Math.Max(0f, Math.Min(minimumFlickForce, maximumFlickForce));
            flickForceResponseExponent = Math.Max(0.1f, flickForceResponseExponent);
            minimumDragDistance = Math.Max(0f, minimumDragDistance);
            maximumDragDistance = Math.Max(minimumDragDistance + 0.001f, maximumDragDistance);
            footballStoppingThreshold = Math.Max(0.001f, footballStoppingThreshold);
            angularStoppingThreshold = Math.Max(0.001f, angularStoppingThreshold);
            footballAngularDamping = Math.Max(0.05f, footballAngularDamping);
            contactYawTorqueMultiplier = Math.Max(0f, contactYawTorqueMultiplier);
            maximumFootballAngularVelocity = Math.Max(0.1f, maximumFootballAngularVelocity);
            requiredStillTime = Math.Max(0.01f, requiredStillTime);
            fieldGoalTimeLimit = Math.Max(0.25f, fieldGoalTimeLimit);
            kickoffOffsetFromCenter = Math.Max(0f, kickoffOffsetFromCenter);
            maximumFieldGoalForce = Math.Max(0.01f, maximumFieldGoalForce);
            minimumFieldGoalForce = Math.Max(0f, Math.Min(minimumFieldGoalForce, maximumFieldGoalForce));
            minimumFieldGoalLaunchAngle = Math.Max(0f, Math.Min(minimumFieldGoalLaunchAngle, 89f));
            maximumFieldGoalLaunchAngle = Math.Max(minimumFieldGoalLaunchAngle, Math.Min(maximumFieldGoalLaunchAngle, 89f));
            maximumFieldGoalUpwardForce = Math.Max(0f, maximumFieldGoalUpwardForce);
            minimumFieldGoalUpwardForce = Math.Max(0f, Math.Min(minimumFieldGoalUpwardForce, maximumFieldGoalUpwardForce));
            trajectoryPointCount = Math.Max(2, trajectoryPointCount);
            trajectoryTimeStep = Math.Max(0.01f, trajectoryTimeStep);
            maximumTrajectoryPreviewTime = Math.Max(trajectoryTimeStep, maximumTrajectoryPreviewTime);
        }

        public PaperFootballRuleSet Clone()
        {
            return (PaperFootballRuleSet)MemberwiseClone();
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
