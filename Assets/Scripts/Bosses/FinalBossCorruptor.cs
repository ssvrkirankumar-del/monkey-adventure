using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GuardianSystem.Combat;
using MonkeyAdventure.Core;

namespace MonkeyAdventure.Bosses
{
    public enum CorruptorPhase
    {
        Phase1_LaserTurrets,
        Phase2_EliteSwarm,
        Phase3_ExposedCore,
        Defeated
    }

    /// <summary>
    /// Act 5 - Level 50 Final Boss: The Corruptor Machine.
    /// 3-Phase Epic Boss Encounter:
    /// - Phase 1: Laser turrets vulnerable to Guardian Energy Blast.
    /// - Phase 2: Corrupted elite swarm vulnerable to Guardian Ground Smash.
    /// - Phase 3: Magic Updraft aerial launch to deliver a fatal Ground Smash onto the exposed core.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Bosses/Final Boss Corruptor")]
    public class FinalBossCorruptor : MonoBehaviour, IDamageable
    {
        [Header("Boss State")]
        [SerializeField] private CorruptorPhase currentPhase = CorruptorPhase.Phase1_LaserTurrets;

        [Header("Phase 1: Laser Turrets")]
        [Tooltip("Turret game objects to destroy with Energy Blast.")]
        [SerializeField] private List<GameObject> laserTurrets = new List<GameObject>();
        [SerializeField] private LineRenderer[] laserBeams;
        [SerializeField] private float laserRotateSpeed = 45f;

        [Header("Phase 2: Elite Minion Swarm")]
        [SerializeField] private GameObject energyShieldBubble;
        [SerializeField] private GameObject eliteMinionPrefab;
        [SerializeField] private Transform[] minionSpawnPoints;
        [SerializeField] private int minionCount = 4;
        private List<GameObject> _activeMinions = new List<GameObject>();

        [Header("Phase 3: Exposed Core & Aerial Finisher")]
        [Tooltip("The glowing core weak spot on top of the machine.")]
        [SerializeField] private GameObject exposedCore;

        [Tooltip("The Magic Updraft jump pad activated in Phase 3.")]
        [SerializeField] private GameObject phase3MagicUpdraft;

        [SerializeField] private int coreHealth = 100;
        private int _currentCoreHealth;

        [Header("VFX, Cinematics & Audio")]
        [SerializeField] private GameObject explosionVFX;
        [SerializeField] private GameObject finalVictoryCinematic;
        [SerializeField] private AudioClip laserSound;
        [SerializeField] private AudioClip shieldBreakSound;
        [SerializeField] private AudioClip coreExposedSound;
        [SerializeField] private AudioClip epicVictoryFanfare;

        private AudioSource _audioSource;
        private bool _isPhaseTransitioning = false;

        public CorruptorPhase CurrentPhase => currentPhase;

        private void Awake()
        {
            _currentCoreHealth = coreHealth;
            _audioSource = GetComponent<AudioSource>();

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f;
            }
        }

        private void Start()
        {
            InitializePhase1();
        }

        private void Update()
        {
            switch (currentPhase)
            {
                case CorruptorPhase.Phase1_LaserTurrets:
                    UpdatePhase1Lasers();
                    CheckPhase1Progress();
                    break;

                case CorruptorPhase.Phase2_EliteSwarm:
                    CheckPhase2Progress();
                    break;

                case CorruptorPhase.Phase3_ExposedCore:
                    // Awaiting aerial Ground Smash finisher
                    break;
            }
        }

        #region Phase 1: Laser Turrets
        private void InitializePhase1()
        {
            currentPhase = CorruptorPhase.Phase1_LaserTurrets;
            if (energyShieldBubble != null) energyShieldBubble.SetActive(true);
            if (exposedCore != null) exposedCore.SetActive(false);
            if (phase3MagicUpdraft != null) phase3MagicUpdraft.SetActive(false);

            Debug.Log("[FinalBossCorruptor] PHASE 1: Destroy the laser turrets with Guardian Energy Blast!");
        }

        private void UpdatePhase1Lasers()
        {
            // Rotate turrets
            transform.Rotate(Vector3.up * (laserRotateSpeed * Time.deltaTime));
        }

        private void CheckPhase1Progress()
        {
            if (_isPhaseTransitioning) return;

            // Remove destroyed turrets
            laserTurrets.RemoveAll(t => t == null);

            if (laserTurrets.Count == 0)
            {
                StartCoroutine(TransitionToPhase2Routine());
            }
        }
        #endregion

        #region Phase 2: Elite Minion Swarm
        private IEnumerator TransitionToPhase2Routine()
        {
            _isPhaseTransitioning = true;
            Debug.Log("[FinalBossCorruptor] Turrets destroyed! Transitioning to PHASE 2...", this);

            yield return new WaitForSeconds(1.5f);

            currentPhase = CorruptorPhase.Phase2_EliteSwarm;

            // Spawn Minions
            if (eliteMinionPrefab != null && minionSpawnPoints != null)
            {
                for (int i = 0; i < minionCount; i++)
                {
                    Transform spawnPoint = minionSpawnPoints[i % minionSpawnPoints.Length];
                    GameObject minion = Instantiate(eliteMinionPrefab, spawnPoint.position, spawnPoint.rotation);
                    _activeMinions.Add(minion);
                }
            }

            _isPhaseTransitioning = false;
            Debug.Log("[FinalBossCorruptor] PHASE 2: Clear the Corrupted Swarm with Guardian Ground Smash!");
        }

        private void CheckPhase2Progress()
        {
            if (_isPhaseTransitioning) return;

            _activeMinions.RemoveAll(m => m == null);

            if (_activeMinions.Count == 0)
            {
                StartCoroutine(TransitionToPhase3Routine());
            }
        }
        #endregion

        #region Phase 3: Exposed Core & Aerial Finisher
        private IEnumerator TransitionToPhase3Routine()
        {
            _isPhaseTransitioning = true;

            // 1. Break Shield
            if (energyShieldBubble != null)
            {
                energyShieldBubble.SetActive(false);
            }

            if (shieldBreakSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(shieldBreakSound);
            }

            yield return new WaitForSeconds(1.0f);

            currentPhase = CorruptorPhase.Phase3_ExposedCore;

            // 2. Expose Core & Enable Magic Updraft
            if (exposedCore != null) exposedCore.SetActive(true);
            if (phase3MagicUpdraft != null) phase3MagicUpdraft.SetActive(true);

            if (coreExposedSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(coreExposedSound);
            }

            _isPhaseTransitioning = false;
            Debug.Log("[FinalBossCorruptor] PHASE 3: Core Exposed! Jump into the Magic Updraft and perform an Aerial Ground Smash!");
        }
        #endregion

        #region Damage & Epic Victory
        public void TakeDamage(int amount)
        {
            if (currentPhase != CorruptorPhase.Phase3_ExposedCore)
            {
                Debug.Log("[FinalBossCorruptor] Machine shield is invulnerable! Complete the current phase objectives!", this);
                return;
            }

            _currentCoreHealth -= amount;
            Debug.Log($"[FinalBossCorruptor] CORE STRUCK! Core HP: {_currentCoreHealth}/{coreHealth}");

            if (_currentCoreHealth <= 0)
            {
                TriggerUltimateVictory();
            }
        }

        private void TriggerUltimateVictory()
        {
            currentPhase = CorruptorPhase.Defeated;
            Debug.Log("🏆 [FinalBossCorruptor] THE CORRUPTOR MACHINE IS DESTROYED! YOU ARE THE TRUE FOREST GUARDIAN! 🏆");

            // 1. Massive Explosion VFX
            if (explosionVFX != null)
            {
                GameObject vfx = Instantiate(explosionVFX, transform.position + Vector3.up * 2f, Quaternion.identity);
                Destroy(vfx, 6f);
            }

            // 2. Fanfare Audio
            if (epicVictoryFanfare != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(epicVictoryFanfare);
            }

            // 3. Award 500 Coins & 50 Bananas
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(500);
                GameManager.Instance.AddFood(50);
            }

            // 4. Trigger Ending Cinematic
            if (finalVictoryCinematic != null)
            {
                finalVictoryCinematic.SetActive(true);
            }

            Destroy(gameObject, 1.5f);
        }
        #endregion
    }
}
