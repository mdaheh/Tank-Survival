using UnityEngine;
using UnityEngine.UI;

namespace TankSurvival
{
    public class TankHealth : MonoBehaviour, IDamageable
    {
        public float m_StartingHealth = 100f;               // The amount of health each tank starts with.
        public Slider m_Slider;                             // The slider to represent how much health the tank currently has.
        public Image m_FillImage;                           // The image component of the slider.
        public Color m_FullHealthColor = Color.green;    // The color the health bar will be when on full health.
        public Color m_ZeroHealthColor = Color.red;      // The color the health bar will be when on no health.
        public GameObject m_ExplosionPrefab;                // A prefab that will be instantiated in Awake, then used whenever the tank dies.
        [HideInInspector] public bool m_HasShield;          // Has the tank picked up a shield power up?
        
        
        private AudioSource m_ExplosionAudio;               // The audio source to play when the tank explodes.
        private ParticleSystem m_ExplosionParticles;        // The particle system the will play when the tank is destroyed.
        private float m_CurrentHealth;                      // How much health the tank currently has.
        private bool m_Dead;                                // Has the tank been reduced beyond zero health yet?
        private float m_ShieldValue;                        // Percentage of reduced damage when the tank has a shield.
        private bool m_IsInvincible;                        // Is the tank invincible in this moment?

        /// <summary>
        /// Событие смерти — вызывается в OnDeath, когда здоровье падает до 0.
        /// Используется вместо EnemyDeathListener для подписки на смерть врагов.
        /// </summary>
        public event System.Action OnDeathEvent;

        // T020: реализация IDamageable — урон по площади (ShellExplosion) идёт через интерфейс.
        // Имя события не меняем на DeathEvent: подписка GameManager.OnPlayerDied остаётся как есть.
        public float CurrentHealth => m_CurrentHealth;
        public float MaxHealth => m_StartingHealth;
        public bool IsAlive => !m_Dead;

        event System.Action IDamageable.DeathEvent
        {
            add { OnDeathEvent += value; }
            remove { OnDeathEvent -= value; }
        }

        // T019: статы забега — максимальное здоровье читается отсюда
        private StatBlock m_StatBlock;

        /// <summary>
        /// T019: принять StatBlock забега.
        /// </summary>
        public void SetStatBlock(StatBlock statBlock)
        {
            m_StatBlock = statBlock;
        }

        /// <summary>
        /// T019: пересчитать максимальное здоровье из StatBlock, сохранив текущий процент HP.
        /// Вызывается после спавна (пока m_StartingHealth — из префаба) и после улучшения.
        /// </summary>
        public void RefreshStats()
        {
            if (m_StatBlock == null) return;

            float healthPercentage = m_StartingHealth > 0f ? m_CurrentHealth / m_StartingHealth : 1f;
            m_StartingHealth = m_StatBlock.GetMaxHealth();

            if (!m_Dead)
                m_CurrentHealth = m_StartingHealth * healthPercentage;

            if (m_Slider != null)
                m_Slider.maxValue = m_StartingHealth;

            SetHealthUI();
        }

        private void Awake ()
        {
            if (m_ExplosionPrefab != null)
            {
                m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
                m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();
                m_ExplosionParticles.gameObject.SetActive(false);
            }

            if (m_Slider != null)
                m_Slider.maxValue = m_StartingHealth;
        }

        private void OnDestroy()
        {
            if(m_ExplosionParticles != null)
                Destroy(m_ExplosionParticles.gameObject);
        }

        /// <summary>
        /// Пересчитать здоровье и обновить UI (вызывается при спавне/респауне)
        /// </summary>
        public void ResetHealth()
        {
            m_CurrentHealth = m_StartingHealth;
            m_Dead = false;
            m_HasShield = false;
            m_ShieldValue = 0;
            m_IsInvincible = false;

            // Update the health slider's value and color.
            SetHealthUI();
        }

        private void OnEnable()
        {
            ResetHealth();
        }


        public void TakeDamage (float amount)
        {
            // Check if the tank is not invincible
            if (!m_IsInvincible)
            {
                // Reduce current health by the amount of damage done.
                m_CurrentHealth -= amount * (1 - m_ShieldValue);

                // Change the UI elements appropriately.
                SetHealthUI ();

                // If the current health is at or below zero and it has not yet been registered, call OnDeath.
                if (m_CurrentHealth <= 0f && !m_Dead)
                {
                    OnDeath ();
                }
            }
        }


        public void IncreaseHealth(float amount)
        {
            // Check if adding the amount would keep the health within the maximum limit
            if (m_CurrentHealth + amount <= m_StartingHealth)
            {
                // If the new health value is within the limit, add the amount
                m_CurrentHealth += amount;
            }
            else
            {
                // If the new health exceeds the starting health, set it at the maximum
                m_CurrentHealth = m_StartingHealth;
            }

            // Change the UI elements appropriately.
            SetHealthUI();
        }

        /// <summary>
        /// Увеличить максимальное здоровье, сохраняя текущий процент HP
        /// </summary>
        public void IncreaseMaxHealth(float amount)
        {
            if (amount <= 0) return;

            // Сохраняем текущий процент здоровья
            float healthPercentage = m_StartingHealth > 0 ? m_CurrentHealth / m_StartingHealth : 1f;

            // Увеличиваем максимальное здоровье
            m_StartingHealth += amount;

            // Применяем тот же процент к новому максимуму
            m_CurrentHealth = m_StartingHealth * healthPercentage;

            // Ограничиваем, чтобы не превысить новый максимум
            if (m_CurrentHealth > m_StartingHealth)
                m_CurrentHealth = m_StartingHealth;

            // Обновляем UI
            SetHealthUI();
        }


        public void ToggleShield (float shieldAmount)
        {
            // Inverts the value of has shield.
            m_HasShield = !m_HasShield;

            // Stablish the amount of damage that will be reduced by the shield
            if (m_HasShield)
            {
                m_ShieldValue = shieldAmount;
            }
            else
            {
                m_ShieldValue = 0;
            }
        }

        public void ToggleInvincibility()
        {
            m_IsInvincible = !m_IsInvincible;
        }


        private void SetHealthUI ()
        {
            if (m_Slider == null || m_FillImage == null)
                return;

            m_Slider.value = m_CurrentHealth;
            m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, m_CurrentHealth / m_StartingHealth);
        }


        private void OnDeath ()
        {
            // Set the flag so that this function is only called once.
            m_Dead = true;

            // Вызываем событие смерти — слушатели узнают, что объект уничтожен
            OnDeathEvent?.Invoke();

            if (m_ExplosionParticles != null)
            {
                m_ExplosionParticles.transform.position = transform.position;
                m_ExplosionParticles.gameObject.SetActive(true);
                m_ExplosionParticles.Play();
            }

            if (m_ExplosionAudio != null)
                m_ExplosionAudio.Play();

            // Turn the tank off.
            // НЕ Destroy — объект может быть из пула. SetActive(false) позволяет вернуть его.
            gameObject.SetActive (false);
        }
    }
}