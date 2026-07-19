using PaperFootball.Tabletop.Input;
using PaperFootball.Tabletop.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace PaperFootball.Tabletop.Presentation
{
    public class GameHudController : MonoBehaviour
    {
        [SerializeField] private Text playerOneScoreText;
        [SerializeField] private Text playerTwoScoreText;
        [SerializeField] private Text currentPlayerText;
        [SerializeField] private Text phaseText;
        [SerializeField] private Text flickStrengthText;
        [SerializeField] private Text fieldGoalModeText;
        [SerializeField] private Text lastResultText;
        [SerializeField] private Text possessionText;
        [SerializeField] private Text controlsText;

        public void Configure(
            Text playerOneScore,
            Text playerTwoScore,
            Text currentPlayer,
            Text phase,
            Text flickStrength,
            Text fieldGoalMode,
            Text lastResult,
            Text possession,
            Text controls)
        {
            playerOneScoreText = playerOneScore;
            playerTwoScoreText = playerTwoScore;
            currentPlayerText = currentPlayer;
            phaseText = phase;
            flickStrengthText = flickStrength;
            fieldGoalModeText = fieldGoalMode;
            lastResultText = lastResult;
            possessionText = possession;
            controlsText = controls;
        }

        public void Render(PaperFootballMatch match)
        {
            if (match == null)
            {
                return;
            }

            SetText(playerOneScoreText, $"P1: {match.PlayerOneScore}");
            SetText(playerTwoScoreText, $"P2: {match.PlayerTwoScore}");
            SetText(currentPlayerText, $"Current: {PaperFootballMatch.GetPlayerName(match.CurrentPlayer)}");
            SetText(phaseText, $"Phase: {match.Phase}");
            SetText(fieldGoalModeText, $"Field goal: {(match.IsFieldGoalMode ? "On" : "Off")}");
            SetText(lastResultText, $"Last: {match.LastResult}");
            SetText(possessionText, $"Possession: {match.PossessionNumber}");
            SetText(controlsText, "Left mouse: select hit point, then drag/release | Field goal shows arc | R: reset ball | N: new match | Esc: cancel");
        }

        public void RenderFlick(FlickCommand command)
        {
            if (!command.IsValid)
            {
                SetText(flickStrengthText, "Flick: 0%");
                return;
            }

            SetText(flickStrengthText, $"Flick: {Mathf.RoundToInt(command.Strength01 * 100f)}%");
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
