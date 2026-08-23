using UnityEngine;

namespace GuardianSystem.Combat
{
    /// <summary>
    /// Sample enemy target component that responds to GuardianCombat ground smash physics and projectile damage.
    /// Tag this GameObject as "Enemy".
    /// </summary>
    [AddComponentMenu("Guardian System/Enemy Target")]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyTarget : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;

        [Header("Death Effects")]
        [SerializeField] private GameObject deathVFXPrefab;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip deathSound;

        private void Start()
        {
            currentHealth = maxHealth;
            // Ensure tag is Enemy if not set
            if (!gameObject.CompareTag("Enemy"))
            {
                Debug.LogWarning($"[EnemyTarget] GameObject '{name}' is not tagged 'Enemy'. Please set Tag to 'Enemy' in the Inspector.", this);
            }
        }

        public void TakeDamage(int amount)
        {
            currentHealth -= amount;
            
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, transform.position);
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (deathVFXPrefab != null)
            {
                GameObject vfx = Instantiate(deathVFXPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 3f);
            }

            if (deathSound != null)
            {
                AudioSource.PlayClipAtPoint(deathSound, transform.position);
            }

            Destroy(gameObject);
        }
    }
}
