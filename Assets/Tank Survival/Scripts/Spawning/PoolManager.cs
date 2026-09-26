using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace TankSurvival
{
    /// <summary>
    /// Пул врагов на UnityEngine.Pool.ObjectPool.
    /// Создаётся при инициализации, враги возвращаются в пул вместо Destroy.
    /// Множители статов применяются при Get (не *= на инстансе).
    /// </summary>
    public sealed class PoolManager
    {
        public static PoolManager Instance { get; private set; }

        private readonly Dictionary<GameObject, ObjectPool<EnemyHealth>> m_EnemyPools =
            new Dictionary<GameObject, ObjectPool<EnemyHealth>>();

        private readonly Dictionary<EnemyHealth, ObjectPool<EnemyHealth>> m_OwnerPools =
            new Dictionary<EnemyHealth, ObjectPool<EnemyHealth>>();

        private readonly Dictionary<EnemyHealth, EnemyMovement> m_Movements =
            new Dictionary<EnemyHealth, EnemyMovement>();

        private readonly Dictionary<EnemyHealth, EnemyAI> m_AIs =
            new Dictionary<EnemyHealth, EnemyAI>();

        private readonly Dictionary<EnemyHealth, float> m_BaseSpeeds =
            new Dictionary<EnemyHealth, float>();

        private readonly Dictionary<EnemyHealth, int> m_Layers =
            new Dictionary<EnemyHealth, int>();

        private ObjectPool<Projectile> m_ShellPool;
        private Projectile m_ShellPrefab;

        /// <summary>
        /// Инициализация пулов врагов по префабам.
        /// prewarmCount — сколько объектов создать при старте; maxPoolSize — лимит.
        /// </summary>
        public PoolManager(GameObject[] enemyPrefabs, int prewarmCount, int maxPoolSize)
        {
            Instance = this;

            if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

            prewarmCount = Mathf.Max(0, prewarmCount);
            maxPoolSize = Mathf.Max(1, maxPoolSize);

            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                GameObject prefab = enemyPrefabs[i];
                ObjectPool<EnemyHealth> pool = null;
                pool = new ObjectPool<EnemyHealth>(
                    createFunc: () => CreateEnemy(prefab, pool),
                    actionOnGet: OnGetEnemy,
                    actionOnRelease: OnReleaseEnemy,
                    actionOnDestroy: OnDestroyEnemy,
                    collectionCheck: true,
                    defaultCapacity: prewarmCount > 0 ? prewarmCount : maxPoolSize,
                    maxSize: maxPoolSize
                );

                // Prewarm — создаём начальные объекты
                if (prewarmCount > 0)
                {
                    var list = new List<EnemyHealth>(prewarmCount);
                    for (int j = 0; j < prewarmCount; j++)
                    {
                        list.Add(pool.Get());
                    }
                    for (int j = 0; j < list.Count; j++)
                    {
                        pool.Release(list[j]);
                    }
                }

                m_EnemyPools[prefab] = pool;
            }
        }

        /// <summary>
        /// Взять врага из пула. Множители применяются при Get.
        /// </summary>
        public EnemyHealth GetEnemy(
            GameObject prefab,
            Vector3 position,
            float healthMultiplier,
            float speedMultiplier,
            Transform player)
        {
            if (prefab == null || !m_EnemyPools.TryGetValue(prefab, out ObjectPool<EnemyHealth> pool))
            {
                return null;
            }

            EnemyHealth health = pool.Get();
            GameObject enemy = health.gameObject;

            enemy.layer = m_Layers[health];
            enemy.transform.SetPositionAndRotation(position, Quaternion.identity);

            EnemyMovement movement = m_Movements[health];
            if (movement != null)
            {
                movement.m_Speed = m_BaseSpeeds[health] * speedMultiplier;
            }

            // T023: множитель задаётся заново, не *= (иначе при пуле накопится)
            health.SetHealthMultiplier(healthMultiplier);

            EnemyAI ai = m_AIs[health];
            if (ai != null)
            {
                ai.SetPlayer(player);
            }

            return health;
        }

        /// <summary>
        /// Вернуть врага в пул (смерть/деспавн).
        /// </summary>
        public void Release(EnemyHealth health)
        {
            if (health == null || !m_OwnerPools.TryGetValue(health, out ObjectPool<EnemyHealth> pool))
            {
                return;
            }

            pool.Release(health);
        }

        private EnemyHealth CreateEnemy(GameObject prefab, ObjectPool<EnemyHealth> ownerPool)
        {
            GameObject enemy = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();

            m_OwnerPools.Add(health, ownerPool);
            m_Movements.Add(health, movement);
            m_AIs.Add(health, enemy.GetComponent<EnemyAI>());
            m_BaseSpeeds.Add(health, movement != null ? movement.m_Speed : 0f);
            m_Layers.Add(health, enemy.layer);

            return health;
        }

        private void OnGetEnemy(EnemyHealth health)
        {
            // T023: сброс статов к базовым при взятии из пула
            EnemyMovement movement = m_Movements[health];
            if (movement != null)
            {
                movement.m_Speed = m_BaseSpeeds[health];
            }

            health.gameObject.layer = m_Layers[health];
            health.SetHealthMultiplier(1f);
            health.gameObject.SetActive(true);
        }

        private static void OnReleaseEnemy(EnemyHealth health)
        {
            // T023: отписка от реестра и DamageSystem при возврате в пул
            EnemyRegistry.Unregister(health);
            DamageSystem.UnregisterEnemy(health);
            health.gameObject.SetActive(false);
        }

        private void OnDestroyEnemy(EnemyHealth health)
        {
            m_OwnerPools.Remove(health);
            m_Movements.Remove(health);
            m_AIs.Remove(health);
            m_BaseSpeeds.Remove(health);
            m_Layers.Remove(health);
            Object.Destroy(health.gameObject);
        }

        /// <summary>
        /// Инициализация пула снарядов (T024).
        /// </summary>
        public void InitShellPool(Projectile shellPrefab, int prewarmCount = 16, int maxPoolSize = 100)
        {
            if (shellPrefab == null) return;
            m_ShellPrefab = shellPrefab;

            m_ShellPool = new ObjectPool<Projectile>(
                createFunc: () => CreateShell(shellPrefab),
                actionOnGet: OnGetShell,
                actionOnRelease: OnReleaseShell,
                actionOnDestroy: OnDestroyShell,
                collectionCheck: true,
                defaultCapacity: prewarmCount,
                maxSize: maxPoolSize
            );

            if (prewarmCount > 0)
            {
                var list = new List<Projectile>(prewarmCount);
                for (int i = 0; i < prewarmCount; i++)
                {
                    list.Add(m_ShellPool.Get());
                }
                for (int i = 0; i < list.Count; i++)
                {
                    m_ShellPool.Release(list[i]);
                }
            }
        }

        /// <summary>
        /// Взять снаряд из пула.
        /// </summary>
        public Projectile GetShell(Vector3 position, Quaternion rotation)
        {
            if (m_ShellPool == null) return null;
            Projectile shell = m_ShellPool.Get();
            shell.gameObject.SetActive(true);
            shell.transform.SetPositionAndRotation(position, rotation);
            return shell;
        }

        /// <summary>
        /// Вернуть снаряд в пул.
        /// </summary>
        public void ReleaseShell(Projectile shell)
        {
            if (shell == null || m_ShellPool == null) return;
            m_ShellPool.Release(shell);
        }

        private Projectile CreateShell(Projectile prefab)
        {
            Projectile shell = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);

            // T024d: единственная точка доступа снаряда к пулу — внедрённая ссылка
            shell.SetPool(this);

            return shell;
        }

        private void OnGetShell(Projectile shell)
        {
            shell.OnGetFromPool();
        }

        private void OnReleaseShell(Projectile shell)
        {
            shell.OnReleaseFromPool();
        }

        private void OnDestroyShell(Projectile shell)
        {
            Object.Destroy(shell.gameObject);
        }
    }
}
