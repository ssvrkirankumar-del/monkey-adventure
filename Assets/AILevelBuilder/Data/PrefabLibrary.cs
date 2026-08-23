using System;
using System.Collections.Generic;
using UnityEngine;
using MonkeyAdventure.AILevelBuilder;

namespace MonkeyAdventure.AILevelBuilder
{
    /// <summary>
    /// Central catalog and registry for reusable level prefabs across all 50 levels.
    /// Supports categorized asset retrieval for procedural generation and AI level assembly.
    /// </summary>
    [CreateAssetMenu(fileName = "New_PrefabLibrary", menuName = "Monkey Adventure/AI Level Builder/Prefab Library", order = 11)]
    public class PrefabLibrary : ScriptableObject
    {
        [Serializable]
        public class PrefabEntry
        {
            [Tooltip("Unique string identifier for this prefab asset.")]
            public string entryId = "prop_id";

            [Tooltip("Object category.")]
            public ObjectCategory category = ObjectCategory.Environment;

            [Tooltip("Prefab asset reference.")]
            public GameObject prefab;

            [Tooltip("Optional descriptive tags or notes.")]
            public string description = "";
        }

        [Header("Registered Level Prefabs")]
        public List<PrefabEntry> entries = new List<PrefabEntry>();

        /// <summary>
        /// Retrieves a prefab by its unique entry ID.
        /// </summary>
        public GameObject GetPrefabById(string id)
        {
            if (string.IsNullOrEmpty(id) || entries == null) return null;
            var entry = entries.Find(e => e.entryId.Equals(id, StringComparison.OrdinalIgnoreCase));
            return entry != null ? entry.prefab : null;
        }

        /// <summary>
        /// Retrieves all prefabs matching a specific category.
        /// </summary>
        public List<GameObject> GetPrefabsByCategory(ObjectCategory category)
        {
            List<GameObject> results = new List<GameObject>();
            if (entries == null) return results;

            foreach (var entry in entries)
            {
                if (entry.category == category && entry.prefab != null)
                {
                    results.Add(entry.prefab);
                }
            }
            return results;
        }

        /// <summary>
        /// Retrieves a random prefab from the specified category.
        /// </summary>
        public GameObject GetRandomPrefabByCategory(ObjectCategory category)
        {
            var list = GetPrefabsByCategory(category);
            if (list == null || list.Count == 0) return null;
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Adds or updates a prefab entry in the library.
        /// </summary>
        public void RegisterPrefab(string id, ObjectCategory category, GameObject prefab, string description = "")
        {
            if (string.IsNullOrEmpty(id) || prefab == null) return;
            if (entries == null) entries = new List<PrefabEntry>();

            var existing = entries.Find(e => e.entryId.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.category = category;
                existing.prefab = prefab;
                existing.description = description;
            }
            else
            {
                entries.Add(new PrefabEntry
                {
                    entryId = id,
                    category = category,
                    prefab = prefab,
                    description = description
                });
            }
        }
    }
}
