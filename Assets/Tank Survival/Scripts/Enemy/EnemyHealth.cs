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
        private bool m_Dead;

        /// <summary>
        /// Событие смерти — вызывается один раз при достижении HP = 0
        /// </summary>
        public event System.Action DeathEvent;

        // Реализация IDamageable
        public float CurrentHealth => m_CurrentHealth;
        public float MaxHealth => m_StartingHealth;
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
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
        }

        /// <summary>
        /// Применить множитель HP (сложность/волна) и пересчитать текущее здоровье.
        /// T020: сюда перенесена логика, раньше бывшая в WaveManager:
        /// health.m_StartingHealth *= multiplier; health.ResetHealth();
        /// </summary>
        public void ApplyHealthMultiplier(float multiplier)
        {
            m_StartingHealth *= multiplier;
            m_CurrentHealth = m_StartingHealth;
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
            DeathEvent?.Invoke();

            // T020: прячем тело — как это делал TankHealth.OnDeath; объект остаётся в m_WaveEnemies до чистки волны
            gameObject.SetActive(false);
        }
    }
}
