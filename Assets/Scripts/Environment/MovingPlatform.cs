using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MonkeyAdventure.Environment
{
    /// <summary>
    /// Act 3: Moving Platform.
    /// Moves back and forth between waypoints using Vector3.MoveTowards.
    /// Carries the player smoothly when they stand on it.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Environment/Moving Platform")]
    [RequireComponent(typeof(Collider))]
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Waypoints")]
        [Tooltip("List of waypoint transforms to move between.")]
        [SerializeField] private List<Transform> waypoints = new List<Transform>();

        [Header("Movement Settings")]
        [Tooltip("Movement speed towards waypoints.")]
        [SerializeField] private float speed = 3.5f;

        [Tooltip("Time in seconds to wait at each waypoint before reversing or advancing.")]
        [SerializeField] private float waitTimeAtWaypoint = 0.8f;

        [Tooltip("If true, loops in a circle through waypoints. If false, ping-pongs back and forth.")]
        [SerializeField] private bool loopWaypoints = false;

        [Header("Passenger Carrying")]
        [Tooltip("Tag identifying the player character.")]
        [SerializeField] private string passengerTag = "Player";

        private int _currentWaypointIndex = 0;
        private bool _isReversing = false;
        private bool _isWaiting = false;
        private Vector3 _lastPosition;
        private Vector3 _platformDelta;
        private readonly HashSet<Transform> _passengers = new HashSet<Transform>();

        private void Start()
        {
            _lastPosition = transform.position;

            // Fallback: If no waypoints assigned, create 2 default local positions
            if (waypoints.Count == 0)
            {
                GameObject wp1 = new GameObject($"{name}_WP1");
                wp1.transform.position = transform.position;
                GameObject wp2 = new GameObject($"{name}_WP2");
                wp2.transform.position = transform.position + transform.forward * 5f;
                waypoints.Add(wp1.transform);
                waypoints.Add(wp2.transform);
            }
        }

        private void Update()
        {
            if (waypoints.Count < 2 || _isWaiting) return;

            Transform targetWp = waypoints[_currentWaypointIndex];
            if (targetWp == null) return;

            // 1. Move Platform
            transform.position = Vector3.MoveTowards(transform.position, targetWp.position, speed * Time.deltaTime);

            // 2. Check if reached waypoint
            if (Vector3.Distance(transform.position, targetWp.position) < 0.01f)
            {
                StartCoroutine(WaitAtWaypointRoutine());
            }
        }

        private void LateUpdate()
        {
            // Calculate movement delta
            _platformDelta = transform.position - _lastPosition;
            _lastPosition = transform.position;

            // Move passengers along with platform
            foreach (Transform passenger in _passengers)
            {
                if (passenger != null)
                {
                    // If passenger has CharacterController, move it
                    if (passenger.TryGetComponent<CharacterController>(out var cc))
                    {
                        cc.Move(_platformDelta);
                    }
                    else
                    {
                        passenger.position += _platformDelta;
                    }
                }
            }
        }

        private IEnumerator WaitAtWaypointRoutine()
        {
            _isWaiting = true;
            yield return new WaitForSeconds(waitTimeAtWaypoint);

            // Advance waypoint index
            if (loopWaypoints)
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Count;
            }
            else
            {
                if (_isReversing)
                {
                    _currentWaypointIndex--;
                    if (_currentWaypointIndex <= 0)
                    {
                        _currentWaypointIndex = 0;
                        _isReversing = false;
                    }
                }
                else
                {
                    _currentWaypointIndex++;
                    if (_currentWaypointIndex >= waypoints.Count - 1)
                    {
                        _currentWaypointIndex = waypoints.Count - 1;
                        _isReversing = true;
                    }
                }
            }

            _isWaiting = false;
        }

        #region Passenger Detection
        private void OnCollisionEnter(Collision collision)
        {
            if (IsPassenger(collision.gameObject, out Transform root))
            {
                _passengers.Add(root);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (IsPassenger(collision.gameObject, out Transform root))
            {
                _passengers.Remove(root);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsPassenger(other.gameObject, out Transform root))
            {
                _passengers.Add(root);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPassenger(other.gameObject, out Transform root))
            {
                _passengers.Remove(root);
            }
        }

        private bool IsPassenger(GameObject obj, out Transform root)
        {
            root = obj.transform.root;
            return obj.CompareTag(passengerTag) || (root != null && root.CompareTag(passengerTag));
        }
        #endregion

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count < 2) return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawWireSphere(waypoints[i].position, 0.4f);
                    if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    }
                }
            }

            if (loopWaypoints && waypoints.Count > 2 && waypoints[0] != null && waypoints[waypoints.Count - 1] != null)
            {
                Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
            }
        }
    }
}
