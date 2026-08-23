using UnityEngine;
using MonkeyAdventure.Player;
using MonkeyAdventure.Skins;
using MonkeyAdventure.Mechanics;
using GuardianSystem.Combat;

namespace MonkeyAdventure.Animation
{
    /// <summary>
    /// Auto-binding component for imported Monkey 3D models and evolution prefabs.
    /// Automatically detects the Animator component, maps movement and combat parameters,
    /// and binds events with PlayerHealth, EvolutionSkinManager, GuardianCombat, and VineClimb.
    /// Supports Hanuman flying mode and King Kong heavy smash animations.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Animation/Monkey Setup Binder")]
    [DisallowMultipleComponent]
    public class MonkeySetupBinder : MonoBehaviour
    {
        [Header("Animator Reference")]
        [Tooltip("The Animator component on this monkey model or child.")]
        [SerializeField] private Animator animator;

        [Header("Animator Parameter Names")]
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string isGroundedParam = "IsGrounded";
        [SerializeField] private string jumpTriggerParam = "Jump";
        [SerializeField] private string attackTriggerParam = "Attack";
        [SerializeField] private string smashTriggerParam = "Smash";
        [SerializeField] private string isClimbingParam = "IsClimbing";
        [SerializeField] private string isFlyingParam = "IsFlying";
        [SerializeField] private string hurtTriggerParam = "Hurt";
        [SerializeField] private string dieTriggerParam = "Die";
        [SerializeField] private string skinIndexParam = "SkinIndex";

        [Header("Smooth Animation Dampening")]
        [SerializeField] private float speedDampTime = 0.1f;

        [Header("Special Evolutions Support")]
        [Tooltip("Enable King Kong ground slam animations when Ground Smash is triggered.")]
        [SerializeField] private bool enableKingKongSmash = true;

        [Tooltip("Enable Hanuman sky hover and aerial flight animations.")]
        [SerializeField] private bool enableHanumanFlying = true;

        // Cached Animator Hash IDs for optimal mobile CPU performance
        private int _speedHash;
        private int _isGroundedHash;
        private int _jumpHash;
        private int _attackHash;
        private int _smashHash;
        private int _isClimbingHash;
        private int _isFlyingHash;
        private int _hurtHash;
        private int _dieHash;
        private int _skinIndexHash;

        // Component References (Found on root player object)
        private CharacterController _characterController;
        private PlayerHealth _playerHealth;
        private EvolutionSkinManager _skinManager;
        private VineClimb _vineClimb;
        private GuardianCombat _guardianCombat;

        private void Awake()
        {
            FindAnimator();
            CacheParameterHashes();
            FindParentPlayerComponents();
        }

        private void Start()
        {
            SubscribeToPlayerEvents();
            SyncSkinState();
        }

        private void OnDestroy()
        {
            UnsubscribeFromPlayerEvents();
        }

        private void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            UpdateMovementParameters();
            UpdateVineClimbingState();
            UpdateFlyingState();
        }

        #region Initialization & Hash Caching
        private void FindAnimator()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }
        }

        private void CacheParameterHashes()
        {
            _speedHash = Animator.StringToHash(speedParam);
            _isGroundedHash = Animator.StringToHash(isGroundedParam);
            _jumpHash = Animator.StringToHash(jumpTriggerParam);
            _attackHash = Animator.StringToHash(attackTriggerParam);
            _smashHash = Animator.StringToHash(smashTriggerParam);
            _isClimbingHash = Animator.StringToHash(isClimbingParam);
            _isFlyingHash = Animator.StringToHash(isFlyingParam);
            _hurtHash = Animator.StringToHash(hurtTriggerParam);
            _dieHash = Animator.StringToHash(dieTriggerParam);
            _skinIndexHash = Animator.StringToHash(skinIndexParam);
        }

        private void FindParentPlayerComponents()
        {
            Transform root = transform.root;
            _characterController = root.GetComponent<CharacterController>();
            _playerHealth = root.GetComponent<PlayerHealth>();
            _skinManager = root.GetComponent<EvolutionSkinManager>();
            _vineClimb = root.GetComponent<VineClimb>();
            _guardianCombat = root.GetComponent<GuardianCombat>();
        }
        #endregion

        #region Event Subscriptions
        private void SubscribeToPlayerEvents()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnPlayerDied += HandlePlayerDied;
                _playerHealth.OnHealthChanged += HandleHealthChanged;
            }

            if (_skinManager != null)
            {
                _skinManager.OnSkinEquipped += HandleSkinEquipped;
            }
        }

        private void UnsubscribeFromPlayerEvents()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnPlayerDied -= HandlePlayerDied;
                _playerHealth.OnHealthChanged -= HandleHealthChanged;
            }

            if (_skinManager != null)
            {
                _skinManager.OnSkinEquipped -= HandleSkinEquipped;
            }
        }
        #endregion

        #region Parameter Updates
        private void UpdateMovementParameters()
        {
            if (_characterController != null)
            {
                // Calculate horizontal plane speed
                Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0, _characterController.velocity.z);
                float speed = horizontalVelocity.magnitude;

                // Update Speed float with smooth dampening
                animator.SetFloat(_speedHash, speed, speedDampTime, Time.deltaTime);

                // Update Grounded boolean
                animator.SetBool(_isGroundedHash, _characterController.isGrounded);
            }
            else
            {
                // Fallback velocity check via input axes
                float h = Input.GetAxis("Horizontal");
                float v = Input.GetAxis("Vertical");
                float inputMagnitude = new Vector2(h, v).magnitude;
                animator.SetFloat(_speedHash, inputMagnitude, speedDampTime, Time.deltaTime);
            }
        }

        private void UpdateVineClimbingState()
        {
            if (_vineClimb != null)
            {
                animator.SetBool(_isClimbingHash, _vineClimb.IsClimbing);
            }
        }

        private void UpdateFlyingState()
        {
            if (enableHanumanFlying && _skinManager != null && _skinManager.CurrentSkin != null)
            {
                bool isFlying = _skinManager.CurrentSkin.allowFlying && (_characterController != null && !_characterController.isGrounded);
                animator.SetBool(_isFlyingHash, isFlying);
            }
        }
        #endregion

        #region Animation Triggers
        /// <summary>
        /// Triggered when the player jumps.
        /// </summary>
        public void TriggerJumpAnimation()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(_jumpHash);
            }
        }

        /// <summary>
        /// Triggered when shooting an Energy Blast.
        /// </summary>
        public void TriggerAttackAnimation()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(_attackHash);
            }
        }

        /// <summary>
        /// Triggered during King Kong or Guardian Ground Smash.
        /// </summary>
        public void TriggerSmashAnimation()
        {
            if (enableKingKongSmash && animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(_smashHash);
            }
        }
        #endregion

        #region Event Callbacks
        private void HandleHealthChanged(int current, int max)
        {
            // Trigger Hurt flinch on damage
            if (animator != null && animator.runtimeAnimatorController != null && current > 0)
            {
                animator.SetTrigger(_hurtHash);
            }
        }

        private void HandlePlayerDied()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger(_dieHash);
            }
        }

        private void HandleSkinEquipped(int skinIndex)
        {
            SyncSkinState();
        }

        private void SyncSkinState()
        {
            if (_skinManager != null && animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetInteger(_skinIndexHash, _skinManager.CurrentEquippedIndex);
            }
        }
        #endregion

        #region Editor Auto-Setup Helper
        [ContextMenu("Auto-Bind Components")]
        public void AutoBindInEditor()
        {
            FindAnimator();
            FindParentPlayerComponents();
            CacheParameterHashes();
            Debug.Log($"[MonkeySetupBinder] Successfully auto-bound on '{name}'! Animator: {animator != null}");
        }
        #endregion
    }
}
