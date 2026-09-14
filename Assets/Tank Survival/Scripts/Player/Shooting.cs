using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;

namespace TankSurvival
{
    public class Shooting : MonoBehaviour
    {
        public Rigidbody m_Shell;
        public Transform m_FireTransform;
        public AudioSource m_ShootingAudio;
        public AudioClip m_FireClip;
        public LayerMask enemyMask;
        public Transform aimPosition;
        public GameObject currentGun;
        Rigidbody currentTarget;
        public float m_ShotCooldown = 0.3f;
        public float fireRange = 10f;
        private float m_ShotCooldownTimer = 0.0f;
        private bool m_Fired;
        private float closestDist = float.MaxValue;
        void Awake()
        {

        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, fireRange, enemyMask);
            if (m_ShotCooldownTimer > 0.0f)
            {
                m_ShotCooldownTimer -= Time.deltaTime;
            }

            if (m_ShotCooldownTimer <= 0.0f)
            {
                m_Fired = false;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                Rigidbody targetRigidbody = colliders[i].GetComponent<Rigidbody>();
                TankHealth targetHealth = targetRigidbody.GetComponent<TankHealth>();
                Transform targetTransform = colliders[i].transform;

                float dist = (targetTransform.position - transform.position).sqrMagnitude;
                if (!targetHealth) continue;
                if (dist <= closestDist)
                {
                    currentTarget = targetRigidbody;
                    closestDist = dist;
                    aimPosition = m_FireTransform;
                }
            }
            if (currentTarget)
            {
                AutoAiming();
                Debug.Log("target: " + currentTarget);
            } 
            if (!m_Fired && currentTarget)
            {
                Fire();
            }
            closestDist = float.MaxValue;
            currentTarget = null;
            Debug.DrawLine(aimPosition.position, aimPosition.position + aimPosition.forward, Color.red);
        }

        private void Fire()
        {
            m_Fired = true;
            Rigidbody shellInstance = Instantiate(m_Shell, m_FireTransform.position, m_FireTransform.rotation) as Rigidbody;
            
            shellInstance.linearVelocity = 20 * m_FireTransform.forward;

            m_ShotCooldownTimer = m_ShotCooldown;
            
        }

        private void AutoAiming()
        {
            currentGun.transform.LookAt(currentTarget.transform);
        }
    }
}