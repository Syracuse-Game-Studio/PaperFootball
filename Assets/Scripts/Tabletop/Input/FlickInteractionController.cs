using System;
using PaperFootball.Tabletop.Physics;
using PaperFootball.Tabletop.Presentation;
using PaperFootball.Tabletop.Rules;
using UnityEngine;

namespace PaperFootball.Tabletop.Input
{
    public class FlickInteractionController : MonoBehaviour
    {
        [SerializeField] private ContactPointSelector contactPointSelector;
        [SerializeField] private FlickInputReader flickInputReader;
        [SerializeField] private FootballCameraController cameraController;
        [SerializeField] private ContactPointIndicator contactPointIndicator;
        [SerializeField] private FootballPhysicsController footballPhysics;
        [SerializeField] private Collider footballCollider;
        [SerializeField] private bool requireContactSelectionForFieldGoals = true;

        private readonly FlickInteractionStateMachine stateMachine = new();
        private SelectedContactPoint selectedContactPoint;
        private bool hasSelectedContactPoint;
        private PaperFootballPlayer? lastPlayer;
        private MatchPhase? lastPhase;
        private bool isSubscribed;

        public FlickInteractionState State => stateMachine.CurrentState;
        public bool HasSelectedContactPoint => hasSelectedContactPoint;
        public SelectedContactPoint SelectedContactPoint => selectedContactPoint;

        public event Action<FlickCommand> DragChanged;
        public event Action<FlickCommand> FlickReleased;
        public event Action ResetBallRequested;
        public event Action NewMatchRequested;
        public event Action CancelRequested;
        public event Action<FlickInteractionState> StateChanged;

        public void Configure(
            ContactPointSelector selector,
            FlickInputReader inputReader,
            FootballCameraController camera,
            ContactPointIndicator indicator,
            FootballPhysicsController physicsController,
            Collider football)
        {
            Unsubscribe();
            contactPointSelector = selector;
            flickInputReader = inputReader;
            cameraController = camera;
            contactPointIndicator = indicator;
            footballPhysics = physicsController;
            footballCollider = football;
            if (footballCollider == null && footballPhysics != null)
            {
                footballCollider = footballPhysics.GetComponent<Collider>();
            }

            RefreshInputOverride();
            ApplyStateSideEffects();

            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        public void ApplyMatchState(PaperFootballMatch match)
        {
            if (match == null)
            {
                EnterDisabled(true);
                return;
            }

            MatchPhase phase = match.Phase;
            bool playerChanged = lastPlayer.HasValue && lastPlayer.Value != match.CurrentPlayer;
            bool phaseChanged = lastPhase.HasValue && lastPhase.Value != phase;
            lastPlayer = match.CurrentPlayer;
            lastPhase = phase;

            if (phase == MatchPhase.WaitingForFlick || (requireContactSelectionForFieldGoals && phase == MatchPhase.FieldGoalSetup))
            {
                if (playerChanged || phaseChanged || !hasSelectedContactPoint)
                {
                    BeginContactSelection();
                }
                else
                {
                    SetState(FlickInteractionState.WaitingForFlick);
                    RefreshInputOverride();
                }

                return;
            }

            if (!requireContactSelectionForFieldGoals && phase == MatchPhase.FieldGoalSetup)
            {
                EnterLegacyFlickInput();
                return;
            }

            if (phase == MatchPhase.FootballMoving ||
                phase == MatchPhase.FieldGoalAttempt ||
                phase == MatchPhase.ResolvingFlick ||
                phase == MatchPhase.ChangingPossession ||
                phase == MatchPhase.TouchdownScored)
            {
                EnterResolving();
                return;
            }

            EnterDisabled(phase == MatchPhase.MatchComplete);
        }

        public void ClearSelection()
        {
            hasSelectedContactPoint = false;
            selectedContactPoint = default;
            contactPointSelector?.ClearSelection();
            contactPointIndicator?.Hide();
            flickInputReader?.ClearContactPointOverride();
        }

        public bool TryConfirmContactPoint(SelectedContactPoint contactPoint)
        {
            if (State != FlickInteractionState.SelectingContact && State != FlickInteractionState.WaitingForContact)
            {
                return false;
            }

            selectedContactPoint = contactPoint;
            hasSelectedContactPoint = contactPoint.IsValid;
            if (!hasSelectedContactPoint)
            {
                BeginContactSelection();
                return false;
            }

            SetState(FlickInteractionState.WaitingForFlick);
            cameraController?.ShowTabletopView();
            RefreshInputOverride();
            ApplyStateSideEffects();
            contactPointIndicator?.Show(selectedContactPoint);
            return true;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (State == FlickInteractionState.WaitingForFlick && hasSelectedContactPoint)
            {
                RefreshInputOverride();
                if (flickInputReader != null)
                {
                    flickInputReader.InputEnabled = cameraController == null || !cameraController.IsTransitioning;
                }
            }
        }

        private void Subscribe()
        {
            if (isSubscribed)
            {
                return;
            }

            if (contactPointSelector != null)
            {
                contactPointSelector.SelectionChanged += OnContactSelectionChanged;
                contactPointSelector.SelectionConfirmed += OnContactSelectionConfirmed;
            }

            if (flickInputReader != null)
            {
                flickInputReader.DragChanged += OnDragChanged;
                flickInputReader.FlickReleased += OnFlickReleased;
                flickInputReader.ResetBallRequested += OnResetBallRequested;
                flickInputReader.NewMatchRequested += OnNewMatchRequested;
                flickInputReader.CancelRequested += OnCancelRequested;
            }

            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (contactPointSelector != null)
            {
                contactPointSelector.SelectionChanged -= OnContactSelectionChanged;
                contactPointSelector.SelectionConfirmed -= OnContactSelectionConfirmed;
            }

            if (flickInputReader != null)
            {
                flickInputReader.DragChanged -= OnDragChanged;
                flickInputReader.FlickReleased -= OnFlickReleased;
                flickInputReader.ResetBallRequested -= OnResetBallRequested;
                flickInputReader.NewMatchRequested -= OnNewMatchRequested;
                flickInputReader.CancelRequested -= OnCancelRequested;
            }

            isSubscribed = false;
        }

        private void BeginContactSelection()
        {
            ClearSelection();
            SetState(FlickInteractionState.WaitingForContact);
            cameraController?.ShowContactSelectionView();
            ApplyStateSideEffects();
        }

        private void EnterLegacyFlickInput()
        {
            ClearSelection();
            SetState(FlickInteractionState.WaitingForFlick);
            cameraController?.ShowTabletopView();
            if (flickInputReader != null)
            {
                flickInputReader.InputEnabled = cameraController == null || !cameraController.IsTransitioning;
            }
        }

        private void EnterResolving()
        {
            SetState(FlickInteractionState.Resolving);
            if (flickInputReader != null)
            {
                flickInputReader.InputEnabled = false;
                flickInputReader.CancelDrag();
                flickInputReader.ClearContactPointOverride();
            }

            if (contactPointSelector != null)
            {
                contactPointSelector.InputEnabled = false;
            }

            contactPointIndicator?.Hide();
            cameraController?.ShowResolutionView();
        }

        private void EnterDisabled(bool clearSelection)
        {
            if (clearSelection)
            {
                ClearSelection();
            }

            SetState(FlickInteractionState.Disabled);
            ApplyStateSideEffects();
        }

        private void OnContactSelectionChanged(SelectedContactPoint contactPoint)
        {
            if (State != FlickInteractionState.WaitingForContact && State != FlickInteractionState.SelectingContact)
            {
                return;
            }

            SetState(FlickInteractionState.SelectingContact);
            contactPointIndicator?.Show(contactPoint);
        }

        private void OnContactSelectionConfirmed(SelectedContactPoint contactPoint)
        {
            TryConfirmContactPoint(contactPoint);
        }

        private void OnDragChanged(FlickCommand command)
        {
            if (State == FlickInteractionState.WaitingForFlick && command.IsValid)
            {
                SetState(FlickInteractionState.SelectingFlick);
            }
            else if (State == FlickInteractionState.SelectingFlick && !command.IsValid)
            {
                SetState(FlickInteractionState.WaitingForFlick);
            }

            if (hasSelectedContactPoint && command.IsValid)
            {
                contactPointIndicator?.ShowFlickPreview(selectedContactPoint, command.Direction);
            }

            DragChanged?.Invoke(command);
        }

        private void OnFlickReleased(FlickCommand command)
        {
            if (command.IsValid && (State == FlickInteractionState.SelectingFlick || State == FlickInteractionState.WaitingForFlick))
            {
                SetState(FlickInteractionState.Resolving);
                if (flickInputReader != null)
                {
                    flickInputReader.InputEnabled = false;
                    flickInputReader.ClearContactPointOverride();
                }

                if (contactPointSelector != null)
                {
                    contactPointSelector.InputEnabled = false;
                }
                contactPointIndicator?.Hide();
                cameraController?.ShowResolutionView();
            }
            else if (!command.IsValid && State == FlickInteractionState.SelectingFlick)
            {
                SetState(FlickInteractionState.WaitingForFlick);
            }

            FlickReleased?.Invoke(command);
        }

        private void OnResetBallRequested()
        {
            ClearSelection();
            cameraController?.ShowTabletopView();
            ResetBallRequested?.Invoke();
        }

        private void OnNewMatchRequested()
        {
            ClearSelection();
            cameraController?.ShowTabletopView();
            NewMatchRequested?.Invoke();
        }

        private void OnCancelRequested()
        {
            if (State == FlickInteractionState.SelectingContact)
            {
                BeginContactSelection();
            }
            else if (State == FlickInteractionState.SelectingFlick)
            {
                SetState(FlickInteractionState.WaitingForFlick);
                contactPointIndicator?.Show(selectedContactPoint);
            }

            CancelRequested?.Invoke();
        }

        private void RefreshInputOverride()
        {
            if (flickInputReader == null)
            {
                return;
            }

            if (hasSelectedContactPoint)
            {
                flickInputReader.SetContactPointOverride(selectedContactPoint.GetWorldPoint());
            }
            else
            {
                flickInputReader.ClearContactPointOverride();
            }
        }

        private void ApplyStateSideEffects()
        {
            if (contactPointSelector != null)
            {
                contactPointSelector.InputEnabled = State == FlickInteractionState.WaitingForContact ||
                                                    State == FlickInteractionState.SelectingContact;
            }

            if (flickInputReader != null)
            {
                flickInputReader.InputEnabled = State == FlickInteractionState.WaitingForFlick ||
                                                State == FlickInteractionState.SelectingFlick;
            }
        }

        private void SetState(FlickInteractionState state)
        {
            if (!stateMachine.TryTransitionTo(state))
            {
                stateMachine.Reset(state);
            }

            StateChanged?.Invoke(stateMachine.CurrentState);
        }
    }
}
