using UnityEngine;

namespace PaperFootball.Ball
{
    /// <summary>
    /// Generates a 3D paper football triangle mesh.
    /// Creates a folded paper triangle with proper thickness.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PaperFootballMesh : MonoBehaviour
    {
        [Header("Triangle Settings")]
        [SerializeField] private float baseWidth = 0.4f;
        [SerializeField] private float height = 0.6f;
        [SerializeField] private float thickness = 0.15f;

        [Header("Visual Settings")]
        [SerializeField] private Color paperColor = new Color(0.95f, 0.95f, 0.9f); // Off-white
        [SerializeField] private Material paperMaterial;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();

            GeneratePaperFootballMesh();
            SetupMaterial();
        }

        /// <summary>
        /// Generates the 3D paper football triangle mesh
        /// </summary>
        private void GeneratePaperFootballMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "Paper Football";

            // Define vertices for a 3D triangle (front and back faces with thickness)
            Vector3[] vertices = new Vector3[]
            {
                // Front face (triangle)
                new Vector3(0, height / 2, thickness / 2),              // Top
                new Vector3(-baseWidth / 2, -height / 2, thickness / 2), // Bottom left
                new Vector3(baseWidth / 2, -height / 2, thickness / 2),  // Bottom right

                // Back face (triangle)
                new Vector3(0, height / 2, -thickness / 2),              // Top
                new Vector3(-baseWidth / 2, -height / 2, -thickness / 2), // Bottom left
                new Vector3(baseWidth / 2, -height / 2, -thickness / 2),  // Bottom right

                // Side edges (for thickness)
                // Left edge
                new Vector3(-baseWidth / 2, -height / 2, thickness / 2),  // 6
                new Vector3(-baseWidth / 2, -height / 2, -thickness / 2), // 7
                new Vector3(0, height / 2, thickness / 2),                // 8
                new Vector3(0, height / 2, -thickness / 2),               // 9

                // Right edge
                new Vector3(baseWidth / 2, -height / 2, thickness / 2),   // 10
                new Vector3(baseWidth / 2, -height / 2, -thickness / 2),  // 11
                new Vector3(0, height / 2, thickness / 2),                // 12
                new Vector3(0, height / 2, -thickness / 2),               // 13

                // Bottom edge
                new Vector3(-baseWidth / 2, -height / 2, thickness / 2),  // 14
                new Vector3(-baseWidth / 2, -height / 2, -thickness / 2), // 15
                new Vector3(baseWidth / 2, -height / 2, thickness / 2),   // 16
                new Vector3(baseWidth / 2, -height / 2, -thickness / 2)   // 17
            };

            // Define triangles (each face needs 2 triangles = 6 vertices)
            int[] triangles = new int[]
            {
                // Front face
                0, 1, 2,

                // Back face (reverse winding)
                3, 5, 4,

                // Left edge
                8, 7, 6,
                8, 9, 7,

                // Right edge
                12, 10, 11,
                12, 11, 13,

                // Bottom edge
                14, 15, 17,
                14, 17, 16
            };

            // Calculate normals
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.mesh = mesh;
        }

        /// <summary>
        /// Sets up the material for the paper football
        /// </summary>
        private void SetupMaterial()
        {
            if (paperMaterial != null)
            {
                meshRenderer.sharedMaterial = paperMaterial;
            }

            if (meshRenderer.sharedMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", paperColor);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", paperColor);
            }

            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0.2f);
            }

            meshRenderer.sharedMaterial = mat;
        }

        /// <summary>
        /// Updates the triangle dimensions at runtime
        /// </summary>
        public void UpdateDimensions(float newWidth, float newHeight, float newThickness)
        {
            baseWidth = newWidth;
            height = newHeight;
            thickness = newThickness;
            GeneratePaperFootballMesh();
        }
    }
}
