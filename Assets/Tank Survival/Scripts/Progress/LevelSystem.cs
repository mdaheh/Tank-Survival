using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TankSurvival
{
    /// <summary>
    /// Система опыта и уровней (T034) — начисляет XP за убийства, повышает уровни,
    /// предлагает улучшения при повышении уровня. HUD читает состояние через события.
    /// </summary>
    public class LevelSystem : MonoBehaviour
    {
        public static LevelSystem Instance { get; private set; }

        [Header("UI References (для интеграции с UI)")]
        public GameObject levelUpPanel;                 // Панель выбора улучшений (устаревшее)
        public UpgradePanel m_UpgradePanel;             // Скрипт панели улучшений (новый)
        public UnityEngine.UI.Image xpBarFill;          // Заполнение прогресс-бара XP
        public TMPro.TMP_Text levelText;              // Текст текущего уровня
        public TMPro.TMP_Text xpText;                 // Текст текущего XP / необходимого XP

        [Header("Debug Info")]
        [SerializeField] private bool debugMode = false; // Показывать логи отладки

        // --- Состояние ---
        private int m_CurrentLevel = 1;
        private int m_CurrentXp = 0;
        private int m_XpRequired;
        private bool m_LevelUpPaused;                   // Пауза (показана панель улучшений)

        // T034: переиспользуемый буфер Fisher–Yates для выбора 3 вариантов (без LINQ)
        private readonly List<UpgradeOptionData> m_ShuffleBuffer = new();

        // События
        public UnityEvent<int> OnLevelUp;               // (newLevel)
        public UnityEvent<int, int> OnXpChanged;        // (currentXp, xpRequired)
        public UnityEvent<List<UpgradeOptionData>> OnUpgradeOptionsRequested; // Запрос вариантов улучшений

        public int CurrentLevel => m_CurrentLevel;
        public int CurrentXp => m_CurrentXp;
        public int XpRequired => m_XpRequired;
        public float XpProgress => m_XpRequired > 0 ? (float)m_CurrentXp / m_XpRequired : 0f;

        /// <summary>
        /// Ссылка на пул снарядов (T024)
        /// </summary>
        public PoolManager Pool => PoolManager.Instance;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            var curve = DataCatalog.GetLevelCurve();
            if (curve == null)
            {
                Debug.LogWarning("[LevelSystem] LevelCurve не назначен! Использую дефолтное значение (50 XP).");
                m_XpRequired = 50;
            }
            else
            {
                m_XpRequired = curve.GetXpRequiredForLevel(m_CurrentLevel);
            }
            UpdateUI();
        }

        /// <summary>
        /// Добавить опыт. При достижении порога — повышение уровня.
        /// </summary>
        public void AddXp(int amount)
        {
            if (m_LevelUpPaused) return; // Не начислять XP во время выбора улучшений

            m_CurrentXp += amount;

            // Проверка повышения уровня (может быть несколько за раз, если дали много XP)
            while (m_CurrentXp >= m_XpRequired)
            {
                m_CurrentXp -= m_XpRequired;
                LevelUp();
            }

            // T034: XP-бар/тексты обновляются по событию (UpdateUI шлёт OnXpChanged)
            UpdateUI();
        }

        /// <summary>
        /// Повысить уровень
        /// </summary>
        private void LevelUp()
        {
            m_CurrentLevel++;
            m_XpRequired = DataCatalog.GetLevelCurve().GetXpRequiredForLevel(m_CurrentLevel);

            OnLevelUp?.Invoke(m_CurrentLevel);

            // Если достигнут максимальный уровень — ничего не предлагаем
            if (!DataCatalog.GetLevelCurve().IsValidLevel(m_CurrentLevel))
            {
                return;
            }

            // Запросить варианты улучшений
            RequestUpgradeOptions();
        }

        /// <summary>
        /// Запросить варианты улучшений (вызывает обновление UI)
        /// </summary>
        private void RequestUpgradeOptions()
        {
            m_LevelUpPaused = true;

            // T034: Fisher–Yates в переиспользуемый буфер (без LINQ/OrderBy — без аллокаций)
            m_ShuffleBuffer.Clear();
            List<UpgradeOptionData> upgrades = DataCatalog.GetAllUpgrades();
            for (int i = 0; i < upgrades.Count; i++)
            {
                m_ShuffleBuffer.Add(upgrades[i]);
            }

            int count = Mathf.Min(3, m_ShuffleBuffer.Count); // Максимум 3 варианта
            for (int i = 0; i < count; i++)
            {
                int j = Random.Range(i, m_ShuffleBuffer.Count);   // j ∈ [i, Count)
                UpgradeOptionData tmp = m_ShuffleBuffer[i];
                m_ShuffleBuffer[i] = m_ShuffleBuffer[j];
                m_ShuffleBuffer[j] = tmp;
            }

            // В буфере остаются ровно count вариантов — он же и payload события.
            // Владение: слушатель потребляет список синхронно (панель пересоздаёт карточки
            // при каждом запросе; новый level-up невозможен во время паузы m_LevelUpPaused).
            if (count < m_ShuffleBuffer.Count)
            {
                m_ShuffleBuffer.RemoveRange(count, m_ShuffleBuffer.Count - count);
            }

            OnUpgradeOptionsRequested?.Invoke(m_ShuffleBuffer);
        }

        /// <summary>
        /// Применить выбранное улучшение и продолжить игру
        /// </summary>
        public void ApplyUpgrade(UpgradeOptionData upgrade)
        {
            if (upgrade == null)
            {
                Debug.LogError("[LevelSystem] Попытка применить null-улучшение!");
                return;
            }

            upgrade.ApplyToPlayer();
            m_LevelUpPaused = false;

            // Скрыть панель улучшений (оба варианта)
            if (m_UpgradePanel != null)
                m_UpgradePanel.HidePanel();
            else if (levelUpPanel != null)
                levelUpPanel.SetActive(false);
        }

        /// <summary>
        /// Обновить UI (вызывается из UI-компонентов)
        /// </summary>
        public void UpdateUI()
        {
            if (levelText != null)
                levelText.text = $"Уровень {m_CurrentLevel}";

            if (xpText != null)
                xpText.text = $"{m_CurrentXp} / {m_XpRequired} XP";

            if (xpBarFill != null)
                xpBarFill.fillAmount = XpProgress;

            OnXpChanged?.Invoke(m_CurrentXp, m_XpRequired);
        }

        /// <summary>
        /// Сбросить прогресс уровня (в начало нового раунда)
        /// </summary>
        public void ResetRound()
        {
            m_CurrentLevel = 1;
            m_CurrentXp = 0;
            m_XpRequired = DataCatalog.GetLevelCurve().GetXpRequiredForLevel(1);
            m_LevelUpPaused = false;

            // T085: сброс раунда обязан обновить UI уровня/XP — иначе после возврата в меню
            // в HUD остаются значения прошлого забега
            UpdateUI();
        }

        /// <summary>
        /// Установить конкретный уровень и XP (для отладки)
        /// </summary>
        public void SetLevelAndXp(int level, int xp)
        {
            if (!DataCatalog.GetLevelCurve().IsValidLevel(level))
            {
                Debug.LogWarning($"[LevelSystem] Недопустимый уровень: {level}");
                return;
            }

            m_CurrentLevel = level;
            m_CurrentXp = xp;
            m_XpRequired = DataCatalog.GetLevelCurve().GetXpRequiredForLevel(level);
            UpdateUI();
        }
    }
}
