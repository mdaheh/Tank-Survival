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
        /// T019: применить улучшение к статам забега (StatBlock) и пересчитать компоненты танка.
        /// </summary>
        public void ApplyToPlayer()
        {
            var stats = PlayerManager.Instance != null ? PlayerManager.Instance.Stats : null;

            if (stats == null)
            {
                Debug.LogWarning($"[UpgradeOptionData] StatBlock забега недоступен — улучшение '{displayName}' не применено");
                return;
            }

            switch (type)
            {
                case UpgradeType.Damage:
                    // value — абсолютный урон
                    stats.AddDamage(value);
                    break;
                case UpgradeType.FireRate:
                    // value — секунды, уменьшают задержку между выстрелами
                    stats.AddFireRate(value);
                    break;
                case UpgradeType.MoveSpeed:
                    // value — проценты от базовой скорости (см. описание ассета)
                    stats.AddMoveSpeed(stats.baseMoveSpeed * value / 100f);
                    break;
                case UpgradeType.TurnSpeed:
                    // value — проценты от базовой скорости поворота
                    stats.AddTurnSpeed(stats.baseTurnSpeed * value / 100f);
                    break;
                case UpgradeType.MaxHealth:
                    stats.AddMaxHealth(value);
                    break;
                case UpgradeType.ExplosionRadius:
                    ApplyExplosionRadius();
                    break;
                case UpgradeType.ExplosionForce:
                    ApplyExplosionForce();
                    break;
            }

            // Пересчитываем итоговые значения на живом танке (база из данных + модификаторы)
            PlayerManager.Instance.RefreshStats();
        }

        private void ApplyExplosionRadius()
        {
            // T024: Projectile вместо ShellExplosion
            var projectile = FindAnyObjectByType<Projectile>();
            if (projectile)
                projectile.m_ExplosionRadius += value;
        }

        private void ApplyExplosionForce()
        {
            // T024: Projectile вместо ShellExplosion
            var projectile = FindAnyObjectByType<Projectile>();
            if (projectile)
                projectile.m_ExplosionForce += value;
        }
    }

}
