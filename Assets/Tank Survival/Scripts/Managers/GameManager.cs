using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace TankSurvival
{
    /// <summary>
    /// Главный менеджер игры — управляет состояниями, прогрессом, волнами и раундами.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public enum GameState
        {
            MainMenu,
            DifficultySelect,
            Playing,
            LevelUp,
            WaveComplete,
            RoundEnd
        }

        /// <summary>
        /// Данные, передаваемые из меню в GameManager
        /// </summary>
        [System.Serializable]
        public class PlayerData
        {
            public int chassisId;           // ID выбранного шасси (из ChassisData.id)
            public int turretId;            // ID выбранной башни (из TurretData.id)
            public GameObject chassisPrefab;
            public GameObject turretPrefab;
            public int difficultyIndex;     // 0=Easy, 1=Medium, 2=Hard
        }

        [Header("References")]
        public CameraControl m_CameraControl;
        public PlayerManager m_PlayerManager;
        public WaveManager m_WaveManager;
        public LevelManager m_LevelManager;

        [Header("UI References")]
        public GameObject m_DifficultyPanel;
        public GameObject m_LevelUpPanel;
        public GameObject m_RoundEndPanel;
        public TMPro.TMP_Text m_TotalKillsText;
        public TMPro.TMP_Text m_WaveText;

        [Header("UI Scripts")]
        public RoundEndUI m_RoundEndUI;                // Скрипт экрана окончания раунда

        [Header("Settings")]
        public List<ChassisData> allChassisData;      // Все данные шасси (заполнить в Inspector)
        public List<TurretData> allTurretData;        // Все данные башен (заполнить в Inspector)
        public List<DifficultyData> allDifficultyData; // Все данные сложности

        // --- Состояние ---
        private GameState m_CurrentState;
        private PlayerData m_CurrentPlayerData;
        private PlayerProgress m_PlayerProgress;
        private int m_CurrentDifficultyIndex;
        private int m_CurrentWaveNumber;
        private const int TOTAL_WAVES = 3; // Количество волн в раунде

        private void Awake()
        {
            // Загружаем прогресс при запуске
            m_PlayerProgress = SaveSystem.Load();
        }

        private void Start()
        {
            m_CurrentState = GameState.MainMenu;
            UpdateUI();
        }

        private void Update()
        {
            UpdateUI();
        }

        /// <summary>
        /// Обновить UI (статистика, волны)
        /// </summary>
        private void UpdateUI()
        {
            if (m_TotalKillsText != null)
                m_TotalKillsText.text = $"Убийств: {m_PlayerProgress.totalKills}";

            if (m_WaveText != null && m_CurrentState == GameState.Playing)
                m_WaveText.text = $"Волна {m_CurrentWaveNumber}/{TOTAL_WAVES}";
        }

        /// <summary>
        /// Начать игру из меню (выбор сложности)
        /// </summary>
        public void StartGameFromMenu(PlayerData playerData)
        {
            m_CurrentPlayerData = playerData;
            m_CurrentDifficultyIndex = playerData.difficultyIndex;

            // Проверяем, разблокирована ли сложность
            if (!m_PlayerProgress.IsDifficultyUnlocked(m_CurrentDifficultyIndex))
            {
                Debug.LogWarning("[GameManager] Сложность не разблокирована!");
                return;
            }

            // Скрываем меню сложности
            if (m_DifficultyPanel != null)
                m_DifficultyPanel.SetActive(false);

            // Начинаем раунд
            StartRound();
        }

        /// <summary>
        /// Начать новый раунд
        /// </summary>
        private void StartRound()
        {
            m_CurrentState = GameState.Playing;
            m_CurrentWaveNumber = 1;

            // Сброс прогресса уровня для нового раунда
            if (m_LevelManager != null)
                m_LevelManager.ResetRound();

            m_PlayerProgress.ResetSession();

            // Спавн танка
            SpawnTank();

            // Запуск первой волны
            StartWave();
        }

        /// <summary>
        /// Спавн танка игрока
        /// </summary>
        private void SpawnTank()
        {
            if (m_PlayerManager == null)
            {
                Debug.LogError("[GameManager] PlayerManager не назначен!");
                return;
            }

            GameObject chassisPrefab = m_CurrentPlayerData.chassisPrefab;
            GameObject turretPrefab = m_CurrentPlayerData.turretPrefab;

            if (chassisPrefab == null)
            {
                Debug.LogError("[GameManager] Шасси не выбрано!");
                return;
            }

            m_PlayerManager.SpawnTank(
                chassisPrefab,
                turretPrefab,
                m_PlayerManager.m_SpawnPoint.position,
                m_PlayerManager.m_SpawnPoint.rotation
            );

            m_PlayerManager.Setup();

            // Устанавливаем камеру на танк
            SetCameraTarget();

            // Включаем управление
            m_PlayerManager.EnableControl();
        }

        /// <summary>
        /// Установить камеру на танк
        /// </summary>
        private void SetCameraTarget()
        {
            if (m_PlayerManager.m_Instance == null)
            {
                Debug.LogError("[GameManager] Танк еще не создан для установки таргета камеры.");
                return;
            }

            Transform tankTransform = m_PlayerManager.m_Instance.transform;
            if (m_CameraControl != null)
                m_CameraControl.m_Target = tankTransform;
        }

        /// <summary>
        /// Начать волну
        /// </summary>
        private void StartWave()
        {
            if (m_WaveManager == null)
            {
                Debug.LogError("[GameManager] WaveManager не назначен!");
                return;
            }

            // Передаём ссылку на игрока WaveManager
            if (m_PlayerManager.m_Instance != null)
                m_WaveManager.playerTransform = m_PlayerManager.m_Instance.transform;

            DifficultyData difficulty = allDifficultyData[m_CurrentDifficultyIndex];
            m_WaveManager.StartWave(difficulty, m_CurrentWaveNumber, TOTAL_WAVES);

            // Подписываемся на события волны
            m_WaveManager.OnEnemyDied.AddListener(OnEnemyDied);
            m_WaveManager.OnWaveCompleted.AddListener(OnWaveCompleted);
        }

        /// <summary>
        /// Обработка смерти врага
        /// </summary>
        private void OnEnemyDied(int remaining)
        {
            // Увеличиваем счётчики
            m_PlayerProgress.totalKills++;
            m_PlayerProgress.currentSessionKills++;

            // Обновляем разблокировки
            m_PlayerProgress.UpdateDifficultyUnlocks(m_PlayerProgress.totalKills);
            m_PlayerProgress.UpdatePartUnlocks(allChassisData, allTurretData, m_PlayerProgress.totalKills);

            // Сохраняем прогресс
            SaveSystem.Save(m_PlayerProgress);

            // Даем XP
            if (m_LevelManager != null)
            {
                int xpAmount = 10; // Базовый XP за убийство
                m_LevelManager.AddXp(xpAmount);
            }
        }

        /// <summary>
        /// Обработка завершения волны
        /// </summary>
        private void OnWaveCompleted(int waveNumber)
        {
            m_CurrentWaveNumber++;

            // Проверяем, все ли волны пройдены
            if (m_CurrentWaveNumber > TOTAL_WAVES)
            {
                EndRound();
            }
            else
            {
                // Начинаем следующую волну через паузу
                Invoke(nameof(StartNextWave), 3f); // 3 секунды паузы
            }
        }

        /// <summary>
        /// Начать следующую волну
        /// </summary>
        private void StartNextWave()
        {
            if (m_WaveManager != null)
            {
                // Отписываемся от старой волны
                m_WaveManager.OnEnemyDied.RemoveListener(OnEnemyDied);
                m_WaveManager.OnWaveCompleted.RemoveListener(OnWaveCompleted);

                // Переподписываемся
                m_WaveManager.OnEnemyDied.AddListener(OnEnemyDied);
                m_WaveManager.OnWaveCompleted.AddListener(OnWaveCompleted);

                StartWave();
            }
        }

        /// <summary>
        /// Завершить раунд
        /// </summary>
        private void EndRound()
        {
            m_CurrentState = GameState.RoundEnd;
            m_PlayerProgress.totalGamesPlayed++;

            // Сохраняем прогресс
            SaveSystem.Save(m_PlayerProgress);

            // Показываем экран окончания раунда через RoundEndUI
            if (m_RoundEndUI != null)
                m_RoundEndUI.ShowRoundEnd(m_PlayerProgress, m_CurrentDifficultyIndex);
            else if (m_RoundEndPanel != null)
                m_RoundEndPanel.SetActive(true);
        }

        /// <summary>
        /// Начать новый раунд (кнопка "Новая игра")
        /// </summary>
        public void StartNewRound()
        {
            if (m_RoundEndPanel != null)
                m_RoundEndPanel.SetActive(false);

            // Перезагружаем сцену
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Вернуться в главное меню (кнопка "Ангар")
        /// </summary>
        public void GoToHangar()
        {
            if (m_RoundEndPanel != null)
                m_RoundEndPanel.SetActive(false);

            // Перезагружаем сцену
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Получить текущий прогресс игрока
        /// </summary>
        public PlayerProgress GetPlayerProgress()
        {
            return m_PlayerProgress;
        }

        /// <summary>
        /// Получить текущую сложность
        /// </summary>
        public int GetCurrentDifficultyIndex()
        {
            return m_CurrentDifficultyIndex;
        }

        /// <summary>
        /// Получить текущие данные игрока
        /// </summary>
        public PlayerData GetCurrentPlayerData()
        {
            return m_CurrentPlayerData;
        }
    }
}
