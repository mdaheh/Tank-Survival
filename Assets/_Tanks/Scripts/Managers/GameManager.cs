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

        // Data about the selected tanks passed from the menu to the GameManager
        public class PlayerData
        {
            public GameObject UsedPrefab;
            public int ControlIndex;
        }

        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases.
        public GameObject m_PlayerTank;
        
        [FormerlySerializedAs("m_Tanks")] 
        public PlayerManager m_SpawnPoint;         // A collection of managers for enabling and disabling different aspects of the tanks.
        
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
            if (m_TankData == null || m_SpawnPoint == null)
            {
                Debug.LogError("GameManager: Данные танка или точка спавна не настроены!");
                return;
            }
            GameObject tankInstance = Instantiate(
                m_TankData.UsedPrefab, 
                m_SpawnPoint.m_SpawnPoint.position, 
                m_SpawnPoint.m_SpawnPoint.rotation
            );

            m_SpawnPoint.m_Instance = tankInstance;
            m_SpawnPoint.Setup();

        }


        private void SetCameraTarget()
        {
            // Проверяем, что танк уже создан
            if (m_SpawnPoint.m_Instance == null)
            {
                Debug.LogError("GameManager: Танк еще не создан для установки таргета камеры.");
                return;
            }
            // Получаем трансформацию танка
            Transform tankTransform = m_SpawnPoint.m_Instance.transform;

            m_CameraControl.m_Target = tankTransform;
        }

        private void EnableTankControl()
        {
            if (m_SpawnPoint != null && m_SpawnPoint.m_Instance != null)
            {
                m_SpawnPoint.EnableControl();
            }
        }
    }
}