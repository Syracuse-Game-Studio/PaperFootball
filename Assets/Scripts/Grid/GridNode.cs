using UnityEngine;

namespace PaperFootball.Grid
{
    /// <summary>
    /// Represents a single node in the paper football grid.
    /// Tracks position, state, and connections to neighboring nodes.
    /// </summary>
    public class GridNode
    {
        /// <summary>
        /// Grid coordinates (x, y)
        /// </summary>
        public Vector2Int GridPosition { get; private set; }

        /// <summary>
        /// World position for rendering
        /// </summary>
        public Vector3 WorldPosition { get; private set; }

        /// <summary>
        /// Whether this node has been visited during the game
        /// </summary>
        public bool IsVisited { get; set; }

        /// <summary>
        /// Whether this node is in the end zone (top or bottom row)
        /// </summary>
        public bool IsEndZone { get; set; }

        /// <summary>
        /// Which end zone this belongs to (1 = top, -1 = bottom, 0 = neither)
        /// </summary>
        public int EndZoneTeam { get; set; }

        /// <summary>
        /// Current visual state of the node
        /// </summary>
        public NodeVisualState VisualState { get; set; }

        /// <summary>
        /// Constructor for GridNode
        /// </summary>
        /// <param name="gridX">X coordinate in grid</param>
        /// <param name="gridY">Y coordinate in grid</param>
        /// <param name="worldPos">World position for rendering</param>
        public GridNode(int gridX, int gridY, Vector3 worldPos)
        {
            GridPosition = new Vector2Int(gridX, gridY);
            WorldPosition = worldPos;
            IsVisited = false;
            IsEndZone = false;
            EndZoneTeam = 0;
            VisualState = NodeVisualState.Normal;
        }

        /// <summary>
        /// Resets the node to its initial state
        /// </summary>
        public void Reset()
        {
            IsVisited = false;
            VisualState = NodeVisualState.Normal;
        }

        /// <summary>
        /// Marks this node as visited
        /// </summary>
        public void Visit()
        {
            IsVisited = true;
            VisualState = NodeVisualState.Visited;
        }
    }

    /// <summary>
    /// Visual states for grid nodes
    /// </summary>
    public enum NodeVisualState
    {
        Normal,         // Unvisited, not highlighted
        Visited,        // Has been visited
        ValidMove,      // Available for current move
        CurrentPosition, // Current ball position
        EndZone         // In scoring zone
    }
}