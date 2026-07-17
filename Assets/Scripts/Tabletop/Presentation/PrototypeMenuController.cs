using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PaperFootball.Tabletop.Presentation
{
    public class PrototypeMenuController : MonoBehaviour
    {
        [SerializeField] private Button startPrototypeButton;
        [SerializeField] private Button legacyMenuButton;
        [SerializeField] private Button legacyTableButton;
        [SerializeField] private string prototypeSceneName = "PaperFootballGame";
        [SerializeField] private string legacyMenuSceneName = "MainMenu";
        [SerializeField] private string legacyTableSceneName = "TableScene";

        public void Configure(
            Button prototypeButton,
            Button menuButton,
            Button tableButton,
            string prototypeScene,
            string menuScene,
            string tableScene)
        {
            startPrototypeButton = prototypeButton;
            legacyMenuButton = menuButton;
            legacyTableButton = tableButton;
            prototypeSceneName = prototypeScene;
            legacyMenuSceneName = menuScene;
            legacyTableSceneName = tableScene;
        }

        private void Awake()
        {
            WireButtons();
        }

        private void OnEnable()
        {
            WireButtons();
        }

        private void WireButtons()
        {
            if (startPrototypeButton != null)
            {
                startPrototypeButton.onClick.RemoveListener(LoadPrototype);
                startPrototypeButton.onClick.AddListener(LoadPrototype);
            }

            if (legacyMenuButton != null)
            {
                legacyMenuButton.onClick.RemoveListener(LoadLegacyMenu);
                legacyMenuButton.onClick.AddListener(LoadLegacyMenu);
            }

            if (legacyTableButton != null)
            {
                legacyTableButton.onClick.RemoveListener(LoadLegacyTable);
                legacyTableButton.onClick.AddListener(LoadLegacyTable);
            }
        }

        private void LoadPrototype()
        {
            SceneManager.LoadScene(prototypeSceneName);
        }

        private void LoadLegacyMenu()
        {
            SceneManager.LoadScene(legacyMenuSceneName);
        }

        private void LoadLegacyTable()
        {
            SceneManager.LoadScene(legacyTableSceneName);
        }
    }
}
