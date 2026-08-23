using System.Collections.Generic;
using UnityEngine;
using MonkeyAdventure.Core;

namespace MonkeyAdventure.Endless
{
    /// <summary>
    /// Endless Mode Level Generator (Levels 51-100 Expansion).
    /// Procedurally spawns modular jungle segments ahead of the player while recycling
    /// passed tiles behind to optimize mobile memory.
    /// Gradually ramps up game speed every 30 seconds for escalating challenge.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Endless/Endless Level Generator")]
    public class EndlessLevelGenerator : MonoBehaviour
    {
        [Header("Modular Tile Prefabs")]
        [Tooltip("Collection of procedural jungle path segments (with coins, gaps, obstacles, vines).")]
        [SerializeField] private GameObject[] tilePrefabs;

        [Tooltip("Safe starting tile prefab with no obstacles.")]
        [SerializeField] private GameObject startTilePrefab;

        [Header("Generation Parameters")]
        [Tooltip("Length of each tile segment in meters along the Z-axis.")]
        [SerializeField] private float tileLength = 20.0f;

        [Tooltip("Number of tiles visible simultaneously ahead of the player.")]
        [SerializeField] private int activeTileCount = 6;

        [Tooltip("Distance behind the player before a tile is recycled/destroyed.")]
        [SerializeField] private float despawnDistance = 30.0f;

        [Header("Difficulty Ramp Settings")]
        [Tooltip("Interval in seconds between speed difficulty increases.")]
        [SerializeField] private float speedRampInterval = 30.0f;

        [Tooltip("Speed multiplier added every 30 seconds.")]
        [SerializeField] private float speedIncrement = 0.05f;

        [Tooltip("Maximum allowed speed multiplier cap.")]
        [SerializeField] private float maxSpeedMultiplier = 2.0f;

        [Header("Distance & High Score")]
        [SerializeField] private float currentDistance = 0f;
        [SerializeField] private float bestDistance = 0f;

        private const string ENDLESS_HIGHSCORE_KEY = "MonkeyAdventure_EndlessHighScore";

        private Transform _playerTransform;
        private float _spawnZ = 0f;
        private readonly List<GameObject> _activeTiles = new List<GameObject>();
        private float _speedTimer = 0f;
        private float _currentSpeedMultiplier = 1.0f;
        private bool _isEndlessActive = true;

        public float CurrentDistance => currentDistance;
        public float BestDistance => bestDistance;
        public float SpeedMultiplier => _currentSpeedMultiplier;

        private void Start()
        {
            FindPlayer();

            bestDistance = PlayerPrefs.GetFloat(ENDLESS_HIGHSCORE_KEY, 0f);

            // Spawn Initial Safe Tiles
            for (int i = 0; i < activeTileCount; i++)
            {
                if (i == 0 && startTilePrefab != null)
                {
                    SpawnTile(startTilePrefab);
                }
                else
                {
                    SpawnRandomTile();
                }
            }
        }

        private void Update()
        {
            if (!_isEndlessActive) return;

            if (_playerTransform == null)
            {
                FindPlayer();
                if (_playerTransform == null) return;
            }

            // 1. Track Distance in Meters
            currentDistance = Mathf.Max(currentDistance, _playerTransform.position.z);
            if (currentDistance > bestDistance)
            {
                bestDistance = currentDistance;
                PlayerPrefs.SetFloat(ENDLESS_HIGHSCORE_KEY, bestDistance);
            }

            // 2. Check if player has advanced far enough to spawn new tile ahead
            if (_playerTransform.position.z + (activeTileCount * tileLength * 0.7f) > _spawnZ)
            {
                SpawnRandomTile();
                RecycleOldestTile();
            }

            // 3. Difficulty Speed Ramp (every 30s)
            _speedTimer += Time.deltaTime;
            if (_speedTimer >= speedRampInterval)
            {
                _speedTimer = 0f;
                RampUpDifficulty();
            }
        }

        private void SpawnTile(GameObject prefab)
        {
            if (prefab == null) return;

            GameObject tile = Instantiate(prefab, transform.forward * _spawnZ, Quaternion.identity, transform);
            _activeTiles.Add(tile);
            _spawnZ += tileLength;
        }

        private void SpawnRandomTile()
        {
            if (tilePrefabs == null || tilePrefabs.Length == 0) return;

            GameObject randomPrefab = tilePrefabs[Random.Range(0, tilePrefabs.Length)];
            SpawnTile(randomPrefab);
        }

        private void RecycleOldestTile()
        {
            if (_activeTiles.Count == 0) return;

            GameObject oldest = _activeTiles[0];

            // If oldest tile is far enough behind player, destroy/recycle it
            if (_playerTransform != null && (_playerTransform.position.z - oldest.transform.position.z > despawnDistance))
            {
                _activeTiles.RemoveAt(0);
                Destroy(oldest);
            }
        }

        private void RampUpDifficulty()
        {
            if (_currentSpeedMultiplier < maxSpeedMultiplier)
            {
                _currentSpeedMultiplier = Mathf.Min(maxSpeedMultiplier, _currentSpeedMultiplier + speedIncrement);
                Time.timeScale = _currentSpeedMultiplier;
                Debug.Log($"[EndlessLevelGenerator] SPEED INCREASE! New Speed Multiplier: {_currentSpeedMultiplier:F2}x (Distance: {currentDistance:F0}m)");
            }
        }

        private void FindPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        private void OnDestroy()
        {
            // Reset timescale on exit
            Time.timeScale = 1.0f;
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                normal = { textColor = Color.yellow }
            };

            GUILayout.BeginArea(new Rect(Screen.width - 240, 15, 220, 80), GUI.skin.box);
            GUILayout.Label($"🏃 <b>Distance:</b> {currentDistance:F0} m", style);
            GUILayout.Label($"🏆 <b>Best:</b> {bestDistance:F0} m", style);
            GUILayout.Label($"⚡ <b>Speed:</b> {_currentSpeedMultiplier:F2}x", style);
            GUILayout.EndArea();
        }
    }
}
