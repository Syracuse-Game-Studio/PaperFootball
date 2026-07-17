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
            body.useGravity = false;
            FootballPhysicsController controller = football.AddComponent<FootballPhysicsController>();
            controller.Configure(new PaperFootballRuleSet());

            FlickCommand command = new(true, Vector3.zero, new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, -1f), Vector3.forward, 5f, 1f, 0.2f, 0.5f);
            controller.Flick(command);

            yield return new WaitForFixedUpdate();

            Assert.That(body.linearVelocity.z, Is.GreaterThan(0f));
            Object.Destroy(football);
        }
    }
}
