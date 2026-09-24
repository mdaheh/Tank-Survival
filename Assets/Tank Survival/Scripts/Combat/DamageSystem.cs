using System.Collections.Generic;

namespace TankSurvival
{
    /// <summary>
    /// Единая точка урона и маршрутизации смертей.
    /// Публикует OnEnemyKilled / OnPlayerKilled для всех подписчиков.
    /// </summary>
    public static class DamageSystem
    {
        public static event System.Action<EnemyHealth> OnEnemyKilled;
        public static event System.Action OnPlayerKilled;

        private static readonly List<(EnemyHealth, System.Action<EnemyHealth>)> s_EnemySubscriptions = new();
        private static readonly List<(TankHealth, System.Action)> s_PlayerSubscriptions = new();

        /// <summary>
        /// Применить урон через DamageSystem
        /// </summary>
        public static void ApplyDamage(IDamageable target, float amount)
        {
            target.TakeDamage(amount);
        }

        /// <summary>
        /// Зарегистрировать врага для отслеживания смерти
        /// </summary>
        public static void RegisterEnemy(EnemyHealth health)
        {
            if (health == null) return;
            health.DeathEvent += HandleEnemyDeath;
            s_EnemySubscriptions.Add((health, HandleEnemyDeath));
        }

        /// <summary>
        /// Отменить регистрацию врага
        /// </summary>
        public static void UnregisterEnemy(EnemyHealth health)
        {
            if (health == null) return;
            health.DeathEvent -= HandleEnemyDeath;
            s_EnemySubscriptions.RemoveAll(s => s.Item1 == health);
        }

        /// <summary>
        /// Зарегистрировать игрока для отслеживания смерти
        /// </summary>
        public static void RegisterPlayer(TankHealth health)
        {
            if (health == null) return;
            health.OnDeathEvent += HandlePlayerDeath;
            s_PlayerSubscriptions.Add((health, HandlePlayerDeath));
        }

        /// <summary>
        /// Отменить регистрацию игрока
        /// </summary>
        public static void UnregisterPlayer(TankHealth health)
        {
            if (health == null) return;
            health.OnDeathEvent -= HandlePlayerDeath;
            s_PlayerSubscriptions.RemoveAll(s => s.Item1 == health);
        }

        /// <summary>
        /// Очистить все подписки (при сбросе мира)
        /// </summary>
        public static void Clear()
        {
            foreach (var (health, callback) in s_EnemySubscriptions)
                health.DeathEvent -= callback;
            s_EnemySubscriptions.Clear();

            foreach (var (health, callback) in s_PlayerSubscriptions)
                health.OnDeathEvent -= callback;
            s_PlayerSubscriptions.Clear();
        }

        private static void HandleEnemyDeath(EnemyHealth health)
        {
            OnEnemyKilled?.Invoke(health);
        }

        private static void HandlePlayerDeath()
        {
            OnPlayerKilled?.Invoke();
        }
    }
}
