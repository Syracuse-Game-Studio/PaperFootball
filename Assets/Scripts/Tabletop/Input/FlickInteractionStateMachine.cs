namespace PaperFootball.Tabletop.Input
{
    public class FlickInteractionStateMachine
    {
        public FlickInteractionStateMachine(FlickInteractionState initialState = FlickInteractionState.Disabled)
        {
            CurrentState = initialState;
        }

        public FlickInteractionState CurrentState { get; private set; }

        public bool CanTransitionTo(FlickInteractionState nextState)
        {
            if (CurrentState == nextState)
            {
                return true;
            }

            return CurrentState switch
            {
                FlickInteractionState.Disabled => nextState == FlickInteractionState.WaitingForContact ||
                                                  nextState == FlickInteractionState.Resolving,
                FlickInteractionState.WaitingForContact => nextState == FlickInteractionState.SelectingContact ||
                                                           nextState == FlickInteractionState.WaitingForFlick ||
                                                           nextState == FlickInteractionState.Disabled ||
                                                           nextState == FlickInteractionState.Resolving,
                FlickInteractionState.SelectingContact => nextState == FlickInteractionState.WaitingForContact ||
                                                          nextState == FlickInteractionState.WaitingForFlick ||
                                                          nextState == FlickInteractionState.Disabled ||
                                                          nextState == FlickInteractionState.Resolving,
                FlickInteractionState.WaitingForFlick => nextState == FlickInteractionState.SelectingFlick ||
                                                         nextState == FlickInteractionState.WaitingForContact ||
                                                         nextState == FlickInteractionState.Disabled ||
                                                         nextState == FlickInteractionState.Resolving,
                FlickInteractionState.SelectingFlick => nextState == FlickInteractionState.WaitingForFlick ||
                                                        nextState == FlickInteractionState.Resolving ||
                                                        nextState == FlickInteractionState.Disabled,
                FlickInteractionState.Resolving => nextState == FlickInteractionState.WaitingForContact ||
                                                   nextState == FlickInteractionState.Disabled,
                _ => false
            };
        }

        public bool TryTransitionTo(FlickInteractionState nextState)
        {
            if (!CanTransitionTo(nextState))
            {
                return false;
            }

            CurrentState = nextState;
            return true;
        }

        public void Reset(FlickInteractionState state = FlickInteractionState.Disabled)
        {
            CurrentState = state;
        }
    }
}
