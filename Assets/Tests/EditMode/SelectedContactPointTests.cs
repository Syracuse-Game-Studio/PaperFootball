using NUnit.Framework;
using PaperFootball.Tabletop.Input;
using UnityEngine;

namespace PaperFootball.Tabletop.Tests
{
    public class SelectedContactPointTests
    {
        [Test]
        public void LocalContactPointConvertsBackToExpectedWorldPoint()
        {
            GameObject football = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                football.transform.SetPositionAndRotation(new Vector3(2f, 0.5f, -1f), Quaternion.Euler(0f, 35f, 0f));
                Collider collider = football.GetComponent<Collider>();
                Vector3 worldPoint = football.transform.TransformPoint(new Vector3(0.25f, 0.1f, -0.2f));
                Vector3 worldNormal = football.transform.TransformDirection(Vector3.right);

                SelectedContactPoint contactPoint = new(
                    collider,
                    football.transform.InverseTransformPoint(worldPoint),
                    football.transform.InverseTransformDirection(worldNormal));

                Assert.That(contactPoint.GetWorldPoint().x, Is.EqualTo(worldPoint.x).Within(0.0001f));
                Assert.That(contactPoint.GetWorldPoint().y, Is.EqualTo(worldPoint.y).Within(0.0001f));
                Assert.That(contactPoint.GetWorldPoint().z, Is.EqualTo(worldPoint.z).Within(0.0001f));
                Assert.That(Vector3.Dot(contactPoint.GetWorldNormal(), worldNormal.normalized), Is.GreaterThan(0.999f));
            }
            finally
            {
                Object.DestroyImmediate(football);
            }
        }

        [Test]
        public void ContactPointRemainsAttachedWhenFootballRotates()
        {
            GameObject football = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                Collider collider = football.GetComponent<Collider>();
                SelectedContactPoint contactPoint = new(collider, new Vector3(0.5f, 0f, 0f), Vector3.right);

                football.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

                Vector3 expectedPoint = football.transform.TransformPoint(new Vector3(0.5f, 0f, 0f));
                Assert.That(contactPoint.GetWorldPoint().x, Is.EqualTo(expectedPoint.x).Within(0.0001f));
                Assert.That(contactPoint.GetWorldPoint().y, Is.EqualTo(expectedPoint.y).Within(0.0001f));
                Assert.That(contactPoint.GetWorldPoint().z, Is.EqualTo(expectedPoint.z).Within(0.0001f));
                Assert.That(Vector3.Dot(contactPoint.GetWorldNormal(), football.transform.right), Is.GreaterThan(0.999f));
            }
            finally
            {
                Object.DestroyImmediate(football);
            }
        }
    }
}
