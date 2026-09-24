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
        /// Принять урон. Смерть объекта сигналится его собственным событием
        /// (EnemyHealth.DeathEvent / TankHealth.OnDeathEvent) — в интерфейс не выносим:
        /// полезная нагрузка события у разных типов разная.
        /// </summary>
        void TakeDamage(float amount);
    }
}
