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
}
