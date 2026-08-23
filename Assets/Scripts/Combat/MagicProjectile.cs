using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuardianSystem.Combat
{
    /// <summary>
    /// MagicProjectile handles continuous forward movement, collision detection with tagged enemies,
    /// damage infliction, impact VFX spawning, and auto-destruction after 3 seconds.
    /// </summary>
    [AddComponentMenu("Guardian System/Magic Projectile")]
    [RequireComponent(typeof(Collider))]
    public class MagicProjectile : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Forward velocity speed of the projectile.")]
        [SerializeField] private float speed = 30f;

        [Tooltip("Auto-destruct timer to prevent orphaned objects in memory.")]
        [SerializeField] private float lifetime = 3.0f;

        [Tooltip("If true, updates Rigidbody linearVelocity. If false or no Rigidbody, moves via Transform.")]
        [SerializeField] private bool usePhysicsMovement = true;

        [Header("Combat & Damage")]
        [Tooltip("Tag identifying valid targets.")]
        [SerializeField] private string targetTag = "Enemy";

        [Tooltip("Damage dealt to the target on impact.")]
        [SerializeField] private int damage = 35;

        [Tooltip("If true and the enemy has no health script, instantly destroys the enemy object.")]
        [SerializeField] private bool destroyEnemyOnHitIfNoHealth = true;

        [Tooltip("Impact pushback force applied to the enemy's Rigidbody on hit.")]
        [SerializeField] private float impactKnockback = 300f;

        [Header("Impact VFX & Audio")]
        [Tooltip("Particle effect prefab spawned upon impact.")]
        [SerializeField] private GameObject impactVFXPrefab;

        [Tooltip("Optional sound played on impact.")]
        [SerializeField] private AudioClip impactSound;

        [Tooltip("Lifetime of the spawned impact VFX in seconds.")]
        [SerializeField] private float impactVFXDuration = 2.0f;

        [Header("Ignore Layers/Tags")]
        [Tooltip("Layer mask specifying what this projectile can collide with.")]
        [SerializeField] private LayerMask collisionMask = ~0;

        private Rigidbody _rigidbody;
        private bool _hasImpacted = false;
        private Vector3 _moveDirection = Vector3.forward;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                _rigidbody.useGravity = false;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }

        private void Start()
        {
            // Auto-destroy after lifetime (3 seconds default)
            Destroy(gameObject, lifetime);

            // Initialize forward direction
            _moveDirection = transform.forward;

            if (_rigidbody != null && usePhysicsMovement)
            {
                #if UNITY_6000_0_OR_NEWER
                _rigidbody.linearVelocity = _moveDirection * speed;
                #else
                _rigidbody.velocity = _moveDirection * speed;
                #endif
            }
        }

        private void Update()
        {
            // Fallback kinematic translation if Rigidbody is absent or non-physics mode is selected
            if (_rigidbody == null || !usePhysicsMovement)
            {
                transform.position += _moveDirection * (speed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Allows external initiators (e.g. GuardianCombat) to customize direction and speed dynamically.
        /// </summary>
        /// <param name="direction">World direction vector</param>
        /// <param name="customSpeed">Speed override</param>
        public void Launch(Vector3 direction, float customSpeed)
        {
            speed = customSpeed;
            _moveDirection = direction.normalized;
            transform.forward = _moveDirection;

            if (_rigidbody != null && usePhysicsMovement)
            {
                #if UNITY_6000_0_OR_NEWER
                _rigidbody.linearVelocity = _moveDirection * speed;
                #else
                _rigidbody.velocity = _moveDirection * speed;
                #endif
            }
        }

        #region Collision Handling
        private void OnTriggerEnter(Collider other)
        {
            HandleHit(other.gameObject, transform.position, -transform.forward);
        }

        private void OnCollisionEnter(Collision collision)
        {
            Vector3 contactPoint = collision.contactCount > 0 ? collision.contacts[0].point : transform.position;
            Vector3 contactNormal = collision.contactCount > 0 ? collision.contacts[0].normal : -transform.forward;
            HandleHit(collision.gameObject, contactPoint, contactNormal);
        }

        private void HandleHit(GameObject hitObj, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (_hasImpacted) return;

            // Check if object is in collision mask
            if (((1 << hitObj.layer) & collisionMask) == 0) return;

            // Check if object is an Enemy
            bool isEnemy = hitObj.CompareTag(targetTag) || 
                           (hitObj.transform.root != null && hitObj.transform.root.CompareTag(targetTag));

            if (isEnemy)
            {
                _hasImpacted = true;

                // 1. Try dealing damage via interface
                if (hitObj.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                }
                // 2. Try dealing damage via root interface
                else if (hitObj.transform.root.TryGetComponent<IDamageable>(out var rootDamageable))
                {
                    rootDamageable.TakeDamage(damage);
                }
                // 3. Fallback message system
                else
                {
                    hitObj.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);

                    // If configured to destroy enemy on hit when no health script is present
                    if (destroyEnemyOnHitIfNoHealth)
                    {
                        GameObject enemyToDestroy = hitObj.transform.root != null ? hitObj.transform.root.gameObject : hitObj;
                        Destroy(enemyToDestroy);
                    }
                }

                // 4. Apply knockback force if target has Rigidbody
                if (hitObj.TryGetComponent<Rigidbody>(out var enemyRb) || 
                    (hitObj.transform.root != null && hitObj.transform.root.TryGetComponent<Rigidbody>(out enemyRb)))
                {
                    if (!enemyRb.isKinematic)
                    {
                        enemyRb.AddForce(_moveDirection * impactKnockback, ForceMode.Impulse);
                    }
                }

                TriggerImpactAndDestroy(hitPoint, hitNormal);
            }
            else
            {
                // Optional: Hit a wall or obstacle
                if (!hitObj.CompareTag("Player") && !hitObj.CompareTag("Untagged") || hitObj.layer == LayerMask.NameToLayer("Default"))
                {
                    // Check if not triggering on player
                    if (!hitObj.CompareTag("Player"))
                    {
                        _hasImpacted = true;
                        TriggerImpactAndDestroy(hitPoint, hitNormal);
                    }
                }
            }
        }

        private void TriggerImpactAndDestroy(Vector3 point, Vector3 normal)
        {
            // Spawn Impact VFX
            if (impactVFXPrefab != null)
            {
                Quaternion rot = normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity;
                GameObject vfx = Instantiate(impactVFXPrefab, point, rot);
                Destroy(vfx, impactVFXDuration);
            }

            // Play Audio
            if (impactSound != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, point);
            }

            // Destroy Projectile
            Destroy(gameObject);
        }
        #endregion
    }
}
