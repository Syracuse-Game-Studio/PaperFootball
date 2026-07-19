using System;
using PaperFootball.Tabletop.Rules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaperFootball.Tabletop.Input
{
    public class FlickInputReader : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Collider footballCollider;
        [SerializeField] private float dragPlaneY = 0.16f;

        private PaperFootballRuleSet rules = new();
        private bool isDragging;
        private Vector3 dragStartWorld;
        private Vector3 dragContactWorld;
        private Vector3 contactPointOverrideWorld;
        private Vector3 currentWorld;
        private float dragStartTime;
        private bool hasContactPointOverride;

        public bool InputEnabled { get; set; } = true;
        public bool IsDragging => isDragging;
        public FlickCommand CurrentPreview { get; private set; }
        public bool HasContactPointOverride => hasContactPointOverride;
        public Vector3 ContactPointOverrideWorld => contactPointOverrideWorld;

        public event Action<FlickCommand> DragChanged;
        public event Action<FlickCommand> FlickReleased;
        public event Action ResetBallRequested;
        public event Action NewMatchRequested;
        public event Action CancelRequested;

        public void Configure(Camera cameraReference, Collider targetFootball, PaperFootballRuleSet ruleSet, float planeY)
        {
            gameplayCamera = cameraReference;
            footballCollider = targetFootball;
            rules = ruleSet != null ? ruleSet.Clone() : new PaperFootballRuleSet();
            rules.Sanitize();
            dragPlaneY = planeY;
        }

        public void SetRules(PaperFootballRuleSet ruleSet)
        {
            rules = ruleSet != null ? ruleSet.Clone() : new PaperFootballRuleSet();
            rules.Sanitize();
        }

        public void SetContactPointOverride(Vector3 worldPoint)
        {
            contactPointOverrideWorld = worldPoint;
            hasContactPointOverride = true;
        }

        public void ClearContactPointOverride()
        {
            hasContactPointOverride = false;
            contactPointOverrideWorld = Vector3.zero;
        }

        public void CancelDrag()
        {
            if (!isDragging)
            {
                return;
            }

            isDragging = false;
            CurrentPreview = FlickCommand.Invalid(dragStartWorld, currentWorld, Time.time - dragStartTime, dragContactWorld);
            DragChanged?.Invoke(CurrentPreview);
        }

        private void Awake()
        {
            rules.Sanitize();
        }

        private void Update()
        {
            ReadKeyboardShortcuts();

            if (!InputEnabled)
            {
                return;
            }

            ReadPointer();
        }

        private void ReadKeyboardShortcuts()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                ResetBallRequested?.Invoke();
            }

            if (keyboard.nKey.wasPressedThisFrame)
            {
                NewMatchRequested?.Invoke();
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelDrag();
                CancelRequested?.Invoke();
            }
        }

        private void ReadPointer()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || gameplayCamera == null || footballCollider == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUi())
            {
                TryStartDrag(mouse.position.ReadValue());
            }

            if (isDragging && mouse.leftButton.isPressed)
            {
                UpdateDrag(mouse.position.ReadValue());
            }

            if (isDragging && mouse.leftButton.wasReleasedThisFrame)
            {
                ReleaseDrag(mouse.position.ReadValue());
            }
        }

        private void TryStartDrag(Vector2 screenPosition)
        {
            Ray ray = gameplayCamera.ScreenPointToRay(screenPosition);
            if (!UnityEngine.Physics.Raycast(ray, out RaycastHit hit, 200f))
            {
                return;
            }

            if (hit.collider != footballCollider)
            {
                return;
            }

            if (!TryScreenToDragPlane(screenPosition, out dragStartWorld))
            {
                return;
            }

            isDragging = true;
            dragStartTime = Time.time;
            dragContactWorld = hasContactPointOverride ? contactPointOverrideWorld : hit.point;
            currentWorld = dragStartWorld;
            CurrentPreview = FlickCommand.Invalid(dragStartWorld, currentWorld, 0f, dragContactWorld);
            DragChanged?.Invoke(CurrentPreview);
        }

        private void UpdateDrag(Vector2 screenPosition)
        {
            if (!TryScreenToDragPlane(screenPosition, out currentWorld))
            {
                return;
            }

            CurrentPreview = FlickForceCalculator.Calculate(dragStartWorld, currentWorld, Time.time - dragStartTime, rules, dragContactWorld);
            DragChanged?.Invoke(CurrentPreview);
        }

        private void ReleaseDrag(Vector2 screenPosition)
        {
            if (TryScreenToDragPlane(screenPosition, out currentWorld))
            {
                CurrentPreview = FlickForceCalculator.Calculate(dragStartWorld, currentWorld, Time.time - dragStartTime, rules, dragContactWorld);
            }

            isDragging = false;
            FlickReleased?.Invoke(CurrentPreview);
            DragChanged?.Invoke(FlickCommand.Invalid(dragStartWorld, currentWorld, Time.time - dragStartTime, dragContactWorld));
        }

        private bool TryScreenToDragPlane(Vector2 screenPosition, out Vector3 worldPosition)
        {
            Plane plane = new(Vector3.up, new Vector3(0f, dragPlaneY, 0f));
            Ray ray = gameplayCamera.ScreenPointToRay(screenPosition);

            if (plane.Raycast(ray, out float distance))
            {
                worldPosition = ray.GetPoint(distance);
                return true;
            }

            worldPosition = Vector3.zero;
            return false;
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
