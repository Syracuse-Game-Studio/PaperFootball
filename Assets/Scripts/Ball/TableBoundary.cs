using UnityEngine;

namespace PaperFootball.Environment
{
    /// <summary>
    /// Creates invisible boundaries around the playing field
    /// </summary>
    public class TableBoundary : MonoBehaviour
    {
        [Header("Boundary Settings")]
        [SerializeField] private Vector2 fieldSize = new Vector2(10f, 15f);
        [SerializeField] private float wallHeight = 2f;
        [SerializeField] private float wallThickness = 0.5f;

        private void Start()
        {
            CreateBoundaries();
        }

        private void CreateBoundaries()
        {
            // Top wall
            CreateWall("TopWall", new Vector3(0, fieldSize.y / 2, 0), new Vector3(fieldSize.x, wallHeight, wallThickness));

            // Bottom wall
            CreateWall("BottomWall", new Vector3(0, -fieldSize.y / 2, 0), new Vector3(fieldSize.x, wallHeight, wallThickness));

            // Left wall
            CreateWall("LeftWall", new Vector3(-fieldSize.x / 2, 0, 0), new Vector3(wallThickness, wallHeight, fieldSize.y));

            // Right wall
            CreateWall("RightWall", new Vector3(fieldSize.x / 2, 0, 0), new Vector3(wallThickness, wallHeight, fieldSize.y));
        }

        private void CreateWall(string name, Vector3 position, Vector3 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(transform);
            wall.transform.localPosition = position;

            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = size;

            // Optional: Add physics material for bouncing
            PhysicsMaterial bounceMat = new PhysicsMaterial();
            bounceMat.bounciness = 0.5f;
            collider.material = bounceMat;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(fieldSize.x, fieldSize.y, 0.1f));
        }
    }
}