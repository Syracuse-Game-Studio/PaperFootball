using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Roguelike.Random;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Shots
{
    public static class AirFlickShotCalculator
    {
        public static AirFlickShotResult Calculate(
            FlickCommand command,
            PaperFootballRuleSet rules,
            AirFlickShotSettings settings,
            Collider footballCollider,
            IRunRandom landingRandom,
            ShotExecutionContext context,
            bool generateLandingVariance = true)
        {
            PaperFootballRuleSet runtimeRules = rules != null ? rules.Clone() : new PaperFootballRuleSet();
            runtimeRules.Sanitize();
            AirFlickShotSettings runtimeSettings = settings != null ? settings : AirFlickShotSettings.CreateRuntimeDefault();
            runtimeSettings.Sanitize();

            if (!command.IsValid || command.DragDistance < runtimeRules.minimumDragDistance)
            {
                return AirFlickShotResult.Invalid(context);
            }

            Vector3 horizontalDirection = command.Direction;
            horizontalDirection.y = 0f;
            if (horizontalDirection.sqrMagnitude <= 0.000001f)
            {
                return AirFlickShotResult.Invalid(context);
            }

            horizontalDirection.Normalize();
            float dragDistance = Mathf.Min(command.DragDistance, runtimeRules.maximumDragDistance);
            float normalizedPower = Mathf.InverseLerp(runtimeRules.minimumDragDistance, runtimeRules.maximumDragDistance, dragDistance);
            float contactLift = CalculateContactLift(command, footballCollider, horizontalDirection, runtimeSettings);
            float launchBlend = Mathf.Clamp01(normalizedPower * 0.65f + contactLift * 0.35f);
            float launchAngle = Mathf.Lerp(runtimeSettings.MinimumLaunchAngle, runtimeSettings.MaximumLaunchAngle, launchBlend);

            float sidePenalty = CalculateSideContactPenalty(command, footballCollider, horizontalDirection, runtimeSettings);
            float forwardImpulse = command.Force * runtimeSettings.ForwardImpulseMultiplier * Mathf.Lerp(1f, 0.82f, sidePenalty);
            forwardImpulse = Mathf.Clamp(forwardImpulse, runtimeRules.minimumFlickForce * 0.4f, runtimeRules.maximumFlickForce);

            float upwardFromAngle = Mathf.Tan(launchAngle * Mathf.Deg2Rad) * forwardImpulse;
            float upwardImpulse = Mathf.Clamp(upwardFromAngle, runtimeSettings.MinimumUpwardImpulse, runtimeSettings.MaximumUpwardImpulse);
            float predictedHeight = PredictMaximumHeight(upwardImpulse, footballCollider);
            LandingVarianceSample sample = generateLandingVariance && landingRandom != null
                ? LandingVarianceSample.Generate(runtimeSettings, landingRandom)
                : LandingVarianceSample.None;

            Vector3 contactPoint = command.HasContactPoint
                ? command.ContactPointWorld
                : (footballCollider != null ? footballCollider.bounds.center : command.DragStartWorld);

            return new AirFlickShotResult(
                true,
                horizontalDirection,
                forwardImpulse,
                upwardImpulse,
                contactPoint,
                command.HasContactPoint,
                launchAngle,
                normalizedPower,
                predictedHeight,
                sample,
                context);
        }

        private static float CalculateContactLift(
            FlickCommand command,
            Collider footballCollider,
            Vector3 horizontalDirection,
            AirFlickShotSettings settings)
        {
            if (!command.HasContactPoint || footballCollider == null)
            {
                return 0.5f;
            }

            Vector3 center = footballCollider.bounds.center;
            Vector3 planarOffset = Vector3.ProjectOnPlane(command.ContactPointWorld - center, Vector3.up);
            float forwardExtent = Mathf.Max(0.05f, Vector3.ProjectOnPlane(footballCollider.bounds.extents, Vector3.up).magnitude);
            float rearBias = Mathf.Clamp(Vector3.Dot(-horizontalDirection, planarOffset) / forwardExtent, -1f, 1f);
            float sidePenalty = CalculateSideContactPenalty(command, footballCollider, horizontalDirection, settings);
            float lift = 0.5f + rearBias * settings.ContactLiftInfluence - sidePenalty * settings.SideContactStabilityPenalty;
            return Mathf.Clamp01(lift);
        }

        private static float CalculateSideContactPenalty(
            FlickCommand command,
            Collider footballCollider,
            Vector3 horizontalDirection,
            AirFlickShotSettings settings)
        {
            if (!command.HasContactPoint || footballCollider == null)
            {
                return 0f;
            }

            Vector3 side = Vector3.Cross(Vector3.up, horizontalDirection).normalized;
            Vector3 offset = Vector3.ProjectOnPlane(command.ContactPointWorld - footballCollider.bounds.center, Vector3.up);
            float extent = Mathf.Max(0.05f, Vector3.Dot(footballCollider.bounds.extents, Abs(side)));
            return Mathf.Clamp01(Mathf.Abs(Vector3.Dot(offset, side)) / extent);
        }

        private static float PredictMaximumHeight(float upwardImpulse, Collider footballCollider)
        {
            float mass = 0.16f;
            Rigidbody body = footballCollider != null ? footballCollider.attachedRigidbody : null;
            if (body != null)
            {
                mass = Mathf.Max(0.0001f, body.mass);
            }

            float upwardVelocity = upwardImpulse / mass;
            float gravity = Mathf.Abs(UnityEngine.Physics.gravity.y);
            return gravity <= 0.0001f ? 0f : upwardVelocity * upwardVelocity / (2f * gravity);
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }
    }
}
