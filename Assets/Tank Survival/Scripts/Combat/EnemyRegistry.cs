using System.Collections.Generic;
using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Статический реестр живых врагов.
    /// Источник целей для оружия и AoE вместо Physics.OverlapSphere.
    /// Swap-remove без аллокаций на кадр.
    /// </summary>
    public static class EnemyRegistry
    {
        private static readonly List<EnemyHealth> s_LivingEnemies = new List<EnemyHealth>();

        /// <summary>
        /// Количество живых врагов в реестре
        /// </summary>
        public static int Count => s_LivingEnemies.Count;

        /// <summary>
        /// Зарегистрировать врага при спавне
        /// </summary>
        public static void Register(EnemyHealth health)
        {
            if (health == null) return;
            if (s_LivingEnemies.Contains(health)) return;
            s_LivingEnemies.Add(health);
        }

        /// <summary>
        /// Отрегистрировать врага при смерти/деспавне (swap-remove, без аллокаций)
        /// </summary>
        public static void Unregister(EnemyHealth health)
        {
            if (health == null) return;
            int idx = s_LivingEnemies.IndexOf(health);
            if (idx >= 0)
            {
                s_LivingEnemies[idx] = s_LivingEnemies[s_LivingEnemies.Count - 1];
                s_LivingEnemies.RemoveAt(s_LivingEnemies.Count - 1);
            }
        }

        /// <summary>
        /// Очистить все записи (при чистке мира T012)
        /// </summary>
        public static void Clear()
        {
            s_LivingEnemies.Clear();
        }
    }
}
