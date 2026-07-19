using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PaperFootball.Tabletop.Input
{
    public class ContactPointSelector : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Collider footballCollider;
        [SerializeField] private float raycastDistance = 200f;

        public bool InputEnabled { get; set; }
        public bool HasCurrentSelection { get; private set; }
        public SelectedContactPoint CurrentSelection { get; private set; }

        public event Action<SelectedContactPoint> SelectionChanged;
        public event Action<SelectedContactPoint> SelectionConfirmed;

        public void Configure(Camera cameraReference, Collider targetFootball)
        {
            gameplayCamera = cameraReference;
            footballCollider = targetFootball;
            ClearSelection();
        }

        public void ClearSelection()
        {
            HasCurrentSelection = false;
            CurrentSelection = default;
        }

        public bool TrySelectFromRay(Ray ray, out SelectedContactPoint contactPoint)
        {
            if (footballCollider == null)
            {
                contactPoint = default;
                return false;
            }

            if (!UnityEngine.Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
            {
                contactPoint = default;
                return false;
            }

            if (!IsFootballCollider(hit.collider))
            {
                contactPoint = default;
                return false;
            }

            contactPoint = SelectedContactPoint.FromRaycastHit(hit);
            return true;
        }

        private void Update()
        {
            if (!InputEnabled || gameplayCamera == null)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || IsPointerOverUi())
            {
                return;
            }

            Ray ray = gameplayCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (TrySelectFromRay(ray, out SelectedContactPoint contactPoint))
            {
                HasCurrentSelection = true;
                CurrentSelection = contactPoint;
                SelectionChanged?.Invoke(contactPoint);

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    SelectionConfirmed?.Invoke(contactPoint);
                }
            }
            else
            {
                ClearSelection();
            }
        }

        private bool IsFootballCollider(Collider candidate)
        {
            if (candidate == null || footballCollider == null)
            {
                return false;
            }

            Transform footballTransform = footballCollider.transform;
            Transform candidateTransform = candidate.transform;
            return candidate == footballCollider ||
                   candidateTransform == footballTransform ||
                   candidateTransform.IsChildOf(footballTransform);
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
