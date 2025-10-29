using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaperFootball.Input
{
    /// <summary>
    /// Handles touch input for mobile platforms.
    /// Supports tap, swipe, and multi-touch gestures.
    /// </summary>
    public class TouchInputHandler : MonoBehaviour
    {
        [Header("Touch Settings")]
        [SerializeField] private float tapThreshold = 0.2f; // Max time for tap
        [SerializeField] private float swipeThreshold = 50f; // Min pixels for swipe
        [SerializeField] private float pinchThreshold = 10f; // Min pixels for pinch

        // Events
        public event Action<Vector3> OnTap;
        public event Action<Vector3> OnDoubleTap;
        public event Action<Vector2> OnSwipe;
        public event Action<float> OnPinch;

        private Camera mainCamera;
        private readonly Dictionary<int, TouchData> activeTouches = new();
        private float lastTapTime = 0f;
        private Vector3 lastTapPosition;

        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("TouchInputHandler: No main camera found!");
            }
        }

        private void Update()
        {
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
            HandleTouchInput();
#endif
        }

        /// <summary>
        /// Handles touch input each frame
        /// </summary>
        private void HandleTouchInput()
        {
            if (mainCamera == null) return;

            // Process all active touches
            for (int i = 0; i < UnityEngine.Input.touchCount; i++)
            {
                Touch touch = UnityEngine.Input.GetTouch(i);
                ProcessTouch(touch);
            }

            // Check for pinch gesture
            if (UnityEngine.Input.touchCount == 2)
            {
                HandlePinchGesture();
            }
        }

        /// <summary>
        /// Processes a single touch
        /// </summary>
        private void ProcessTouch(Touch touch)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleTouchBegan(touch);
                    break;

                case TouchPhase.Moved:
                    HandleTouchMoved(touch);
                    break;

                case TouchPhase.Ended:
                    HandleTouchEnded(touch);
                    break;

                case TouchPhase.Canceled:
                    HandleTouchCanceled(touch);
                    break;
            }
        }

        /// <summary>
        /// Handles touch began
        /// </summary>
        private void HandleTouchBegan(Touch touch)
        {
            TouchData touchData = new()
            {
                fingerId = touch.fingerId,
                startPosition = touch.position,
                startTime = Time.time,
                startWorldPosition = ScreenToWorldPosition(touch.position)
            };

            activeTouches[touch.fingerId] = touchData;
        }

        /// <summary>
        /// Handles touch moved
        /// </summary>
        private void HandleTouchMoved(Touch touch)
        {
            if (activeTouches.ContainsKey(touch.fingerId))
            {
                TouchData touchData = activeTouches[touch.fingerId];
                touchData.currentPosition = touch.position;
                touchData.hasMoved = true;
                activeTouches[touch.fingerId] = touchData;
            }
        }

        /// <summary>
        /// Handles touch ended
        /// </summary>
        private void HandleTouchEnded(Touch touch)
        {
            if (!activeTouches.ContainsKey(touch.fingerId)) return;

            TouchData touchData = activeTouches[touch.fingerId];
            float touchDuration = Time.time - touchData.startTime;
            Vector2 touchDelta = touch.position - touchData.startPosition;
            float touchDistance = touchDelta.magnitude;

            // Check for tap
            if (touchDuration <= tapThreshold && touchDistance < swipeThreshold / 10f)
            {
                Vector3 worldPos = ScreenToWorldPosition(touch.position);

                // Check for double tap
                if (Time.time - lastTapTime < 0.3f && Vector3.Distance(worldPos, lastTapPosition) < 0.5f)
                {
                    OnDoubleTap?.Invoke(worldPos);
                }
                else
                {
                    OnTap?.Invoke(worldPos);
                }

                lastTapTime = Time.time;
                lastTapPosition = worldPos;
            }
            // Check for swipe
            else if (touchDistance >= swipeThreshold)
            {
                Vector2 swipeDirection = touchDelta.normalized;
                OnSwipe?.Invoke(swipeDirection);
            }

            activeTouches.Remove(touch.fingerId);
        }

        /// <summary>
        /// Handles touch canceled
        /// </summary>
        private void HandleTouchCanceled(Touch touch)
        {
            activeTouches.Remove(touch.fingerId);
        }

        /// <summary>
        /// Handles pinch gesture (two-finger zoom)
        /// </summary>
        private void HandlePinchGesture()
        {
            Touch touch0 = UnityEngine.Input.GetTouch(0);
            Touch touch1 = UnityEngine.Input.GetTouch(1);

            // Get previous touch positions
            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            // Calculate distances
            float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
            float currentMagnitude = (touch0.position - touch1.position).magnitude;

            // Calculate pinch amount
            float pinchAmount = currentMagnitude - prevMagnitude;

            if (Mathf.Abs(pinchAmount) > pinchThreshold * Time.deltaTime)
            {
                OnPinch?.Invoke(pinchAmount);
            }
        }

        /// <summary>
        /// Converts screen position to world position
        /// </summary>
        private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            if (mainCamera == null) return Vector3.zero;

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(mainCamera.transform.position.z)));
            worldPos.z = 0f; // Force Z to 0 for 2D
            return worldPos;
        }

        /// <summary>
        /// Checks if a touch is over a UI element
        /// </summary>
        public bool IsTouchOverUI(int touchId)
        {
            return UnityEngine.EventSystems.EventSystem.current != null &&
                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touchId);
        }
    }

    /// <summary>
    /// Data structure to track touch information
    /// </summary>
    public struct TouchData
    {
        public int fingerId;
        public Vector2 startPosition;
        public Vector2 currentPosition;
        public float startTime;
        public Vector3 startWorldPosition;
        public bool hasMoved;
    }
}