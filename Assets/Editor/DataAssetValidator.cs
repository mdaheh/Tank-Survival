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
            s_LastReport = BuildReport();

            if (s_LastReport.StartsWith("OK"))
            {
                Debug.Log("[DataAssetValidator] " + s_LastReport);
            }
            else
            {
                Debug.LogError("[DataAssetValidator] " + s_LastReport);
            }

            EditorUtility.DisplayDialog("Валидация данных", s_LastReport, "OK");
        }

        /// <summary>
        /// T078: сбор отчёта без побочных эффектов (без лога и модального диалога).
        /// Вызывается пунктом меню; может вызываться и кодом (в т.ч. из проверок через MCP),
        /// чтобы модальное окно не блокировало редактор.
        /// </summary>
        public static string BuildReport()
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

            return errors.Count == 0
                ? "OK: Все ассеты данных корректны."
                : $"Найдено ошибок: {errors.Count}\n" + string.Join("\n", errors);
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

            // Проверка 3: шасси — состав компонентов задаётся префабом (T006–T008, T077)
            if (type == "Chassis")
            {
                if (prefab.GetComponentInChildren<TurretMountPoint>() == null)
                {
                    errors.Add($"{type} {prefabPath}: префаб не имеет компонента TurretMountPoint");
                }

                if (prefab.GetComponent<PlayerMovement>() == null)
                {
                    errors.Add($"{type} {prefabPath}: префаб не имеет компонента PlayerMovement");
                }

                if (prefab.GetComponent<TankHealth>() == null)
                {
                    errors.Add($"{type} {prefabPath}: префаб не имеет компонента TankHealth");
                }

                var inputUser = prefab.GetComponent<TankInputUser>();
                if (inputUser == null)
                {
                    errors.Add($"{type} {prefabPath}: префаб не имеет компонента TankInputUser");
                }
                else if (inputUser.ActionAsset == null)
                {
                    errors.Add($"{type} {prefabPath}: у TankInputUser не назначен ActionAsset");
                }
            }

            // Проверка 4: турель — Shooting, его ссылки и данные радиуса (T024d/T025a/T025b)
            if (type == "Turret")
            {
                var shooting = prefab.GetComponent<Shooting>();
                if (shooting == null)
                {
                    errors.Add($"{type} {prefabPath}: префаб не имеет компонента Shooting");
                }
                else
                {
                    if (shooting.m_Shell == null)
                    {
                        errors.Add($"{type} {prefabPath}: у Shooting не назначен m_Shell");
                    }

                    if (shooting.m_FireTransform == null)
                    {
                        errors.Add($"{type} {prefabPath}: у Shooting не назначен m_FireTransform");
                    }

                    if (shooting.enemyMask.value == 0)
                    {
                        errors.Add($"{type} {prefabPath}: у Shooting пустой enemyMask");
                    }
                }

                if (asset is TurretData turretData)
                {
                    if (turretData.baseExplosionRadius <= 0f)
                    {
                        errors.Add($"{type} {path}: baseExplosionRadius = {turretData.baseExplosionRadius} (должно быть > 0 — T025b)");
                    }

                    ValidateShellPrefab(turretData.shellPrefab, path, errors);
                }
            }
        }

        /// <summary>
        /// T078: проверки префаба снаряда под контракт T024b/T024c/T024d.
        /// </summary>
        private static void ValidateShellPrefab(GameObject shell, string assetPath, System.Collections.Generic.List<string> errors)
        {
            if (shell == null)
            {
                errors.Add($"Turret {assetPath}: поле shellPrefab = null");
                return;
            }

            var shellPath = AssetDatabase.GetAssetPath(shell);

            if (shell.GetComponent<Projectile>() == null)
            {
                errors.Add($"Turret {assetPath} → shell {shellPath}: префаб снаряда не имеет компонента Projectile");
            }

            var body = shell.GetComponent<Rigidbody>();
            if (body == null)
            {
                errors.Add($"Turret {assetPath} → shell {shellPath}: у префаба снаряда нет Rigidbody");
            }
            else if (body.useGravity)
            {
                errors.Add($"Turret {assetPath} → shell {shellPath}: useGravity = true (для снаряда должно быть false — T024b)");
            }

            var lights = shell.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                errors.Add($"Turret {assetPath} → shell {shellPath}: лишний Light на объекте '{lights[i].gameObject.name}' (удалён в T024b)");
            }
        }
    }
}
