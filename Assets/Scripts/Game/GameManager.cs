using PaperFootball.Ball;
using PaperFootball.Grid;
using PaperFootball.Input;
using System.Collections.Generic;
using UnityEngine;

namespace PaperFootball.Game
{
    /// <summary>
    /// Main game controller that coordinates all game systems.
    /// Handles game flow, input processing, and rule enforcement.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private GridVisualizer gridVisualizer;
        [SerializeField] private BallToken ballToken;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private MovementValidator movementValidator;
        [SerializeField] private InputManager inputManager;

        [Header("Game Settings")]
        [SerializeField] private bool autoStartGame = true;
        [SerializeField] private float moveDelay = 0.3f;

        // Game state
        public enum GameState
        {
            Initializing,
            WaitingForInput,
            ProcessingMove,
            GameOver
        }

        public GameState CurrentState { get; private set; }
        private List<GridNode> currentValidMoves;

        public static GameManager Instance { get; private set; }

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
            InitializeGame();

            if (autoStartGame)
            {
                StartGame();
            }
        }

        /// <summary>
        /// Initializes all game systems
        /// </summary>
        private void InitializeGame()
        {
            CurrentState = GameState.Initializing;

            // Find references if not set
            if (gridManager == null) gridManager = GridManager.Instance;
            if (gridVisualizer == null) gridVisualizer = Object.FindFirstObjectByType<GridVisualizer>();
            if (ballToken == null) ballToken = Object.FindFirstObjectByType<BallToken>();
            if (turnManager == null) turnManager = TurnManager.Instance;
            if (scoreManager == null) scoreManager = ScoreManager.Instance;
            if (movementValidator == null) movementValidator = MovementValidator.Instance;
            if (inputManager == null) inputManager = InputManager.Instance;

            // Subscribe to turn manager events
            if (turnManager != null)
            {
                turnManager.OnTurnChanged += OnTurnChanged;
                turnManager.OnMoveCompleted += OnMoveCompleted;
            }

            // Subscribe to score manager events
            if (scoreManager != null)
            {
                scoreManager.OnTouchdown += OnTouchdown;
                scoreManager.OnGameWon += OnGameWon;
                scoreManager.OnDeadEnd += OnDeadEnd;
            }

            // Subscribe to input manager events
            if (inputManager != null)
            {
                inputManager.OnGridPositionClicked += OnGridPositionClicked;
            }

            Debug.Log("Game Manager initialized!");
        }

        /// <summary>
        /// Starts a new game
        /// </summary>
        public void StartGame()
        {
            Debug.Log("Starting new game of Paper Football!");

            // Reset all systems
            if (gridManager != null) gridManager.ResetGrid();
            if (ballToken != null) ballToken.ResetToStart();
            if (turnManager != null) turnManager.ResetTurns();
            if (scoreManager != null) scoreManager.ResetScores();

            // Mark starting position as visited
            if (gridManager != null && ballToken != null)
            {
                GridNode startNode = gridManager.GetNode(ballToken.CurrentGridPosition);
                startNode?.Visit();
            }

            // Enable input
            if (inputManager != null)
            {
                inputManager.EnableInput();
            }

            CurrentState = GameState.WaitingForInput;
            UpdateValidMoves();
        }

        /// <summary>
        /// Updates the list of valid moves for the current position
        /// </summary>
        private void UpdateValidMoves()
        {
            if (ballToken == null || movementValidator == null) return;

            currentValidMoves = movementValidator.GetValidMoves(ballToken.CurrentGridPosition);

            // Highlight valid moves
            if (gridVisualizer != null)
            {
                gridVisualizer.HighlightValidMoves(currentValidMoves);
            }

            // Check for dead end
            if (currentValidMoves.Count == 0)
            {
                HandleDeadEnd();
            }

            Debug.Log($"Valid moves available: {currentValidMoves.Count}");
        }

        /// <summary>
        /// Handles input when a grid position is clicked
        /// </summary>
        private void OnGridPositionClicked(Vector2Int gridPosition)
        {
            if (CurrentState != GameState.WaitingForInput) return;

            Debug.Log($"Grid position clicked: {gridPosition}");
            AttemptMove(gridPosition);
        }

        /// <summary>
        /// Attempts to move the ball to a target position
        /// </summary>
        public void AttemptMove(Vector2Int targetPosition)
        {
            if (CurrentState != GameState.WaitingForInput)
            {
                Debug.LogWarning("Not ready for input!");
                return;
            }

            if (ballToken == null || movementValidator == null) return;

            // Validate move
            if (!movementValidator.IsValidMove(ballToken.CurrentGridPosition, targetPosition))
            {
                Debug.LogWarning($"Invalid move to {targetPosition}!");
                return;
            }

            // Execute move
            ExecuteMove(targetPosition);
        }

        /// <summary>
        /// Executes a move to the target position
        /// </summary>
        private void ExecuteMove(Vector2Int targetPosition)
        {
            CurrentState = GameState.ProcessingMove;

            // Disable input during move
            if (inputManager != null)
            {
                inputManager.DisableInput();
            }

            Vector2Int previousPosition = ballToken.CurrentGridPosition;
            GridNode targetNode = gridManager.GetNode(targetPosition);

            if (targetNode == null)
            {
                Debug.LogError($"Target node not found at {targetPosition}");
                CurrentState = GameState.WaitingForInput;
                if (inputManager != null) inputManager.EnableInput();
                return;
            }

            bool wasNewNode = !targetNode.IsVisited;

            // Move the ball
            ballToken.MoveTo(targetPosition, () => OnMoveAnimationComplete(targetPosition, wasNewNode));

            Debug.Log($"{turnManager.GetCurrentPlayerName()} moved from {previousPosition} to {targetPosition}");
        }

        /// <summary>
        /// Called when ball movement animation completes
        /// </summary>
        private void OnMoveAnimationComplete(Vector2Int targetPosition, bool wasNewNode)
        {
            // Process the move with turn manager
            if (turnManager != null)
            {
                turnManager.ProcessMove(wasNewNode);
            }

            // Check for touchdown
            if (movementValidator.IsInEndZone(targetPosition))
            {
                HandleTouchdown(targetPosition);
                return;
            }

            // Update valid moves for next turn
            UpdateValidMoves();

            // If no extra turn was earned, end turn
            if (turnManager != null && !turnManager.HasExtraTurn())
            {
                Invoke(nameof(EndCurrentTurn), moveDelay);
            }
            else
            {
                CurrentState = GameState.WaitingForInput;
                if (inputManager != null) inputManager.EnableInput();
            }
        }

        /// <summary>
        /// Ends the current player's turn
        /// </summary>
        private void EndCurrentTurn()
        {
            if (turnManager != null)
            {
                turnManager.EndTurn();
            }

            CurrentState = GameState.WaitingForInput;
            if (inputManager != null) inputManager.EnableInput();
        }

        /// <summary>
        /// Handles a touchdown
        /// </summary>
        private void HandleTouchdown(Vector2Int position)
        {
            if (scoreManager != null && turnManager != null)
            {
                scoreManager.RecordTouchdown(turnManager.CurrentPlayer, position);
            }

            // Check if game is over
            if (scoreManager != null && scoreManager.GameOver)
            {
                CurrentState = GameState.GameOver;
                if (inputManager != null) inputManager.DisableInput();
            }
            else
            {
                // Reset for next play
                Invoke(nameof(StartGame), 2f);
            }
        }

        /// <summary>
        /// Handles a dead-end situation
        /// </summary>
        private void HandleDeadEnd()
        {
            Debug.Log($"{turnManager.GetCurrentPlayerName()} has no valid moves!");

            if (scoreManager != null && turnManager != null)
            {
                scoreManager.RecordDeadEnd(turnManager.CurrentPlayer);
            }

            CurrentState = GameState.GameOver;
            if (inputManager != null) inputManager.DisableInput();
        }

        /// <summary>
        /// Event handler for turn changes
        /// </summary>
        private void OnTurnChanged(TurnManager.Player newPlayer)
        {
            Debug.Log($"Turn changed to {newPlayer}");
            UpdateValidMoves();
        }

        /// <summary>
        /// Event handler for move completion
        /// </summary>
        private void OnMoveCompleted(TurnManager.Player player, int moveCount)
        {
            Debug.Log($"{player} completed move #{moveCount}");
        }

        /// <summary>
        /// Event handler for touchdowns
        /// </summary>
        private void OnTouchdown(TurnManager.Player player, Vector2Int position)
        {
            Debug.Log($"{player} scored a touchdown at {position}!");
        }

        /// <summary>
        /// Event handler for game won
        /// </summary>
        private void OnGameWon(TurnManager.Player winner, int finalScore)
        {
            Debug.Log($"{winner} wins with {finalScore} points!");
        }

        /// <summary>
        /// Event handler for dead ends
        /// </summary>
        private void OnDeadEnd(TurnManager.Player stuckPlayer)
        {
            Debug.Log($"{stuckPlayer} hit a dead end!");
        }

        /// <summary>
        /// Restarts the game
        /// </summary>
        public void RestartGame()
        {
            StartGame();
        }

        /// <summary>
        /// Gets the current valid moves
        /// </summary>
        public List<GridNode> GetCurrentValidMoves()
        {
            return currentValidMoves != null ? new List<GridNode>(currentValidMoves) : new List<GridNode>();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (turnManager != null)
            {
                turnManager.OnTurnChanged -= OnTurnChanged;
                turnManager.OnMoveCompleted -= OnMoveCompleted;
            }

            if (scoreManager != null)
            {
                scoreManager.OnTouchdown -= OnTouchdown;
                scoreManager.OnGameWon -= OnGameWon;
                scoreManager.OnDeadEnd -= OnDeadEnd;
            }

            if (inputManager != null)
            {
                inputManager.OnGridPositionClicked -= OnGridPositionClicked;
            }
        }
    }
}