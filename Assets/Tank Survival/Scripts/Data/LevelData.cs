using System.Collections.Generic;
using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Данные кривой опыта — сколько XP нужно для каждого уровня
    /// levelXpCurve[i] = XP, необходимый для достижения уровня i+1
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCurve", menuName = "Tank Survival/Level Curve")]
    public class LevelData : ScriptableObject
    {
        [Tooltip("XP, необходимый для каждого уровня. levelXpCurve[0] = XP для 1-го уровня")]
        public int[] levelXpCurve = new int[]
        {
            50,   // Уровень 1: нужно 50 XP
            100,  // Уровень 2: нужно ещё 100 XP
            150,  // Уровень 3: нужно ещё 150 XP
            200,  // Уровень 4: нужно ещё 200 XP
            250,  // Уровень 5: нужно ещё 250 XP
            300,  // Уровень 6: нужно ещё 300 XP
            350,  // Уровень 7: нужно ещё 350 XP
            400,  // Уровень 8: нужно ещё 400 XP
            450,  // Уровень 9: нужно ещё 450 XP
            500   // Уровень 10: нужно ещё 500 XP
        };

        /// <summary>
        /// Получить количество XP, необходимое для достижения данного уровня
        /// </summary>
        public int GetXpRequiredForLevel(int level)
        {
            if (level < 1) return 0;
            if (level > levelXpCurve.Length) return levelXpCurve[levelXpCurve.Length - 1];
            return levelXpCurve[level - 1];
        }

        /// <summary>
        /// Получить максимальный уровень из кривой
        /// </summary>
        public int MaxLevel => levelXpCurve.Length;

        /// <summary>
        /// Проверить, существует ли данный уровень
        /// </summary>
        public bool IsValidLevel(int level) => level >= 1 && level <= levelXpCurve.Length;
    }
}
