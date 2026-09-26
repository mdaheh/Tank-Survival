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

        // T019: статы забега — единственный источник итоговых значений (владелец — RunContext)
        private StatBlock m_StatBlock;

        // T019: кэш компонентов, чтобы после улучшения пересчитать значения без поиска по сцене
        private PlayerMovement m_CachedMovement;
        private Shooting m_CachedShooting;
        private TankHealth m_CachedHealth;

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
        /// T019: принять StatBlock забега. Вызывается GameManager до SpawnTank.
        /// </summary>
        public void SetStatBlock(StatBlock statBlock)
        {
            m_StatBlock = statBlock;
        }

        /// <summary>
        /// T019: статы забега — сюда пишут улучшения, отсюда читают компоненты танка.
        /// </summary>
        public StatBlock Stats => m_StatBlock;

        /// <summary>
        /// T019: пересчитать итоговые значения компонентов танка из StatBlock
        /// (вызывается после применения улучшения). Без поиска по сцене и аллокаций.
        /// </summary>
        public void RefreshStats()
        {
            if (m_StatBlock == null) return;

            if (m_CachedMovement != null) m_CachedMovement.RefreshStats();
            if (m_CachedHealth != null) m_CachedHealth.RefreshStats();
            if (m_CachedShooting != null) m_CachedShooting.RefreshStats();
        }

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

            // T019: без StatBlock забега собрать танк нельзя — итоговые статы берутся только из него
            if (m_StatBlock == null)
            {
                Debug.LogError("[PlayerManager] StatBlock забега не передан (GameManager.SpawnTank → SetStatBlock)! Танк не будет собран.");
                return;
            }

            // Инстанцировать префаб из данных
            m_Instance = UnityEngine.Object.Instantiate(chassisData.prefab, position, rotation);

            TurretMountPoint turretMount = m_Instance.GetComponentInChildren<TurretMountPoint>();

            if (turretMount == null)
            {
                Debug.LogError($"[PlayerManager] На шасси '{chassisData.name}' не найден TurretMountPoint! Турель не будет смонтирована.");
                return;
            }

            Transform turretPos = turretMount.transform;

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
            // T019: базы из данных кладём в StatBlock — единственный источник итоговых значений
            m_StatBlock.baseMaxHealth = chassis.maxHealth;
            m_StatBlock.baseMoveSpeed = chassis.moveSpeed;
            m_StatBlock.baseTurnSpeed = chassis.turnSpeed;

            // PlayerMovement — движение и поворот
            var movement = m_Instance.GetComponent<PlayerMovement>();
            if (movement == null)
            {
                movement = m_Instance.AddComponent<PlayerMovement>();
            }
            movement.SetStatBlock(m_StatBlock);
            movement.RefreshStats();
            m_CachedMovement = movement;

            // TankHealth — здоровье
            var health = m_Instance.GetComponent<TankHealth>();
            if (health == null)
            {
                health = m_Instance.AddComponent<TankHealth>();
            }
            health.SetStatBlock(m_StatBlock);
            health.RefreshStats();
            m_CachedHealth = health;
        }

        /// <summary>
        /// Применить настройки башни к компонентам на башне
        /// </summary>
        private void ApplyTurretStats(TurretData turret)
        {
            // T019: урон и перезарядка — базы в StatBlock; улучшения добавляют модификаторы к ним
            m_StatBlock.baseDamage = turret.damage;
            m_StatBlock.baseFireRate = turret.fireRate;

            var shooting = m_TurretInstance.GetComponent<Shooting>();
            if (shooting == null)
            {
                // T077: состав компонентов задаётся префабом — фоллбэк маскировал ошибку конфигурации
                Debug.LogError("[PlayerManager] На башне нет компонента Shooting — стрельба недоступна (проверьте префаб башни).");
                return;
            }
            shooting.SetStatBlock(m_StatBlock);
            shooting.fireRange = turret.fireRange; // fireRange остаётся базой из данных (в StatBlock его нет)
            shooting.m_BaseExplosionRadius = turret.baseExplosionRadius; // T025b: база радиуса взрыва — тоже данные турели
            shooting.RefreshStats();
            m_CachedShooting = shooting;
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
