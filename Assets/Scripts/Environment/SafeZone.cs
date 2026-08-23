using UnityEngine;
using MonkeyAdventure.Hazards;

namespace MonkeyAdventure.Environment
{
    /// <summary>
    /// Act 4: Safe Zone / Bioluminescent Crystal.
    /// Recharges the Player's LightAura to maximum when inside the crystal's radius.
    /// Provides sanctuary from the Dark Forest hazards.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Environment/Safe Zone Crystal")]
    [RequireComponent(typeof(Collider))]
    public class SafeZone : MonoBehaviour
    {
        [Header("Recharge Settings")]
        [Tooltip("Instant recharge on entry, or continuous refill per second.")]
        [SerializeField] private bool instantFullRecharge = true;

        [Tooltip("Continuous refill rate per second if instant is disabled.")]
        [SerializeField] private float refillRate = 40f;

        [Header("Visual & Audio")]
        [SerializeField] private Light crystalLight;
        [SerializeField] private ParticleSystem auraMoteParticles;
        [SerializeField] private AudioClip rechargeSound;

        private Collider _triggerCollider;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider>();
            if (_triggerCollider != null)
            {
                _triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<LightAura>(out var lightAura) ||
                (other.transform.root != null && other.transform.root.TryGetComponent<LightAura>(out lightAura)))
            {
                if (instantFullRecharge)
                {
                    lightAura.RechargeToMax();
                }

                if (rechargeSound != null)
                {
                    AudioSource.PlayClipAtPoint(rechargeSound, transform.position);
                }

                if (auraMoteParticles != null)
                {
                    auraMoteParticles.Play();
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.TryGetComponent<LightAura>(out var lightAura) ||
                (other.transform.root != null && other.transform.root.TryGetComponent<LightAura>(out lightAura)))
            {
                lightAura.NotifyInSafeZone(refillRate * Time.deltaTime);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<LightAura>(out var lightAura) ||
                (other.transform.root != null && other.transform.root.TryGetComponent<LightAura>(out lightAura)))
            {
                lightAura.NotifyExitedSafeZone();
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, 2.5f);
        }
    }
}
