using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Пулируемый VFX-компонент взрыва (T024c).
    /// Добавляется на префаб CompleteShellExplosion (корень).
    /// Кэширует ParticleSystem-ы и AudioSource; при PlayAndRelease
    /// запускает эффект и автоматически возвращает себя в пул
    /// после завершения самой длинной частицы.
    /// </summary>
    public class BurstEffect : MonoBehaviour, IPoolable
    {
        private ParticleSystem[] m_Systems;
        private AudioSource m_Audio;
        private PoolManager m_Pool;
        private float m_MaxDuration;
        private bool m_Detonated;

        private void Awake()
        {
            // Кэшируем все ParticleSystem: корень + дети (Trails, Burst)
            var childSystems = GetComponentsInChildren<ParticleSystem>();
            m_Systems = new ParticleSystem[childSystems.Length + 1];
            var rootPS = GetComponent<ParticleSystem>();
            int idx = 0;
            if (rootPS != null)
            {
                m_Systems[idx++] = rootPS;
            }
            for (int i = 0; i < childSystems.Length; i++)
            {
                m_Systems[idx++] = childSystems[i];
            }

            m_Audio = GetComponent<AudioSource>();

            // Вычисляем максимальную длительность частиц для авто-возврата
            m_MaxDuration = 0.5f;
            for (int i = 0; i < m_Systems.Length; i++)
            {
                if (m_Systems[i] != null)
                {
                    float d = m_Systems[i].main.duration + m_Systems[i].main.startLifetime.constantMax;
                    if (d > m_MaxDuration) m_MaxDuration = d;
                }
            }
        }

        /// <summary>
        /// Запуск эффекта и авто-возврат в пул после завершения (T024c).
        /// </summary>
        public void PlayAndRelease()
        {
            m_Detonated = false;
            CancelInvoke();

            for (int i = 0; i < m_Systems.Length; i++)
            {
                if (m_Systems[i] != null) m_Systems[i].Play();
            }

            if (m_Audio != null)
            {
                m_Audio.Play();
            }

            Invoke(nameof(ReturnToPool), m_MaxDuration);
        }

        private void ReturnToPool()
        {
            if (m_Detonated) return;
            m_Detonated = true;

            if (m_Pool != null)
            {
                m_Pool.ReleaseBurst(this);
            }
        }

        // --- IPoolable ---

        public void OnGetFromPool()
        {
            // Сброс не нужен: PlayAndRelease вызывается явно после GetBurst
        }

        public void OnReleaseFromPool()
        {
            CancelInvoke();
            m_Detonated = true;

            for (int i = 0; i < m_Systems.Length; i++)
            {
                if (m_Systems[i] != null) m_Systems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (m_Audio != null)
            {
                m_Audio.Stop();
            }

            gameObject.SetActive(false);
        }

        /// <summary>Инъекция пула при создании инстанса (T024c).</summary>
        public void SetPool(PoolManager pool)
        {
            m_Pool = pool;
        }
    }
}
