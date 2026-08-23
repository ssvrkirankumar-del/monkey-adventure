using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonkeyAdventure.AILevelBuilder
{
    /// <summary>
    /// Supported environment asset categories for complete HD jungle level replacement.
    /// </summary>
    public enum HDObjectCategory
    {
        Tree,
        Rock,
        RiverRock,
        Grass,
        DeadLeaves,
        Bush,
        Ground,
        Water,
        Waterfall,
        WoodTrunk,
        AncientStone,
        Arch,
        Other
    }

    /// <summary>
    /// Category-specific replacement configuration including prefabs, scale multipliers, and ground offsets.
    /// </summary>
    [Serializable]
    public class HDCategoryMapping
    {
        public HDObjectCategory category;
        public List<GameObject> prefabs = new List<GameObject>();
        [Range(0.1f, 5f)] public float scaleMultiplier = 1.0f;
        public float verticalOffset = 0f;
        public bool randomRotationY = true;
        public bool randomScaleVariation = true;
        public Vector2 scaleVariationRange = new Vector2(0.9f, 1.15f);

        public HDCategoryMapping(HDObjectCategory cat, float scale = 1.0f, float yOffset = 0f)
        {
            category = cat;
            prefabs = new List<GameObject>();
            scaleMultiplier = scale;
            verticalOffset = yOffset;
            randomRotationY = true;
            randomScaleVariation = true;
            scaleVariationRange = new Vector2(0.9f, 1.15f);
        }
    }

    [Serializable]
    public class HDDiscoveryReport
    {
        public int totalPrefabsDiscovered = 0;
        public int totalUsableHDPrefabs = 0;
        public Dictionary<HDObjectCategory, int> categoryCounts = new Dictionary<HDObjectCategory, int>();
        public List<string> missingCategories = new List<string>();
        public int urpCompatibleMaterials = 0;
        public int builtInStandardMaterials = 0;
    }

    /// <summary>
    /// ScriptableObject asset library mapping blockout environment categories to high-quality 3D prefabs.
    /// </summary>
    [CreateAssetMenu(fileName = "HDAssetLibrary", menuName = "Monkey Adventure/AI Level Builder/HD Asset Library", order = 20)]
    public class HDAssetLibrary : ScriptableObject
    {
        [Header("Category Prefab Mappings")]
        [SerializeField]
        private List<HDCategoryMapping> categoryMappings = new List<HDCategoryMapping>();

        public List<HDCategoryMapping> CategoryMappings => categoryMappings;

        private void OnEnable()
        {
            EnsureDefaultCategories();
        }

        private void Reset()
        {
            EnsureDefaultCategories();
        }

        /// <summary>
        /// Ensures all 13 standard categories are present in the library with appropriate defaults.
        /// </summary>
        public void EnsureDefaultCategories()
        {
            if (categoryMappings == null) categoryMappings = new List<HDCategoryMapping>();

            Array categories = Enum.GetValues(typeof(HDObjectCategory));
            foreach (HDObjectCategory cat in categories)
            {
                if (!categoryMappings.Exists(m => m.category == cat))
                {
                    float defaultScale = 1.0f;
                    float defaultOffset = 0f;

                    switch (cat)
                    {
                        case HDObjectCategory.Tree:
                            defaultScale = 1.0f;
                            break;
                        case HDObjectCategory.Rock:
                        case HDObjectCategory.RiverRock:
                            defaultScale = 1.0f;
                            break;
                        case HDObjectCategory.Grass:
                        case HDObjectCategory.DeadLeaves:
                            defaultScale = 1.2f;
                            break;
                        case HDObjectCategory.Bush:
                            defaultScale = 1.0f;
                            break;
                        case HDObjectCategory.AncientStone:
                        case HDObjectCategory.Arch:
                            defaultScale = 1.0f;
                            break;
                    }

                    categoryMappings.Add(new HDCategoryMapping(cat, defaultScale, defaultOffset));
                }
            }
        }

        /// <summary>
        /// Gets the mapping for a specific category.
        /// </summary>
        public HDCategoryMapping GetMapping(HDObjectCategory category)
        {
            EnsureDefaultCategories();
            return categoryMappings.Find(m => m.category == category);
        }

        /// <summary>
        /// Gets a deterministic prefab variation for the given category and seed index.
        /// </summary>
        public GameObject GetPrefab(HDObjectCategory category, int variantIndex)
        {
            var mapping = GetMapping(category);
            if (mapping == null || mapping.prefabs == null || mapping.prefabs.Count == 0)
            {
                // Fallback for specific categories if empty
                if (category == HDObjectCategory.RiverRock) return GetPrefab(HDObjectCategory.Rock, variantIndex);
                if (category == HDObjectCategory.Bush) return GetPrefab(HDObjectCategory.Grass, variantIndex);
                if (category == HDObjectCategory.DeadLeaves) return GetPrefab(HDObjectCategory.Grass, variantIndex);
                if (category == HDObjectCategory.Waterfall) return GetPrefab(HDObjectCategory.Water, variantIndex);
                if (category == HDObjectCategory.WoodTrunk) return GetPrefab(HDObjectCategory.Tree, variantIndex);
                if (category == HDObjectCategory.Arch) return GetPrefab(HDObjectCategory.AncientStone, variantIndex);
                return null;
            }

            // Filter valid prefabs
            List<GameObject> validPrefabs = mapping.prefabs.FindAll(p => p != null);
            if (validPrefabs.Count == 0) return null;

            int index = Mathf.Abs(variantIndex) % validPrefabs.Count;
            return validPrefabs[index];
        }

        /// <summary>
        /// Returns whether a category has at least one valid prefab assigned (or fallback available).
        /// </summary>
        public bool HasPrefabForCategory(HDObjectCategory category)
        {
            var mapping = GetMapping(category);
            if (mapping != null && mapping.prefabs != null && mapping.prefabs.Exists(p => p != null))
                return true;

            // Fallback checks
            if (category == HDObjectCategory.RiverRock) return HasPrefabForCategory(HDObjectCategory.Rock);
            if (category == HDObjectCategory.Bush || category == HDObjectCategory.DeadLeaves) return HasPrefabForCategory(HDObjectCategory.Grass);
            if (category == HDObjectCategory.Waterfall) return HasPrefabForCategory(HDObjectCategory.Water);
            if (category == HDObjectCategory.WoodTrunk) return HasPrefabForCategory(HDObjectCategory.Tree);
            if (category == HDObjectCategory.Arch) return HasPrefabForCategory(HDObjectCategory.AncientStone);

            return false;
        }

        /// <summary>
        /// Gets the count of assigned prefabs for a specific category.
        /// </summary>
        public int GetPrefabCountForCategory(HDObjectCategory category)
        {
            var mapping = GetMapping(category);
            if (mapping == null || mapping.prefabs == null) return 0;
            return mapping.prefabs.FindAll(p => p != null).Count;
        }

        /// <summary>
        /// Gets total assigned prefabs count across all categories.
        /// </summary>
        public int GetAssignedPrefabCount()
        {
            EnsureDefaultCategories();
            int count = 0;
            foreach (var mapping in categoryMappings)
            {
                if (mapping.prefabs != null)
                {
                    foreach (var p in mapping.prefabs)
                    {
                        if (p != null) count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Lists all categories that currently lack an assigned HD prefab.
        /// </summary>
        public List<HDObjectCategory> GetMissingCategories()
        {
            EnsureDefaultCategories();
            List<HDObjectCategory> missing = new List<HDObjectCategory>();
            foreach (var mapping in categoryMappings)
            {
                if (mapping.prefabs == null || !mapping.prefabs.Exists(p => p != null))
                {
                    missing.Add(mapping.category);
                }
            }
            return missing;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Recursively scans project HD folders (including Assets/HD_Jungle_Assets/) and populates the library.
        /// </summary>
        public HDDiscoveryReport DiscoverJungleAssets()
        {
            EnsureDefaultCategories();
            HDDiscoveryReport report = new HDDiscoveryReport();

            string[] searchFolders = new string[]
            {
                "Assets/HD_Jungle_Assets",
                "Assets/Low Poly Environment Starter Kit/Prefabs/URP",
                "Assets/Supercyan Free Forest Sample/Prefabs/High Quality",
                "Assets/Art/Environment/HD"
            };

            List<string> validFolders = new List<string>();
            foreach (var f in searchFolders)
            {
                if (System.IO.Directory.Exists(f)) validFolders.Add(f);
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", validFolders.ToArray());
            report.totalPrefabsDiscovered = prefabGuids.Length;

            foreach (var guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                Renderer rend = prefab.GetComponentInChildren<Renderer>(true);
                if (rend == null) continue;

                report.totalUsableHDPrefabs++;

                // Check materials for shader type
                Material[] mats = rend.sharedMaterials;
                if (mats != null)
                {
                    foreach (var m in mats)
                    {
                        if (m == null) continue;
                        if (m.shader != null && m.shader.name.StartsWith("Universal Render Pipeline/"))
                            report.urpCompatibleMaterials++;
                        else
                            report.builtInStandardMaterials++;
                    }
                }

                // Classify prefab into category
                HDObjectCategory category = ClassifyDiscoveredPrefab(path, prefab.name);
                var mapping = GetMapping(category);
                if (mapping != null && !mapping.prefabs.Contains(prefab))
                {
                    mapping.prefabs.Add(prefab);
                }
            }

            // Calculate final counts per category
            foreach (HDObjectCategory cat in Enum.GetValues(typeof(HDObjectCategory)))
            {
                int count = GetPrefabCountForCategory(cat);
                report.categoryCounts[cat] = count;
                if (count == 0) report.missingCategories.Add(cat.ToString());
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=#00FF88><b>[HDAssetLibrary] Discovery Complete: {report.totalUsableHDPrefabs} usable HD prefabs categorized across {categoryMappings.Count} categories.</b></color>");
            return report;
        }

        private static HDObjectCategory ClassifyDiscoveredPrefab(string assetPath, string name)
        {
            string pLower = assetPath.ToLowerInvariant();
            string nLower = name.ToLowerInvariant();

            if (pLower.Contains("riverrock") || nLower.Contains("riverrock") || nLower.Contains("steppingstone"))
                return HDObjectCategory.RiverRock;

            if (pLower.Contains("waterfall") || nLower.Contains("waterfall") || nLower.Contains("cascade"))
                return HDObjectCategory.Waterfall;

            if (pLower.Contains("water") || nLower.Contains("water") || nLower.Contains("lake") || nLower.Contains("river"))
                return HDObjectCategory.Water;

            if (pLower.Contains("dead leaves") || pLower.Contains("deadleaves") || nLower.Contains("deadleaves") || nLower.Contains("litter"))
                return HDObjectCategory.DeadLeaves;

            if (pLower.Contains("wood+trunk") || pLower.Contains("wood") || nLower.Contains("trunk") || nLower.Contains("log") || nLower.Contains("stump"))
                return HDObjectCategory.WoodTrunk;

            if (pLower.Contains("arch") || nLower.Contains("arch") || nLower.Contains("rock arc"))
                return HDObjectCategory.Arch;

            if (pLower.Contains("ancient") || nLower.Contains("ancient") || nLower.Contains("pillar") || nLower.Contains("ruin") || nLower.Contains("totem"))
                return HDObjectCategory.AncientStone;

            if (pLower.Contains("bush") || nLower.Contains("bush") || nLower.Contains("fern") || nLower.Contains("shrub"))
                return HDObjectCategory.Bush;

            if (pLower.Contains("grass") || nLower.Contains("grass") || nLower.Contains("foliage") || nLower.Contains("mushroom"))
                return HDObjectCategory.Grass;

            if (pLower.Contains("stone") || pLower.Contains("rock") || nLower.Contains("rock") || nLower.Contains("boulder") || nLower.Contains("stone"))
                return HDObjectCategory.Rock;

            if (pLower.Contains("tree") || pLower.Contains("jungle") || nLower.Contains("tree") || nLower.Contains("palm") || nLower.Contains("fir") || nLower.Contains("pine"))
                return HDObjectCategory.Tree;

            if (pLower.Contains("terrain") || pLower.Contains("ground") || nLower.Contains("ground") || nLower.Contains("canyon") || nLower.Contains("field"))
                return HDObjectCategory.Ground;

            return HDObjectCategory.Other;
        }
#endif
    }
}
