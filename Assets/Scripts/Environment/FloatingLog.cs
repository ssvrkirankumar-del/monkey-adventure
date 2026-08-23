using UnityEngine;

namespace MonkeyAdventure.Environment
{
    /// <summary>
    /// Act 3: Floating Log.
    /// Simulates buoyant floating and bobbing on water.
    /// Sinks slightly under player weight when stepped on and springs back naturally.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Environment/Floating Log")]
    [RequireComponent(typeof(Collider))]
    public class FloatingLog : MonoBehaviour
    {
        [Header("Floating Bob Physics")]
        [Tooltip("Frequency of natural water bobbing.")]
        [SerializeField] private float bobFrequency = 1.8f;

        [Tooltip("Amplitude of vertical natural water bobbing.")]
        [SerializeField] private float bobAmplitude = 0.12f;

        [Tooltip("Rotational roll sway angle on water in degrees.")]
        [SerializeField] private float rollSwayAmount = 2.5f;

        [Header("Player Weight Sink Mechanics")]
        [Tooltip("How much the log dips when the player stands on it.")]
        [SerializeField] private float playerSinkDepth = 0.35f;

        [Tooltip("Spring speed for sinking down and returning up.")]
        [SerializeField] private float springSpeed = 4.0f;

        [Tooltip("Sound played when player jumps onto the log.")]
        [SerializeField] private AudioClip waterSplashSound;

        [SerializeField] private ParticleSystem waterRipplesVFX;

        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private float _currentSinkOffset = 0f;
        private float _targetSinkOffset = 0f;
        private int _playerCountOnLog = 0;
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true; // Use kinematic spring movement for stability
            }
        }

        private void Start()
        {
            _basePosition = transform.position;
            _baseRotation = transform.rotation;
        }

        private void Update()
        {
            // 1. Calculate Target Sink Offset based on whether player is on log
            _targetSinkOffset = _playerCountOnLog > 0 ? -playerSinkDepth : 0f;
            _currentSinkOffset = Mathf.Lerp(_currentSinkOffset, _targetSinkOffset, Time.deltaTime * springSpeed);

            // 2. Natural Water Bobbing & Rolling Wave
            float waveBob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            float rollWave = Mathf.Sin(Time.time * bobFrequency * 0.8f) * rollSwayAmount;

            // 3. Apply New Position and Rotation
            Vector3 finalPos = new Vector3(_basePosition.x, _basePosition.y + waveBob + _currentSinkOffset, _basePosition.z);
            Quaternion finalRot = _baseRotation * Quaternion.Euler(0f, 0f, rollWave);

            if (_rigidbody != null && !_rigidbody.isKinematic)
            {
                _rigidbody.MovePosition(finalPos);
                _rigidbody.MoveRotation(finalRot);
            }
            else
            {
                transform.position = finalPos;
                transform.rotation = finalRot;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") || 
               (collision.gameObject.transform.root != null && collision.gameObject.transform.root.CompareTag("Player")))
            {
                _playerCountOnLog++;
                PlayWaterEffects();
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player") || 
               (collision.gameObject.transform.root != null && collision.gameObject.transform.root.CompareTag("Player")))
            {
                _playerCountOnLog = Mathf.Max(0, _playerCountOnLog - 1);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
            {
                _playerCountOnLog++;
                PlayWaterEffects();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
            {
                _playerCountOnLog = Mathf.Max(0, _playerCountOnLog - 1);
            }
        }

        private void PlayWaterEffects()
        {
            if (waterRipplesVFX != null)
            {
                waterRipplesVFX.Play();
            }

            if (waterSplashSound != null)
            {
                AudioSource.PlayClipAtPoint(waterSplashSound, transform.position);
            }
        }
    }
}
