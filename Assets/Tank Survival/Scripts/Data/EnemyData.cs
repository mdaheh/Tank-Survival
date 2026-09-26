using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Данные типа врага (T026): HP, скорость, XP-ценность, вес в составе волны, префаб.
    /// Источник базовых статов при спавне — вместо хардкода в коде/префабе.
    /// База для 5 типов врагов из ГД.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Tank Survival/Enemy")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string displayName;

        [Header("Prefab")]
        public GameObject prefab;

        [Header("Stats")]
        [Min(1f)] public float maxHealth = 100f;
        [Min(0f)] public float moveSpeed = 12f;
        [Min(0f)] public float xpReward = 10f;

        [Tooltip("Вес в составе волны. Использование спавном — Ф2/T031")]
        [Min(0f)] public float waveWeight = 1f;
    }
}
