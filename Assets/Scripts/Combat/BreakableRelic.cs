using UnityEngine;
using GuardianSystem.Combat;
using MonkeyAdventure.Core;

namespace MonkeyAdventure.Combat
{
    /// <summary>
    /// Act 5: Breakable Relic.
    /// Ancient magical relic that requires the Guardian Ground Smash (or high-power attack) to shatter.
    /// When broken, releases celestial energy particles and grants massive progression rewards.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Combat/Breakable Relic")]
    [RequireComponent(typeof(Collider))]
    public class BreakableRelic : MonoBehaviour, IDamageable
    {
        [Header("Durability & Resistance")]
        [Tooltip("Health points of the relic.")]
        [SerializeField] private int relicHealth = 50;

        [Tooltip("Minimum single-hit damage required to damage this relic (e.g. 40+ to require Ground Smash).")]
        [SerializeField] private int minimumHitDamage = 40;

        [Tooltip("If true, requires Ground Smash damage threshold to crack open.")]
        [SerializeField] private bool requireHeavySmash = true;

        [Header("Rewards on Shatter")]
        [SerializeField] private int bonusFood = 10;
        [SerializeField] private int bonusCoins = 50;
        [SerializeField] private GameObject energyEssencePrefab;

        [Header("Visuals, VFX & Audio")]
        [SerializeField] private GameObject shatterVFXPrefab;
        [SerializeField] private AudioClip heavyHitSound;
        [SerializeField] private AudioClip shatterSound;
        [SerializeField] private float vfxLifetime = 3.5f;

        private bool _isBroken = false;

        private void Start()
        {
            // Ensure tagged appropriately for detection
            if (!gameObject.CompareTag("Enemy") && !gameObject.CompareTag("Untagged"))
            {
                // Can optionally tag as Enemy so Ground Smash overlap sphere targets it automatically
            }
        }

        public void TakeDamage(int amount)
        {
            if (_isBroken) return;

            // Check if damage meets the heavy smash requirement
            if (requireHeavySmash && amount < minimumHitDamage)
            {
                Debug.Log($"[BreakableRelic] Hit deflected! Damage {amount} is too weak. Use Guardian Ground Smash!", this);
                if (heavyHitSound != null)
                {
                    AudioSource.PlayClipAtPoint(heavyHitSound, transform.position);
                }
                return;
            }

            relicHealth -= amount;

            if (heavyHitSound != null)
            {
                AudioSource.PlayClipAtPoint(heavyHitSound, transform.position);
            }

            if (relicHealth <= 0)
            {
                ShatterRelic();
            }
        }

        private void ShatterRelic()
        {
            if (_isBroken) return;
            _isBroken = true;

            // 1. Grant Rewards to GameManager
            if (GameManager.Instance != null)
            {
                if (bonusFood > 0) GameManager.Instance.AddFood(bonusFood);
                if (bonusCoins > 0) GameManager.Instance.AddCoins(bonusCoins);
            }

            // 2. Spawn Shatter Particles & Essence
            if (shatterVFXPrefab != null)
            {
                GameObject vfx = Instantiate(shatterVFXPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, vfxLifetime);
            }

            if (energyEssencePrefab != null)
            {
                Instantiate(energyEssencePrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            }

            // 3. Play Shatter Sound
            if (shatterSound != null)
            {
                AudioSource.PlayClipAtPoint(shatterSound, transform.position);
            }

            Debug.Log($"[BreakableRelic] '{name}' SHATTERED by Guardian Power! Granted {bonusFood} Food & {bonusCoins} Coins.", this);

            // 4. Destroy Relic Shell
            Destroy(gameObject);
        }
    }
}
