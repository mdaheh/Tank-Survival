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
        public float m_ExplosionForce = 1f;        // Сила взрыва
        public float m_ExplosionRadius = 1f;       // Радиус взрыва
        
        [Header("Аудио")]
        public AudioClip m_ExplosionAudio;         // Звук взрыва (копия из префаба)
        
        [Header("Визуал")]
        public GameObject m_BurstEffect;           // Burst-эффект (частицы + звук)

        private float m_LifeTimer;
        private bool m_Detonated;
        private Vector3 m_LastKnownTargetPos;
        private Collider m_Collider;

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
            Detonate();
        }

        private void Detonate()
        {
            if (m_Detonated) return;
            m_Detonated = true;

            // Наносим урон если есть цель
            if (m_LastKnownTargetPos != Vector3.zero)
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
            // Собираем все colliders в сфере взрыва
            Collider[] colliders = Physics.OverlapSphere(center, m_ExplosionRadius, ~0);

            for (int i = 0; i < colliders.Length; i++)
            {
                Rigidbody rb = colliders[i].GetComponent<Rigidbody>();
                if (rb == null) continue;

                IDamageable damageable = rb.GetComponent<IDamageable>();
                if (damageable == null || !damageable.IsAlive) continue;

                // Расчёт урона по расстоянию (формула не меняется)
                float distance = Vector3.Distance(rb.position, transform.position);
                float damage = CalculateDamage(distance);

                if (damage > 0f)
                {
                    damageable.TakeDamage(damage);
                }

                // Apply explosion force (только если есть PlayerMovement)
                PlayerMovement pm = rb.GetComponent<PlayerMovement>();
                if (pm != null)
                {
                    pm.AddExplosionForce(m_ExplosionForce, center, m_ExplosionRadius);
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
            if (m_BurstEffect != null)
            {
                // Ищем ParticleSystem во всех детей
                ParticleSystem ps = m_BurstEffect.GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                {
                    ps.Play();
                }

                // Ищем AudioSource
                AudioSource audio = m_BurstEffect.GetComponent<AudioSource>();
                if (audio == null)
                {
                    audio = m_BurstEffect.GetComponentInChildren<AudioSource>();
                }
                
                if (audio != null && m_ExplosionAudio != null)
                {
                    audio.clip = m_ExplosionAudio;
                    audio.Play();
                }

                // Дезактивируем объект после завершения анимации (не destroy!)
                // Используем Invoke с отменой — безопасно для пула
                float duration = ps != null ? ps.main.duration : 0.1f;
                Invoke(nameof(DisableBurstEffect), duration);
            }
        }

        private void DisableBurstEffect()
        {
            if (m_BurstEffect != null)
            {
                m_BurstEffect.SetActive(false);
            }
        }

        private void ReturnToPool()
        {
            // Отменяем Invoke если он ещё не сработал
            CancelInvoke();

            // Отключаем коллайдер
            if (m_Collider != null)
            {
                m_Collider.enabled = false;
            }

            // Возвращаем снаряд в пул через PoolManager
            if (LevelManager.Instance != null && LevelManager.Instance.Pool != null)
            {
                LevelManager.Instance.Pool.ReleaseShell(this);
            }
            else
            {
                // Fallback: уничтожаем если пул не доступен
                Object.Destroy(gameObject);
            }
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
            CancelInvoke();

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
            CancelInvoke();

            // Отключаем коллайдер пока объект в пуле
            if (m_Collider != null)
            {
                m_Collider.enabled = false;
            }

            gameObject.SetActive(false);
        }
    }
}
