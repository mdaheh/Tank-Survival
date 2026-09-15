using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace TankSurvival
{
    /// <summary>
    /// Менеджер опыта и уровней — начисляет XP за убийства, повышает уровни,
    /// предлагает улучшения при повышении уровня.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("References")]
        public LevelData levelCurve;                    // Кривая XP (ScriptableObject)
        public List<UpgradeOptionData> upgradePool;     // Пул всех доступных улучшений

        [Header("UI References (для интеграции с UI)")]
        public GameObject levelUpPanel;                 // Панель выбора улучшений
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

        // События
        public UnityEvent<int> OnLevelUp;               // (newLevel)
        public UnityEvent<int, int> OnXpChanged;        // (currentXp, xpRequired)
        public UnityEvent<List<UpgradeOptionData>> OnUpgradeOptionsRequested; // Запрос вариантов улучшений

        public int CurrentLevel => m_CurrentLevel;
        public int CurrentXp => m_CurrentXp;
        public int XpRequired => m_XpRequired;
        public float XpProgress => m_XpRequired > 0 ? (float)m_CurrentXp / m_XpRequired : 0f;

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
            m_XpRequired = levelCurve.GetXpRequiredForLevel(m_CurrentLevel);
            UpdateUI();
        }

        private void Update()
        {
            // Обновляем UI прогресс-бара каждый кадр
            if (xpBarFill != null && !m_LevelUpPaused)
            {
                xpBarFill.fillAmount = XpProgress;
            }
        }

        /// <summary>
        /// Добавить опыт. При достижении порога — повышение уровня.
        /// </summary>
        public void AddXp(int amount)
        {
            if (m_LevelUpPaused) return; // Не начислять XP во время выбора улучшений

            m_CurrentXp += amount;

            if (debugMode)
                Debug.Log($"[LevelManager] +{amount} XP. Итого: {m_CurrentXp}/{m_XpRequired}");

            // Проверка повышения уровня (может быть несколько за раз, если дали много XP)
            while (m_CurrentXp >= m_XpRequired)
            {
                m_CurrentXp -= m_XpRequired;
                LevelUp();
            }

            OnXpChanged?.Invoke(m_CurrentXp, m_XpRequired);
        }

        /// <summary>
        /// Повысить уровень
        /// </summary>
        private void LevelUp()
        {
            m_CurrentLevel++;
            m_XpRequired = levelCurve.GetXpRequiredForLevel(m_CurrentLevel);

            if (debugMode)
                Debug.Log($"[LevelManager] Уровень {m_CurrentLevel}! Нужно ещё {m_XpRequired} XP");

            OnLevelUp?.Invoke(m_CurrentLevel);

            // Если достигнут максимальный уровень — ничего не предлагаем
            if (!levelCurve.IsValidLevel(m_CurrentLevel))
            {
                if (debugMode)
                    Debug.Log($"[LevelManager] Максимальный уровень {levelCurve.MaxLevel} достигнут!");
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

            // Выбрать случайные улучшения из пула (перемешиваем через временные ключи)
            List<UpgradeOptionData> options = new();
            var withKeys = upgradePool.Select(u => (u, key: Random.value)).OrderBy(x => x.key).ToList();

            int count = Mathf.Min(3, withKeys.Count); // Максимум 3 варианта
            for (int i = 0; i < count; i++)
            {
                options.Add(withKeys[i].u);
            }

            OnUpgradeOptionsRequested?.Invoke(options);
        }

        /// <summary>
        /// Применить выбранное улучшение и продолжить игру
        /// </summary>
        public void ApplyUpgrade(UpgradeOptionData upgrade)
        {
            if (upgrade == null)
            {
                Debug.LogError("[LevelManager] Попытка применить null-улучшение!");
                return;
            }

            upgrade.ApplyToPlayer();
            m_LevelUpPaused = false;

            if (debugMode)
                Debug.Log($"[LevelManager] Применено улучшение: {upgrade.displayName}");

            // Скрыть панель улучшений
            if (levelUpPanel != null)
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
            m_XpRequired = levelCurve.GetXpRequiredForLevel(1);
            m_LevelUpPaused = false;

            if (debugMode)
                Debug.Log("[LevelManager] Прогресс уровня сброшен для нового раунда");
        }

        /// <summary>
        /// Установить конкретный уровень и XP (для отладки)
        /// </summary>
        public void SetLevelAndXp(int level, int xp)
        {
            if (!levelCurve.IsValidLevel(level))
            {
                Debug.LogWarning($"[LevelManager] Недопустимый уровень: {level}");
                return;
            }

            m_CurrentLevel = level;
            m_CurrentXp = xp;
            m_XpRequired = levelCurve.GetXpRequiredForLevel(level);
            UpdateUI();

            if (debugMode)
                Debug.Log($"[LevelManager] Установлено: Уровень {level}, XP {xp}");
        }
    }
}
