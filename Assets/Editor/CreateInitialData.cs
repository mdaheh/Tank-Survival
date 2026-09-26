using UnityEngine;
using UnityEditor;

namespace TankSurvival.Editor
{
    public class CreateInitialData : MonoBehaviour
    {
        [MenuItem("Assets/Tank Survival/Create Initial Data")]
        public static void CreateAll()
        {
            CreateChassisData();
            CreateTurretData();
            CreateDifficultyData();
            CreateUpgradeData();
            CreateLevelCurve();
            
            Debug.Log("[CreateInitialData] Все начальные данные созданы!");
        }

        private static void CreateChassisData()
        {
            string[] names = { "Standart", "Fancy", "Fat", "Veteran" };
            int[] ids = { 101, 102, 103, 104 };
            int[] killsRequired = { 0, 5, 15, 30 };
            float[] health = { 100f, 120f, 150f, 90f };
            float[] speed = { 12f, 10f, 8f, 15f };
            float[] turnSpeed = { 180f, 160f, 140f, 200f };

            for (int i = 0; i < names.Length; i++)
            {
                string path = $"Assets/Tank Survival/ScriptableObjects/Chassis/Chassis_{names[i]}.asset";
                ChassisData data = ScriptableObject.CreateInstance<ChassisData>();
                data.id = ids[i];
                data.displayName = $"Шасси {names[i]}";
                data.killsRequired = killsRequired[i];
                data.maxHealth = health[i];
                data.moveSpeed = speed[i];
                data.turnSpeed = turnSpeed[i];
                
                AssetDatabase.CreateAsset(data, path);
                Debug.Log($"Создан: {path} (id={data.id})");
            }
        }

        private static void CreateTurretData()
        {
            string[] names = { "Standart", "Long", "Twins", "Mandibles" };
            int[] ids = { 201, 202, 203, 204 };
            int[] killsRequired = { 0, 10, 20, 40 };
            float[] damage = { 25f, 40f, 15f, 60f };
            float[] fireRate = { 0.8f, 1.2f, 0.4f, 2.0f };
            float[] range = { 15f, 25f, 12f, 10f };

            for (int i = 0; i < names.Length; i++)
            {
                string path = $"Assets/Tank Survival/ScriptableObjects/Turrets/Turret_{names[i]}.asset";
                TurretData data = ScriptableObject.CreateInstance<TurretData>();
                data.id = ids[i];
                data.displayName = $"Башня {names[i]}";
                data.killsRequired = killsRequired[i];
                data.damage = damage[i];
                data.fireRate = fireRate[i];
                data.fireRange = range[i];
                
                AssetDatabase.CreateAsset(data, path);
                Debug.Log($"Создан: {path} (id={data.id})");
            }
        }

        private static void CreateDifficultyData()
        {
            string[] names = { "Easy", "Medium", "Hard" };
            int[] ids = { 0, 1, 2 };

            for (int i = 0; i < names.Length; i++)
            {
                string path = $"Assets/Tank Survival/ScriptableObjects/Difficulties/Difficulty_{names[i]}.asset";
                DifficultyData data = ScriptableObject.CreateInstance<DifficultyData>();
                data.id = ids[i];
                data.displayName = names[i];
                // T030: числа волн живут в плане волн (WaveData) — сложность только ссылается на него
                data.waveData = AssetDatabase.LoadAssetAtPath<WaveData>($"Assets/Tank Survival/ScriptableObjects/Waves/Wave_{names[i]}.asset");

                AssetDatabase.CreateAsset(data, path);
                Debug.Log($"Создан: {path} (id={data.id})");
            }
        }

        private static void CreateUpgradeData()
        {
            string[] names = { "Damage", "FireRate", "MoveSpeed", "TurnSpeed", "MaxHealth" };
            UpgradeType[] types = { 
                UpgradeType.Damage, UpgradeType.FireRate, 
                UpgradeType.MoveSpeed, UpgradeType.TurnSpeed, 
                UpgradeType.MaxHealth 
            };
            float[] values = { 10f, 0.1f, 2f, 10f, 20f };
            string[] descs = { "+10 урона", "-0.1с перезарядка", "+2% скорость", "+10% поворот", "+20 макс. HP" };

            for (int i = 0; i < names.Length; i++)
            {
                string path = $"Assets/Tank Survival/ScriptableObjects/Upgrades/Upgrade_{names[i]}.asset";
                UpgradeOptionData data = ScriptableObject.CreateInstance<UpgradeOptionData>();
                data.type = types[i];
                data.displayName = names[i];
                data.description = descs[i];
                data.value = values[i];
                
                AssetDatabase.CreateAsset(data, path);
                Debug.Log($"Создан: {path}");
            }
        }

        private static void CreateLevelCurve()
        {
            int[] xpPerLevel = { 50, 100, 150, 200, 250, 300, 350, 400, 450, 500 };
            
            string path = "Assets/Tank Survival/ScriptableObjects/LevelCurve.asset";
            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.levelXpCurve = xpPerLevel;
            
            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"Создан: {path}");
        }
    }
}
