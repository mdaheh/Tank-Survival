using UnityEngine;
using TankSurvival; // WaveData — план волн (T030)

[CreateAssetMenu(fileName = "NewDifficulty", menuName = "Tank Survival/Difficulty")]
public class DifficultyData : ScriptableObject
{
    [Header("Identity")]
    public int id;                    // 0=Easy, 1=Medium, 2=Hard
    public string displayName;

    [Header("Unlock")]
    public int killsRequiredToUnlock; // убийств для разблокировки

    // T030: разблокировка — из PlayerProgress (§2), поле isUnlocked удалено.
    // T030: числа волн (количество врагов, интервал, множители) — в WaveData, здесь только ссылка на план волн.
    [Header("Wave Plan")]
    [Tooltip("План волн этой сложности: число волн, состав по весам EnemyData.waveWeight и числа спавна живут в WaveData.")]
    public WaveData waveData;
}