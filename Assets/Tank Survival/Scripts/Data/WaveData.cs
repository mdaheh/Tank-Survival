using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Данные волн (T029): все числа волн — данные, а не код.
    /// Один ассет описывает набор волн одной сложности; ссылку на него добавляет
    /// DifficultyData (T030), спавн читает WaveController (T031).
    /// Перенос «магических чисел»: WaveManager.StartWave (+5 врагов за волну,
    /// +0.2 к множителю HP, +0.1 к множителю скорости, −0.05 с к интервалу, минимум 0.3 с)
    /// и GameManager (TOTAL_WAVES = 3, пауза после волны 3 с).
    /// </summary>
    [CreateAssetMenu(fileName = "NewWave", menuName = "Tank Survival/Wave")]
    public class WaveData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName;

        [Header("Waves")]
        [Tooltip("Число волн в раунде (было GameManager.TOTAL_WAVES = 3).")]
        [Min(1)] public int waveCount = 3;

        [Tooltip("Пауза после завершённой волны, с (была Invoke(\"StartNextWave\", 3f) в GameManager).")]
        [Min(0f)] public float postWavePauseSeconds = 3f;

        [Header("Enemy Count")]
        [Tooltip("Врагов в первой волне (было DifficultyData.baseEnemyCount).")]
        [Min(0)] public int baseEnemyCount = 10;

        [Tooltip("Прибавка врагов за каждую следующую волну (было (waveNumber - 1) * 5).")]
        [Min(0)] public int enemiesPerWaveIncrease = 5;

        [Header("Spawn Interval")]
        [Tooltip("Интервал спавна первой волны, с (было DifficultyData.spawnInterval).")]
        [Min(0.01f)] public float spawnInterval = 2f;

        [Tooltip("Уменьшение интервала за каждую следующую волну, с (было (waveNumber - 1) * 0.05f).")]
        [Min(0f)] public float spawnIntervalDecreasePerWave = 0.05f;

        [Tooltip("Нижняя граница интервала спавна, с (было зажатие 0.3f).")]
        [Min(0.01f)] public float minSpawnInterval = 0.3f;

        [Header("Wave Multipliers")]
        [Tooltip("Множитель HP врагов первой волны (было DifficultyData.enemyHealthMultiplier).")]
        [Min(0f)] public float enemyHealthMultiplier = 1f;

        [Tooltip("Прибавка к множителю HP за каждую следующую волну (было (waveNumber - 1) * 0.2f).")]
        [Min(0f)] public float healthMultiplierIncreasePerWave = 0.2f;

        [Tooltip("Множитель скорости врагов первой волны (было DifficultyData.enemySpeedMultiplier).")]
        [Min(0f)] public float enemySpeedMultiplier = 1f;

        [Tooltip("Прибавка к множителю скорости за каждую следующую волну (было (waveNumber - 1) * 0.1f).")]
        [Min(0f)] public float speedMultiplierIncreasePerWave = 0.1f;

        [Header("Composition")]
        [Tooltip("Типы врагов волны. Доля типа — его EnemyData.waveWeight (веса читает спавн, T031).")]
        public EnemyData[] enemyTypes;
    }
}
