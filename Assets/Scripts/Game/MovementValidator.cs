using PaperFootball.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace PaperFootball.Game
{
    /// <summary>
    /// Validates moves according to paper football rules.
    /// Handles move legality, bounce detection, and path validation.
    /// </summary>
    public class MovementValidator : MonoBehaviour
    {
        private GridManager gridManager;

        // 8 directional offsets for movement
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

        public static MovementValidator Instance { get; private set; }

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
            gridManager = GridManager.Instance;
        }

        /// <summary>
        /// Gets all valid moves from a given position
        /// </summary>
        public List<GridNode> GetValidMoves(Vector2Int currentPosition)
        {
            List<GridNode> validMoves = new();

            if (gridManager == null) return validMoves;

            // Check all 8 directions
            foreach (Vector2Int offset in DirectionOffsets)
            {
                Vector2Int targetPos = currentPosition + offset;

                // Check if position is valid and unvisited
                if (gridManager.IsValidPosition(targetPos))
                {
                    GridNode targetNode = gridManager.GetNode(targetPos);

                    if (targetNode != null && !targetNode.IsVisited)
                    {
                        validMoves.Add(targetNode);
                    }
                }
            }

            return validMoves;
        }

        /// <summary>
        /// Checks if a move from one position to another is valid
        /// </summary>
        public bool IsValidMove(Vector2Int from, Vector2Int to)
        {
            // Check if target is in valid range (1 square away in any direction)
            int dx = Mathf.Abs(to.x - from.x);
            int dy = Mathf.Abs(to.y - from.y);

            if (dx > 1 || dy > 1)
            {
                return false; // Too far
            }

            if (dx == 0 && dy == 0)
            {
                return false; // Same position
            }

            // Check if position exists and is unvisited
            if (!gridManager.IsValidPosition(to))
            {
                return false;
            }

            GridNode targetNode = gridManager.GetNode(to);
            return targetNode != null && !targetNode.IsVisited;
        }

        /// <summary>
        /// Checks if the ball has hit a boundary (for bounce rule)
        /// </summary>
        public bool IsOnBoundary(Vector2Int position)
        {
            if (gridManager == null) return false;

            return position.x == 0 ||
                   position.x == gridManager.GridWidth - 1 ||
                   position.y == 0 ||
                   position.y == gridManager.GridHeight - 1;
        }

        /// <summary>
        /// Checks if a position is a new unvisited node (grants extra turn)
        /// </summary>
        public bool IsNewNode(Vector2Int position)
        {
            if (gridManager == null) return false;

            GridNode node = gridManager.GetNode(position);
            return node != null && !node.IsVisited;
        }

        /// <summary>
        /// Checks if there are any valid moves from a position (dead-end detection)
        /// </summary>
        public bool HasValidMoves(Vector2Int position)
        {
            List<GridNode> validMoves = GetValidMoves(position);
            return validMoves.Count > 0;
        }

        /// <summary>
        /// Determines if a move causes a bounce (hits boundary or visited node)
        /// </summary>
        public bool WouldCauseBounce(Vector2Int from, Vector2Int to)
        {
            // Check if target is on the boundary
            if (IsOnBoundary(to))
            {
                return true;
            }

            // Check if target is adjacent to boundaries
            return IsNearBoundary(to);
        }

        /// <summary>
        /// Checks if a position is adjacent to a boundary
        /// </summary>
        private bool IsNearBoundary(Vector2Int position)
        {
            foreach (Vector2Int offset in DirectionOffsets)
            {
                Vector2Int adjacentPos = position + offset;

                if (!gridManager.IsValidPosition(adjacentPos))
                {
                    return true; // Adjacent to edge
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the number of valid moves from a position
        /// </summary>
        public int GetValidMoveCount(Vector2Int position)
        {
            return GetValidMoves(position).Count;
        }

        /// <summary>
        /// Checks if the current position is in an end zone
        /// </summary>
        public bool IsInEndZone(Vector2Int position)
        {
            if (gridManager == null) return false;

            GridNode node = gridManager.GetNode(position);
            return node != null && node.IsEndZone;
        }

        /// <summary>
        /// Gets which team's end zone the position is in (1 = top, -1 = bottom, 0 = neither)
        /// </summary>
        public int GetEndZoneTeam(Vector2Int position)
        {
            if (gridManager == null) return 0;

            GridNode node = gridManager.GetNode(position);
            return node != null ? node.EndZoneTeam : 0;
        }

        /// <summary>
        /// Gets all positions that would result in scoring
        /// </summary>
        public List<GridNode> GetScoringMoves(Vector2Int currentPosition)
        {
            List<GridNode> validMoves = GetValidMoves(currentPosition);
            return validMoves.FindAll(node => node.IsEndZone);
        }
    }
}