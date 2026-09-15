using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Структура разблокированной части (шасси или башни)
    /// </summary>
    [Serializable]
    public class UnlockedPart
    {
        public int partId;                    // ID части (соответствует ChassisData/TurretData.id)
        public int unlockedAtKillCount;       // Количество убийств, при котором часть открылась
    }

    /// <summary>
    /// Полный прогресс игрока — сохраняется между сессиями
    /// </summary>
    [Serializable]
    public class PlayerProgress
    {
        // --- Базовая статистика ---
        public int totalKills;                        // Общее количество убийств за всё время
        public int totalGamesPlayed;                  // Общее количество сыгранных раундов
        public int highestDifficultyReached;          // 0=Easy, 1=Medium, 2=Hard

        // --- Разблокированные части ---
        public List<UnlockedPart> unlockedChassis = new();
        public List<UnlockedPart> unlockedTurrets = new();

        // --- Разблокированные сложности ---
        public bool isEasyUnlocked = true;            // Доступна с начала
        public bool isMediumUnlocked;                 // Разблокируется при 10 убийствах
        public bool isHardUnlocked;                   // Разблокируется при 50 убийствах

        // --- Текущая сессия (не сохраняется, используется во время игры) ---
        public int currentSessionKills;               // Убийства в текущем раунде
        public int currentLevel;                      // Текущий уровень в раунде
        public int currentXp;                         // Текущий XP в раунде

        /// <summary>
        /// Проверить, разблокировано ли шасси с данным ID
        /// </summary>
        public bool IsChassisUnlocked(int id)
        {
            return unlockedChassis.Any(p => p.partId == id);
        }

        /// <summary>
        /// Проверить, разблокирована ли башня с данным ID
        /// </summary>
        public bool IsTurretUnlocked(int id)
        {
            return unlockedTurrets.Any(p => p.partId == id);
        }

        /// <summary>
        /// Добавить разблокированное шасси
        /// </summary>
        public void AddUnlockedChassis(int chassisId, int killCount)
        {
            if (!IsChassisUnlocked(chassisId))
            {
                unlockedChassis.Add(new UnlockedPart
                {
                    partId = chassisId,
                    unlockedAtKillCount = killCount
                });
            }
        }

        /// <summary>
        /// Добавить разблокированную башню
        /// </summary>
        public void AddUnlockedTurret(int turretId, int killCount)
        {
            if (!IsTurretUnlocked(turretId))
            {
                unlockedTurrets.Add(new UnlockedPart
                {
                    partId = turretId,
                    unlockedAtKillCount = killCount
                });
            }
        }

        /// <summary>
        /// Проверить, доступна ли сложность по индексу
        /// </summary>
        public bool IsDifficultyUnlocked(int difficultyIndex)
        {
            switch (difficultyIndex)
            {
                case 0: return isEasyUnlocked;
                case 1: return isMediumUnlocked;
                case 2: return isHardUnlocked;
                default: return false;
            }
        }

        /// <summary>
        /// Обновить разблокировки сложностей на основе totalKills
        /// Вызывать после увеличения totalKills
        /// </summary>
        public void UpdateDifficultyUnlocks(int totalKills)
        {
            if (totalKills >= 10 && !isMediumUnlocked)
                isMediumUnlocked = true;

            if (totalKills >= 50 && !isHardUnlocked)
                isHardUnlocked = true;
        }

        /// <summary>
        /// Обновить разблокировки частей на основе totalKills
        /// Вызывать после увеличения totalKills
        /// </summary>
        public void UpdatePartUnlocks(List<ChassisData> allChassis, List<TurretData> allTurrets, int totalKills)
        {
            foreach (var chassis in allChassis)
            {
                if (chassis.killsRequired > 0 && totalKills >= chassis.killsRequired)
                    AddUnlockedChassis(chassis.id, totalKills);
            }

            foreach (var turret in allTurrets)
            {
                if (turret.killsRequired > 0 && totalKills >= turret.killsRequired)
                    AddUnlockedTurret(turret.id, totalKills);
            }
        }

        /// <summary>
        /// Сбросить текущую сессию (в начало нового раунда)
        /// </summary>
        public void ResetSession()
        {
            currentSessionKills = 0;
            currentLevel = 1;
            currentXp = 0;
        }
    }
}
