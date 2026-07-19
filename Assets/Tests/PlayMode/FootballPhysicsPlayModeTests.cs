using System.Collections;
using NUnit.Framework;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Rules;
using UnityEngine;
using UnityEngine.TestTools;

namespace PaperFootball.Tabletop.PlayModeTests
{
    public class FootballPhysicsPlayModeTests
    {
        [UnityTest]
        public IEnumerator RestDetectorReportsRestAfterStillTime()
        {
            GameObject football = new("RestDetectorFootball");
            Rigidbody body = football.AddComponent<Rigidbody>();
            body.useGravity = false;
            FootballRestDetector detector = football.AddComponent<FootballRestDetector>();
            detector.Configure(new PaperFootballRuleSet
            {
                footballStoppingThreshold = 0.05f,
                angularStoppingThreshold = 0.05f,
                requiredStillTime = 0.05f
            });

            yield return new WaitForSeconds(0.15f);

            Assert.IsTrue(detector.IsResting);
            Object.Destroy(football);
        }

        [UnityTest]
        public IEnumerator FlickAppliesForwardVelocity()
        {
            GameObject football = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Rigidbody body = football.AddComponent<Rigidbody>();
            FootballPhysicsController controller = football.AddComponent<FootballPhysicsController>();
            controller.Configure(new PaperFootballRuleSet());
            body.useGravity = false;

            FlickCommand command = new(true, Vector3.zero, new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, -1f), Vector3.forward, 5f, 1f, 0.2f, 0.5f);
            controller.Flick(command);

            yield return new WaitForFixedUpdate();

            Assert.That(body.linearVelocity.z, Is.GreaterThan(0f));
            Object.Destroy(football);
        }

        [UnityTest]
        public IEnumerator CenteredFlickProducesLessYawAngularVelocityThanOffCenterFlick()
        {
            GameObject centeredFootball = CreatePhysicsFootball("CenteredFootball", new Vector3(-2f, 0f, 0f), out Rigidbody centeredBody, out FootballPhysicsController centeredController);
            GameObject offCenterFootball = CreatePhysicsFootball("OffCenterFootball", new Vector3(2f, 0f, 0f), out Rigidbody offCenterBody, out FootballPhysicsController offCenterController);

            centeredController.Flick(CommandWithContact(centeredFootball.transform.position));
            offCenterController.Flick(CommandWithContact(offCenterFootball.transform.position + new Vector3(0.45f, 0f, 0f)));

            yield return new WaitForFixedUpdate();

            float centeredYaw = Mathf.Abs(centeredBody.angularVelocity.y);
            float offCenterYaw = Mathf.Abs(offCenterBody.angularVelocity.y);

            Assert.That(centeredYaw, Is.LessThan(0.01f));
            Assert.That(offCenterYaw, Is.GreaterThan(centeredYaw + 0.01f));
            Assert.That(offCenterController.LastContactLeverArmDistance, Is.GreaterThan(centeredController.LastContactLeverArmDistance + 0.1f));

            Object.Destroy(centeredFootball);
            Object.Destroy(offCenterFootball);
        }

        [UnityTest]
        public IEnumerator OffCenterFlicksCreateOppositeYawSpin()
        {
            GameObject leftHitFootball = CreatePhysicsFootball("LeftHitFootball", new Vector3(-2f, 0f, 0f), out Rigidbody leftBody, out FootballPhysicsController leftController);
            GameObject rightHitFootball = CreatePhysicsFootball("RightHitFootball", new Vector3(2f, 0f, 0f), out Rigidbody rightBody, out FootballPhysicsController rightController);

            FlickCommand leftHit = CommandWithContact(leftHitFootball.transform.position + new Vector3(-0.45f, 0f, 0f));
            FlickCommand rightHit = CommandWithContact(rightHitFootball.transform.position + new Vector3(0.45f, 0f, 0f));

            leftController.Flick(leftHit);
            rightController.Flick(rightHit);

            yield return new WaitForFixedUpdate();

            Assert.That(leftBody.linearVelocity.z, Is.GreaterThan(0f));
            Assert.That(rightBody.linearVelocity.z, Is.GreaterThan(0f));
            Assert.That(Mathf.Abs(leftBody.angularVelocity.y), Is.GreaterThan(0.01f));
            Assert.That(Mathf.Abs(rightBody.angularVelocity.y), Is.GreaterThan(0.01f));
            Assert.That(Mathf.Sign(leftBody.angularVelocity.y), Is.EqualTo(-Mathf.Sign(rightBody.angularVelocity.y)));

            Object.Destroy(leftHitFootball);
            Object.Destroy(rightHitFootball);
        }

        [UnityTest]
        public IEnumerator OffCenterFlickChangesTransformYawAndKeepsRotatingPastFirstPhysicsStep()
        {
            GameObject football = CreatePhysicsFootball("YawingFootball", Vector3.zero, out Rigidbody body, out FootballPhysicsController controller);
            float yawBefore = body.rotation.eulerAngles.y;

            controller.Flick(CommandWithContact(football.transform.position + new Vector3(0.45f, 0f, 0f)));

            yield return new WaitForFixedUpdate();

            float yawAfterFirstStep = body.rotation.eulerAngles.y;
            float yawVelocityAfterFirstStep = body.angularVelocity.y;

            yield return new WaitForFixedUpdate();

            float yawAfterSecondStep = body.rotation.eulerAngles.y;

            Assert.That(Mathf.Abs(yawVelocityAfterFirstStep), Is.GreaterThan(0.01f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(yawBefore, yawAfterFirstStep)), Is.GreaterThan(0.001f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(yawAfterFirstStep, yawAfterSecondStep)), Is.GreaterThan(0.001f));

            Object.Destroy(football);
        }

        [UnityTest]
        public IEnumerator TabletopAlignedFootballOffCenterFlickProducesVisibleYaw()
        {
            GameObject football = CreatePhysicsFootball("TabletopYawingFootball", Vector3.zero, out Rigidbody body, out FootballPhysicsController controller);
            Quaternion startingRotation = Quaternion.identity;
            controller.PlaceAt(Vector3.zero, startingRotation);

            controller.Flick(CommandWithContact(football.transform.position + new Vector3(0.45f, 0f, 0f), 1f));

            yield return new WaitForFixedUpdate();

            float yawAfterFirstStep = body.rotation.eulerAngles.y;
            float yawVelocityAfterFirstStep = body.angularVelocity.y;

            yield return new WaitForFixedUpdate();

            float yawAfterSecondStep = body.rotation.eulerAngles.y;

            Assert.That(Mathf.Abs(controller.LastAppliedYawTorqueImpulse.y), Is.GreaterThan(0.001f));
            Assert.That(Mathf.Abs(yawVelocityAfterFirstStep), Is.GreaterThan(0.05f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(startingRotation.eulerAngles.y, yawAfterFirstStep)), Is.GreaterThan(0.001f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(yawAfterFirstStep, yawAfterSecondStep)), Is.GreaterThan(0.001f));
            Assert.IsFalse((body.constraints & RigidbodyConstraints.FreezeRotationY) != 0);

            Object.Destroy(football);
        }

        [UnityTest]
        public IEnumerator OffCenterFlickYawEventuallySlowsBecauseOfAngularDamping()
        {
            PaperFootballRuleSet rules = new()
            {
                footballAngularDamping = 1.5f
            };
            GameObject football = CreatePhysicsFootball("DampedYawFootball", Vector3.zero, out Rigidbody body, out FootballPhysicsController controller, rules);

            controller.Flick(CommandWithContact(football.transform.position + new Vector3(0.45f, 0f, 0f), 1f));

            yield return new WaitForFixedUpdate();

            float initialYawSpeed = Mathf.Abs(body.angularVelocity.y);

            yield return new WaitForSeconds(4f);

            float finalYawSpeed = Mathf.Abs(body.angularVelocity.y);
            Assert.That(initialYawSpeed, Is.GreaterThan(0.01f));
            Assert.That(finalYawSpeed, Is.LessThan(initialYawSpeed));
            Assert.That(finalYawSpeed, Is.LessThan(0.15f));

            Object.Destroy(football);
        }

        [UnityTest]
        public IEnumerator RestDetectorDoesNotCompleteWhileAngularVelocityIsAboveThreshold()
        {
            GameObject football = new("AngularRestFootball");
            Rigidbody body = football.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.angularDamping = 0f;
            FootballRestDetector detector = football.AddComponent<FootballRestDetector>();
            detector.Configure(new PaperFootballRuleSet
            {
                footballStoppingThreshold = 0.05f,
                angularStoppingThreshold = 0.1f,
                requiredStillTime = 0.05f
            });

            body.angularVelocity = Vector3.up;
            body.WakeUp();

            yield return new WaitForSeconds(0.15f);

            Assert.IsFalse(detector.IsResting);

            body.angularVelocity = Vector3.zero;

            yield return new WaitForSeconds(0.15f);

            Assert.IsTrue(detector.IsResting);
            Object.Destroy(football);
        }

        [UnityTest]
        public IEnumerator PlaceAtRestoresExpectedRotationAfterManualReset()
        {
            GameObject football = CreatePhysicsFootball("ResetRotationFootball", Vector3.zero, out Rigidbody body, out FootballPhysicsController controller);
            Quaternion resetRotation = Quaternion.Euler(0f, 35f, 0f);
            controller.PlaceAt(Vector3.zero, resetRotation);

            controller.Flick(CommandWithContact(football.transform.position + new Vector3(0.45f, 0f, 0f)));

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(resetRotation.eulerAngles.y, body.rotation.eulerAngles.y)), Is.GreaterThan(0.001f));

            controller.PlaceAt(Vector3.zero, resetRotation);

            Assert.That(Mathf.Abs(Mathf.DeltaAngle(resetRotation.eulerAngles.y, body.rotation.eulerAngles.y)), Is.LessThan(0.001f));
            Assert.That(body.angularVelocity.sqrMagnitude, Is.EqualTo(0f).Within(0.0001f));

            Object.Destroy(football);
        }

        private static GameObject CreatePhysicsFootball(
            string name,
            Vector3 position,
            out Rigidbody body,
            out FootballPhysicsController controller,
            PaperFootballRuleSet rules = null)
        {
            GameObject football = GameObject.CreatePrimitive(PrimitiveType.Cube);
            football.name = name;
            football.transform.position = position;
            body = football.AddComponent<Rigidbody>();
            controller = football.AddComponent<FootballPhysicsController>();
            controller.Configure(rules ?? new PaperFootballRuleSet());
            body.useGravity = false;
            return football;
        }

        private static FlickCommand CommandWithContact(Vector3 contactPoint, float force = 2f)
        {
            return new FlickCommand(
                true,
                Vector3.zero,
                new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0f, -1f),
                Vector3.forward,
                force,
                1f,
                0.2f,
                0.5f,
                contactPoint);
        }
    }
}
