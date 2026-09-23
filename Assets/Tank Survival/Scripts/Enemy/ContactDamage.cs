using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Контактный урон врага — наносит урон танку игрока при столкновении.
    /// Поведение: при столкновении с объектом слоя Players вызывает TankHealth.TakeDamage.
    /// Использует OnCollisionEnter (не триггер), кулдаун — простое поле-таймер.
    /// </summary>
    public class ContactDamage : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("Урон за один контакт с танком игрока")]
        public float m_Damage = 10f;

        [Tooltip("Минимальный интервал между контактами (секунды)")]
        public float m_Cooldown = 0.5f;

        private float m_LastDamageTime;

        private void OnCollisionEnter(Collision other)
        {
            // Проверяем слой — только объекты Players
            int layer = other.gameObject.layer;
            int playerLayer = LayerMask.NameToLayer("Players");
            if ((1 << layer) != (1 << playerLayer))
                return;

            // Проверяем кулдаун
            float now = Time.time;
            if (now - m_LastDamageTime < m_Cooldown)
                return;

            // Наносим урон
            m_LastDamageTime = now;

            // Ищем TankHealth на танке игрока
            TankHealth health = other.gameObject.GetComponent<TankHealth>();
            if (health != null)
            {
                health.TakeDamage(m_Damage);
            }
        }
    }
}
