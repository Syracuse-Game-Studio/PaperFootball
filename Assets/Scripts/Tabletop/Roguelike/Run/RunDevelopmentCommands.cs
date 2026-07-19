using PaperFootball.Tabletop.Roguelike.Variance;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaperFootball.Tabletop.Roguelike.Run
{
    public class RunDevelopmentCommands : MonoBehaviour
    {
        [SerializeField] private RunController runController;
        [SerializeField] private ShotVarianceController shotVarianceController;
        [SerializeField] private bool commandsEnabled = true;

        public void Configure(RunController run, ShotVarianceController variance, bool enabled)
        {
            runController = run;
            shotVarianceController = variance;
            commandsEnabled = enabled;
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!commandsEnabled || Keyboard.current == null || runController == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard.f5Key.wasPressedThisFrame)
            {
                runController.StartRun(runController.State.runSeed);
            }

            if (keyboard.f6Key.wasPressedThisFrame)
            {
                runController.StartRunWithRandomSeed();
            }

            if (keyboard.f9Key.wasPressedThisFrame && shotVarianceController != null)
            {
                shotVarianceController.SetVarianceEnabled(!shotVarianceController.VarianceEnabled);
            }
#endif
        }
    }
}
