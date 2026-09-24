using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Состояние забега — чистый C#-объект, не MonoBehaviour.
    /// Содержит уровень, XP, StatBlock и выбранные ID.
    /// Создаётся при StartRound, сбрасывается при FullReset.
    /// </summary>
    [System.Serializable]
    public class RunContext
    {
        public int level = 1;
        public int xp = 0;
        public StatBlock statBlock = new();

        public int selectedChassisId;
        public int selectedTurretId;
        public int selectedDifficultyIndex;

        /// <summary>
        /// Сбросить состояние к начальному (для нового раунда)
        /// </summary>
        public void Reset()
        {
            level = 1;
            xp = 0;
            statBlock.Reset();
        }
    }
}
