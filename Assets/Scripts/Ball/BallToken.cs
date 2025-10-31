using System.Collections;
using UnityEngine;

namespace PaperFootball.Ball
{
    /// <summary>
    /// Represents the football token that moves across the grid.
    /// Now supports both grid-based movement and physics-based flicking.
    /// </summary>
    [RequireComponent(typeof(PaperFootballMesh), typeof(PaperFootballPhysics))]
    public class BallToken : MonoBehaviour
    {
        [Header("Movement Mode")]
        [SerializeField] private bool usePhysicsMode = true; // Toggle between grid and physics

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Visual Settings")]
        [SerializeField] private Color ballColor = Color.white;
        [SerializeField] private float ballSize = 0.3f;

        // Current grid position
        public Vector2Int CurrentGridPosition { get; private set; }

        // Is the ball currently moving
        public bool IsMoving => usePhysicsMode ? physicsController.IsMoving : isMovingGrid;
        private bool isMovingGrid;

        private Grid.GridManager gridManager;
        private PaperFootballMesh meshGenerator;
        private PaperFootballPhysics physicsController;

        private void Awake()
        {
            // Get components
            meshGenerator = GetComponent<PaperFootballMesh>();
            physicsController = GetComponent<PaperFootballPhysics>();

            transform.localScale = Vector3.one * ballSize;

            // Subscribe to physics events
            if (physicsController != null)
            {
                physicsController.OnFlick += OnBallFlicked;
                physicsController.OnLanded += OnBallLanded;
            }
        }

        private void Start()
        {
            gridManager = Grid.GridManager.Instance;

            if (gridManager != null)
            {
                // Start at the starting position
                SetPosition(gridManager.StartPosition);
            }
        }

        /// <summary>
        /// Sets the ball position instantly without animation
        /// </summary>
        public void SetPosition(Vector2Int gridPos)
        {
            if (gridManager == null) return;

            CurrentGridPosition = gridPos;
            Vector3 worldPos = gridManager.GridToWorldPosition(gridPos);

            if (usePhysicsMode && physicsController != null)
            {
                physicsController.SetPosition(worldPos);
            }
            else
            {
                transform.position = worldPos;
            }

            // Mark node as visited
            Grid.GridNode node = gridManager.GetNode(gridPos);
            node?.Visit();
        }

        /// <summary>
        /// Moves the ball to a new grid position with animation
        /// </summary>
        public void MoveTo(Vector2Int targetGridPos, System.Action onComplete = null)
        {
            if (isMovingGrid)
            {
                Debug.LogWarning("Ball is already moving!");
                return;
            }

            StartCoroutine(MoveCoroutine(targetGridPos, onComplete));
        }

        /// <summary>
        /// Coroutine for animated movement
        /// </summary>
        private IEnumerator MoveCoroutine(Vector2Int targetGridPos, System.Action onComplete)
        {
            isMovingGrid = true;

            Vector3 startPos = transform.position;
            Vector3 targetPos = gridManager.GridToWorldPosition(targetGridPos);
            float distance = Vector3.Distance(startPos, targetPos);
            float duration = distance / moveSpeed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curveT = moveCurve.Evaluate(t);

                transform.position = Vector3.Lerp(startPos, targetPos, curveT);
                yield return null;
            }

            // Ensure final position is exact
            transform.position = targetPos;
            CurrentGridPosition = targetGridPos;

            // Mark node as visited
            Grid.GridNode node = gridManager.GetNode(targetGridPos);
            node?.Visit();

            isMovingGrid = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Gets the current world position of the ball
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            return transform.position;
        }

        /// <summary>
        /// Resets the ball to the starting position
        /// </summary>
        public void ResetToStart()
        {
            if (gridManager != null)
            {
                SetPosition(gridManager.StartPosition);
            }
        }

        /// <summary>
        /// Called when the ball is flicked
        /// </summary>
        private void OnBallFlicked(float force)
        {
            Debug.Log($"Ball flicked with force: {force:F2}");
            // Add visual/audio feedback here
        }

        /// <summary>
        /// Called when the ball lands after physics movement
        /// </summary>
        private void OnBallLanded()
        {
            Debug.Log("Ball landed!");

            // Update grid position based on world position
            if (gridManager != null && usePhysicsMode)
            {
                Vector2Int nearestGridPos = gridManager.WorldToGridPosition(transform.position);
                CurrentGridPosition = nearestGridPos;

                // Mark node as visited
                Grid.GridNode node = gridManager.GetNode(nearestGridPos);
                node?.Visit();
            }
        }

        /// <summary>
        /// Toggles between physics and grid movement modes
        /// </summary>
        public void SetPhysicsMode(bool enabled)
        {
            usePhysicsMode = enabled;
        }

        private void OnDestroy()
        {
            if (physicsController != null)
            {
                physicsController.OnFlick -= OnBallFlicked;
                physicsController.OnLanded -= OnBallLanded;
            }
        }
    }
}