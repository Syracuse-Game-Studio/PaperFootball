using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    public class FootballPhysicsController : MonoBehaviour
    {
        [SerializeField] private float spinImpulse = 0.18f;
        [SerializeField] private float linearDamping = 1.15f;
        [SerializeField] private float angularDamping = 1.8f;
        [SerializeField] private bool constrainFlipping = true;

        private Rigidbody body;

        public Rigidbody Rigidbody => body;
        public bool IsMoving => body != null && body.linearVelocity.sqrMagnitude > 0.0001f;

        public void Configure(PaperFootballRuleSet rules)
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            ApplyBodySettings();
        }

        public void Flick(FlickCommand command, float upwardImpulse = 0f)
        {
            if (body == null || !command.IsValid)
            {
                return;
            }

            body.WakeUp();
            Vector3 impulse = command.Direction * command.Force;
            if (upwardImpulse > 0f)
            {
                impulse += Vector3.up * upwardImpulse;
            }

            body.AddForce(impulse, ForceMode.Impulse);

            float spin = Vector3.Dot(command.Direction, transform.right) * command.Force * spinImpulse;
            body.AddTorque(Vector3.up * spin, ForceMode.Impulse);
        }

        public void Stop()
        {
            if (body == null)
            {
                return;
            }

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Sleep();
        }

        public void PlaceAt(Vector3 position, Quaternion rotation)
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }

            body.position = position;
            body.rotation = rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Sleep();
            transform.SetPositionAndRotation(position, rotation);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ApplyBodySettings();
        }

        private void ApplyBodySettings()
        {
            body.mass = 0.16f;
            body.useGravity = true;
            body.linearDamping = linearDamping;
            body.angularDamping = angularDamping;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = constrainFlipping
                ? RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ
                : RigidbodyConstraints.None;
        }
    }
}
