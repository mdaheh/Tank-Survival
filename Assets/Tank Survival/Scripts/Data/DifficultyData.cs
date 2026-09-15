using UnityEngine;

[CreateAssetMenu(fileName = "NewDifficulty", menuName = "Tank Survival/Difficulty")]
public class DifficultyData : ScriptableObject
{
    [Header("Identity")]
    public int id;                    // 0=Easy, 1=Medium, 2=Hard
    public string displayName;

    [Header("Unlock")]
    public int killsRequiredToUnlock; // убийств для разблокировки
    public bool isUnlocked;           // читается из PlayerProgress

    [Header("Wave Settings")]
    public int baseEnemyCount;
    public float spawnInterval;
    public float enemyHealthMultiplier;
    public float enemySpeedMultiplier;
}