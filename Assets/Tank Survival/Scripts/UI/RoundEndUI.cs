using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TankSurvival
{
    /// <summary>
    /// Скрипт для экрана окончания раунда.
    /// Показывает статистику и кнопки "Новая игра" / "Ангар".
    /// 
    /// === НАСТРОЙКА В UNITY EDITOR ===
    /// 
    /// 1. Создай пустой GameObject и назови его "RoundEndUI"
    /// 2. Добавь компонент RoundEndUI
    /// 3. Создай Panel (UI > Panel) как дочерний элемент
    /// 4. На Panel добавь Image (фон, тёмный с прозрачностью)
    /// 5. На Panel добавь TextMeshPro элементы:
    ///    - "RoundCompleteText" — заголовок "Раунд завершён!"
    ///    - "TotalKillsText" — "Убийств: X"
    ///    - "GamesPlayedText" — "Раундов сыграно: X"
    ///    - "BestDifficultyText" — "Лучшая сложность: X"
    /// 6. На Panel добавь 2 Button:
    ///    - "NewGameButton" — "Новая игра"
    ///    - "HangarButton" — "Ангар"
    /// 7. Перетащи элементы в Inspector:
    ///    - RoundEndPanel → сам Panel
    ///    - RoundCompleteText → TextMeshPro "RoundCompleteText"
    ///    - TotalKillsText → TextMeshPro "TotalKillsText"
    ///    - GamesPlayedText → TextMeshPro "GamesPlayedText"
    ///    - BestDifficultyText → TextMeshPro "BestDifficultyText"
    ///    - NewGameButton → Button "Новая игра"
    ///    - HangarButton → Button "Ангар"
    /// </summary>
    public class RoundEndUI : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject m_Panel;                    // Сам Panel (весь экран)

        [Header("Text Elements")]
        public TMPro.TMP_Text m_RoundCompleteText;  // Заголовок "Раунд завершён!"
        public TMPro.TMP_Text m_TotalKillsText;     // "Убийств: X"
        public TMPro.TMP_Text m_GamesPlayedText;    // "Раундов сыграно: X"
        public TMPro.TMP_Text m_BestDifficultyText; // "Лучшая сложность: X"

        [Header("Buttons")]
        public Button m_NewGameButton;                // Кнопка "Новая игра"
        public Button m_HangarButton;                 // Кнопка "Ангар"

        [Header("References")]
        public GameManager m_GameManager;             // Ссылка на GameManager

        private void Awake()
        {
            // Панель скрыта по умолчанию
            if (m_Panel != null)
                m_Panel.SetActive(false);

            SetupButtons();
        }

        /// <summary>
        /// Настроить обработчики кнопок
        /// </summary>
        private void SetupButtons()
        {
            if (m_NewGameButton != null)
            {
                m_NewGameButton.onClick.AddListener(OnNewGame);
            }

            if (m_HangarButton != null)
            {
                m_HangarButton.onClick.AddListener(OnHangar);
            }
        }

        /// <summary>
        /// Показать панель с результатами
        /// </summary>
        public void ShowRoundEnd(PlayerProgress progress, int currentDifficultyIndex)
        {
            if (m_Panel == null) return;

            // Обновляем статистику
            if (m_TotalKillsText != null)
                m_TotalKillsText.text = $"Убийств: {progress.totalKills}";

            if (m_GamesPlayedText != null)
                m_GamesPlayedText.text = $"Раундов сыграно: {progress.totalGamesPlayed}";

            if (m_BestDifficultyText != null)
            {
                string difficultyName = GetDifficultyName(currentDifficultyIndex);
                m_BestDifficultyText.text = $"Лучшая сложность: {difficultyName}";
            }

            // Показываем панель
            m_Panel.SetActive(true);

            Debug.Log("[RoundEndUI] Раунд завершён. Статистика обновлена.");
        }

        /// <summary>
        /// Скрыть панель
        /// </summary>
        public void HideRoundEnd()
        {
            if (m_Panel != null)
                m_Panel.SetActive(false);
        }

        /// <summary>
        /// Обработчик кнопки "Новая игра"
        /// </summary>
        private void OnNewGame()
        {
            Debug.Log("[RoundEndUI] Новая игра");
            HideRoundEnd();

            if (m_GameManager != null)
                m_GameManager.StartNewRound();
        }

        /// <summary>
        /// Обработчик кнопки "Ангар"
        /// </summary>
        private void OnHangar()
        {
            Debug.Log("[RoundEndUI] Ангар");
            HideRoundEnd();

            if (m_GameManager != null)
                m_GameManager.GoToHangar();
        }

        /// <summary>
        /// Получить название сложности по индексу
        /// </summary>
        private string GetDifficultyName(int index)
        {
            switch (index)
            {
                case 0: return "Лёгкий";
                case 1: return "Средний";
                case 2: return "Тяжёлый";
                default: return "Неизвестно";
            }
        }
    }
}
