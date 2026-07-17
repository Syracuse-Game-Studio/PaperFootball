using PaperFootball.Tabletop.Input;
using UnityEngine;

namespace PaperFootball.Tabletop.Presentation
{
    [RequireComponent(typeof(LineRenderer))]
    public class FlickAimIndicator : MonoBehaviour
    {
        [SerializeField] private float lineLengthScale = 0.35f;
        [SerializeField] private Gradient lineGradient;

        private LineRenderer lineRenderer;

        public void Show(FlickCommand command)
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            if (!command.IsValid)
            {
                Hide();
                return;
            }

            lineRenderer.enabled = true;
            Vector3 start = command.DragStartWorld;
            start.y += 0.06f;
            Vector3 end = start + command.Direction * command.Force * lineLengthScale;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.startWidth = Mathf.Lerp(0.035f, 0.11f, command.Strength01);
            lineRenderer.endWidth = 0.01f;
        }

        public void Hide()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            lineRenderer.enabled = false;
        }

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = false;

            if (lineGradient == null)
            {
                lineGradient = new Gradient();
                lineGradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(0.1f, 0.95f, 0.75f), 0f),
                        new GradientColorKey(new Color(1f, 0.85f, 0.2f), 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    });
            }

            lineRenderer.colorGradient = lineGradient;
        }
    }
}
