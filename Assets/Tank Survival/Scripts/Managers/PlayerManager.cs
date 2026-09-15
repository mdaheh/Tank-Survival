using System;
using UnityEngine;
using UnityEngine.InputSystem.Users;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace TankSurvival
{
    /// <summary>
    /// MonoBehaviour Singleton для управления танком игрока.
    /// Отвечает за спавн, настройку и контроль танка.
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        public Transform m_SpawnPoint;                          // The position and direction the tank will have when it spawns.
        [HideInInspector] public int m_PlayerNumber;            // This specifies which player this the manager for.
        [HideInInspector] public GameObject m_Instance;         // A reference to the instance of the body when it is created.
        [HideInInspector] public GameObject m_TurretInstance;   // A reference to the instance of the turret when it is created.
        [HideInInspector] public int m_Wins;                    // The number of wins this player has so far.
        
        // References to the components on the spawned objects
        private PlayerMovement m_Movement;                      // Reference to body's movement script
        private Shooting m_Shooting;                            // Reference to turret's shooting script
        private GameObject m_CanvasGameObject;                  // Used to disable the world space UI during the Starting and Ending phases of each round.

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public static PlayerManager Instance { get; private set; }

        /// <summary>
        /// Spawn the player's tank: body + turret attached to TurretPos
        /// </summary>
        public void SpawnTank(GameObject bodyPrefab, GameObject turretPrefab, Vector3 position, Quaternion rotation)
        {
            // Spawn the body (chassis)
            m_Instance = UnityEngine.Object.Instantiate(bodyPrefab, position, rotation);

            // Find the TurretPos transform on the body
            Transform turretPos = m_Instance.transform.Find("TurretPos");
            if (turretPos == null)
            {
                Debug.LogError("PlayerManager: На теле танка не найден объект 'TurretPos' для крепления башни!");
                return;
            }

            // Spawn the turret as a child of TurretPos
            if (turretPrefab != null)
            {
                m_TurretInstance = UnityEngine.Object.Instantiate(turretPrefab, turretPos);
            }
        }

        public void Setup(int controlIndex = 1)
        {
            if (m_Instance == null)
            {
                Debug.LogError("PlayerManager: Танк еще не создан!");
                return;
            }

            // Get references to the components on the body
            m_Movement = m_Instance.GetComponent<PlayerMovement>();
            if (m_Movement == null)
            {
                Debug.LogError("PlayerManager: Не найден компонент PlayerMovement на теле танка!");
                return;
            }

            // Get reference to the Shooting component on the turret
            if (m_TurretInstance != null)
            {
                m_Shooting = m_TurretInstance.GetComponent<Shooting>();
                if (m_Shooting == null)
                {
                    Debug.LogError("PlayerManager: Не найден компонент Shooting на башне танка!");
                }
            }

            // Find Canvas among children
            var canvas = m_Instance.GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                m_CanvasGameObject = canvas.gameObject;
            }
            else
            {
                m_CanvasGameObject = null;
            }
        }

        // Used during the phases of the game where the player shouldn't be able to control their tank.
        public void DisableControl()
        {
            if (m_Movement != null)
                m_Movement.enabled = false;
            if (m_Shooting != null)
                m_Shooting.enabled = false;
            if (m_CanvasGameObject != null)
                m_CanvasGameObject.SetActive(false);
        }

        // Used during the phases of the game where the player should be able to control their tank.
        public void EnableControl()
        {
            if (m_Movement != null)
                m_Movement.enabled = true;
            if (m_Shooting != null)
                m_Shooting.enabled = true;
            if (m_CanvasGameObject != null)
                m_CanvasGameObject.SetActive(true);
        }

        // Used at the start of each round to put the tank into it's default state.
        public void Reset()
        {
            m_Instance.transform.position = m_SpawnPoint.position;
            m_Instance.transform.rotation = m_SpawnPoint.rotation;

            m_Instance.SetActive(false);
            m_Instance.SetActive(true);

            if (m_TurretInstance != null)
            {
                m_TurretInstance.SetActive(false);
                m_TurretInstance.SetActive(true);
            }
        }
    }
    
#if UNITY_EDITOR
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
