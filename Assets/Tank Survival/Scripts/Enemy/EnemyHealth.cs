using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Здоровье врага — без UI, с событием смерти.
    /// HP масштабируется по волнам.
    /// Заменяет TankHealth на вражеских префабах.
    /// </summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [SerializeField] private float m_StartingHealth = 30f;

        private float m_CurrentHealth;
        private float m_HealthMultiplier = 1f;

        private bool m_Dead;

        /// <summary>
        /// Событие смерти — вызывается один раз при достижении HP = 0
        /// </summary>
        public event System.Action<IDamageable> DeathEvent;

        // Реализация IDamageable
        public Transform Transform => transform;
        public float CurrentHealth => m_CurrentHealth;
        public float MaxHealth => m_StartingHealth * m_HealthMultiplier;
        public bool IsAlive => !m_Dead;

        private void OnEnable()
        {
            ResetHealth();
        }

        /// <summary>
        /// Сбросить здоровье к начальному (вызывается при включении объекта)
        /// </summary>
        public void ResetHealth()
        {
            m_CurrentHealth = m_StartingHealth * m_HealthMultiplier;
            m_Dead = false;
        }

        /// <summary>
        /// Задать множитель HP для текущего спавна. Предыдущий множитель не сохраняется.
        /// </summary>
        public void SetHealthMultiplier(float multiplier)
        {
            m_HealthMultiplier = multiplier;
            ResetHealth();
        }

        /// <summary>
        /// T026: задать базовое здоровье типа врага (источник — данные EnemyData).
        /// Текущий множитель волны сохраняется: HP = база × множитель.
        /// </summary>
        public void SetBaseHealth(float value)
        {
            m_StartingHealth = Mathf.Max(1f, value);
            ResetHealth();
        }

        // Сохранено для совместимости с T020. Множитель задаётся заново,
        // а не накапливается умножением на предыдущий.
        public void ApplyHealthMultiplier(float multiplier)
        {
            SetHealthMultiplier(multiplier);
        }

        public void TakeDamage(float amount)
        {
            if (m_Dead) return;

            m_CurrentHealth -= amount;

            if (m_CurrentHealth <= 0f && !m_Dead)
            {
                OnDeath();
            }
        }

        private void OnDeath()
        {
            m_Dead = true;
            DeathEvent?.Invoke(this as IDamageable);

            // T023: возврат в пул выполняется обработчиком WaveManager после события смерти.
            gameObject.SetActive(false);
        }
    }
}
