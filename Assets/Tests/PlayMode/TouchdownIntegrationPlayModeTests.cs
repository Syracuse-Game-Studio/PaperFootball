using System.Collections;
using NUnit.Framework;
using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Match;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Rules;
using PaperFootball.Tabletop.Scoring;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PaperFootball.Tabletop.PlayModeTests
{
    public class TouchdownIntegrationPlayModeTests
    {
        [UnityTest]
        public IEnumerator TinyPositiveOverhangAwardsExactlyOneTouchdownAndEntersFieldGoalSetup()
        {
            yield return LoadPrototypeScene();
            SceneRefs refs = SceneRefs.Find();
            refs.Controller.Match.ResetMatch();

            Assert.That(refs.Controller.Match.PlayerOneScore, Is.EqualTo(0));

            PlaceFootballAtPositiveOverhang(refs, 0.005f);
            refs.RestDetector.ResetDetector();
            Assert.IsTrue(refs.Controller.Match.TryBeginFlick());

            yield return WaitForPhaseExit(refs.Controller, MatchPhase.FootballMoving);

            Assert.That(refs.Controller.Match.PlayerOneScore, Is.EqualTo(6));
            Assert.That(refs.Controller.Match.PlayerTwoScore, Is.EqualTo(0));
            Assert.That(refs.Controller.Match.Phase, Is.EqualTo(MatchPhase.FieldGoalSetup));
            Assert.That(refs.Controller.Match.CurrentPlayer, Is.EqualTo(PaperFootballPlayer.PlayerOne));

            for (int i = 0; i < 12; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(refs.Controller.Match.PlayerOneScore, Is.EqualTo(6));
            Assert.IsTrue(refs.Controller.LatestOverhangSnapshot.HasValue);
            OverhangDebugSnapshot snapshot = refs.Controller.LatestOverhangSnapshot.Value;
            Assert.IsTrue(snapshot.HasPositiveOverhang);
            Assert.IsTrue(snapshot.FinalTouchdownDecision);
            Assert.IsTrue(snapshot.ScoringEventAlreadyProcessed);
        }

        [UnityTest]
        public IEnumerator ZeroOrNegativeOverhangDoesNotAwardTouchdown()
        {
            yield return LoadPrototypeScene();
            SceneRefs refs = SceneRefs.Find();
            refs.Controller.Match.ResetMatch();

            PlaceFootballAtPositiveOverhang(refs, -0.005f);
            refs.RestDetector.ResetDetector();
            Assert.IsTrue(refs.Controller.Match.TryBeginFlick());

            yield return WaitForPhaseExit(refs.Controller, MatchPhase.FootballMoving);

            Assert.That(refs.Controller.Match.PlayerOneScore, Is.EqualTo(0));
            Assert.That(refs.Controller.Match.Phase, Is.Not.EqualTo(MatchPhase.FieldGoalSetup));
            Assert.IsTrue(refs.Controller.LatestOverhangSnapshot.HasValue);
            Assert.IsFalse(refs.Controller.LatestOverhangSnapshot.Value.HasPositiveOverhang);
            Assert.IsFalse(refs.Controller.LatestOverhangSnapshot.Value.FinalTouchdownDecision);
        }

        private static IEnumerator LoadPrototypeScene()
        {
            SceneManager.LoadScene("PaperFootballGame");
            yield return null;
            yield return null;
        }

        private static IEnumerator WaitForPhaseExit(MatchController controller, MatchPhase phase)
        {
            int frames = 0;
            while (controller.Match.Phase == phase && frames < 120)
            {
                frames++;
                yield return new WaitForFixedUpdate();
            }

            Assert.That(controller.Match.Phase, Is.Not.EqualTo(phase));
        }

        private static void PlaceFootballAtPositiveOverhang(SceneRefs refs, float overhangDistance)
        {
            Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
            refs.Physics.PlaceAt(new Vector3(0f, refs.Table.TableTopY + 0.2f, 0f), rotation);
            UnityEngine.Physics.SyncTransforms();

            Bounds tableBounds = refs.Table.TableBounds;
            Bounds footballBounds = refs.FootballCollider.bounds;
            float centerY = tableBounds.max.y + footballBounds.extents.y + 0.005f;
            float centerZ = tableBounds.max.z - footballBounds.extents.z + overhangDistance;
            refs.Physics.PlaceAt(new Vector3(0f, centerY, centerZ), rotation);
            refs.Physics.Stop();
            UnityEngine.Physics.SyncTransforms();

            Assert.That(refs.FootballCollider.bounds.min.y, Is.GreaterThan(refs.Table.TableTopY - 0.01f));
        }

        private readonly struct SceneRefs
        {
            private SceneRefs(
                MatchController controller,
                FootballPhysicsController physics,
                FootballRestDetector restDetector,
                TableBoundaryDetector table,
                Collider footballCollider,
                FieldGoalController fieldGoalController)
            {
                Controller = controller;
                Physics = physics;
                RestDetector = restDetector;
                Table = table;
                FootballCollider = footballCollider;
                FieldGoalController = fieldGoalController;
            }

            public MatchController Controller { get; }
            public FootballPhysicsController Physics { get; }
            public FootballRestDetector RestDetector { get; }
            public TableBoundaryDetector Table { get; }
            public Collider FootballCollider { get; }
            public FieldGoalController FieldGoalController { get; }

            public static SceneRefs Find()
            {
                MatchController controller = Object.FindFirstObjectByType<MatchController>();
                FootballPhysicsController physics = Object.FindFirstObjectByType<FootballPhysicsController>();
                FootballRestDetector restDetector = Object.FindFirstObjectByType<FootballRestDetector>();
                TableBoundaryDetector table = Object.FindFirstObjectByType<TableBoundaryDetector>();
                FieldGoalController fieldGoalController = Object.FindFirstObjectByType<FieldGoalController>();
                Collider collider = physics.GetComponent<Collider>();

                Assert.IsNotNull(controller);
                Assert.IsNotNull(physics);
                Assert.IsNotNull(restDetector);
                Assert.IsNotNull(table);
                Assert.IsNotNull(collider);
                Assert.IsNotNull(fieldGoalController);

                return new SceneRefs(controller, physics, restDetector, table, collider, fieldGoalController);
            }
        }
    }
}
