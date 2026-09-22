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

        public void SpawnTank(int chassisId, int turretId, Vector3 position, Quaternion rotation)
        {
            // Получить данные из DataCatalog
            var chassisData = DataCatalog.GetChassis(chassisId);
            var turretData = DataCatalog.GetTurret(turretId);

            if (chassisData == null)
            {
                Debug.LogError($"[PlayerManager] Шасси с ID {chassisId} не найдено в DataCatalog!");
                return;
            }

            // Инстанцировать префаб из данных
            m_Instance = UnityEngine.Object.Instantiate(chassisData.prefab, position, rotation);

            TurretMountPoint turretMount = m_Instance.GetComponentInChildren<TurretMountPoint>();

            // Найти TurretPos на теле танка
            Transform turretPos = turretMount != null ? turretMount.transform : null;

            // Инстанцировать башню
            if (turretData != null && turretData.prefab != null)
            {
                m_TurretInstance = UnityEngine.Object.Instantiate(turretData.prefab, turretPos.position, turretPos.rotation, turretPos);
            }

            // Добавить компоненты и применить настройки
            ApplyChassisStats(chassisData);
            if (turretData != null)
                ApplyTurretStats(turretData);
        }
        /// <summary>
        /// Применить настройки шасси к компонентам на теле танка
        /// </summary>
        private void ApplyChassisStats(ChassisData chassis)
        {
            // PlayerMovement — движение и поворот
            var movement = m_Instance.GetComponent<PlayerMovement>();
            if (movement == null)
            {
                movement = m_Instance.AddComponent<PlayerMovement>();
            }
            movement.m_Speed = chassis.moveSpeed;
            movement.m_TurnSpeed = chassis.turnSpeed;

            // TankHealth — здоровье
            var health = m_Instance.GetComponent<TankHealth>();
            if (health == null)
            {
                health = m_Instance.AddComponent<TankHealth>();
            }
            health.m_StartingHealth = chassis.maxHealth;
        }

        /// <summary>
        /// Применить настройки башни к компонентам на башне
        /// </summary>
        private void ApplyTurretStats(TurretData turret)
        {
            var shooting = m_TurretInstance.GetComponent<Shooting>();
            if (shooting == null)
            {
                shooting = m_TurretInstance.AddComponent<Shooting>();
            }
            shooting.m_ShotCooldown = turret.fireRate;
            shooting.fireRange = turret.fireRange;
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
}
