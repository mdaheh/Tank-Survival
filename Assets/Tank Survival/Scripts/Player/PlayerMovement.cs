using UnityEngine;
using UnityEngine.InputSystem;

namespace TankSurvival
{
    /// <summary>
    /// Движение игрока — использует новый Input System.
    /// Управление: WASD (W=вперёд, S=назад, A=поворот влево, D=поворот вправо).
    /// Поддерживает бонусы от улучшений через компонент MovementData.
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        [Tooltip("The speed in unity unit/second the tank move at")]
        public float m_Speed = 12f;                 // How fast the tank moves forward and back.
        [Tooltip("The speed in deg/s that tank will rotate at")]
        public float m_TurnSpeed = 180f;            // How fast the tank turns in degrees per second.
        [Tooltip("If set to true, the tank auto orient and move toward the pressed direction instead of rotating on left/right and move forward on up")]
        public bool m_IsDirectControl;
        public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
        public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
        public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
        public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.

        public Rigidbody Rigidbody => m_Rigidbody;

        private Rigidbody m_Rigidbody;              // Reference used to move the tank.
        private InputAction m_MoveAction;           // Action from the new Input System
        private Vector2 m_MovementInput;            // Текущее значение ввода (x=поворот, y=движение)
        private Vector3 m_ExplosionForceValue;      // Текущая сила от взрыва
        private float m_OriginalPitch;              // Pitch аудио источника в начале сцены
        private ParticleSystem[] m_particleSystems; // Ссылки на все particle системы танка

        private Vector3 m_RequestedDirection;       // В режиме Direct Control — направление движения

        // Компонент бонусов от улучшений
        private MovementData m_MovementData;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_MovementData = GetComponent<MovementData>();
        }

        private void OnEnable()
        {
            m_Rigidbody.isKinematic = false;
            m_MovementInput = Vector2.zero;
            m_ExplosionForceValue = Vector3.zero;

            m_particleSystems = GetComponentsInChildren<ParticleSystem>();
            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Play();
            }
        }

        private void OnDisable()
        {
            m_Rigidbody.isKinematic = true;

            for (int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Stop();
            }
        }

        private void Start()
        {
            var inputUser = GetComponent<TankInputUser>();
            if (inputUser == null)
            {
                inputUser = gameObject.AddComponent<TankInputUser>();
            }

            // Получаем действие Move из нового Input System
            if (inputUser.ActionAsset != null)
            {
                m_MoveAction = inputUser.ActionAsset.FindActionMap("Gameplay").FindAction("Move");
                if (m_MoveAction != null)
                {
                    m_MoveAction.Enable();
                }
                else
                {
                    Debug.LogError("[PlayerMovement] Действие 'Move' не найдено в ActionAsset 'Gameplay'!");
                }
            }
            else
            {
                Debug.LogError("[PlayerMovement] ActionAsset не назначен на TankInputUser!");
            }

            if (m_MovementAudio)
            {
                m_OriginalPitch = m_MovementAudio.pitch;
            }
        }

        private void Update()
        {
            // Читаем значение из нового Input System
            if (m_MoveAction != null)
            {
                m_MovementInput = m_MoveAction.ReadValue<Vector2>();
            }
            else
            {
                m_MovementInput = Vector2.zero;
            }

            if (m_MovementAudio)
            {
                EngineAudio();
            }
        }

        private void EngineAudio()
        {
            bool isMoving = Mathf.Abs(m_MovementInput.y) > 0.1f || Mathf.Abs(m_MovementInput.x) > 0.1f;

            if (!isMoving)
            {
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
            else
            {
                if (m_MovementAudio.clip == m_EngineIdling)
                {
                    m_MovementAudio.clip = m_EngineDriving;
                    m_MovementAudio.pitch = Random.Range(m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play();
                }
            }
        }

        private void FixedUpdate()
        {
            if (m_IsDirectControl)
            {
                var camForward = Camera.main.transform.forward;
                camForward.y = 0;

                if (camForward.sqrMagnitude < 0.0001f)
                {
                    camForward = Camera.main.transform.up;
                    camForward.y = 0;
                }

                camForward.Normalize();
                var camRight = Vector3.Cross(Vector3.up, camForward);

                m_RequestedDirection = (camForward * m_MovementInput.y + camRight * m_MovementInput.x);
                m_RequestedDirection.Normalize();
            }

            Move();
            Turn();
        }

        private void Move()
        {
            float speedInput = 0.0f;

            if (m_IsDirectControl)
            {
                speedInput = m_RequestedDirection.magnitude;
                speedInput *= 1.0f - Mathf.Clamp01((Vector3.Angle(m_RequestedDirection, transform.forward) - 90) / 90.0f);
            }
            else
            {
                // В обычном режиме: Y от W/S (вперёд/назад), X от A/D (поворот)
                speedInput = m_MovementInput.y;
            }

            // Применяем бонусы от улучшений
            float speedMultiplier = 1f;
            if (m_MovementData != null)
            {
                speedMultiplier = 1f + m_MovementData.speedBonus / 100f;
            }

            Vector3 movement = transform.forward * speedInput * m_Speed * speedMultiplier;

            m_Rigidbody.linearVelocity = movement + m_ExplosionForceValue;
            m_ExplosionForceValue = Vector3.Lerp(m_ExplosionForceValue, Vector3.zero, Time.deltaTime * 3f);
        }

        private void Turn()
        {
            Quaternion turnRotation;

            if (m_IsDirectControl)
            {
                float angleTowardTarget = Vector3.SignedAngle(m_RequestedDirection, transform.forward, transform.up);

                // Применяем бонусы от улучшений
                float turnSpeedMultiplier = 1f;
                if (m_MovementData != null)
                {
                    turnSpeedMultiplier = 1f + m_MovementData.turnSpeedBonus / 100f;
                }

                float maxTurn = m_TurnSpeed * Time.deltaTime * turnSpeedMultiplier;
                float rotatingAngle = Mathf.Sign(angleTowardTarget) * Mathf.Min(Mathf.Abs(angleTowardTarget), maxTurn);
                turnRotation = Quaternion.AngleAxis(-rotatingAngle, Vector3.up);
            }
            else
            {
                // В обычном режиме: X от A/D (поворот влево/вправо)
                float turn = m_MovementInput.x * m_TurnSpeed * Time.deltaTime;
                turnRotation = Quaternion.Euler(0f, turn, 0f);
            }

            m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
        }

        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier = 0f)
        {
            Vector3 explosionDir = transform.position - explosionPosition;
            float explosionDistance = explosionDir.magnitude;

            if (upwardsModifier != 0)
            {
                explosionDir.y += upwardsModifier;
                explosionDir.Normalize();
            }
            else
            {
                explosionDir = explosionDir.normalized;
            }

            float attenuation = 1f - Mathf.Clamp01(explosionDistance / explosionRadius);

            Vector3 velocityChange = explosionDir * (explosionForce * attenuation);

            m_ExplosionForceValue = velocityChange;
        }
    }
}
