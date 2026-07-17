using NUnit.Framework;
using PaperFootball.Tabletop.FieldGoals;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Tests
{
    public class TrajectoryPredictionServiceTests
    {
        [Test]
        public void FirstPointBeginsAtLaunchPosition()
        {
            Vector3[] points = new Vector3[8];
            Vector3 start = new(1f, 2f, 3f);

            int count = TrajectoryPredictionService.Predict(start, new Vector3(1f, 2f, 0f), 1f, Rules(), points);

            Assert.That(count, Is.GreaterThan(1));
            Assert.That(points[0], Is.EqualTo(start));
        }

        [Test]
        public void PredictedPathRisesWithPositiveUpwardForceThenGravityLowersIt()
        {
            Vector3[] points = new Vector3[32];
            PaperFootballRuleSet rules = Rules();

            int count = TrajectoryPredictionService.Predict(Vector3.zero, new Vector3(0f, 7f, 3f), 1f, rules, points);

            Assert.That(points[1].y, Is.GreaterThan(points[0].y));
            Assert.That(points[count - 1].y, Is.LessThan(points[count / 2].y));
        }

        [Test]
        public void PointCountAndDurationRespectConfiguration()
        {
            Vector3[] points = new Vector3[64];
            PaperFootballRuleSet rules = Rules();
            rules.trajectoryPointCount = 10;
            rules.trajectoryTimeStep = 0.1f;
            rules.maximumTrajectoryPreviewTime = 0.35f;

            int count = TrajectoryPredictionService.Predict(Vector3.zero, new Vector3(1f, 4f, 0f), 1f, rules, points);

            Assert.That(count, Is.EqualTo(5));
        }

        [Test]
        public void PreviewAndActualKickUseSameImpulseResult()
        {
            FieldGoalKickResult result = new(
                true,
                Vector3.forward,
                4f,
                3f,
                35f,
                0.5f,
                1f,
                new Vector3(0f, 3f, 4f));
            Vector3[] points = new Vector3[8];

            TrajectoryPredictionService.Predict(Vector3.zero, result.TotalImpulse, 1f, Rules(), points);

            Assert.That(points[1].z, Is.GreaterThan(0f));
            Assert.That(points[1].y, Is.GreaterThan(0f));
        }

        private static PaperFootballRuleSet Rules()
        {
            return new PaperFootballRuleSet
            {
                trajectoryPointCount = 12,
                trajectoryTimeStep = 0.1f,
                maximumTrajectoryPreviewTime = 1f
            };
        }
    }
}
