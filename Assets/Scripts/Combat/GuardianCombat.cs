using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuardianSystem.Combat
{
    /// <summary>
    /// GuardianCombat handles the combat abilities for the player character when in Guardian Form.
    /// Includes Power 1 (Energy Blast projectile) and Power 2 (Ground Smash AOE explosion).
    /// </summary>
    [AddComponentMenu("Guardian System/Guardian Combat")]
    [DisallowMultipleComponent]
    public class GuardianCombat : MonoBehaviour
    {
        [Header("Guardian Form State")]
        [Tooltip("If false, guardian powers and attacks will be disabled.")]
        [SerializeField] private bool isGuardianForm = true;

        [Header("Power 1: Energy Blast (Primary Attack)")]
        [Tooltip("Prefab of the magic projectile (must have MagicProjectile script & Rigidbody).")]
        [SerializeField] private GameObject magicProjectilePrefab;

        [Tooltip("Transform from where the projectile originates.")]
        [SerializeField] private Transform firePoint;

        [Tooltip("Speed imparted to the projectile.")]
        [SerializeField] private float projectileSpeed = 30f;

        [Tooltip("Minimum time between consecutive energy blasts.")]
        [SerializeField] private float blastCooldown = 0.35f;

        [Tooltip("Key used to trigger Energy Blast.")]
        [SerializeField] private KeyCode primaryAttackKey = KeyCode.Mouse0;

        [Tooltip("Optional muzzle flash particle effect when firing.")]
        [SerializeField] private ParticleSystem muzzleFlashEffect;

        [Tooltip("Optional sound effect when shooting.")]
        [SerializeField] private AudioClip blastAudioClip;

        [Header("Power 2: Ground Smash (Secondary AOE Attack)")]
        [Tooltip("Radius of the AOE ground smash explosion.")]
        [SerializeField] private float smashRadius = 7f;

        [Tooltip("Explosive pushback force applied to nearby enemies.")]
        [SerializeField] private float smashExplosionForce = 900f;

        [Tooltip("Upward lift modifier for the explosion physics.")]
        [SerializeField] private float smashUpwardsModifier = 1.8f;

        [Tooltip("Damage dealt to enemies within the smash radius.")]
        [SerializeField] private int smashDamage = 50;

        [Tooltip("Cooldown period for the ground smash in seconds.")]
        [SerializeField] private float smashCooldown = 2.0f;

        [Tooltip("Key used to trigger Ground Smash.")]
        [SerializeField] private KeyCode secondaryAttackKey = KeyCode.Mouse1;

        [Tooltip("Particle effect spawned at the smash center.")]
        [SerializeField] private GameObject groundSmashVFXPrefab;

        [Tooltip("Optional custom ground position transform. If null, player feet/position is used.")]
        [SerializeField] private Transform smashPoint;

        [Tooltip("Layer mask filter for smash detection (leave Everything/Default if using Tags).")]
        [SerializeField] private LayerMask targetLayers = ~0;

        [Tooltip("Tag required on target objects to receive damage and pushback.")]
        [SerializeField] private string enemyTag = "Enemy";

        [Tooltip("Optional sound effect for ground smash.")]
        [SerializeField] private AudioClip smashAudioClip;

        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;

        [Header("Debug & Gizmos")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color smashGizmoColor = new Color(1f, 0.5f, 0f, 0.35f);

        // Internal Cooldown Timers
        private float _nextBlastTime;
        private float _nextSmashTime;

        #region Public Properties
        /// <summary>
        /// Gets or sets whether the character is currently in Guardian Form.
        /// </summary>
        public bool IsGuardianForm
        {
            get => isGuardianForm;
            set => isGuardianForm = value;
        }

        public float NextBlastTime => _nextBlastTime;
        public float NextSmashTime => _nextSmashTime;
        #endregion

        private void Awake()
        {
            // Auto-setup AudioSource if not manually assigned
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null && (blastAudioClip != null || smashAudioClip != null))
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f; // 2D sound for player feedback
                }
            }

            // Fallback for FirePoint if not assigned
            if (firePoint == null)
            {
                firePoint = transform;
            }
        }

        private void Update()
        {
            // If not in Guardian Form, abilities are locked
            if (!isGuardianForm) return;

            HandleCombatInput();
        }

        /// <summary>
        /// Polls combat inputs and triggers abilities when cooldowns allow.
        /// </summary>
        private void HandleCombatInput()
        {
            // Primary Attack: Energy Blast
            if (Input.GetKeyDown(primaryAttackKey) && Time.time >= _nextBlastTime)
            {
                CastEnergyBlast();
            }

            // Secondary Attack: Ground Smash
            if (Input.GetKeyDown(secondaryAttackKey) && Time.time >= _nextSmashTime)
            {
                CastGroundSmash();
            }
        }

        #region Power 1: Energy Blast
        /// <summary>
        /// Spawns and launches a magic projectile forward from firePoint.
        /// </summary>
        public void CastEnergyBlast()
        {
            if (!isGuardianForm) return;

            _nextBlastTime = Time.time + blastCooldown;

            if (magicProjectilePrefab == null)
            {
                Debug.LogWarning("[GuardianCombat] Magic Projectile Prefab is not assigned!", this);
                return;
            }

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + transform.forward;
            Quaternion spawnRot = firePoint != null ? firePoint.rotation : transform.rotation;

            // Instantiate projectile
            GameObject projectileObj = Instantiate(magicProjectilePrefab, spawnPos, spawnRot);

            // Configure MagicProjectile component if present
            if (projectileObj.TryGetComponent<MagicProjectile>(out var magicProj))
            {
                magicProj.Launch(spawnRot * Vector3.forward, projectileSpeed);
            }
            else if (projectileObj.TryGetComponent<Rigidbody>(out var rb))
            {
                #if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = spawnRot * Vector3.forward * projectileSpeed;
                #else
                rb.velocity = spawnRot * Vector3.forward * projectileSpeed;
                #endif
            }

            // Play Muzzle Flash VFX
            if (muzzleFlashEffect != null)
            {
                muzzleFlashEffect.Play();
            }

            // Play Audio
            PlaySound(blastAudioClip);
        }
        #endregion

        #region Power 2: Ground Smash
        /// <summary>
        /// Triggers an AOE ground smash that applies explosive pushback and damage to nearby enemies.
        /// </summary>
        public void CastGroundSmash()
        {
            if (!isGuardianForm) return;

            _nextSmashTime = Time.time + smashCooldown;

            Vector3 origin = smashPoint != null ? smashPoint.position : transform.position;

            // 1. Spawn Ground Smash Particle VFX
            if (groundSmashVFXPrefab != null)
            {
                GameObject vfx = Instantiate(groundSmashVFXPrefab, origin, Quaternion.identity);
                Destroy(vfx, 3.5f);
            }

            // 2. Play Sound
            PlaySound(smashAudioClip);

            // 3. Detect objects in AOE radius
            Collider[] hitColliders = Physics.OverlapSphere(origin, smashRadius, targetLayers, QueryTriggerInteraction.Ignore);

            // Track affected rigidbodies to avoid duplicate forces on multi-collider enemies
            HashSet<Rigidbody> affectedBodies = new HashSet<Rigidbody>();

            foreach (Collider col in hitColliders)
            {
                // Ignore self
                if (col.transform.root == transform.root) continue;

                // Check tag on object or parent
                bool isEnemy = col.CompareTag(enemyTag) || (col.attachedRigidbody != null && col.attachedRigidbody.CompareTag(enemyTag));
                if (!isEnemy) continue;

                // A. Apply Damage (Interface or fallback message)
                if (col.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(smashDamage);
                }
                else
                {
                    col.SendMessage("TakeDamage", smashDamage, SendMessageOptions.DontRequireReceiver);
                }

                // B. Apply Physics Explosion Force
                Rigidbody targetRb = col.attachedRigidbody;
                if (targetRb != null && !targetRb.isKinematic && !affectedBodies.Contains(targetRb))
                {
                    affectedBodies.Add(targetRb);
                    targetRb.AddExplosionForce(smashExplosionForce, origin, smashRadius, smashUpwardsModifier, ForceMode.Impulse);
                }
            }
        }
        #endregion

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            // Draw Ground Smash Radius
            Vector3 origin = smashPoint != null ? smashPoint.position : transform.position;
            Gizmos.color = smashGizmoColor;
            Gizmos.DrawSphere(origin, smashRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, smashRadius);

            // Draw Fire Point Forward Direction
            if (firePoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(firePoint.position, firePoint.forward * 3f);
                Gizmos.DrawWireSphere(firePoint.position, 0.15f);
            }
        }
    }
}
