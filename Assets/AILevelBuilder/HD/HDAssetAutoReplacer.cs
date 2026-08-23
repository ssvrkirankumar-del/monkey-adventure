using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonkeyAdventure.AILevelBuilder
{
    /// <summary>
    /// Statistics for a specific category replacement pass.
    /// </summary>
    [Serializable]
    public class CategoryReplacementStats
    {
        public HDObjectCategory category;
        public int blockoutCount;
        public int replacedCount;
        public int missingCount;

        public CategoryReplacementStats(HDObjectCategory cat)
        {
            category = cat;
            blockoutCount = 0;
            replacedCount = 0;
            missingCount = 0;
        }
    }

    /// <summary>
    /// Summary report generated after an HD replacement pass or preview.
    /// </summary>
    [Serializable]
    public class HDReplacementReport
    {
        public bool isPreview = false;
        public int totalBlockoutObjects = 0;
        public int totalReplacedObjects = 0;
        public int totalSkippedObjects = 0;
        public List<CategoryReplacementStats> categoryStats = new List<CategoryReplacementStats>();
        public List<string> logEntries = new List<string>();

        public CategoryReplacementStats GetOrCreateStats(HDObjectCategory cat)
        {
            var stat = categoryStats.Find(s => s.category == cat);
            if (stat == null)
            {
                stat = new CategoryReplacementStats(cat);
                categoryStats.Add(stat);
            }
            return stat;
        }
    }

    /// <summary>
    /// Automated system that converts primitive blockout environment objects into
    /// higher-quality 3D HD assets while strictly preserving gameplay transforms, colliders, and level flow.
    /// </summary>
    public static class HDAssetAutoReplacer
    {
        public const string HD_ROOT_NAME = "HD_REPLACEMENTS";
        public const string PREVIEW_ROOT_NAME = "HD_REPLACEMENTS_PREVIEW";

        /// <summary>
        /// Classifies a scene object into its corresponding HD environment category based on its name and parent hierarchy.
        /// </summary>
        public static HDObjectCategory ClassifyObject(Transform t)
        {
            if (t == null) return HDObjectCategory.Other;

            string nameLower = t.name.ToLowerInvariant();
            string parentNameLower = t.parent != null ? t.parent.name.ToLowerInvariant() : "";
            string combined = $"{parentNameLower}/{nameLower}";

            // 1. River rocks / stepping stones
            if (combined.Contains("steppingstone") || combined.Contains("riverrock") || combined.Contains("river_rock") || combined.Contains("riverstone") || combined.Contains("crossing"))
                return HDObjectCategory.RiverRock;

            // 2. Waterfall
            if (combined.Contains("waterfall") || combined.Contains("cascade"))
                return HDObjectCategory.Waterfall;

            // 3. Water surface / river
            if (combined.Contains("water") || combined.Contains("river") || combined.Contains("lake") || combined.Contains("pond") || combined.Contains("stream"))
                return HDObjectCategory.Water;

            // 4. Arches
            if (combined.Contains("arch") || combined.Contains("rock arc") || combined.Contains("stone_arch"))
                return HDObjectCategory.Arch;

            // 5. Ancient stones / ruins / pillars
            if (combined.Contains("ancient") || combined.Contains("ruin") || combined.Contains("pillar") || combined.Contains("totem") || combined.Contains("monolith") || combined.Contains("temple"))
                return HDObjectCategory.AncientStone;

            // 6. Wood, logs, tree stumps
            if (combined.Contains("wood") || combined.Contains("log") || combined.Contains("stump") || combined.Contains("fallen_tree") || combined.Contains("dead_tree"))
                return HDObjectCategory.WoodTrunk;

            // 7. Dead leaves / forest floor litter
            if (combined.Contains("dead leaves") || combined.Contains("deadleaves") || combined.Contains("litter") || combined.Contains("leaf_debris"))
                return HDObjectCategory.DeadLeaves;

            // 8. Bushes / shrubs / ferns
            if (combined.Contains("bush") || combined.Contains("shrub") || combined.Contains("fern") || combined.Contains("plant"))
                return HDObjectCategory.Bush;

            // 9. Grass / small foliage
            if (combined.Contains("grass") || combined.Contains("turf") || combined.Contains("lawn") || combined.Contains("foliage") || combined.Contains("flower"))
                return HDObjectCategory.Grass;

            // 10. Rocks / boulders / cliffs
            if (combined.Contains("rock") || combined.Contains("boulder") || combined.Contains("stone") || combined.Contains("cliff"))
                return HDObjectCategory.Rock;

            // 11. Trees / canopy
            if (combined.Contains("tree") || combined.Contains("palm") || combined.Contains("fir") || combined.Contains("pine") || combined.Contains("canopy") || combined.Contains("jungle_tree"))
                return HDObjectCategory.Tree;

            // 12. Ground / path / terrain
            if (combined.Contains("ground") || combined.Contains("path") || combined.Contains("floor") || combined.Contains("soil") || combined.Contains("terrain"))
                return HDObjectCategory.Ground;

            return HDObjectCategory.Other;
        }

        /// <summary>
        /// Generates non-destructive preview instances of HD prefabs without modifying original blockout visuals.
        /// </summary>
        public static HDReplacementReport PreviewHDReplacements(HDAssetLibrary library, int seed = 1337)
        {
            ClearHDPreview();
            return ExecuteReplacementPass(library, true, seed);
        }

        /// <summary>
        /// Applies the HD replacements non-destructively, hiding blockout visuals and creating the HD_REPLACEMENTS hierarchy.
        /// </summary>
        public static HDReplacementReport ApplyHDReplacements(HDAssetLibrary library, int seed = 1337)
        {
            ClearHDPreview();
            RollbackHDReplacements();
            return ExecuteReplacementPass(library, false, seed);
        }

        /// <summary>
        /// Removes all HD replacement hierarchies and restores original blockout visibility.
        /// </summary>
        public static void RollbackHDReplacements()
        {
            ClearHDPreview();

            GameObject levelRoot = GameObject.Find(LevelGenerator.ROOT_NAME);
            if (levelRoot != null)
            {
                Transform hdRoot = levelRoot.transform.Find(HD_ROOT_NAME);
                if (hdRoot != null)
                {
#if UNITY_EDITOR
                    Undo.DestroyObjectImmediate(hdRoot.gameObject);
#else
                    UnityEngine.Object.Destroy(hdRoot.gameObject);
#endif
                    Debug.Log($"[HDAssetAutoReplacer] Removed '{HD_ROOT_NAME}' hierarchy.");
                }

                // Restore all disabled renderers in Environment
                Transform envFolder = levelRoot.transform.Find(LevelGenerator.ENV_FOLDER);
                if (envFolder != null)
                {
                    Renderer[] allRenderers = envFolder.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in allRenderers)
                    {
                        if (!r.enabled)
                        {
                            r.enabled = true;
                        }
                    }
                    Debug.Log($"[HDAssetAutoReplacer] Restored {allRenderers.Length} blockout renderer(s).");
                }
            }
        }

        /// <summary>
        /// Removes only the temporary preview hierarchy.
        /// </summary>
        public static void ClearHDPreview()
        {
            GameObject levelRoot = GameObject.Find(LevelGenerator.ROOT_NAME);
            if (levelRoot != null)
            {
                Transform previewRoot = levelRoot.transform.Find(PREVIEW_ROOT_NAME);
                if (previewRoot != null)
                {
#if UNITY_EDITOR
                    Undo.DestroyObjectImmediate(previewRoot.gameObject);
#else
                    UnityEngine.Object.Destroy(previewRoot.gameObject);
#endif
                    Debug.Log($"[HDAssetAutoReplacer] Cleared '{PREVIEW_ROOT_NAME}'.");
                }
            }
        }

        private static HDReplacementReport ExecuteReplacementPass(HDAssetLibrary library, bool isPreview, int seed)
        {
            HDReplacementReport report = new HDReplacementReport { isPreview = isPreview };

            if (library == null)
            {
                report.logEntries.Add("HDAssetLibrary is null. Cannot proceed with replacement.");
                Debug.LogWarning("[HDAssetAutoReplacer] HDAssetLibrary is null.");
                return report;
            }

            GameObject levelRoot = GameObject.Find(LevelGenerator.ROOT_NAME);
            if (levelRoot == null)
            {
                report.logEntries.Add($"Root '{LevelGenerator.ROOT_NAME}' not found in active scene.");
                Debug.LogWarning($"[HDAssetAutoReplacer] '{LevelGenerator.ROOT_NAME}' not found in scene.");
                return report;
            }

            Transform envFolder = levelRoot.transform.Find(LevelGenerator.ENV_FOLDER);
            if (envFolder == null)
            {
                report.logEntries.Add("Environment folder not found in level root.");
                return report;
            }

            // Deterministic RNG
            UnityEngine.Random.InitState(seed);

            // Master replacement folder under AI_GENERATED_LEVEL
            string rootFolderName = isPreview ? PREVIEW_ROOT_NAME : HD_ROOT_NAME;
            GameObject hdMasterRoot = new GameObject(rootFolderName);
            hdMasterRoot.transform.SetParent(levelRoot.transform, false);

#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(hdMasterRoot, isPreview ? "Preview HD Replacements" : "Apply HD Replacements");
#endif

            // Category subfolders
            Dictionary<HDObjectCategory, Transform> categoryFolders = new Dictionary<HDObjectCategory, Transform>();
            Array cats = Enum.GetValues(typeof(HDObjectCategory));
            foreach (HDObjectCategory c in cats)
            {
                GameObject folder = new GameObject(c.ToString());
                folder.transform.SetParent(hdMasterRoot.transform, false);
                categoryFolders[c] = folder.transform;
            }

            // Scan only top-level environment children and their discrete props
            List<Transform> targetTransforms = new List<Transform>();
            GetTargetEnvironmentObjects(envFolder, targetTransforms);
            report.totalBlockoutObjects = targetTransforms.Count;

            int variantCounter = 0;
            foreach (Transform blockoutObj in targetTransforms)
            {
                HDObjectCategory category = ClassifyObject(blockoutObj);
                var stat = report.GetOrCreateStats(category);
                stat.blockoutCount++;

                var mapping = library.GetMapping(category);
                if (mapping == null || mapping.prefabs == null || mapping.prefabs.Count == 0 || !mapping.prefabs.Exists(p => p != null))
                {
                    // Check if a fallback is available in library
                    if (!library.HasPrefabForCategory(category))
                    {
                        stat.missingCount++;
                        report.totalSkippedObjects++;
                        continue;
                    }
                }

                GameObject prefab = library.GetPrefab(category, variantCounter++);
                if (prefab == null)
                {
                    stat.missingCount++;
                    report.totalSkippedObjects++;
                    continue;
                }

                // Calculate transform
                Vector3 finalPos = blockoutObj.position + Vector3.up * (mapping != null ? mapping.verticalOffset : 0f);
                Quaternion finalRot = blockoutObj.rotation;
                if (mapping != null && mapping.randomRotationY)
                {
                    finalRot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                }

                float scaleMultiplier = mapping != null ? mapping.scaleMultiplier : 1.0f;
                if (mapping != null && mapping.randomScaleVariation)
                {
                    scaleMultiplier *= UnityEngine.Random.Range(mapping.scaleVariationRange.x, mapping.scaleVariationRange.y);
                }
                Vector3 finalScale = Vector3.one * scaleMultiplier;

                // Instantiate HD Replacement
                GameObject hdInstance;
#if UNITY_EDITOR
                hdInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, categoryFolders[category]);
                if (hdInstance == null)
                {
                    hdInstance = UnityEngine.Object.Instantiate(prefab, categoryFolders[category]);
                }
#else
                hdInstance = UnityEngine.Object.Instantiate(prefab, categoryFolders[category]);
#endif
                hdInstance.name = $"HD_{blockoutObj.name}";
                hdInstance.transform.position = finalPos;
                hdInstance.transform.rotation = finalRot;
                hdInstance.transform.localScale = finalScale;

                // Ensure all renderers and material slots on the instance use converted URP materials
                HDMaterialURPConverter.ApplyConvertedMaterialsToInstance(hdInstance);

                stat.replacedCount++;
                report.totalReplacedObjects++;

                // If applying (not preview), disable only the visual renderer on the blockout object
                if (!isPreview)
                {
                    Renderer[] blockoutRenderers = blockoutObj.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in blockoutRenderers)
                    {
                        r.enabled = false;
                    }
                }
            }

            Debug.Log($"<color=#00FF88><b>[HDAssetAutoReplacer] {(isPreview ? "Preview" : "Applied")} HD Replacements: {report.totalReplacedObjects}/{report.totalBlockoutObjects} objects replaced ({report.totalSkippedObjects} skipped).</b></color>");
            return report;
        }

        private static void GetTargetEnvironmentObjects(Transform envFolder, List<Transform> list)
        {
            if (envFolder == null) return;

            foreach (Transform categorySubfolder in envFolder)
            {
                if (categorySubfolder.name.Equals("Ground", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (Transform groundSection in categorySubfolder)
                    {
                        list.Add(groundSection);
                    }
                }
                else
                {
                    foreach (Transform item in categorySubfolder)
                    {
                        list.Add(item);
                    }
                }
            }
        }
    }
}
