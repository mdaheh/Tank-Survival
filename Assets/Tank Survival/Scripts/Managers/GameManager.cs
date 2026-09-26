using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
            public int chassisId;           // ID выбранного шасси
            public int turretId;            // ID выбранной башни
            public int difficultyIndex;     // 0=Easy, 1=Medium, 2=Hard
        }

        [Header("References")]
        public CameraControl m_CameraControl;
        public PlayerManager m_PlayerManager;
        public WaveController m_WaveController;
        public LevelManager m_LevelManager;

        [Header("UI References")]
        public GameObject m_DifficultyPanel;
        public GameObject m_LevelUpPanel;
        public GameObject m_RoundEndPanel;
        public TMPro.TMP_Text m_TotalKillsText;
        public TMPro.TMP_Text m_WaveText;

        [Header("UI Scripts")]
        public RoundEndUI m_RoundEndUI;                // Скрипт экрана окончания раунда

        private TankHealth m_PlayerHealth;              // Ссылка на здоровье игрока (для события смерти)
        private bool m_IsVictory;                       // true = победа (все волны раунда пройдены, T031), false = поражение (смерть игрока)
        [SerializeField] private RunContext m_RunContext; // Состояние забега (T018)

        // --- Состояние ---
        private GameState m_CurrentState;
        private PlayerData m_CurrentPlayerData;
        private PlayerProgress m_PlayerProgress;
        private int m_CurrentDifficultyIndex;
        // T031: счётчик волн и их число в раунде — у WaveController (WaveData.waveCount)

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

            // T031: номер волны и их число — из WaveController (число волн — данные сложности)
            if (m_WaveText != null && m_CurrentState == GameState.Playing && m_WaveController != null)
                m_WaveText.text = $"Волна {m_WaveController.currentWave}/{m_WaveController.TotalWaveCount}";
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

            // Создаём RunContext (T018)
            m_RunContext = new RunContext
            {
                selectedChassisId = m_CurrentPlayerData.chassisId,
                selectedTurretId = m_CurrentPlayerData.turretId,
                selectedDifficultyIndex = m_CurrentDifficultyIndex
            };

            // Сброс прогресса уровня для нового раунда
            if (m_LevelManager != null)
                m_LevelManager.ResetRound();

            m_PlayerProgress.ResetSession();

            // Спавн танка
            SpawnTank();

            // Запуск волнового раунда (переходы между волнами — внутри WaveController)
            StartWaveRound();
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

            // T019: передаём StatBlock забега до сборки танка — итоговые статы читаются из него
            m_PlayerManager.SetStatBlock(m_RunContext != null ? m_RunContext.statBlock : null);

            m_PlayerManager.SpawnTank(
                m_CurrentPlayerData.chassisId,
                m_CurrentPlayerData.turretId,
                m_PlayerManager.m_SpawnPoint.position,
                m_PlayerManager.m_SpawnPoint.rotation
            );

            m_PlayerManager.Setup();

            // Подписываемся на смерть игрока через DamageSystem (T022)
            m_PlayerHealth = m_PlayerManager.m_Instance.GetComponent<TankHealth>();
            if (m_PlayerHealth != null)
            {
                DamageSystem.RegisterPlayer(m_PlayerHealth);
            }

            // T031: прогрессом волн владеет WaveController, поэтому GameManager слушает только
            // сигнал «все волны раунда пройдены» — им раунд завершается победой.
            if (m_WaveController != null)
            {
                m_WaveController.OnAllWavesCompleted.RemoveListener(OnAllWavesCompleted);
                m_WaveController.OnAllWavesCompleted.AddListener(OnAllWavesCompleted);
            }

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
        /// Начать волновой раунд
        /// </summary>
        private void StartWaveRound()
        {
            if (m_WaveController == null)
            {
                Debug.LogError("[GameManager] WaveController не назначен!");
                return;
            }

            // Передаём ссылку на игрока WaveController (для спавна врагов вокруг игрока)
            if (m_PlayerManager.m_Instance != null)
                m_WaveController.playerTransform = m_PlayerManager.m_Instance.transform;

            // T031: числа волн и число волн в раунде — из данных сложности (DifficultyData → WaveData)
            DifficultyData difficulty = DataCatalog.GetAllDifficulties()[m_CurrentDifficultyIndex];
            m_WaveController.StartRound(difficulty);

            // T022: подписка на события через DamageSystem вместо UnityEvent
            DamageSystem.OnEnemyKilled += OnEnemyKilled;
            DamageSystem.OnPlayerKilled += OnPlayerKilled;
        }

        /// <summary>
        /// Обработка убийства врага (T022: через DamageSystem)
        /// </summary>
        private void OnEnemyKilled(IDamageable damageable)
        {
            // Увеличиваем счётчики
            m_PlayerProgress.totalKills++;
            m_PlayerProgress.currentSessionKills++;

            // Обновляем разблокировки
            m_PlayerProgress.UpdateDifficultyUnlocks(m_PlayerProgress.totalKills);
            m_PlayerProgress.UpdatePartUnlocks(DataCatalog.GetAllChassis(), DataCatalog.GetAllTurrets(), m_PlayerProgress.totalKills);

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
        /// Обработка смерти игрока (T022: через DamageSystem)
        /// </summary>
        private void OnPlayerKilled()
        {
            m_IsVictory = false;
            EndRound();
        }

        /// <summary>
        /// Обработка завершения всех волн раунда (T031: сигнал WaveController).
        /// Переход между волнами и пауза между ними — внутри WaveController.
        /// </summary>
        private void OnAllWavesCompleted()
        {
            m_IsVictory = true;
            EndRound();
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
                m_RoundEndUI.ShowRoundEnd(m_PlayerProgress, m_CurrentDifficultyIndex, m_IsVictory);
            else if (m_RoundEndPanel != null)
                m_RoundEndPanel.SetActive(true);
        }

        /// <summary>
        /// Начать новый раунд (кнопка "Новая игра")
        /// </summary>
        public void StartNewRound()
        {
            FullReset();
        }

        /// <summary>
        /// Вернуться в главное меню (кнопка "Ангар")
        /// </summary>
        public void GoToHangar()
        {
            FullReset();
        }

        /// <summary>
        /// Полный сброс состояния без перезагрузки сцены
        /// </summary>
        private void FullReset()
        {
            // Скрываем панель окончания раунда
            if (m_RoundEndPanel != null)
                m_RoundEndPanel.SetActive(false);

            // 1. Останавливаем волны и чистим врагов
            if (m_WaveController != null)
            {
                // T022/T031 (регрессия): снимаем слушатель ДО ForceEndWave — иначе
                // принудительное завершение волны завершит раунд уже после сброса.
                m_WaveController.OnAllWavesCompleted.RemoveListener(OnAllWavesCompleted);

                m_WaveController.ForceEndWave();
                m_WaveController.ClearWaveEnemies();
                // T022: отписка от DamageSystem
                DamageSystem.OnEnemyKilled -= OnEnemyKilled;
                DamageSystem.OnPlayerKilled -= OnPlayerKilled;
            }

            // 2. Деспавн танка игрока
            if (m_PlayerManager != null)
            {
                m_PlayerManager.DisableControl();
                
                // T022: отписка от DamageSystem
                if (m_PlayerHealth != null)
                {
                    DamageSystem.UnregisterPlayer(m_PlayerHealth);
                    m_PlayerHealth = null;
                }
                
                if (m_PlayerManager.m_Instance != null)
                {
                    UnityEngine.Object.Destroy(m_PlayerManager.m_Instance);
                    m_PlayerManager.m_Instance = null;
                }
                if (m_PlayerManager.m_TurretInstance != null)
                {
                    UnityEngine.Object.Destroy(m_PlayerManager.m_TurretInstance);
                    m_PlayerManager.m_TurretInstance = null;
                }
            }

            // 3. Сброс состояния игры
            m_CurrentState = GameState.MainMenu;
            // T085/T031: счётчик волн раунда сбрасывает WaveController.ClearWaveEnemies() — в меню раунд не идёт

            m_PlayerProgress.ResetSession();

            // T085: сбрасываем прогресс уровня/XP раунда — иначе в меню остаётся XP прошлого забега
            if (m_LevelManager != null)
                m_LevelManager.ResetRound();

            // T018: сброс RunContext
            if (m_RunContext != null)
            {
                m_RunContext.Reset();
                m_RunContext = null;
            }

            // 4. Показываем меню выбора сложности
            if (m_DifficultyPanel != null)
                m_DifficultyPanel.SetActive(true);

            // 5. Сбрасываем HUD
            UpdateUI();

            // T067/T085: возвращаем превью, dropdown'ы и КОРЕНЬ стартового меню.
            // Корень меню скрывается GameUIHandler.StartGame() при старте забега, а
            // ShowPreviewAndDropdowns() восстанавливает только превью и dropdown'ы: без корня
            // после поражения кнопки меню недоступны («лимб», T082/T084).
            var gameUI = FindAnyObjectByType<GameUIHandler>();
            if (gameUI != null)
            {
                gameUI.ShowPreviewAndDropdowns();

                if (gameUI.m_StartMenuRoot != null)
                    gameUI.m_StartMenuRoot.gameObject.SetActive(true);

                // В меню экранная кнопка паузы скрыта (как в свежей сессии) — её показывает забег
                if (gameUI.m_PauseMenuButton != null)
                    gameUI.m_PauseMenuButton.gameObject.SetActive(false);
            }
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