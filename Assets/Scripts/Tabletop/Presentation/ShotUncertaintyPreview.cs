using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Roguelike.Variance;
using UnityEngine;
using UnityEngine.UI;

namespace PaperFootball.Tabletop.Presentation
{
    public class ShotUncertaintyPreview : MonoBehaviour
    {
        [SerializeField] private LineRenderer coneLeftLine;
        [SerializeField] private LineRenderer coneRightLine;
        [SerializeField] private LineRenderer contactJitterLine;
        [SerializeField] private Text uncertaintyText;
        [SerializeField] private ContactPointSelector contactPointSelector;
        [SerializeField] private ShotVarianceController shotVarianceController;
        [SerializeField] private float lineLengthScale = 0.36f;
        [SerializeField] private int jitterSegments = 24;

        private bool flickVisible;
        private ShotVarianceTuning lastTuning = ShotVarianceTuning.Disabled;

        public void Configure(
            LineRenderer leftCone,
            LineRenderer rightCone,
            LineRenderer jitterCircle,
            Text label,
            ContactPointSelector selector,
            ShotVarianceController varianceController)
        {
            coneLeftLine = leftCone;
            coneRightLine = rightCone;
            contactJitterLine = jitterCircle;
            uncertaintyText = label;
            contactPointSelector = selector;
            shotVarianceController = varianceController;
            Hide();
        }

        public void Show(FlickCommand command, ShotVarianceTuning tuning)
        {
            lastTuning = tuning;
            if (!command.IsValid || !tuning.VarianceEnabled)
            {
                HideFlickPreview();
                return;
            }

            flickVisible = true;
            DrawCone(command, tuning);
            SetText(BuildText(tuning, command.Force));
        }

        public void HideFlickPreview()
        {
            flickVisible = false;
            SetLineVisible(coneLeftLine, false);
            SetLineVisible(coneRightLine, false);
            if (uncertaintyText != null)
            {
                uncertaintyText.enabled = false;
            }
        }

        public void Hide()
        {
            HideFlickPreview();
            SetLineVisible(contactJitterLine, false);
        }

        private void Update()
        {
            ShotVarianceTuning tuning = shotVarianceController != null ? shotVarianceController.CurrentTuning : lastTuning;
            if (contactPointSelector != null &&
                contactPointSelector.InputEnabled &&
                contactPointSelector.HasCurrentSelection &&
                tuning.VarianceEnabled &&
                tuning.ContactPointVarianceRadius > 0f)
            {
                DrawContactJitter(contactPointSelector.CurrentSelection, tuning.ContactPointVarianceRadius);
            }
            else
            {
                SetLineVisible(contactJitterLine, false);
            }
        }

        private void DrawCone(FlickCommand command, ShotVarianceTuning tuning)
        {
            Vector3 start = command.DragStartWorld + Vector3.up * 0.08f;
            float length = Mathf.Max(0.1f, command.Force * lineLengthScale);
            Vector3 left = Quaternion.AngleAxis(-tuning.DirectionVarianceDegrees, Vector3.up) * command.Direction;
            Vector3 right = Quaternion.AngleAxis(tuning.DirectionVarianceDegrees, Vector3.up) * command.Direction;
            DrawLine(coneLeftLine, start, start + left.normalized * length);
            DrawLine(coneRightLine, start, start + right.normalized * length);
        }

        private void DrawContactJitter(SelectedContactPoint contact, float radius)
        {
            if (contactJitterLine == null || !contact.IsValid)
            {
                return;
            }

            contactJitterLine.enabled = true;
            contactJitterLine.useWorldSpace = true;
            contactJitterLine.loop = true;
            contactJitterLine.positionCount = jitterSegments;
            contactJitterLine.startWidth = 0.012f;
            contactJitterLine.endWidth = 0.012f;

            Vector3 center = contact.GetWorldPoint() + contact.GetWorldNormal() * 0.035f;
            for (int i = 0; i < jitterSegments; i++)
            {
                float angle = Mathf.PI * 2f * i / jitterSegments;
                Vector3 offset = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                contactJitterLine.SetPosition(i, center + offset);
            }
        }

        private static void DrawLine(LineRenderer line, Vector3 start, Vector3 end)
        {
            if (line == null)
            {
                return;
            }

            line.enabled = true;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.018f;
            line.endWidth = 0.008f;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
        }

        private static void SetLineVisible(LineRenderer line, bool visible)
        {
            if (line != null)
            {
                line.enabled = visible;
                if (!visible)
                {
                    line.positionCount = 0;
                }
            }
        }

        private string BuildText(ShotVarianceTuning tuning, float baseForce)
        {
            float minForce = baseForce * (1f - tuning.ForceVariancePercent);
            float maxForce = baseForce * (1f + tuning.ForceVariancePercent);
            return $"Accuracy: {tuning.AccuracyRating}\nPower range: {minForce:0.00}-{maxForce:0.00}\nDirection cone: +/-{tuning.DirectionVarianceDegrees:0.0} deg\nContact jitter: {tuning.ContactPointVarianceRadius:0.0000}";
        }

        private void SetText(string text)
        {
            if (uncertaintyText != null)
            {
                uncertaintyText.text = text;
                uncertaintyText.enabled = flickVisible;
            }
        }
    }
}
