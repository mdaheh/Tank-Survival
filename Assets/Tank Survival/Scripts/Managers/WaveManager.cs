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
        
        // Список инстансов врагов текущей волны — для чистки и пула в Ф1/T023
        private readonly List<GameObject> m_WaveEnemies = new List<GameObject>();

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

            // T022: подписка на смерть врагов через DamageSystem
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
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            
            // Сохраняем ссылку для чистки
            m_WaveEnemies.Add(enemy);

            // Применяем множители сложности к движению
            EnemyMovement move = enemy.GetComponent<EnemyMovement>();
            if (move)
            {
                move.m_Speed *= currentEnemySpeedMultiplier;
            }

            // Применяем множитель сложности к здоровью и подписываемся на смерть через DamageSystem (T022)
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.ApplyHealthMultiplier(currentEnemyHealthMultiplier);
                // T022: регистрируем в DamageSystem вместо прямой подписки на DeathEvent
                DamageSystem.RegisterEnemy(health);
                // T021: регистрируем в реестре
                EnemyRegistry.Register(health);
            }

            // Указываем врагу, кто игрок
            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
                ai.SetPlayer(playerTransform);

            OnEnemySpawned?.Invoke(enemiesRemaining, enemiesToSpawn);
        }

        /// <summary>
        /// Обработка смерти врага (T022: вызывается через DamageSystem.OnEnemyKilled)
        /// </summary>
        private void HandleEnemyDeath(EnemyHealth health)
        {
            // T021: unregister из реестра
            if (health != null)
                EnemyRegistry.Unregister(health);

            enemiesRemaining--;

            if (enemiesRemaining <= 0 && enemiesToSpawn <= 0)
                EndWave();
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
            for (int i = 0; i < m_WaveEnemies.Count; i++)
            {
                if (m_WaveEnemies[i] != null)
                {
                    UnityEngine.Object.Destroy(m_WaveEnemies[i]);
                }
            }
            m_WaveEnemies.Clear();
            // T021: очистить реестр при чистке мира
            EnemyRegistry.Clear();
            // T022: отписка от DamageSystem
            DamageSystem.OnEnemyKilled -= HandleEnemyDeath;
            
            enemiesRemaining = 0;
            enemiesToSpawn = 0;
            m_WaveActive = false;
        }
    }
}
