using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// ИИ врага — минимальный.
    /// Просто передаёт ссылку на игрока EnemyMovement,
    /// который сам идёт к нему с учётом коллизий.
    /// 
    /// Без NavMesh — работает даже на слабых устройствах.
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        private EnemyMovement m_Movement;

        private void Awake()
        {
            if (!isActiveAndEnabled)
                return;

            m_Movement = GetComponent<EnemyMovement>();
        }

        /// <summary>
        /// Установить ссылку на игрока (вызывается WaveManager при спавне)
        /// </summary>
        public void SetPlayer(Transform player)
        {
            if (m_Movement != null)
                m_Movement.SetPlayer(player);
        }

        /// <summary>
        /// Отключить AI (для отладки)
        /// </summary>
        public void TurnOff()
        {
            enabled = false;
        }
    }
}
