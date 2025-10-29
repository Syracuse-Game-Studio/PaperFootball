using System;
using UnityEngine;

namespace PaperFootball.Game
{
    /// <summary>
    /// Manages player turns and turn-based game flow.
    /// Handles turn switching, continuous turn rules, and turn history.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public enum Player
        {
            Player1 = 1,
            Player2 = 2
        }

        [Header("Turn Settings")]
        [SerializeField] private Player startingPlayer = Player.Player1;

        // Current player
        public Player CurrentPlayer { get; private set; }

        // Turn tracking
        public int TurnNumber { get; private set; }
        public int Player1Moves { get; private set; }
        public int Player2Moves { get; private set; }

        // Continuous turn tracking
        private bool earnedExtraTurn = false;

        // Events
        public event Action<Player> OnTurnChanged;
        public event Action<Player, int> OnMoveCompleted;
        public event Action<Player> OnExtraTurnEarned;

        public static TurnManager Instance { get; private set; }

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
            InitializeTurns();
        }

        /// <summary>
        /// Initializes the turn system
        /// </summary>
        private void InitializeTurns()
        {
            CurrentPlayer = startingPlayer;
            TurnNumber = 1;
            Player1Moves = 0;
            Player2Moves = 0;
            earnedExtraTurn = false;

            Debug.Log($"Turn system initialized. {CurrentPlayer} goes first!");
            OnTurnChanged?.Invoke(CurrentPlayer);
        }

        /// <summary>
        /// Processes a completed move and determines if turn continues
        /// </summary>
        public void ProcessMove(bool movedToNewNode)
        {
            // Increment move count
            if (CurrentPlayer == Player.Player1)
            {
                Player1Moves++;
            }
            else
            {
                Player2Moves++;
            }

            OnMoveCompleted?.Invoke(CurrentPlayer, GetCurrentPlayerMoveCount());

            // Check if player earned an extra turn (moved to unvisited node)
            if (movedToNewNode)
            {
                earnedExtraTurn = true;
                OnExtraTurnEarned?.Invoke(CurrentPlayer);
                Debug.Log($"{CurrentPlayer} moved to a new node - extra turn!");
            }
            else
            {
                earnedExtraTurn = false;
            }
        }

        /// <summary>
        /// Ends the current turn and switches to the next player
        /// </summary>
        public void EndTurn()
        {
            // If player earned an extra turn, don't switch players
            if (earnedExtraTurn)
            {
                earnedExtraTurn = false;
                Debug.Log($"{CurrentPlayer} continues their turn!");
                return;
            }

            // Switch players
            SwitchPlayer();
        }

        /// <summary>
        /// Forces a turn switch (used for dead-ends or special cases)
        /// </summary>
        public void ForceTurnSwitch()
        {
            earnedExtraTurn = false;
            SwitchPlayer();
        }

        /// <summary>
        /// Switches to the other player
        /// </summary>
        private void SwitchPlayer()
        {
            CurrentPlayer = (CurrentPlayer == Player.Player1) ? Player.Player2 : Player.Player1;
            TurnNumber++;

            Debug.Log($"Turn {TurnNumber}: {CurrentPlayer}'s turn");
            OnTurnChanged?.Invoke(CurrentPlayer);
        }

        /// <summary>
        /// Gets the move count for the current player
        /// </summary>
        public int GetCurrentPlayerMoveCount()
        {
            return CurrentPlayer == Player.Player1 ? Player1Moves : Player2Moves;
        }

        /// <summary>
        /// Gets the move count for a specific player
        /// </summary>
        public int GetPlayerMoveCount(Player player)
        {
            return player == Player.Player1 ? Player1Moves : Player2Moves;
        }

        /// <summary>
        /// Resets the turn system
        /// </summary>
        public void ResetTurns()
        {
            InitializeTurns();
            Debug.Log("Turn system reset!");
        }

        /// <summary>
        /// Gets the opponent of the specified player
        /// </summary>
        public Player GetOpponent(Player player)
        {
            return player == Player.Player1 ? Player.Player2 : Player.Player1;
        }

        /// <summary>
        /// Checks if the current player has earned an extra turn
        /// </summary>
        public bool HasExtraTurn()
        {
            return earnedExtraTurn;
        }

        /// <summary>
        /// Gets a formatted string of the current player
        /// </summary>
        public string GetCurrentPlayerName()
        {
            return CurrentPlayer == Player.Player1 ? "Player 1" : "Player 2";
        }

        /// <summary>
        /// Gets the opponent's name
        /// </summary>
        public string GetOpponentName()
        {
            return CurrentPlayer == Player.Player1 ? "Player 2" : "Player 1";
        }
    }
}