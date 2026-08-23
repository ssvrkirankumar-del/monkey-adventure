using UnityEngine;

namespace MonkeyAdventure.Environment
{
    /// <summary>
    /// Act 5: Magic Updraft.
    /// Vertical wind column / jump pad. Applies strong continuous upward velocity to lift the player
    /// high into the sky to reach floating islands.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Environment/Magic Updraft")]
    [RequireComponent(typeof(Collider))]
    public class MagicUpdraft : MonoBehaviour
    {
        [Header("Updraft Physics")]
        [Tooltip("Target upward velocity applied to players inside the updraft.")]
        [SerializeField] private float upwardVelocity = 22f;

        [Tooltip("Continuous upward acceleration force for Rigidbodies.")]
        [SerializeField] private float upwardForce = 45f;

        [Tooltip("If true, overrides vertical velocity directly for snappy, consistent jump height.")]
        [SerializeField] private bool overrideVerticalVelocity = true;

        [Header("Visual & Sound FX")]
        [SerializeField] private ParticleSystem windColumnVFX;
        [SerializeField] private AudioClip updraftLaunchSound;

        [Header("Gizmo Settings")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color updraftColor = new Color(0.2f, 1f, 0.8f, 0.35f);

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
            if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
            {
                if (updraftLaunchSound != null)
                {
                    AudioSource.PlayClipAtPoint(updraftLaunchSound, transform.position);
                }

                if (windColumnVFX != null)
                {
                    windColumnVFX.Play();
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // 1. Lift CharacterController Players
            if (other.TryGetComponent<CharacterController>(out var cc) ||
               (other.transform.root != null && other.transform.root.TryGetComponent<CharacterController>(out cc)))
            {
                if (cc.enabled)
                {
                    cc.Move(Vector3.up * (upwardVelocity * Time.deltaTime));
                }
            }

            // 2. Lift Rigidbody Players or Objects
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                if (overrideVerticalVelocity)
                {
                    #if UNITY_6000_0_OR_NEWER
                    Vector3 currentVel = rb.linearVelocity;
                    rb.linearVelocity = new Vector3(currentVel.x, Mathf.Max(currentVel.y, upwardVelocity), currentVel.z);
                    #else
                    Vector3 currentVel = rb.velocity;
                    rb.velocity = new Vector3(currentVel.x, Mathf.Max(currentVel.y, upwardVelocity), currentVel.z);
                    #endif
                }
                else
                {
                    rb.AddForce(Vector3.up * upwardForce, ForceMode.Acceleration);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            Gizmos.color = updraftColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);

            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, Vector3.up * 8f);
        }
    }
}
