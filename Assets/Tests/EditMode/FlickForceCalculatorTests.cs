using NUnit.Framework;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Tests
{
    public class FlickForceCalculatorTests
    {
        private PaperFootballRuleSet rules;

        [SetUp]
        public void SetUp()
        {
            rules = new PaperFootballRuleSet
            {
                minimumDragDistance = 0.1f,
                maximumDragDistance = 2f,
                minimumFlickForce = 2f,
                maximumFlickForce = 20f
            };
        }

        [Test]
        public void DragBackwardCreatesForwardSlingshotForce()
        {
            FlickCommand command = FlickForceCalculator.Calculate(Vector3.zero, new Vector3(0f, 0f, -1f), 0.3f, rules);

            Assert.IsTrue(command.IsValid);
            Assert.That(command.Direction.z, Is.GreaterThan(0.99f));
            Assert.That(command.Force, Is.GreaterThan(rules.minimumFlickForce));
        }

        [Test]
        public void MinimumDragDistanceRejectsInvalidInput()
        {
            FlickCommand command = FlickForceCalculator.Calculate(Vector3.zero, new Vector3(0f, 0f, -0.01f), 0.3f, rules);

            Assert.IsFalse(command.IsValid);
            Assert.That(command.Force, Is.EqualTo(0f));
        }

        [Test]
        public void ForceClampsAtMaximum()
        {
            FlickCommand command = FlickForceCalculator.Calculate(Vector3.zero, new Vector3(0f, 0f, -10f), 0.3f, rules);

            Assert.IsTrue(command.IsValid);
            Assert.That(command.DragDistance, Is.EqualTo(rules.maximumDragDistance).Within(0.0001f));
            Assert.That(command.Force, Is.EqualTo(rules.maximumFlickForce).Within(0.0001f));
        }

        [Test]
        public void ResponseExponentSoftensLowStrengthForce()
        {
            rules.minimumDragDistance = 0f;
            rules.maximumDragDistance = 1f;
            rules.minimumFlickForce = 0f;
            rules.maximumFlickForce = 10f;
            rules.flickForceResponseExponent = 2f;

            FlickCommand command = FlickForceCalculator.Calculate(Vector3.zero, new Vector3(0f, 0f, -0.5f), 0.3f, rules);

            Assert.IsTrue(command.IsValid);
            Assert.That(command.Strength01, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(command.Force, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void ContactPointIsPreservedForPhysicsApplication()
        {
            Vector3 contactPoint = new(0.2f, 0.16f, 0.05f);

            FlickCommand command = FlickForceCalculator.Calculate(
                Vector3.zero,
                new Vector3(0f, 0f, -1f),
                0.3f,
                rules,
                contactPoint);

            Assert.IsTrue(command.IsValid);
            Assert.IsTrue(command.HasContactPoint);
            Assert.That(command.ContactPointWorld.x, Is.EqualTo(contactPoint.x).Within(0.0001f));
            Assert.That(command.ContactPointWorld.y, Is.EqualTo(contactPoint.y).Within(0.0001f));
            Assert.That(command.ContactPointWorld.z, Is.EqualTo(contactPoint.z).Within(0.0001f));
        }
    }
}
