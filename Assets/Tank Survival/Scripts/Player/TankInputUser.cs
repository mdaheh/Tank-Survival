using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace TankSurvival
{
    /// <summary>
    /// Содержит InputUser, привязанный к танку.
    /// Загружает локальный InputActionAsset (из .inputactions файла)
    /// и привязывает его к устройству ввода (клавиатуре).
    /// 
    /// === НАСТРОЙКА ===
    /// Перетащи TankControl.inputactions в поле "Action Asset" в Inspector.
    /// </summary>
    public class TankInputUser : MonoBehaviour
    {
        public InputActionAsset ActionAsset => m_LocalActionAsset;

        [Header("Input Settings")]
        [Tooltip("Перетащи сюда TankControl.inputactions")]
        public InputActionAsset m_ActionAsset;

        private InputActionAsset m_LocalActionAsset;

        private void Awake()
        {
            if (m_ActionAsset == null)
            {
                Debug.LogError("[TankInputUser] Action Asset не назначен! Перетащи TankControl.inputactions в Inspector.");
                return;
            }

            // Клонируем Action Map и привязываем к клавиатуре
            m_LocalActionAsset = InputActionAsset.FromJson(m_ActionAsset.ToJson());
            m_LocalActionAsset.FindActionMap("Gameplay").FindAction("Move").Enable();
            m_LocalActionAsset.FindActionMap("Gameplay").FindAction("Pause").Enable();
        }
    }
}
