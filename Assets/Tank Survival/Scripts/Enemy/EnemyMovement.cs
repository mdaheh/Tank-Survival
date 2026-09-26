using UnityEngine;

namespace TankSurvival
{
    /// <summary>
    /// Простое преследование игрока с учётом коллизий.
    /// Враг идёт по прямой к игроку. При столкновении с препятствием
    /// Rigidbody автоматически скользит вдоль него — без дополнительного кода.
    /// 
    /// Враги сталкиваются друг с другом, создавая "пробки" в узких местах.
    /// Подходит для браузерных игр — минимальная нагрузка на CPU.
    /// </summary>
    public class EnemyMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float m_Speed = 12f;                 // Скорость движения
        public float m_TurnSpeed = 180f;            // Скорость поворота (град/сек)
        public float m_PathfindInterval = 0.5f;     // Как часто пересчитывать направление (сек)

        [Header("References")]
        public Transform m_Player;                  // Ссылка на игрока

        private Rigidbody m_Rigidbody;
        private float m_PathfindTimer;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            
            // Ключевая настройка для скольжения вдоль стен:
            // Interpolate — сглаживает физику между кадрами
            m_Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            
            // Continuous Dynamic — предотвращает прохождение сквозь объекты
            // при высокой скорости (важно для браузерной игры)
            m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        private void Update()
        {
            // Пересчитываем направление к игроку не каждый кадр, а раз в m_PathfindInterval
            m_PathfindTimer += Time.deltaTime;
            if (m_PathfindTimer >= m_PathfindInterval)
            {
                MoveTowardPlayer();
                m_PathfindTimer = 0f;
            }
        }

        /// <summary>
        /// Двигаться к игроку. При столкновении с коллайдером
        /// Rigidbody автоматически скользит вдоль преграды.
        /// Враги сталкиваются друг с другом, создавая "пробки".
        /// </summary>
        private void MoveTowardPlayer()
        {
            if (m_Player == null)
                return;

            // Вектор к игроку
            Vector3 direction = m_Player.position - transform.position;
            direction.y = 0; // Игнорируем высоту — враги не летают

            float distance = direction.magnitude;
            if (distance < 0.5f)
                return; // Уже слишком близко

            direction.Normalize();

            // Поворачиваемся к игроку
            TurnToward(direction);

            // Двигаемся вперёд.
            // При столкновении с коллайдером Rigidbody сам скользит вдоль стены
            // Благодаря настройкам физики (не нужно писать обходной код).
            // Враги сталкиваются друг с другом — создаются "пробки" в узких местах.
            m_Rigidbody.linearVelocity = transform.forward * m_Speed;
        }

        /// <summary>
        /// Повернуть к направлению direction
        /// </summary>
        private void TurnToward(Vector3 direction)
        {
            direction.y = 0;
            direction.Normalize();

            Vector3 forward = transform.forward;
            float angle = Vector3.SignedAngle(direction, forward, Vector3.up);

            float maxTurn = m_TurnSpeed * Time.deltaTime;
            angle = Mathf.Sign(angle) * Mathf.Min(Mathf.Abs(angle), maxTurn);

            if (Mathf.Abs(angle) > 0.001f)
            {
                Quaternion turnRotation = Quaternion.AngleAxis(-angle, Vector3.up);
                m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
            }
        }

        /// <summary>
        /// Обновить ссылку на игрока (вызывается при спавне врага)
        /// </summary>
        public void SetPlayer(Transform player)
        {
            m_Player = player;
        }
    }
}
