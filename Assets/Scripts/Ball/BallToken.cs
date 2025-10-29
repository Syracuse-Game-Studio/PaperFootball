using System.Collections;
using UnityEngine;

namespace PaperFootball.Ball
{
    /// <summary>
    /// Represents the football token that moves across the grid.
    /// </summary>
    public class BallToken : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Visual Settings")]
        [SerializeField] private Color ballColor = Color.black;
        [SerializeField] private float ballSize = 0.3f;

        // Current grid position
        public Vector2Int CurrentGridPosition { get; private set; }

        // Is the ball currently moving
        public bool IsMoving { get; private set; }

        private Grid.GridManager gridManager;
        private Renderer ballRenderer;

        private void Awake()
        {
            // Setup visual
            ballRenderer = GetComponent<Renderer>();
            if (ballRenderer != null)
            {
                ballRenderer.material.color = ballColor;
            }

            transform.localScale = Vector3.one * ballSize;
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
            transform.position = worldPos;

            // Mark node as visited
            Grid.GridNode node = gridManager.GetNode(gridPos);
            node?.Visit();
        }

        /// <summary>
        /// Moves the ball to a new grid position with animation
        /// </summary>
        public void MoveTo(Vector2Int targetGridPos, System.Action onComplete = null)
        {
            if (IsMoving)
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
            IsMoving = true;

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

            IsMoving = false;
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
    }
}