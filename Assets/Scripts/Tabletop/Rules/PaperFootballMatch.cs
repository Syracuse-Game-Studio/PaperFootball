using System;

namespace PaperFootball.Tabletop.Rules
{
    public class PaperFootballMatch
    {
        private readonly PaperFootballRuleSet rules;
        private readonly MatchStateMachine stateMachine = new();
        private bool currentFlickResolved;
        private bool currentFieldGoalResolved;

        public PaperFootballMatch(PaperFootballRuleSet ruleSet)
        {
            rules = ruleSet != null ? ruleSet.Clone() : new PaperFootballRuleSet();
            rules.Sanitize();
            ResetMatch();
        }

        public MatchPhase Phase => stateMachine.CurrentPhase;
        public PaperFootballPlayer CurrentPlayer { get; private set; }
        public PaperFootballPlayer? Winner { get; private set; }
        public int PlayerOneScore { get; private set; }
        public int PlayerTwoScore { get; private set; }
        public int PossessionNumber { get; private set; }
        public string LastResult { get; private set; }
        public bool CurrentFlickResolved => currentFlickResolved;
        public bool CurrentFieldGoalResolved => currentFieldGoalResolved;
        public bool IsFieldGoalMode => Phase == MatchPhase.FieldGoalSetup || Phase == MatchPhase.FieldGoalAttempt;

        public event Action StateChanged;

        public void ResetMatch()
        {
            PlayerOneScore = 0;
            PlayerTwoScore = 0;
            CurrentPlayer = PaperFootballPlayer.PlayerOne;
            Winner = null;
            PossessionNumber = 1;
            currentFlickResolved = false;
            currentFieldGoalResolved = false;
            LastResult = "New match";
            stateMachine.Reset(MatchPhase.WaitingForFlick);
            StateChanged?.Invoke();
        }

        public bool TryBeginFlick()
        {
            if (!stateMachine.TryTransitionTo(MatchPhase.FootballMoving))
            {
                return false;
            }

            currentFlickResolved = false;
            LastResult = $"{GetPlayerName(CurrentPlayer)} flicked";
            StateChanged?.Invoke();
            return true;
        }

        public bool TryBeginFieldGoalAttempt()
        {
            if (!stateMachine.TryTransitionTo(MatchPhase.FieldGoalAttempt))
            {
                return false;
            }

            currentFieldGoalResolved = false;
            LastResult = $"{GetPlayerName(CurrentPlayer)} field goal attempt";
            StateChanged?.Invoke();
            return true;
        }

        public bool TryBeginResolving()
        {
            bool transitioned = stateMachine.TryTransitionTo(MatchPhase.ResolvingFlick);
            if (transitioned)
            {
                StateChanged?.Invoke();
            }

            return transitioned;
        }

        public FlickResolution ApplyResolution(FlickResolutionType resolutionType)
        {
            if (currentFlickResolved)
            {
                return new FlickResolution(FlickResolutionType.None, "Flick already resolved");
            }

            if (stateMachine.CurrentPhase == MatchPhase.FootballMoving)
            {
                stateMachine.TransitionTo(MatchPhase.ResolvingFlick);
            }

            if (stateMachine.CurrentPhase != MatchPhase.ResolvingFlick)
            {
                return new FlickResolution(FlickResolutionType.InvalidState, $"Cannot resolve while {stateMachine.CurrentPhase}");
            }

            currentFlickResolved = true;

            switch (resolutionType)
            {
                case FlickResolutionType.Touchdown:
                    return ScoreTouchdown();
                case FlickResolutionType.FellFromTable:
                    LastResult = $"{GetPlayerName(CurrentPlayer)} fell from the table";
                    ChangePossession();
                    return new FlickResolution(FlickResolutionType.FellFromTable, LastResult);
                case FlickResolutionType.StoppedNoScore:
                    LastResult = $"{GetPlayerName(CurrentPlayer)} stopped without scoring";
                    ChangePossession();
                    return new FlickResolution(FlickResolutionType.StoppedNoScore, LastResult);
                default:
                    LastResult = "Invalid flick result";
                    ChangePossession();
                    return new FlickResolution(FlickResolutionType.InvalidState, LastResult);
            }
        }

        public FlickResolution ApplyFieldGoalResult(bool successful)
        {
            if (currentFieldGoalResolved)
            {
                return new FlickResolution(FlickResolutionType.None, "Field goal already resolved");
            }

            if (stateMachine.CurrentPhase == MatchPhase.FieldGoalSetup)
            {
                stateMachine.TransitionTo(MatchPhase.FieldGoalAttempt);
            }

            if (stateMachine.CurrentPhase != MatchPhase.FieldGoalAttempt)
            {
                return new FlickResolution(FlickResolutionType.InvalidState, $"Cannot resolve field goal while {stateMachine.CurrentPhase}");
            }

            currentFieldGoalResolved = true;

            if (successful)
            {
                AddScore(CurrentPlayer, rules.successfulKickPoints);
                LastResult = $"{GetPlayerName(CurrentPlayer)} field goal good";

                if (HasWon(CurrentPlayer) || HasReachedPossessionLimit())
                {
                    Winner = CurrentPlayer;
                    stateMachine.TransitionTo(MatchPhase.MatchComplete);
                    LastResult = $"{GetPlayerName(CurrentPlayer)} wins";
                    StateChanged?.Invoke();
                    return new FlickResolution(FlickResolutionType.FieldGoalGood, LastResult);
                }
            }
            else
            {
                LastResult = $"{GetPlayerName(CurrentPlayer)} field goal missed";
            }

            ChangePossession();
            return new FlickResolution(successful ? FlickResolutionType.FieldGoalGood : FlickResolutionType.FieldGoalMissed, LastResult);
        }

        public void ResetCurrentBall()
        {
            currentFlickResolved = false;
            currentFieldGoalResolved = false;
            if (stateMachine.CurrentPhase != MatchPhase.MatchComplete)
            {
                bool shouldRemainFieldGoal = stateMachine.CurrentPhase == MatchPhase.TouchdownScored ||
                                             stateMachine.CurrentPhase == MatchPhase.FieldGoalSetup ||
                                             stateMachine.CurrentPhase == MatchPhase.FieldGoalAttempt;
                stateMachine.Reset(shouldRemainFieldGoal ? MatchPhase.FieldGoalSetup : MatchPhase.WaitingForFlick);
            }

            LastResult = "Ball reset";
            StateChanged?.Invoke();
        }

        public int GetScore(PaperFootballPlayer player)
        {
            return player == PaperFootballPlayer.PlayerOne ? PlayerOneScore : PlayerTwoScore;
        }

        public void AddBonusScore(PaperFootballPlayer player, int points, string reason)
        {
            if (points <= 0 || stateMachine.CurrentPhase == MatchPhase.MatchComplete)
            {
                return;
            }

            AddScore(player, points);
            LastResult = string.IsNullOrWhiteSpace(reason)
                ? $"{GetPlayerName(player)} bonus score"
                : reason;

            if (HasWon(player) || HasReachedPossessionLimit())
            {
                Winner = player;
                stateMachine.TransitionTo(MatchPhase.MatchComplete);
                LastResult = $"{GetPlayerName(player)} wins";
            }

            StateChanged?.Invoke();
        }

        public static PaperFootballPlayer OpponentOf(PaperFootballPlayer player)
        {
            return player == PaperFootballPlayer.PlayerOne ? PaperFootballPlayer.PlayerTwo : PaperFootballPlayer.PlayerOne;
        }

        public static string GetPlayerName(PaperFootballPlayer player)
        {
            return player == PaperFootballPlayer.PlayerOne ? "Player One" : "Player Two";
        }

        private FlickResolution ScoreTouchdown()
        {
            AddScore(CurrentPlayer, rules.touchdownPoints);

            LastResult = $"{GetPlayerName(CurrentPlayer)} touchdown";
            currentFieldGoalResolved = false;

            if (HasWon(CurrentPlayer) || HasReachedPossessionLimit())
            {
                Winner = CurrentPlayer;
                stateMachine.TransitionTo(MatchPhase.MatchComplete);
                LastResult = $"{GetPlayerName(CurrentPlayer)} wins";
            }
            else
            {
                stateMachine.TransitionTo(MatchPhase.TouchdownScored);
                stateMachine.TransitionTo(MatchPhase.FieldGoalSetup);
            }

            StateChanged?.Invoke();
            return new FlickResolution(FlickResolutionType.Touchdown, LastResult);
        }

        private void ChangePossession()
        {
            if (stateMachine.CurrentPhase == MatchPhase.MatchComplete)
            {
                StateChanged?.Invoke();
                return;
            }

            if (stateMachine.CurrentPhase == MatchPhase.TouchdownScored)
            {
                stateMachine.TransitionTo(MatchPhase.ChangingPossession);
            }
            else if (stateMachine.CurrentPhase == MatchPhase.ResolvingFlick)
            {
                stateMachine.TransitionTo(MatchPhase.ChangingPossession);
            }
            else if (stateMachine.CurrentPhase == MatchPhase.FieldGoalSetup)
            {
                stateMachine.TransitionTo(MatchPhase.ChangingPossession);
            }
            else if (stateMachine.CurrentPhase == MatchPhase.FieldGoalAttempt)
            {
                stateMachine.TransitionTo(MatchPhase.ChangingPossession);
            }

            CurrentPlayer = OpponentOf(CurrentPlayer);
            PossessionNumber++;

            if (HasReachedPossessionLimit())
            {
                Winner = PlayerOneScore == PlayerTwoScore
                    ? null
                    : (PlayerOneScore > PlayerTwoScore ? PaperFootballPlayer.PlayerOne : PaperFootballPlayer.PlayerTwo);
                stateMachine.TransitionTo(MatchPhase.MatchComplete);
            }
            else
            {
                stateMachine.TransitionTo(MatchPhase.WaitingForFlick);
            }

            StateChanged?.Invoke();
        }

        private bool HasWon(PaperFootballPlayer player)
        {
            return GetScore(player) >= rules.targetScore;
        }

        private void AddScore(PaperFootballPlayer player, int points)
        {
            if (player == PaperFootballPlayer.PlayerOne)
            {
                PlayerOneScore += points;
            }
            else
            {
                PlayerTwoScore += points;
            }
        }

        private bool HasReachedPossessionLimit()
        {
            return rules.maximumPossessions > 0 && PossessionNumber >= rules.maximumPossessions;
        }
    }
}
