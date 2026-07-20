using System.Collections;
using NUnit.Framework;
using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Input;
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
            AirFlickLandingController airFlickLandingController = Object.FindFirstObjectByType<AirFlickLandingController>();
            GameHudController hudController = Object.FindFirstObjectByType<GameHudController>();
            ShotSelectionController shotSelectionController = Object.FindFirstObjectByType<ShotSelectionController>();
            FlickInteractionController interactionController = Object.FindFirstObjectByType<FlickInteractionController>();
            ContactPointSelector contactPointSelector = Object.FindFirstObjectByType<ContactPointSelector>();
            ContactPointIndicator contactPointIndicator = Object.FindFirstObjectByType<ContactPointIndicator>();
            FootballCameraController cameraController = Object.FindFirstObjectByType<FootballCameraController>();
            FootballSpinDebugOverlay spinDebugOverlay = Object.FindFirstObjectByType<FootballSpinDebugOverlay>();
            GoalPostTrigger[] goalTriggers = Object.FindObjectsByType<GoalPostTrigger>(FindObjectsSortMode.None);
            Transform foldLine = physicsController != null ? physicsController.transform.Find("FootballFoldLine") : null;
            Transform cornerMark = physicsController != null ? physicsController.transform.Find("FootballCornerMark") : null;
            Transform visual = physicsController != null ? physicsController.transform.Find("PaperFootballVisual") : null;

            Assert.IsNotNull(matchController);
            Assert.IsNotNull(matchController.Match);
            Assert.IsNotNull(fieldGoalController);
            Assert.IsNotNull(physicsController);
            Assert.IsNotNull(airFlickLandingController);
            Assert.IsNotNull(hudController);
            Assert.IsNotNull(shotSelectionController);
            Assert.IsNotNull(interactionController);
            Assert.IsNotNull(contactPointSelector);
            Assert.IsNotNull(contactPointIndicator);
            Assert.IsNotNull(cameraController);
            Assert.IsNotNull(spinDebugOverlay);
            Assert.IsNotNull(foldLine);
            Assert.IsNotNull(cornerMark);
            Assert.IsNotNull(visual);
            Assert.IsFalse((physicsController.Rigidbody.constraints & RigidbodyConstraints.FreezeRotationY) != 0);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, physicsController.transform.eulerAngles.x)), Is.LessThan(0.01f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, physicsController.transform.eulerAngles.z)), Is.LessThan(0.01f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(90f, visual.localEulerAngles.x)), Is.LessThan(0.01f));
            Assert.IsNotNull(foldLine.GetComponent<Renderer>());
            Assert.IsFalse(foldLine.TryGetComponent(out Collider _));
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

        [UnityTest]
        public IEnumerator VisibleSpinReferenceStaysParentedToRigidbodyFootball()
        {
            yield return LoadPrototypeScene();

            FootballPhysicsController physicsController = Object.FindFirstObjectByType<FootballPhysicsController>();
            Transform foldLine = physicsController.transform.Find("FootballFoldLine");
            Vector3 localPosition = foldLine.localPosition;
            Quaternion localRotation = foldLine.localRotation;

            physicsController.transform.rotation = Quaternion.Euler(0f, 47f, 0f);

            yield return null;

            Assert.That(foldLine.parent, Is.EqualTo(physicsController.transform));
            Assert.That(foldLine.localPosition.x, Is.EqualTo(localPosition.x).Within(0.0001f));
            Assert.That(foldLine.localPosition.y, Is.EqualTo(localPosition.y).Within(0.0001f));
            Assert.That(foldLine.localPosition.z, Is.EqualTo(localPosition.z).Within(0.0001f));
            Assert.That(Quaternion.Angle(localRotation, foldLine.localRotation), Is.LessThan(0.001f));
        }

        private static IEnumerator LoadPrototypeScene()
        {
            SceneManager.LoadScene("PaperFootballGame");
            yield return null;
            yield return null;
        }
    }
}
