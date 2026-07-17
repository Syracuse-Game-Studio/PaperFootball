using UnityEngine;

namespace PaperFootball.Environment
{
    /// <summary>
    /// Creates invisible boundaries around the playing field.
    /// Oriented for XZ plane (top-down camera looking down Y-axis)
    /// </summary>
    public class TableBoundary : MonoBehaviour
    {
        [Header("Boundary Settings")]
        [SerializeField] private Vector2 fieldSize = new Vector2(8f, 12f); // X and Z dimensions
        [SerializeField] private float wallHeight = 2f; // Height in Y direction
        [SerializeField] private float wallThickness = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool showBoundaries = true;

        private void Start()
        {
            CreateBoundaries();
        }

        private void CreateBoundaries()
        {
            // Top wall (positive Z / "forward")
            CreateWall("TopWall",
                new Vector3(0, wallHeight / 2, fieldSize.y / 2),
                new Vector3(fieldSize.x + wallThickness * 2, wallHeight, wallThickness));

            // Bottom wall (negative Z / "back")
            CreateWall("BottomWall",
                new Vector3(0, wallHeight / 2, -fieldSize.y / 2),
                new Vector3(fieldSize.x + wallThickness * 2, wallHeight, wallThickness));

            // Left wall (negative X)
            CreateWall("LeftWall",
                new Vector3(-fieldSize.x / 2, wallHeight / 2, 0),
                new Vector3(wallThickness, wallHeight, fieldSize.y));

            // Right wall (positive X)
            CreateWall("RightWall",
                new Vector3(fieldSize.x / 2, wallHeight / 2, 0),
                new Vector3(wallThickness, wallHeight, fieldSize.y));

            Debug.Log($"Table boundaries created: {fieldSize.x} x {fieldSize.y} (XZ plane)");
        }

        private void CreateWall(string name, Vector3 position, Vector3 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(transform);
            wall.transform.localPosition = position;

            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = size;

            // Add physics material for bouncing
            PhysicsMaterial bounceMat = new PhysicsMaterial("WallMaterial");
            bounceMat.bounciness = 0.5f;
            bounceMat.frictionCombine = PhysicsMaterialCombine.Minimum;
            bounceMat.bounceCombine = PhysicsMaterialCombine.Maximum;
            collider.material = bounceMat;

            Debug.Log($"Created wall: {name} at {position} with size {size}");
        }

        private void OnDrawGizmos()
        {
            if (!showBoundaries) return;

            Gizmos.color = Color.red;

            // Draw field boundary on XZ plane (Y=0)
            Vector3 topLeft = new Vector3(-fieldSize.x / 2, 0, fieldSize.y / 2);
            Vector3 topRight = new Vector3(fieldSize.x / 2, 0, fieldSize.y / 2);
            Vector3 bottomLeft = new Vector3(-fieldSize.x / 2, 0, -fieldSize.y / 2);
            Vector3 bottomRight = new Vector3(fieldSize.x / 2, 0, -fieldSize.y / 2);

            // Draw rectangle on XZ plane
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);

            // Draw wall positions
            Gizmos.color = Color.yellow;

            // Top wall
            Gizmos.DrawWireCube(new Vector3(0, wallHeight / 2, fieldSize.y / 2),
                new Vector3(fieldSize.x, wallHeight, wallThickness));

            // Bottom wall
            Gizmos.DrawWireCube(new Vector3(0, wallHeight / 2, -fieldSize.y / 2),
                new Vector3(fieldSize.x, wallHeight, wallThickness));

            // Left wall
            Gizmos.DrawWireCube(new Vector3(-fieldSize.x / 2, wallHeight / 2, 0),
                new Vector3(wallThickness, wallHeight, fieldSize.y));

            // Right wall
            Gizmos.DrawWireCube(new Vector3(fieldSize.x / 2, wallHeight / 2, 0),
                new Vector3(wallThickness, wallHeight, fieldSize.y));
        }
    }
}