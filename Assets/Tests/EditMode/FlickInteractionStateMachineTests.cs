using NUnit.Framework;
using PaperFootball.Tabletop.Input;

namespace PaperFootball.Tabletop.Tests
{
    public class FlickInteractionStateMachineTests
    {
        [Test]
        public void AllowsExpectedTwoStageFlow()
        {
            FlickInteractionStateMachine stateMachine = new();

            Assert.IsTrue(stateMachine.TryTransitionTo(FlickInteractionState.WaitingForContact));
            Assert.IsTrue(stateMachine.TryTransitionTo(FlickInteractionState.SelectingContact));
            Assert.IsTrue(stateMachine.TryTransitionTo(FlickInteractionState.WaitingForFlick));
            Assert.IsTrue(stateMachine.TryTransitionTo(FlickInteractionState.SelectingFlick));
            Assert.IsTrue(stateMachine.TryTransitionTo(FlickInteractionState.Resolving));
            Assert.IsTrue(stateMachine.TryTransitionTo(FlickInteractionState.WaitingForContact));
        }

        [Test]
        public void RejectsInvalidTransitionFromContactSelectionToFlickSelection()
        {
            FlickInteractionStateMachine stateMachine = new(FlickInteractionState.SelectingContact);

            Assert.IsFalse(stateMachine.TryTransitionTo(FlickInteractionState.SelectingFlick));
            Assert.That(stateMachine.CurrentState, Is.EqualTo(FlickInteractionState.SelectingContact));
        }

        [Test]
        public void MatchCompleteStyleDisabledStateDoesNotJumpIntoFlickDrag()
        {
            FlickInteractionStateMachine stateMachine = new(FlickInteractionState.Disabled);

            Assert.IsFalse(stateMachine.TryTransitionTo(FlickInteractionState.SelectingFlick));
            Assert.That(stateMachine.CurrentState, Is.EqualTo(FlickInteractionState.Disabled));
        }
    }
}
