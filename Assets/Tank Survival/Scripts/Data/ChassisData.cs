using UnityEngine;

[CreateAssetMenu(fileName = "NewChassis", menuName = "Tank Survival/Chassis")]
public class ChassisData : ScriptableObject
{
    [Header("Identity")]
    public int id;                        // Уникальный ID для системы прогресса
    public string displayName;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Stats")]
    public float maxHealth;
    public float moveSpeed;
    public float turnSpeed;

    [Header("Unlock")]
    public int killsRequired;             // 0 = доступен с начала
    public Sprite unlockIcon;
}
