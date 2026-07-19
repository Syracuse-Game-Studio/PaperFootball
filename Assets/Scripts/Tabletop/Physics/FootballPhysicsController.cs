using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    public class FootballPhysicsController : MonoBehaviour
    {
        [SerializeField] private float linearDamping = 1.15f;
        [SerializeField] private float angularDamping = 0.8f;
        [SerializeField] private float contactYawTorqueMultiplier = 2.5f;
        [SerializeField] private float maximumAngularVelocity = 24f;
        [SerializeField] private bool constrainFlipping = true;

        private Rigidbody body;
        private Collider footballCollider;
        private PendingImpulse pendingImpulse;
        private bool hasPendingImpulse;
        private float debugLineExpiresAt;

        public Rigidbody Rigidbody => body;
        public bool IsMoving => body != null &&
                                (body.linearVelocity.sqrMagnitude > 0.0001f ||
                                 body.angularVelocity.sqrMagnitude > 0.0001f);
        public bool HasLastContactPoint { get; private set; }
        public Vector3 LastAppliedContactPointWorld { get; private set; }
        public Vector3 LastCenterOfMassWorld { get; private set; }
        public Vector3 LastAppliedImpulse { get; private set; }
        public Vector3 LastAppliedYawTorqueImpulse { get; private set; }
        public float LastContactLeverArmDistance { get; private set; }
        public float AngularDamping => body != null ? body.angularDamping : angularDamping;
        public float ContactYawTorqueMultiplier => contactYawTorqueMultiplier;

        public void Configure(PaperFootballRuleSet rules)
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (footballCollider == null)
            {
                footballCollider = GetComponent<Collider>();
            }

            if (rules != null)
            {
                angularDamping = rules.footballAngularDamping;
                contactYawTorqueMultiplier = rules.contactYawTorqueMultiplier;
                maximumAngularVelocity = rules.maximumFootballAngularVelocity;
            }

            ApplyBodySettings();
        }

        public void Flick(FlickCommand command, float upwardImpulse = 0f)
        {
            if (body == null || !command.IsValid)
            {
                return;
            }

            Vector3 impulse = command.Direction * command.Force;
            if (upwardImpulse > 0f)
            {
                impulse += Vector3.up * upwardImpulse;
            }

            QueueImpulse(impulse, command.HasContactPoint, command.ContactPointWorld);
        }

        public void KickFieldGoal(FieldGoalKickResult result)
        {
            if (body == null || !result.IsValid)
            {
                return;
            }

            QueueImpulse(result.TotalImpulse, result.HasContactPoint, result.ContactPointWorld);
        }

        public void Stop()
        {
            if (body == null)
            {
                return;
            }

            hasPendingImpulse = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Sleep();
            ClearLastImpulseDebug();
        }

        public void PlaceAt(Vector3 position, Quaternion rotation)
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            if (footballCollider == null)
            {
                footballCollider = GetComponent<Collider>();
            }

            body.position = position;
            body.rotation = rotation;
            hasPendingImpulse = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Sleep();
            transform.SetPositionAndRotation(position, rotation);
            ClearLastImpulseDebug();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            footballCollider = GetComponent<Collider>();
            ApplyBodySettings();
        }

        private void FixedUpdate()
        {
            if (!hasPendingImpulse || body == null)
            {
                DrawLastImpulseDebugLine();
                return;
            }

            PendingImpulse impulse = pendingImpulse;
            hasPendingImpulse = false;
            ApplyImpulse(impulse);
            DrawLastImpulseDebugLine();
        }

        private void ApplyBodySettings()
        {
            body.mass = 0.16f;
            body.useGravity = true;
            body.linearDamping = linearDamping;
            body.angularDamping = angularDamping;
            body.maxAngularVelocity = maximumAngularVelocity;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = constrainFlipping
                ? RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ
                : RigidbodyConstraints.None;
        }

        private void QueueImpulse(Vector3 impulse, bool applyAtContactPoint, Vector3 contactPointWorld)
        {
            pendingImpulse = new PendingImpulse(impulse, applyAtContactPoint, contactPointWorld);
            hasPendingImpulse = true;
            body.WakeUp();
        }

        private void ApplyImpulse(PendingImpulse impulse)
        {
            body.WakeUp();

            if (!impulse.ApplyAtContactPoint)
            {
                body.AddForce(impulse.Force, ForceMode.Impulse);
                RecordImpulseDebug(impulse.Force, body.worldCenterOfMass, body.worldCenterOfMass, false);
                return;
            }

            Vector3 applicationPoint = ResolveApplicationPoint(impulse.ContactPointWorld);
            body.AddForceAtPosition(impulse.Force, applicationPoint, ForceMode.Impulse);
            Vector3 yawTorqueImpulse = CalculateYawTorqueImpulse(applicationPoint, impulse.Force);
            if (yawTorqueImpulse.sqrMagnitude > 0.000001f)
            {
                body.AddTorque(yawTorqueImpulse, ForceMode.Impulse);
            }

            RecordImpulseDebug(impulse.Force, yawTorqueImpulse, applicationPoint, body.worldCenterOfMass, true);
        }

        private Vector3 ResolveApplicationPoint(Vector3 contactPointWorld)
        {
            Vector3 applicationPoint = contactPointWorld;
            if (footballCollider != null)
            {
                applicationPoint = footballCollider.ClosestPoint(applicationPoint);
            }

            if (constrainFlipping)
            {
                applicationPoint.y = body.worldCenterOfMass.y;
            }

            return applicationPoint;
        }

        private Vector3 CalculateYawTorqueImpulse(Vector3 applicationPoint, Vector3 impulse)
        {
            if (contactYawTorqueMultiplier <= 0f)
            {
                return Vector3.zero;
            }

            Vector3 leverArm = Vector3.ProjectOnPlane(applicationPoint - body.worldCenterOfMass, Vector3.up);
            Vector3 horizontalImpulse = Vector3.ProjectOnPlane(impulse, Vector3.up);
            Vector3 torqueImpulse = Vector3.Cross(leverArm, horizontalImpulse);
            return Vector3.Project(torqueImpulse, Vector3.up) * contactYawTorqueMultiplier;
        }

        private void RecordImpulseDebug(Vector3 impulse, Vector3 applicationPoint, Vector3 centerOfMass, bool hasContactPoint)
        {
            RecordImpulseDebug(impulse, Vector3.zero, applicationPoint, centerOfMass, hasContactPoint);
        }

        private void RecordImpulseDebug(Vector3 impulse, Vector3 yawTorqueImpulse, Vector3 applicationPoint, Vector3 centerOfMass, bool hasContactPoint)
        {
            LastAppliedImpulse = impulse;
            LastAppliedYawTorqueImpulse = yawTorqueImpulse;
            LastAppliedContactPointWorld = applicationPoint;
            LastCenterOfMassWorld = centerOfMass;
            Vector3 horizontalLeverArm = Vector3.ProjectOnPlane(applicationPoint - centerOfMass, Vector3.up);
            LastContactLeverArmDistance = hasContactPoint ? horizontalLeverArm.magnitude : 0f;
            HasLastContactPoint = hasContactPoint;
            debugLineExpiresAt = Time.time + 2f;
        }

        private void ClearLastImpulseDebug()
        {
            LastAppliedImpulse = Vector3.zero;
            LastAppliedYawTorqueImpulse = Vector3.zero;
            LastAppliedContactPointWorld = Vector3.zero;
            LastCenterOfMassWorld = Vector3.zero;
            LastContactLeverArmDistance = 0f;
            HasLastContactPoint = false;
            debugLineExpiresAt = 0f;
        }

        private void DrawLastImpulseDebugLine()
        {
            if (!HasLastContactPoint || Time.time > debugLineExpiresAt)
            {
                return;
            }

            Debug.DrawLine(LastCenterOfMassWorld, LastAppliedContactPointWorld, Color.yellow, 0f, false);
        }

        private readonly struct PendingImpulse
        {
            public PendingImpulse(Vector3 force, bool applyAtContactPoint, Vector3 contactPointWorld)
            {
                Force = force;
                ApplyAtContactPoint = applyAtContactPoint;
                ContactPointWorld = contactPointWorld;
            }

            public Vector3 Force { get; }
            public bool ApplyAtContactPoint { get; }
            public Vector3 ContactPointWorld { get; }
        }
    }
}
