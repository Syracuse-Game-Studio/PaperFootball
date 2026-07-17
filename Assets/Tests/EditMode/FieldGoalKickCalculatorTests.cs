using NUnit.Framework;
using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Tests
{
    public class FieldGoalKickCalculatorTests
    {
        private PaperFootballRuleSet rules;

        [SetUp]
        public void SetUp()
        {
            rules = new PaperFootballRuleSet
            {
                minimumDragDistance = 0.1f,
                maximumDragDistance = 2f,
                minimumFieldGoalForce = 3f,
                maximumFieldGoalForce = 9f,
                minimumFieldGoalLaunchAngle = 25f,
                maximumFieldGoalLaunchAngle = 55f,
                minimumFieldGoalUpwardForce = 1f,
                maximumFieldGoalUpwardForce = 8f
            };
        }

        [Test]
        public void MinimumDragProducesMinimumValidForwardForce()
        {
            FieldGoalKickResult result = FieldGoalKickCalculator.Calculate(
                CommandWithDrag(rules.minimumDragDistance),
                rules);

            Assert.IsTrue(result.IsValid);
            Assert.That(result.ForwardImpulse, Is.EqualTo(rules.minimumFieldGoalForce).Within(0.0001f));
            Assert.That(result.NormalizedPower, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void MaximumDragIsClamped()
        {
            FieldGoalKickResult result = FieldGoalKickCalculator.Calculate(CommandWithDrag(20f), rules);

            Assert.IsTrue(result.IsValid);
            Assert.That(result.DragDistance, Is.EqualTo(rules.maximumDragDistance).Within(0.0001f));
            Assert.That(result.ForwardImpulse, Is.EqualTo(rules.maximumFieldGoalForce).Within(0.0001f));
        }

        [Test]
        public void InvalidShortDragIsRejected()
        {
            FieldGoalKickResult result = FieldGoalKickCalculator.Calculate(CommandWithDrag(0.01f), rules);

            Assert.IsFalse(result.IsValid);
            Assert.That(result.TotalImpulse, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void HorizontalDirectionIsNormalized()
        {
            FieldGoalKickResult result = FieldGoalKickCalculator.Calculate(CommandWithDrag(1f), rules);

            Assert.That(result.HorizontalDirection.magnitude, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(result.HorizontalDirection.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void UpwardForceIncreasesWithInput()
        {
            FieldGoalKickResult low = FieldGoalKickCalculator.Calculate(CommandWithDrag(0.2f), rules);
            FieldGoalKickResult high = FieldGoalKickCalculator.Calculate(CommandWithDrag(2f), rules);

            Assert.That(high.UpwardImpulse, Is.GreaterThan(low.UpwardImpulse));
        }

        [Test]
        public void LaunchAngleRemainsWithinConfiguredLimits()
        {
            FieldGoalKickResult result = FieldGoalKickCalculator.Calculate(CommandWithDrag(1f), rules);

            Assert.That(result.LaunchAngle, Is.InRange(rules.minimumFieldGoalLaunchAngle, rules.maximumFieldGoalLaunchAngle));
        }

        [Test]
        public void DownwardImpulsesCannotBeProduced()
        {
            rules.minimumFieldGoalUpwardForce = -10f;
            FieldGoalKickResult result = FieldGoalKickCalculator.Calculate(CommandWithDrag(1f), rules);

            Assert.That(result.UpwardImpulse, Is.GreaterThanOrEqualTo(0f));
            Assert.That(result.TotalImpulse.y, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void IdenticalInputProducesIdenticalResult()
        {
            FlickCommand command = CommandWithDrag(1.25f);

            FieldGoalKickResult first = FieldGoalKickCalculator.Calculate(command, rules);
            FieldGoalKickResult second = FieldGoalKickCalculator.Calculate(command, rules);

            Assert.That(second.TotalImpulse, Is.EqualTo(first.TotalImpulse));
            Assert.That(second.LaunchAngle, Is.EqualTo(first.LaunchAngle));
            Assert.That(second.NormalizedPower, Is.EqualTo(first.NormalizedPower));
        }

        private static FlickCommand CommandWithDrag(float dragDistance)
        {
            Vector3 start = Vector3.zero;
            Vector3 current = new(0f, 0f, -dragDistance);
            return new FlickCommand(true, start, current, current, Vector3.forward, 1f, dragDistance, 0.2f, 0.5f);
        }
    }
}
