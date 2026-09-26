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
    public float baseExplosionRadius = 1f;  // T025b: база радиуса взрыва снаряда (в Ф2 переезжает в WeaponBehaviorSO вместе с типами атак)

    [Header("Unlock")]
    public int killsRequired;
    public Sprite unlockIcon;
}