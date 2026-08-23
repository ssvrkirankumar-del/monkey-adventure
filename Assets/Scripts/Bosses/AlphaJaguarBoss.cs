using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using GuardianSystem.Combat;
using MonkeyAdventure.AI;
using MonkeyAdventure.Core;

namespace MonkeyAdventure.Bosses
{
    /// <summary>
    /// Act 1 - Level 10 Boss: The Alpha Jaguar.
    /// Paces back and forth in the arena, performs high-speed charges every 5 seconds.
    /// If it hits an arena wall during a charge, it becomes Stunned for 3 seconds and takes 2x damage.
    /// On defeat: drops 50 coins and unlocks the Act 2 Gate.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Bosses/Alpha Jaguar Boss")]
    public class AlphaJaguarBoss : EnemyAI
    {
        [Header("Boss Specific Health")]
        [SerializeField] private int bossMaxHealth = 250;
        [SerializeField] private int bossCurrentHealth;

        [Header("Charge Attack Settings")]
        [Tooltip("Interval between charge attacks in seconds.")]
        [SerializeField] private float chargeCooldown = 5.0f;

        [Tooltip("Movement speed during the charge.")]
        [SerializeField] private float chargeSpeed = 16.0f;

        [Tooltip("Damage inflicted if charge hits player.")]
        [SerializeField] private int chargeDamage = 35;

        [Tooltip("Duration of stun when hitting an arena wall.")]
        [SerializeField] private float stunDuration = 3.0f;

        [Tooltip("Damage multiplier while stunned.")]
        [SerializeField] private float stunnedDamageMultiplier = 2.0f;

        [Header("VFX & Audio")]
        [SerializeField] private GameObject stunStarsVFX;
        [SerializeField] private GameObject wallImpactVFX;
        [SerializeField] private GameObject bossDefeatVFX;
        [SerializeField] private AudioClip chargeRoarSound;
        [SerializeField] private AudioClip wallCrashSound;
        [SerializeField] private AudioClip stunGroanSound;

        [Header("Victory & Gate Unlock")]
        [Tooltip("Gate leading to Act 2 (The Lost Forest).")]
        [SerializeField] private GameObject act2Gate;

        [Tooltip("Number of bonus coins awarded upon defeat.")]
        [SerializeField] private int rewardCoins = 50;

        [Tooltip("Tag on arena boundaries that trigger stun upon impact.")]
        [SerializeField] private string wallTag = "Wall";

        // Internal Boss State
        private bool _isCharging = false;
        private bool _isStunned = false;
        private float _nextChargeTime;
        private Vector3 _chargeDirection;
        private NavMeshAgent _navAgent;
        private AudioSource _bossAudioSource;
        private Rigidbody _rigidbody;

        public bool IsStunned => _isStunned;
        public bool IsCharging => _isCharging;

        private void Awake()
        {
            bossCurrentHealth = bossMaxHealth;
            _navAgent = GetComponent<NavMeshAgent>();
            _rigidbody = GetComponent<Rigidbody>();

            _bossAudioSource = GetComponent<AudioSource>();
            if (_bossAudioSource == null)
            {
                _bossAudioSource = gameObject.AddComponent<AudioSource>();
                _bossAudioSource.spatialBlend = 1f;
                _bossAudioSource.playOnAwake = false;
            }

            if (stunStarsVFX != null)
            {
                stunStarsVFX.SetActive(false);
            }
        }

        private void Start()
        {
            _nextChargeTime = Time.time + chargeCooldown;
        }

        private void Update()
        {
            if (_isStunned) return;

            // Handle Charge Timer
            if (!_isCharging && Time.time >= _nextChargeTime)
            {
                StartCoroutine(ExecuteChargeRoutine());
            }

            // Charge forward translation if currently in charge state
            if (_isCharging)
            {
                transform.position += _chargeDirection * (chargeSpeed * Time.deltaTime);
            }
        }

        private IEnumerator ExecuteChargeRoutine()
        {
            _isCharging = true;

            // 1. Disable NavMeshAgent during straight-line charge
            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.isStopped = true;
                _navAgent.enabled = false;
            }

            // 2. Aim at player position
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 targetDir = (player.transform.position - transform.position);
                targetDir.y = 0f;
                _chargeDirection = targetDir.normalized;
                transform.forward = _chargeDirection;
            }
            else
            {
                _chargeDirection = transform.forward;
            }

            // 3. Play Charge Roar
            if (chargeRoarSound != null && _bossAudioSource != null)
            {
                _bossAudioSource.PlayOneShot(chargeRoarSound);
            }

            // Charge for a maximum of 2.5 seconds or until hitting a wall
            float chargeTimer = 0f;
            while (_isCharging && chargeTimer < 2.5f)
            {
                chargeTimer += Time.deltaTime;
                yield return null;
            }

            // If charge didn't hit a wall, resume normal pacing
            if (_isCharging)
            {
                EndChargeWithoutStun();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_isCharging) return;

            // 1. Check if hit Arena Wall -> STUN!
            if (collision.gameObject.CompareTag(wallTag) || collision.gameObject.name.ToLower().Contains("wall"))
            {
                StartCoroutine(StunRoutine(collision.contacts[0].point));
            }
            // 2. Hit Player -> Deal heavy damage
            else if (collision.gameObject.CompareTag("Player"))
            {
                if (collision.gameObject.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(chargeDamage);
                }
                EndChargeWithoutStun();
            }
        }

        private IEnumerator StunRoutine(Vector3 crashPoint)
        {
            _isCharging = false;
            _isStunned = true;

            // 1. Play Wall Crash VFX & Audio
            if (wallImpactVFX != null)
            {
                GameObject vfx = Instantiate(wallImpactVFX, crashPoint, Quaternion.identity);
                Destroy(vfx, 2.5f);
            }

            if (wallCrashSound != null && _bossAudioSource != null)
            {
                _bossAudioSource.PlayOneShot(wallCrashSound);
            }

            // 2. Enable Stun Stars
            if (stunStarsVFX != null)
            {
                stunStarsVFX.SetActive(true);
            }

            if (stunGroanSound != null && _bossAudioSource != null)
            {
                _bossAudioSource.PlayOneShot(stunGroanSound);
            }

            Debug.Log("[AlphaJaguarBoss] STUNNED! Vulnerable to 2X damage!", this);

            // 3. Wait 3 seconds
            yield return new WaitForSeconds(stunDuration);

            // 4. Recover from stun
            if (stunStarsVFX != null)
            {
                stunStarsVFX.SetActive(false);
            }

            _isStunned = false;
            _nextChargeTime = Time.time + chargeCooldown;

            if (_navAgent != null)
            {
                _navAgent.enabled = true;
                if (_navAgent.isOnNavMesh) _navAgent.isStopped = false;
            }
        }

        private void EndChargeWithoutStun()
        {
            _isCharging = false;
            _nextChargeTime = Time.time + chargeCooldown;

            if (_navAgent != null)
            {
                _navAgent.enabled = true;
                if (_navAgent.isOnNavMesh) _navAgent.isStopped = false;
            }
        }

        #region Damage & Defeat
        public new void TakeDamage(int amount)
        {
            // Apply 2X Damage multiplier if stunned
            int finalDamage = _isStunned ? Mathf.RoundToInt(amount * stunnedDamageMultiplier) : amount;
            bossCurrentHealth -= finalDamage;

            Debug.Log($"[AlphaJaguarBoss] Took {finalDamage} damage! Health: {bossCurrentHealth}/{bossMaxHealth}");

            if (bossCurrentHealth <= 0)
            {
                DefeatBoss();
            }
        }

        private void DefeatBoss()
        {
            Debug.Log("[AlphaJaguarBoss] DEFEATED! Unlocking Act 2 Gate...", this);

            // 1. Spawn Massive Death VFX
            if (bossDefeatVFX != null)
            {
                GameObject vfx = Instantiate(bossDefeatVFX, transform.position + Vector3.up * 1f, Quaternion.identity);
                Destroy(vfx, 4f);
            }

            // 2. Grant 50 Coins
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(rewardCoins);
            }

            // 3. Unlock Act 2 Gate
            if (act2Gate != null)
            {
                act2Gate.SetActive(false); // Or trigger open animation
                Debug.Log("[AlphaJaguarBoss] Act 2 Gate Unlocked!");
            }

            Destroy(gameObject, 0.2f);
        }
        #endregion
    }
}
