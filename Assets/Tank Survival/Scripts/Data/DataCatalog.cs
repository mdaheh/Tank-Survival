using System.Collections.Generic;
using UnityEngine;

namespace TankSurvival
{
    public static class DataCatalog
    {
        private static Dictionary<int, ChassisData> s_ChassisCache = new();
        private static Dictionary<int, TurretData> s_TurretCache = new();
        private static Dictionary<int, DifficultyData> s_DifficultyCache = new();
        private static List<UpgradeOptionData> s_UpgradePool = new();
        private static LevelData s_LevelCurve;

        public static void Init(
            List<ChassisData> chassis,
            List<TurretData> turrets,
            List<DifficultyData> difficulties,
            List<UpgradeOptionData> upgrades,
            LevelData levelCurve)
        {
            s_ChassisCache.Clear();
            foreach (var c in chassis) if (c != null) s_ChassisCache[c.id] = c;

            s_TurretCache.Clear();
            foreach (var t in turrets) if (t != null) s_TurretCache[t.id] = t;

            s_DifficultyCache.Clear();
            foreach (var d in difficulties) if (d != null) s_DifficultyCache[d.id] = d;

            s_UpgradePool.Clear();
            foreach (var u in upgrades) if (u != null) s_UpgradePool.Add(u);

            s_LevelCurve = levelCurve;
        }

        public static ChassisData GetChassis(int id) => s_ChassisCache.TryGetValue(id, out var d) ? d : null;
        public static TurretData GetTurret(int id) => s_TurretCache.TryGetValue(id, out var d) ? d : null;
        public static DifficultyData GetDifficulty(int id) => s_DifficultyCache.TryGetValue(id, out var d) ? d : null;
        public static List<ChassisData> GetAllChassis() { var l = new List<ChassisData>(s_ChassisCache.Values); l.Sort((a,b)=>a.id.CompareTo(b.id)); return l; }
        public static List<TurretData> GetAllTurrets() { var l = new List<TurretData>(s_TurretCache.Values); l.Sort((a,b)=>a.id.CompareTo(b.id)); return l; }
        public static List<DifficultyData> GetAllDifficulties() { var l = new List<DifficultyData>(s_DifficultyCache.Values); l.Sort((a,b)=>a.id.CompareTo(b.id)); return l; }
        public static List<UpgradeOptionData> GetAllUpgrades() => s_UpgradePool;
        public static LevelData GetLevelCurve() => s_LevelCurve;
    }
}