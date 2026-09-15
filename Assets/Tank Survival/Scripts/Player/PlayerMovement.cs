using UnityEngine;
using UnityEngine.InputSystem;

namespace TankSurvival
{
    /// <summary>
    /// Движение игрока — наследуется от логики TankMover (общая база для всех танков).
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

        private string m_MovementAxisName;          // The name of the input axis for moving forward and back.
        private string m_TurnAxisName;              // The name of the input axis for turning.
        private Rigidbody m_Rigidbody;              // Reference used to move the tank.
        private float m_MovementInputValue;         // The current value of the movement input.
        private float m_TurnInputValue;             // The current value of the turn input.
        private Vector3 m_ExplosionForceValue;      // The current force applied on the tank from an explosion.
        private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.
        private ParticleSystem[] m_particleSystems; // References to all the particle systems used by the Tank

        private InputAction m_MoveAction;           // The InputAction used to move
        private InputAction m_TurnAction;           // The InputAction used to turn

        private Vector3 m_RequestedDirection;       // In Direct Control mode, store the direction the user wants to go toward

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

            m_MovementInputValue = 0f;
            m_TurnInputValue = 0f;
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
                inputUser = gameObject.AddComponent<TankInputUser>();

            m_MovementAxisName = "Vertical";
            m_TurnAxisName = "Horizontal";

            m_MoveAction = inputUser.ActionAsset.FindAction(m_MovementAxisName);
            m_TurnAction = inputUser.ActionAsset.FindAction(m_TurnAxisName);

            m_MoveAction.Enable();
            m_TurnAction.Enable();

            if (m_MovementAudio)
            {
                m_OriginalPitch = m_MovementAudio.pitch;
            }
        }

        private void Update()
        {
            m_MovementInputValue = m_MoveAction.ReadValue<float>();
            m_TurnInputValue = m_TurnAction.ReadValue<float>();

            if (m_MovementAudio)
            {
                EngineAudio();
            }
        }

        private void EngineAudio()
        {
            if (Mathf.Abs(m_MovementInputValue) < 0.1f && Mathf.Abs(m_TurnInputValue) < 0.1f)
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

                m_RequestedDirection = (camForward * m_MovementInputValue + camRight * m_TurnInputValue);
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
                speedInput = m_MovementInputValue;
            }

            // Применяем бонусы от улучшений
            float speedMultiplier = 1f;
            if (m_MovementData != null)
            {
                speedMultiplier = 1f + m_MovementData.speedBonus / 100f; // speedBonus — в процентах
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
                float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;
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
