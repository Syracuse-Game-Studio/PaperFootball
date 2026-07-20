using NUnit.Framework;
using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Roguelike.Modifiers;
using PaperFootball.Tabletop.Roguelike.Opponents;
using PaperFootball.Tabletop.Roguelike.Random;
using PaperFootball.Tabletop.Roguelike.Variance;
using PaperFootball.Tabletop.Rules;
using PaperFootball.Tabletop.Shots;
using UnityEngine;

namespace PaperFootball.Tabletop.Tests
{
    public class AirFlickShotTests
    {
        [Test]
        public void FlickCommandPreservesShotType()
        {
            FlickCommand command = Command(FootballShotType.AirFlickShot);

            Assert.That(command.ShotType, Is.EqualTo(FootballShotType.AirFlickShot));
            Assert.That(command.WithShotType(FootballShotType.FieldGoalKick).ShotType, Is.EqualTo(FootballShotType.FieldGoalKick));
        }

        [Test]
        public void FlatShotCommandHasNoIntentionalUpwardImpulse()
        {
            FlickCommand command = Command(FootballShotType.FlatTableShot);

            Assert.That(command.ShotType, Is.EqualTo(FootballShotType.FlatTableShot));
            Assert.That(command.Direction.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void AirFlickProducesPositiveUpwardImpulseWithinLaunchBounds()
        {
            AirFlickShotSettings settings = ScriptableObject.CreateInstance<AirFlickShotSettings>();
            try
            {
                AirFlickShotResult result = AirFlickShotCalculator.Calculate(
                    Command(FootballShotType.AirFlickShot),
                    Rules(),
                    settings,
                    null,
                    new DeterministicRunRandom(10),
                    Context(FootballShotType.AirFlickShot));

                Assert.IsTrue(result.IsValid);
                Assert.That(result.UpwardImpulse, Is.GreaterThan(0f));
                Assert.That(result.LaunchAngleDegrees, Is.InRange(settings.MinimumLaunchAngle, settings.MaximumLaunchAngle));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void AirFlickVarianceScalesAreBounded()
        {
            ShotVarianceTuning flat = new(true, 0.03f, 1.5f, 0.01f, false, "Stable");
            ShotVarianceTuning air = flat.Scaled(1.25f, 1.5f, 1.5f);

            Assert.That(air.ForceVariancePercent, Is.EqualTo(0.0375f).Within(0.0001f));
            Assert.That(air.DirectionVarianceDegrees, Is.EqualTo(2.25f).Within(0.0001f));
            Assert.That(air.ContactPointVarianceRadius, Is.EqualTo(0.015f).Within(0.0001f));
        }

        [Test]
        public void LandingVarianceIsReproducibleFromSameSeed()
        {
            AirFlickShotSettings settings = ScriptableObject.CreateInstance<AirFlickShotSettings>();
            try
            {
                LandingVarianceSample first = LandingVarianceSample.Generate(settings, new DeterministicRunRandom(22));
                LandingVarianceSample second = LandingVarianceSample.Generate(settings, new DeterministicRunRandom(22));
                LandingVarianceSample different = LandingVarianceSample.Generate(settings, new DeterministicRunRandom(23));

                Assert.That(first.TangentialImpulse, Is.EqualTo(second.TangentialImpulse).Within(0.0001f));
                Assert.That(first.YawImpulse, Is.EqualTo(second.YawImpulse).Within(0.0001f));
                Assert.That(Mathf.Abs(first.TangentialImpulse - different.TangentialImpulse), Is.GreaterThan(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void LandingImpulseIsCappedBySettings()
        {
            AirFlickShotSettings settings = ScriptableObject.CreateInstance<AirFlickShotSettings>();
            try
            {
                LandingVarianceSample sample = new(2f, 20f, 0f, 1f, 0f, 5);

                Vector3 impulse = AirFlickLandingController.CalculateLandingImpulse(
                    new Vector3(4f, -8f, 1f),
                    Vector3.up,
                    sample,
                    settings,
                    0.16f,
                    Vector3.forward,
                    out _,
                    out _);

                Assert.That(impulse.magnitude, Is.LessThanOrEqualTo(settings.MaximumLandingCorrectionImpulse + 0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void ShotExecutionContextRestrictsFieldGoalEligibility()
        {
            ShotExecutionContext flat = Context(FootballShotType.FlatTableShot);
            ShotExecutionContext air = Context(FootballShotType.AirFlickShot);
            ShotExecutionContext fieldGoal = ShotExecutionContext.FieldGoal(PaperFootballPlayer.PlayerOne, 1, 0, 2, 3);

            Assert.IsFalse(flat.CanScoreFieldGoal);
            Assert.IsFalse(air.CanScoreFieldGoal);
            Assert.IsTrue(fieldGoal.CanScoreFieldGoal);
            Assert.That(fieldGoal.ShotType, Is.EqualTo(FootballShotType.FieldGoalKick));
        }

        [Test]
        public void ShotModeSelectionRejectsFieldGoalAndResolvingPhases()
        {
            GameObject selectionObject = new("ShotSelection");
            try
            {
                ShotSelectionController selector = selectionObject.AddComponent<ShotSelectionController>();
                PaperFootballMatch match = new(Rules());
                match.TryBeginFlick();
                match.TryBeginResolving();
                match.ApplyResolution(FlickResolutionType.Touchdown);

                selector.ApplyMatchState(match, false, FlickInteractionState.WaitingForFlick);
                Assert.IsFalse(selector.TrySelectNormalShot(FootballShotType.AirFlickShot));

                match.ResetMatch();
                match.TryBeginFlick();
                selector.ApplyMatchState(match, false, FlickInteractionState.Resolving);
                Assert.IsFalse(selector.TrySelectNormalShot(FootballShotType.AirFlickShot));
            }
            finally
            {
                Object.DestroyImmediate(selectionObject);
            }
        }

        [Test]
        public void AiChoosesValidShotTypesAndPrefersAirWhenObstacleBlocksPath()
        {
            GameObject football = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                Collider collider = football.GetComponent<Collider>();
                OpponentProfile profile = ScriptableObject.CreateInstance<OpponentProfile>();
                profile.Configure("ai", "AI", 0.55f, 0f, 0f, OpponentContactPreference.Center, 0.95f, 0.65f, 0.5f, 0.5f, 1f, 0f);
                Bounds table = new(Vector3.zero, new Vector3(8f, 1f, 12f));
                Bounds blocker = new(Vector3.back * 1.5f, new Vector3(1f, 1f, 0.4f));

                OpponentDecision blocked = OpponentDecisionService.Decide(
                    new OpponentDecisionContext(profile, collider, table, Rules(), PaperFootballPlayer.PlayerTwo, 1, 0, new[] { blocker }),
                    new DeterministicRunRandom(5));
                OpponentDecision clear = OpponentDecisionService.Decide(
                    new OpponentDecisionContext(profile, collider, table, Rules(), PaperFootballPlayer.PlayerTwo, 1, 0),
                    new DeterministicRunRandom(5));

                Assert.IsTrue(blocked.IsValid);
                Assert.IsTrue(clear.IsValid);
                Assert.That(blocked.Command.ShotType, Is.EqualTo(FootballShotType.AirFlickShot));
                Assert.That(clear.Command.ShotType, Is.EqualTo(FootballShotType.FlatTableShot));

                Object.DestroyImmediate(profile);
            }
            finally
            {
                Object.DestroyImmediate(football);
            }
        }

        [Test]
        public void AirFlickModifierCompositionIsDeterministic()
        {
            FootballModifier[] modifiers =
            {
                new("air_forward", FootballModifierType.AirFlickForwardImpulseMultiplier, FootballModifierOperation.Multiply, 1.2f),
                new("air_landing", FootballModifierType.AirFlickLandingVarianceMultiplier, FootballModifierOperation.Multiply, 0.7f)
            };

            float forward = ModifierPipeline.Compose(1f, modifiers, FootballModifierType.AirFlickForwardImpulseMultiplier, 0.05f, 4f);
            float landing = ModifierPipeline.Compose(1f, modifiers, FootballModifierType.AirFlickLandingVarianceMultiplier, 0f, 4f);

            Assert.That(forward, Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(landing, Is.EqualTo(0.7f).Within(0.0001f));
        }

        private static FlickCommand Command(FootballShotType shotType)
        {
            return new FlickCommand(
                true,
                Vector3.zero,
                new Vector3(0f, 0f, -1f),
                new Vector3(0f, 0f, -1f),
                Vector3.forward,
                2f,
                1f,
                0.2f,
                0.5f,
                new Vector3(0.25f, 0.1f, -0.2f),
                shotType);
        }

        private static PaperFootballRuleSet Rules()
        {
            return new PaperFootballRuleSet
            {
                minimumDragDistance = 0.05f,
                maximumDragDistance = 2.5f,
                minimumFlickForce = 0.35f,
                maximumFlickForce = 4f
            };
        }

        private static ShotExecutionContext Context(FootballShotType shotType)
        {
            return ShotExecutionContext.Normal(shotType, PaperFootballPlayer.PlayerOne, 1, 0, 2, 3);
        }
    }
}
