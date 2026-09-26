using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Снаряд — пул, жизнь по таймеру или попаданию, урон через IDamageable.
    /// Возвращается в пул вместо Destroy (T024).
    /// </summary>
    public class Projectile : MonoBehaviour, IPoolable
    {
        [Header("Настройки")]
        public float m_LifeTime = 2f;              // Время жизни снаряда
        public float m_MaxDamage = 5f;             // Максимальный урон (передаётся из Shooting)
        public float m_ExplosionRadius = 1f;       // Радиус взрыва
        public LayerMask m_TankMask;               // Слои, которые задевает взрыв (T024d)

        private float m_LifeTimer;
        private bool m_Detonated;
        private bool m_HasTarget;                  // цель зафиксирована попаданием (вместо сентинела, T024d)
        private Vector3 m_LastKnownTargetPos;
        private Collider m_Collider;
        private PoolManager m_Pool;                // пул внедряется при создании (T024d)

        // Переиспользуемый буфер запроса: ноль аллокаций в горячем пути (T024d)
        private const int k_MaxOverlapResults = 32;
        private readonly Collider[] m_OverlapResults = new Collider[k_MaxOverlapResults];

        // Базовые значения из префаба: восстановление при взятии из пула не даёт
        // бонусам апгрейдов накапливаться на инстансах, побывавших в пуле (T024b)
        private float m_BaseMaxDamage;
        private float m_BaseExplosionRadius;
        private float m_BaseLifeTime;

        private void Awake()
        {
            m_Collider = GetComponent<Collider>();

            m_BaseMaxDamage = m_MaxDamage;
            m_BaseExplosionRadius = m_ExplosionRadius;
            m_BaseLifeTime = m_LifeTime;
        }

        private void Update()
        {
            if (m_Detonated) return;

            m_LifeTimer -= Time.deltaTime;
            if (m_LifeTimer <= 0f)
            {
                Detonate();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (m_Detonated) return;

            // Цель определяется по IDamageable, а не по слою/тегу (грабли T024)
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb == null) return;

            IDamageable damageable = rb.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                DetonateOn(damageable);
            }
        }

        private void DetonateOn(IDamageable target)
        {
            Transform t = target.Transform;
            Rigidbody rb = t.GetComponent<Rigidbody>();
            m_LastKnownTargetPos = rb != null ? rb.position : t.position;
            m_HasTarget = true;
            Detonate();
        }

        private void Detonate()
        {
            if (m_Detonated) return;
            m_Detonated = true;

            // Наносим урон если есть цель
            if (m_HasTarget)
            {
                ApplyDamageTo(m_LastKnownTargetPos);
            }

            // Запускаем burst-эффект
            UseBurstEffect();

            // Возвращаем снаряд в пул
            ReturnToPool();
        }

        private void ApplyDamageTo(Vector3 center)
        {
            // Запрос без аллокаций: переиспользуемый буфер и маска слоёв (T024d)
            int hitCount = Physics.OverlapSphereNonAlloc(center, m_ExplosionRadius, m_OverlapResults, m_TankMask);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = m_OverlapResults[i];
                if (hitCollider == null) continue;

                Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
                if (rb == null) continue;

                IDamageable damageable = rb.GetComponent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;

                // T083: дистанция урона — до ПОВЕРХНОСТИ коллайдера и от той же точки (center),
                // из которой строился запрос OverlapSphere. Иначе касание капсулы давало дистанцию
                // до центра (≥ радиуса) и прямое попадание наносило 0 урона при радиусе меньше
                // габарита цели. Форма спада и коэффициенты (CalculateDamage) не меняются.
                float distance = Vector3.Distance(center, hitCollider.ClosestPoint(center));
                float damage = CalculateDamage(distance);

                if (damage > 0f)
                {
                    damageable.TakeDamage(damage);
                }
            }
        }

        private float CalculateDamage(float distance)
        {
            float relativeDistance = Mathf.Max(0f, (m_ExplosionRadius - distance) / m_ExplosionRadius);
            return relativeDistance * m_MaxDamage;
        }

        private void UseBurstEffect()
        {
            // T024c: берём VFX из пула вместо Instantiate
            if (m_Pool == null) return;
            BurstEffect burst = m_Pool.GetBurst(transform.position);
            if (burst != null)
            {
                burst.PlayAndRelease();
            }
        }

        private void ReturnToPool()
        {
            // Отключаем коллайдер
            if (m_Collider != null)
            {
                m_Collider.enabled = false;
            }

            // Возвращаем снаряд в пул по внедрённой ссылке (T024d: снаряд не знает о LevelManager)
            if (m_Pool == null)
            {
                Debug.LogError("[Projectile] Пул не внедрён (SetPool) — снаряд не возвращён в пул.", this);
                return;
            }

            m_Pool.ReleaseShell(this);
        }

        /// <summary>
        /// Внедрение пула при создании снаряда (вызывает PoolManager.CreateShell, T024d).
        /// </summary>
        public void SetPool(PoolManager pool)
        {
            m_Pool = pool;
        }

        /// <summary>
        /// T025a: приём итогового радиуса взрыва на один выстрел (база префаба + бонус StatBlock).
        /// Значение абсолютное: база восстанавливается в OnGetFromPool (T024b), накопления нет.
        /// </summary>
        public void SetShotExplosionRadius(float radius)
        {
            m_ExplosionRadius = radius;
        }

        // Для пула: восстановление при выдаче
        public void OnGetFromPool()
        {
            m_Detonated = false;

            // Возврат к базам префаба: значения, изменённые на предыдущем выстреле,
            // не накапливаются между выстрелами (T024b)
            m_MaxDamage = m_BaseMaxDamage;
            m_ExplosionRadius = m_BaseExplosionRadius;
            m_LifeTime = m_BaseLifeTime;
            m_LifeTimer = m_LifeTime;

            m_LastKnownTargetPos = Vector3.zero;
            m_HasTarget = false;

            // Включаем коллайдер
            if (m_Collider != null)
            {
                m_Collider.enabled = true;
            }

            gameObject.SetActive(true);
        }

        // Для пула: сброс состояния при возврате
        public void OnReleaseFromPool()
        {
            m_Detonated = false;
            m_LifeTimer = 0f;
            m_LastKnownTargetPos = Vector3.zero;
            m_HasTarget = false;

            // Отключаем коллайдер пока объект в пуле
            if (m_Collider != null)
            {
                m_Collider.enabled = false;
            }

            gameObject.SetActive(false);
        }
    }
}
