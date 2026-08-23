using System;
using System.Collections;
using UnityEngine;
using GuardianSystem.Combat;
using MonkeyAdventure.Core;

namespace MonkeyAdventure.Player
{
    /// <summary>
    /// PlayerHealth handles health management, damage reception via IDamageable,
    /// invulnerability frames (i-frames), damage feedback, and checkpoint respawning upon death.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Player/Player Health")]
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [Tooltip("Maximum health capacity.")]
        [SerializeField] private int maxHealth = 100;

        [Tooltip("Current health value.")]
        [SerializeField] private int currentHealth;

        [Header("Invulnerability Frames (i-Frames)")]
        [Tooltip("Duration of temporary invincibility after taking damage.")]
        [SerializeField] private float invulnerabilityDuration = 1.0f;

        [Tooltip("Flicker rate of player model during invulnerability.")]
        [SerializeField] private float flickerSpeed = 0.1f;

        [Header("Visual & Audio Feedback")]
        [SerializeField] private Renderer[] playerRenderers;
        [SerializeField] private GameObject hitVFXPrefab;
        [SerializeField] private GameObject deathVFXPrefab;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip deathSound;
        [SerializeField] private AudioClip healSound;

        [Header("Audio Source")]
        [SerializeField] private AudioSource audioSource;

        private bool _isInvulnerable = false;
        private bool _isDead = false;

        #region Events
        public event Action<int, int> OnHealthChanged; // (current, max)
        public event Action OnPlayerDied;
        #endregion

        #region Properties
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public float HealthPercent => (float)currentHealth / maxHealth;
        public bool IsDead => _isDead;
        public bool IsInvulnerable => _isInvulnerable;
        #endregion

        private void Awake()
        {
            currentHealth = maxHealth;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null && (hitSound != null || deathSound != null))
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }

            if (playerRenderers == null || playerRenderers.Length == 0)
            {
                playerRenderers = GetComponentsInChildren<Renderer>();
            }
        }

        private void Start()
        {
            // Subscribe to GameManager respawn event to auto-restore health
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerRespawned += HandleRespawnRestoration;
            }

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerRespawned -= HandleRespawnRestoration;
            }
        }

        #region IDamageable Implementation
        /// <summary>
        /// Applies damage to the player if not currently invulnerable or dead.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (_isDead || _isInvulnerable) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Play Hit Audio & VFX
            if (hitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitSound);
            }

            if (hitVFXPrefab != null)
            {
                GameObject vfx = Instantiate(hitVFXPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
                Destroy(vfx, 2f);
            }

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(InvulnerabilityRoutine());
            }
        }
        #endregion

        #region Healing
        /// <summary>
        /// Restores player health up to maxHealth.
        /// </summary>
        public void Heal(int amount)
        {
            if (_isDead) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (healSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(healSound);
            }
        }
        #endregion

        [Header("Revive System")]
        [Tooltip("If true, triggers OnPlayerDied for ReviveUIManager to offer Ad/Gem revive before checkpoint respawn.")]
        [SerializeField] private bool enableRevivePrompt = true;

        public bool EnableRevivePrompt
        {
            get => enableRevivePrompt;
            set => enableRevivePrompt = value;
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log("[PlayerHealth] Player has fallen! Notifying ReviveUIManager...", this);

            // Play Death Audio & VFX
            if (deathSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deathSound);
            }

            if (deathVFXPrefab != null)
            {
                GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 3f);
            }

            OnPlayerDied?.Invoke();

            if (!enableRevivePrompt)
            {
                // Instant Checkpoint Respawn if revive prompt is disabled
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerManualRespawn();
                }
                else
                {
                    StartCoroutine(FallbackRespawnRoutine());
                }
            }
        }

        /// <summary>
        /// Revives the player at their current location with 100% health and invulnerability frames.
        /// </summary>
        public void ReviveInPlace(int healthPercentage = 100)
        {
            _isDead = false;
            currentHealth = Mathf.RoundToInt(maxHealth * (healthPercentage / 100f));
            SetRenderersVisible(true);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            StartCoroutine(InvulnerabilityRoutine());
            Debug.Log($"[PlayerHealth] Player REVIVED IN PLACE with {currentHealth} HP!", this);
        }

        private void HandleRespawnRestoration()
        {
            _isDead = false;
            currentHealth = maxHealth;
            _isInvulnerable = false;
            SetRenderersVisible(true);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            Debug.Log("[PlayerHealth] Player Restored at Checkpoint!", this);
        }

        private IEnumerator FallbackRespawnRoutine()
        {
            yield return new WaitForSeconds(1.0f);
            HandleRespawnRestoration();
        }

        private IEnumerator InvulnerabilityRoutine()
        {
            _isInvulnerable = true;
            float elapsed = 0f;
            bool isVisible = true;

            while (elapsed < invulnerabilityDuration)
            {
                isVisible = !isVisible;
                SetRenderersVisible(isVisible);
                yield return new WaitForSeconds(flickerSpeed);
                elapsed += flickerSpeed;
            }

            SetRenderersVisible(true);
            _isInvulnerable = false;
        }

        private void SetRenderersVisible(bool visible)
        {
            if (playerRenderers == null) return;
            foreach (var rend in playerRenderers)
            {
                if (rend != null)
                {
                    rend.enabled = visible;
                }
            }
        }
    }
}
