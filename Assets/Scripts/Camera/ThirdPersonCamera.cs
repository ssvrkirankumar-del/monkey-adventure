using UnityEngine;

namespace MonkeyAdventure.Cameras
{
    /// <summary>
    /// Smooth Third-Person Orbit Follow Camera for Monkey Adventure.
    /// Follows the player without rigid parenting, supports mouse orbit (Right-Click or Free Look),
    /// touch orbit, pitch clamping, smooth dampening, and SphereCast occlusion collision avoidance.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Camera/Third Person Camera")]
    [DisallowMultipleComponent]
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Follow Target")]
        [Tooltip("The target transform to follow (typically the Player). Auto-finds tagged 'Player' if null.")]
        [SerializeField] private Transform target;

        [Tooltip("Height offset above the target to focus on (player's upper body).")]
        [SerializeField] private float targetHeightOffset = 1.4f;

        [Header("Distance & Orbit Settings")]
        [Tooltip("Standard follow distance behind the player.")]
        [SerializeField] private float defaultDistance = 5.0f;

        [Tooltip("Minimum allowable distance when colliding with obstacles.")]
        [SerializeField] private float minDistance = 1.0f;

        [Tooltip("Maximum allowable distance.")]
        [SerializeField] private float maxDistance = 9.0f;

        [Tooltip("Horizontal mouse/touch orbit sensitivity.")]
        [SerializeField] private float horizontalSensitivity = 140f;

        [Tooltip("Vertical pitch sensitivity.")]
        [SerializeField] private float verticalSensitivity = 100f;

        [Tooltip("Minimum vertical pitch angle in degrees.")]
        [SerializeField] private float minPitch = -15f;

        [Tooltip("Maximum vertical pitch angle in degrees.")]
        [SerializeField] private float maxPitch = 60f;

        [Header("Smoothing & Dampening")]
        [Tooltip("Time taken to smoothly damp camera position.")]
        [SerializeField] private float positionSmoothTime = 0.08f;

        [Tooltip("Time taken to smoothly damp rotation angles.")]
        [SerializeField] private float rotationSmoothTime = 0.05f;

        [Header("Collision & Occlusion Avoidance")]
        [Tooltip("Enable SphereCast to prevent camera from clipping through terrain and walls.")]
        [SerializeField] private bool enableCollisionAvoidance = true;

        [Tooltip("Radius of the collision detection sphere.")]
        [SerializeField] private float collisionRadius = 0.25f;

        [Tooltip("Offset distance pushed away from hit surface.")]
        [SerializeField] private float collisionOffset = 0.15f;

        [Tooltip("Layer mask specifying what obstacles push the camera closer.")]
        [SerializeField] private LayerMask collisionLayers = ~0;

        [Header("Mouse Orbit Mode")]
        [Tooltip("If true, requires holding Right Mouse Button to rotate camera in Editor. If false, always tracks mouse.")]
        [SerializeField] private bool requireRightClickToOrbit = true;

        // Current Spherical Orbit Coordinates
        private float _currentYaw = 0f;
        private float _currentPitch = 15f;
        private float _targetYaw = 0f;
        private float _targetPitch = 15f;

        private float _yawVelocity;
        private float _pitchVelocity;
        private Vector3 _currentVelocity;
        private float _currentDistance;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        private void Start()
        {
            FindTargetIfNull();

            _currentDistance = defaultDistance;

            if (target != null)
            {
                _targetYaw = target.eulerAngles.y;
                _currentYaw = _targetYaw;
            }

            // Exclude Player/Ignore Raycast layers from collision mask if default
            if (collisionLayers == ~0)
            {
                int playerLayer = LayerMask.NameToLayer("Ignore Raycast");
                if (playerLayer != -1)
                {
                    collisionLayers &= ~(1 << playerLayer);
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindTargetIfNull();
                if (target == null) return;
            }

            HandleOrbitInput();
            CalculateCameraPosition();
        }

        private void FindTargetIfNull()
        {
            if (target != null) return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        #region Orbit Input
        private void HandleOrbitInput()
        {
            bool canOrbit = true;
            if (requireRightClickToOrbit && Application.isEditor)
            {
                canOrbit = Input.GetMouseButton(1) || Input.GetMouseButton(0);
            }

            if (canOrbit)
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                _targetYaw += mouseX * horizontalSensitivity * Time.deltaTime;
                _targetPitch -= mouseY * verticalSensitivity * Time.deltaTime;
                _targetPitch = Mathf.Clamp(_targetPitch, minPitch, maxPitch);
            }

            // Smooth angles
            _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _targetYaw, ref _yawVelocity, rotationSmoothTime);
            _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _targetPitch, ref _pitchVelocity, rotationSmoothTime);
        }
        #endregion

        #region Position Calculation & Collision Avoidance
        private void CalculateCameraPosition()
        {
            Vector3 focusPoint = target.position + Vector3.up * targetHeightOffset;
            Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);

            // Calculate desired un-occluded position
            Vector3 desiredDirection = rotation * -Vector3.forward;
            float desiredDistance = defaultDistance;

            // Handle Collision with Terrain / Walls
            if (enableCollisionAvoidance)
            {
                Ray ray = new Ray(focusPoint, desiredDirection);
                if (Physics.SphereCast(ray, collisionRadius, out RaycastHit hit, defaultDistance, collisionLayers, QueryTriggerInteraction.Ignore))
                {
                    // Don't collide with the target itself
                    if (hit.transform.root != target.root)
                    {
                        desiredDistance = Mathf.Clamp(hit.distance - collisionOffset, minDistance, maxDistance);
                    }
                }
            }

            _currentDistance = Mathf.Lerp(_currentDistance, desiredDistance, Time.deltaTime * 15f);

            Vector3 targetPosition = focusPoint + desiredDirection * _currentDistance;

            // Smooth Damp to target position
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, positionSmoothTime);
            transform.LookAt(focusPoint);
        }
        #endregion

        /// <summary>
        /// Instantly snaps the camera behind the target without smooth lag.
        /// </summary>
        public void SnapBehindTarget()
        {
            if (target == null) return;

            _targetYaw = target.eulerAngles.y;
            _currentYaw = _targetYaw;
            _targetPitch = 15f;
            _currentPitch = 15f;

            Vector3 focusPoint = target.position + Vector3.up * targetHeightOffset;
            Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            Vector3 desiredDirection = rotation * -Vector3.forward;
            transform.position = focusPoint + desiredDirection * defaultDistance;
            transform.LookAt(focusPoint);
        }
    }
}
