using UnityEngine;
using GuardianSystem.Combat;
using MonkeyAdventure.Puzzles;

namespace MonkeyAdventure.UI
{
    /// <summary>
    /// Connects Unity Canvas UI Buttons directly to player actions for Mobile Android/iOS APK builds.
    /// Provides OnClick listener hooks: TriggerMagic(), TriggerGroundSmash(), TriggerJump(), and TriggerRescue().
    /// </summary>
    [AddComponentMenu("Monkey Adventure/UI/Mobile Button Linker")]
    public class MobileButtonLinker : MonoBehaviour
    {
        [Header("Target References")]
        [Tooltip("Reference to the player's GuardianCombat script. Auto-detected if null.")]
        [SerializeField] private GuardianCombat playerCombat;

        [Tooltip("Reference to the player GameObject. Auto-detected if null.")]
        [SerializeField] private GameObject playerObject;

        [Header("Jump Settings (Mobile Jump Button)")]
        [Tooltip("Upward velocity applied when pressing the mobile Jump button.")]
        [SerializeField] private float jumpPower = 10f;

        [Header("Interaction & Rescue Settings")]
        [Tooltip("Search radius for nearby rescue cages or interactable rune switches.")]
        [SerializeField] private float interactionRadius = 3.5f;

        private CharacterController _characterController;
        private Rigidbody _playerRigidbody;

        private void Start()
        {
            FindPlayerComponents();
        }

        private void FindPlayerComponents()
        {
            if (playerObject == null)
            {
                playerObject = GameObject.FindGameObjectWithTag("Player");
            }

            if (playerObject != null)
            {
                if (playerCombat == null)
                {
                    playerCombat = playerObject.GetComponent<GuardianCombat>();
                }

                _characterController = playerObject.GetComponent<CharacterController>();
                _playerRigidbody = playerObject.GetComponent<Rigidbody>();
            }
        }

        #region Public Methods for Canvas UI Button OnClick()

        /// <summary>
        /// Hook for Mobile Attack Button 1 (Energy Blast).
        /// </summary>
        public void TriggerMagic()
        {
            if (playerCombat == null) FindPlayerComponents();

            if (playerCombat != null)
            {
                playerCombat.CastEnergyBlast();
            }
            else
            {
                Debug.LogWarning("[MobileButtonLinker] GuardianCombat reference missing on Player!", this);
            }
        }

        /// <summary>
        /// Hook for Mobile Attack Button 2 (Ground Smash AOE).
        /// </summary>
        public void TriggerGroundSmash()
        {
            if (playerCombat == null) FindPlayerComponents();

            if (playerCombat != null)
            {
                playerCombat.CastGroundSmash();
            }
            else
            {
                Debug.LogWarning("[MobileButtonLinker] GuardianCombat reference missing on Player!", this);
            }
        }

        /// <summary>
        /// Hook for Mobile Jump Button.
        /// </summary>
        public void TriggerJump()
        {
            if (playerObject == null) FindPlayerComponents();

            if (playerObject != null)
            {
                // 1. Try sending custom message to player movement script
                playerObject.SendMessage("OnJumpInput", SendMessageOptions.DontRequireReceiver);
                playerObject.SendMessage("Jump", SendMessageOptions.DontRequireReceiver);

                // 2. Direct Rigidbody impulse fallback
                if (_playerRigidbody != null && !_playerRigidbody.isKinematic)
                {
                    _playerRigidbody.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
                }
            }
        }

        /// <summary>
        /// Hook for Mobile Rescue / Interact Button (cages, switches, NPCs).
        /// </summary>
        public void TriggerRescue()
        {
            if (playerObject == null) FindPlayerComponents();

            Vector3 origin = playerObject != null ? playerObject.transform.position : transform.position;

            // Detect nearby interactable objects (Rune Switches, rescue cages, NPCs)
            Collider[] hits = Physics.OverlapSphere(origin, interactionRadius);
            foreach (var hit in hits)
            {
                // Try activating Rune Switch
                if (hit.TryGetComponent<RuneSwitch>(out var rune))
                {
                    rune.ActivateSwitch();
                    return;
                }

                // Try generic interact message
                hit.SendMessage("Interact", SendMessageOptions.DontRequireReceiver);
                hit.SendMessage("OnRescue", SendMessageOptions.DontRequireReceiver);
            }

            Debug.Log("[MobileButtonLinker] TriggerRescue() executed.", this);
        }

        #endregion
    }
}
