using System.Collections.Generic;
using UnityEngine;

namespace TankSurvival
{
    public class DataInitializer : MonoBehaviour
    {
        public List<ChassisData> chassisData;
        public List<TurretData> turretData;
        public List<DifficultyData> difficultyData;
        public List<UpgradeOptionData> upgradeData;
        public LevelData levelCurve;

        private void Start()
        {
            DataCatalog.Init(chassisData, turretData, difficultyData, upgradeData, levelCurve);
        }
    }
}