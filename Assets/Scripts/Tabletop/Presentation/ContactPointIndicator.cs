using PaperFootball.Tabletop.Input;
using UnityEngine;
using UnityEngine.UI;

namespace PaperFootball.Tabletop.Presentation
{
    public class ContactPointIndicator : MonoBehaviour
    {
        [SerializeField] private Transform marker;
        [SerializeField] private Text feedbackText;
        [SerializeField] private LineRenderer yawPreviewLine;
        [SerializeField] private float surfaceOffset = 0.025f;
        [SerializeField] private float yawPreviewRadius = 0.38f;
        [SerializeField] private int yawPreviewSegments = 12;

        private bool isVisible;
        private bool hasPlannedDirection;
        private SelectedContactPoint selectedContact;
        private Vector3 plannedDirection;

        public bool IsVisible => isVisible;

        public void Configure(Transform markerTransform, Text feedback, LineRenderer yawLine)
        {
            marker = markerTransform;
            feedbackText = feedback;
            yawPreviewLine = yawLine;
            Hide();
        }

        public void Show(SelectedContactPoint contactPoint)
        {
            selectedContact = contactPoint;
            hasPlannedDirection = false;
            isVisible = contactPoint.IsValid;
            ApplyVisibility();
            UpdateVisuals();
        }

        public void ShowFlickPreview(SelectedContactPoint contactPoint, Vector3 direction)
        {
            selectedContact = contactPoint;
            plannedDirection = direction;
            plannedDirection.y = 0f;
            hasPlannedDirection = plannedDirection.sqrMagnitude > 0.000001f;
            if (hasPlannedDirection)
            {
                plannedDirection.Normalize();
            }

            isVisible = contactPoint.IsValid;
            ApplyVisibility();
            UpdateVisuals();
        }

        public void Hide()
        {
            isVisible = false;
            hasPlannedDirection = false;
            ApplyVisibility();
            SetText(string.Empty);
        }

        private void LateUpdate()
        {
            if (isVisible)
            {
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            if (!isVisible || !selectedContact.IsValid)
            {
                Hide();
                return;
            }

            Vector3 point = selectedContact.GetWorldPoint();
            Vector3 normal = selectedContact.GetWorldNormal();

            if (marker != null)
            {
                marker.position = point + normal * surfaceOffset;
                marker.rotation = Quaternion.LookRotation(normal);
            }

            SetText(BuildFeedbackText(point, normal));
            UpdateYawPreview(point);
        }

        private string BuildFeedbackText(Vector3 worldPoint, Vector3 normal)
        {
            Collider collider = selectedContact.Collider;
            Vector3 center = collider != null ? collider.bounds.center : worldPoint;
            float maxExtent = collider != null ? Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.z, 0.001f) : 1f;
            float offset01 = Mathf.Clamp01(Vector3.Distance(new Vector3(worldPoint.x, 0f, worldPoint.z), new Vector3(center.x, 0f, center.z)) / maxExtent);

            string spinLabel;
            string stabilityLabel;
            if (offset01 > 0.68f)
            {
                spinLabel = "HIGH SPIN";
                stabilityLabel = "LOW CONTROL";
            }
            else if (offset01 > 0.34f)
            {
                spinLabel = "HIGHER SPIN";
                stabilityLabel = "MEDIUM STABILITY";
            }
            else
            {
                spinLabel = "LOW SPIN";
                stabilityLabel = "HIGH STABILITY";
            }

            string yawLabel = hasPlannedDirection
                ? $"Yaw: {GetYawLabel(worldPoint, center)}"
                : "Yaw: aim to preview";

            return $"Contact: {spinLabel}\n{stabilityLabel}\n{yawLabel}\nNormal: {normal.x:F2}, {normal.y:F2}, {normal.z:F2}";
        }

        private string GetYawLabel(Vector3 worldPoint, Vector3 center)
        {
            Vector3 leverArm = worldPoint - center;
            leverArm.y = 0f;
            float yaw = Vector3.Cross(leverArm, plannedDirection).y;
            if (Mathf.Abs(yaw) < 0.0001f)
            {
                return "mostly straight";
            }

            return yaw > 0f ? "counterclockwise" : "clockwise";
        }

        private void UpdateYawPreview(Vector3 worldPoint)
        {
            if (yawPreviewLine == null)
            {
                return;
            }

            if (!hasPlannedDirection || selectedContact.Collider == null)
            {
                yawPreviewLine.enabled = false;
                yawPreviewLine.positionCount = 0;
                return;
            }

            Vector3 center = selectedContact.Collider.bounds.center;
            Vector3 leverArm = worldPoint - center;
            leverArm.y = 0f;
            if (leverArm.sqrMagnitude < 0.0001f)
            {
                leverArm = Vector3.right;
            }

            float yawSign = Mathf.Sign(Vector3.Cross(leverArm.normalized, plannedDirection).y);
            if (Mathf.Approximately(yawSign, 0f))
            {
                yawPreviewLine.enabled = false;
                yawPreviewLine.positionCount = 0;
                return;
            }

            yawPreviewLine.enabled = true;
            yawPreviewLine.positionCount = yawPreviewSegments;
            float startAngle = Mathf.Atan2(leverArm.z, leverArm.x);
            float sweep = yawSign * Mathf.PI * 0.55f;
            float y = worldPoint.y + surfaceOffset * 2f;

            for (int i = 0; i < yawPreviewSegments; i++)
            {
                float t = yawPreviewSegments <= 1 ? 0f : i / (float)(yawPreviewSegments - 1);
                float angle = startAngle + sweep * t;
                Vector3 offset = new(Mathf.Cos(angle) * yawPreviewRadius, 0f, Mathf.Sin(angle) * yawPreviewRadius);
                yawPreviewLine.SetPosition(i, new Vector3(center.x, y, center.z) + offset);
            }
        }

        private void ApplyVisibility()
        {
            if (marker != null)
            {
                marker.gameObject.SetActive(isVisible);
            }

            if (feedbackText != null)
            {
                feedbackText.enabled = isVisible;
            }

            if (!isVisible && yawPreviewLine != null)
            {
                yawPreviewLine.enabled = false;
                yawPreviewLine.positionCount = 0;
            }
        }

        private void SetText(string value)
        {
            if (feedbackText != null)
            {
                feedbackText.text = value;
            }
        }
    }
}
