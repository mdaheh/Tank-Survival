using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace TankSurvival
{
    /// <summary>
    /// Пул противников. Экземпляры создаются только при инициализации и росте
    /// максимального размера, а между волнами переиспользуются.
    /// </summary>
    public sealed class PoolManager
    {
        private readonly Dictionary<GameObject, ObjectPool<EnemyHealth>> m_Pools =
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

        public PoolManager(GameObject[] enemyPrefabs, int prewarmCount, int maxPoolSize)
        {
            if (enemyPrefabs == null)
            {
                return;
            }

            prewarmCount = Mathf.Max(0, prewarmCount);
            maxPoolSize = Mathf.Max(1, maxPoolSize);

            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                GameObject prefab = enemyPrefabs[i];
                if (prefab == null || prefab.GetComponent<EnemyHealth>() == null)
                {
                    Debug.LogError("[PoolManager] Префаб врага должен содержать EnemyHealth.");
                    continue;
                }

                if (m_Pools.ContainsKey(prefab))
                {
                    continue;
                }

                ObjectPool<EnemyHealth> pool = null;
                pool = new ObjectPool<EnemyHealth>(
                    () => CreateEnemy(prefab, pool),
                    OnGetEnemy,
                    OnReleaseEnemy,
                    OnDestroyEnemy,
                    true,
                    prewarmCount,
                    maxPoolSize);

                m_Pools.Add(prefab, pool);

                // ObjectPool не имеет отдельного prewarm API: сначала удерживаем
                // все новые объекты, затем возвращаем их одним проходом.
                EnemyHealth[] prewarmBuffer = new EnemyHealth[prewarmCount];
                for (int j = 0; j < prewarmBuffer.Length; j++)
                {
                    prewarmBuffer[j] = pool.Get();
                }
                for (int j = 0; j < prewarmBuffer.Length; j++)
                {
                    pool.Release(prewarmBuffer[j]);
                }
            }
        }

        public EnemyHealth GetEnemy(
            GameObject prefab,
            Vector3 position,
            float healthMultiplier,
            float speedMultiplier,
            Transform player)
        {
            if (prefab == null || !m_Pools.TryGetValue(prefab, out ObjectPool<EnemyHealth> pool))
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

            health.SetHealthMultiplier(healthMultiplier);

            EnemyAI ai = m_AIs[health];
            if (ai != null)
            {
                ai.SetPlayer(player);
            }

            return health;
        }

        public void Release(EnemyHealth health)
        {
            if (health == null || !m_OwnerPools.TryGetValue(health, out ObjectPool<EnemyHealth> pool))
            {
                return;
            }

            // collectionCheck включён в конструкторе ObjectPool: повторный Release
            // обнаруживается Unity, а не приводит к тихой порче состояния врага.
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
    }
}
