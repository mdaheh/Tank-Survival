using UnityEngine;

[CreateAssetMenu(fileName = "NewTurret", menuName = "Tank Survival/Turret")]
public class TurretData : ScriptableObject
{
    [Header("Identity")]
    public int id;                        // Уникальный ID для системы прогресса
    public string displayName;

    [Header("Prefab")]
    public GameObject prefab;
    public GameObject shellPrefab;

    [Header("Stats")]
    public float damage;
    public float fireRate;          // секунд между выстрелами
    public float fireRange;

    [Header("Unlock")]
    public int killsRequired;
    public Sprite unlockIcon;
}