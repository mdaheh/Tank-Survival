using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Tanks.Complete
{
    public class GameManager : MonoBehaviour
    {
        // Which state the game is currently in
        public enum GameState
        {
            MainMenu,
            Game
        }

        // Хранит необходимые данные о игроке
        public class PlayerData
        {
            public GameObject ChassisPrefab;  // Префаб шасси
            public GameObject TurretPrefab;   // Префаб туррели
            public int ControlIndex;
        }

        public CameraControl m_CameraControl;       // Скрипт управления камерой
        public GameObject m_PlayerTank;
        
        [FormerlySerializedAs("m_Tanks")] 
        public PlayerManager m_PlayerManager;         // A collection of managers for enabling and disabling different aspects of the tanks.
        
        private GameState m_CurrentState;

        private PlayerData m_TankData;            // Data passed from the menu about each selected tank (at least 2, max 4)

        private void Start()
        {
            m_CurrentState = GameState.MainMenu;
        }

        void GameStart()
        {
            SpawnTank();
            SetCameraTarget();

            // Once the tanks have been created and the camera is using them as targets, start the game.
            //StartCoroutine (GameLoop ());
            EnableTankControl ();
        }

        void ChangeGameState(GameState newState)
        {
            m_CurrentState = newState;

            switch (m_CurrentState)
            {
                case GameState.Game:
                    GameStart();
                    break;
            }
        }

        // Called by the menu, passing along the data from the selection made by the player in the menu
        public void StartGame(PlayerData playerData)
        {
            m_TankData = playerData;
            ChangeGameState(GameState.Game);
        }


        private void SpawnTank()
        {
            if (m_TankData == null || m_PlayerManager == null)
            {
                Debug.LogError("GameManager: Данные танка или точка спавна не настроены!");
                return;
            }

            // Spawn the tank (chassis + turret)
            m_PlayerManager.SpawnTank(
                m_TankData.ChassisPrefab,
                m_TankData.TurretPrefab,
                m_PlayerManager.m_SpawnPoint.position,
                m_PlayerManager.m_SpawnPoint.rotation
            );

            m_PlayerManager.Setup(m_TankData.ControlIndex);

        }


        private void SetCameraTarget()
        {
            // Проверяем, что танк уже создан
            if (m_PlayerManager.m_Instance == null)
            {
                Debug.LogError("GameManager: Танк еще не создан для установки таргета камеры.");
                return;
            }
            // Получаем трансформацию танка
            Transform tankTransform = m_PlayerManager.m_Instance.transform;

            m_CameraControl.m_Target = tankTransform;
        }

        private void EnableTankControl()
        {
            if (m_PlayerManager != null && m_PlayerManager.m_Instance != null)
            {
                m_PlayerManager.EnableControl();
            }
        }
    }
}