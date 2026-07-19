using UnityEngine;

namespace PaperFootball.Tabletop.Input
{
    public readonly struct SelectedContactPoint
    {
        public SelectedContactPoint(Collider collider, Vector3 localPoint, Vector3 localNormal)
        {
            Collider = collider;
            LocalPoint = localPoint;
            LocalNormal = localNormal.sqrMagnitude > 0.000001f ? localNormal.normalized : Vector3.up;
        }

        public Collider Collider { get; }
        public Vector3 LocalPoint { get; }
        public Vector3 LocalNormal { get; }
        public bool IsValid => Collider != null;

        public Vector3 GetWorldPoint()
        {
            return Collider != null ? Collider.transform.TransformPoint(LocalPoint) : Vector3.zero;
        }

        public Vector3 GetWorldNormal()
        {
            if (Collider == null)
            {
                return Vector3.up;
            }

            Vector3 normal = Collider.transform.TransformDirection(LocalNormal);
            return normal.sqrMagnitude > 0.000001f ? normal.normalized : Vector3.up;
        }

        public static SelectedContactPoint FromRaycastHit(RaycastHit hit)
        {
            Collider hitCollider = hit.collider;
            Transform hitTransform = hitCollider.transform;
            return new SelectedContactPoint(
                hitCollider,
                hitTransform.InverseTransformPoint(hit.point),
                hitTransform.InverseTransformDirection(hit.normal));
        }
    }

    internal static class FootballContactRaycaster
    {
        private const int MaxBufferedHits = 32;
        private static readonly RaycastHit[] BufferedHits = new RaycastHit[MaxBufferedHits];

        public static bool TryRaycast(Collider footballCollider, Ray ray, float maxDistance, out RaycastHit footballHit)
        {
            footballHit = default;
            if (footballCollider == null || maxDistance <= 0f)
            {
                return false;
            }

            if (footballCollider.Raycast(ray, out RaycastHit directHit, maxDistance))
            {
                footballHit = directHit;
                return true;
            }

            int hitCount = UnityEngine.Physics.RaycastNonAlloc(
                ray,
                BufferedHits,
                maxDistance,
                UnityEngine.Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float closestFootballDistance = float.PositiveInfinity;
            bool foundFootball = false;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = BufferedHits[i];
                if (!IsFootballCollider(candidate.collider, footballCollider) ||
                    candidate.distance >= closestFootballDistance)
                {
                    continue;
                }

                closestFootballDistance = candidate.distance;
                footballHit = candidate;
                foundFootball = true;
            }

            return foundFootball;
        }

        private static bool IsFootballCollider(Collider candidate, Collider footballCollider)
        {
            if (candidate == null || footballCollider == null)
            {
                return false;
            }

            Transform footballTransform = footballCollider.transform;
            Transform candidateTransform = candidate.transform;
            return candidate == footballCollider ||
                   candidateTransform == footballTransform ||
                   candidateTransform.IsChildOf(footballTransform);
        }
    }
}
