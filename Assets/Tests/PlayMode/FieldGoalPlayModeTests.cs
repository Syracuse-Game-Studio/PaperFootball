using System.Collections;
using NUnit.Framework;
using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Match;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Presentation;
using PaperFootball.Tabletop.Rules;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PaperFootball.Tabletop.PlayModeTests
{
    public class FieldGoalPlayModeTests
    {
        [UnityTest]
        public IEnumerator TrajectoryPreviewAppearsDuringAimAndDisappearsAfterRelease()
        {
            yield return LoadPrototypeScene();
            MatchController controller = Object.FindFirstObjectByType<MatchController>();
            FootballPhysicsController physics = Object.FindFirstObjectByType<FootballPhysicsController>();
            TrajectoryPreviewRenderer preview = Object.FindFirstObjectByType<TrajectoryPreviewRenderer>();

            EnterFieldGoalSetup(controller);
            FlickCommand command = ValidFieldGoalCommand();

            FieldGoalKickResult previewResult = controller.PreviewFieldGoalKick(command);

            Assert.IsTrue(previewResult.IsValid);
            Assert.IsTrue(preview.IsVisible);
            Assert.That(preview.LastPreviewImpulse, Is.EqualTo(previewResult.TotalImpulse));

            Vector3 expectedVelocity = previewResult.TotalImpulse / physics.Rigidbody.mass;
            Assert.IsTrue(controller.TryLaunchFieldGoalKick(command));

            Assert.IsFalse(preview.IsVisible);
            Assert.That(controller.Match.Phase, Is.EqualTo(MatchPhase.FieldGoalAttempt));
            yield return new WaitForFixedUpdate();

            Assert.That(physics.Rigidbody.linearVelocity.x, Is.EqualTo(expectedVelocity.x).Within(1f));
            Assert.That(physics.Rigidbody.linearVelocity.y, Is.EqualTo(expectedVelocity.y + UnityEngine.Physics.gravity.y * Time.fixedDeltaTime).Within(1f));
            Assert.That(physics.Rigidbody.linearVelocity.z, Is.EqualTo(expectedVelocity.z).Within(1f));
        }

        [UnityTest]
        public IEnumerator GoalTriggerCannotScoreMoreThanOnceForSameAttempt()
        {
            yield return LoadPrototypeScene();
            FieldGoalController fieldGoalController = Object.FindFirstObjectByType<FieldGoalController>();
            FootballPhysicsController physics = Object.FindFirstObjectByType<FootballPhysicsController>();
            int scoreEvents = 0;
            fieldGoalController.FieldGoalScored += () => scoreEvents++;

            fieldGoalController.BeginAttempt(PaperFootballPlayer.PlayerOne);
            fieldGoalController.ReportGoalMouthEntered(PaperFootballPlayer.PlayerOne, physics.GetComponent<Collider>());
            fieldGoalController.ReportGoalMouthEntered(PaperFootballPlayer.PlayerOne, physics.GetComponent<Collider>());

            Assert.That(scoreEvents, Is.EqualTo(1));
            Assert.IsTrue(fieldGoalController.ScoredThisAttempt);
        }

        private static IEnumerator LoadPrototypeScene()
        {
            SceneManager.LoadScene("PaperFootballGame");
            yield return null;
            yield return null;
        }

        private static void EnterFieldGoalSetup(MatchController controller)
        {
            controller.Match.ResetMatch();
            Assert.IsTrue(controller.Match.TryBeginFlick());
            Assert.IsTrue(controller.Match.TryBeginResolving());
            controller.Match.ApplyResolution(FlickResolutionType.Touchdown);
            Assert.That(controller.Match.Phase, Is.EqualTo(MatchPhase.FieldGoalSetup));
        }

        private static FlickCommand ValidFieldGoalCommand()
        {
            Vector3 start = Vector3.zero;
            Vector3 current = new(0f, 0f, -1.25f);
            return new FlickCommand(true, start, current, current, Vector3.forward, 1f, 1.25f, 0.2f, 0.5f);
        }
    }
}
