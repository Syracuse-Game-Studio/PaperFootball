using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.FieldGoals
{
    public static class FieldGoalKickCalculator
    {
        public static FieldGoalKickResult Calculate(FlickCommand command, PaperFootballRuleSet rules)
        {
            PaperFootballRuleSet runtimeRules = rules != null ? rules.Clone() : new PaperFootballRuleSet();
            runtimeRules.Sanitize();

            if (!command.IsValid || command.DragDistance < runtimeRules.minimumDragDistance)
            {
                return FieldGoalKickResult.Invalid();
            }

            Vector3 horizontalDirection = command.Direction;
            horizontalDirection.y = 0f;
            if (horizontalDirection.sqrMagnitude <= 0.000001f)
            {
                return FieldGoalKickResult.Invalid();
            }

            horizontalDirection.Normalize();
            float dragDistance = Mathf.Min(command.DragDistance, runtimeRules.maximumDragDistance);
            float normalizedPower = Mathf.InverseLerp(runtimeRules.minimumDragDistance, runtimeRules.maximumDragDistance, dragDistance);
            float forwardImpulse = Mathf.Lerp(runtimeRules.minimumFieldGoalForce, runtimeRules.maximumFieldGoalForce, normalizedPower);
            float launchAngle = Mathf.Lerp(runtimeRules.minimumFieldGoalLaunchAngle, runtimeRules.maximumFieldGoalLaunchAngle, normalizedPower);
            float upwardFromAngle = Mathf.Tan(launchAngle * Mathf.Deg2Rad) * forwardImpulse;
            float upwardImpulse = Mathf.Clamp(upwardFromAngle, runtimeRules.minimumFieldGoalUpwardForce, runtimeRules.maximumFieldGoalUpwardForce);
            upwardImpulse = Mathf.Max(0f, upwardImpulse);
            Vector3 totalImpulse = horizontalDirection * forwardImpulse + Vector3.up * upwardImpulse;

            return new FieldGoalKickResult(
                true,
                horizontalDirection,
                forwardImpulse,
                upwardImpulse,
                launchAngle,
                normalizedPower,
                dragDistance,
                totalImpulse,
                command.ContactPointWorld,
                command.HasContactPoint);
        }
    }
}
