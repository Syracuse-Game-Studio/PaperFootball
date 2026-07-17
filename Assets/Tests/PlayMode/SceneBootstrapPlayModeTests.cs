using System.Collections;
using NUnit.Framework;
using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Match;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PaperFootball.Tabletop.PlayModeTests
{
    public class SceneBootstrapPlayModeTests
    {
        [UnityTest]
        public IEnumerator PaperFootballGameSceneHasPrototypeReferences()
        {
            yield return LoadPrototypeScene();

            MatchController matchController = Object.FindFirstObjectByType<MatchController>();
            FieldGoalController fieldGoalController = Object.FindFirstObjectByType<FieldGoalController>();
            FootballPhysicsController physicsController = Object.FindFirstObjectByType<FootballPhysicsController>();
            GameHudController hudController = Object.FindFirstObjectByType<GameHudController>();
            GoalPostTrigger[] goalTriggers = Object.FindObjectsByType<GoalPostTrigger>(FindObjectsSortMode.None);

            Assert.IsNotNull(matchController);
            Assert.IsNotNull(matchController.Match);
            Assert.IsNotNull(fieldGoalController);
            Assert.IsNotNull(physicsController);
            Assert.IsNotNull(hudController);
            Assert.That(goalTriggers.Length, Is.GreaterThanOrEqualTo(2));
        }

        [UnityTest]
        public IEnumerator PaperFootballGameCameraFramesFootballAtKickoff()
        {
            yield return LoadPrototypeScene();

            Camera camera = Camera.main;
            FootballPhysicsController physicsController = Object.FindFirstObjectByType<FootballPhysicsController>();
            Renderer footballRenderer = physicsController.GetComponentInChildren<Renderer>();

            Assert.IsNotNull(camera);
            Assert.IsNotNull(footballRenderer);

            Vector3 viewportPoint = camera.WorldToViewportPoint(footballRenderer.bounds.center);
            Assert.That(viewportPoint.z, Is.GreaterThan(0f));
            Assert.That(viewportPoint.x, Is.InRange(0.08f, 0.92f));
            Assert.That(viewportPoint.y, Is.InRange(0.08f, 0.92f));
        }

        private static IEnumerator LoadPrototypeScene()
        {
            SceneManager.LoadScene("PaperFootballGame");
            yield return null;
            yield return null;
        }
    }
}
