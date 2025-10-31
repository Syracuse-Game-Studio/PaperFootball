using UnityEngine;

namespace PaperFootball.Ball
{
    /// <summary>
    /// Handles physics-based flicking mechanics for the paper football.
    /// Allows players to drag and flick the ball with realistic physics.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PaperFootballPhysics : MonoBehaviour
    {
        [Header("Flick Settings")]
        [SerializeField] private float flickForceMultiplier = 15f;
        [SerializeField] private float maxFlickForce = 50f;
        [SerializeField] private float dragForce = 2f;
        [SerializeField] private float spinMultiplier = 5f;

        [Header("Physics Settings")]
        [SerializeField] private float mass = 0.5f;
        [SerializeField] private float drag = 1f;
        [SerializeField] private float angularDrag = 0.5f;
        [SerializeField] private bool useGravity = false;

        [Header("Bounce Settings")]
        [SerializeField] private float bounciness = 0.4f;
        [SerializeField] private PhysicsMaterial bounceMaterial;

        [Header("Debug")]
        [SerializeField] private bool showFlickLine = true;
        [SerializeField] private Color flickLineColor = Color.yellow;

        // Components
        private Rigidbody rb;
        private Camera mainCamera;
        private Collider ballCollider;

        // Flick state
        private bool isDragging = false;
        private Vector3 dragStartPosition;
        private Vector3 currentDragPosition;
        private float dragStartTime;

        // Events
        public event System.Action<float> OnFlick; // Passes flick force
        public event System.Action OnLanded;

        public bool IsMoving => rb != null && rb.linearVelocity.magnitude > 0.1f;

        private void Awake()
        {
            SetupRigidbody();
            SetupPhysicsMaterial();
            mainCamera = Camera.main;
            ballCollider = GetComponent<Collider>();
        }

        private void Start()
        {
            if (mainCamera == null)
            {
                Debug.LogError("PaperFootballPhysics: No main camera found!");
            }
        }

        /// <summary>
        /// Sets up the Rigidbody component
        /// </summary>
        private void SetupRigidbody()
        {
            rb = GetComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = drag;
            rb.angularDamping = angularDrag;
            rb.useGravity = useGravity;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        /// <summary>
        /// Sets up physics material for bouncing
        /// </summary>
        private void SetupPhysicsMaterial()
        {
            if (ballCollider == null)
                ballCollider = gameObject.AddComponent<BoxCollider>();

            if (bounceMaterial == null)
            {
                bounceMaterial = new PhysicsMaterial("PaperFootballMaterial");
                bounceMaterial.bounciness = bounciness;
                bounceMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
                bounceMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;
            }

            ballCollider.material = bounceMaterial;
        }

        private void Update()
        {
            HandleMouseInput();
        }

        /// <summary>
        /// Handles mouse input for dragging and flicking
        /// </summary>
        private void HandleMouseInput()
        {
            if (mainCamera == null) return;

            // Start drag
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                Ray ray = mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider == ballCollider)
                    {
                        StartDrag(hit.point);
                    }
                }
            }

            // Continue drag
            if (UnityEngine.Input.GetMouseButton(0) && isDragging)
            {
                UpdateDrag();
            }

            // End drag (flick!)
            if (UnityEngine.Input.GetMouseButtonUp(0) && isDragging)
            {
                ExecuteFlick();
            }
        }

        /// <summary>
        /// Starts dragging the ball
        /// </summary>
        private void StartDrag(Vector3 worldPosition)
        {
            isDragging = true;
            dragStartPosition = worldPosition;
            currentDragPosition = worldPosition;
            dragStartTime = Time.time;

            // Stop current movement
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Debug.Log("Started dragging paper football");
        }

        /// <summary>
        /// Updates drag position
        /// </summary>
        private void UpdateDrag()
        {
            Ray ray = mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            Plane dragPlane = new Plane(Vector3.forward, transform.position);

            if (dragPlane.Raycast(ray, out float distance))
            {
                currentDragPosition = ray.GetPoint(distance);
            }
        }

        /// <summary>
        /// Executes the flick based on drag distance and speed
        /// </summary>
        private void ExecuteFlick()
        {
            if (!isDragging) return;

            Vector3 flickDirection = dragStartPosition - currentDragPosition;
            float dragTime = Time.time - dragStartTime;

            // Calculate flick force based on distance and time
            float flickDistance = flickDirection.magnitude;
            float flickSpeed = dragTime > 0 ? flickDistance / dragTime : 0;

            float flickForce = Mathf.Min(flickSpeed * flickForceMultiplier, maxFlickForce);

            if (flickForce > 0.1f)
            {
                // Apply force in the flick direction
                Vector3 forceDirection = flickDirection.normalized;
                rb.AddForce(forceDirection * flickForce, ForceMode.Impulse);

                // Add spin based on flick angle
                Vector3 torque = new Vector3(
                    -forceDirection.y * spinMultiplier,
                    0,
                    forceDirection.x * spinMultiplier
                );
                rb.AddTorque(torque, ForceMode.Impulse);

                OnFlick?.Invoke(flickForce);
                Debug.Log($"Flicked with force: {flickForce:F2}");
            }

            isDragging = false;
        }

        /// <summary>
        /// Applies a force to the ball (for external control)
        /// </summary>
        public void ApplyForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
        {
            if (rb != null)
            {
                rb.AddForce(force, mode);
            }
        }

        /// <summary>
        /// Stops all movement
        /// </summary>
        public void StopMovement()
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Sets the position without physics
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            rb.position = position;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Check if ball has stopped moving after collision
            if (rb.linearVelocity.magnitude < 0.5f)
            {
                OnLanded?.Invoke();
                Debug.Log("Paper football landed!");
            }
        }

        /// <summary>
        /// Draws the flick line in the editor
        /// </summary>
        private void OnDrawGizmos()
        {
            if (showFlickLine && isDragging)
            {
                Gizmos.color = flickLineColor;
                Gizmos.DrawLine(currentDragPosition, dragStartPosition);
                Gizmos.DrawSphere(dragStartPosition, 0.1f);
            }
        }
    }
}