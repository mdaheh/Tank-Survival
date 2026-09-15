using System;
using System.Collections.Generic;
using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Тип улучшения, применяемого к танку
    /// </summary>
    public enum UpgradeType
    {
        Damage,           // Увеличение урона снаряда
        FireRate,         // Уменьшение задержки между выстрелами
        MoveSpeed,        // Увеличение скорости движения
        TurnSpeed,        // Увеличение скорости поворота
        MaxHealth,        // Увеличение максимального здоровья
        ExplosionRadius,  // Увеличение радиуса взрыва
        ExplosionForce    // Увеличение силы взрыва
    }

    /// <summary>
    /// Данные об одном варианте улучшения (ScriptableObject)
    /// Создается через Asset > Tank Survival > Upgrade
    /// </summary>
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "Tank Survival/Upgrade")]
    public class UpgradeOptionData : ScriptableObject
    {
        public UpgradeType type;
        public string displayName;
        public string description;
        public Sprite icon;
        public float value;  // Абсолютное значение (для урона/здоровья) или процент (для скорости)

        /// <summary>
        /// Применить улучшение к танку игрока
        /// </summary>
        public void ApplyToPlayer()
        {
            // Ищем компоненты игрока напрямую (PlayerManager — не MonoBehaviour, поэтому FindObjectOfType не работает)
            var playerMovement = FindAnyObjectByType<PlayerMovement>();
            var shooting = FindAnyObjectByType<Shooting>();
            var tankHealth = playerMovement ? playerMovement.GetComponent<TankHealth>() : null;
            var shellExplosion = FindAnyObjectByType<ShellExplosion>();

            if (!playerMovement && !shooting)
            {
                Debug.LogWarning($"[UpgradeOptionData] Игровой танк не найден в сцене");
                return;
            }

            GameObject chassis = playerMovement ? playerMovement.gameObject : null;
            GameObject turret = shooting ? shooting.gameObject : null;

            switch (type)
            {
                case UpgradeType.Damage:
                    ApplyDamage(chassis, turret, shellExplosion, shooting);
                    break;
                case UpgradeType.FireRate:
                    ApplyFireRate(shooting);
                    break;
                case UpgradeType.MoveSpeed:
                    ApplyMoveSpeed(playerMovement);
                    break;
                case UpgradeType.TurnSpeed:
                    ApplyTurnSpeed(playerMovement);
                    break;
                case UpgradeType.MaxHealth:
                    ApplyMaxHealth(tankHealth);
                    break;
                case UpgradeType.ExplosionRadius:
                    ApplyExplosionRadius(shellExplosion);
                    break;
                case UpgradeType.ExplosionForce:
                    ApplyExplosionForce(shellExplosion);
                    break;
            }

            Debug.Log($"[UpgradeOptionData] Применено улучшение: {displayName}");
        }

        private void ApplyDamage(GameObject chassis, GameObject turret, ShellExplosion shellExplosion, Shooting shooting)
        {
            if (shellExplosion)
                shellExplosion.m_MaxDamage += value;

            if (shooting)
            {
                if (!shooting.TryGetComponent(out ShootingData shootingData))
                    shootingData = shooting.gameObject.AddComponent<ShootingData>();
                shootingData.damageBonus += value;
            }
        }

        private void ApplyFireRate(Shooting shooting)
        {
            if (shooting)
            {
                shooting.m_ShotCooldown = Mathf.Max(0.05f, shooting.m_ShotCooldown - value);
            }
        }

        private void ApplyMoveSpeed(PlayerMovement movement)
        {
            if (movement)
            {
                if (!movement.TryGetComponent(out MovementData movementData))
                    movementData = movement.gameObject.AddComponent<MovementData>();
                movementData.speedBonus += value;
            }
        }

        private void ApplyTurnSpeed(PlayerMovement movement)
        {
            if (movement)
            {
                if (!movement.TryGetComponent(out MovementData movementData))
                    movementData = movement.gameObject.AddComponent<MovementData>();
                movementData.turnSpeedBonus += value;
            }
        }

        private void ApplyMaxHealth(TankHealth health)
        {
            if (health)
                health.IncreaseMaxHealth(value);
        }

        private void ApplyExplosionRadius(ShellExplosion shellExplosion)
        {
            if (shellExplosion)
                shellExplosion.m_ExplosionRadius += value;
        }

        private void ApplyExplosionForce(ShellExplosion shellExplosion)
        {
            if (shellExplosion)
                shellExplosion.m_ExplosionForce += value;
        }
    }

    /// <summary>
    /// Компонент для хранения бонусов скорости движения (накапливается от улучшений)
    /// </summary>
    [DisallowMultipleComponent]
    public class MovementData : MonoBehaviour
    {
        public float speedBonus;
        public float turnSpeedBonus;
    }

    /// <summary>
    /// Компонент для хранения бонусов урона (накапливается от улучшений)
    /// </summary>
    [DisallowMultipleComponent]
    public class ShootingData : MonoBehaviour
    {
        public float damageBonus;
    }
}
