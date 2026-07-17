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
        public float maximumFlickForce = 18f;
        public float minimumFlickForce = 1.5f;
        public float minimumDragDistance = 0.05f;
        public float maximumDragDistance = 2.5f;
        public float footballStoppingThreshold = 0.08f;
        public float angularStoppingThreshold = 0.25f;
        public float requiredStillTime = 0.35f;
        public float fallHeight = -1.2f;
        public float fieldGoalTimeLimit = 6f;
        public float turnTimeLimit = 0f;
        public float kickoffOffsetFromCenter = 3.8f;

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
            minimumDragDistance = Math.Max(0f, minimumDragDistance);
            maximumDragDistance = Math.Max(minimumDragDistance + 0.001f, maximumDragDistance);
            footballStoppingThreshold = Math.Max(0.001f, footballStoppingThreshold);
            angularStoppingThreshold = Math.Max(0.001f, angularStoppingThreshold);
            requiredStillTime = Math.Max(0.01f, requiredStillTime);
            kickoffOffsetFromCenter = Math.Max(0f, kickoffOffsetFromCenter);
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
