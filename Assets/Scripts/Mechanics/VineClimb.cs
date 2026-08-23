using UnityEngine;

namespace MonkeyAdventure.Mechanics
{
    /// <summary>
    /// Attached to the Player or Vine object.
    /// Allows the Player's CharacterController to grab vertical vines/ropes,
    /// climb up and down, and dismount with a jump.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Mechanics/Vine Climb")]
    public class VineClimb : MonoBehaviour
    {
        [Header("Climb Settings")]
        [Tooltip("Climbing speed moving up and down the vine.")]
        [SerializeField] private float climbSpeed = 4.5f;

        [Tooltip("Force applied when leaping/jumping off the vine.")]
        [SerializeField] private float dismountJumpForce = 8.0f;

        [Tooltip("Tag identifying climbable vine objects in the scene.")]
        [SerializeField] private string vineTag = "Vine";

        [Header("Audio Feedback")]
        [SerializeField] private AudioClip grabVineSound;
        [SerializeField] private AudioClip climbStepSound;
        [SerializeField] private AudioClip leapOffSound;

        [Header("State")]
        [SerializeField] private bool isClimbing = false;

        private CharacterController _characterController;
        private Transform _currentVine;
        private AudioSource _audioSource;
        private float _nextStepSoundTime;

        public bool IsClimbing => isClimbing;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (!isClimbing) return;

            HandleClimbMovement();
        }

        private void HandleClimbMovement()
        {
            // 1. Vertical Climb Input (Joystick Y / W-S keys)
            float vInput = Input.GetAxis("Vertical");

            if (Mathf.Abs(vInput) > 0.1f && _characterController != null)
            {
                Vector3 climbMove = Vector3.up * (vInput * climbSpeed * Time.deltaTime);
                _characterController.Move(climbMove);

                // Periodic climbing sound
                if (climbStepSound != null && Time.time >= _nextStepSoundTime)
                {
                    _nextStepSoundTime = Time.time + 0.4f;
                    PlaySound(climbStepSound);
                }
            }

            // 2. Jump Off / Dismount Vine (Space / Jump Button)
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            {
                DismountVine();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(vineTag) || (other.name.ToLower().Contains("vine") && other.isTrigger))
            {
                AttachToVine(other.transform);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(vineTag) || other.transform == _currentVine)
            {
                if (isClimbing)
                {
                    DetachFromVine();
                }
            }
        }

        public void AttachToVine(Transform vineTransform)
        {
            isClimbing = true;
            _currentVine = vineTransform;

            PlaySound(grabVineSound);
            Debug.Log($"[VineClimb] Attached to Vine: {vineTransform.name}");
        }

        public void DetachFromVine()
        {
            isClimbing = false;
            _currentVine = null;
            Debug.Log("[VineClimb] Detached from Vine.");
        }

        public void DismountVine()
        {
            if (!isClimbing) return;

            Vector3 leapDirection = transform.forward + Vector3.up * 0.8f;
            if (_characterController != null)
            {
                _characterController.Move(leapDirection * (dismountJumpForce * Time.deltaTime));
            }

            PlaySound(leapOffSound);
            DetachFromVine();
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}
