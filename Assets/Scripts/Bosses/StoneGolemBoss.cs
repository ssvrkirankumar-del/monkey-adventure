using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GuardianSystem.Combat;
using MonkeyAdventure.Puzzles;
using MonkeyAdventure.Core;

namespace MonkeyAdventure.Bosses
{
    /// <summary>
    /// Act 2 - Level 20 Boss: Ancient Stone Golem.
    /// Sits in the center and throws rolling boulders at the player.
    /// Immune to regular attacks. Defeated by solving 3 Rune Switches to trigger falling stone pillars (3 hits to defeat).
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Bosses/Stone Golem Boss")]
    public class StoneGolemBoss : MonoBehaviour, IDamageable
    {
        [Header("Boss Segments Health")]
        [Tooltip("Number of pillar strikes required to defeat the Golem (Default 3).")]
        [SerializeField] private int maxHealthSegments = 3;
        [SerializeField] private int currentHealthSegments;

        [Header("Boulder Throw Attack")]
        [SerializeField] private GameObject boulderPrefab;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private float throwInterval = 3.2f;
        [SerializeField] private float boulderThrowForce = 22f;

        [Header("Rune Switches & Falling Pillar Mechanism")]
        [Tooltip("The 3 Rune Switches surrounding the arena.")]
        [SerializeField] private List<RuneSwitch> arenaRuneSwitches = new List<RuneSwitch>();

        [Tooltip("The falling stone pillar that crushes the Golem.")]
        [SerializeField] private GameObject fallingPillar;
        [SerializeField] private Transform pillarStartPoint;
        [SerializeField] private Transform pillarImpactPoint;

        [Header("VFX & Audio")]
        [SerializeField] private GameObject pillarCrushVFX;
        [SerializeField] private GameObject golemDeathVFX;
        [SerializeField] private AudioClip boulderThrowSound;
        [SerializeField] private AudioClip pillarCrashSound;
        [SerializeField] private AudioClip golemRoarSound;

        [Header("Unlock Path")]
        [SerializeField] private GameObject act3PathGate;

        private float _nextThrowTime;
        private bool _isCrushed = false;
        private bool _isDead = false;
        private AudioSource _audioSource;

        public int CurrentHealthSegments => currentHealthSegments;

        private void Awake()
        {
            currentHealthSegments = maxHealthSegments;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f;
            }
        }

        private void OnEnable()
        {
            foreach (var rune in arenaRuneSwitches)
            {
                if (rune != null)
                {
                    rune.OnSwitchStateChanged += HandleRuneStateChanged;
                }
            }
        }

        private void OnDisable()
        {
            foreach (var rune in arenaRuneSwitches)
            {
                if (rune != null)
                {
                    rune.OnSwitchStateChanged -= HandleRuneStateChanged;
                }
            }
        }

        private void Start()
        {
            _nextThrowTime = Time.time + 2.0f;
            if (throwPoint == null) throwPoint = transform;
        }

        private void Update()
        {
            if (_isDead || _isCrushed) return;

            // Continuously throw rolling boulders towards player
            if (Time.time >= _nextThrowTime)
            {
                ThrowBoulderAtPlayer();
            }
        }

        private void ThrowBoulderAtPlayer()
        {
            _nextThrowTime = Time.time + throwInterval;

            if (boulderPrefab == null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Vector3 throwDir = player != null ? (player.transform.position - throwPoint.position).normalized : transform.forward;
            throwDir.y = 0.2f; // Slight arc

            // Spawn Boulder
            GameObject boulder = Instantiate(boulderPrefab, throwPoint.position, Quaternion.identity);
            if (boulder.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.AddForce(throwDir * boulderThrowForce, ForceMode.Impulse);
                rb.AddTorque(transform.right * 15f, ForceMode.Impulse);
            }

            Destroy(boulder, 7f);

            if (boulderThrowSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(boulderThrowSound);
            }
        }

        private void HandleRuneStateChanged(RuneSwitch sw)
        {
            if (_isCrushed || _isDead) return;

            // Check if all 3 switches are active
            bool allActive = true;
            foreach (var rune in arenaRuneSwitches)
            {
                if (rune == null || !rune.IsActivated)
                {
                    allActive = false;
                    break;
                }
            }

            if (allActive)
            {
                StartCoroutine(TriggerPillarDropRoutine());
            }
        }

        private IEnumerator TriggerPillarDropRoutine()
        {
            _isCrushed = true;

            Debug.Log("[StoneGolemBoss] ALL 3 RUNES ACTIVE! Dropping Stone Pillar...", this);

            // 1. Drop Pillar Animation / Lerp
            if (fallingPillar != null)
            {
                Vector3 startPos = pillarStartPoint != null ? pillarStartPoint.position : fallingPillar.transform.position + Vector3.up * 10f;
                Vector3 targetPos = pillarImpactPoint != null ? pillarImpactPoint.position : transform.position + Vector3.up * 1.5f;

                fallingPillar.transform.position = startPos;
                fallingPillar.SetActive(true);

                float elapsed = 0f;
                while (elapsed < 0.6f)
                {
                    elapsed += Time.deltaTime;
                    fallingPillar.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / 0.6f);
                    yield return null;
                }
            }

            // 2. Impact VFX & SFX
            if (pillarCrushVFX != null)
            {
                GameObject vfx = Instantiate(pillarCrushVFX, transform.position + Vector3.up * 1f, Quaternion.identity);
                Destroy(vfx, 3f);
            }

            if (pillarCrashSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(pillarCrashSound);
            }

            // 3. Deduct 1 Health Segment
            currentHealthSegments--;
            Debug.Log($"[StoneGolemBoss] Crushed! Segments remaining: {currentHealthSegments}/{maxHealthSegments}");

            yield return new WaitForSeconds(1.5f);

            // 4. Check if boss is dead
            if (currentHealthSegments <= 0)
            {
                DefeatGolem();
            }
            else
            {
                // 5. Reset Arena Switches and Pillar for next round
                ResetArenaSwitches();
                _isCrushed = false;
            }
        }

        private void ResetArenaSwitches()
        {
            foreach (var rune in arenaRuneSwitches)
            {
                if (rune != null)
                {
                    // Deactivate switch
                    rune.SendMessage("ResetSwitch", SendMessageOptions.DontRequireReceiver);
                }
            }

            if (fallingPillar != null && pillarStartPoint != null)
            {
                fallingPillar.transform.position = pillarStartPoint.position;
            }
        }

        public void TakeDamage(int amount)
        {
            // Immune to direct player attacks
            Debug.Log("[StoneGolemBoss] IMMUNE! Player attacks bounce off ancient stone armor. Solve the Rune Switches!", this);
        }

        private void DefeatGolem()
        {
            _isDead = true;
            Debug.Log("[StoneGolemBoss] DEFEATED! Act 3 Path Unlocked!");

            if (golemDeathVFX != null)
            {
                GameObject vfx = Instantiate(golemDeathVFX, transform.position + Vector3.up * 1f, Quaternion.identity);
                Destroy(vfx, 4f);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoins(100);
            }

            if (act3PathGate != null)
            {
                act3PathGate.SetActive(false);
            }

            Destroy(gameObject, 0.5f);
        }
    }
}
