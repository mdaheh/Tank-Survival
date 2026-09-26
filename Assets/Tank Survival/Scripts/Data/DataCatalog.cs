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

        // === КЭШ СПИСКОВ (T027) ===
        // Строятся и сортируются ОДИН раз в Init; GetAll* возвращает их как есть.
        // Ноль аллокаций на вызов (GetAll* вызывается в горячем пути, в т.ч. на убийство врага).
        // Внимание: списки общие — мутировать их нельзя (только чтение/поиск).
        private static readonly List<ChassisData> s_ChassisList = new();
        private static readonly List<TurretData> s_TurretList = new();
        private static readonly List<DifficultyData> s_DifficultyList = new();

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

            // T027: списки для GetAll* строим и сортируем один раз здесь —
            // дальше они отдаются без создания новых коллекций
            s_ChassisList.Clear();
            s_ChassisList.AddRange(s_ChassisCache.Values);
            s_ChassisList.Sort((a, b) => a.id.CompareTo(b.id));

            s_TurretList.Clear();
            s_TurretList.AddRange(s_TurretCache.Values);
            s_TurretList.Sort((a, b) => a.id.CompareTo(b.id));

            s_DifficultyList.Clear();
            s_DifficultyList.AddRange(s_DifficultyCache.Values);
            s_DifficultyList.Sort((a, b) => a.id.CompareTo(b.id));

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

        /// <summary>
        /// T027: кэшированный список (построен и отсортирован один раз в Init).
        /// Возвращается без аллокаций; список только для чтения/поиска.
        /// </summary>
        public static List<ChassisData> GetAllChassis()
        {
            CheckInitialized();
            return s_ChassisList;
        }

        /// <summary>
        /// T027: кэшированный список (построен и отсортирован один раз в Init).
        /// </summary>
        public static List<TurretData> GetAllTurrets()
        {
            CheckInitialized();
            return s_TurretList;
        }

        /// <summary>
        /// T027: кэшированный список (построен и отсортирован один раз в Init).
        /// </summary>
        public static List<DifficultyData> GetAllDifficulties()
        {
            CheckInitialized();
            return s_DifficultyList;
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