using UnityEngine;
using MonkeyAdventure.Core;

namespace MonkeyAdventure.Environment
{
    /// <summary>
    /// Checkpoint component for Act 1: The Awakening.
    /// When triggered by the player, updates the GameManager respawn coordinates,
    /// plays activation particles, sound, and toggles visual active states.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Environment/Checkpoint")]
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [Header("Spawn Transform")]
        [Tooltip("Exact position and rotation where the player will respawn. Defaults to this transform.")]
        [SerializeField] private Transform respawnPoint;

        [Header("Activation FX & Audio")]
        [Tooltip("One-shot burst particle effect triggered when the checkpoint is first activated.")]
        [SerializeField] private GameObject activationBurstVFXPrefab;

        [Tooltip("Continuous/looping particle or light visual enabled while checkpoint is active.")]
        [SerializeField] private GameObject activeStateVisual;

        [Tooltip("Sound clip played upon activation.")]
        [SerializeField] private AudioClip activationSound;

        [Tooltip("Duration of the one-shot burst particle effect.")]
        [SerializeField] private float burstVFXDuration = 2.5f;

        [Header("State")]
        [Tooltip("Whether this checkpoint is currently activated.")]
        [SerializeField] private bool isActivated = false;

        [Header("Gizmo Settings")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color activeGizmoColor = Color.green;
        [SerializeField] private Color inactiveGizmoColor = Color.yellow;

        private Collider _collider;

        public bool IsActivated => isActivated;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }

            if (respawnPoint == null)
            {
                respawnPoint = transform;
            }

            // Sync visual state at launch
            if (activeStateVisual != null)
            {
                activeStateVisual.SetActive(isActivated);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isActivated) return;

            // Detect Player
            if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
            {
                ActivateCheckpoint();
            }
        }

        /// <summary>
        /// Activates the checkpoint and syncs coordinates with GameManager.
        /// </summary>
        public void ActivateCheckpoint()
        {
            if (isActivated) return;

            isActivated = true;

            Vector3 spawnPos = respawnPoint != null ? respawnPoint.position : transform.position;
            Quaternion spawnRot = respawnPoint != null ? respawnPoint.rotation : transform.rotation;

            // 1. Notify GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCheckpoint(spawnPos, spawnRot);
            }
            else
            {
                Debug.LogWarning("[Checkpoint] GameManager.Instance not found in scene!", this);
            }

            // 2. Spawn Activation VFX
            if (activationBurstVFXPrefab != null)
            {
                GameObject vfx = Instantiate(activationBurstVFXPrefab, spawnPos + Vector3.up * 0.5f, Quaternion.identity);
                Destroy(vfx, burstVFXDuration);
            }

            // 3. Enable Persistent Active Visual
            if (activeStateVisual != null)
            {
                activeStateVisual.SetActive(true);
            }

            // 4. Play Audio
            if (activationSound != null)
            {
                AudioSource.PlayClipAtPoint(activationSound, spawnPos);
            }

            Debug.Log($"[Checkpoint] Activated at {spawnPos}");
        }

        /// <summary>
        /// Deactivates this checkpoint (e.g. if another checkpoint becomes the only active one).
        /// </summary>
        public void DeactivateCheckpoint()
        {
            isActivated = false;
            if (activeStateVisual != null)
            {
                activeStateVisual.SetActive(false);
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position;
            Gizmos.color = isActivated ? activeGizmoColor : inactiveGizmoColor;
            Gizmos.DrawWireSphere(pos, 0.6f);
            Gizmos.DrawRay(pos, (respawnPoint != null ? respawnPoint.forward : transform.forward) * 1.5f);
        }
    }
}
