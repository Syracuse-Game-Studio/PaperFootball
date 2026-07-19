using UnityEngine;

namespace PaperFootball.Tabletop.Roguelike.Encounters
{
    public class PrecisionTargetZone : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField] private Vector3 size = new(1f, 0.25f, 1f);

        public Bounds Bounds => new(transform.position, size);

        public void Configure(Transform visualTransform)
        {
            visual = visualTransform;
            Hide();
        }

        public void Show(Vector3 center, Vector3 targetSize)
        {
            transform.position = center;
            size = new Vector3(Mathf.Max(0.05f, targetSize.x), Mathf.Max(0.05f, targetSize.y), Mathf.Max(0.05f, targetSize.z));
            if (visual != null)
            {
                visual.gameObject.SetActive(true);
                visual.position = center;
                visual.localScale = new Vector3(size.x, 0.025f, size.z);
            }
        }

        public void Hide()
        {
            if (visual != null)
            {
                visual.gameObject.SetActive(false);
            }
        }

        public bool Contains(Vector3 point)
        {
            return Bounds.Contains(point);
        }
    }
}
