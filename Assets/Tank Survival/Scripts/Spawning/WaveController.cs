using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TankSurvival
{
    /// <summary>
    /// Контроллер волн (T031, переименован из WaveManager).
    /// Числа волн (число волн, количество врагов, интервал спавна, множители HP/скорости,
    /// пауза между волнами) — из плана волн DifficultyData.waveData (WaveData);
    /// «магических чисел» в коде не остаётся.
    /// Состав волны — взвешенный случайный выбор по EnemyData.waveWeight.
    /// Переход между волнами — поле-таймер (было Invoke(nameof(StartNextWave), 3f) в GameManager).
    /// </summary>
    public class WaveController : MonoBehaviour
    {
        public static WaveController Instance { get; private set; }

        [Header("Spawn Settings")]
        [SerializeField] private SpawnDirector m_SpawnDirector; // T032: точки/зоны спавна — данные сцены
        public Transform playerTransform;       // Ссылка на игрока (для AI)
        public GameObject[] enemyPrefabs;       // Прежний путь: префабы, если состав волны не задан данными

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

        [Header("Wave Multipliers (applied at wave start)")]
        public float currentEnemyHealthMultiplier;
        public float currentEnemySpeedMultiplier;

        // События волны (контракт сохранён — слушатели HUD)
        public UnityEvent<int> OnWaveStarted;       // (waveNumber)
        public UnityEvent<int, int> OnEnemySpawned; // (remaining, toSpawn)
        public UnityEvent<int> OnEnemyDied;         // (remaining)
        public UnityEvent<int> OnWaveCompleted;     // (waveNumber)
        public UnityEvent OnAllWavesCompleted;

        private bool m_WaveActive;
        private WaveData m_Wave;                  // План волн текущего раунда (T031)

        private PoolManager m_EnemyPool;

        // Список активных инстансов врагов текущей волны — для очистки и возврата в пул (T023)
        private readonly List<EnemyHealth> m_WaveEnemies = new List<EnemyHealth>();

        // T031: таймер паузы между волнами (замена Invoke из GameManager)
        private float m_NextWaveTimer;

        /// <summary>
        /// Число волн в раунде — из плана волн (WaveData). Для HUD.
        /// </summary>
        public int TotalWaveCount => m_Wave != null ? m_Wave.waveCount : 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            EnsurePool();
        }

        /// <summary>
        /// Создать пулы (враги, снаряды, VFX). Состав пула врагов берётся из каталога данных,
        /// а DataCatalog.Init выполняется в Awake другого компонента — порядок Awake в Unity
        /// не определён, поэтому обращение к каталогу вынесено из Awake в Start (T031).
        /// </summary>
        private void EnsurePool()
        {
            if (m_EnemyPool != null) return;

            m_EnemyPool = new PoolManager(BuildPrefabList(), m_EnemyPoolPrewarmCount, m_EnemyPoolMaxSize);

            if (m_ShellPrefab != null)
            {
                m_EnemyPool.InitShellPool(m_ShellPrefab, m_ShellPoolPrewarmCount, m_ShellPoolMaxSize);
            }

            // T024c: пул VFX-эффектов взрыва
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
        /// T031: начать раунд. Число волн, их числа и переходы между ними — внутри WaveController,
        /// GameManager только передаёт сложность и слушает OnAllWavesCompleted.
        /// </summary>
        public void StartRound(DifficultyData difficulty)
        {
            WaveData wave = difficulty != null ? difficulty.waveData : null;
            if (wave == null)
            {
                Debug.LogError("[WaveController] У сложности не задан план волн (DifficultyData.waveData) — раунд не начат.");
                return;
            }

            EnsurePool();

            m_Wave = wave;
            m_NextWaveTimer = 0f;

            StartWave(1);
        }

        /// <summary>
        /// Начать волну. T031: числа волны (врагов, интервал, множители) — только из плана волн (WaveData).
        /// </summary>
        private void StartWave(int waveNumber)
        {
            currentWave = waveNumber;
            m_WaveActive = true;

            // Количество врагов: база первой волны + прирост за каждую следующую
            enemiesToSpawn = m_Wave.baseEnemyCount + (waveNumber - 1) * m_Wave.enemiesPerWaveIncrease;
            enemiesRemaining = enemiesToSpawn;

            // Множители HP/скорости: база плана волн + прирост за каждую следующую волну
            currentEnemyHealthMultiplier = m_Wave.enemyHealthMultiplier + (waveNumber - 1) * m_Wave.healthMultiplierIncreasePerWave;
            currentEnemySpeedMultiplier = m_Wave.enemySpeedMultiplier + (waveNumber - 1) * m_Wave.speedMultiplierIncreasePerWave;

            // Таймер спавна: интервал первой волны минус падение за каждую следующую, но не ниже минимума
            spawnInterval = m_Wave.spawnInterval - (waveNumber - 1) * m_Wave.spawnIntervalDecreasePerWave;
            if (spawnInterval < m_Wave.minSpawnInterval) spawnInterval = m_Wave.minSpawnInterval;
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
            // T031: пауза между волнами — поле-таймер вместо Invoke(nameof(StartNextWave), 3f)
            if (m_NextWaveTimer > 0f)
            {
                m_NextWaveTimer -= Time.deltaTime;
                if (m_NextWaveTimer > 0f)
                {
                    return;
                }

                m_NextWaveTimer = 0f;

                // Пауза истекла — начинаем следующую волну (число волн проверено при завершении предыдущей)
                StartWave(currentWave + 1);
                return;
            }

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
            if (m_SpawnDirector == null)
            {
                Debug.LogError("[WaveController] Не назначен SpawnDirector — точки спавна врагов не заданы.");
                return;
            }

            bool hasTypes = m_Wave != null && m_Wave.enemyTypes != null && m_Wave.enemyTypes.Length > 0;
            if (!hasTypes && (enemyPrefabs == null || enemyPrefabs.Length == 0))
            {
                Debug.LogError("[WaveController] В плане волн нет типов врагов (WaveData.enemyTypes) и нет префабов!");
                return;
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("[WaveController] Ссылка на игрока не указана!");
                return;
            }

            if (m_EnemyPool == null)
            {
                Debug.LogError("[WaveController] Пул врагов не инициализирован.");
                return;
            }

            // T032: позиция — из точек/зон спавна (данные сцены), а не круг вокруг игрока
            Vector3 spawnPosition;
            if (!m_SpawnDirector.TryGetSpawnPosition(out spawnPosition))
            {
                Debug.LogError("[WaveController] В SpawnDirector нет валидных точек спавна.");
                return;
            }

            // T031: тип врага — из состава волны (WaveData.enemyTypes) по весам; без данных — прежний путь
            EnemyData enemyType = PickEnemyType();
            GameObject enemyPrefab = enemyType != null
                ? enemyType.prefab
                : enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            if (enemyPrefab == null)
            {
                Debug.LogError("[WaveController] У выбранного типа врага не задан префаб.");
                return;
            }

            EnemyHealth health = m_EnemyPool.GetEnemy(
                enemyPrefab,
                spawnPosition,
                currentEnemyHealthMultiplier,
                currentEnemySpeedMultiplier,
                playerTransform);
            if (health == null)
            {
                Debug.LogError("[WaveController] Не удалось получить врага из пула.");
                return;
            }

            // T026: базовые статы — из данных типа
            if (enemyType != null)
            {
                ApplyEnemyType(health, enemyType);
            }

            m_WaveEnemies.Add(health);
            DamageSystem.RegisterEnemy(health);
            EnemyRegistry.Register(health);

            OnEnemySpawned?.Invoke(enemiesRemaining, enemiesToSpawn);
        }

        /// <summary>
        /// T031: список префабов для пула врагов — все типы из состава волн всех сложностей каталога
        /// (пул создаётся до выбора сложности, а состав волны — данные, поэтому берём объединение).
        /// Благодаря этому новый тип врага в WaveData попадает в игру без правки сцены и кода.
        /// При пустом каталоге используется прежний массив префабов.
        /// </summary>
        private GameObject[] BuildPrefabList()
        {
            List<DifficultyData> difficulties = DataCatalog.GetAllDifficulties();
            if (difficulties != null && difficulties.Count > 0)
            {
                var prefabs = new List<GameObject>();

                for (int d = 0; d < difficulties.Count; d++)
                {
                    DifficultyData difficulty = difficulties[d];
                    WaveData wave = difficulty != null ? difficulty.waveData : null;
                    if (wave == null || wave.enemyTypes == null) continue;

                    for (int i = 0; i < wave.enemyTypes.Length; i++)
                    {
                        EnemyData type = wave.enemyTypes[i];
                        if (type == null || type.prefab == null) continue;
                        if (prefabs.Contains(type.prefab)) continue;

                        prefabs.Add(type.prefab);
                    }
                }

                if (prefabs.Count > 0)
                {
                    return prefabs.ToArray();
                }
            }

            return enemyPrefabs;
        }

        /// <summary>
        /// T031: тип врага для спавна — взвешенный случайный по EnemyData.waveWeight
        /// из состава волны (WaveData.enemyTypes). Нет данных — null (прежний путь по префабам).
        /// </summary>
        private EnemyData PickEnemyType()
        {
            EnemyData[] types = m_Wave != null ? m_Wave.enemyTypes : null;
            if (types == null || types.Length == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] != null) totalWeight += types[i].waveWeight;
            }

            if (totalWeight <= 0f) return types[0];

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == null) continue;

                cumulative += types[i].waveWeight;
                if (roll < cumulative) return types[i];
            }

            return types[types.Length - 1];
        }

        /// <summary>
        /// T026: применить базовые статы типа врага. База — данные, множители волны
        /// уже применены пулом при выдаче (SetBaseHealth сохраняет множитель HP).
        /// </summary>
        private void ApplyEnemyType(EnemyHealth health, EnemyData data)
        {
            health.SetBaseHealth(data.maxHealth);

            // T033: награда XP за убийство — из данных типа врага
            health.SetXpReward(data.xpReward);

            var movement = health.GetComponent<EnemyMovement>();
            if (movement != null)
            {
                movement.m_Speed = data.moveSpeed * currentEnemySpeedMultiplier;
            }
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

            OnEnemyDied?.Invoke(enemiesRemaining);

            if (enemiesRemaining <= 0 && enemiesToSpawn <= 0)
            {
                EndWave();
            }
        }

        /// <summary>
        /// Завершить текущую волну. T031: если по плану волн (WaveData.waveCount) есть следующая —
        /// запускается таймер паузы; если все волны пройдены — сигнал OnAllWavesCompleted.
        /// </summary>
        private void EndWave()
        {
            m_WaveActive = false;

            OnWaveCompleted?.Invoke(currentWave);

            if (m_Wave != null && currentWave < m_Wave.waveCount)
            {
                // Пауза перед следующей волной — из данных (было Invoke("StartNextWave", 3f))
                if (m_Wave.postWavePauseSeconds > 0f)
                {
                    m_NextWaveTimer = m_Wave.postWavePauseSeconds;
                }
                else
                {
                    StartWave(currentWave + 1);
                }

                return;
            }

            // Все волны раунда пройдены
            OnAllWavesCompleted?.Invoke();
        }

        /// <summary>
        /// Проверить, активна ли сейчас волна
        /// </summary>
        public bool IsWaveActive => m_WaveActive;

        /// <summary>
        /// Принудительно завершить волну (выход из раунда/отладка).
        /// T031: следующая волна НЕ планируется — раунд останавливает GameManager (ForceEndWave + ClearWaveEnemies).
        /// </summary>
        public void ForceEndWave()
        {
            m_WaveActive = false;
            m_NextWaveTimer = 0f;
            enemiesRemaining = 0;
            enemiesToSpawn = 0;

            OnWaveCompleted?.Invoke(currentWave);
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
        /// Очистить все инстансы врагов, созданные в текущей волне, отменить переход к следующей
        /// волне и сбросить счётчик волн (в меню раунд не идёт — было GameManager.m_CurrentWaveNumber, T085).
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

            // T031: пауза и переход к следующей волне отменяются (сцена не перезагружается)
            m_NextWaveTimer = 0f;
            m_WaveActive = false;

            currentWave = 0;
            enemiesRemaining = 0;
            enemiesToSpawn = 0;
        }
    }
}
