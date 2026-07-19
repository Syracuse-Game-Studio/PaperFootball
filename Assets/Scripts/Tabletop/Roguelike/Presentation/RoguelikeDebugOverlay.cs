using System.Text;
using PaperFootball.Tabletop.Roguelike.Encounters;
using PaperFootball.Tabletop.Roguelike.Opponents;
using PaperFootball.Tabletop.Roguelike.Run;
using PaperFootball.Tabletop.Roguelike.Variance;
using UnityEngine;
using UnityEngine.UI;

namespace PaperFootball.Tabletop.Roguelike.Presentation
{
    public class RoguelikeDebugOverlay : MonoBehaviour
    {
        [SerializeField] private RunController runController;
        [SerializeField] private ShotVarianceController shotVarianceController;
        [SerializeField] private OpponentTurnController opponentTurnController;
        [SerializeField] private TableSurfaceApplier tableSurfaceApplier;
        [SerializeField] private ObstacleLayoutController obstacleLayoutController;
        [SerializeField] private Text debugText;
        [SerializeField] private bool visible = true;

        private readonly StringBuilder builder = new();

        public void Configure(
            RunController run,
            ShotVarianceController variance,
            OpponentTurnController opponent,
            TableSurfaceApplier surface,
            ObstacleLayoutController obstacles,
            Text text,
            bool show)
        {
            runController = run;
            shotVarianceController = variance;
            opponentTurnController = opponent;
            tableSurfaceApplier = surface;
            obstacleLayoutController = obstacles;
            debugText = text;
            visible = show;
        }

        private void Update()
        {
            if (debugText == null)
            {
                return;
            }

            if (!visible || runController == null)
            {
                debugText.enabled = false;
                return;
            }

            RunState state = runController.State;
            GeneratedEncounter encounter = runController.CurrentEncounter;
            ShotVarianceTuning tuning = shotVarianceController != null ? shotVarianceController.CurrentTuning : ShotVarianceTuning.Disabled;

            builder.Clear();
            builder.AppendLine("Roguelike Debug");
            builder.Append("Run status: ").Append(state.status).AppendLine();
            builder.Append("Run seed: ").Append(state.runSeed).AppendLine();
            builder.Append("Encounter index: ").Append(state.currentEncounterIndex).AppendLine();
            builder.Append("Encounter seed: ").Append(encounter != null ? encounter.seed.ToString() : "none").AppendLine();
            builder.Append("Random stream: ShotVariance ").Append(shotVarianceController != null ? shotVarianceController.LastRandomStreamSeed.ToString() : "none").AppendLine();
            builder.Append("Flick sequence: ").Append(shotVarianceController != null ? shotVarianceController.FlickSequenceNumber.ToString() : "0").AppendLine();
            builder.Append("Base/final force: ");
            if (shotVarianceController != null && shotVarianceController.LastResolved.IsValid)
            {
                builder.Append(shotVarianceController.LastResolved.BaseForce.ToString("0.00")).Append(" / ")
                    .Append(shotVarianceController.LastResolved.FinalForce.ToString("0.00")).AppendLine();
                builder.Append("Direction offset: ").Append(shotVarianceController.LastResolved.AppliedDirectionVarianceDegrees.ToString("0.00")).AppendLine();
                builder.Append("Contact jitter: ").Append(shotVarianceController.LastResolved.AppliedContactOffsetLocal.magnitude.ToString("0.0000")).AppendLine();
            }
            else
            {
                builder.AppendLine("none");
            }

            builder.Append("Variance F/D/C: ")
                .Append(tuning.ForceVariancePercent.ToString("0.000")).Append(" / ")
                .Append(tuning.DirectionVarianceDegrees.ToString("0.00")).Append(" / ")
                .Append(tuning.ContactPointVarianceRadius.ToString("0.0000")).AppendLine();
            builder.Append("Active upgrades: ").Append(state.playerBuild.ToSummary(runController.UpgradeCatalog)).AppendLine();
            builder.Append("Opponent: ").Append(opponentTurnController != null && opponentTurnController.ActiveProfile != null ? opponentTurnController.ActiveProfile.DisplayName : "none").AppendLine();
            builder.Append("Surface: ").Append(tableSurfaceApplier != null && tableSurfaceApplier.CurrentSurface != null ? tableSurfaceApplier.CurrentSurface.DisplayName : "none").AppendLine();
            builder.Append("Obstacle layout objects: ").Append(obstacleLayoutController != null ? obstacleLayoutController.ActiveObstacles.Count.ToString() : "0").AppendLine();
            builder.Append("Reward index: ").Append(runController.PendingRewards.Count);

            debugText.text = builder.ToString();
            debugText.enabled = true;
        }
    }
}
