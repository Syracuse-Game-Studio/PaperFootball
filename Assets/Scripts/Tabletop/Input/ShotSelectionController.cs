using System;
using PaperFootball.Tabletop.Rules;
using PaperFootball.Tabletop.Shots;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaperFootball.Tabletop.Input
{
    public class ShotSelectionController : MonoBehaviour
    {
        [SerializeField] private Button flatShotButton;
        [SerializeField] private Button airFlickShotButton;
        [SerializeField] private Text selectedShotText;
        [SerializeField] private Text shotDescriptionText;
        [SerializeField] private FootballShotType selectedNormalShotType = FootballShotType.FlatTableShot;

        private bool canSelectNormalShot;
        private bool buttonsWired;

        public FootballShotType SelectedNormalShotType => selectedNormalShotType == FootballShotType.AirFlickShot
            ? FootballShotType.AirFlickShot
            : FootballShotType.FlatTableShot;
        public FootballShotType DisplayedShotType { get; private set; } = FootballShotType.FlatTableShot;
        public bool CanSelectNormalShot => canSelectNormalShot;

        public event Action<FootballShotType> NormalShotTypeChanged;

        public static ShotSelectionController CreateRuntimeHud(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            GameObject root = GetOrCreateUiChild("ShotSelectionController", parent);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(410f, -24f);
            rootRect.sizeDelta = new Vector2(560f, 210f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Text label = ConfigureText("SelectedShotLabel", root.transform, new Vector2(0f, 0f), font, 24, TextAnchor.UpperLeft);
            label.rectTransform.sizeDelta = new Vector2(320f, 34f);

            Text description = ConfigureText("ShotDescription", root.transform, new Vector2(0f, -36f), font, 17, TextAnchor.UpperLeft);
            description.rectTransform.sizeDelta = new Vector2(520f, 84f);
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;

            Button flat = ConfigureButton("FlatShotButton", root.transform, new Vector2(0f, -142f), "1 Flat", font, new Color(0.08f, 0.44f, 0.58f));
            Button air = ConfigureButton("AirFlickShotButton", root.transform, new Vector2(180f, -142f), "2 Flick", font, new Color(0.56f, 0.36f, 0.08f));

            ShotSelectionController selector = root.GetComponent<ShotSelectionController>();
            if (selector == null)
            {
                selector = root.AddComponent<ShotSelectionController>();
            }

            selector.Configure(flat, air, label, description);
            return selector;
        }

        public void Configure(Button flatButton, Button airButton, Text selectedLabel, Text descriptionLabel)
        {
            UnwireButtons();
            flatShotButton = flatButton;
            airFlickShotButton = airButton;
            selectedShotText = selectedLabel;
            shotDescriptionText = descriptionLabel;
            WireButtons();
            RefreshDisplay();
        }

        public bool TrySelectNormalShot(FootballShotType shotType)
        {
            if (!canSelectNormalShot)
            {
                return false;
            }

            FootballShotType normalized = shotType == FootballShotType.AirFlickShot
                ? FootballShotType.AirFlickShot
                : FootballShotType.FlatTableShot;
            if (selectedNormalShotType == normalized)
            {
                RefreshDisplay();
                return true;
            }

            selectedNormalShotType = normalized;
            RefreshDisplay();
            NormalShotTypeChanged?.Invoke(selectedNormalShotType);
            return true;
        }

        public void ResetNormalShotType()
        {
            if (selectedNormalShotType == FootballShotType.FlatTableShot)
            {
                RefreshDisplay();
                return;
            }

            selectedNormalShotType = FootballShotType.FlatTableShot;
            RefreshDisplay();
            NormalShotTypeChanged?.Invoke(selectedNormalShotType);
        }

        public void ApplyMatchState(PaperFootballMatch match, bool inputSuppressed, FlickInteractionState interactionState)
        {
            MatchPhase phase = match != null ? match.Phase : MatchPhase.MatchComplete;
            bool isFieldGoal = phase == MatchPhase.FieldGoalSetup || phase == MatchPhase.FieldGoalAttempt;
            DisplayedShotType = isFieldGoal ? FootballShotType.FieldGoalKick : SelectedNormalShotType;

            canSelectNormalShot = !inputSuppressed &&
                                  phase == MatchPhase.WaitingForFlick &&
                                  (interactionState == FlickInteractionState.WaitingForContact ||
                                   interactionState == FlickInteractionState.SelectingContact ||
                                   interactionState == FlickInteractionState.WaitingForFlick);
            RefreshDisplay();
        }

        private void Awake()
        {
            WireButtons();
            RefreshDisplay();
        }

        private void OnEnable()
        {
            WireButtons();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        private void Update()
        {
            if (!canSelectNormalShot)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            {
                TrySelectNormalShot(FootballShotType.FlatTableShot);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            {
                TrySelectNormalShot(FootballShotType.AirFlickShot);
            }
        }

        private void WireButtons()
        {
            if (buttonsWired)
            {
                return;
            }

            if (flatShotButton != null)
            {
                flatShotButton.onClick.AddListener(SelectFlatShot);
            }

            if (airFlickShotButton != null)
            {
                airFlickShotButton.onClick.AddListener(SelectAirFlickShot);
            }

            buttonsWired = true;
        }

        private void UnwireButtons()
        {
            if (!buttonsWired)
            {
                return;
            }

            if (flatShotButton != null)
            {
                flatShotButton.onClick.RemoveListener(SelectFlatShot);
            }

            if (airFlickShotButton != null)
            {
                airFlickShotButton.onClick.RemoveListener(SelectAirFlickShot);
            }

            buttonsWired = false;
        }

        private void SelectFlatShot()
        {
            TrySelectNormalShot(FootballShotType.FlatTableShot);
        }

        private void SelectAirFlickShot()
        {
            TrySelectNormalShot(FootballShotType.AirFlickShot);
        }

        private void RefreshDisplay()
        {
            if (flatShotButton != null)
            {
                flatShotButton.interactable = canSelectNormalShot && SelectedNormalShotType != FootballShotType.FlatTableShot;
            }

            if (airFlickShotButton != null)
            {
                airFlickShotButton.interactable = canSelectNormalShot && SelectedNormalShotType != FootballShotType.AirFlickShot;
            }

            if (selectedShotText != null)
            {
                selectedShotText.text = BuildShotLabel(DisplayedShotType);
            }

            if (shotDescriptionText != null)
            {
                shotDescriptionText.text = BuildDescription(DisplayedShotType);
            }
        }

        private static string BuildShotLabel(FootballShotType shotType)
        {
            return shotType switch
            {
                FootballShotType.AirFlickShot => "SHOT: FLICK",
                FootballShotType.FieldGoalKick => "SHOT: FIELD GOAL",
                _ => "SHOT: FLAT"
            };
        }

        private static string BuildDescription(FootballShotType shotType)
        {
            return shotType switch
            {
                FootballShotType.AirFlickShot => "Launches over obstacles.\nHarder to control after landing.\nCannot score a field goal.",
                FootballShotType.FieldGoalKick => "Field-goal attempt only.\nNormal shot modes are locked.",
                _ => "Travels along the table.\nMore accurate and predictable.\nCannot pass over solid obstacles."
            };
        }

        private static GameObject GetOrCreateUiChild(string name, Transform parent)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject child = new(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Text ConfigureText(string name, Transform parent, Vector2 anchoredPosition, Font font, int fontSize, TextAnchor anchor)
        {
            GameObject textObject = GetOrCreateUiChild(name, parent);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;

            Text text = textObject.GetComponent<Text>();
            if (text == null)
            {
                text = textObject.AddComponent<Text>();
            }

            text.font = font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button ConfigureButton(string name, Transform parent, Vector2 anchoredPosition, string label, Font font, Color color)
        {
            GameObject buttonObject = GetOrCreateUiChild(name, parent);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(160f, 48f);

            Image image = buttonObject.GetComponent<Image>();
            if (image == null)
            {
                image = buttonObject.AddComponent<Image>();
            }

            image.color = color;
            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                button = buttonObject.AddComponent<Button>();
            }

            Text text = ConfigureText($"{name}Text", buttonObject.transform, Vector2.zero, font, 20, TextAnchor.MiddleCenter);
            text.text = label;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            text.rectTransform.anchoredPosition = Vector2.zero;
            text.rectTransform.sizeDelta = Vector2.zero;
            return button;
        }
    }
}
