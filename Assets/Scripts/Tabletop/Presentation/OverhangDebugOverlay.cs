using System.Text;
using PaperFootball.Tabletop.Match;
using PaperFootball.Tabletop.Rules;
using PaperFootball.Tabletop.Scoring;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PaperFootball.Tabletop.Presentation
{
    public class OverhangDebugOverlay : MonoBehaviour
    {
        [SerializeField] private MatchController matchController;
        [SerializeField] private Text overlayText;
        [SerializeField] private bool showOnStart;
        [SerializeField] private bool explicitlyEnabledInReleaseBuild;
        [SerializeField] private Key toggleKey = Key.F3;

        private readonly StringBuilder builder = new(1024);
        private bool isVisible;
        private bool overlayAllowed;
        private OverhangDebugSnapshot? latestSnapshot;

        public bool IsVisible => isVisible;
        public bool HasSnapshot => latestSnapshot.HasValue;
        public OverhangDebugSnapshot? LatestSnapshot => latestSnapshot;

        public void Configure(MatchController controller, Text text, bool autoShow = false, Key key = Key.F3)
        {
            if (matchController != controller)
            {
                Unsubscribe();
                matchController = controller;
                Subscribe();
            }

            if (text != null)
            {
                overlayText = text;
            }

            showOnStart = autoShow;
            toggleKey = key;
            ApplyVisibility(overlayAllowed && showOnStart);
            Render();
        }

        private void Awake()
        {
            overlayAllowed = Application.isEditor || Debug.isDebugBuild || explicitlyEnabledInReleaseBuild;
            ApplyVisibility(overlayAllowed && showOnStart);
        }

        private void OnEnable()
        {
            Subscribe();
            Render();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (!overlayAllowed || toggleKey == Key.None || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                ApplyVisibility(!isVisible);
                Render();
            }
        }

        private void Subscribe()
        {
            if (matchController != null)
            {
                matchController.OverhangSnapshotChanged -= OnOverhangSnapshotChanged;
                matchController.OverhangSnapshotChanged += OnOverhangSnapshotChanged;
            }
        }

        private void Unsubscribe()
        {
            if (matchController != null)
            {
                matchController.OverhangSnapshotChanged -= OnOverhangSnapshotChanged;
            }
        }

        private void OnOverhangSnapshotChanged(OverhangDebugSnapshot snapshot)
        {
            latestSnapshot = snapshot;
            Render();
        }

        private void ApplyVisibility(bool visible)
        {
            isVisible = overlayAllowed && visible;
            if (overlayText != null)
            {
                overlayText.gameObject.SetActive(isVisible);
            }
        }

        private void Render()
        {
            if (overlayText == null || !isVisible)
            {
                return;
            }

            builder.Clear();
            builder.AppendLine("Overhang Diagnostics");

            if (!latestSnapshot.HasValue)
            {
                builder.AppendLine("No stopped-football resolution yet.");
                overlayText.text = builder.ToString();
                return;
            }

            OverhangDebugSnapshot snapshot = latestSnapshot.Value;
            builder.Append("Attacker: ").AppendLine(PaperFootballMatch.GetPlayerName(snapshot.AttackingPlayer));
            builder.Append("Edge: ").AppendLine(snapshot.AttackingEdge.ToString());
            AppendBounds("Football", snapshot.FootballBounds);
            AppendBounds("Table", snapshot.TableBounds);
            builder.Append("Overhang: ").Append(snapshot.OverhangDistance.ToString("0.0000")).Append(" (")
                .Append((snapshot.OverhangPercent * 100f).ToString("0.0")).AppendLine("%)");
            builder.Append("Supported: ").Append((snapshot.SupportedPercent * 100f).ToString("0.0")).AppendLine("%");
            builder.Append("Supported enough: ").AppendLine(FormatBool(snapshot.IsSupported));
            builder.Append("Fallen: ").AppendLine(FormatBool(snapshot.FootballFell));
            builder.Append("Positive overhang: ").AppendLine(FormatBool(snapshot.HasPositiveOverhang));
            builder.Append("Required overhang: ").Append((snapshot.RequiredOverhangPercent * 100f).ToString("0.0")).AppendLine("%");
            builder.Append("Required supported: ").Append((snapshot.RequiredSupportedPercent * 100f).ToString("0.0")).AppendLine("%");
            builder.Append("Touchdown: ").AppendLine(FormatBool(snapshot.FinalTouchdownDecision));
            builder.Append("Already processed: ").AppendLine(FormatBool(snapshot.ScoringEventAlreadyProcessed));
            overlayText.text = builder.ToString();
        }

        private void AppendBounds(string label, Bounds bounds)
        {
            builder.Append(label).Append(" bounds min(")
                .Append(bounds.min.x.ToString("0.00")).Append(", ")
                .Append(bounds.min.y.ToString("0.00")).Append(", ")
                .Append(bounds.min.z.ToString("0.00")).Append(") max(")
                .Append(bounds.max.x.ToString("0.00")).Append(", ")
                .Append(bounds.max.y.ToString("0.00")).Append(", ")
                .Append(bounds.max.z.ToString("0.00")).AppendLine(")");
        }

        private static string FormatBool(bool value)
        {
            return value ? "yes" : "no";
        }
    }
}
