using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Контактный урон врага — наносит урон танку игрока при столкновении.
    /// Поведение: при столкновении с объектом слоя Players вызывает TankHealth.TakeDamage.
    /// </summary>
    public class ContactDamage : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("Урон за один контакт с танком игрока")]
        public float m_Damage = 10f;

        [Tooltip("Минимальный интервал между контактами (секунды)")]
        public float m_Cooldown = 0.5f;

        private float m_LastDamageTime;
        private bool m_HasTriggered;

        private void OnTriggerEnter(Collider other)
        {
            // Проверяем слой — только объекты Players
            int layer = other.gameObject.layer;
            int playerLayer = LayerMask.NameToLayer("Players");
            if ((1 << layer) != (1 << playerLayer))
                return;

            // Предотвращаем повторный триггер для одного и того же столкновения
            if (m_HasTriggered)
                return;

            // Проверяем кулдаун
            float now = Time.time;
            if (now - m_LastDamageTime < m_Cooldown)
                return;

            // Наносим урон
            m_LastDamageTime = now;

            // Ищем TankHealth на танке игрока
            TankHealth health = other.GetComponent<TankHealth>();
            if (health != null)
            {
                health.TakeDamage(m_Damage);
            }

            m_HasTriggered = true;
        }

        private void OnTriggerExit(Collider other)
        {
            // Сбрасываем флаг при выходе из триггера — разрешаем новый контакт
            m_HasTriggered = false;
        }
    }
}
