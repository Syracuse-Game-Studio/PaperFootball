using NUnit.Framework;
using PaperFootball.Tabletop.Rules;

namespace PaperFootball.Tabletop.Tests
{
    public class MatchStateMachineTests
    {
        [Test]
        public void LegalMatchStateTransitionsSucceed()
        {
            MatchStateMachine stateMachine = new();

            Assert.IsTrue(stateMachine.TryTransitionTo(MatchPhase.FootballMoving));
            Assert.IsTrue(stateMachine.TryTransitionTo(MatchPhase.ResolvingFlick));
            Assert.IsTrue(stateMachine.TryTransitionTo(MatchPhase.TouchdownScored));
            Assert.IsTrue(stateMachine.TryTransitionTo(MatchPhase.FieldGoalSetup));
            Assert.IsTrue(stateMachine.TryTransitionTo(MatchPhase.FieldGoalAttempt));
            Assert.IsTrue(stateMachine.TryTransitionTo(MatchPhase.ChangingPossession));
            Assert.IsTrue(stateMachine.TryTransitionTo(MatchPhase.WaitingForFlick));
            Assert.That(stateMachine.CurrentPhase, Is.EqualTo(MatchPhase.WaitingForFlick));
        }

        [Test]
        public void IllegalMatchStateTransitionsFail()
        {
            MatchStateMachine stateMachine = new();

            Assert.IsFalse(stateMachine.TryTransitionTo(MatchPhase.ResolvingFlick));
            Assert.That(stateMachine.CurrentPhase, Is.EqualTo(MatchPhase.WaitingForFlick));
        }
    }
}
