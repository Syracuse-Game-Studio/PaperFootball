using System;
using UnityEngine;

namespace PaperFootball.Input
{
    /// <summary>
    /// Central input manager that coordinates different input methods.
    /// Supports mouse, touch, and keyboard input.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private bool enableMouseInput = true;
        [SerializeField] private bool enableTouchInput = true;
        [SerializeField] private bool enableKeyboardInput = true;

        [Header("Camera")]
        [SerializeField] private Camera mainCamera;

        [Header("Input Handlers")]
        [SerializeField] private MouseInputHandler mouseHandler;
        [SerializeField] private TouchInputHandler touchHandler;

        // Events
        public event Action<Vector3> OnWorldPositionClicked;
        public event Action<Vector2Int> OnGridPositionClicked;
        public event Action<Vector3> OnPointerMove;

        public static InputManager Instance { get; private set; }

        // Input state
        public bool InputEnabled { get; set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Get camera if not assigned
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("No camera found! Input will not work properly.");
                }
            }

            // Setup input handlers
            SetupInputHandlers();

            Debug.Log("Input Manager initialized!");
        }

        /// <summary>
        /// Sets up all input handlers
        /// </summary>
        private void SetupInputHandlers()
        {
            // Create mouse handler if needed
            if (enableMouseInput && mouseHandler == null)
            {
                GameObject mouseObj = new("MouseInputHandler");
                mouseObj.transform.parent = transform;
                mouseHandler = mouseObj.AddComponent<MouseInputHandler>();
            }

            // Create touch handler if needed
            if (enableTouchInput && touchHandler == null)
            {
                GameObject touchObj = new("TouchInputHandler");
                touchObj.transform.parent = transform;
                touchHandler = touchObj.AddComponent<TouchInputHandler>();
            }

            // Subscribe to handler events
            if (mouseHandler != null)
            {
                mouseHandler.OnClick += HandleWorldClick;
                mouseHandler.OnPointerMove += HandlePointerMove;
            }

            if (touchHandler != null)
            {
                touchHandler.OnTap += HandleWorldClick;
            }
        }

        /// <summary>
        /// Handles a click/tap at a world position
        /// </summary>
        private void HandleWorldClick(Vector3 worldPosition)
        {
            if (!InputEnabled) return;

            OnWorldPositionClicked?.Invoke(worldPosition);

            // Convert to grid position
            if (Grid.GridManager.Instance != null)
            {
                Vector2Int gridPos = Grid.GridManager.Instance.WorldToGridPosition(worldPosition);
                OnGridPositionClicked?.Invoke(gridPos);
            }
        }

        /// <summary>
        /// Handles pointer movement
        /// </summary>
        private void HandlePointerMove(Vector3 worldPosition)
        {
            if (!InputEnabled) return;
            OnPointerMove?.Invoke(worldPosition);
        }

        /// <summary>
        /// Converts screen position to world position
        /// </summary>
        public Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            if (mainCamera == null) return Vector3.zero;

            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));
            worldPos.z = 0f; // Force Z to 0 for 2D
            return worldPos;
        }

        /// <summary>
        /// Gets the world position under the mouse/pointer
        /// </summary>
        public Vector3 GetPointerWorldPosition()
        {
            if (mainCamera == null) return Vector3.zero;

        #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            return ScreenToWorldPosition(UnityEngine.Input.mousePosition);
        #elif UNITY_IOS || UNITY_ANDROID
            if (UnityEngine.Input.touchCount > 0)
            {
                return ScreenToWorldPosition(UnityEngine.Input.GetTouch(0).position);
            }
            return Vector3.zero;
        #else
            return ScreenToWorldPosition(UnityEngine.Input.mousePosition);
        #endif
        }

        /// <summary>
        /// Enables input
        /// </summary>
        public void EnableInput()
        {
            InputEnabled = true;
        }

        /// <summary>
        /// Disables input
        /// </summary>
        public void DisableInput()
        {
            InputEnabled = false;
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (mouseHandler != null)
            {
                mouseHandler.OnClick -= HandleWorldClick;
                mouseHandler.OnPointerMove -= HandlePointerMove;
            }

            if (touchHandler != null)
            {
                touchHandler.OnTap -= HandleWorldClick;
            }
        }
    }
}