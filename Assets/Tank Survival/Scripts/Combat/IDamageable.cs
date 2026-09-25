using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Интерфейс повреждаемого объекта.
    /// Используется для поиска целей стрельбой и обработки урона.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Получить Transform объекта (для доступа к позиции и gameObject)
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Получить текущее здоровье (не меньше 0)
        /// </summary>
        float CurrentHealth { get; }

        /// <summary>
        /// Получить максимальное здоровье
        /// </summary>
        float MaxHealth { get; }

        /// <summary>
        /// Получить жив ли объект
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        /// Принять урон. Вызывает DeathEvent при достижении HP = 0.
        /// </summary>
        void TakeDamage(float amount);

        /// <summary>
        /// Событие смерти — вызывается один раз при достижении HP = 0
        /// </summary>
        event System.Action<IDamageable> DeathEvent;
    }
}
