using System;
using UnityEngine;
using GuardianSystem.Combat;

namespace MonkeyAdventure.Puzzles
{
    /// <summary>
    /// Act 2: Rune Switch.
    /// Stone switch that activates when struck by player attacks, projectiles, or player interaction.
    /// Transitions material to glowing blue URP emission and alerts linked systems.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Puzzles/Rune Switch")]
    [RequireComponent(typeof(Collider))]
    public class RuneSwitch : MonoBehaviour, IDamageable
    {
        [Header("State")]
        [SerializeField] private bool isActivated = false;
        [Tooltip("If true, the switch stays permanently active once triggered.")]
        [SerializeField] private bool stayActiveForever = true;

        [Header("Visual & URP Material")]
        [Tooltip("Renderer containing the rune symbol.")]
        [SerializeField] private Renderer runeRenderer;

        [Tooltip("Material applied when rune switch is active (with HDR/URP Emission).")]
        [SerializeField] private Material activeGlowingMaterial;

        [Tooltip("Color of emission if modifying material properties directly.")]
        [ColorUsage(true, true)]
        [SerializeField] private Color emissionColor = new Color(0f, 0.75f, 1f, 1f) * 3f;

        [Header("FX & Audio")]
        [SerializeField] private GameObject activationVFXPrefab;
        [SerializeField] private AudioClip activationSound;
        [SerializeField] private float vfxDuration = 2.5f;

        public event Action<RuneSwitch> OnSwitchStateChanged;

        public bool IsActivated => isActivated;
        public bool StayActiveForever => stayActiveForever;

        private Material _instancedMaterial;

        private void Awake()
        {
            if (runeRenderer == null)
            {
                runeRenderer = GetComponent<Renderer>();
            }

            if (runeRenderer != null)
            {
                _instancedMaterial = runeRenderer.material;
            }
        }

        private void Start()
        {
            if (isActivated)
            {
                ApplyActiveVisuals();
            }
        }

        #region Interaction & Damage Triggers
        /// <summary>
        /// Triggered when struck by Guardian combat powers or MagicProjectile.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (!isActivated)
            {
                ActivateSwitch();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isActivated) return;

            // Triggered by Player or Magic Projectile
            if (other.CompareTag("Player") || other.GetComponent<MagicProjectile>() != null)
            {
                ActivateSwitch();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isActivated) return;

            if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<MagicProjectile>() != null)
            {
                ActivateSwitch();
            }
        }
        #endregion

        /// <summary>
        /// Activates the rune switch and notifies listeners.
        /// </summary>
        public void ActivateSwitch()
        {
            if (isActivated) return;

            isActivated = true;
            ApplyActiveVisuals();

            // Spawn VFX
            if (activationVFXPrefab != null)
            {
                GameObject vfx = Instantiate(activationVFXPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, vfxDuration);
            }

            // Play Audio
            if (activationSound != null)
            {
                AudioSource.PlayClipAtPoint(activationSound, transform.position);
            }

            OnSwitchStateChanged?.Invoke(this);
            Debug.Log($"[RuneSwitch] '{name}' ACTIVATED!", this);
        }

        public void ResetSwitch()
        {
            isActivated = false;
            ApplyInactiveVisuals();
            OnSwitchStateChanged?.Invoke(this);
            Debug.Log($"[RuneSwitch] '{name}' Reset to Inactive.", this);
        }

        private void ApplyActiveVisuals()
        {
            if (runeRenderer != null)
            {
                if (activeGlowingMaterial != null)
                {
                    runeRenderer.material = activeGlowingMaterial;
                }
                else if (_instancedMaterial != null)
                {
                    _instancedMaterial.EnableKeyword("_EMISSION");
                    _instancedMaterial.SetColor("_EmissionColor", emissionColor);
                }
            }
        }

        private void ApplyInactiveVisuals()
        {
            if (runeRenderer != null && _instancedMaterial != null)
            {
                _instancedMaterial.DisableKeyword("_EMISSION");
                _instancedMaterial.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}
