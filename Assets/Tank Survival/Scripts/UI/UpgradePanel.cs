using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TankSurvival
{
    /// <summary>
    /// Скрипт для панели выбора улучшений при повышении уровня.
    /// Показывает 3 случайных улучшения из пула и позволяет выбрать одно.
    /// 
    /// === НАСТРОЙКА В UNITY EDITOR ===
    /// 
    /// 1. Создай пустой GameObject и назови его "UpgradePanel"
    /// 2. Добавь компонент UpgradePanel
    /// 3. Создай Panel (UI > Panel) как дочерний элемент
    /// 4. На Panel добавь Image (фон, полупрозрачный тёмный)
    /// 5. На Panel добавь TextMeshPro:
    ///    - "LevelUpText" — заголовок "Уровень X!"
    ///    - "ChooseUpgradeText" — "Выбери улучшение:"
    /// 6. Создай префаб карточки улучшения (UpgradeCard):
    ///    - GameObject с Image (фон карточки)
    ///    - TMP_Text "UpgradeName" — название улучшения
    ///    - TMP_Text "UpgradeDesc" — описание
    ///    - Image "UpgradeIcon" — иконка
    ///    - Button (весь префаб) — клик для выбора
    /// 7. На UpgradePanel добавь 3 пустых GameObject как дочерние:
    ///    - "UpgradeSlot1" — слот для карточки 1
    ///    - "UpgradeSlot2" — слот для карточки 2
    ///    - "UpgradeSlot3" — слот для карточки 3
    /// 8. Перетащи элементы в Inspector:
    ///    - Panel → сам Panel
    ///    - LevelUpText → TextMeshPro "LevelUpText"
    ///    - ChooseUpgradeText → TextMeshPro "ChooseUpgradeText"
    ///    - UpgradeSlot1 → "UpgradeSlot1"
    ///    - UpgradeSlot2 → "UpgradeSlot2"
    ///    - UpgradeSlot3 → "UpgradeSlot3"
    /// 9. Перетащи префаб карточки в UpgradeCardPrefab
    /// </summary>
    public class UpgradePanel : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject m_Panel;                    // Сам Panel (весь экран)

        [Header("Text Elements")]
        public TMPro.TMP_Text m_LevelUpText;        // "Уровень 5!"
        public TMPro.TMP_Text m_ChooseUpgradeText;  // "Выбери улучшение:"

        [Header("Upgrade Slots")]
        public Transform m_Slot1;                     // Позиция для карточки 1
        public Transform m_Slot2;                     // Позиция для карточки 2
        public Transform m_Slot3;                     // Позиция для карточки 3

        [Header("Settings")]
        public GameObject m_UpgradeCardPrefab;        // Префаб карточки улучшения
        public int m_OptionsCount = 3;                // Количество вариантов (обычно 3)

        [Header("References")]
        public LevelManager m_LevelManager;           // Ссылка на LevelManager

        private List<UpgradeOptionData> m_CurrentOptions; // Текущие варианты
        private List<GameObject> m_CardInstances;     // Инстансы карточек

        private void Awake()
        {
            // Панель скрыта по умолчанию
            if (m_Panel != null)
                m_Panel.SetActive(false);

            m_CardInstances = new List<GameObject>();
        }

        private void Start()
        {
            // Подписываемся на запрос вариантов улучшений
            if (m_LevelManager != null)
            {
                m_LevelManager.OnUpgradeOptionsRequested.AddListener(OnUpgradeOptionsRequested);
            }
        }

        /// <summary>
        /// Получить варианты улучшений от LevelManager и показать панель
        /// </summary>
        public void OnUpgradeOptionsRequested(List<UpgradeOptionData> options)
        {
            if (options == null || options.Count == 0)
            {
                Debug.LogWarning("[UpgradePanel] Нет вариантов улучшений!");
                return;
            }

            m_CurrentOptions = options;

            // Очистить старые карточки
            ClearCards();

            // Показать панель
            if (m_Panel != null)
                m_Panel.SetActive(true);

            // Обновить заголовок с уровнем
            if (m_LevelUpText != null)
            {
                int level = m_LevelManager.CurrentLevel;
                m_LevelUpText.text = $"Уровень {level}!";
            }

            // Создать карточки
            CreateCards(options);
        }

        /// <summary>
        /// Создать карточки улучшений в слотах
        /// </summary>
        private void CreateCards(List<UpgradeOptionData> options)
        {
            Transform[] slots = { m_Slot1, m_Slot2, m_Slot3 };

            for (int i = 0; i < options.Count && i < slots.Length; i++)
            {
                if (slots[i] == null || m_UpgradeCardPrefab == null)
                {
                    Debug.LogWarning($"[UpgradePanel] Слот {i+1} или префаб не назначены!");
                    continue;
                }

                // Создаём карточку
                GameObject card = Instantiate(m_UpgradeCardPrefab, slots[i]);

                // Получаем компоненты карточки
                var nameText = card.GetComponentInChildren<TMPro.TMP_Text>();
                var descText = FindDescText(card);
                var iconImage = FindIconImage(card);
                var button = card.GetComponent<Button>();

                if (nameText != null)
                {
                    nameText.text = options[i].displayName;
                }

                if (descText != null)
                {
                    descText.text = options[i].description;
                }

                if (iconImage != null && options[i].icon != null)
                {
                    iconImage.sprite = options[i].icon;
                }

                // Подписываемся на клик
                int optionIndex = i;
                if (button != null)
                {
                    button.onClick.AddListener(() => OnUpgradeSelected(options[optionIndex]));
                }

                m_CardInstances.Add(card);
            }
        }

        /// <summary>
        /// Найти Text для описания (не главный)
        /// </summary>
        private TMPro.TMP_Text FindDescText(GameObject card)
        {
            var texts = card.GetComponentsInChildren<TMPro.TMP_Text>();
            // Возвращаем второй текст (первый — название)
            return texts.Length > 1 ? texts[1] : null;
        }

        /// <summary>
        /// Найти Image для иконки
        /// </summary>
        private Image FindIconImage(GameObject card)
        {
            var images = card.GetComponentsInChildren<Image>();
            // Первый Image — фон карточки, второй — иконка
            return images.Length > 1 ? images[1] : null;
        }

        /// <summary>
        /// Очистить все карточки
        /// </summary>
        private void ClearCards()
        {
            foreach (var card in m_CardInstances)
            {
                if (card != null)
                    Destroy(card);
            }
            m_CardInstances.Clear();
        }

        /// <summary>
        /// Выбрано улучшение — применить и скрыть панель
        /// </summary>
        private void OnUpgradeSelected(UpgradeOptionData upgrade)
        {
            // Применяем улучшение
            if (m_LevelManager != null)
                m_LevelManager.ApplyUpgrade(upgrade);

            // Скрываем панель
            HidePanel();
        }

        /// <summary>
        /// Скрыть панель
        /// </summary>
        public void HidePanel()
        {
            if (m_Panel != null)
                m_Panel.SetActive(false);

            ClearCards();
            m_CurrentOptions = null;
        }
    }
}
