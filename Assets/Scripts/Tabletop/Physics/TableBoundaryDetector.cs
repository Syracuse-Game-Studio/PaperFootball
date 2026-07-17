using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Physics
{
    public class TableBoundaryDetector : MonoBehaviour
    {
        [SerializeField] private Collider tableCollider;
        [SerializeField] private float fallHeight = -1.2f;

        public Bounds TableBounds => tableCollider != null ? tableCollider.bounds : new Bounds(Vector3.zero, Vector3.zero);
        public float TableTopY => tableCollider != null ? tableCollider.bounds.max.y : 0f;

        public void Configure(Collider table, PaperFootballRuleSet rules)
        {
            tableCollider = table;
            if (rules != null)
            {
                fallHeight = rules.fallHeight;
            }
        }

        public bool HasFallen(Transform football)
        {
            return football != null && football.position.y <= fallHeight;
        }

        public bool HasTable()
        {
            return tableCollider != null;
        }
    }
}
