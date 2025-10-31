using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace PaperFootball.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings")]
        [SerializeField] private string gameSceneName = "TableScene";

        private void Start()
        {
            // Hook up button events
            if (startButton != null)
                startButton.onClick.AddListener(OnStartGame);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettings);

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuit);
        }

        private void OnStartGame()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        private void OnSettings()
        {
            Debug.Log("Settings - Coming in Phase 6!");
            // Placeholder for Phase 6
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}