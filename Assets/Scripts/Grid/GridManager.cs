using System.Collections.Generic;
using UnityEngine;

namespace PaperFootball.Grid
{
    /// <summary>
    /// Manages the grid system for the paper football game.
    /// Handles node creation, neighbor detection, and grid queries.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 9;
        [SerializeField] private int gridHeight = 13;
        [SerializeField] private float nodeSpacing = 1f;

        [Header("End Zone Settings")]
        [SerializeField] private int topEndZoneRow = 12;
        [SerializeField] private int bottomEndZoneRow = 0;

        // Singleton instance
        public static GridManager Instance { get; private set; }

        // Grid storage
        private GridNode[,] grid;

        // Starting position (center bottom)
        public Vector2Int StartPosition { get; private set; }

        // 8 directional offsets for neighbors
        private static readonly Vector2Int[] DirectionOffsets = new Vector2Int[]
        {
            new(0, 1),   // Up
            new(1, 1),   // Up-Right
            new(1, 0),   // Right
            new(1, -1),  // Down-Right
            new(0, -1),  // Down
            new(-1, -1), // Down-Left
            new(-1, 0),  // Left
            new(-1, 1)   // Up-Left
        };

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;

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
            InitializeGrid();
        }

        /// <summary>
        /// Initializes the grid with all nodes
        /// </summary>
        private void InitializeGrid()
        {
            grid = new GridNode[gridWidth, gridHeight];

            // Calculate starting position (center of grid)
            Vector3 gridCenter = new(
                -(gridWidth - 1) * nodeSpacing / 2f,
                -(gridHeight - 1) * nodeSpacing / 2f,
                0f
            );

            // Create all nodes
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Vector3 worldPos = gridCenter + new Vector3(x * nodeSpacing, y * nodeSpacing, 0f);
                    grid[x, y] = new GridNode(x, y, worldPos);

                    // Mark end zones
                    if (y == topEndZoneRow)
                    {
                        grid[x, y].IsEndZone = true;
                        grid[x, y].EndZoneTeam = 1; // Top end zone
                        grid[x, y].VisualState = NodeVisualState.EndZone;
                    }
                    else if (y == bottomEndZoneRow)
                    {
                        grid[x, y].IsEndZone = true;
                        grid[x, y].EndZoneTeam = -1; // Bottom end zone
                        grid[x, y].VisualState = NodeVisualState.EndZone;
                    }
                }
            }

            // Set starting position (center, second row from bottom)
            StartPosition = new Vector2Int(gridWidth / 2, 1);

            Debug.Log($"Grid initialized: {gridWidth}x{gridHeight}, Start: {StartPosition}");
        }

        /// <summary>
        /// Gets a node at the specified grid position
        /// </summary>
        public GridNode GetNode(Vector2Int gridPos)
        {
            if (IsValidPosition(gridPos))
            {
                return grid[gridPos.x, gridPos.y];
            }
            return null;
        }

        /// <summary>
        /// Gets a node at the specified coordinates
        /// </summary>
        public GridNode GetNode(int x, int y)
        {
            return GetNode(new Vector2Int(x, y));
        }

        /// <summary>
        /// Checks if a grid position is valid
        /// </summary>
        public bool IsValidPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < gridWidth &&
                   gridPos.y >= 0 && gridPos.y < gridHeight;
        }

        /// <summary>
        /// Gets all valid neighbors of a node (8 directions)
        /// </summary>
        public List<GridNode> GetNeighbors(Vector2Int gridPos)
        {
            List<GridNode> neighbors = new();

            foreach (Vector2Int offset in DirectionOffsets)
            {
                Vector2Int neighborPos = gridPos + offset;
                if (IsValidPosition(neighborPos))
                {
                    neighbors.Add(grid[neighborPos.x, neighborPos.y]);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Gets all unvisited neighbors of a node
        /// </summary>
        public List<GridNode> GetUnvisitedNeighbors(Vector2Int gridPos)
        {
            List<GridNode> neighbors = GetNeighbors(gridPos);
            return neighbors.FindAll(n => !n.IsVisited);
        }

        /// <summary>
        /// Resets all nodes to their initial state
        /// </summary>
        public void ResetGrid()
        {
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    grid[x, y].Reset();

                    // Restore end zone visual state
                    if (grid[x, y].IsEndZone)
                    {
                        grid[x, y].VisualState = NodeVisualState.EndZone;
                    }
                }
            }
        }

        /// <summary>
        /// Converts world position to grid position
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            Vector3 gridCenter = new(
                -(gridWidth - 1) * nodeSpacing / 2f,
                -(gridHeight - 1) * nodeSpacing / 2f,
                0f
            );

            Vector3 localPos = worldPos - gridCenter;
            int x = Mathf.RoundToInt(localPos.x / nodeSpacing);
            int y = Mathf.RoundToInt(localPos.y / nodeSpacing);

            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Gets the world position of a grid coordinate
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            if (IsValidPosition(gridPos))
            {
                return grid[gridPos.x, gridPos.y].WorldPosition;
            }
            return Vector3.zero;
        }
    }
}