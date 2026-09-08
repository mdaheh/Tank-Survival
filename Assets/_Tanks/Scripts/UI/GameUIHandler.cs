using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Tanks.Complete
{
    // This handle both the start menu (selecting which tank each player use) and the pause menu if present
    public class GameUIHandler : MonoBehaviour
    {
        public GameManager m_GameManager;               // Reference to the GameManager in the scene
        [Header("Start Menu")] 
        public RectTransform m_StartMenuRoot;           // The GameObject root that is parent of the Start Menu
        public Button m_StartButton;                    // The Button that will start the game
        public OnScreenButton m_PauseMenuButton;        // Reference to OnScreenButton that emulate pressing a Gamepad Start button
        public GameObject m_TankPreview;
        private PauseMenu m_PauseMenu;                  // Reference to the pause menu (if present in the scene)
        private InputAction m_PauseAction;              // The InputAction that will trigger the pause menu
        private CanvasScaler m_CanvasScaler;
        private PlayerPreview m_PlayerPreview;

        [Header("Part Selection")]
        public Dropdown m_ChassisDropdown;   // Ссылка на Dropdown для выбора шасси
        public Dropdown m_TurretDropdown;    // Ссылка на Dropdown для выбора туррели
        
        // Массивы префабов, которые ты перетащишь в инспектор
        public GameObject[] m_ChassisPrefabs; 
        public GameObject[] m_TurretPrefabs;
        
        // Ссылка на объект, в котором крутится превью
        public GameObject m_PreviewContainer; 

        private int m_SelectedChassisIndex = 0;
        private int m_SelectedTurretIndex = 0;

        private void Awake()
        {
            // Получаем CanvasScaler
            m_CanvasScaler = GetComponentInParent<CanvasScaler>();
        }

        private void Start()
        {
            // Hide the Mobile UI Control if present in the scene. This will have no effect on desktop, but on mobile we
            // do not want the mobile UI control on top of the start menu
            if (MobileUIControl.Instance != null)
                MobileUIControl.Instance.Hide();

            // Setup the Start button to StartGame when clicked
            m_StartButton.onClick.AddListener(StartGame);
            // Disable the on screen pause button
            m_PauseMenuButton.gameObject.SetActive(false);
            
            m_PlayerPreview = FindAnyObjectByType<PlayerPreview>(FindObjectsInactive.Include);
            // tank preview
            m_PlayerPreview.SetTankPreview(m_TankPreview);

            // Pause Menu
            m_PauseMenu = FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
            if (m_PauseMenu != null)
            {
                m_PauseMenu.Init();
                //clone the action so it doesn't change the default one
                m_PauseAction = InputSystem.actions.FindAction("Pause").Clone();
                var rectTransform = m_PauseMenuButton.GetComponent<RectTransform>();
                //force the button to be on top of everything so it can be clicked no matter what other screen is shown 
                rectTransform.SetAsLastSibling();
            }

            // We use an array because the code was originally written to have any number of prefabs and player, but
            // this was fixed to always 4 tanks during development, so to avoid rewriting the code for static number,
            // we simply transform our 4 static tank prefab into an array
            var tankPrefab = m_GameManager.m_PlayerTank;
        }

        void StartGame()
        {
            // When starting the game, we disable the Start Menu
            m_StartMenuRoot.gameObject.SetActive(false);

            // PlayerData is a structure that allow to pass info between the menu and the GameManager
            GameManager.PlayerData playerData = new GameManager.PlayerData()
            {
                UsedPrefab = m_TankPreview
            };

            m_GameManager.StartGame(playerData);

            // If there was a Mobile UI Control, we now show it again (on desktop this will do nothing)
            if (MobileUIControl.Instance != null)
                MobileUIControl.Instance.Show();

            // If there is a pause menu, we re-enable the on screen pause button and listen to the pause action to
            // display the pause menu when pressed
            if (m_PauseMenu != null)
            {
                m_PauseAction.performed += evt => { TogglePause(); };
                m_PauseAction.Enable();
                
                m_PauseMenuButton.gameObject.SetActive(true);
            }
        }
        
        private void TogglePause()
        {
            m_PauseMenu.TogglePause();
        }

        private void Update()
        {
            // This help keeping the UI readable in both portrait and landscape mode (game should only be played in landscape
            // but Unity Play cannot enforce an orientation so we need it to be readable even in portrait)
            float ratio = Screen.width / (float)Screen.height;
            m_CanvasScaler.matchWidthOrHeight = ratio > 1.0f ? 1.0f : 0.0f;
        }
    }
}