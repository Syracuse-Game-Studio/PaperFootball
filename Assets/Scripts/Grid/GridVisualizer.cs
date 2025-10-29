using System.Collections.Generic;
using UnityEngine;

namespace PaperFootball.Grid
{
    /// <summary>
    /// Handles visualization of the game grid, including lines, nodes, and states.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Color normalLineColor = Color.gray;
        [SerializeField] private Color endZoneLineColor = Color.yellow;
        [SerializeField] private float lineWidth = 0.05f;

        [Header("Node Visuals")]
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private Color normalNodeColor = Color.white;
        [SerializeField] private Color visitedNodeColor = Color.red;
        [SerializeField] private Color validMoveColor = Color.green;
        [SerializeField] private Color currentPositionColor = Color.blue;
        [SerializeField] private Color endZoneColor = Color.yellow;
        [SerializeField] private float nodeSize = 0.2f;

        private GridManager gridManager;
        private LineRenderer lineRenderer;
        private static readonly Dictionary<Vector2Int, GameObject> dictionary = new();
        private readonly Dictionary<Vector2Int, GameObject> nodeVisuals = dictionary;

        private void Start()
        {
            gridManager = GridManager.Instance;
            lineRenderer = GetComponent<LineRenderer>();

            if (gridManager == null)
            {
                Debug.LogError("GridManager not found!");
                return;
            }

            SetupLineRenderer();
            DrawGrid();
            CreateNodeVisuals();
        }

        /// <summary>
        /// Configures the line renderer settings
        /// </summary>
        private void SetupLineRenderer()
        {
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = normalLineColor;
            lineRenderer.endColor = normalLineColor;
            lineRenderer.sortingOrder = 1;
        }

        /// <summary>
        /// Draws the grid lines
        /// </summary>
        private void DrawGrid()
        {
            List<Vector3> linePoints = new();

            // Draw vertical lines
            for (int x = 0; x < gridManager.GridWidth; x++)
            {
                Vector3 bottom = gridManager.GridToWorldPosition(new Vector2Int(x, 0));
                Vector3 top = gridManager.GridToWorldPosition(new Vector2Int(x, gridManager.GridHeight - 1));

                linePoints.Add(bottom);
                linePoints.Add(top);
            }

            // Draw horizontal lines
            for (int y = 0; y < gridManager.GridHeight; y++)
            {
                Vector3 left = gridManager.GridToWorldPosition(new Vector2Int(0, y));
                Vector3 right = gridManager.GridToWorldPosition(new Vector2Int(gridManager.GridWidth - 1, y));

                linePoints.Add(left);
                linePoints.Add(right);
            }

            lineRenderer.positionCount = linePoints.Count;
            lineRenderer.SetPositions(linePoints.ToArray());
        }

        /// <summary>
        /// Creates visual representations for each node
        /// </summary>
        private void CreateNodeVisuals()
        {
            for (int y = 0; y < gridManager.GridHeight; y++)
            {
                for (int x = 0; x < gridManager.GridWidth; x++)
                {
                    Vector2Int gridPos = new(x, y);
                    GridNode node = gridManager.GetNode(gridPos);

                    if (node != null)
                    {
                        GameObject nodeVisual = CreateNodeVisual(node);
                        nodeVisuals[gridPos] = nodeVisual;
                        UpdateNodeVisual(node);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a single node visual GameObject
        /// </summary>
        private GameObject CreateNodeVisual(GridNode node)
        {
            GameObject nodeObj;

            if (nodePrefab != null)
            {
                nodeObj = Instantiate(nodePrefab, node.WorldPosition, Quaternion.identity, transform);
            }
            else
            {
                // Create a simple sphere if no prefab is provided
                nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                nodeObj.transform.position = node.WorldPosition;
                nodeObj.transform.localScale = Vector3.one * nodeSize;
                nodeObj.transform.parent = transform;

                // Remove collider if present
                if (nodeObj.TryGetComponent<Collider>(out Collider collider)) Destroy(collider);
            }

            nodeObj.name = $"Node_{node.GridPosition.x}_{node.GridPosition.y}";
            return nodeObj;
        }

        /// <summary>
        /// Updates the visual state of a specific node
        /// </summary>
        public void UpdateNodeVisual(GridNode node)
        {
            if (nodeVisuals.TryGetValue(node.GridPosition, out GameObject nodeObj))
            {
                if (nodeObj.TryGetComponent<Renderer>(out Renderer renderer))
                {
                    Color color = GetColorForState(node.VisualState);
                    renderer.material.color = color;
                }
            }
        }

        /// <summary>
        /// Gets the appropriate color for a node state
        /// </summary>
        private Color GetColorForState(NodeVisualState state)
        {
            return state switch
            {
                NodeVisualState.Normal => normalNodeColor,
                NodeVisualState.Visited => visitedNodeColor,
                NodeVisualState.ValidMove => validMoveColor,
                NodeVisualState.CurrentPosition => currentPositionColor,
                NodeVisualState.EndZone => endZoneColor,
                _ => normalNodeColor,
            };
        }

        /// <summary>
        /// Highlights valid moves for the current position
        /// </summary>
        public void HighlightValidMoves(List<GridNode> validNodes)
        {
            // Reset all highlights first
            ClearHighlights();

            // Highlight valid moves
            foreach (GridNode node in validNodes)
            {
                node.VisualState = NodeVisualState.ValidMove;
                UpdateNodeVisual(node);
            }
        }

        /// <summary>
        /// Clears all move highlights
        /// </summary>
        public void ClearHighlights()
        {
            for (int y = 0; y < gridManager.GridHeight; y++)
            {
                for (int x = 0; x < gridManager.GridWidth; x++)
                {
                    GridNode node = gridManager.GetNode(x, y);
                    if (node != null && node.VisualState == NodeVisualState.ValidMove)
                    {
                        node.VisualState = node.IsVisited ? NodeVisualState.Visited : NodeVisualState.Normal;
                        if (node.IsEndZone) node.VisualState = NodeVisualState.EndZone;
                        UpdateNodeVisual(node);
                    }
                }
            }
        }

        /// <summary>
        /// Updates all node visuals to match their current states
        /// </summary>
        public void RefreshAllNodes()
        {
            for (int y = 0; y < gridManager.GridHeight; y++)
            {
                for (int x = 0; x < gridManager.GridWidth; x++)
                {
                    GridNode node = gridManager.GetNode(x, y);
                    if (node != null)
                    {
                        UpdateNodeVisual(node);
                    }
                }
            }
        }
    }
}