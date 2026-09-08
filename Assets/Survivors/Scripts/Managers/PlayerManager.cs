using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif
using UnityEngine;
using UnityEngine.InputSystem.Users;
using UnityEngine.UIElements;

namespace Tanks.Complete
{
    [Serializable]
    public class PlayerManager
    {
        public Transform m_SpawnPoint;                          // The position and direction the tank will have when it spawns.
        [HideInInspector] public int m_PlayerNumber;            // This specifies which player this the manager for.
        [HideInInspector] public GameObject m_Instance;         // A reference to the instance of the tank when it is created.
        [HideInInspector] public int m_Wins;                    // The number of wins this player has so far.
        public int ControlIndex { get; set; } = 1;              //this defines the index of the control 1 = left keyboard or pad, 2 = right keyboard, -1 = no control

        private TankMovement m_Movement;                        // Reference to tank's movement script, used to disable and enable control.
        private GameObject m_CanvasGameObject;                  // Used to disable the world space UI during the Starting and Ending phases of each round.
        
        public void Setup (int controlIndex = 1)
        {
            if (m_Instance == null)
            {
                Debug.LogError("PlayerManager: Танк еще не создан!");
                return;
            }

            // Get references to the components.
            m_Movement = m_Instance.GetComponent<TankMovement> ();
            // Проверка, если компонент не найден
            if (m_Movement == null)
            {
                Debug.LogError("PlayerManager: Не найден компонент TankMovement на танке!");
                return;
            }
            // Ищем Canvas среди детей, если он есть
            var canvas = m_Instance.GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                m_CanvasGameObject = canvas.gameObject;
            }
            else
            {
                m_CanvasGameObject = null; // Canvas может отсутствовать
            }

            // Настраиваем номер игрока и контрольный индекс
            m_PlayerNumber = 1; // Для одного игрока всегда 1
            m_Movement.m_PlayerNumber = m_PlayerNumber;
            m_Movement.ControlIndex = controlIndex;
            
        }

        // Used during the phases of the game where the player shouldn't be able to control their tank.
        public void DisableControl ()
        {
            m_Movement.enabled = false;
            //m_Shooting.enabled = false;
            m_CanvasGameObject.SetActive (false);
        }

        // Used during the phases of the game where the player should be able to control their tank.
        public void EnableControl ()
        {
            m_Movement.enabled = true;
            //m_Shooting.enabled = true;
            m_CanvasGameObject.SetActive (true);
        }

        // Used at the start of each round to put the tank into it's default state.
        public void Reset ()
        {
            m_Instance.transform.position = m_SpawnPoint.position;
            m_Instance.transform.rotation = m_SpawnPoint.rotation;

            m_Instance.SetActive (false);
            m_Instance.SetActive (true);
        }
    }
    
    #if UNITY_EDITOR
    // This is a class only used in the unity editor (and not in the final game). It customizes how the TankManager component
    // will appear in the Inspector. The default make a foldout entry where SpawnPoint is "inside" the TankManager foldout
    // in the manager array in the GameManager. This change this behavior to directly display the spawn point in the TankManager
    // Inspector, simplifying the display in the GameManager.
    [CustomPropertyDrawer(typeof(PlayerManager))]
    public class PlayerManagerDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var itemSlot = new PropertyField(property.FindPropertyRelative(nameof(PlayerManager.m_SpawnPoint)));
            return itemSlot;
        }
    }
    #endif
}