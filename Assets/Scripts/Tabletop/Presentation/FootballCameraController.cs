using UnityEngine;

namespace PaperFootball.Tabletop.Presentation
{
    public class FootballCameraController : MonoBehaviour
    {
        [SerializeField] private Camera controlledCamera;
        [SerializeField] private Transform footballTarget;
        [SerializeField] private Vector3 tabletopPosition = new(0f, 9.4f, -7.4f);
        [SerializeField] private Vector3 tabletopLookAt = new(0f, 0.12f, 0f);
        [SerializeField] private float tabletopOrthographicSize = 6.8f;
        [SerializeField] private Vector3 contactSelectionOffset = new(0f, 2.15f, -1.85f);
        [SerializeField] private float contactSelectionOrthographicSize = 0.95f;
        [SerializeField] private float transitionDuration = 0.35f;

        private Vector3 transitionStartPosition;
        private Quaternion transitionStartRotation;
        private float transitionStartSize;
        private Vector3 transitionTargetPosition;
        private Quaternion transitionTargetRotation;
        private float transitionTargetSize;
        private float transitionElapsed;

        public bool IsTransitioning { get; private set; }

        public void Configure(
            Camera cameraReference,
            Transform targetFootball,
            Vector3 tablePosition,
            Vector3 tableLookAt,
            float tableOrthographicSize,
            Vector3 closeOffset,
            float closeOrthographicSize,
            float duration)
        {
            controlledCamera = cameraReference;
            footballTarget = targetFootball;
            tabletopPosition = tablePosition;
            tabletopLookAt = tableLookAt;
            tabletopOrthographicSize = Mathf.Max(0.1f, tableOrthographicSize);
            contactSelectionOffset = closeOffset;
            contactSelectionOrthographicSize = Mathf.Max(0.1f, closeOrthographicSize);
            transitionDuration = Mathf.Max(0f, duration);
        }

        public void ShowContactSelectionView()
        {
            Vector3 target = footballTarget != null ? footballTarget.position : tabletopLookAt;
            Vector3 position = target + contactSelectionOffset;
            BeginTransition(position, LookAtRotation(position, target), contactSelectionOrthographicSize);
        }

        public void ShowTabletopView()
        {
            BeginTransition(tabletopPosition, LookAtRotation(tabletopPosition, tabletopLookAt), tabletopOrthographicSize);
        }

        public void ShowResolutionView()
        {
            ShowTabletopView();
        }

        private void Awake()
        {
            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<Camera>();
            }
        }

        private void LateUpdate()
        {
            if (!IsTransitioning || controlledCamera == null)
            {
                return;
            }

            transitionElapsed += Time.unscaledDeltaTime;
            float duration = Mathf.Max(0.0001f, transitionDuration);
            float t = Mathf.Clamp01(transitionElapsed / duration);
            float eased = t * t * (3f - 2f * t);

            Transform cameraTransform = controlledCamera.transform;
            cameraTransform.position = Vector3.Lerp(transitionStartPosition, transitionTargetPosition, eased);
            cameraTransform.rotation = Quaternion.Slerp(transitionStartRotation, transitionTargetRotation, eased);
            controlledCamera.orthographicSize = Mathf.Lerp(transitionStartSize, transitionTargetSize, eased);

            if (t >= 1f)
            {
                IsTransitioning = false;
            }
        }

        private void BeginTransition(Vector3 position, Quaternion rotation, float orthographicSize)
        {
            if (controlledCamera == null)
            {
                return;
            }

            Transform cameraTransform = controlledCamera.transform;
            transitionStartPosition = cameraTransform.position;
            transitionStartRotation = cameraTransform.rotation;
            transitionStartSize = controlledCamera.orthographicSize;
            transitionTargetPosition = position;
            transitionTargetRotation = rotation;
            transitionTargetSize = Mathf.Max(0.1f, orthographicSize);
            transitionElapsed = 0f;

            if (transitionDuration <= 0f)
            {
                cameraTransform.SetPositionAndRotation(transitionTargetPosition, transitionTargetRotation);
                controlledCamera.orthographicSize = transitionTargetSize;
                IsTransitioning = false;
                return;
            }

            IsTransitioning = true;
        }

        private static Quaternion LookAtRotation(Vector3 position, Vector3 target)
        {
            Vector3 direction = target - position;
            return direction.sqrMagnitude > 0.000001f
                ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                : Quaternion.identity;
        }
    }
}
