using System.Collections.Generic;
using UnityEngine;

namespace PaperFootball.Tabletop.Roguelike.Encounters
{
    public class ObstacleLayoutController : MonoBehaviour
    {
        [SerializeField] private Transform obstacleRoot;
        [SerializeField] private Material obstacleMaterial;

        private readonly List<GameObject> activeObstacles = new();

        public IReadOnlyList<GameObject> ActiveObstacles => activeObstacles;

        public void Configure(Transform root, Material material)
        {
            obstacleRoot = root;
            obstacleMaterial = material;
        }

        public void Clear()
        {
            for (int i = activeObstacles.Count - 1; i >= 0; i--)
            {
                if (activeObstacles[i] != null)
                {
                    Destroy(activeObstacles[i]);
                }
            }

            activeObstacles.Clear();
        }

        public void Apply(ObstacleLayoutDefinition layout)
        {
            Clear();
            if (layout == null || obstacleRoot == null)
            {
                return;
            }

            foreach (ObstacleSpawn spawn in layout.Obstacles)
            {
                GameObject obstacle = CreateObstacle(spawn);
                activeObstacles.Add(obstacle);
            }
        }

        public List<Bounds> GetActiveBounds()
        {
            List<Bounds> bounds = new();
            foreach (GameObject obstacle in activeObstacles)
            {
                if (obstacle != null && obstacle.TryGetComponent(out Collider collider))
                {
                    bounds.Add(collider.bounds);
                }
            }

            return bounds;
        }

        private GameObject CreateObstacle(ObstacleSpawn spawn)
        {
            PrimitiveType primitive = spawn.kind == ObstacleKind.Book ? PrimitiveType.Cube : PrimitiveType.Cylinder;
            GameObject obstacle = GameObject.CreatePrimitive(primitive);
            obstacle.name = $"EncounterObstacle_{spawn.kind}";
            obstacle.transform.SetParent(obstacleRoot, false);
            obstacle.transform.localPosition = spawn.position;
            obstacle.transform.localEulerAngles = spawn.eulerAngles;
            obstacle.transform.localScale = spawn.scale;

            if (obstacleMaterial != null && obstacle.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = obstacleMaterial;
            }

            Rigidbody body = obstacle.AddComponent<Rigidbody>();
            body.isKinematic = true;
            return obstacle;
        }
    }
}
