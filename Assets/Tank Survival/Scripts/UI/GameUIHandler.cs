using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace TankSurvival
{
    /// <summary>
    /// Обработчик UI — главное меню, выбор сложности, выбор частей танка, превью.
    /// Интегрирован с системой прогресса и разблокировок.
    /// </summary>
    public class GameUIHandler : MonoBehaviour
    {
        [Header("GameManager References")]
        public GameManager m_GameManager;

        [Header("Start Menu")]
        public RectTransform m_StartMenuRoot;
        public Button m_StartButton;
        public OnScreenButton m_PauseMenuButton;
        public GameObject m_TankPreview;
        private PauseMenu m_PauseMenu;
        private InputAction m_PauseAction;
        private CanvasScaler m_CanvasScaler;
        private PlayerPreview m_PlayerPreview;

        [Header("Difficulty Selection")]
        public GameObject m_DifficultyPanel;
        public Button[] m_DifficultyButtons;           // [0]=Easy, [1]=Medium, [2]=Hard
        public TMPro.TMP_Text[] m_DifficultyNames;    // Текстовые метки сложностей
        public TMPro.TMP_Text[] m_DifficultyLockText; // Текст "Заблокировано"

        [Header("Part Selection")]
        public Dropdown m_ChassisDropdown;
        public Dropdown m_TurretDropdown;
        public GameObject m_DefaultChassis;
        public GameObject m_DefaultTurret;

        [Header("UI Feedback")]
        public TMPro.TMP_Text m_KillsRequiredText;   // Текст "Требуется убийств: X"

        // --- Состояние ---
        private PlayerProgress m_PlayerProgress;
        private int m_SelectedChassisIndex = 0;
        private int m_SelectedTurretIndex = 0;
        private int m_SelectedDifficultyIndex = 0;
        private List<Dropdown.OptionData> m_UnlockedChassisOptions = new();
        private List<Dropdown.OptionData> m_UnlockedTurretOptions = new();

        private void Awake()
        {
            m_CanvasScaler = GetComponentInParent<CanvasScaler>();
            m_PlayerProgress = SaveSystem.Load();
        }

        private void Start()
        {
            // Настройка кнопки старта
            m_StartButton.onClick.AddListener(StartGame);

            // Отключаем кнопку паузы в меню
            m_PauseMenuButton.gameObject.SetActive(false);

            // Получаем превью
            m_PlayerPreview = FindAnyObjectByType<PlayerPreview>(FindObjectsInactive.Include);
            if (m_PlayerPreview != null)
                m_PlayerPreview.SetTankPreview(m_TankPreview);

            // Настройка паузы
            m_PauseMenu = FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
            if (m_PauseMenu != null)
            {
                m_PauseMenu.Init();
                var rectTransform = m_PauseMenuButton.GetComponent<RectTransform>();
                rectTransform.SetAsLastSibling();
            }

            // Настройка сложности
            SetupDifficultySelection();

            // Настройка выбора частей (с учётом разблокировок)
            SetupPartSelection();

            // Начальное превью
            UpdatePreview();
        }

        /// <summary>
        /// Настройка выбора сложности с разблокировками
        /// </summary>
        private void SetupDifficultySelection()
        {
            if (m_DifficultyPanel == null)
                return;

            // Показываем/скрываем панель сложности
            m_DifficultyPanel.SetActive(true);

            // Настройка каждой сложности
            for (int i = 0; i < m_DifficultyButtons.Length && i < m_DifficultyNames.Length; i++)
            {
                bool isUnlocked = m_PlayerProgress.IsDifficultyUnlocked(i);

                // Обновляем текст
                if (m_DifficultyNames[i] != null)
                {
                    string difficultyName = i == 0 ? "Лёгкий" : i == 1 ? "Средний" : "Тяжёлый";
                    m_DifficultyNames[i].text = difficultyName;
                }

                // Показываем/скрываем замок
                if (m_DifficultyLockText[i] != null)
                {
                    m_DifficultyLockText[i].gameObject.SetActive(!isUnlocked);
                }

                // Включаем/отключаем кнопку
                m_DifficultyButtons[i].interactable = isUnlocked;

                // Подписываемся на клик
                int index = i;
                m_DifficultyButtons[i].onClick.AddListener(() => SelectDifficulty(index));
            }

            // Автоматически выбираем первую доступную сложность
            for (int i = 0; i < m_DifficultyButtons.Length; i++)
            {
                if (m_PlayerProgress.IsDifficultyUnlocked(i))
                {
                    SelectDifficulty(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Выбрать сложность
        /// </summary>
        private void SelectDifficulty(int index)
        {
            if (!m_PlayerProgress.IsDifficultyUnlocked(index))
                return;

            m_SelectedDifficultyIndex = index;
            Debug.Log($"[GameUIHandler] Выбрана сложность: {index}");

            // Скрываем панель сложности после выбора
            if (m_DifficultyPanel != null)
                m_DifficultyPanel.SetActive(false);
        }

        /// <summary>
        /// Настройка выбора частей с учётом разблокировок
        /// </summary>
        private void SetupPartSelection()
        {
            // Заполняем списки разблокированных частей
            m_UnlockedChassisOptions.Clear();
            m_UnlockedTurretOptions.Clear();

            for (int i = 0; i < DataCatalog.GetAllChassis().Count; i++)
            {
                var chassis = DataCatalog.GetAllChassis()[i];
                bool isUnlocked = m_PlayerProgress.IsChassisUnlocked(chassis.id) ||
                                  chassis.killsRequired == 0;

                if (isUnlocked)
                {
                    m_UnlockedChassisOptions.Add(new Dropdown.OptionData { text = chassis.displayName });
                }
            }

            for (int i = 0; i < DataCatalog.GetAllTurrets().Count; i++)
            {
                var turret = DataCatalog.GetAllTurrets()[i];
                bool isUnlocked = m_PlayerProgress.IsTurretUnlocked(turret.id) ||
                                  turret.killsRequired == 0;

                if (isUnlocked)
                {
                    m_UnlockedTurretOptions.Add(new Dropdown.OptionData { text = turret.displayName });
                }
            }

            // Настройка дропдаунов
            if (m_ChassisDropdown != null && m_UnlockedChassisOptions.Count > 0)
            {
                m_ChassisDropdown.ClearOptions();
                m_ChassisDropdown.AddOptions(m_UnlockedChassisOptions);
                m_ChassisDropdown.value = 0;
                m_SelectedChassisIndex = 0;
                m_ChassisDropdown.onValueChanged.AddListener(index =>
                {
                    m_SelectedChassisIndex = index;
                    UpdatePreview();
                    UpdateKillsRequiredText();
                });
            }

            if (m_TurretDropdown != null && m_UnlockedTurretOptions.Count > 0)
            {
                m_TurretDropdown.ClearOptions();
                m_TurretDropdown.AddOptions(m_UnlockedTurretOptions);
                m_TurretDropdown.value = 0;
                m_SelectedTurretIndex = 0;
                m_TurretDropdown.onValueChanged.AddListener(index =>
                {
                    m_SelectedTurretIndex = index;
                    UpdatePreview();
                    UpdateKillsRequiredText();
                });
            }

            // Обновляем текст требований
            UpdateKillsRequiredText();
        }

        /// <summary>
        /// Обновить текст требований к убийствам
        /// </summary>
        private void UpdateKillsRequiredText()
        {
            if (m_KillsRequiredText == null)
                return;

            // Находим выбранные части
            ChassisData selectedChassis = null;
            TurretData selectedTurret = null;

            if (m_ChassisDropdown != null && m_ChassisDropdown.value < m_UnlockedChassisOptions.Count)
            {
                // Ищем ChassisData по имени из dropdown
                string chassisName = m_UnlockedChassisOptions[m_ChassisDropdown.value].text;
                selectedChassis = DataCatalog.GetAllChassis().Find(c => c.displayName == chassisName);
            }

            if (m_TurretDropdown != null && m_TurretDropdown.value < m_UnlockedTurretOptions.Count)
            {
                string turretName = m_UnlockedTurretOptions[m_TurretDropdown.value].text;
                selectedTurret = DataCatalog.GetAllTurrets().Find(t => t.displayName == turretName);
            }

            // Показываем требования
            string text = "";
            if (selectedChassis != null && selectedChassis.killsRequired > 0)
                text += $"Шасси: {selectedChassis.killsRequired} убийств. ";
            if (selectedTurret != null && selectedTurret.killsRequired > 0)
                text += $"Башня: {selectedTurret.killsRequired} убийств.";

            m_KillsRequiredText.text = text;
        }

        /// <summary>
        /// Начать игру
        /// </summary>
        void StartGame()
        {
            // Отключаем меню
            m_StartMenuRoot.gameObject.SetActive(false);

            // Получаем выбранные данные частей
            ChassisData selectedChassis = GetSelectedChassisData();
            TurretData selectedTurret = GetSelectedTurretData();

            // Создаём PlayerData
            GameManager.PlayerData playerData = new GameManager.PlayerData()
            {
                chassisId = selectedChassis ? selectedChassis.id : 0,
                turretId = selectedTurret ? selectedTurret.id : 0,
                chassisPrefab = selectedChassis ? selectedChassis.prefab : null,
                turretPrefab = selectedTurret ? selectedTurret.prefab : null,
                difficultyIndex = m_SelectedDifficultyIndex
            };

            // Запускаем игру через GameManager
            if (m_GameManager != null)
            {
                m_GameManager.StartGameFromMenu(playerData);
            }
            else
            {
                Debug.LogError("[GameUIHandler] GameManager не назначен!");
            }

            // Включаем кнопку паузы
            if (m_PauseMenu != null)
            {
                // Получаем Pause действие из TankInputUser
                var tankInput = FindAnyObjectByType<TankInputUser>(FindObjectsInactive.Include);
                if (tankInput?.ActionAsset != null)
                {
                    m_PauseAction = tankInput.ActionAsset.FindActionMap("Gameplay").FindAction("Pause").Clone();
                    m_PauseAction.performed += evt => { TogglePause(); };
                    m_PauseAction.Enable();
                }
                m_PauseMenuButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Получить данные выбранного шасси
        /// </summary>
        private ChassisData GetSelectedChassisData()
        {
            if (m_ChassisDropdown == null || m_ChassisDropdown.value >= m_UnlockedChassisOptions.Count)
                return null;

            string chassisName = m_UnlockedChassisOptions[m_ChassisDropdown.value].text;
            return DataCatalog.GetAllChassis().Find(c => c.displayName == chassisName);
        }

        /// <summary>
        /// Получить данные выбранной башни
        /// </summary>
        private TurretData GetSelectedTurretData()
        {
            if (m_TurretDropdown == null || m_TurretDropdown.value >= m_UnlockedTurretOptions.Count)
                return null;

            string turretName = m_UnlockedTurretOptions[m_TurretDropdown.value].text;
            return DataCatalog.GetAllTurrets().Find(t => t.displayName == turretName);
        }

        /// <summary>
        /// Получить префаб выбранного шасси
        /// </summary>
        private GameObject GetSelectedChassisPrefab()
        {
            var data = GetSelectedChassisData();
            return data ? data.prefab : m_DefaultChassis;
        }

        /// <summary>
        /// Получить префаб выбранной башни
        /// </summary>
        private GameObject GetSelectedTurretPrefab()
        {
            var data = GetSelectedTurretData();
            return data ? data.prefab : m_DefaultTurret;
        }

        private void TogglePause()
        {
            if (m_PauseMenu != null)
                m_PauseMenu.TogglePause();
        }

        private void Update()
        {
            // Адаптивный Canvas
            float ratio = Screen.width / (float)Screen.height;
            m_CanvasScaler.matchWidthOrHeight = ratio > 1.0f ? 1.0f : 0.0f;
        }

        /// <summary>
        /// Обновить превью танка
        /// </summary>
        private void UpdatePreview()
        {
            if (m_PlayerPreview == null)
                return;

            GameObject selectedChassis = GetSelectedChassisPrefab();
            GameObject selectedTurret = GetSelectedTurretPrefab();

            m_PlayerPreview.SetTankPreview(selectedChassis, selectedTurret);
        }
    }
}
