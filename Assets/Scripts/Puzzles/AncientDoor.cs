using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MonkeyAdventure.Puzzles
{
    /// <summary>
    /// Act 2: Ancient Door.
    /// Monitors a list of RuneSwitches. When all switches are activated, smoothly slides downwards
    /// to open over a configurable duration using Vector3.Lerp, with sound and camera shake.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Puzzles/Ancient Door")]
    public class AncientDoor : MonoBehaviour
    {
        [Header("Linked Rune Switches")]
        [Tooltip("List of rune switches required to unlock this ancient door.")]
        [SerializeField] private List<RuneSwitch> requiredSwitches = new List<RuneSwitch>();

        [Header("Movement & Slide Settings")]
        [Tooltip("Offset applied to the door when fully opened (e.g. (0, -6, 0) to slide into the ground).")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, -6f, 0f);

        [Tooltip("Time in seconds to fully slide open.")]
        [SerializeField] private float openDuration = 2.0f;

        [Tooltip("Animation curve for realistic stone grinding acceleration/easing.")]
        [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Audio & Camera Shake")]
        [SerializeField] private AudioClip doorOpeningSound;
        [SerializeField] private AudioClip doorOpenedRumbleSound;
        [SerializeField] private ParticleSystem dustParticles;

        [Tooltip("Duration of camera shake when door opens.")]
        [SerializeField] private float cameraShakeDuration = 1.5f;
        [SerializeField] private float cameraShakeMagnitude = 0.2f;

        private Vector3 _closedPosition;
        private Vector3 _openPosition;
        private bool _isOpen = false;
        private bool _isOpening = false;
        private AudioSource _audioSource;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _closedPosition = transform.position;
            _openPosition = _closedPosition + openOffset;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f; // 3D sound in world space
                _audioSource.playOnAwake = false;
            }
        }

        private void OnEnable()
        {
            // Subscribe to switch activation events
            foreach (var rune in requiredSwitches)
            {
                if (rune != null)
                {
                    rune.OnSwitchStateChanged += HandleSwitchChanged;
                }
            }
        }

        private void OnDisable()
        {
            foreach (var rune in requiredSwitches)
            {
                if (rune != null)
                {
                    rune.OnSwitchStateChanged -= HandleSwitchChanged;
                }
            }
        }

        private void Start()
        {
            CheckAllSwitches();
        }

        private void HandleSwitchChanged(RuneSwitch changedSwitch)
        {
            CheckAllSwitches();
        }

        /// <summary>
        /// Validates if every linked rune switch is currently active.
        /// </summary>
        public void CheckAllSwitches()
        {
            if (_isOpen || _isOpening) return;

            if (requiredSwitches.Count == 0) return;

            bool allActive = true;
            foreach (var rune in requiredSwitches)
            {
                if (rune == null || !rune.IsActivated)
                {
                    allActive = false;
                    break;
                }
            }

            if (allActive)
            {
                StartCoroutine(OpenDoorRoutine());
            }
        }

        private IEnumerator OpenDoorRoutine()
        {
            _isOpening = true;

            // 1. Play Opening Audio & Dust
            if (doorOpeningSound != null && _audioSource != null)
            {
                _audioSource.clip = doorOpeningSound;
                _audioSource.Play();
            }

            if (dustParticles != null)
            {
                dustParticles.Play();
            }

            // 2. Trigger Camera Shake
            StartCoroutine(ShakeCameraRoutine(cameraShakeDuration, cameraShakeMagnitude));

            // 3. Smooth Lerp movement downwards
            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / openDuration);
                float curvedProgress = slideCurve.Evaluate(progress);

                transform.position = Vector3.Lerp(_closedPosition, _openPosition, curvedProgress);
                yield return null;
            }

            transform.position = _openPosition;
            _isOpen = true;
            _isOpening = false;

            if (doorOpenedRumbleSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(doorOpenedRumbleSound);
            }

            Debug.Log($"[AncientDoor] '{name}' successfully opened!", this);
        }

        private IEnumerator ShakeCameraRoutine(float duration, float magnitude)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) yield break;

            Vector3 originalCamPos = mainCam.transform.localPosition;
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                float x = (UnityEngine.Random.value * 2f - 1f) * magnitude;
                float y = (UnityEngine.Random.value * 2f - 1f) * magnitude;

                mainCam.transform.localPosition = originalCamPos + new Vector3(x, y, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            mainCam.transform.localPosition = originalCamPos;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw lines connecting door to linked switches in Scene view
            Gizmos.color = Color.cyan;
            foreach (var rune in requiredSwitches)
            {
                if (rune != null)
                {
                    Gizmos.DrawLine(transform.position, rune.transform.position);
                }
            }

            // Draw target open position preview
            Vector3 openPos = Application.isPlaying ? _openPosition : transform.position + openOffset;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(openPos, transform.localScale);
        }
    }
}
