using System.Collections.Generic;
using UnityEngine;

namespace MonkeyAdventure.Environment
{
    /// <summary>
    /// Act 5: Floating Island.
    /// Hovering floating island platform using Sine wave Mathf.Sin(Time.time).
    /// Maintains smooth player collision and footing without jitter.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Environment/Floating Island")]
    public class FloatingIsland : MonoBehaviour
    {
        [Header("Floating Sine Wave Settings")]
        [Tooltip("Frequency/speed of the vertical hover cycle.")]
        [SerializeField] private float hoverFrequency = 0.8f;

        [Tooltip("Height amplitude of the hover.")]
        [SerializeField] private float hoverAmplitude = 1.2f;

        [Tooltip("Subtle rotational tilt sway in degrees.")]
        [SerializeField] private float tiltAngle = 1.5f;

        [Tooltip("Offset time to stagger motion between multiple islands.")]
        [SerializeField] private float timeOffset = 0f;

        [Header("Passenger Tag")]
        [SerializeField] private string passengerTag = "Player";

        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private Vector3 _lastPosition;
        private Vector3 _islandDelta;
        private readonly HashSet<Transform> _passengers = new HashSet<Transform>();
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
            }
        }

        private void Start()
        {
            _basePosition = transform.position;
            _baseRotation = transform.rotation;
            _lastPosition = transform.position;

            if (timeOffset == 0f)
            {
                // Auto random offset so adjacent islands don't bob in exact lockstep
                timeOffset = Random.Range(0f, 10f);
            }
        }

        private void FixedUpdate()
        {
            float t = Time.time + timeOffset;

            // 1. Calculate Sine Hover and Gentle Tilt
            float hoverOffset = Mathf.Sin(t * hoverFrequency) * hoverAmplitude;
            float tiltZ = Mathf.Sin(t * (hoverFrequency * 0.7f)) * tiltAngle;
            float tiltX = Mathf.Cos(t * (hoverFrequency * 0.5f)) * (tiltAngle * 0.5f);

            Vector3 targetPosition = new Vector3(_basePosition.x, _basePosition.y + hoverOffset, _basePosition.z);
            Quaternion targetRotation = _baseRotation * Quaternion.Euler(tiltX, 0f, tiltZ);

            if (_rigidbody != null)
            {
                _rigidbody.MovePosition(targetPosition);
                _rigidbody.MoveRotation(targetRotation);
            }
            else
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
        }

        private void LateUpdate()
        {
            _islandDelta = transform.position - _lastPosition;
            _lastPosition = transform.position;

            // Move passengers along with island
            foreach (Transform passenger in _passengers)
            {
                if (passenger != null)
                {
                    if (passenger.TryGetComponent<CharacterController>(out var cc))
                    {
                        cc.Move(_islandDelta);
                    }
                    else
                    {
                        passenger.position += _islandDelta;
                    }
                }
            }
        }

        #region Passenger Detection
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag(passengerTag) || 
               (collision.gameObject.transform.root != null && collision.gameObject.transform.root.CompareTag(passengerTag)))
            {
                _passengers.Add(collision.gameObject.transform.root);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag(passengerTag) || 
               (collision.gameObject.transform.root != null && collision.gameObject.transform.root.CompareTag(passengerTag)))
            {
                _passengers.Remove(collision.gameObject.transform.root);
            }
        }
        #endregion
    }
}
