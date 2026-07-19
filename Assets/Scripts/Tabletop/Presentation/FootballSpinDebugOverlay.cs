using System.Text;
using PaperFootball.Tabletop.Physics;
using UnityEngine;
using UnityEngine.UI;

namespace PaperFootball.Tabletop.Presentation
{
    public class FootballSpinDebugOverlay : MonoBehaviour
    {
        [SerializeField] private FootballPhysicsController footballPhysics;
        [SerializeField] private Text debugText;
        [SerializeField] private bool visible = true;

        private readonly StringBuilder builder = new();

        public void Configure(FootballPhysicsController physicsController, Text text, bool show = true)
        {
            footballPhysics = physicsController;
            debugText = text;
            visible = show;
            ApplyTextVisibility();
        }

        public void SetVisible(bool show)
        {
            visible = show;
            ApplyTextVisibility();
        }

        private void Update()
        {
            if (debugText == null)
            {
                return;
            }

            if (!visible || footballPhysics == null || footballPhysics.Rigidbody == null)
            {
                debugText.enabled = false;
                return;
            }

            Rigidbody body = footballPhysics.Rigidbody;
            Vector3 linearVelocity = body.linearVelocity;
            Vector3 angularVelocity = body.angularVelocity;
            Vector3 contactPoint = footballPhysics.HasLastContactPoint
                ? footballPhysics.LastAppliedContactPointWorld
                : Vector3.zero;
            Vector3 centerOfMass = body.worldCenterOfMass;

            builder.Clear();
            builder.AppendLine("Spin Debug");
            builder.Append("Linear velocity: ").Append(Format(linearVelocity)).AppendLine();
            builder.Append("Angular velocity: ").Append(Format(angularVelocity)).AppendLine();
            builder.Append("Yaw angular velocity: ").Append(angularVelocity.y.ToString("0.000")).AppendLine(" rad/s");
            builder.Append("Current Y rotation: ").Append(body.rotation.eulerAngles.y.ToString("0.0")).AppendLine(" deg");
            builder.Append("Applied contact point: ").Append(footballPhysics.HasLastContactPoint ? Format(contactPoint) : "none").AppendLine();
            builder.Append("Center of mass: ").Append(Format(centerOfMass)).AppendLine();
            builder.Append("Contact distance: ").Append(footballPhysics.LastContactLeverArmDistance.ToString("0.000")).AppendLine(" m");
            builder.Append("Yaw torque impulse: ").Append(footballPhysics.LastAppliedYawTorqueImpulse.y.ToString("0.000")).AppendLine();
            builder.Append("Yaw torque multiplier: ").Append(footballPhysics.ContactYawTorqueMultiplier.ToString("0.00")).AppendLine();
            builder.Append("Angular damping: ").Append(footballPhysics.AngularDamping.ToString("0.00"));

            debugText.text = builder.ToString();
            debugText.enabled = true;
        }

        private void ApplyTextVisibility()
        {
            if (debugText != null)
            {
                debugText.enabled = visible;
            }
        }

        private static string Format(Vector3 value)
        {
            return $"({value.x:0.000}, {value.y:0.000}, {value.z:0.000})";
        }
    }
}
