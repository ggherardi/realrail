using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealRail
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] string gameplaySceneName = "SampleScene";
        [SerializeField] GameObject mainPanel;
        [SerializeField] GameObject settingsPanel;

        void Awake()
        {
            ShowMainPanel();
        }

        public void Play()
        {
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void OpenSettings()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        public void CloseSettings()
        {
            ShowMainPanel();
        }

        public void Quit()
        {
            Application.Quit();
        }

        void ShowMainPanel()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }
    }
}
