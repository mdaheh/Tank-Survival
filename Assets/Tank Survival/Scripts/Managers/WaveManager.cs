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

            Debug.Log($"[WaveManager] Волна {waveNumber}: {enemiesToSpawn} врагов, HP x{currentEnemyHealthMultiplier:F1}, Speed x{currentEnemySpeedMultiplier:F1}");

            OnWaveStarted?.Invoke(currentWave);
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

            // Применяем множители сложности к движению
            EnemyMovement move = enemy.GetComponent<EnemyMovement>();
            if (move)
            {
                move.m_Speed *= currentEnemySpeedMultiplier;
            }

            // Применяем множители сложности к здоровью
            TankHealth health = enemy.GetComponent<TankHealth>();
            if (health)
            {
                health.m_StartingHealth *= currentEnemyHealthMultiplier;
                health.ResetHealth(); // Пересчитать текущее здоровье
            }

            // Указываем врагу, кто игрок
            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
                ai.SetPlayer(playerTransform);

            // Подписываемся на смерть врага
            var listener = enemy.AddComponent<EnemyDeathListener>();
            listener.OnDeath.AddListener(HandleEnemyDeath);

            OnEnemySpawned?.Invoke(enemiesRemaining, enemiesToSpawn);
        }

        /// <summary>
        /// Обработка смерти врага
        /// </summary>
        private void HandleEnemyDeath()
        {
            enemiesRemaining--;
            OnEnemyDied?.Invoke(enemiesRemaining);

            if (enemiesRemaining <= 0 && enemiesToSpawn <= 0)
                EndWave();
        }

        /// <summary>
        /// Завершить текущую волну
        /// </summary>
        private void EndWave()
        {
            m_WaveActive = false;
            Debug.Log($"[WaveManager] Волна {currentWave} завершена!");

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
    }

    /// <summary>
    /// Вспомогательный компонент — подписывается на смерть врага и сообщает WaveManager
    /// </summary>
    public class EnemyDeathListener : MonoBehaviour
    {
        public UnityEvent OnDeath;

        private void OnDestroy()
        {
            OnDeath?.Invoke();
        }
    }
}
