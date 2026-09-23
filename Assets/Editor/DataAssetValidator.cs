using UnityEngine;
using UnityEditor;
using System.IO;

namespace TankSurvival.Editor
{
    /// <summary>
    /// Editor-валидатор данных шасси/турелей.
    /// Проверяет, что поля prefab в ChassisData/TurretData ссылаются на сборные префабы,
    /// а не на сырые модели (.fbx) из папки Arts/.
    /// </summary>
    public class DataAssetValidator : EditorWindow
    {
        private static string s_LastReport;

        [MenuItem("Tank Survival/Validate Data Assets")]
        public static void Validate()
        {
            var errors = new System.Collections.Generic.List<string>();

            // Валидация ChassisData
            var chassisPaths = AssetDatabase.FindAssets("t:ChassisData");
            foreach (var guid in chassisPaths)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var chassis = AssetDatabase.LoadAssetAtPath<ChassisData>(path);
                if (chassis == null) continue;

                ValidateDataAsset(chassis, path, "Chassis", errors);
            }

            // Валидация TurretData
            var turretPaths = AssetDatabase.FindAssets("t:TurretData");
            foreach (var guid in turretPaths)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var turret = AssetDatabase.LoadAssetAtPath<TurretData>(path);
                if (turret == null) continue;

                ValidateDataAsset(turret, path, "Turret", errors);
            }

            s_LastReport = errors.Count == 0
                ? "OK: Все ассеты данных корректны."
                : $"Найдено ошибок: {errors.Count}\n" + string.Join("\n", errors);

            if (errors.Count == 0)
            {
                Debug.Log("[DataAssetValidator] " + s_LastReport);
                EditorUtility.DisplayDialog("Валидация данных", s_LastReport, "OK");
            }
            else
            {
                Debug.LogError("[DataAssetValidator] " + s_LastReport);
                EditorUtility.DisplayDialog("Валидация данных", s_LastReport, "OK");
            }
        }

        private static void ValidateDataAsset<T>(T asset, string path, string type, System.Collections.Generic.List<string> errors) where T : ScriptableObject
        {
            var prop = asset.GetType().GetProperty("prefab");
            if (prop == null) return;

            var prefab = prop.GetValue(asset) as GameObject;
            if (prefab == null)
            {
                errors.Add($"{type} {path}: поле prefab = null");
                return;
            }

            var prefabPath = AssetDatabase.GetAssetPath(prefab);

            // Проверка 1: не должна быть моделью
            if (prefabPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase) ||
                prefabPath.Contains("/Arts/") ||
                prefabPath.Contains("/arts/"))
            {
                errors.Add($"{type} {path}: prefab ссылается на модель {prefabPath} (ожидался .prefab)");
            }

            // Проверка 2: должен быть префабом
            if (!prefabPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{type} {path}: prefab не является .prefab файлом ({prefabPath})");
            }

            // Проверка 3: для шасси должен иметь TurretMountPoint
            if (type == "Chassis")
            {
                var mountPoint = prefab.GetComponentInChildren<TurretMountPoint>();
                if (mountPoint == null)
                {
                    errors.Add($"{type} {prefabPath}: префаб не имеет компонента TurretMountPoint");
                }
            }

            // Проверка 4: для турели должен иметь Shooting
            if (type == "Turret")
            {
                var shooting = prefab.GetComponent<Shooting>();
                if (shooting == null)
                {
                    errors.Add($"{type} {prefabPath}: префаб не имеет компонента Shooting");
                }
            }
        }
    }
}
