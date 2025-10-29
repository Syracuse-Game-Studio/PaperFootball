using PaperFootball.Ball;
using PaperFootball.Grid;
using UnityEngine;

namespace PaperFootball
{
    /// <summary>
    /// Manages the overall game board, coordinating the grid and ball.
    /// </summary>
    public class GameBoard : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private GridVisualizer gridVisualizer;
        [SerializeField] private BallToken ballToken;

        [Header("Board Settings")]
        [SerializeField] private bool autoInitialize = true;

        public static GameBoard Instance { get; private set; }

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (autoInitialize)
            {
                InitializeBoard();
            }
        }

        /// <summary>
        /// Initializes the game board
        /// </summary>
        public void InitializeBoard()
        {
            // Verify all components are present
            if (gridManager == null)
            {
                gridManager = Object.FindFirstObjectByType<GridManager>();
                if (gridManager == null)
                {
                    Debug.LogError("GridManager not found! Please add GridManager to the scene.");
                    return;
                }
            }

            if (gridVisualizer == null)
            {
                gridVisualizer = Object.FindFirstObjectByType<GridVisualizer>();
            }

            if (ballToken == null)
            {
                ballToken = Object.FindFirstObjectByType<BallToken>();
            }

            Debug.Log("Game Board initialized successfully!");
        }

        /// <summary>
        /// Resets the board to initial state
        /// </summary>
        public void ResetBoard()
        {
            if (gridManager != null)
            {
                gridManager.ResetGrid();
            }

            if (ballToken != null)
            {
                ballToken.ResetToStart();
            }

            if (gridVisualizer != null)
            {
                gridVisualizer.RefreshAllNodes();
            }

            Debug.Log("Game Board reset!");
        }

        /// <summary>
        /// Gets the current ball position
        /// </summary>
        public Vector2Int GetBallPosition()
        {
            return ballToken != null ? ballToken.CurrentGridPosition : Vector2Int.zero;
        }

        /// <summary>
        /// Checks if a position is in an end zone
        /// </summary>
        public bool IsInEndZone(Vector2Int gridPos)
        {
            if (gridManager != null)
            {
                GridNode node = gridManager.GetNode(gridPos);
                return node != null && node.IsEndZone;
            }
            return false;
        }

        /// <summary>
        /// Gets which team's end zone a position is in
        /// </summary>
        public int GetEndZoneTeam(Vector2Int gridPos)
        {
            if (gridManager != null)
            {
                GridNode node = gridManager.GetNode(gridPos);
                return node != null ? node.EndZoneTeam : 0;
            }
            return 0;
        }
    }
}