using UnityEngine;
using UnityEngine.AI;
using GuardianSystem.Combat;
using MonkeyAdventure.AI;
using MonkeyAdventure.Environment;
using MonkeyAdventure.Core;

namespace MonkeyAdventure.Bosses
{
    /// <summary>
    /// Act 4 - Level 40 Boss: The Shadow Beast.
    /// In darkness: Invisible, fast, and immune to damage.
    /// In Bioluminescent Light (SafeZone): Revealed, speed reduced by 80%, vulnerable to player attacks.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Bosses/Shadow Beast Boss")]
    public class ShadowBeastBoss : EnemyAI
    {
        [Header("Boss Health")]
        [SerializeField] private int bossMaxHealth = 350;
        [SerializeField] private int bossCurrentHealth;

        [Header("Speed Settings")]
        [Tooltip("Fast speed in the dark.")]
        [SerializeField] private float darkSpeed = 8.5f;

        [Tooltip("Speed when weakened in the light (80% reduction).")]
        [SerializeField] private float lightWeakenedSpeed = 1.7f;

        [Header("Visual Renderers & Particle Aura")]
        [Tooltip("Renderers hidden in darkness and revealed in light.")]
        [SerializeField] private Renderer[] beastRenderers;

        [Tooltip("Dark smoke/shadow particle aura.")]
        [SerializeField] private ParticleSystem shadowSmokeAura;

        [Tooltip("Light burning / smoke steam VFX when inside light.")]
        [SerializeField] private ParticleSystem lightBurningVFX;

        [Header("Audio")]
        [SerializeField] private AudioClip shadowWhisperSound;
        [SerializeField] private AudioClip lightBurnScreamSound;

        [Header("Unlock Act 5 Sky Path")]
        [SerializeField] private GameObject act5SkyPortal;

        private bool _isInLight = false;
        private NavMeshAgent _navMeshAgent;
        private AudioSource _audio;

        public bool IsInLight => _isInLight;
        public int BossCurrentHealth => bossCurrentHealth;

        private void Awake()
        {
            bossCurrentHealth = bossMaxHealth;
            _navMeshAgent = GetComponent<NavMeshAgent>();
            _audio = GetComponent<AudioSource>();

            if (beastRenderers == null || beastRenderers.Length == 0)
            {
                beastRenderers = GetComponentsInChildren<Renderer>();
            }

            // Start in Dark Shadow state
            SetShadowState(true);
        }

        private void Update()
        {
            // Update agent speed based on lighting
            if (_navMeshAgent != null && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.speed = _isInLight ? lightWeakenedSpeed : darkSpeed;
            }
        }

        #region SafeZone Light Trigger Checks
        private void OnTriggerEnter(Collider other)
        {
            // Check if entering a SafeZone crystal or Light Zone
            if (other.GetComponent<SafeZone>() != null || other.name.ToLower().Contains("safezone") || other.name.ToLower().Contains("light"))
            {
                EnterLightZone();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!_isInLight && (other.GetComponent<SafeZone>() != null || other.name.ToLower().Contains("safezone")))
            {
                EnterLightZone();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<SafeZone>() != null || other.name.ToLower().Contains("safezone") || other.name.ToLower().Contains("light"))
            {
                ExitLightZone();
            }
        }

        public void EnterLightZone()
        {
            if (_isInLight) return;

            _isInLight = true;
            SetShadowState(false);

            if (lightBurningVFX != null) lightBurningVFX.Play();

            if (lightBurnScreamSound != null && _audio != null)
            {
                _audio.PlayOneShot(lightBurnScreamSound);
            }

            Debug.Log("[ShadowBeastBoss] REVEALED IN LIGHT! Speed dropped by 80% & VULNERABLE!", this);
        }

        public void ExitLightZone()
        {
            if (!_isInLight) return;

            _isInLight = false;
            SetShadowState(true);

            if (lightBurningVFX != null) lightBurningVFX.Stop();

            if (shadowWhisperSound != null && _audio != null)
            {
                _audio.PlayOneShot(shadowWhisperSound);
            }

            Debug.Log("[ShadowBeastBoss] Vanished back into Darkness. Fast & Immune.", this);
        }
        #endregion

        private void SetShadowState(bool isShadow)
        {
            // 1. Hide/Show Renderers
            if (beastRenderers != null)
            {
                foreach (var rend in beastRenderers)
                {
                    if (rend != null) rend.enabled = !isShadow; // Invisible in dark, visible in light
                }
            }

            // 2. Toggle Shadow particles
            if (shadowSmokeAura != null)
            {
                if (isShadow) shadowSmokeAura.Play();
                else shadowSmokeAura.Stop();
            }
        }

        #region Damage
        public new void TakeDamage(int amount)
        {
            // Immune if in darkness
            if (!_isInLight)
            {
                Debug.Log("[ShadowBeastBoss] IMMUNE! Lure the Shadow Beast into the glowing Crystal Light to weaken it!", this);
                return;
            }

            bossCurrentHealth -= amount;
            Debug.Log($"[ShadowBeastBoss] Struck in Light for {amount} damage! HP: {bossCurrentHealth}/{bossMaxHealth}");

            if (bossCurrentHealth <= 0)
            {
                DefeatShadowBeast();
            }
        }

        private void DefeatShadowBeast()
        {
            Debug.Log("[ShadowBeastBoss] DEFEATED! Act 5 Sky Portal Unlocked!");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(100);
            }

            if (act5SkyPortal != null)
            {
                act5SkyPortal.SetActive(true);
            }

            Destroy(gameObject, 0.2f);
        }
        #endregion
    }
}
