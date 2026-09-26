using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TankSurvival
{
    /// <summary>
    /// Менеджер волн врагов — ядро геймплея в стиле Vampire Survivors.
    /// Спавнит врагов волнами, отслеживает оставшихся, сигнализирует об окончании волны.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Spawn Settings")]
        public Transform spawnPoint;            // Точка спавна врагов (по краям карты)
        public Transform playerTransform;       // Ссылка на игрока (для AI)
        public GameObject[] enemyPrefabs;       // Доступные типы врагов для спавна
        [Header("Enemy Pool")]
        [SerializeField, Min(0)] private int m_EnemyPoolPrewarmCount = 32;
        [SerializeField, Min(1)] private int m_EnemyPoolMaxSize = 300;
        [Header("Shell Pool (T024)")]
        [SerializeField] private Projectile m_ShellPrefab;
        [SerializeField, Min(0)] private int m_ShellPoolPrewarmCount = 16;
        [SerializeField, Min(1)] private int m_ShellPoolMaxSize = 100;
        [Header("VFX Pool (T024c)")]
        [SerializeField] private GameObject m_ExplosionPrefab;

        [Header("Current Wave Info")]
        public int currentWave;                   // Текущая волна (начинается с 1)
        public int enemiesRemaining;              // Врагов, которые ещё живы
        public int enemiesToSpawn;                // Врагов, которые ещё не спавнились
        public float spawnTimer;                  // Таймер до следующего спавна
        public float spawnInterval;               // Интервал между спавнами (секунды)

        [Header("Difficulty Multipliers (applied at wave start)")]
        public float currentEnemyHealthMultiplier;
        public float currentEnemySpeedMultiplier;

        private bool m_WaveActive;
        private DifficultyData m_CurrentDifficulty;
        private PoolManager m_EnemyPool;
        
        // Список активных инстансов врагов текущей волны — для очистки и пула в Ф1/T023
        private readonly List<EnemyHealth> m_WaveEnemies = new List<EnemyHealth>();

        // События
        public UnityEvent<int> OnWaveStarted;     // (waveNumber)
        public UnityEvent<int, int> OnEnemySpawned; // (remaining, toSpawn)
        public UnityEvent<int> OnEnemyDied;       // (remaining) — renamed to avoid conflict
        public UnityEvent<int> OnWaveCompleted;   // (waveNumber)
        public UnityEvent OnAllWavesCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            m_EnemyPool = new PoolManager(enemyPrefabs, m_EnemyPoolPrewarmCount, m_EnemyPoolMaxSize);
            if (m_ShellPrefab != null)
            {
                m_EnemyPool.InitShellPool(m_ShellPrefab, m_ShellPoolPrewarmCount, m_ShellPoolMaxSize);
            }
            // T024c: Инициализация пула VFX-эффектов взрыва
            if (m_ExplosionPrefab != null)
            {
                var burstEffect = m_ExplosionPrefab.GetComponent<BurstEffect>();
                if (burstEffect != null)
                {
                    m_EnemyPool.InitBurstPool(burstEffect, prewarmCount: 4, maxPoolSize: 32);
                }
            }
        }

        /// <summary>
        /// Начать новую волну с заданной сложностью
        /// </summary>
        public void StartWave(DifficultyData difficulty, int waveNumber = 1, int totalWaves = 3)
        {
            m_CurrentDifficulty = difficulty;
            currentWave = waveNumber;
            m_WaveActive = true;

            // Рассчитать количество врагов: база * коэффициент волны
            int baseCount = difficulty.baseEnemyCount;
            enemiesToSpawn = baseCount + (waveNumber - 1) * 5; // +5 врагов за каждую волну
            enemiesRemaining = enemiesToSpawn;

            // Применить множители сложности
            currentEnemyHealthMultiplier = difficulty.enemyHealthMultiplier + (waveNumber - 1) * 0.2f;
            currentEnemySpeedMultiplier = difficulty.enemySpeedMultiplier + (waveNumber - 1) * 0.1f;

            // Таймер спавна
            spawnInterval = difficulty.spawnInterval - (waveNumber - 1) * 0.05f;
            if (spawnInterval < 0.3f) spawnInterval = 0.3f; // Минимальный интервал
            spawnTimer = spawnInterval; // Первый спавн сразу

            OnWaveStarted?.Invoke(currentWave);

            // T022: подписка на смерть врагов через DamageSystem.
            // Минус перед плюсом обязателен: StartWave вызывается на каждой волне,
            // и без отписки обработчик дублируется — enemiesRemaining уменьшался бы
            // дважды за одного убитого врага.
            DamageSystem.OnEnemyKilled -= HandleEnemyDeath;
            DamageSystem.OnEnemyKilled += HandleEnemyDeath;
        }

        private void Update()
        {
            if (!m_WaveActive) return;

            // Спавн врагов
            if (enemiesToSpawn > 0)
            {
                spawnTimer -= Time.deltaTime;
                if (spawnTimer <= 0f)
                {
                    SpawnEnemy();
                    enemiesToSpawn--;
                    spawnTimer = spawnInterval;
                }
            }
            // Проверка окончания волны
            else if (enemiesRemaining <= 0)
            {
                EndWave();
            }
        }

        /// <summary>
        /// Спавнит одного врага в случайной точке спавна
        /// </summary>
        private void SpawnEnemy()
        {
            if (spawnPoint == null)
            {
                Debug.LogError("[WaveManager] Не указана точка спавна!");
                return;
            }

            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                Debug.LogError("[WaveManager] Не указаны префабы врагов!");
                return;
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("[WaveManager] Ссылка на игрока не указана!");
                return;
            }

            if (m_EnemyPool == null)
            {
                Debug.LogError("[WaveManager] Пул врагов не инициализирован.");
                return;
            }

            // Случайная точка спавна (добавляем случайное смещение по кругу)
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = 15f; // Радиус спавна вокруг игрока
            Vector3 spawnPosition = playerTransform.position + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            // Случайный тип врага
            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            EnemyHealth health = m_EnemyPool.GetEnemy(
                enemyPrefab,
                spawnPosition,
                currentEnemyHealthMultiplier,
                currentEnemySpeedMultiplier,
                playerTransform);
            if (health == null)
            {
                Debug.LogError("[WaveManager] Не удалось получить врага из пула.");
                return;
            }

            m_WaveEnemies.Add(health);
            DamageSystem.RegisterEnemy(health);
            EnemyRegistry.Register(health);

            OnEnemySpawned?.Invoke(enemiesRemaining, enemiesToSpawn);
        }

        /// <summary>
        /// Обработка смерти врага (T022: вызывается через DamageSystem.OnEnemyKilled)
        /// </summary>
        private void HandleEnemyDeath(IDamageable damageable)
        {
            EnemyHealth health = damageable as EnemyHealth;
            if (health == null)
            {
                return;
            }

            // T074: счётчик волны меняется только для врагов текущей волны.
            // Смерть «чужого» врага (например, взятого из пула вне волны) не должна
            // уменьшать enemiesRemaining и запускать EndWave.
            int index = m_WaveEnemies.IndexOf(health);
            bool belongsToWave = index >= 0;
            if (belongsToWave)
            {
                int lastIndex = m_WaveEnemies.Count - 1;
                m_WaveEnemies[index] = m_WaveEnemies[lastIndex];
                m_WaveEnemies.RemoveAt(lastIndex);
            }

            // PoolManager снимает регистрацию перед деактивацией объекта.
            m_EnemyPool?.Release(health);

            if (!belongsToWave)
            {
                return;
            }

            enemiesRemaining--;

            if (enemiesRemaining <= 0 && enemiesToSpawn <= 0)
            {
                EndWave();
            }
        }

        /// <summary>
        /// Завершить текущую волну
        /// </summary>
        private void EndWave()
        {
            m_WaveActive = false;

            OnWaveCompleted?.Invoke(currentWave);

            // Проверить, все ли волны пройдены
            // (если нужно больше волн — GameManager решит, продолжать ли)
        }

        /// <summary>
        /// Проверить, активна ли сейчас волна
        /// </summary>
        public bool IsWaveActive => m_WaveActive;

        /// <summary>
        /// Принудительно завершить волну (для отладки или特殊ных ситуаций)
        /// </summary>
        public void ForceEndWave()
        {
            enemiesRemaining = 0;
            enemiesToSpawn = 0;
            EndWave();
        }

        /// <summary>
        /// Пауза/возобновление спавна
        /// </summary>
        public void PauseSpawning(bool pause)
        {
            if (pause)
                spawnTimer = spawnInterval + 1f; // Чтобы не спавнил пока пауза
            else
                spawnTimer = 0f;
        }

        /// <summary>
        /// Очистить все инстансы врагов, созданные в текущей волне
        /// </summary>
        public void ClearWaveEnemies()
        {
            for (int i = m_WaveEnemies.Count - 1; i >= 0; i--)
            {
                if (m_WaveEnemies[i] != null)
                {
                    m_EnemyPool?.Release(m_WaveEnemies[i]);
                }
            }
            m_WaveEnemies.Clear();

            // T021/T023: очистка реестра и отписка от DamageSystem
            EnemyRegistry.Clear();
            DamageSystem.OnEnemyKilled -= HandleEnemyDeath;

            enemiesRemaining = 0;
            enemiesToSpawn = 0;
            m_WaveActive = false;
        }
    }
}
