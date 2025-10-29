using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaperFootball.Game
{
    /// <summary>
    /// Manages scoring, win conditions, and game statistics.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        [Header("Scoring Settings")]
        [SerializeField] private int touchdownPoints = 6;
        [SerializeField] private int winningScore = 6;

        // Scores
        public int Player1Score { get; private set; }
        public int Player2Score { get; private set; }

        // Win tracking
        public TurnManager.Player? Winner { get; private set; }
        public bool GameOver { get; private set; }

        // Statistics
        public int TotalTouchdowns { get; private set; }
        private readonly List<ScoringEvent> scoringHistory = new();

        // Events
        public event Action<TurnManager.Player, int> OnScoreChanged;
        public event Action<TurnManager.Player, Vector2Int> OnTouchdown;
        public event Action<TurnManager.Player, int> OnGameWon;
        public event Action<TurnManager.Player> OnDeadEnd;

        public static ScoreManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeScores();
        }

        /// <summary>
        /// Initializes the scoring system
        /// </summary>
        private void InitializeScores()
        {
            Player1Score = 0;
            Player2Score = 0;
            Winner = null;
            GameOver = false;
            TotalTouchdowns = 0;
            scoringHistory.Clear();

            Debug.Log("Score system initialized!");
        }

        /// <summary>
        /// Records a touchdown for the specified player
        /// </summary>
        public void RecordTouchdown(TurnManager.Player player, Vector2Int position)
        {
            if (GameOver)
            {
                Debug.LogWarning("Game is already over!");
                return;
            }

            // Add points
            if (player == TurnManager.Player.Player1)
            {
                Player1Score += touchdownPoints;
            }
            else
            {
                Player2Score += touchdownPoints;
            }

            TotalTouchdowns++;

            // Record in history
            ScoringEvent scoringEvent = new()
            {
                player = player,
                position = position,
                timestamp = Time.time,
                turnNumber = (TurnManager.Instance != null) ? TurnManager.Instance.TurnNumber : 0
            };
            scoringHistory.Add(scoringEvent);

            Debug.Log($"{player} scored a touchdown at {position}! Score: P1={Player1Score}, P2={Player2Score}");

            // Trigger events
            OnTouchdown?.Invoke(player, position);
            OnScoreChanged?.Invoke(player, GetPlayerScore(player));

            // Check for win condition
            CheckWinCondition();
        }

        /// <summary>
        /// Records a dead-end (no valid moves) - opponent wins
        /// </summary>
        public void RecordDeadEnd(TurnManager.Player stuckPlayer)
        {
            if (GameOver) return;

            Debug.Log($"{stuckPlayer} has no valid moves! Dead end!");
            OnDeadEnd?.Invoke(stuckPlayer);

            // The opponent wins
            TurnManager.Player opponent = TurnManager.Instance.GetOpponent(stuckPlayer);
            DeclareWinner(opponent, WinReason.DeadEnd);
        }

        /// <summary>
        /// Checks if a player has won
        /// </summary>
        private void CheckWinCondition()
        {
            if (Player1Score >= winningScore)
            {
                DeclareWinner(TurnManager.Player.Player1, WinReason.ReachedScore);
            }
            else if (Player2Score >= winningScore)
            {
                DeclareWinner(TurnManager.Player.Player2, WinReason.ReachedScore);
            }
        }

        /// <summary>
        /// Declares a winner and ends the game
        /// </summary>
        private void DeclareWinner(TurnManager.Player winner, WinReason reason)
        {
            Winner = winner;
            GameOver = true;

            int finalScore = GetPlayerScore(winner);
            Debug.Log($"{winner} WINS! Final Score: P1={Player1Score}, P2={Player2Score}. Reason: {reason}");

            OnGameWon?.Invoke(winner, finalScore);
        }

        /// <summary>
        /// Gets the score for a specific player
        /// </summary>
        public int GetPlayerScore(TurnManager.Player player)
        {
            return player == TurnManager.Player.Player1 ? Player1Score : Player2Score;
        }

        /// <summary>
        /// Gets the score difference (positive = Player1 ahead, negative = Player2 ahead)
        /// </summary>
        public int GetScoreDifference()
        {
            return Player1Score - Player2Score;
        }

        /// <summary>
        /// Gets the leading player (null if tied)
        /// </summary>
        public TurnManager.Player? GetLeadingPlayer()
        {
            if (Player1Score > Player2Score)
                return TurnManager.Player.Player1;
            else if (Player2Score > Player1Score)
                return TurnManager.Player.Player2;
            else
                return null; // Tied
        }

        /// <summary>
        /// Gets the scoring history
        /// </summary>
        public List<ScoringEvent> GetScoringHistory()
        {
            return new List<ScoringEvent>(scoringHistory);
        }

        /// <summary>
        /// Resets all scores and game state
        /// </summary>
        public void ResetScores()
        {
            InitializeScores();
            Debug.Log("Scores reset!");
        }

        /// <summary>
        /// Gets a formatted score string
        /// </summary>
        public string GetScoreString()
        {
            return $"Player 1: {Player1Score} | Player 2: {Player2Score}";
        }
    }

    /// <summary>
    /// Represents a scoring event in the game
    /// </summary>
    [System.Serializable]
    public struct ScoringEvent
    {
        public TurnManager.Player player;
        public Vector2Int position;
        public float timestamp;
        public int turnNumber;
    }

    /// <summary>
    /// Reasons for winning the game
    /// </summary>
    public enum WinReason
    {
        ReachedScore,   // Reached winning score through touchdowns
        DeadEnd,        // Opponent had no valid moves
        Forfeit         // Opponent forfeited (for future use)
    }
}