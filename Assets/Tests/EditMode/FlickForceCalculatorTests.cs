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
    }
}
