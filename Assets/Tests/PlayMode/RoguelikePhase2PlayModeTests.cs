using System.Collections;
using NUnit.Framework;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Presentation;
using PaperFootball.Tabletop.Roguelike.Encounters;
using PaperFootball.Tabletop.Roguelike.Opponents;
using PaperFootball.Tabletop.Roguelike.Presentation;
using PaperFootball.Tabletop.Roguelike.Random;
using PaperFootball.Tabletop.Roguelike.Run;
using PaperFootball.Tabletop.Roguelike.Variance;
using PaperFootball.Tabletop.Rules;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PaperFootball.Tabletop.PlayModeTests
{
    public class RoguelikePhase2PlayModeTests
    {
        [UnityTest]
        public IEnumerator ShotVarianceChangesAppliedPhysicalInputWhenEnabled()
        {
            GameObject football = CreateFootball(out FootballPhysicsController physics, out Collider collider);
            GameObject varianceObject = new("ShotVarianceController");
            ShotVarianceController variance = varianceObject.AddComponent<ShotVarianceController>();
            ShotVarianceSettings settings = ScriptableObject.CreateInstance<ShotVarianceSettings>();
            settings.Configure(true, 0.5f, 10f, 0.05f, false, "Wild");
            variance.Configure(settings, collider, 100);
            variance.SetVarianceEnabled(true);

            FlickCommand command = Command(football.transform.position + Vector3.right * 0.5f, 2f);
            ResolvedFlickParameters resolved = variance.Resolve(command, new PaperFootballRuleSet(), PaperFootballPlayer.PlayerOne, 1, "test");
            physics.Flick(resolved.ToFlickCommand());

            yield return new WaitForFixedUpdate();

            Assert.That(Mathf.Abs(resolved.AppliedForceMultiplier - 1f), Is.GreaterThan(0.001f));
            Assert.That(Vector3.Angle(command.Direction, resolved.FinalDirection), Is.GreaterThan(0.001f));
            Assert.That(physics.LastAppliedImpulse.magnitude, Is.EqualTo(resolved.FinalForce).Within(0.001f));

            Object.Destroy(settings);
            Object.Destroy(varianceObject);
            Object.Destroy(football);
        }

        [UnityTest]
        public IEnumerator ZeroVariancePreservesBaselinePhysicsInput()
        {
            GameObject football = CreateFootball(out FootballPhysicsController physics, out _);
            FlickCommand command = Command(football.transform.position + Vector3.right * 0.5f, 2f);
            ResolvedFlickParameters resolved = FlickParameterResolver.Resolve(
                command,
                ShotVarianceTuning.Disabled,
                new PaperFootballRuleSet(),
                null,
                new DeterministicRunRandom(1),
                1,
                1);

            physics.Flick(resolved.ToFlickCommand());
            yield return new WaitForFixedUpdate();

            Assert.That(resolved.FinalForce, Is.EqualTo(command.Force).Within(0.0001f));
            Assert.That(physics.LastAppliedImpulse.magnitude, Is.EqualTo(command.Force).Within(0.001f));

            Object.Destroy(football);
        }

        [UnityTest]
        public IEnumerator AiDecisionUsesSharedFlickCommandAndPhysicsController()
        {
            GameObject football = CreateFootball(out FootballPhysicsController physics, out Collider collider);
            OpponentProfile profile = ScriptableObject.CreateInstance<OpponentProfile>();
            profile.Configure("spinner", "Spinner", 0.55f, 0.1f, 1f, OpponentContactPreference.OffCenter, 0.6f, 0.7f, 0.5f, 0.5f, 0.5f, 0f);
            OpponentDecisionContext context = new(profile, collider, new Bounds(Vector3.zero, new Vector3(8f, 1f, 12f)), new PaperFootballRuleSet(), PaperFootballPlayer.PlayerTwo, 1, 0);
            OpponentDecision decision = OpponentDecisionService.Decide(context, new DeterministicRunRandom(2));

            physics.Flick(decision.Command);
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(decision.Command.IsValid);
            Assert.IsTrue(decision.Command.HasContactPoint);
            Assert.That(physics.Rigidbody.linearVelocity.sqrMagnitude, Is.GreaterThan(0.001f));
            Assert.IsTrue(physics.HasLastContactPoint);

            Object.Destroy(profile);
            Object.Destroy(football);
        }

        [UnityTest]
        public IEnumerator SurfaceApplierUsesRuntimePhysicsMaterials()
        {
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Collider tableCollider = table.GetComponent<Collider>();
            TableSurfaceApplier applier = table.AddComponent<TableSurfaceApplier>();
            applier.Configure(tableCollider, table.GetComponent<Renderer>());
            TableSurfaceDefinition normal = ScriptableObject.CreateInstance<TableSurfaceDefinition>();
            normal.Configure("normal", "Normal", TableSurfaceKind.NormalDesk, 0.55f, 0.65f, 0.04f, 1f, Color.white);
            TableSurfaceDefinition slippery = ScriptableObject.CreateInstance<TableSurfaceDefinition>();
            slippery.Configure("slippery", "Slippery", TableSurfaceKind.SlipperyDesk, 0.2f, 0.3f, 0.02f, 0.85f, Color.cyan);
            TableSurfaceDefinition rough = ScriptableObject.CreateInstance<TableSurfaceDefinition>();
            rough.Configure("rough", "Rough", TableSurfaceKind.RoughDesk, 0.95f, 1.05f, 0.01f, 1.2f, Color.gray);

            applier.Apply(normal);
            float normalFriction = tableCollider.sharedMaterial.dynamicFriction;
            applier.Apply(slippery);
            float slipperyFriction = tableCollider.sharedMaterial.dynamicFriction;
            applier.Apply(rough);
            float roughFriction = tableCollider.sharedMaterial.dynamicFriction;

            yield return null;

            Assert.That(slipperyFriction, Is.LessThan(normalFriction));
            Assert.That(roughFriction, Is.GreaterThan(normalFriction));

            Object.Destroy(normal);
            Object.Destroy(slippery);
            Object.Destroy(rough);
            Object.Destroy(table);
        }

        [UnityTest]
        public IEnumerator ObstacleLayoutClearsTemporaryEncounterObjects()
        {
            GameObject root = new("ObstacleRoot");
            ObstacleLayoutController controller = root.AddComponent<ObstacleLayoutController>();
            controller.Configure(root.transform, null);
            ObstacleLayoutDefinition layout = ScriptableObject.CreateInstance<ObstacleLayoutDefinition>();
            layout.Configure("eraser", "Eraser", ObstacleLayoutKind.Eraser, new[]
            {
                new ObstacleSpawn { kind = ObstacleKind.Eraser, position = Vector3.zero, scale = Vector3.one * 0.25f, eulerAngles = Vector3.zero }
            });

            controller.Apply(layout);
            Assert.That(controller.ActiveObstacles.Count, Is.EqualTo(1));

            controller.Clear();
            yield return null;

            Assert.That(controller.ActiveObstacles.Count, Is.EqualTo(0));

            Object.Destroy(layout);
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator PaperFootballGameSceneHasRoguelikePhase2References()
        {
            SceneManager.LoadScene("PaperFootballGame");
            yield return null;
            yield return null;

            Assert.IsNotNull(Object.FindFirstObjectByType<RunController>());
            Assert.IsNotNull(Object.FindFirstObjectByType<ShotVarianceController>());
            Assert.IsNotNull(Object.FindFirstObjectByType<OpponentTurnController>());
            Assert.IsNotNull(Object.FindFirstObjectByType<RunProgressionUiController>());
            Assert.IsNotNull(Object.FindFirstObjectByType<RoguelikeDebugOverlay>());
            Assert.IsNotNull(Object.FindFirstObjectByType<ShotUncertaintyPreview>());
            Assert.IsNotNull(Object.FindFirstObjectByType<TableSurfaceApplier>());
            Assert.IsNotNull(Object.FindFirstObjectByType<ObstacleLayoutController>());
            Assert.IsNotNull(Object.FindFirstObjectByType<PrecisionTargetZone>());
        }

        private static GameObject CreateFootball(out FootballPhysicsController physics, out Collider collider)
        {
            GameObject football = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Rigidbody body = football.AddComponent<Rigidbody>();
            body.useGravity = false;
            physics = football.AddComponent<FootballPhysicsController>();
            physics.Configure(new PaperFootballRuleSet());
            collider = football.GetComponent<Collider>();
            return football;
        }

        private static FlickCommand Command(Vector3 contactPoint, float force)
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
