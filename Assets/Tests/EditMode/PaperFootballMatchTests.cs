using NUnit.Framework;
using PaperFootball.Tabletop.Rules;

namespace PaperFootball.Tabletop.Tests
{
    public class PaperFootballMatchTests
    {
        private PaperFootballRuleSet rules;

        [SetUp]
        public void SetUp()
        {
            rules = new PaperFootballRuleSet
            {
                touchdownPoints = 6,
                successfulKickPoints = 3,
                targetScore = 12
            };
        }

        [Test]
        public void StoppedNoScoreSwitchesTurn()
        {
            PaperFootballMatch match = CreateMovingMatch();

            match.TryBeginResolving();
            match.ApplyResolution(FlickResolutionType.StoppedNoScore);

            Assert.That(match.CurrentPlayer, Is.EqualTo(PaperFootballPlayer.PlayerTwo));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.WaitingForFlick));
        }

        [Test]
        public void TouchdownAwardsConfiguredPoints()
        {
            PaperFootballMatch match = CreateMovingMatch();

            match.TryBeginResolving();
            match.ApplyResolution(FlickResolutionType.Touchdown);

            Assert.That(match.PlayerOneScore, Is.EqualTo(6));
            Assert.That(match.PlayerTwoScore, Is.EqualTo(0));
            Assert.That(match.CurrentPlayer, Is.EqualTo(PaperFootballPlayer.PlayerOne));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.FieldGoalSetup));
        }

        [Test]
        public void DuplicateScorePreventionKeepsScoreStable()
        {
            PaperFootballMatch match = CreateMovingMatch();

            match.TryBeginResolving();
            match.ApplyResolution(FlickResolutionType.Touchdown);
            match.ApplyResolution(FlickResolutionType.Touchdown);

            Assert.That(match.PlayerOneScore, Is.EqualTo(6));
        }

        [Test]
        public void FieldGoalGoodAwardsConfiguredPointsAndSwitchesTurn()
        {
            PaperFootballMatch match = CreateFieldGoalSetupMatch();

            Assert.IsTrue(match.TryBeginFieldGoalAttempt());
            FlickResolution result = match.ApplyFieldGoalResult(true);

            Assert.That(result.Type, Is.EqualTo(FlickResolutionType.FieldGoalGood));
            Assert.That(match.PlayerOneScore, Is.EqualTo(9));
            Assert.That(match.CurrentPlayer, Is.EqualTo(PaperFootballPlayer.PlayerTwo));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.WaitingForFlick));
        }

        [Test]
        public void FieldGoalMissChangesPossessionWithoutScoring()
        {
            PaperFootballMatch match = CreateFieldGoalSetupMatch();

            Assert.IsTrue(match.TryBeginFieldGoalAttempt());
            FlickResolution result = match.ApplyFieldGoalResult(false);

            Assert.That(result.Type, Is.EqualTo(FlickResolutionType.FieldGoalMissed));
            Assert.That(match.PlayerOneScore, Is.EqualTo(6));
            Assert.That(match.CurrentPlayer, Is.EqualTo(PaperFootballPlayer.PlayerTwo));
        }

        [Test]
        public void DuplicateFieldGoalScorePreventionKeepsScoreStable()
        {
            PaperFootballMatch match = CreateFieldGoalSetupMatch();

            Assert.IsTrue(match.TryBeginFieldGoalAttempt());
            match.ApplyFieldGoalResult(true);
            match.ApplyFieldGoalResult(true);

            Assert.That(match.PlayerOneScore, Is.EqualTo(9));
        }

        [Test]
        public void FootballFellResolutionChangesPossession()
        {
            PaperFootballMatch match = CreateMovingMatch();

            match.TryBeginResolving();
            match.ApplyResolution(FlickResolutionType.FellFromTable);

            Assert.That(match.CurrentPlayer, Is.EqualTo(PaperFootballPlayer.PlayerTwo));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.WaitingForFlick));
        }

        [Test]
        public void TargetScoreCompletesMatch()
        {
            PaperFootballMatch match = new(new PaperFootballRuleSet
            {
                touchdownPoints = 6,
                targetScore = 6
            });

            match.TryBeginFlick();
            match.TryBeginResolving();
            match.ApplyResolution(FlickResolutionType.Touchdown);

            Assert.That(match.Phase, Is.EqualTo(MatchPhase.MatchComplete));
            Assert.That(match.Winner, Is.EqualTo(PaperFootballPlayer.PlayerOne));
        }

        [Test]
        public void MatchResetClearsScoresAndRestoresFirstPlayer()
        {
            PaperFootballMatch match = CreateMovingMatch();
            match.TryBeginResolving();
            match.ApplyResolution(FlickResolutionType.Touchdown);

            match.ResetMatch();

            Assert.That(match.PlayerOneScore, Is.EqualTo(0));
            Assert.That(match.PlayerTwoScore, Is.EqualTo(0));
            Assert.That(match.CurrentPlayer, Is.EqualTo(PaperFootballPlayer.PlayerOne));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.WaitingForFlick));
        }

        private PaperFootballMatch CreateMovingMatch()
        {
            PaperFootballMatch match = new(rules);
            Assert.IsTrue(match.TryBeginFlick());
            return match;
        }

        private PaperFootballMatch CreateFieldGoalSetupMatch()
        {
            PaperFootballMatch match = CreateMovingMatch();
            match.TryBeginResolving();
            match.ApplyResolution(FlickResolutionType.Touchdown);
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.FieldGoalSetup));
            return match;
        }
    }
}
