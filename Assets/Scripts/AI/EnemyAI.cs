using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using GuardianSystem.Combat;
using MonkeyAdventure.Player;

namespace MonkeyAdventure.AI
{
    public enum AIState
    {
        Patrol,
        Chase,
        Attack
    }

    /// <summary>
    /// EnemyAI for Jungle Predators using UnityEngine.AI.NavMeshAgent.
    /// Handles 3 states: Patrol (waypoints), Chase (escaping gameplay), and Attack (close-quarters damage).
    /// Implements IDamageable to respond to Guardian combat abilities.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/AI/Enemy AI")]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour, IDamageable
    {
        [Header("AI State")]
        [SerializeField] private AIState currentState = AIState.Patrol;

        [Header("Health & Durability")]
        [SerializeField] private int maxHealth = 60;
        [SerializeField] private int currentHealth;
        [SerializeField] private GameObject deathVFXPrefab;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip deathSound;

        [Header("Patrol State Settings")]
        [Tooltip("Waypoints for the enemy to patrol between.")]
        [SerializeField] private List<Transform> patrolWaypoints = new List<Transform>();

        [Tooltip("Patrol movement speed.")]
        [SerializeField] private float patrolSpeed = 3.0f;

        [Tooltip("Wait time at each patrol waypoint in seconds.")]
        [SerializeField] private float waypointWaitTime = 1.5f;

        [Header("Chase State Settings")]
        [Tooltip("Radius within which the predator detects and chases the player.")]
        [SerializeField] private float detectionRadius = 8.0f;

        [Tooltip("Distance at which the predator loses sight of the player and resumes patrol.")]
        [SerializeField] private float losePlayerRadius = 12.0f;

        [Tooltip("Chase movement speed.")]
        [SerializeField] private float chaseSpeed = 5.5f;

        [Tooltip("Tag identifying the player character.")]
        [SerializeField] private string playerTag = "Player";

        [Header("Attack State Settings")]
        [Tooltip("Distance from player required to initiate an attack.")]
        [SerializeField] private float attackRange = 1.8f;

        [Tooltip("Damage dealt per attack.")]
        [SerializeField] private int attackDamage = 20;

        [Tooltip("Cooldown period between attacks.")]
        [SerializeField] private float attackCooldown = 1.2f;

        [Tooltip("Sound played when performing an attack.")]
        [SerializeField] private AudioClip attackSound;

        [Tooltip("Particle effect spawned on the player when struck.")]
        [SerializeField] private GameObject attackHitVFX;

        [Header("Gizmos")]
        [SerializeField] private bool drawGizmos = true;

        private NavMeshAgent _agent;
        private Transform _playerTransform;
        private PlayerHealth _playerHealth;
        private IDamageable _playerDamageable;
        private int _currentWaypointIndex = 0;
        private bool _isWaitingAtWaypoint = false;
        private float _nextAttackTime = 0f;
        private bool _isDead = false;
        private AudioSource _audioSource;

        private bool IsNavMeshValid()
        {
            return _agent != null && _agent.enabled && _agent.isOnNavMesh;
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            currentHealth = maxHealth;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f; // 3D sound
                _audioSource.playOnAwake = false;
            }

            // Ensure enemy tag is set for combat targeting
            if (!gameObject.CompareTag("Enemy"))
            {
                gameObject.tag = "Enemy";
            }
        }

        private void Start()
        {
            FindPlayerReference();

            // Attempt safe NavMesh placement
            if (_agent != null)
            {
                if (!_agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
                    {
                        _agent.Warp(hit.position);
                    }
                }

                if (IsNavMeshValid())
                {
                    _agent.speed = patrolSpeed;
                }
            }

            if (patrolWaypoints.Count > 0)
            {
                SetNextPatrolDestination();
            }
        }

        private void Update()
        {
            if (_isDead) return;

            if (_playerTransform == null)
            {
                FindPlayerReference();
                if (_playerTransform == null) return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            switch (currentState)
            {
                case AIState.Patrol:
                    HandlePatrolState(distanceToPlayer);
                    break;

                case AIState.Chase:
                    HandleChaseState(distanceToPlayer);
                    break;

                case AIState.Attack:
                    HandleAttackState(distanceToPlayer);
                    break;
            }
        }

        #region State Handlers
        private void HandlePatrolState(float distanceToPlayer)
        {
            // Transition to Chase if player enters detection radius
            if (distanceToPlayer <= detectionRadius)
            {
                TransitionToState(AIState.Chase);
                return;
            }

            // Patrol waypoint traversal
            if (patrolWaypoints.Count == 0) return;

            if (IsNavMeshValid())
            {
                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                {
                    if (!_isWaitingAtWaypoint)
                    {
                        StartCoroutine(WaitAtWaypointRoutine());
                    }
                }
            }
            else
            {
                // Robust Fallback: direct transform movement between waypoints when NavMesh is absent
                Transform targetWp = patrolWaypoints[_currentWaypointIndex];
                if (targetWp != null)
                {
                    Vector3 targetPos = targetWp.position;
                    targetPos.y = transform.position.y;
                    float dist = Vector3.Distance(transform.position, targetPos);
                    if (dist <= 0.6f)
                    {
                        if (!_isWaitingAtWaypoint)
                        {
                            StartCoroutine(WaitAtWaypointRoutine());
                        }
                    }
                    else
                    {
                        Vector3 moveDir = (targetPos - transform.position).normalized;
                        transform.position += moveDir * (patrolSpeed * Time.deltaTime);
                        if (moveDir != Vector3.zero)
                        {
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 6f);
                        }
                    }
                }
            }
        }

        private void HandleChaseState(float distanceToPlayer)
        {
            // Transition to Patrol if player escaped
            if (distanceToPlayer > losePlayerRadius)
            {
                TransitionToState(AIState.Patrol);
                return;
            }

            // Transition to Attack if in attack range
            if (distanceToPlayer <= attackRange)
            {
                TransitionToState(AIState.Attack);
                return;
            }

            // Continuously update destination to player's position
            if (IsNavMeshValid())
            {
                _agent.SetDestination(_playerTransform.position);
            }
            else
            {
                // Robust Fallback: direct transform pursuit
                Vector3 targetPos = _playerTransform.position;
                targetPos.y = transform.position.y;
                Vector3 moveDir = (targetPos - transform.position).normalized;
                transform.position += moveDir * (chaseSpeed * Time.deltaTime);
                if (moveDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 8f);
                }
            }
        }

        private void HandleAttackState(float distanceToPlayer)
        {
            // Transition back to Chase if player moved out of attack range
            if (distanceToPlayer > attackRange)
            {
                TransitionToState(AIState.Chase);
                return;
            }

            // Stop moving during attack
            if (IsNavMeshValid())
            {
                _agent.ResetPath();
            }

            // Face the player
            Vector3 lookDirection = (_playerTransform.position - transform.position).normalized;
            lookDirection.y = 0f;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 8f);
            }

            // Execute Attack if cooldown ready
            if (Time.time >= _nextAttackTime)
            {
                ExecuteAttack();
            }
        }

        private void TransitionToState(AIState newState)
        {
            currentState = newState;

            switch (newState)
            {
                case AIState.Patrol:
                    if (IsNavMeshValid())
                    {
                        _agent.speed = patrolSpeed;
                    }
                    SetNextPatrolDestination();
                    break;

                case AIState.Chase:
                    if (IsNavMeshValid())
                    {
                        _agent.speed = chaseSpeed;
                        _agent.SetDestination(_playerTransform.position);
                    }
                    _isWaitingAtWaypoint = false;
                    break;

                case AIState.Attack:
                    if (IsNavMeshValid())
                    {
                        _agent.ResetPath();
                    }
                    break;
            }
        }
        #endregion

        #region Actions
        private void ExecuteAttack()
        {
            _nextAttackTime = Time.time + attackCooldown;

            // Play Attack SFX
            if (attackSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(attackSound);
            }

            // Deal Damage to Player
            if (_playerDamageable != null)
            {
                _playerDamageable.TakeDamage(attackDamage);
            }
            else if (_playerHealth != null)
            {
                _playerHealth.TakeDamage(attackDamage);
            }
            else if (_playerTransform != null)
            {
                _playerTransform.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
            }

            // Spawn Hit VFX
            if (attackHitVFX != null && _playerTransform != null)
            {
                GameObject vfx = Instantiate(attackHitVFX, _playerTransform.position + Vector3.up * 1f, Quaternion.identity);
                Destroy(vfx, 2f);
            }

            Debug.Log($"[EnemyAI] '{name}' attacked Player for {attackDamage} damage!", this);
        }

        private IEnumerator WaitAtWaypointRoutine()
        {
            _isWaitingAtWaypoint = true;
            yield return new WaitForSeconds(waypointWaitTime);

            _currentWaypointIndex = (_currentWaypointIndex + 1) % patrolWaypoints.Count;
            SetNextPatrolDestination();

            _isWaitingAtWaypoint = false;
        }

        private void SetNextPatrolDestination()
        {
            if (patrolWaypoints.Count == 0) return;

            Transform target = patrolWaypoints[_currentWaypointIndex];
            if (target != null && IsNavMeshValid())
            {
                _agent.SetDestination(target.position);
            }
        }

        private void FindPlayerReference()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                _playerTransform = playerObj.transform;
                _playerHealth = playerObj.GetComponent<PlayerHealth>();
                _playerDamageable = playerObj.GetComponent<IDamageable>();
            }
        }
        #endregion

        #region IDamageable Implementation (Combat Reception)
        public void TakeDamage(int amount)
        {
            if (_isDead) return;

            currentHealth -= amount;

            if (hitSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(hitSound);
            }

            // If attacked while on patrol, immediately aggro and chase player!
            if (currentState == AIState.Patrol && _playerTransform != null)
            {
                TransitionToState(AIState.Chase);
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (_isDead) return;
            _isDead = true;

            if (deathSound != null)
            {
                AudioSource.PlayClipAtPoint(deathSound, transform.position);
            }

            if (deathVFXPrefab != null)
            {
                GameObject vfx = Instantiate(deathVFXPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                Destroy(vfx, 3f);
            }

            if (_agent != null)
            {
                _agent.enabled = false;
            }
            Destroy(gameObject, 0.1f);
        }
        #endregion

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            // Detection Radius (Yellow)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            // Lose Player Radius (Orange)
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, losePlayerRadius);

            // Attack Range (Red)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw patrol lines between waypoints
            if (patrolWaypoints != null && patrolWaypoints.Count > 1)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < patrolWaypoints.Count; i++)
                {
                    if (patrolWaypoints[i] != null)
                    {
                        Gizmos.DrawSphere(patrolWaypoints[i].position, 0.3f);
                        int next = (i + 1) % patrolWaypoints.Count;
                        if (patrolWaypoints[next] != null)
                        {
                            Gizmos.DrawLine(patrolWaypoints[i].position, patrolWaypoints[next].position);
                        }
                    }
                }
            }
        }
    }
}
