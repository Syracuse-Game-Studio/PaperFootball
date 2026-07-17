using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Input
{
    public static class FlickForceCalculator
    {
        public static FlickCommand Calculate(Vector3 dragStartWorld, Vector3 currentWorld, float dragDuration, PaperFootballRuleSet rules)
        {
            PaperFootballRuleSet runtimeRules = rules ?? new PaperFootballRuleSet();
            runtimeRules.Sanitize();

            Vector3 dragVector = dragStartWorld - currentWorld;
            dragVector.y = 0f;
            float unclampedDistance = dragVector.magnitude;

            if (unclampedDistance < runtimeRules.minimumDragDistance)
            {
                return FlickCommand.Invalid(dragStartWorld, currentWorld, dragDuration);
            }

            float dragDistance = Mathf.Min(unclampedDistance, runtimeRules.maximumDragDistance);
            float strength01 = Mathf.InverseLerp(runtimeRules.minimumDragDistance, runtimeRules.maximumDragDistance, dragDistance);
            float force = Mathf.Lerp(runtimeRules.minimumFlickForce, runtimeRules.maximumFlickForce, strength01);
            Vector3 direction = dragVector.normalized;

            return new FlickCommand(
                true,
                dragStartWorld,
                currentWorld,
                currentWorld,
                direction,
                force,
                dragDistance,
                Mathf.Max(0f, dragDuration),
                strength01);
        }
    }
}
