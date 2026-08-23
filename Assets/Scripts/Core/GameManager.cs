using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MonkeyAdventure.Core
{
    /// <summary>
    /// Central manager for Act 1: The Awakening.
    /// Tracks Food (Bananas), Coins, Player status, Checkpoints, and falling off map respawn.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Core/Game Manager")]
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Player & Respawn System")]
        [Tooltip("Reference to the player Transform. Auto-found by 'Player' tag if unassigned.")]
        [SerializeField] private Transform playerTransform;

        [Tooltip("Y-coordinate threshold below which the player is considered fallen and will respawn.")]
        [SerializeField] private float fallDeathYThreshold = -10f;

        [Tooltip("Delay in seconds before respawning the player after a fall.")]
        [SerializeField] private float respawnDelay = 0.5f;

        [Tooltip("Optional particle effect spawned at the checkpoint when the player respawns.")]
        [SerializeField] private GameObject respawnVFXPrefab;

        [Tooltip("Optional audio clip played upon respawning.")]
        [SerializeField] private AudioClip respawnSound;

        [Header("Act 1 Progression & Collectibles")]
        [SerializeField] private int foodCount = 0;
        [SerializeField] private int coinCount = 0;

        [Header("Debug & On-Screen UI")]
        [Tooltip("Shows a lightweight on-screen HUD for testing food, coins, and checkpoints.")]
        [SerializeField] private bool showDebugHUD = true;

        // Checkpoint internal state
        private Vector3 _currentCheckpointPosition;
        private Quaternion _currentCheckpointRotation;
        private bool _hasCheckpointSet = false;
        private bool _isRespawning = false;
        private AudioSource _audioSource;

        #region Events for UI and Systems
        public event Action<int> OnFoodCountChanged;
        public event Action<int> OnCoinCountChanged;
        public event Action OnPlayerRespawned;
        #endregion

        #region Public Properties
        public int FoodCount => foodCount;
        public int CoinCount => coinCount;
        public Vector3 CurrentCheckpointPosition => _currentCheckpointPosition;
        public Transform PlayerTransform => playerTransform;
        #endregion

        private void Awake()
        {
            // Singleton Pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            // Find player if not assigned in Inspector
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                }
                else
                {
                    Debug.LogWarning("[GameManager] No Player GameObject tagged 'Player' found in scene.", this);
                }
            }

            // Set initial spawn point as default checkpoint
            if (playerTransform != null)
            {
                _currentCheckpointPosition = playerTransform.position;
                _currentCheckpointRotation = playerTransform.rotation;
                _hasCheckpointSet = true;
            }
        }

        private void Update()
        {
            CheckFallDeath();
        }

        /// <summary>
        /// Continuously monitors player height to detect falling off platforms.
        /// </summary>
        private void CheckFallDeath()
        {
            if (playerTransform == null || _isRespawning) return;

            if (playerTransform.position.y < fallDeathYThreshold)
            {
                StartCoroutine(RespawnPlayerRoutine());
            }
        }

        #region Checkpoints & Respawn
        /// <summary>
        /// Updates the current active checkpoint coordinates.
        /// </summary>
        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            _currentCheckpointPosition = position;
            _currentCheckpointRotation = rotation;
            _hasCheckpointSet = true;
            Debug.Log($"[GameManager] Checkpoint set at: {position}");
        }

        /// <summary>
        /// Coroutine to smoothly teleport the player back to the last touched checkpoint.
        /// </summary>
        public IEnumerator RespawnPlayerRoutine()
        {
            if (_isRespawning || playerTransform == null) yield break;

            _isRespawning = true;

            yield return new WaitForSeconds(respawnDelay);

            // 1. Temporarily disable CharacterController / Kinematics if present to avoid teleport conflicts
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                #if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Vector3.zero;
                #else
                rb.velocity = Vector3.zero;
                #endif
                rb.angularVelocity = Vector3.zero;
            }

            // 2. Teleport player to checkpoint
            Vector3 targetPos = _hasCheckpointSet ? _currentCheckpointPosition : Vector3.zero;
            Quaternion targetRot = _hasCheckpointSet ? _currentCheckpointRotation : Quaternion.identity;

            playerTransform.position = targetPos;
            playerTransform.rotation = targetRot;

            if (cc != null) cc.enabled = true;

            // 3. Spawn Respawn VFX & Audio
            if (respawnVFXPrefab != null)
            {
                GameObject vfx = Instantiate(respawnVFXPrefab, targetPos, Quaternion.identity);
                Destroy(vfx, 3f);
            }

            if (respawnSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(respawnSound);
            }

            OnPlayerRespawned?.Invoke();
            _isRespawning = false;
        }

        public void TriggerManualRespawn()
        {
            StartCoroutine(RespawnPlayerRoutine());
        }
        #endregion

        #region Collectibles API
        /// <summary>
        /// Adds bananas/food to inventory and notifies listeners.
        /// </summary>
        public void AddFood(int amount = 1)
        {
            foodCount += amount;
            OnFoodCountChanged?.Invoke(foodCount);
            Debug.Log($"[GameManager] Food Collected! Total: {foodCount}");
        }

        /// <summary>
        /// Adds coins to inventory and notifies listeners.
        /// </summary>
        public void AddCoins(int amount = 1)
        {
            coinCount += amount;
            OnCoinCountChanged?.Invoke(coinCount);
            Debug.Log($"[GameManager] Coin Collected! Total: {coinCount}");
        }
        #endregion

        #region Debug OnGUI HUD
        private void OnGUI()
        {
            if (!showDebugHUD) return;

            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                normal = { textColor = Color.white }
            };

            GUILayout.BeginArea(new Rect(15, 15, 220, 110), GUI.skin.box);
            GUILayout.Label("🍌 <b>Act 1 - Jungle HUD</b>", style);
            GUILayout.Label($"Food (Bananas): <b>{foodCount}</b>", style);
            GUILayout.Label($"Coins: <b>{coinCount}</b>", style);
            if (GUILayout.Button("Respawn at Checkpoint"))
            {
                TriggerManualRespawn();
            }
            GUILayout.EndArea();
        }
        #endregion
    }
}
