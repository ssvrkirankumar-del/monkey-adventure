using System.Collections;
using UnityEngine;
using GuardianSystem.Combat;

namespace MonkeyAdventure.Hazards
{
    /// <summary>
    /// Fire Hazard for the Burning Forest Act.
    /// Deals constant fire burn damage over time to the player, ignites foliage,
    /// and can be extinguished by Water Buff attacks or Extinguishers.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Hazards/Fire Hazard")]
    [RequireComponent(typeof(Collider))]
    public class FireHazard : MonoBehaviour
    {
        [Header("Damage Settings")]
        [Tooltip("Damage dealt per burn interval.")]
        [SerializeField] private int burnDamagePerTick = 12;

        [Tooltip("Interval between damage ticks in seconds.")]
        [SerializeField] private float burnInterval = 0.5f;

        [Header("Extinguish VFX & Audio")]
        [SerializeField] private GameObject steamExtinguishVFX;
        [SerializeField] private AudioClip extinguishHissSound;
        [SerializeField] private ParticleSystem fireParticleSystem;

        private Collider _collider;
        private bool _isExtinguished = false;
        private Coroutine _burnCoroutine;

        public bool IsExtinguished => _isExtinguished;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }

            if (fireParticleSystem == null)
            {
                fireParticleSystem = GetComponentInChildren<ParticleSystem>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isExtinguished) return;

            if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
            {
                if (_burnCoroutine == null)
                {
                    _burnCoroutine = StartCoroutine(BurnDamageRoutine(other.gameObject));
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
            {
                if (_burnCoroutine != null)
                {
                    StopCoroutine(_burnCoroutine);
                    _burnCoroutine = null;
                }
            }
        }

        private IEnumerator BurnDamageRoutine(GameObject playerObj)
        {
            while (!_isExtinguished)
            {
                if (playerObj.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(burnDamagePerTick);
                }
                else
                {
                    playerObj.SendMessage("TakeDamage", burnDamagePerTick, SendMessageOptions.DontRequireReceiver);
                }

                yield return new WaitForSeconds(burnInterval);
            }
        }

        /// <summary>
        /// Extinguishes the fire with steam VFX and disables the damage collider.
        /// </summary>
        public void ExtinguishFire()
        {
            if (_isExtinguished) return;
            _isExtinguished = true;

            if (_burnCoroutine != null)
            {
                StopCoroutine(_burnCoroutine);
                _burnCoroutine = null;
            }

            // 1. Stop Fire Particle
            if (fireParticleSystem != null)
            {
                fireParticleSystem.Stop();
            }

            // 2. Disable Trigger Collider
            if (_collider != null)
            {
                _collider.enabled = false;
            }

            // 3. Spawn Steam VFX & Sound
            if (steamExtinguishVFX != null)
            {
                GameObject vfx = Instantiate(steamExtinguishVFX, transform.position, Quaternion.identity);
                Destroy(vfx, 3f);
            }

            if (extinguishHissSound != null)
            {
                AudioSource.PlayClipAtPoint(extinguishHissSound, transform.position);
            }

            Debug.Log($"[FireHazard] '{name}' EXTINGUISHED by Water!", this);

            // Destroy object after smoke dissipates
            Destroy(gameObject, 1.5f);
        }
    }
}
