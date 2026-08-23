using System.Collections;
using UnityEngine;

namespace MonkeyAdventure.AI
{
    public enum WildlifeType
    {
        WanderingAnimal, // Deer, Boar, Marmoset
        PerchedBird,     // Parrot, Toucan
        TreeFrog,        // Jungle Frog
        Butterfly        // Flapping Butterfly
    }

    /// <summary>
    /// Lightweight, mobile-optimized ambient AI for non-hostile jungle wildlife.
    /// Uses pure transform kinematics (0 NavMesh dependency) to wander, forage, perch, leap, or flutter.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/AI/Wildlife AI")]
    [DisallowMultipleComponent]
    public class WildlifeAI : MonoBehaviour
    {
        [Header("Wildlife Behavior")]
        [SerializeField] private WildlifeType wildlifeType = WildlifeType.WanderingAnimal;
        [SerializeField] private float wanderRadius = 6.0f;
        [SerializeField] private float moveSpeed = 1.8f;
        [SerializeField] private float idleDurationMin = 2.0f;
        [SerializeField] private float idleDurationMax = 5.0f;

        [Header("Fleeing / Player Awareness")]
        [SerializeField] private float fleeDistance = 4.0f;
        [SerializeField] private float fleeSpeed = 3.5f;

        private Vector3 _homePosition;
        private Vector3 _targetPosition;
        private bool _isMoving = false;
        private bool _isFleeing = false;
        private Transform _playerTransform;
        private float _nextActionTime;
        private float _frogLeapTimer;
        private float _butterflyNoiseOffset;

        private void Start()
        {
            _homePosition = transform.position;
            _butterflyNoiseOffset = Random.Range(0f, 100f);
            _nextActionTime = Time.time + Random.Range(0.5f, 2.0f);

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        private void Update()
        {
            switch (wildlifeType)
            {
                case WildlifeType.WanderingAnimal:
                    HandleWanderingBehavior();
                    break;
                case WildlifeType.PerchedBird:
                    HandleBirdBehavior();
                    break;
                case WildlifeType.TreeFrog:
                    HandleFrogBehavior();
                    break;
                case WildlifeType.Butterfly:
                    HandleButterflyBehavior();
                    break;
            }
        }

        private void HandleWanderingBehavior()
        {
            // Check player proximity to flee
            if (_playerTransform != null)
            {
                float distToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
                if (distToPlayer < fleeDistance)
                {
                    _isFleeing = true;
                    Vector3 fleeDir = (transform.position - _playerTransform.position).normalized;
                    fleeDir.y = 0;
                    _targetPosition = transform.position + fleeDir * 4.0f;
                }
                else if (_isFleeing && distToPlayer > fleeDistance * 1.5f)
                {
                    _isFleeing = false;
                }
            }

            if (_isMoving || _isFleeing)
            {
                float currentSpeed = _isFleeing ? fleeSpeed : moveSpeed;
                Vector3 moveDir = (_targetPosition - transform.position);
                moveDir.y = 0;

                if (moveDir.sqrMagnitude > 0.05f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
                    transform.position += transform.forward * (currentSpeed * Time.deltaTime);
                }
                else
                {
                    _isMoving = false;
                    _nextActionTime = Time.time + Random.Range(idleDurationMin, idleDurationMax);
                }
            }
            else if (Time.time >= _nextActionTime)
            {
                // Pick new random wander point around home
                Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
                _targetPosition = _homePosition + new Vector3(randomCircle.x, 0, randomCircle.y);
                _isMoving = true;
            }
        }

        private void HandleBirdBehavior()
        {
            // Occasional head bob / turn
            if (Time.time >= _nextActionTime)
            {
                float randomAngle = Random.Range(-45f, 45f);
                transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + randomAngle, 0);
                _nextActionTime = Time.time + Random.Range(1.5f, 4.0f);
            }
        }

        private void HandleFrogBehavior()
        {
            _frogLeapTimer += Time.deltaTime;
            if (_frogLeapTimer >= 3.0f)
            {
                _frogLeapTimer = 0;
                StartCoroutine(FrogLeapRoutine());
            }
        }

        private IEnumerator FrogLeapRoutine()
        {
            Vector3 startPos = transform.position;
            Vector3 hopDir = transform.forward + Vector3.up * 1.2f;
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float height = Mathf.Sin(t * Mathf.PI) * 0.8f;
                transform.position = Vector3.Lerp(startPos, startPos + transform.forward * 1.2f, t) + Vector3.up * height;
                yield return null;
            }

            // Small random turn
            transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        }

        private void HandleButterflyBehavior()
        {
            // Smooth harmonic hovering motion
            float time = Time.time + _butterflyNoiseOffset;
            float xOffset = Mathf.Sin(time * 1.2f) * 1.5f;
            float yOffset = Mathf.Sin(time * 3.0f) * 0.4f + Mathf.Cos(time * 1.5f) * 0.2f;
            float zOffset = Mathf.Cos(time * 0.9f) * 1.5f;

            Vector3 targetHover = _homePosition + new Vector3(xOffset, yOffset + 1.2f, zOffset);
            Vector3 moveDelta = targetHover - transform.position;

            if (moveDelta.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDelta), Time.deltaTime * 6f);
            }

            transform.position = Vector3.Lerp(transform.position, targetHover, Time.deltaTime * 3f);
        }
    }
}
