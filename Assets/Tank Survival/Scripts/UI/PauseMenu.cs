using UnityEngine;
using UnityEngine.UI;

namespace TankSurvival
{
    // Handle a simple pause menu displaying the control and allowing to restart the game or quit it.
    public class PauseMenu : MonoBehaviour
    {
        public RectTransform m_PauseMenuRoot;           // Reference to the root Transform of the Pause Menu
        public RectTransform m_PauseMenuButtonsRoot;    // Reference to the root containing the button of the menu
        public Button m_ControlScreenButton;            // Reference to the button opening the control screen

        public RectTransform m_ControlMenuRoot;         // Reference to the root of the Control Screen
        public Button m_ControlMenuBackButton;          // Reference to the button that allow to go back to the Pause Menu from Control screen

        public Button m_SelectTankButton;               // Reference to the button that go back to tank selection
        public Button m_QuitButton;                     // Reference to the button that quit the Game

        private GameManager m_GameManager;              // Ссылка на GameManager для полного сброса

        public void Init()
        {
            // Получаем ссылку на GameManager
            m_GameManager = FindObjectOfType<GameManager>();

            // Setup clicking on the back button on the Control Screen disabling the Control Screen and re-enabling the pause menu buttons
            m_ControlMenuBackButton.onClick.AddListener(() =>
            {
                m_ControlMenuRoot.gameObject.SetActive(false);
                m_PauseMenuButtonsRoot.gameObject.SetActive(true);
            });

            m_PauseMenuButtonsRoot.gameObject.SetActive(false);
            
            // Setup clicking on the Control button enabling the Control Screen and disabling the pause menu buttons
            m_ControlScreenButton.onClick.AddListener(() =>
            {
                m_ControlMenuRoot.gameObject.SetActive(true);
                m_PauseMenuButtonsRoot.gameObject.SetActive(false);
            });

            // Setup clicking on the Tank Selection button — полный сброс через GameManager (без перезагрузки сцены)
            m_SelectTankButton.onClick.AddListener(() =>
            {
                Time.timeScale = 1.0f;
                if (m_GameManager != null)
                {
                    m_GameManager.StartNewRound();
                }
                else
                {
                    Debug.LogWarning("[PauseMenu] GameManager не найден на сцене!");
                }
            });

            // If the application is the editor or a web build, quitting the game is impossible...
            if (Application.platform == RuntimePlatform.WebGLPlayer || Application.isEditor)
            {
                // so disable the quit button
                m_QuitButton.gameObject.SetActive(false);
            }
            else
            {
                // otherwise setup the quit button to Quit the application
                m_QuitButton.gameObject.SetActive(true);
                m_QuitButton.onClick.AddListener(Application.Quit);
            }

            m_PauseMenuRoot.gameObject.SetActive(false);
            m_PauseMenuButtonsRoot.gameObject.SetActive(true);
        }
    
        public void TogglePause()
        {
            if (m_PauseMenuRoot != null)
            {
                // When toggling, swap the value of active for the pause menu root.
                bool state = !m_PauseMenuRoot.gameObject.activeSelf;
                m_PauseMenuRoot.gameObject.SetActive(state);

                // set the time scale to 0.0f, which is a simple way of "pausing" the game as everything will return 0 as 
                // delta time and nothing will move anymore.
                Time.timeScale = state ? 0.0f : 1.0f;

                m_ControlMenuRoot.gameObject.SetActive(false);
                m_PauseMenuButtonsRoot.gameObject.SetActive(true);
            }
        }
    }
}