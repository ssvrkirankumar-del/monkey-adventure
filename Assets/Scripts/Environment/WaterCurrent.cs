using UnityEngine;

namespace MonkeyAdventure.Environment
{
    /// <summary>
    /// Act 3: Water Current.
    /// Attached to a trigger collider representing a river volume.
    /// Constantly pushes Rigidbodies and CharacterControllers in a specified flow direction.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Environment/Water Current")]
    [RequireComponent(typeof(Collider))]
    public class WaterCurrent : MonoBehaviour
    {
        [Header("Current Flow Settings")]
        [Tooltip("Direction the current pushes. If useTransformForward is true, uses this object's forward vector.")]
        [SerializeField] private Vector3 flowDirection = Vector3.forward;

        [Tooltip("Use the local forward direction of this transform as the water stream direction.")]
        [SerializeField] private bool useTransformForward = true;

        [Tooltip("Force magnitude applied to Rigidbodies in the current.")]
        [SerializeField] private float currentForce = 25f;

        [Tooltip("Speed modifier applied to CharacterController players in the water.")]
        [SerializeField] private float controllerPushSpeed = 6f;

        [Tooltip("Water drag dampening applied to slow down objects in water.")]
        [SerializeField] private float waterDrag = 1.5f;

        [Header("Gizmo Settings")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color currentGizmoColor = new Color(0f, 0.6f, 1f, 0.4f);

        private Collider _triggerCollider;

        public Vector3 EffectiveFlowDirection => useTransformForward ? transform.forward : flowDirection.normalized;

        private void Awake()
        {
            _triggerCollider = GetComponent<Collider>();
            if (_triggerCollider != null)
            {
                _triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            Vector3 pushDir = EffectiveFlowDirection;

            // 1. Push Rigidbody Objects (e.g. Floating Logs, crates, ragdolls)
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                // Apply flow force
                rb.AddForce(pushDir * currentForce, ForceMode.Acceleration);

                #if UNITY_6000_0_OR_NEWER
                rb.linearDamping = waterDrag;
                #else
                rb.drag = waterDrag;
                #endif
            }

            // 2. Push CharacterController if player uses CharacterController
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null && cc.enabled)
            {
                cc.Move(pushDir * (controllerPushSpeed * Time.deltaTime));
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null)
            {
                #if UNITY_6000_0_OR_NEWER
                rb.linearDamping = 0.05f;
                #else
                rb.drag = 0.05f;
                #endif
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            Gizmos.color = currentGizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);

            // Draw flow direction arrows
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.blue;
            Vector3 center = transform.position;
            Vector3 dir = EffectiveFlowDirection;
            Gizmos.DrawRay(center, dir * 3f);
        }
    }
}
