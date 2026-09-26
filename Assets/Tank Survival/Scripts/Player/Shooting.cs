using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Стрельба башни — автоматический поиск цели и стрельба.
    /// Итоговые урон и перезарядка читаются из StatBlock забега (T019).
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

        // T066: урон берётся из TurretData
        [Tooltip("Урон снаряда — передаётся из TurretData при спавне")]
        public float m_Damage = 5f;

        // T064: скорость поворота турели
        [Tooltip("Скорость поворота турели в градусах в секунду")]
        public float m_TurretTurnSpeed = 180f;

        private float m_ShotCooldownTimer = 0.0f;
        private bool m_Fired;
        private float closestDist = float.MaxValue;

        // T019: статы забега — итоговые урон/перезарядка читаются отсюда
        private StatBlock m_StatBlock;

        // Оптимизация: ищем новую цель не каждый кадр, а раз в N секунд
        private float m_LastTargetSearch = 0f;
        private const float TARGET_SEARCH_INTERVAL = 0.5f;

        // T064: кэш Transform цели (избегаем GetComponent в Update)
        private Transform m_TargetTransform;
        // T020: кэш IDamageable цели (вместо Rigidbody + TankHealth)
        private IDamageable m_TargetDamageable;

        void Awake()
        {
            // T064: назначить currentGun если не назначен
            if (currentGun == null)
            {
                currentGun = transform.gameObject;
            }
        }

        /// <summary>
        /// T019: принять StatBlock забега.
        /// </summary>
        public void SetStatBlock(StatBlock statBlock)
        {
            m_StatBlock = statBlock;
        }

        /// <summary>
        /// T019: пересчитать итоговый урон и перезарядку (база из данных + модификаторы).
        /// </summary>
        public void RefreshStats()
        {
            if (m_StatBlock == null) return;

            m_Damage = m_StatBlock.GetDamage();
            m_ShotCooldown = m_StatBlock.GetFireRate();
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

            // T064: поворот турели к цели
            if (m_TargetDamageable != null && m_TargetTransform != null)
            {
                Vector3 dir = m_TargetTransform.position - currentGun.transform.position;
                dir.y = 0; // только вокруг Y

                if (dir.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(dir);
                    currentGun.transform.rotation = Quaternion.RotateTowards(
                        currentGun.transform.rotation,
                        targetRotation,
                        m_TurretTurnSpeed * Time.deltaTime
                    );

                    // T064: стреляем только если угол до цели < 10°
                    float angle = Quaternion.Angle(currentGun.transform.rotation, targetRotation);
                    if (angle > 10f)
                        return;
                }
            }

            // Стреляем, если есть цель и кулдаун прошёл
            if (!m_Fired && m_TargetDamageable != null)
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
            m_TargetDamageable = null;
            m_TargetTransform = null;

            for (int i = 0; i < colliders.Length; i++)
            {
                Transform targetTransform = colliders[i].transform;
                IDamageable targetDamageable = colliders[i].GetComponent<IDamageable>();

                if (targetDamageable == null) continue;

                float dist = (targetTransform.position - transform.position).sqrMagnitude;
                if (dist <= closestDist)
                {
                    m_TargetDamageable = targetDamageable;
                    m_TargetTransform = targetTransform; // T064: кэшируем Transform
                    closestDist = dist;
                    aimPosition = m_FireTransform;
                }
            }
        }

        private void Fire()
        {
            m_Fired = true;
            
            // Получаем снаряд из пула PoolManager
            Projectile shell = LevelManager.Instance.Pool.GetShell(
                m_FireTransform.position, 
                m_FireTransform.rotation);
                
            if (shell == null)
            {
                return; // пул не настроен или исчерпан
            }

            // Передаём урон из StatBlock на снаряд
            shell.m_MaxDamage = m_Damage;

            // T025a: итоговый радиус = база снаряда (восстановлена при взятии из пула, T024b)
            // + бонус StatBlock. Передаём абсолютное значение — между выстрелами не накапливается.
            float radiusBonus = m_StatBlock != null ? m_StatBlock.GetExplosionRadiusBonus() : 0f;
            shell.SetShotExplosionRadius(shell.m_ExplosionRadius + radiusBonus);

            float shellSpeed = 20f;
            Rigidbody rb = shell.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = shellSpeed * m_FireTransform.forward;
            }

            m_ShotCooldownTimer = m_ShotCooldown;
        }
    }
}
