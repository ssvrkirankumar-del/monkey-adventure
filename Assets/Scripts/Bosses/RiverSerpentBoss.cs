using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GuardianSystem.Combat;
using MonkeyAdventure.Core;

namespace MonkeyAdventure.Bosses
{
    /// <summary>
    /// Act 3 - Level 30 Boss: The River Serpent.
    /// Operates in a water arena filled with floating logs.
    /// Emerges from random water points, fires water-blast projectiles at the player, and dives back under.
    /// Vulnerable to Guardian attacks while surfaced.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Bosses/River Serpent Boss")]
    public class RiverSerpentBoss : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 300;
        [SerializeField] private int currentHealth;

        [Header("Emerge & Dive Settings")]
        [Tooltip("List of surface spawn points where the serpent can emerge.")]
        [SerializeField] private List<Transform> emergePoints = new List<Transform>();

        [Tooltip("Time spent surfaced before diving back underwater.")]
        [SerializeField] private float surfacedDuration = 4.0f;

        [Tooltip("Time spent submerged underwater before emerging again.")]
        [SerializeField] private float submergedDuration = 2.5f;

        [Tooltip("Height offset when fully emerged above water.")]
        [SerializeField] private float emergedHeightOffset = 4.5f;

        [Header("Water Blast Attack")]
        [SerializeField] private GameObject waterBlastPrefab;
        [SerializeField] private Transform mouthFirePoint;
        [SerializeField] private float projectileSpeed = 18f;
        [SerializeField] private int shotsPerEmerge = 2;
        [SerializeField] private float shotInterval = 1.2f;

        [Header("VFX & Audio")]
        [SerializeField] private GameObject waterSplashVFX;
        [SerializeField] private GameObject defeatVFX;
        [SerializeField] private AudioClip emergeSplashSound;
        [SerializeField] private AudioClip diveSplashSound;
        [SerializeField] private AudioClip shootSound;
        [SerializeField] private AudioClip serpentRoarSound;

        [Header("Unlock Act 4 Gate")]
        [SerializeField] private GameObject act4Gate;

        private bool _isSurfaced = false;
        private bool _isInvulnerable = true;
        private bool _isDead = false;
        private AudioSource _audioSource;
        private Collider _serpentCollider;

        public bool IsSurfaced => _isSurfaced;
        public int CurrentHealth => currentHealth;

        private void Awake()
        {
            currentHealth = maxHealth;
            _serpentCollider = GetComponent<Collider>();
            _audioSource = GetComponent<AudioSource>();

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f;
            }
        }

        private void Start()
        {
            if (mouthFirePoint == null) mouthFirePoint = transform;

            // Start submerged
            SetUnderwaterState();

            // Begin Boss Loop
            StartCoroutine(SerpentBossCycleRoutine());
        }

        private IEnumerator SerpentBossCycleRoutine()
        {
            yield return new WaitForSeconds(1.5f);

            while (!_isDead)
            {
                // 1. Choose random emerge point
                Transform targetPoint = transform;
                if (emergePoints.Count > 0)
                {
                    targetPoint = emergePoints[Random.Range(0, emergePoints.Count)];
                }

                // 2. Emerge Animation
                yield return StartCoroutine(EmergeRoutine(targetPoint));

                // 3. Attack Player with Water Blasts while surfaced
                yield return StartCoroutine(PerformWaterBlastAttackRoutine());

                // 4. Remain surfaced for remaining duration
                yield return new WaitForSeconds(Mathf.Max(0.5f, surfacedDuration - (shotsPerEmerge * shotInterval)));

                // 5. Dive back down
                yield return StartCoroutine(DiveRoutine(targetPoint));

                // 6. Submerged wait period
                yield return new WaitForSeconds(submergedDuration);
            }
        }

        private IEnumerator EmergeRoutine(Transform spawnPoint)
        {
            Vector3 submergedPos = spawnPoint.position - Vector3.up * emergedHeightOffset;
            Vector3 targetPos = spawnPoint.position;

            transform.position = submergedPos;
            FacePlayer();

            // Water Splash VFX & Sound
            PlaySplashEffects(targetPos, emergeSplashSound);

            float elapsed = 0f;
            while (elapsed < 0.8f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(submergedPos, targetPos, elapsed / 0.8f);
                yield return null;
            }

            transform.position = targetPos;
            _isSurfaced = true;
            _isInvulnerable = false;
            if (_serpentCollider != null) _serpentCollider.enabled = true;

            if (serpentRoarSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(serpentRoarSound);
            }

            Debug.Log("[RiverSerpentBoss] Surfaced! VULNERABLE TO ATTACK!", this);
        }

        private IEnumerator PerformWaterBlastAttackRoutine()
        {
            for (int i = 0; i < shotsPerEmerge; i++)
            {
                if (_isDead || !_isSurfaced) yield break;

                yield return new WaitForSeconds(shotInterval);

                FacePlayer();

                if (waterBlastPrefab != null)
                {
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    Vector3 shootDir = player != null ? (player.transform.position + Vector3.up * 0.8f - mouthFirePoint.position).normalized : transform.forward;

                    GameObject blast = Instantiate(waterBlastPrefab, mouthFirePoint.position, Quaternion.LookRotation(shootDir));
                    if (blast.TryGetComponent<Rigidbody>(out var rb))
                    {
                        #if UNITY_6000_0_OR_NEWER
                        rb.linearVelocity = shootDir * projectileSpeed;
                        #else
                        rb.velocity = shootDir * projectileSpeed;
                        #endif
                    }

                    Destroy(blast, 5f);
                }

                if (shootSound != null && _audioSource != null)
                {
                    _audioSource.PlayOneShot(shootSound);
                }
            }
        }

        private IEnumerator DiveRoutine(Transform spawnPoint)
        {
            _isSurfaced = false;
            _isInvulnerable = true;
            if (_serpentCollider != null) _serpentCollider.enabled = false;

            Vector3 startPos = transform.position;
            Vector3 submergedPos = spawnPoint.position - Vector3.up * emergedHeightOffset;

            PlaySplashEffects(startPos, diveSplashSound);

            float elapsed = 0f;
            while (elapsed < 0.7f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, submergedPos, elapsed / 0.7f);
                yield return null;
            }

            transform.position = submergedPos;
            Debug.Log("[RiverSerpentBoss] Submerged underwater. Immune.", this);
        }

        private void SetUnderwaterState()
        {
            _isSurfaced = false;
            _isInvulnerable = true;
            if (_serpentCollider != null) _serpentCollider.enabled = false;
            transform.position -= Vector3.up * emergedHeightOffset;
        }

        private void FacePlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 lookDir = (player.transform.position - transform.position);
                lookDir.y = 0f;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
        }

        private void PlaySplashEffects(Vector3 pos, AudioClip clip)
        {
            if (waterSplashVFX != null)
            {
                GameObject vfx = Instantiate(waterSplashVFX, pos, Quaternion.identity);
                Destroy(vfx, 2.5f);
            }

            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        public void TakeDamage(int amount)
        {
            if (_isInvulnerable || !_isSurfaced || _isDead) return;

            currentHealth -= amount;
            Debug.Log($"[RiverSerpentBoss] Struck for {amount} damage! HP: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0)
            {
                DefeatSerpent();
            }
        }

        private void DefeatSerpent()
        {
            _isDead = true;
            StopAllCoroutines();

            Debug.Log("[RiverSerpentBoss] DEFEATED! Act 4 Path Unlocked!");

            if (defeatVFX != null)
            {
                GameObject vfx = Instantiate(defeatVFX, transform.position + Vector3.up * 1f, Quaternion.identity);
                Destroy(vfx, 4f);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(75);
            }

            if (act4Gate != null)
            {
                act4Gate.SetActive(false);
            }

            Destroy(gameObject, 0.5f);
        }
    }
}
