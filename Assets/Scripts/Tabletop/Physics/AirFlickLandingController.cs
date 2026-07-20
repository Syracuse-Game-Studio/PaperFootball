using PaperFootball.Tabletop.Shots;
using UnityEngine;

namespace PaperFootball.Tabletop.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    public class AirFlickLandingController : MonoBehaviour
    {
        [SerializeField] private FootballPhysicsController footballPhysics;
        [SerializeField] private Collider tableCollider;
        [SerializeField] private AirFlickShotSettings settings;

        private Rigidbody body;
        private AirFlickShotResult activeShot;
        private bool hasActiveShot;

        public AirFlickState CurrentState { get; private set; } = AirFlickState.Inactive;
        public bool HasActiveShot => hasActiveShot;
        public bool HasBecomeAirborne { get; private set; }
        public bool LandingVarianceConsumed { get; private set; }
        public Vector3 LastLandingTangentialImpulse { get; private set; }
        public Vector3 LastLandingBounceImpulse { get; private set; }
        public Vector3 LastLandingYawImpulse { get; private set; }
        public int LastLandingVarianceSeed { get; private set; }

        public void Configure(FootballPhysicsController physicsController, Collider activeTableCollider, AirFlickShotSettings airFlickSettings)
        {
            footballPhysics = physicsController;
            tableCollider = activeTableCollider;
            settings = airFlickSettings;
            body = GetComponent<Rigidbody>();
            ResetState();
        }

        public void BeginTracking(AirFlickShotResult result)
        {
            if (!result.IsValid)
            {
                ResetState();
                return;
            }

            body ??= GetComponent<Rigidbody>();
            activeShot = result;
            hasActiveShot = true;
            HasBecomeAirborne = false;
            LandingVarianceConsumed = false;
            LastLandingTangentialImpulse = Vector3.zero;
            LastLandingBounceImpulse = Vector3.zero;
            LastLandingYawImpulse = Vector3.zero;
            LastLandingVarianceSeed = result.LandingVariance.Seed;
            CurrentState = AirFlickState.Launched;
        }

        public void MarkResolved()
        {
            if (hasActiveShot)
            {
                CurrentState = AirFlickState.Resolved;
            }
        }

        public void ResetState()
        {
            hasActiveShot = false;
            activeShot = default;
            HasBecomeAirborne = false;
            LandingVarianceConsumed = false;
            CurrentState = AirFlickState.Inactive;
            LastLandingTangentialImpulse = Vector3.zero;
            LastLandingBounceImpulse = Vector3.zero;
            LastLandingYawImpulse = Vector3.zero;
            LastLandingVarianceSeed = 0;
        }

        public static Vector3 CalculateLandingImpulse(
            Vector3 incomingVelocity,
            Vector3 collisionNormal,
            LandingVarianceSample sample,
            AirFlickShotSettings settings,
            float bodyMass,
            Vector3 fallbackDirection,
            out Vector3 tangentialImpulse,
            out Vector3 bounceImpulse)
        {
            AirFlickShotSettings runtimeSettings = settings != null ? settings : AirFlickShotSettings.CreateRuntimeDefault();
            Vector3 normal = collisionNormal.sqrMagnitude > 0.000001f ? collisionNormal.normalized : Vector3.up;
            Vector3 incomingPlanar = Vector3.ProjectOnPlane(incomingVelocity, normal);
            Vector3 travelDirection = incomingPlanar.sqrMagnitude > 0.000001f
                ? incomingPlanar.normalized
                : Vector3.ProjectOnPlane(fallbackDirection, normal).normalized;
            if (travelDirection.sqrMagnitude <= 0.000001f)
            {
                travelDirection = Vector3.forward;
            }

            travelDirection = Quaternion.AngleAxis(sample.DirectionOffsetDegrees, normal) * travelDirection;
            Vector3 sideways = Vector3.Cross(normal, travelDirection).normalized;
            tangentialImpulse = sideways * sample.TangentialImpulse;

            float incomingIntoSurface = Mathf.Max(0f, -Vector3.Dot(incomingVelocity, normal));
            float bounceExtra = Mathf.Max(0f, sample.BounceMultiplier - 1f);
            bounceImpulse = normal * incomingIntoSurface * Mathf.Max(0.0001f, bodyMass) * bounceExtra;

            Vector3 combined = tangentialImpulse + bounceImpulse;
            float cap = runtimeSettings.MaximumLandingCorrectionImpulse;
            if (cap <= 0f)
            {
                tangentialImpulse = Vector3.zero;
                bounceImpulse = Vector3.zero;
                return Vector3.zero;
            }

            if (combined.magnitude > cap)
            {
                combined = combined.normalized * cap;
                tangentialImpulse = Vector3.Project(combined, sideways);
                bounceImpulse = combined - tangentialImpulse;
            }

            return combined;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (footballPhysics == null)
            {
                footballPhysics = GetComponent<FootballPhysicsController>();
            }
        }

        private void FixedUpdate()
        {
            if (!hasActiveShot || body == null || CurrentState != AirFlickState.Launched)
            {
                return;
            }

            if (HasActuallyBecomeAirborne())
            {
                HasBecomeAirborne = true;
                CurrentState = AirFlickState.Airborne;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryConsumeLanding(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            TryConsumeLanding(collision);
        }

        private bool HasActuallyBecomeAirborne()
        {
            AirFlickShotSettings runtimeSettings = settings != null ? settings : AirFlickShotSettings.CreateRuntimeDefault();
            float tableTopY = tableCollider != null ? tableCollider.bounds.max.y : 0f;
            bool highEnough = body.worldCenterOfMass.y >= tableTopY + runtimeSettings.MinimumAirborneHeight;
            bool movingUp = body.linearVelocity.y >= runtimeSettings.MinimumPositiveVerticalVelocity &&
                            body.worldCenterOfMass.y >= tableTopY + runtimeSettings.MinimumAirborneHeight * 0.5f;
            return highEnough || movingUp;
        }

        private void TryConsumeLanding(Collision collision)
        {
            if (!hasActiveShot ||
                LandingVarianceConsumed ||
                CurrentState != AirFlickState.Airborne ||
                collision == null ||
                !IsValidTableLanding(collision))
            {
                return;
            }

            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            Vector3 normal = contact.normal.sqrMagnitude > 0.000001f ? contact.normal : Vector3.up;
            Vector3 point = contact.point.sqrMagnitude > 0.000001f ? contact.point : body.worldCenterOfMass;

            Vector3 impulse = CalculateLandingImpulse(
                body.linearVelocity,
                normal,
                activeShot.LandingVariance,
                settings,
                body.mass,
                activeShot.Direction,
                out Vector3 tangential,
                out Vector3 bounce);

            if (impulse.sqrMagnitude > 0.000001f)
            {
                footballPhysics?.ApplyExternalImpulseAtPoint(impulse, point);
            }

            Vector3 yawImpulse = Vector3.up * activeShot.LandingVariance.YawImpulse;
            if (yawImpulse.sqrMagnitude > 0.000001f)
            {
                footballPhysics?.ApplyExternalTorqueImpulse(yawImpulse);
            }

            LastLandingTangentialImpulse = tangential;
            LastLandingBounceImpulse = bounce;
            LastLandingYawImpulse = yawImpulse;
            LandingVarianceConsumed = true;
            CurrentState = AirFlickState.Landed;
        }

        private bool IsValidTableLanding(Collision collision)
        {
            if (collision.collider == null || collision.collider.isTrigger)
            {
                return false;
            }

            if (tableCollider != null)
            {
                return collision.collider == tableCollider;
            }

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y >= 0.45f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
