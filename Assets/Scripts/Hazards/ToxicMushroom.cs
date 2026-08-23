using System.Collections;
using UnityEngine;
using GuardianSystem.Combat;

namespace MonkeyAdventure.Hazards
{
    /// <summary>
    /// Act 4: Toxic Mushroom Hazard.
    /// Detects player proximity, swells up in size, releases toxic spore gas,
    /// and deals damage over time to players within its poison cloud.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Hazards/Toxic Mushroom")]
    [RequireComponent(typeof(Collider))]
    public class ToxicMushroom : MonoBehaviour
    {
        [Header("Detection & Proximity")]
        [Tooltip("Detection radius to trigger swelling and spore release.")]
        [SerializeField] private float detectionRadius = 4.5f;

        [Tooltip("Layer mask or tags identifying targets.")]
        [SerializeField] private string targetTag = "Player";

        [Header("Swelling Animation")]
        [Tooltip("Scale multiplier when fully swollen.")]
        [SerializeField] private Vector3 swollenScaleMultiplier = new Vector3(1.4f, 1.6f, 1.4f);

        [Tooltip("Time taken to swell before popping/releasing gas.")]
        [SerializeField] private float swellDuration = 0.8f;

        [Tooltip("Cooldown period before mushroom shrinks and can trigger again.")]
        [SerializeField] private float cooldownTime = 3.5f;

        [Header("Toxic Gas Spores")]
        [SerializeField] private ParticleSystem toxicGasParticles;
        [SerializeField] private AudioClip swellSound;
        [SerializeField] private AudioClip gasReleaseSound;

        [Header("Damage Settings")]
        [Tooltip("Damage dealt per poison tick.")]
        [SerializeField] private int damagePerTick = 10;

        [Tooltip("Interval in seconds between poison damage ticks.")]
        [SerializeField] private float damageInterval = 0.5f;

        [Tooltip("Duration the gas cloud remains dangerous.")]
        [SerializeField] private float gasCloudDuration = 3.0f;

        private Vector3 _originalScale;
        private bool _isTriggered = false;

        private void Start()
        {
            _originalScale = transform.localScale;
        }

        private void Update()
        {
            if (_isTriggered) return;

            // Check distance to player
            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag(targetTag) || (hit.transform.root != null && hit.transform.root.CompareTag(targetTag)))
                {
                    StartCoroutine(TriggerEruptionRoutine(hit.gameObject));
                    break;
                }
            }
        }

        private IEnumerator TriggerEruptionRoutine(GameObject triggeringPlayer)
        {
            _isTriggered = true;

            // 1. Play Swell Audio
            if (swellSound != null)
            {
                AudioSource.PlayClipAtPoint(swellSound, transform.position);
            }

            // 2. Swell Up Animation
            float elapsed = 0f;
            Vector3 targetScale = Vector3.Scale(_originalScale, swollenScaleMultiplier);

            while (elapsed < swellDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / swellDuration;
                transform.localScale = Vector3.Lerp(_originalScale, targetScale, t);
                yield return null;
            }

            // 3. Erupt & Release Toxic Gas Spores
            if (gasReleaseSound != null)
            {
                AudioSource.PlayClipAtPoint(gasReleaseSound, transform.position);
            }

            if (toxicGasParticles != null)
            {
                toxicGasParticles.Play();
            }

            // 4. Deal Poison Damage over Time
            StartCoroutine(PoisonCloudDamageRoutine());

            // 5. Shrink back down smoothly
            yield return new WaitForSeconds(gasCloudDuration);

            elapsed = 0f;
            while (elapsed < 0.6f)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(targetScale, _originalScale, elapsed / 0.6f);
                yield return null;
            }
            transform.localScale = _originalScale;

            // 6. Cooldown before rearming
            yield return new WaitForSeconds(cooldownTime);
            _isTriggered = false;
        }

        private IEnumerator PoisonCloudDamageRoutine()
        {
            float timer = 0f;
            while (timer < gasCloudDuration)
            {
                // Check if player is within cloud radius
                Collider[] victims = Physics.OverlapSphere(transform.position, detectionRadius * 1.2f);
                foreach (var victim in victims)
                {
                    if (victim.CompareTag(targetTag) || (victim.transform.root != null && victim.transform.root.CompareTag(targetTag)))
                    {
                        if (victim.TryGetComponent<IDamageable>(out var damageable))
                        {
                            damageable.TakeDamage(damagePerTick);
                        }
                        else
                        {
                            victim.SendMessage("TakeDamage", damagePerTick, SendMessageOptions.DontRequireReceiver);
                        }
                    }
                }

                yield return new WaitForSeconds(damageInterval);
                timer += damageInterval;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.2f, 0.3f);
            Gizmos.DrawSphere(transform.position, detectionRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
