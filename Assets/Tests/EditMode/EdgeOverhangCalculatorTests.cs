using NUnit.Framework;
using PaperFootball.Tabletop.Rules;
using PaperFootball.Tabletop.Scoring;
using UnityEngine;

namespace PaperFootball.Tabletop.Tests
{
    public class EdgeOverhangCalculatorTests
    {
        private readonly Bounds tableBounds = new(Vector3.zero, new Vector3(8f, 0.25f, 12f));

        [Test]
        public void AnyPositiveOverhangAtOpponentEdgeCanScore()
        {
            PaperFootballRuleSet rules = new()
            {
                requiredOverhangPercent = 0f,
                minimumSupportedPercent = 0.25f
            };
            Bounds footballBounds = new(new Vector3(0f, 0.2f, 5.72f), new Vector3(0.5f, 0.16f, 0.6f));

            EdgeOverhangResult result = EdgeOverhangCalculator.Calculate(tableBounds, footballBounds, PaperFootballPlayer.PlayerOne, rules);

            Assert.IsTrue(result.IsTouchdown);
            Assert.That(result.OverhangDistance, Is.GreaterThan(0f));
            Assert.That(result.SupportedPercent, Is.GreaterThanOrEqualTo(0.25f));
        }

        [Test]
        public void ConfiguredOverhangThresholdCanRequireMoreThanAnyPositiveOverhang()
        {
            PaperFootballRuleSet rules = new()
            {
                requiredOverhangPercent = 0.5f,
                minimumSupportedPercent = 0.25f
            };
            Bounds footballBounds = new(new Vector3(0f, 0.2f, 5.9f), new Vector3(0.5f, 0.16f, 0.6f));

            EdgeOverhangResult result = EdgeOverhangCalculator.Calculate(tableBounds, footballBounds, PaperFootballPlayer.PlayerOne, rules);

            Assert.IsFalse(result.IsTouchdown);
        }

        [Test]
        public void UnsupportedFootballDoesNotScore()
        {
            PaperFootballRuleSet rules = new()
            {
                requiredOverhangPercent = 0.25f,
                minimumSupportedPercent = 0.25f
            };
            Bounds footballBounds = new(new Vector3(6f, 0.2f, 6.25f), new Vector3(0.5f, 0.16f, 0.6f));

            EdgeOverhangResult result = EdgeOverhangCalculator.Calculate(tableBounds, footballBounds, PaperFootballPlayer.PlayerOne, rules);

            Assert.IsFalse(result.IsTouchdown);
        }

        [Test]
        public void PlayerTwoScoresOnNegativeEdge()
        {
            PaperFootballRuleSet rules = new()
            {
                requiredOverhangPercent = 0.25f,
                minimumSupportedPercent = 0.25f
            };
            Bounds footballBounds = new(new Vector3(0f, 0.2f, -6.15f), new Vector3(0.5f, 0.16f, 0.6f));

            EdgeOverhangResult result = EdgeOverhangCalculator.Calculate(tableBounds, footballBounds, PaperFootballPlayer.PlayerTwo, rules);

            Assert.IsTrue(result.IsTouchdown);
            Assert.That(result.Edge, Is.EqualTo(ScoringEdge.NegativeZ));
        }
    }
}
