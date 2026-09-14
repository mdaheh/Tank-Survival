using UnityEngine;

namespace TankSurvival
{
    public class EnemyMovement : MonoBehaviour
    {
        [Tooltip("The speed in unity unit/second the tank move at")]
        public float m_Speed = 12f;                 // How fast the tank moves forward and back.
        [Tooltip("The speed in deg/s that tank will rotate at")]
        public float m_TurnSpeed = 180f;            // How fast the tank turns in degrees per second.
        public AudioSource m_MovementAudio;         // Reference to the audio source used to play engine sounds. NB: different to the shooting audio source.
        public AudioClip m_EngineIdling;            // Audio to play when the tank isn't moving.
        public AudioClip m_EngineDriving;           // Audio to play when the tank is moving.
        public float m_PitchRange = 0.2f;           // The amount by which the pitch of the engine noises can vary.

        public Rigidbody Rigidbody => m_Rigidbody;

        private Rigidbody m_Rigidbody;              // Reference used to move the tank.
        private Vector3 m_ExplosionForceValue;      // The current force applied on the tank from an explosion.
        private float m_OriginalPitch;              // The pitch of the audio source at the start of the scene.
        private ParticleSystem[] m_particleSystems; // References to all the particle systems used by the Tank
        private float m_MovementInputValue;         // Simulated movement input for engine audio
        private float m_TurnInputValue;             // Simulated turn input for engine audio

        private void Awake ()
        {
            m_Rigidbody = GetComponent<Rigidbody> ();
        }

        private void OnEnable ()
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

        private void OnDisable ()
        {
            m_Rigidbody.isKinematic = true;

            for(int i = 0; i < m_particleSystems.Length; ++i)
            {
                m_particleSystems[i].Stop();
            }
        }

        private void Start ()
        {
            if(m_MovementAudio)
            {
                m_OriginalPitch = m_MovementAudio.pitch;
            }
        }

        private void Update ()
        {
            if(m_MovementAudio)
            {
                EngineAudio ();
            }
        }

        private void EngineAudio ()
        {
            if (Mathf.Abs (m_MovementInputValue) < 0.1f && Mathf.Abs (m_TurnInputValue) < 0.1f)
            {
                if (m_MovementAudio.clip == m_EngineDriving)
                {
                    m_MovementAudio.clip = m_EngineIdling;
                    m_MovementAudio.pitch = Random.Range (m_OriginalPitch - m_PitchRange, m_OriginalPitch + m_PitchRange);
                    m_MovementAudio.Play ();
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

        /// <summary>
        /// Call this to set the movement input for engine audio feedback
        /// </summary>
        public void SetMovementInput(float movementInput, float turnInput)
        {
            m_MovementInputValue = movementInput;
            m_TurnInputValue = turnInput;
        }

        /// <summary>
        /// Move the tank forward based on speed and time
        /// </summary>
        public void Move(float speedMultiplier = 1f)
        {
            float speedInput = m_MovementInputValue;
            if (speedInput == 0f)
                speedInput = 1f;

            Vector3 movement = transform.forward * speedInput * m_Speed * speedMultiplier;

            m_Rigidbody.linearVelocity = movement + m_ExplosionForceValue;
            m_ExplosionForceValue = Vector3.Lerp(m_ExplosionForceValue, Vector3.zero, Time.deltaTime * 3f);
        }

        /// <summary>
        /// Rotate the tank by the given angle in degrees
        /// </summary>
        public void Turn(float angle)
        {
            if (Mathf.Abs(angle) > 0.000001f)
            {
                Quaternion turnRotation = Quaternion.Euler (0f, angle, 0f);
                m_Rigidbody.MoveRotation (m_Rigidbody.rotation * turnRotation);
            }
        }

        /// <summary>
        /// Rotate the tank toward a target direction
        /// </summary>
        public void TurnToward(Vector3 direction, float speedMultiplier = 1f)
        {
            direction.y = 0;
            direction.Normalize();

            Vector3 forward = transform.forward;
            float rotatingAngle = Vector3.SignedAngle(direction, forward, Vector3.up);

            float maxTurn = m_TurnSpeed * Time.deltaTime * speedMultiplier;
            rotatingAngle = Mathf.Sign(rotatingAngle) * Mathf.Min(Mathf.Abs(rotatingAngle), maxTurn);

            if (Mathf.Abs(rotatingAngle) > 0.000001f)
            {
                Quaternion turnRotation = Quaternion.AngleAxis(-rotatingAngle, Vector3.up);
                m_Rigidbody.MoveRotation (m_Rigidbody.rotation * turnRotation);
            }
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
