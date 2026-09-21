using System.Collections.Generic;
using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Назначает данные в Inspector и инициализирует DataCatalog.
    /// </summary>
    public class DataInitializer : MonoBehaviour
    {
        public List<ChassisData> chassisData;
        public List<TurretData> turretData;
        public List<DifficultyData> difficultyData;
        public List<UpgradeOptionData> upgradeData;
        public LevelData levelCurve;

        private void Awake()
        {
            // Проверяем — данные назначены в Inspector?
            if (chassisData == null || chassisData.Count == 0)
            {
                Debug.LogError("[DataInitializer] Данные не назначены в Inspector!");
                return;
            }

            // Инициализируем каталог
            DataCatalog.Init(chassisData, turretData, difficultyData, upgradeData, levelCurve);
        }
    }
}