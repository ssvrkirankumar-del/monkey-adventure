using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonkeyAdventure.AILevelBuilder
{
    [Serializable]
    public class DiscoveredAssetItem
    {
        public string prefabName;
        public string assetPath;
        public string guid;
        public HDObjectCategory category;
        public int rendererCount;
        public bool hasMeshRenderer;
        public bool hasSkinnedRenderer;
        public bool hasLODGroup;
        public bool hasCollider;
        public int materialCount;
        public List<string> shaderNames = new List<string>();
        public string urpStatus = "URP Compatible";
        public Vector3 boundsSize = Vector3.one;
        public bool isUsable = true;
        public string rejectionReason = "";
        public bool isDuplicate = false;
        public bool isExistingMapping = false;
        public bool isAccepted = true;
        public GameObject prefabObject;
    }

    [Serializable]
    public class HDJungleDiscoveryReport
    {
        public string scanPath = "Assets/HD_Jungle_Assets";
        public int totalFilesScanned = 0;
        public int totalPrefabsDiscovered = 0;
        public int usablePrefabsCount = 0;
        public int rejectedPrefabsCount = 0;
        public int duplicatesSkippedCount = 0;
        public int existingMappingsPreservedCount = 0;
        public int newMappingsAddedCount = 0;

        public Dictionary<HDObjectCategory, int> categoryCounts = new Dictionary<HDObjectCategory, int>();
        public List<string> missingCategories = new List<string>();

        public int urpCompatibleCount = 0;
        public int builtInStandardCount = 0;
        public int missingShaderCount = 0;
        public int missingMaterialCount = 0;
        public int unknownShaderCount = 0;

        public List<DiscoveredAssetItem> discoveredItems = new List<DiscoveredAssetItem>();
        public List<DiscoveredAssetItem> rejectedItems = new List<DiscoveredAssetItem>();
    }

    /// <summary>
    /// Non-destructive asset discovery and auto-mapping engine for HD Jungle Assets.
    /// Recursively discovers, filters, verifies shaders, and classifies prefabs into HDAssetLibrary categories.
    /// </summary>
    public static class HDJungleAssetDiscovery
    {
        public const string TARGET_FOLDER = "Assets/HD_Jungle_Assets";
        public const string REPORT_PATH = "Assets/AILevelBuilder/Reports/HDJungleAssetDiscoveryReport.txt";

#if UNITY_EDITOR
        /// <summary>
        /// Scans Assets/HD_Jungle_Assets and updates HDAssetLibrary with valid categorized prefabs.
        /// </summary>
        public static HDJungleDiscoveryReport DiscoverAndMapJungleAssets(HDAssetLibrary library, bool applyToLibrary = true)
        {
            HDJungleDiscoveryReport report = new HDJungleDiscoveryReport();
            if (library == null)
            {
                Debug.LogWarning("[HDJungleAssetDiscovery] HDAssetLibrary is null. Discovery aborted.");
                return report;
            }

            library.EnsureDefaultCategories();

            // Count existing mappings in library
            HashSet<string> existingGuids = new HashSet<string>();
            foreach (var mapping in library.CategoryMappings)
            {
                if (mapping.prefabs != null)
                {
                    foreach (var p in mapping.prefabs)
                    {
                        if (p != null)
                        {
                            string pPath = AssetDatabase.GetAssetPath(p);
                            string pGuid = AssetDatabase.AssetPathToGUID(pPath);
                            if (!string.IsNullOrEmpty(pGuid)) existingGuids.Add(pGuid);
                            report.existingMappingsPreservedCount++;
                        }
                    }
                }
            }

            // Ensure target scan directory exists
            if (!Directory.Exists(TARGET_FOLDER))
            {
                Directory.CreateDirectory(TARGET_FOLDER);
                AssetDatabase.Refresh();
            }

            // Discover all .prefab assets in Assets/HD_Jungle_Assets
            string[] foundGuids = AssetDatabase.FindAssets("t:Prefab", new string[] { TARGET_FOLDER });
            report.totalPrefabsDiscovered = foundGuids.Length;

            // Also check other high-quality environment asset directories if present
            List<string> allGuidsList = new List<string>(foundGuids);
            string[] auxFolders = new string[]
            {
                "Assets/Low Poly Environment Starter Kit/Prefabs/URP",
                "Assets/Supercyan Free Forest Sample/Prefabs/High Quality",
                "Assets/Art/Environment/HD"
            };
            foreach (var aux in auxFolders)
            {
                if (Directory.Exists(aux))
                {
                    string[] auxGuids = AssetDatabase.FindAssets("t:Prefab", new string[] { aux });
                    foreach (var ag in auxGuids)
                    {
                        if (!allGuidsList.Contains(ag)) allGuidsList.Add(ag);
                    }
                }
            }
            report.totalFilesScanned = allGuidsList.Count;

            foreach (string guid in allGuidsList)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab == null)
                {
                    report.rejectedPrefabsCount++;
                    report.rejectedItems.Add(new DiscoveredAssetItem
                    {
                        prefabName = Path.GetFileNameWithoutExtension(assetPath),
                        assetPath = assetPath,
                        guid = guid,
                        isUsable = false,
                        rejectionReason = "Asset could not be loaded as a GameObject prefab."
                    });
                    continue;
                }

                // Inspect candidate
                DiscoveredAssetItem item = InspectPrefabCandidate(prefab, assetPath, guid);

                // Quality Filter
                if (!item.isUsable)
                {
                    report.rejectedPrefabsCount++;
                    report.rejectedItems.Add(item);
                    continue;
                }

                // Check duplicate / existing
                if (existingGuids.Contains(guid))
                {
                    item.isExistingMapping = true;
                    item.isDuplicate = true;
                    report.duplicatesSkippedCount++;
                }

                // Update shader statistics
                if (item.urpStatus.Contains("Standard")) report.builtInStandardCount++;
                else if (item.urpStatus.Contains("Missing Shader")) report.missingShaderCount++;
                else if (item.urpStatus.Contains("Missing Material")) report.missingMaterialCount++;
                else if (item.urpStatus.Contains("Unknown")) report.unknownShaderCount++;
                else report.urpCompatibleCount++;

                report.usablePrefabsCount++;
                report.discoveredItems.Add(item);

                // Add to library mapping if applying and not already present
                if (applyToLibrary && !item.isDuplicate)
                {
                    var mapping = library.GetMapping(item.category);
                    if (mapping != null && !mapping.prefabs.Contains(prefab))
                    {
                        mapping.prefabs.Add(prefab);
                        existingGuids.Add(guid);
                        report.newMappingsAddedCount++;
                    }
                }
            }

            // Calculate final category counts in library
            foreach (HDObjectCategory cat in Enum.GetValues(typeof(HDObjectCategory)))
            {
                int count = library.GetPrefabCountForCategory(cat);
                report.categoryCounts[cat] = count;
                if (count == 0) report.missingCategories.Add(cat.ToString());
            }

            if (applyToLibrary)
            {
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
            }

            // Save report text file
            SaveDiscoveryReport(report);

            // Log Console Summary
            PrintConsoleReport(report);

            return report;
        }

        private static DiscoveredAssetItem InspectPrefabCandidate(GameObject prefab, string assetPath, string guid)
        {
            DiscoveredAssetItem item = new DiscoveredAssetItem
            {
                prefabName = prefab.name,
                assetPath = assetPath,
                guid = guid,
                prefabObject = prefab
            };

            // Inspect renderers
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            item.rendererCount = renderers.Length;

            if (renderers.Length == 0)
            {
                item.isUsable = false;
                item.rejectionReason = "Prefab contains no Renderer components (empty or non-visual object).";
                return item;
            }

            item.hasMeshRenderer = prefab.GetComponentInChildren<MeshRenderer>(true) != null;
            item.hasSkinnedRenderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;
            item.hasLODGroup = prefab.GetComponentInChildren<LODGroup>(true) != null;
            item.hasCollider = prefab.GetComponentInChildren<Collider>(true) != null;

            // Calculate bounds
            Bounds totalBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool hasValidBounds = false;
            foreach (var r in renderers)
            {
                if (r != null)
                {
                    if (!hasValidBounds)
                    {
                        totalBounds = r.bounds;
                        hasValidBounds = true;
                    }
                    else
                    {
                        totalBounds.Encapsulate(r.bounds);
                    }
                }
            }
            item.boundsSize = hasValidBounds ? totalBounds.size : Vector3.one;

            // Inspect materials and shaders
            int totalMats = 0;
            bool hasBuiltInStandard = false;
            bool hasMissingShader = false;
            bool hasMissingMaterial = false;

            foreach (var r in renderers)
            {
                if (r == null) continue;
                Material[] mats = r.sharedMaterials;
                if (mats == null || mats.Length == 0)
                {
                    hasMissingMaterial = true;
                    continue;
                }

                foreach (var m in mats)
                {
                    totalMats++;
                    if (m == null)
                    {
                        hasMissingMaterial = true;
                        continue;
                    }

                    Shader s = m.shader;
                    if (s == null)
                    {
                        hasMissingShader = true;
                        continue;
                    }

                    string sName = s.name;
                    if (!item.shaderNames.Contains(sName)) item.shaderNames.Add(sName);

                    if (sName.Equals("Standard", StringComparison.OrdinalIgnoreCase) ||
                        sName.StartsWith("Mobile/", StringComparison.OrdinalIgnoreCase) ||
                        sName.StartsWith("Legacy Shaders/", StringComparison.OrdinalIgnoreCase) ||
                        sName.Contains("SupercyanShader"))
                    {
                        hasBuiltInStandard = true;
                    }
                }
            }
            item.materialCount = totalMats;

            if (hasMissingMaterial) item.urpStatus = "Missing Material";
            else if (hasMissingShader) item.urpStatus = "Missing Shader";
            else if (hasBuiltInStandard) item.urpStatus = "Built-in Standard (URP Copy Available)";
            else item.urpStatus = "URP Compatible";

            // Classify candidate
            item.category = ClassifyPrefab(assetPath, prefab.name, item.boundsSize);

            return item;
        }

        private static HDObjectCategory ClassifyPrefab(string path, string name, Vector3 bounds)
        {
            string pLower = path.ToLowerInvariant();
            string nLower = name.ToLowerInvariant();

            // 1. River rocks / stepping stones
            if (pLower.Contains("riverrocks") || nLower.Contains("riverrock") || nLower.Contains("steppingstone") || nLower.Contains("riverstone"))
                return HDObjectCategory.RiverRock;

            // 2. Waterfall
            if (pLower.Contains("waterfall") || nLower.Contains("waterfall") || nLower.Contains("cascade"))
                return HDObjectCategory.Waterfall;

            // 3. Water surface
            if (pLower.Contains("water") || nLower.Contains("water") || nLower.Contains("lake") || nLower.Contains("river"))
                return HDObjectCategory.Water;

            // 4. Dead leaves
            if (pLower.Contains("forest dead leaves") || pLower.Contains("deadleaves") || nLower.Contains("deadleaves") || nLower.Contains("litter") || nLower.Contains("mulch"))
                return HDObjectCategory.DeadLeaves;

            // 5. Wood & trunk
            if (pLower.Contains("wood+trunk") || pLower.Contains("wood") || nLower.Contains("wood") || nLower.Contains("trunk") || nLower.Contains("log") || nLower.Contains("stump"))
                return HDObjectCategory.WoodTrunk;

            // 6. Stones / Arches / Ancient stones
            if (pLower.Contains("stones") || pLower.Contains("rocks") || nLower.Contains("stone") || nLower.Contains("rock"))
            {
                if (nLower.Contains("arch") || nLower.Contains("rock arc")) return HDObjectCategory.Arch;
                if (nLower.Contains("ancient") || nLower.Contains("pillar") || nLower.Contains("ruin") || nLower.Contains("totem") || nLower.Contains("monolith"))
                    return HDObjectCategory.AncientStone;
                return HDObjectCategory.Rock;
            }

            // 7. Grass & Foliage
            if (pLower.Contains("grass") || nLower.Contains("grass") || nLower.Contains("tuft") || nLower.Contains("clump") || nLower.Contains("lawn"))
                return HDObjectCategory.Grass;

            // 8. Jungle / Trees / Bushes
            if (pLower.Contains("jungle") || pLower.Contains("trees") || nLower.Contains("tree") || nLower.Contains("palm") || nLower.Contains("pine") || nLower.Contains("fir"))
            {
                if (nLower.Contains("bush") || nLower.Contains("fern") || nLower.Contains("shrub"))
                    return HDObjectCategory.Bush;
                return HDObjectCategory.Tree;
            }

            if (pLower.Contains("other") || nLower.Contains("bush"))
            {
                if (nLower.Contains("bush")) return HDObjectCategory.Bush;
                if (nLower.Contains("grass")) return HDObjectCategory.Grass;
            }

            if (pLower.Contains("terrain") || pLower.Contains("ground") || nLower.Contains("ground") || nLower.Contains("canyon") || nLower.Contains("field"))
                return HDObjectCategory.Ground;

            return HDObjectCategory.Other;
        }

        public static void AcceptCandidate(DiscoveredAssetItem item, HDAssetLibrary library)
        {
            if (item == null || library == null || item.prefabObject == null) return;
            var mapping = library.GetMapping(item.category);
            if (mapping != null && !mapping.prefabs.Contains(item.prefabObject))
            {
                mapping.prefabs.Add(item.prefabObject);
                item.isAccepted = true;
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
            }
        }

        public static void RejectCandidate(DiscoveredAssetItem item, HDAssetLibrary library)
        {
            if (item == null || library == null || item.prefabObject == null) return;
            foreach (var mapping in library.CategoryMappings)
            {
                if (mapping.prefabs.Contains(item.prefabObject))
                {
                    mapping.prefabs.Remove(item.prefabObject);
                }
            }
            item.isAccepted = false;
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }

        public static void ChangeCandidateCategory(DiscoveredAssetItem item, HDObjectCategory newCat, HDAssetLibrary library)
        {
            if (item == null || library == null || item.prefabObject == null) return;
            RejectCandidate(item, library);
            item.category = newCat;
            AcceptCandidate(item, library);
        }

        private static void SaveDiscoveryReport(HDJungleDiscoveryReport report)
        {
            string dir = Path.GetDirectoryName(REPORT_PATH);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("HD JUNGLE ASSET DISCOVERY & AUTO-MAPPING REPORT");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Scan Path:                      {report.scanPath}");
            sb.AppendLine($"Total Assets Scanned:           {report.totalFilesScanned}");
            sb.AppendLine($"Total Prefabs Discovered:       {report.totalPrefabsDiscovered}");
            sb.AppendLine($"Usable Environment Prefabs:     {report.usablePrefabsCount}");
            sb.AppendLine($"Rejected Assets:                {report.rejectedPrefabsCount}");
            sb.AppendLine($"Existing Mappings Preserved:    {report.existingMappingsPreservedCount}");
            sb.AppendLine($"New Mappings Added:             {report.newMappingsAddedCount}");
            sb.AppendLine($"Duplicates Skipped:             {report.duplicatesSkippedCount}\n");

            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("CATEGORY BREAKDOWN:");
            sb.AppendLine("--------------------------------------------------------------------------------");
            foreach (var kvp in report.categoryCounts)
            {
                sb.AppendLine($"- {kvp.Key,-16}: {kvp.Value} prefabs");
            }
            sb.AppendLine($"\nMissing Categories: {(report.missingCategories.Count > 0 ? string.Join(", ", report.missingCategories) : "None (All 13 categories mapped)")}\n");

            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("MATERIAL & SHADER STATUS:");
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine($"URP Compatible:                 {report.urpCompatibleCount}");
            sb.AppendLine($"Built-in Standard:              {report.builtInStandardCount}");
            sb.AppendLine($"Missing Shader:                 {report.missingShaderCount}");
            sb.AppendLine($"Missing Material:               {report.missingMaterialCount}");
            sb.AppendLine($"Unknown Shader:                 {report.unknownShaderCount}\n");

            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("DISCOVERED ASSET DETAILS:");
            sb.AppendLine("--------------------------------------------------------------------------------");
            for (int i = 0; i < report.discoveredItems.Count; i++)
            {
                var item = report.discoveredItems[i];
                sb.AppendLine($"[{i + 1}] {item.prefabName} -> Category: {item.category}");
                sb.AppendLine($"    Path: {item.assetPath}");
                sb.AppendLine($"    Renderers: {item.rendererCount} | Materials: {item.materialCount} | URP Status: {item.urpStatus}");
                sb.AppendLine($"    Bounds: ({item.boundsSize.x:F1}m, {item.boundsSize.y:F1}m, {item.boundsSize.z:F1}m) | Shaders: {string.Join(", ", item.shaderNames)}");
                sb.AppendLine();
            }

            if (report.rejectedItems.Count > 0)
            {
                sb.AppendLine("--------------------------------------------------------------------------------");
                sb.AppendLine("REJECTED ASSETS:");
                sb.AppendLine("--------------------------------------------------------------------------------");
                foreach (var rej in report.rejectedItems)
                {
                    sb.AppendLine($"• {rej.prefabName} ({rej.assetPath}): {rej.rejectionReason}");
                }
            }

            File.WriteAllText(REPORT_PATH, sb.ToString());
            AssetDatabase.Refresh();
        }

        [MenuItem("Window/Monkey Adventure/HD Asset Material Diagnostic/🔎 Discover HD Jungle Assets", false, 126)]
        public static void MenuDiscoverHDJungleAssets()
        {
            if (!AssetDatabase.IsValidFolder(TARGET_FOLDER))
            {
                EditorUtility.DisplayDialog("HD Jungle Asset Discovery",
                    "HD Jungle Assets folder was not found inside Assets/. Move/import HD_Jungle_Assets into the Unity Assets folder before discovery.", "OK");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:HDAssetLibrary");
            HDAssetLibrary library = null;
            if (guids.Length > 0)
            {
                library = AssetDatabase.LoadAssetAtPath<HDAssetLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (library == null)
            {
                string dir = "Assets/AILevelBuilder/Data";
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = $"{dir}/HDAssetLibrary_Level01.asset";
                library = ScriptableObject.CreateInstance<HDAssetLibrary>();
                library.EnsureDefaultCategories();
                AssetDatabase.CreateAsset(library, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var report = DiscoverAndMapJungleAssets(library, true);
            EditorUtility.DisplayDialog("HD Jungle Asset Discovery",
                $"Discovery Finished:\n\n" +
                $"• Scan Path: {report.scanPath}\n" +
                $"• Total Prefabs: {report.totalPrefabsDiscovered}\n" +
                $"• Usable Prefabs: {report.usablePrefabsCount}\n" +
                $"• Newly Mapped: {report.newMappingsAddedCount}\n" +
                $"• Existing Preserved: {report.existingMappingsPreservedCount}\n" +
                $"• URP Compatible: {report.urpCompatibleCount}\n\n" +
                $"Report saved to:\n{REPORT_PATH}", "OK");
        }

        private static void PrintConsoleReport(HDJungleDiscoveryReport report)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b><color=#00FF88>[HDJungleAssetDiscovery] Discovery & Auto-Mapping Finished:</color></b>");
            sb.AppendLine($"• Total Prefabs Found: {report.totalPrefabsDiscovered}");
            sb.AppendLine($"• Usable Environment Prefabs: {report.usablePrefabsCount}");
            sb.AppendLine($"• New Mappings Added: {report.newMappingsAddedCount}");
            sb.AppendLine($"• Preserved Existing Mappings: {report.existingMappingsPreservedCount}");
            sb.AppendLine($"• URP Compatible: {report.urpCompatibleCount} | Built-in Standard: {report.builtInStandardCount}");
            sb.AppendLine($"• Report Saved to: {REPORT_PATH}");
            Debug.Log(sb.ToString());
        }
#endif
    }
}
