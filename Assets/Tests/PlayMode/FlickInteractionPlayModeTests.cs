using System.Collections;
using NUnit.Framework;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Presentation;
using PaperFootball.Tabletop.Rules;
using UnityEngine;
using UnityEngine.TestTools;

namespace PaperFootball.Tabletop.PlayModeTests
{
    public class FlickInteractionPlayModeTests
    {
        [UnityTest]
        public IEnumerator ContactMarkerFollowsFootballTransform()
        {
            GameObject football = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject indicatorObject = new("ContactIndicator");
            ContactPointIndicator indicator = indicatorObject.AddComponent<ContactPointIndicator>();
            indicator.Configure(marker.transform, null, null);

            Collider collider = football.GetComponent<Collider>();
            SelectedContactPoint contactPoint = new(collider, new Vector3(0.5f, 0f, 0f), Vector3.right);
            indicator.Show(contactPoint);

            yield return null;

            football.transform.SetPositionAndRotation(new Vector3(1f, 0.5f, -0.25f), Quaternion.Euler(0f, 90f, 0f));
            yield return null;

            Vector3 expected = contactPoint.GetWorldPoint() + contactPoint.GetWorldNormal() * 0.025f;
            Assert.That(marker.transform.position.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(marker.transform.position.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(marker.transform.position.z, Is.EqualTo(expected.z).Within(0.0001f));

            Object.Destroy(indicatorObject);
            Object.Destroy(marker);
            Object.Destroy(football);
        }

        [UnityTest]
        public IEnumerator WaitingForFlickStartsContactSelectionAndDisablesDragInput()
        {
            InteractionFixture fixture = InteractionFixture.Create();
            try
            {
                PaperFootballMatch match = new(new PaperFootballRuleSet());
                fixture.Controller.ApplyMatchState(match);

                yield return null;

                Assert.That(fixture.Controller.State, Is.EqualTo(FlickInteractionState.WaitingForContact));
                Assert.IsTrue(fixture.Selector.InputEnabled);
                Assert.IsFalse(fixture.InputReader.InputEnabled);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [UnityTest]
        public IEnumerator ContactSelectionTargetsFootballBehindGoalpostColliders()
        {
            GameObject football = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject goalTrigger = new("GoalMouthTrigger");
            GameObject upright = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject selectorObject = new("ContactPointSelector");

            try
            {
                football.transform.position = new Vector3(0f, 0f, 5f);
                Collider footballCollider = football.GetComponent<Collider>();

                BoxCollider triggerCollider = goalTrigger.AddComponent<BoxCollider>();
                triggerCollider.isTrigger = true;
                goalTrigger.transform.position = new Vector3(0f, 0f, 2f);
                goalTrigger.transform.localScale = new Vector3(2f, 2f, 0.25f);

                upright.transform.position = new Vector3(0f, 0f, 3f);
                upright.transform.localScale = new Vector3(0.2f, 2f, 0.2f);

                ContactPointSelector selector = selectorObject.AddComponent<ContactPointSelector>();
                selector.Configure(null, footballCollider);
                UnityEngine.Physics.SyncTransforms();

                bool selected = selector.TrySelectFromRay(new Ray(Vector3.zero, Vector3.forward), out SelectedContactPoint contactPoint);

                Assert.IsTrue(selected);
                Assert.That(contactPoint.Collider, Is.EqualTo(footballCollider));
            }
            finally
            {
                Object.Destroy(selectorObject);
                Object.Destroy(upright);
                Object.Destroy(goalTrigger);
                Object.Destroy(football);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ContactSelectionIsDisabledDuringResolution()
        {
            InteractionFixture fixture = InteractionFixture.Create();
            try
            {
                PaperFootballMatch match = new(new PaperFootballRuleSet());
                Assert.IsTrue(match.TryBeginFlick());
                fixture.Controller.ApplyMatchState(match);

                yield return null;

                Assert.That(fixture.Controller.State, Is.EqualTo(FlickInteractionState.Resolving));
                Assert.IsFalse(fixture.Selector.InputEnabled);
                Assert.IsFalse(fixture.InputReader.InputEnabled);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [UnityTest]
        public IEnumerator ConfirmedContactPointIsPreservedForFlickDrag()
        {
            InteractionFixture fixture = InteractionFixture.Create();
            try
            {
                PaperFootballMatch match = new(new PaperFootballRuleSet());
                fixture.Controller.ApplyMatchState(match);
                SelectedContactPoint contactPoint = new(
                    fixture.FootballCollider,
                    new Vector3(0.35f, 0f, 0f),
                    Vector3.right);

                Assert.IsTrue(fixture.Controller.TryConfirmContactPoint(contactPoint));

                yield return null;

                Vector3 expectedWorldPoint = contactPoint.GetWorldPoint();
                Assert.That(fixture.Controller.State, Is.EqualTo(FlickInteractionState.WaitingForFlick));
                Assert.IsTrue(fixture.Controller.HasSelectedContactPoint);
                Assert.IsTrue(fixture.InputReader.HasContactPointOverride);
                Assert.That(fixture.InputReader.ContactPointOverrideWorld.x, Is.EqualTo(expectedWorldPoint.x).Within(0.0001f));
                Assert.That(fixture.InputReader.ContactPointOverrideWorld.y, Is.EqualTo(expectedWorldPoint.y).Within(0.0001f));
                Assert.That(fixture.InputReader.ContactPointOverrideWorld.z, Is.EqualTo(expectedWorldPoint.z).Within(0.0001f));
            }
            finally
            {
                fixture.Destroy();
            }
        }

        [UnityTest]
        public IEnumerator ResetStyleClearSelectionRemovesStaleContactPoint()
        {
            InteractionFixture fixture = InteractionFixture.Create();
            try
            {
                PaperFootballMatch match = new(new PaperFootballRuleSet());
                fixture.Controller.ApplyMatchState(match);
                SelectedContactPoint contactPoint = new(fixture.FootballCollider, new Vector3(0.35f, 0f, 0f), Vector3.right);
                Assert.IsTrue(fixture.Controller.TryConfirmContactPoint(contactPoint));

                fixture.Controller.ClearSelection();

                yield return null;

                Assert.IsFalse(fixture.Controller.HasSelectedContactPoint);
                Assert.IsFalse(fixture.InputReader.HasContactPointOverride);
            }
            finally
            {
                fixture.Destroy();
            }
        }

        private sealed class InteractionFixture
        {
            private InteractionFixture(
                GameObject root,
                ContactPointSelector selector,
                FlickInputReader inputReader,
                FlickInteractionController controller,
                Collider footballCollider)
            {
                Root = root;
                Selector = selector;
                InputReader = inputReader;
                Controller = controller;
                FootballCollider = footballCollider;
            }

            public GameObject Root { get; }
            public ContactPointSelector Selector { get; }
            public FlickInputReader InputReader { get; }
            public FlickInteractionController Controller { get; }
            public Collider FootballCollider { get; }

            public static InteractionFixture Create()
            {
                GameObject root = new("InteractionFixture");
                GameObject cameraObject = new("InteractionCamera");
                cameraObject.transform.SetParent(root.transform);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.transform.SetPositionAndRotation(new Vector3(0f, 5f, -5f), Quaternion.Euler(45f, 0f, 0f));
                FootballCameraController cameraController = cameraObject.AddComponent<FootballCameraController>();

                GameObject football = GameObject.CreatePrimitive(PrimitiveType.Cube);
                football.transform.SetParent(root.transform);
                Rigidbody body = football.AddComponent<Rigidbody>();
                body.useGravity = false;
                FootballPhysicsController physics = football.AddComponent<FootballPhysicsController>();
                physics.Configure(new PaperFootballRuleSet());
                Collider collider = football.GetComponent<Collider>();

                GameObject selectorObject = new("ContactPointSelector");
                selectorObject.transform.SetParent(root.transform);
                ContactPointSelector selector = selectorObject.AddComponent<ContactPointSelector>();
                selector.Configure(camera, collider);

                GameObject inputObject = new("FlickInputReader");
                inputObject.transform.SetParent(root.transform);
                FlickInputReader inputReader = inputObject.AddComponent<FlickInputReader>();
                inputReader.Configure(camera, collider, new PaperFootballRuleSet(), 0f);

                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.transform.SetParent(root.transform);
                GameObject indicatorObject = new("ContactPointIndicator");
                indicatorObject.transform.SetParent(root.transform);
                ContactPointIndicator indicator = indicatorObject.AddComponent<ContactPointIndicator>();
                indicator.Configure(marker.transform, null, null);

                GameObject controllerObject = new("FlickInteractionController");
                controllerObject.transform.SetParent(root.transform);
                FlickInteractionController controller = controllerObject.AddComponent<FlickInteractionController>();

                cameraController.Configure(camera, football.transform, new Vector3(0f, 5f, -5f), Vector3.zero, 5f, new Vector3(0f, 2f, -2f), 1f, 0f);
                controller.Configure(selector, inputReader, cameraController, indicator, physics, collider);
                return new InteractionFixture(root, selector, inputReader, controller, collider);
            }

            public void Destroy()
            {
                Object.Destroy(Root);
            }

        }
    }
}
