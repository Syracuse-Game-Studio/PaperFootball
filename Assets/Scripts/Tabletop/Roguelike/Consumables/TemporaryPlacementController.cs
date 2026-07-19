using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PaperFootball.Tabletop.Roguelike.Consumables
{
    public class TemporaryPlacementController : MonoBehaviour
    {
        [SerializeField] private Transform placementRoot;
        [SerializeField] private Collider tableCollider;
        [SerializeField] private Collider footballCollider;
        [SerializeField] private Material tapeMaterial;
        [SerializeField] private Material eraserMaterial;

        private readonly List<GameObject> temporaryObjects = new();

        public IReadOnlyList<GameObject> TemporaryObjects => temporaryObjects;

        public void Configure(Transform root, Collider table, Collider football, Material tape, Material eraser)
        {
            placementRoot = root;
            tableCollider = table;
            footballCollider = football;
            tapeMaterial = tape;
            eraserMaterial = eraser;
        }

        public bool TryPlaceTapeFrictionPatch(Vector3 center, Vector2 size)
        {
            if (!IsInsideTable(center) || OverlapsForbidden(center, new Vector3(size.x, 0.04f, size.y)))
            {
                return false;
            }

            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patch.name = "TemporaryTapeFrictionPatch";
            patch.transform.SetParent(placementRoot, true);
            patch.transform.position = center;
            patch.transform.localScale = new Vector3(Mathf.Max(0.1f, size.x), 0.025f, Mathf.Max(0.1f, size.y));
            if (patch.TryGetComponent(out Renderer renderer) && tapeMaterial != null)
            {
                renderer.sharedMaterial = tapeMaterial;
            }

            if (patch.TryGetComponent(out Collider collider))
            {
                collider.isTrigger = true;
                collider.material = new PhysicsMaterial("Tape Patch Runtime")
                {
                    dynamicFriction = 1.1f,
                    staticFriction = 1.2f,
                    frictionCombine = PhysicsMaterialCombine.Maximum
                };
            }

            temporaryObjects.Add(patch);
            return true;
        }

        public bool TryPlaceEraserBlocker(Vector3 center, Vector3 size, IEnumerable<Bounds> forbiddenBounds)
        {
            Vector3 runtimeSize = new(Mathf.Max(0.1f, size.x), Mathf.Max(0.1f, size.y), Mathf.Max(0.1f, size.z));
            Bounds candidate = new(center, runtimeSize);
            if (!IsInsideTable(center) || OverlapsForbidden(center, runtimeSize) || (forbiddenBounds != null && forbiddenBounds.Any(candidate.Intersects)))
            {
                return false;
            }

            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "TemporaryEraserBlocker";
            blocker.transform.SetParent(placementRoot, true);
            blocker.transform.position = center;
            blocker.transform.localScale = runtimeSize;
            if (blocker.TryGetComponent(out Renderer renderer) && eraserMaterial != null)
            {
                renderer.sharedMaterial = eraserMaterial;
            }

            Rigidbody body = blocker.AddComponent<Rigidbody>();
            body.isKinematic = true;
            temporaryObjects.Add(blocker);
            return true;
        }

        public void ClearTemporaryObjects()
        {
            for (int i = temporaryObjects.Count - 1; i >= 0; i--)
            {
                if (temporaryObjects[i] != null)
                {
                    Destroy(temporaryObjects[i]);
                }
            }

            temporaryObjects.Clear();
        }

        private bool IsInsideTable(Vector3 point)
        {
            return tableCollider == null || tableCollider.bounds.Contains(new Vector3(point.x, tableCollider.bounds.center.y, point.z));
        }

        private bool OverlapsForbidden(Vector3 center, Vector3 size)
        {
            if (footballCollider == null)
            {
                return false;
            }

            return new Bounds(center, size).Intersects(footballCollider.bounds);
        }
    }
}
