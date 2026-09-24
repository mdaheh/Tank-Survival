using System.Collections.Generic;
using UnityEngine;

namespace TankSurvival
{
    public static class DataCatalog
    {
        // === ФЛАГ ИНИЦИАЛИЗАЦИИ ===
        // true = данные загружены, false = нужно вызвать Init()
        private static bool s_Initialized;

        // === КАШ ДАННЫХ ===
        private static Dictionary<int, ChassisData> s_ChassisCache = new();
        private static Dictionary<int, TurretData> s_TurretCache = new();
        private static Dictionary<int, DifficultyData> s_DifficultyCache = new();
        private static List<UpgradeOptionData> s_UpgradePool = new();
        private static LevelData s_LevelCurve;

        // === МЕТОД ИНИЦИАЛИЗАЦИИ ===
        public static void Init(
            List<ChassisData> chassis,
            List<TurretData> turrets,
            List<DifficultyData> difficulties,
            List<UpgradeOptionData> upgrades,
            LevelData levelCurve)
        {
            // Если уже инициализирован — не повторяем (защита от двойной инициализации)
            if (s_Initialized)
            {
                return;
            }

            // Очищаем кэш перед заполнением
            s_ChassisCache.Clear();
            foreach (var c in chassis)
                if (c != null)
                    s_ChassisCache[c.id] = c;

            s_TurretCache.Clear();
            foreach (var t in turrets)
                if (t != null)
                    s_TurretCache[t.id] = t;

            s_DifficultyCache.Clear();
            foreach (var d in difficulties)
                if (d != null)
                    s_DifficultyCache[d.id] = d;

            s_UpgradePool.Clear();
            foreach (var u in upgrades)
                if (u != null)
                    s_UpgradePool.Add(u);

            s_LevelCurve = levelCurve;

            // Ставим флаг — инициализирован
            s_Initialized = true;
        }

        // === МЕТОД ПРОВЕРКИ ===
        // Вызывается перед каждым Get...() — если не инициализирован, выводит ошибку
        private static void CheckInitialized()
        {
            if (!s_Initialized)
            {
                Debug.LogError("[DataCatalog] Не инициализирован! Вызови DataCatalog.Init() перед использованием.");
            }
        }

        // === ПОЛУЧЕНИЕ ДАННЫХ ПО ID ===

        public static ChassisData GetChassis(int id)
        {
            CheckInitialized();  // ← Проверяем, что данные загружены
            return s_ChassisCache.TryGetValue(id, out var d) ? d : null;
        }

        public static TurretData GetTurret(int id)
        {
            CheckInitialized();
            return s_TurretCache.TryGetValue(id, out var d) ? d : null;
        }

        public static DifficultyData GetDifficulty(int id)
        {
            CheckInitialized();
            return s_DifficultyCache.TryGetValue(id, out var d) ? d : null;
        }

        // === ПОЛУЧЕНИЕ ВСЕХ ДАННЫХ ===

        public static List<ChassisData> GetAllChassis()
        {
            CheckInitialized();
            var l = new List<ChassisData>(s_ChassisCache.Values);
            l.Sort((a, b) => a.id.CompareTo(b.id));
            return l;
        }

        public static List<TurretData> GetAllTurrets()
        {
            CheckInitialized();
            var l = new List<TurretData>(s_TurretCache.Values);
            l.Sort((a, b) => a.id.CompareTo(b.id));
            return l;
        }

        public static List<DifficultyData> GetAllDifficulties()
        {
            CheckInitialized();
            var l = new List<DifficultyData>(s_DifficultyCache.Values);
            l.Sort((a, b) => a.id.CompareTo(b.id));
            return l;
        }

        public static List<UpgradeOptionData> GetAllUpgrades()
        {
            CheckInitialized();
            return s_UpgradePool;
        }

        public static LevelData GetLevelCurve()
        {
            CheckInitialized();
            return s_LevelCurve;
        }
    }
}