using UnityEngine;
using System.Linq;

namespace TankSurvival
{
    /// <summary>
    /// Стрельба башни — автоматический поиск цели и стрельба.
    /// Поддерживает бонусы от улучшений через компонент ShootingData.
    /// </summary>
    public class Shooting : MonoBehaviour
    {
        public Rigidbody m_Shell;
        public Transform m_FireTransform;
        public AudioSource m_ShootingAudio;
        public AudioClip m_FireClip;
        public LayerMask enemyMask;
        public Transform aimPosition;
        public GameObject currentGun;
        Rigidbody currentTarget;
        public float m_ShotCooldown = 0.3f;
        public float fireRange = 10f;

        private float m_ShotCooldownTimer = 0.0f;
        private bool m_Fired;
        private float closestDist = float.MaxValue;

        // Компонент бонусов от улучшений
        private ShootingData m_ShootingData;

        // Оптимизация: ищем новую цель не каждый кадр, а раз в N секунд
        private float m_LastTargetSearch = 0f;
        private const float TARGET_SEARCH_INTERVAL = 0.5f;

        void Awake()
        {
            m_ShootingData = GetComponent<ShootingData>();
        }

        // Update is called once per frame
        void Update()
        {
            if (m_ShotCooldownTimer > 0.0f)
            {
                m_ShotCooldownTimer -= Time.deltaTime;
            }

            if (m_ShotCooldownTimer <= 0.0f)
            {
                m_Fired = false;
            }

            // Ищем новую цель раз в TARGET_SEARCH_INTERVAL секунд (оптимизация!)
            m_LastTargetSearch -= Time.deltaTime;
            if (m_LastTargetSearch <= 0f)
            {
                FindNewTarget();
                m_LastTargetSearch = TARGET_SEARCH_INTERVAL;
            }

            // Стреляем, если есть цель и кулдаун прошёл
            if (!m_Fired && currentTarget)
            {
                Fire();
            }
        }

        /// <summary>
        /// Найти новую ближайшую цель (вызывается раз в 0.5 сек)
        /// </summary>
        private void FindNewTarget()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, fireRange, enemyMask);
            closestDist = float.MaxValue;
            currentTarget = null;

            for (int i = 0; i < colliders.Length; i++)
            {
                Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();
                TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();
                Transform targetTransform = colliders[i].transform;

                if (!targetHealth) continue;

                float dist = (targetTransform.position - transform.position).sqrMagnitude;
                if (dist <= closestDist)
                {
                    currentTarget = targetRigidbody;
                    closestDist = dist;
                    aimPosition = m_FireTransform;
                }
            }
        }

        private void Fire()
        {
            m_Fired = true;
            Rigidbody shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;

            // Применяем бонусы от улучшений к скорости снаряда
            float shellSpeed = 20f;
            if (m_ShootingData != null && m_ShootingData.damageBonus > 0)
            {
                // damageBonus влияет на урон, а не скорость — это обрабатывается в ShellExplosion
            }

            shellInstance.linearVelocity = shellSpeed * m_FireTransform.forward;

            m_ShotCooldownTimer = m_ShotCooldown;
        }

        private void AutoAiming()
        {
            if (currentGun && currentTarget)
            {
                currentGun.transform.LookAt(currentTarget.transform);
            }
        }
    }
}
