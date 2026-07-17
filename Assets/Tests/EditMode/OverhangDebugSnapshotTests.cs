using NUnit.Framework;
using PaperFootball.Tabletop.Rules;
using PaperFootball.Tabletop.Scoring;
using UnityEngine;

namespace PaperFootball.Tabletop.Tests
{
    public class OverhangDebugSnapshotTests
    {
        private readonly Bounds tableBounds = new(Vector3.zero, new Vector3(8f, 0.25f, 12f));

        [Test]
        public void SnapshotRecordsPositiveOverhangSupportAndTouchdownDecision()
        {
            PaperFootballRuleSet rules = new()
            {
                requiredOverhangPercent = 0f,
                minimumSupportedPercent = 0.25f
            };
            Bounds footballBounds = new(new Vector3(0f, 0.2f, 5.705f), new Vector3(0.5f, 0.16f, 0.6f));

            OverhangDebugSnapshot snapshot = EdgeOverhangCalculator.CalculateSnapshot(
                tableBounds,
                footballBounds,
                PaperFootballPlayer.PlayerOne,
                rules,
                false,
                false);

            Assert.IsTrue(snapshot.HasPositiveOverhang);
            Assert.That(snapshot.OverhangDistance, Is.GreaterThan(0f));
            Assert.That(snapshot.SupportedPercent, Is.GreaterThanOrEqualTo(rules.minimumSupportedPercent));
            Assert.IsTrue(snapshot.IsSupported);
            Assert.IsTrue(snapshot.FinalTouchdownDecision);
        }

        [Test]
        public void SnapshotDoesNotAwardOrChangeScore()
        {
            PaperFootballMatch match = new(new PaperFootballRuleSet
            {
                touchdownPoints = 6,
                requiredOverhangPercent = 0f
            });

            EdgeOverhangCalculator.CalculateSnapshot(
                tableBounds,
                new Bounds(new Vector3(0f, 0.2f, 5.705f), new Vector3(0.5f, 0.16f, 0.6f)),
                PaperFootballPlayer.PlayerOne,
                new PaperFootballRuleSet(),
                false,
                false);

            Assert.That(match.PlayerOneScore, Is.EqualTo(0));
            Assert.That(match.PlayerTwoScore, Is.EqualTo(0));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.WaitingForFlick));
        }

        [Test]
        public void SnapshotUpdatesBetweenStoppedFlicks()
        {
            PaperFootballRuleSet rules = new()
            {
                requiredOverhangPercent = 0f,
                minimumSupportedPercent = 0.25f
            };

            OverhangDebugSnapshot positive = EdgeOverhangCalculator.CalculateSnapshot(
                tableBounds,
                new Bounds(new Vector3(0f, 0.2f, 5.705f), new Vector3(0.5f, 0.16f, 0.6f)),
                PaperFootballPlayer.PlayerOne,
                rules,
                false,
                false);

            OverhangDebugSnapshot edgeAligned = EdgeOverhangCalculator.CalculateSnapshot(
                tableBounds,
                new Bounds(new Vector3(0f, 0.2f, 5.7f), new Vector3(0.5f, 0.16f, 0.6f)),
                PaperFootballPlayer.PlayerOne,
                rules,
                false,
                true);

            Assert.IsTrue(positive.FinalTouchdownDecision);
            Assert.IsFalse(edgeAligned.HasPositiveOverhang);
            Assert.IsFalse(edgeAligned.FinalTouchdownDecision);
            Assert.IsTrue(edgeAligned.ScoringEventAlreadyProcessed);
        }
    }
}
