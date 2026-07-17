using System;

namespace PaperFootball.Tabletop.Rules
{
    public class MatchStateMachine
    {
        public MatchStateMachine(MatchPhase initialPhase = MatchPhase.WaitingForFlick)
        {
            CurrentPhase = initialPhase;
        }

        public MatchPhase CurrentPhase { get; private set; }

        public bool CanTransitionTo(MatchPhase nextPhase)
        {
            if (CurrentPhase == nextPhase)
            {
                return true;
            }

            return CurrentPhase switch
            {
                MatchPhase.WaitingForFlick => nextPhase == MatchPhase.FootballMoving || nextPhase == MatchPhase.MatchComplete,
                MatchPhase.FootballMoving => nextPhase == MatchPhase.ResolvingFlick,
                MatchPhase.ResolvingFlick => nextPhase == MatchPhase.TouchdownScored ||
                                             nextPhase == MatchPhase.ChangingPossession ||
                                             nextPhase == MatchPhase.MatchComplete,
                MatchPhase.TouchdownScored => nextPhase == MatchPhase.FieldGoalSetup ||
                                               nextPhase == MatchPhase.ChangingPossession ||
                                               nextPhase == MatchPhase.MatchComplete,
                MatchPhase.FieldGoalSetup => nextPhase == MatchPhase.FieldGoalAttempt ||
                                             nextPhase == MatchPhase.ChangingPossession ||
                                             nextPhase == MatchPhase.MatchComplete,
                MatchPhase.FieldGoalAttempt => nextPhase == MatchPhase.ChangingPossession ||
                                               nextPhase == MatchPhase.MatchComplete,
                MatchPhase.ChangingPossession => nextPhase == MatchPhase.WaitingForFlick || nextPhase == MatchPhase.MatchComplete,
                MatchPhase.MatchComplete => false,
                _ => false
            };
        }

        public bool TryTransitionTo(MatchPhase nextPhase)
        {
            if (!CanTransitionTo(nextPhase))
            {
                return false;
            }

            CurrentPhase = nextPhase;
            return true;
        }

        public void TransitionTo(MatchPhase nextPhase)
        {
            if (!TryTransitionTo(nextPhase))
            {
                throw new InvalidOperationException($"Cannot transition from {CurrentPhase} to {nextPhase}.");
            }
        }

        public void Reset(MatchPhase phase = MatchPhase.WaitingForFlick)
        {
            CurrentPhase = phase;
        }
    }
}
