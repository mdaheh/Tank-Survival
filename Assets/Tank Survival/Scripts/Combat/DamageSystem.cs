using System.Collections.Generic;

namespace TankSurvival
{
    /// <summary>
    /// Единая точка урона и маршрутизации смертей.
    /// Публикует OnEnemyKilled / OnPlayerKilled для всех подписчиков.
    /// </summary>
    public static class DamageSystem
    {
        public static event System.Action<IDamageable> OnEnemyKilled;
        public static event System.Action OnPlayerKilled;

        private static readonly List<EnemyHealth> s_EnemyList = new();
        private static readonly List<TankHealth> s_PlayerList = new();

        /// <summary>
        /// Применить урон через DamageSystem
        /// </summary>
        public static void ApplyDamage(IDamageable target, float amount)
        {
            if (target == null || !target.IsAlive) return;
            target.TakeDamage(amount);
        }

        /// <summary>
        /// Зарегистрировать врага для отслеживания смерти
        /// </summary>
        public static void RegisterEnemy(EnemyHealth health)
        {
            if (health == null) return;
            if (s_EnemyList.Contains(health)) return;
            health.DeathEvent += HandleEnemyDeath;
            s_EnemyList.Add(health);
        }

        public static void RegisterEnemy(IDamageable damageable)
        {
            EnemyHealth health = damageable as EnemyHealth;
            if (health == null) return;
            RegisterEnemy(health);
        }

        /// <summary>
        /// Отменить регистрацию врага
        /// </summary>
        public static void UnregisterEnemy(EnemyHealth health)
        {
            if (health == null) return;
            health.DeathEvent -= HandleEnemyDeath;
            s_EnemyList.Remove(health);
        }

        public static void UnregisterEnemy(IDamageable damageable)
        {
            EnemyHealth health = damageable as EnemyHealth;
            if (health == null) return;
            UnregisterEnemy(health);
        }

        /// <summary>
        /// Зарегистрировать игрока для отслеживания смерти
        /// </summary>
        public static void RegisterPlayer(TankHealth health)
        {
            if (health == null) return;
            if (s_PlayerList.Contains(health)) return;
            health.OnDeathEvent += HandlePlayerDeath;
            s_PlayerList.Add(health);
        }

        /// <summary>
        /// Отменить регистрацию игрока
        /// </summary>
        public static void UnregisterPlayer(TankHealth health)
        {
            if (health == null) return;
            health.OnDeathEvent -= HandlePlayerDeath;
            s_PlayerList.Remove(health);
        }

        /// <summary>
        /// Очистить все подписки (при сбросе мира)
        /// </summary>
        public static void Clear()
        {
            foreach (var health in s_EnemyList)
            {
                var dmg = health as IDamageable;
                if (dmg != null)
                    dmg.DeathEvent -= HandleEnemyDeath;
            }
            s_EnemyList.Clear();

            foreach (var health in s_PlayerList)
                health.OnDeathEvent -= HandlePlayerDeath;
            s_PlayerList.Clear();
        }

        private static void HandleEnemyDeath(IDamageable damageable)
        {
            EnemyHealth health = damageable as EnemyHealth;
            // Снимаем подписку и убираем из списка сразу: иначе список живых растёт
            // всю сессию, а при пуле (T023) повторная регистрация была бы пропущена.
            if (health != null)
                UnregisterEnemy(health);
            OnEnemyKilled?.Invoke(damageable);
        }

        private static void HandlePlayerDeath()
        {
            OnPlayerKilled?.Invoke();
        }
    }
}
