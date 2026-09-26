using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Аккумулятор модификаторов статов танка.
    /// Единственное место, куда пишутся улучшения; компоненты читают итоговые значения отсюда.
    /// Чистый C#-класс, без UnityEngine-зависимостей кроме [Serializable].
    /// </summary>
    [System.Serializable]
    public class StatBlock
    {
        // === Базовые значения ===
        [Header("Базовые значения")]
        public float baseDamage = 25f;
        public float baseFireRate = 0.8f;
        public float baseMoveSpeed = 5f;
        public float baseTurnSpeed = 180f;
        public float baseMaxHealth = 100f;
        public float baseXpReward = 10f;

        // === Модификаторы (накапливаются от улучшений) ===
        [Header("Модификаторы")]
        public float damageBonus;
        public float fireRateBonus;      // уменьшает cooldown
        public float moveSpeedBonus;
        public float turnSpeedBonus;
        public float maxHealthBonus;
        public float xpBonus;
        public float explosionRadiusBonus;   // T025a: прибавка к радиусу взрыва снаряда

        /// <summary>
        /// Итоговый урон
        /// </summary>
        public float GetDamage() => baseDamage + damageBonus;

        /// <summary>
        /// Итоговый cooldown выстрела (не меньше 0.05)
        /// </summary>
        public float GetFireRate() => Mathf.Max(0.05f, baseFireRate - fireRateBonus);

        /// <summary>
        /// Итоговая скорость движения
        /// </summary>
        public float GetMoveSpeed() => baseMoveSpeed + moveSpeedBonus;

        /// <summary>
        /// Итоговая скорость поворота
        /// </summary>
        public float GetTurnSpeed() => baseTurnSpeed + turnSpeedBonus;

        /// <summary>
        /// Итоговое максимальное здоровье
        /// </summary>
        public float GetMaxHealth() => baseMaxHealth + maxHealthBonus;

        /// <summary>
        /// Итоговая ценность XP за убийство
        /// </summary>
        public float GetXpReward() => baseXpReward + xpBonus;

        /// <summary>
        /// T025a: прибавка к радиусу взрыва снаряда.
        /// База радиуса живёт в префабе снаряда, здесь хранится только модификатор.
        /// </summary>
        public float GetExplosionRadiusBonus() => explosionRadiusBonus;

        /// <summary>
        /// Сбросить все модификаторы к нулю
        /// </summary>
        public void Reset()
        {
            damageBonus = 0f;
            fireRateBonus = 0f;
            moveSpeedBonus = 0f;
            turnSpeedBonus = 0f;
            maxHealthBonus = 0f;
            xpBonus = 0f;
            explosionRadiusBonus = 0f;   // T025a
        }

        // === Add-методы для улучшений ===

        public void AddDamage(float amount) => damageBonus += amount;
        public void AddFireRate(float amount) => fireRateBonus += amount;
        public void AddMoveSpeed(float amount) => moveSpeedBonus += amount;
        public void AddTurnSpeed(float amount) => turnSpeedBonus += amount;
        public void AddMaxHealth(float amount) => maxHealthBonus += amount;
        public void AddXp(float amount) => xpBonus += amount;

        /// <summary>
        /// T025a: увеличить радиус взрыва снаряда
        /// </summary>
        public void AddExplosionRadius(float amount) => explosionRadiusBonus += amount;
    }
}
