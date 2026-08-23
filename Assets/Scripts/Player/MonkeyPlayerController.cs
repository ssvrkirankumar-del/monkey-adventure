using UnityEngine;
using MonkeyAdventure.Mechanics;
using MonkeyAdventure.Skins;

namespace MonkeyAdventure.Player
{
    /// <summary>
    /// Core 3D Player Controller for Monkey Adventure.
    /// Drives locomotion via Unity CharacterController (no Rigidbody required).
    /// Supports WASD/Keyboard, touch/virtual joystick inputs, camera-relative movement,
    /// smooth rotation, continuous gravity, jumping, air control, and integration with VineClimb & Evolution Skins.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Player/Monkey Player Controller")]
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class MonkeyPlayerController : MonoBehaviour
    {
        [Header("Locomotion Parameters")]
        [Tooltip("Base movement speed in meters per second.")]
        [SerializeField] private float moveSpeed = 7.0f;

        [Tooltip("Sprint multiplier when holding LeftShift (Editor testing).")]
        [SerializeField] private float sprintMultiplier = 1.4f;

        [Tooltip("Time taken to smoothly rotate towards movement direction.")]
        [SerializeField] private float rotationSmoothTime = 0.1f;

        [Header("Jumping & Gravity")]
        [Tooltip("Maximum jump height in meters.")]
        [SerializeField] private float jumpHeight = 2.2f;

        [Tooltip("Gravity acceleration multiplier applied to standard Physics.gravity.")]
        [SerializeField] private float gravityMultiplier = 2.0f;

        [Tooltip("Air control mobility factor when airborne (0 = no steering, 1 = full steering).")]
        [Range(0f, 1f)]
        [SerializeField] private float airControlMultiplier = 0.75f;

        [Tooltip("Maximum downward falling speed.")]
        [SerializeField] private float terminalVelocity = -50f;

        [Tooltip("Small downward velocity applied while grounded to ensure reliable slope contact.")]
        [SerializeField] private float groundedStickVelocity = -2.0f;

        [Header("Camera Reference")]
        [Tooltip("Reference to the main camera for camera-relative movement. Auto-finds Camera.main if unassigned.")]
        [SerializeField] private Transform cameraTransform;

        [Header("State (Read-Only)")]
        [SerializeField] private bool isGrounded = true;
        [SerializeField] private float currentSpeed = 0f;
        [SerializeField] private float verticalVelocity = 0f;

        // Components
        private CharacterController _characterController;
        private PlayerHealth _playerHealth;
        private VineClimb _vineClimb;
        private EvolutionSkinManager _skinManager;

        // Runtime Movement Variables
        private Vector2 _mobileMoveInput = Vector2.zero;
        private float _rotationVelocity;
        private bool _jumpRequested = false;
        private bool _canMove = true;

        #region Public Properties for Animation and External Systems
        public float Speed => currentSpeed;
        public bool IsGrounded => isGrounded;
        public float VerticalVelocity => verticalVelocity;
        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = Mathf.Max(0f, value);
        }
        public bool CanMove
        {
            get => _canMove;
            set => _canMove = value;
        }
        #endregion

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _playerHealth = GetComponent<PlayerHealth>();
            _vineClimb = GetComponent<VineClimb>();
            _skinManager = GetComponent<EvolutionSkinManager>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (_playerHealth != null)
            {
                _playerHealth.OnPlayerDied += HandlePlayerDied;
                _playerHealth.OnHealthChanged += HandleHealthChanged;
            }
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnPlayerDied -= HandlePlayerDied;
                _playerHealth.OnHealthChanged -= HandleHealthChanged;
            }
        }

        private void Update()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            // Check if player is alive
            if (_playerHealth != null && _playerHealth.IsDead)
            {
                ApplyGravityOnly();
                return;
            }

            // If climbing vines, let VineClimb handle displacement
            if (_vineClimb != null && _vineClimb.IsClimbing)
            {
                verticalVelocity = 0f;
                currentSpeed = 0f;
                return;
            }

            if (!_canMove)
            {
                ApplyGravityOnly();
                return;
            }

            GatherInputs();
            ProcessMovement();
        }

        #region Input Gathering
        private void GatherInputs()
        {
            // Keyboard inputs (Editor / Desktop)
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // Combine with touch / virtual joystick if active
            Vector2 combinedInput = new Vector2(h, v);
            if (_mobileMoveInput.sqrMagnitude > 0.01f)
            {
                combinedInput = _mobileMoveInput;
            }

            // Jump Key (Spacebar)
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            {
                _jumpRequested = true;
            }
        }
        #endregion

        #region Movement & Physics Logic
        private void ProcessMovement()
        {
            isGrounded = _characterController.isGrounded;

            // 1. Calculate effective gravity
            float effectiveGravity = Physics.gravity.y * gravityMultiplier;

            // 2. Handle Grounded State and Jumping
            if (isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = groundedStickVelocity;
                }

                if (_jumpRequested)
                {
                    ExecuteJump(effectiveGravity);
                }
            }
            else
            {
                // Check if Hanuman flying skin is enabled
                bool isFlyingMode = _skinManager != null && _skinManager.CurrentSkin != null && _skinManager.CurrentSkin.allowFlying;
                if (isFlyingMode)
                {
                    // Hanuman gentle glide / hover
                    if (Input.GetButton("Jump") || Input.GetKey(KeyCode.Space))
                    {
                        verticalVelocity = 2.5f;
                    }
                    else
                    {
                        verticalVelocity = Mathf.Max(verticalVelocity + effectiveGravity * 0.2f * Time.deltaTime, -3.0f);
                    }
                }
                else
                {
                    // Normal continuous downward acceleration
                    verticalVelocity += effectiveGravity * Time.deltaTime;
                    if (verticalVelocity < terminalVelocity)
                    {
                        verticalVelocity = terminalVelocity;
                    }
                }
            }

            _jumpRequested = false;

            // 3. Horizontal Locomotion Direction
            float inputH = Input.GetAxisRaw("Horizontal");
            float inputV = Input.GetAxisRaw("Vertical");
            if (_mobileMoveInput.sqrMagnitude > 0.01f)
            {
                inputH = _mobileMoveInput.x;
                inputV = _mobileMoveInput.y;
            }

            Vector3 inputDirection = new Vector3(inputH, 0f, inputV).normalized;
            Vector3 moveDirection = Vector3.zero;

            if (inputDirection.magnitude >= 0.1f)
            {
                // Camera-relative angle calculation
                float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
                if (cameraTransform != null)
                {
                    targetAngle += cameraTransform.eulerAngles.y;
                }

                float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _rotationVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);

                moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            }

            // 4. Calculate Final Movement Speed
            float targetSpeed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                targetSpeed *= sprintMultiplier;
            }

            // Apply power multiplier from equipped evolution skin
            if (_skinManager != null && _skinManager.CurrentSkin != null)
            {
                targetSpeed *= Mathf.Max(1.0f, _skinManager.CurrentSkin.powerMultiplier * 0.9f);
            }

            // Air control dampening
            float currentAirFactor = isGrounded ? 1.0f : airControlMultiplier;
            Vector3 horizontalVelocity = moveDirection * (targetSpeed * currentAirFactor);
            currentSpeed = inputDirection.magnitude * targetSpeed;

            // 5. Combine Horizontal & Vertical displacement
            Vector3 finalDisplacement = horizontalVelocity;
            finalDisplacement.y = verticalVelocity;

            // 6. Execute CharacterController Move
            _characterController.Move(finalDisplacement * Time.deltaTime);
        }

        private void ExecuteJump(float effectiveGravity)
        {
            // v = sqrt(2 * g * h)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * effectiveGravity);

            // Notify MonkeySetupBinder or child animators
            SendMessage("TriggerJumpAnimation", SendMessageOptions.DontRequireReceiver);
        }

        private void ApplyGravityOnly()
        {
            isGrounded = _characterController.isGrounded;
            float effectiveGravity = Physics.gravity.y * gravityMultiplier;

            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedStickVelocity;
            }
            else
            {
                verticalVelocity += effectiveGravity * Time.deltaTime;
            }

            _characterController.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
            currentSpeed = 0f;
        }
        #endregion

        #region Public Methods for Mobile Controls & External Hooks
        /// <summary>
        /// Public Jump hook callable by mobile canvas jump buttons or event linkers.
        /// </summary>
        public void Jump()
        {
            if (isGrounded)
            {
                _jumpRequested = true;
            }
        }

        /// <summary>
        /// Direct input injector for mobile UI virtual joysticks / touch controls.
        /// </summary>
        public void SetMoveInput(Vector2 input)
        {
            _mobileMoveInput = input;
        }

        /// <summary>
        /// Message receiver hook for SendMessage("OnJumpInput").
        /// </summary>
        public void OnJumpInput()
        {
            Jump();
        }

        /// <summary>
        /// Teleports the player to a target position and resets vertical momentum.
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _characterController.enabled = false;
            transform.position = position;
            transform.rotation = rotation;
            _characterController.enabled = true;
            verticalVelocity = 0f;
        }
        #endregion

        #region Event Callbacks
        private void HandlePlayerDied()
        {
            currentSpeed = 0f;
        }

        private void HandleHealthChanged(int current, int max)
        {
            // Optional flinch feedback
        }
        #endregion
    }
}
