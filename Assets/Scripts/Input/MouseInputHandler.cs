using System;
using UnityEngine;

namespace PaperFootball.Input
{
    /// <summary>
    /// Handles mouse input for desktop platforms.
    /// Supports clicking and hovering.
    /// </summary>
    public class MouseInputHandler : MonoBehaviour
    {
        [Header("Mouse Settings")]
        [SerializeField] private float clickThreshold = 0.2f; // Max time for click
        [SerializeField] private float dragThreshold = 5f; // Min pixels to count as drag

        // Events
        public event Action<Vector3> OnClick;
        public event Action<Vector3> OnRightClick;
        public event Action<Vector3> OnPointerMove;
        public event Action<Vector3, Vector3> OnDrag;

        private Camera mainCamera;
        private bool isDragging = false;
        private Vector3 dragStartPosition;
        private float mouseDownTime;

        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("MouseInputHandler: No main camera found!");
            }
        }

        private void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            HandleMouseInput();
#endif
        }

        /// <summary>
        /// Handles mouse input each frame
        /// </summary>
        private void HandleMouseInput()
        {
            if (mainCamera == null) return;

            // Left mouse button down
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                HandleMouseDown();
            }

            // Left mouse button up
            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                HandleMouseUp();
            }

            // Right mouse button click
            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                HandleRightClick();
            }

            // Mouse movement
            if (UnityEngine.Input.GetAxis("Mouse X") != 0 || UnityEngine.Input.GetAxis("Mouse Y") != 0)
            {
                HandleMouseMove();
            }

            // Check for dragging
            if (UnityEngine.Input.GetMouseButton(0) && isDragging)
            {
                HandleDrag();
            }
        }

        /// <summary>
        /// Handles mouse button down
        /// </summary>
        private void HandleMouseDown()
        {
            mouseDownTime = Time.time;
            dragStartPosition = GetMouseWorldPosition();
            isDragging = false;
        }

        /// <summary>
        /// Handles mouse button up
        /// </summary>
        private void HandleMouseUp()
        {
            Vector3 mouseUpPosition = GetMouseWorldPosition();
            float clickDuration = Time.time - mouseDownTime;
            float dragDistance = Vector3.Distance(dragStartPosition, mouseUpPosition);

            // Check if it was a click (short duration and minimal movement)
            if (clickDuration <= clickThreshold && dragDistance < dragThreshold / 100f)
            {
                OnClick?.Invoke(mouseUpPosition);
            }

            isDragging = false;
        }

        /// <summary>
        /// Handles right mouse button click
        /// </summary>
        private void HandleRightClick()
        {
            Vector3 worldPos = GetMouseWorldPosition();
            OnRightClick?.Invoke(worldPos);
        }

        /// <summary>
        /// Handles mouse movement
        /// </summary>
        private void HandleMouseMove()
        {
            Vector3 worldPos = GetMouseWorldPosition();
            OnPointerMove?.Invoke(worldPos);

            // Check if we should start dragging
            if (UnityEngine.Input.GetMouseButton(0) && !isDragging)
            {
                float dragDistance = Vector3.Distance(dragStartPosition, worldPos);
                if (dragDistance > dragThreshold / 100f)
                {
                    isDragging = true;
                }
            }
        }

        /// <summary>
        /// Handles mouse dragging
        /// </summary>
        private void HandleDrag()
        {
            Vector3 currentPosition = GetMouseWorldPosition();
            OnDrag?.Invoke(dragStartPosition, currentPosition);
        }

        /// <summary>
        /// Gets the world position of the mouse cursor
        /// </summary>
        private Vector3 GetMouseWorldPosition()
        {
            if (mainCamera == null) return Vector3.zero;

            Vector3 mousePos = UnityEngine.Input.mousePosition;
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(mainCamera.transform.position.z)));
            worldPos.z = 0f; // Force Z to 0 for 2D
            return worldPos;
        }

        /// <summary>
        /// Checks if the mouse is over a UI element
        /// </summary>
        public bool IsPointerOverUI()
        {
            return UnityEngine.EventSystems.EventSystem.current != null &&
                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }
    }
}