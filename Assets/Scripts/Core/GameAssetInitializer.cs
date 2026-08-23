using System;
using System.Collections.Generic;
using UnityEngine;
using MonkeyAdventure.AI;
using MonkeyAdventure.Hazards;
using MonkeyAdventure.Skins;
using MonkeyAdventure.Environment;
using GuardianSystem.Combat;

namespace MonkeyAdventure.Core
{
    [Serializable]
    public class EntityCategory
    {
        public string categoryName;
        public List<GameObject> prefabs = new List<GameObject>();
    }

    /// <summary>
    /// Central Asset Manager and Auto-Binder for all game entities across all 50 levels and Endless Mode.
    /// Manages Enemies, Poachers, Obstacles, Props, Evolution Skins, and runtime mobile Object Pooling.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Core/Game Asset Initializer")]
    [DisallowMultipleComponent]
    public class GameAssetInitializer : MonoBehaviour
    {
        public static GameAssetInitializer Instance { get; private set; }

        [Header("1. Enemies & Forest Creatures")]
        [Tooltip("Alpha Jaguar, River Serpent, Shadow Beast, Corrupted Swarm, Forest Predators.")]
        [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();

        [Header("2. Poachers, Cages & Traps")]
        [Tooltip("Poacher guards, rescue cages, bear traps, spikes.")]
        [SerializeField] private List<GameObject> poacherAndTrapPrefabs = new List<GameObject>();

        [Header("3. Environment & Interactive Props")]
        [Tooltip("Boulders, Floating Logs, Fire Hazards, SafeZone Crystals, Magic Updrafts.")]
        [SerializeField] private List<GameObject> environmentPropPrefabs = new List<GameObject>();

        [Header("4. Evolution Skin Models")]
        [Tooltip("0: Base Monkey, 1: Guardian, 2: King Kong, 3: Hanuman.")]
        [SerializeField] private List<GameObject> evolutionSkinPrefabs = new List<GameObject>();

        [Header("Mobile Object Pooling Configuration")]
        [Tooltip("Pre-instantiate instances in memory to eliminate runtime GC lag on Android/iOS.")]
        [SerializeField] private bool enableObjectPooling = true;
        [SerializeField] private int defaultPoolSizePerPrefab = 5;

        // Internal Pool lookup table
        private readonly Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, GameObject> _prefabLookup = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        private Transform _poolContainer;

        public List<GameObject> EnemyPrefabs => enemyPrefabs;
        public List<GameObject> PoacherAndTrapPrefabs => poacherAndTrapPrefabs;
        public List<GameObject> EnvironmentPropPrefabs => environmentPropPrefabs;
        public List<GameObject> EvolutionSkinPrefabs => evolutionSkinPrefabs;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeContainer();
            RegisterAllPrefabs();
            ValidatePrefabIntegrity();

            if (enableObjectPooling)
            {
                PrewarmObjectPools();
            }
        }

        private void InitializeContainer()
        {
            GameObject container = new GameObject("[_Mobile_Object_Pool_Container_]");
            container.transform.SetParent(transform);
            _poolContainer = container.transform;
        }

        private void RegisterAllPrefabs()
        {
            RegisterCategoryList(enemyPrefabs);
            RegisterCategoryList(poacherAndTrapPrefabs);
            RegisterCategoryList(environmentPropPrefabs);
            RegisterCategoryList(evolutionSkinPrefabs);
        }

        private void RegisterCategoryList(List<GameObject> list)
        {
            if (list == null) return;
            foreach (var prefab in list)
            {
                if (prefab != null && !_prefabLookup.ContainsKey(prefab.name))
                {
                    _prefabLookup.Add(prefab.name, prefab);
                }
            }
        }

        /// <summary>
        /// Validates that assigned entity prefabs have their required AI, hazard, and health scripts.
        /// </summary>
        private void ValidatePrefabIntegrity()
        {
            int validCount = 0;

            // Validate Enemies
            foreach (var p in enemyPrefabs)
            {
                if (p == null) continue;
                if (p.GetComponent<EnemyAI>() == null && p.GetComponent<IDamageable>() == null)
                {
                    Debug.LogWarning($"[GameAssetInitializer] Enemy prefab '{p.name}' is missing EnemyAI or IDamageable component!", p);
                }
                else
                {
                    validCount++;
                }
            }

            // Validate Environment & Hazards
            foreach (var p in environmentPropPrefabs)
            {
                if (p == null) continue;
                if (p.name.ToLower().Contains("fire") && p.GetComponent<FireHazard>() == null)
                {
                    Debug.LogWarning($"[GameAssetInitializer] Fire prefab '{p.name}' is missing FireHazard component!", p);
                }
                validCount++;
            }

            Debug.Log($"[GameAssetInitializer] Entity validation complete! Verified {validCount} core production prefabs.");
        }

        #region Mobile Object Pooling System
        private void PrewarmObjectPools()
        {
            foreach (var kvp in _prefabLookup)
            {
                string key = kvp.Key;
                GameObject prefab = kvp.Value;

                if (!_poolDictionary.ContainsKey(key))
                {
                    _poolDictionary[key] = new Queue<GameObject>();
                }

                for (int i = 0; i < defaultPoolSizePerPrefab; i++)
                {
                    GameObject instance = Instantiate(prefab, _poolContainer);
                    instance.name = key; // Preserve key name
                    instance.SetActive(false);
                    _poolDictionary[key].Enqueue(instance);
                }
            }

            Debug.Log($"[GameAssetInitializer] Object Pool pre-warmed for {_poolDictionary.Count} prefab types!");
        }

        /// <summary>
        /// Spawns a pooled instance of an entity by name.
        /// </summary>
        public GameObject SpawnEntity(string entityName, Vector3 position, Quaternion rotation)
        {
            if (!_prefabLookup.ContainsKey(entityName))
            {
                Debug.LogWarning($"[GameAssetInitializer] Entity '{entityName}' not found in asset hub!");
                return null;
            }

            GameObject obj = null;

            if (enableObjectPooling && _poolDictionary.TryGetValue(entityName, out var pool) && pool.Count > 0)
            {
                obj = pool.Dequeue();
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
            }
            else
            {
                // Instantiate new instance if pool empty
                GameObject prefab = _prefabLookup[entityName];
                obj = Instantiate(prefab, position, rotation);
                obj.name = entityName;
            }

            return obj;
        }

        /// <summary>
        /// Recycles an entity back into the mobile object pool instead of destroying it.
        /// </summary>
        public void DespawnEntity(GameObject instance)
        {
            if (instance == null) return;

            string key = instance.name.Replace("(Clone)", "").Trim();

            if (enableObjectPooling)
            {
                instance.SetActive(false);
                instance.transform.SetParent(_poolContainer);

                if (!_poolDictionary.ContainsKey(key))
                {
                    _poolDictionary[key] = new Queue<GameObject>();
                }

                _poolDictionary[key].Enqueue(instance);
            }
            else
            {
                Destroy(instance);
            }
        }
        #endregion
    }
}
