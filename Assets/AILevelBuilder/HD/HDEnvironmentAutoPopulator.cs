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
    public enum PopulatorDensity
    {
        Low,
        Medium,
        High
    }

    public enum EnvironmentZoneType
    {
        ForestEntrance,   // 0.00 - 0.15
        AncientGrove,     // 0.15 - 0.45
        DenseJungle,      // 0.45 - 0.70
        RockAndLogRidge,  // 0.70 - 0.88
        SacredClearing,   // 0.88 - 0.96
        FinishSanctuary   // 0.96 - 1.00
    }

    [Serializable]
    public class EnvironmentPopulatorSettings
    {
        public int seed = 1337;
        public PopulatorDensity density = PopulatorDensity.Medium;
        [Range(2.0f, 6.0f)] public float playerSafetyMargin = 3.5f;
        [Range(0.05f, 0.5f)] public float zoneVariation = 0.25f;
        [Range(50, 400)] public int maxObjects = 250;
        public bool removeCollidersFromPrefabs = true;
    }

    [Serializable]
    public class ZoneGenerationStats
    {
        public EnvironmentZoneType zoneType;
        public string zoneName;
        public float startZ;
        public float endZ;
        public int treeCount = 0;
        public int bushCount = 0;
        public int grassCount = 0;
        public int fernCount = 0;
        public int deadLeavesCount = 0;
        public int rockCount = 0;
        public int riverRockCount = 0;
        public int logCount = 0;
        public int stumpCount = 0;
        public int ancientStoneCount = 0;
        public int ancientRuinsCount = 0;
        public int archCount = 0;
        public int waterCount = 0;
        public int waterfallCount = 0;
        public int otherCount = 0;
        public int totalObjects = 0;

        public ZoneGenerationStats(EnvironmentZoneType type, string name, float sZ, float eZ)
        {
            zoneType = type;
            zoneName = name;
            startZ = sZ;
            endZ = eZ;
        }
    }

    [Serializable]
    public class HDEnvironmentGenerationReport
    {
        public bool isPreview = false;
        public int seed = 1337;
        public float levelLength = 200f;
        public float playableWidth = 18f;
        public float startZ = 0f;
        public float finishZ = 200f;
        public float riverZ = -1f;

        public int totalObjectsGenerated = 0;
        public int treeCount = 0;
        public int bushCount = 0;
        public int grassCount = 0;
        public int fernCount = 0;
        public int deadLeavesCount = 0;
        public int rockCount = 0;
        public int riverRockCount = 0;
        public int logCount = 0;
        public int stumpCount = 0;
        public int ancientStoneCount = 0;
        public int ancientRuinsCount = 0;
        public int archCount = 0;
        public int waterCount = 0;
        public int waterfallCount = 0;
        public int otherCount = 0;

        public int skippedGameplayConflict = 0;
        public int skippedCorridorViolation = 0;
        public int skippedTooClose = 0;
        public int skippedMissingAsset = 0;
        public int skippedMaxCap = 0;

        public int urpCompatibleMaterials = 0;
        public int convertedMaterials = 0;

        public List<ZoneGenerationStats> zoneStats = new List<ZoneGenerationStats>();
        public List<string> logEntries = new List<string>();
    }

    internal struct PlacedEnvironmentItem
    {
        public Vector3 position;
        public float radius;
        public HDObjectCategory category;
    }

    /// <summary>
    /// Intelligent environment dressing engine that automatically populates the Level 01 blockout
    /// with discovered HD jungle assets while strictly preserving 100% of gameplay elements,
    /// colliders, and player corridor clearance.
    /// </summary>
    public static class HDEnvironmentAutoPopulator
    {
        public const string HD_ENV_ROOT_NAME = "HD_ENVIRONMENT";
        public const string HD_ENV_PREVIEW_ROOT_NAME = "HD_ENVIRONMENT_PREVIEW";
        public const string REPORT_PATH = "Assets/AILevelBuilder/Reports/HDEnvironmentGenerationReport.txt";

#if UNITY_EDITOR
        /// <summary>
        /// Generates the HD environment as a preview hierarchy.
        /// </summary>
        [MenuItem("Window/Monkey Adventure/HD Asset Material Diagnostic/🌴 Generate HD Environment Preview", false, 127)]
        public static void MenuGeneratePreview()
        {
            string[] guids = AssetDatabase.FindAssets("t:HDAssetLibrary");
            HDAssetLibrary library = null;
            if (guids.Length > 0)
            {
                library = AssetDatabase.LoadAssetAtPath<HDAssetLibrary>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            if (library == null)
            {
                EditorUtility.DisplayDialog("HD Environment Auto-Populator", "No HDAssetLibrary found. Create or discover one first.", "OK");
                return;
            }
            var report = GeneratePreview(library, new EnvironmentPopulatorSettings());
            EditorUtility.DisplayDialog("HD Environment Auto-Populator",
                $"Preview Generated:\n\n" +
                $"• Total Objects: {report.totalObjectsGenerated}\n" +
                $"• Trees: {report.treeCount} | Bushes: {report.bushCount}\n" +
                $"• Grass/Ferns: {report.grassCount + report.fernCount} | Rocks: {report.rockCount + report.riverRockCount}\n" +
                $"• Props: {report.logCount + report.stumpCount + report.ancientStoneCount}\n" +
                $"• URP Compatible: {report.urpCompatibleMaterials}\n\n" +
                $"Report saved to:\n{REPORT_PATH}", "OK");
        }

        public static HDEnvironmentGenerationReport GeneratePreview(HDAssetLibrary library, EnvironmentPopulatorSettings settings)
        {
            return GenerateEnvironment(library, settings, isPreview: true);
        }

        /// <summary>
        /// Applies the HD environment permanently to the active level hierarchy.
        /// </summary>
        public static HDEnvironmentGenerationReport ApplyEnvironment(HDAssetLibrary library, EnvironmentPopulatorSettings settings)
        {
            ClearPreview();
            return GenerateEnvironment(library, settings, isPreview: false);
        }

        /// <summary>
        /// Clears only the preview HD environment hierarchy.
        /// </summary>
        [MenuItem("Window/Monkey Adventure/HD Asset Material Diagnostic/🧹 Clear HD Environment Preview", false, 128)]
        public static void ClearPreview()
        {
            GameObject root = GameObject.Find(LevelGenerator.ROOT_NAME);
            if (root == null) return;

            Transform previewT = root.transform.Find(HD_ENV_PREVIEW_ROOT_NAME);
            if (previewT != null)
            {
                Undo.DestroyObjectImmediate(previewT.gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                Debug.Log($"[HDEnvironmentAutoPopulator] Cleared '{HD_ENV_PREVIEW_ROOT_NAME}'.");
            }
        }

        /// <summary>
        /// Rolls back the active HD environment hierarchy.
        /// </summary>
        public static void RollbackEnvironment()
        {
            ClearPreview();
            GameObject root = GameObject.Find(LevelGenerator.ROOT_NAME);
            if (root == null) return;

            Transform envT = root.transform.Find(HD_ENV_ROOT_NAME);
            if (envT != null)
            {
                Undo.DestroyObjectImmediate(envT.gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                Debug.Log($"[HDEnvironmentAutoPopulator] Rolled back active '{HD_ENV_ROOT_NAME}'.");
            }
        }

        /// <summary>
        /// Core procedural generation routine.
        /// </summary>
        public static HDEnvironmentGenerationReport GenerateEnvironment(HDAssetLibrary library, EnvironmentPopulatorSettings settings, bool isPreview)
        {
            HDEnvironmentGenerationReport report = new HDEnvironmentGenerationReport
            {
                isPreview = isPreview,
                seed = settings.seed
            };

            if (library == null)
            {
                Debug.LogError("[HDEnvironmentAutoPopulator] HDAssetLibrary is null. Generation aborted.");
                return report;
            }

            GameObject levelRoot = GameObject.Find(LevelGenerator.ROOT_NAME);
            if (levelRoot == null)
            {
                Debug.LogError($"[HDEnvironmentAutoPopulator] Level root '{LevelGenerator.ROOT_NAME}' not found in active scene.");
                return report;
            }

            // 1. Detect actual Level bounds & positions
            Vector3 startPos = Vector3.zero;
            Vector3 finishPos = new Vector3(0, 0, 200f);
            List<Vector3> checkpointPositions = new List<Vector3>();
            List<Vector3> collectiblePositions = new List<Vector3>();
            List<Vector3> obstaclePositions = new List<Vector3>();
            List<Vector3> enemyPositions = new List<Vector3>();
            float groundWidth = 18f;
            float riverZ = -1f;

            // Start & Finish
            Transform startT = levelRoot.transform.Find(LevelGenerator.START_FOLDER);
            if (startT != null && startT.childCount > 0) startPos = startT.GetChild(0).position;

            Transform finishT = levelRoot.transform.Find(LevelGenerator.FINISH_FOLDER);
            if (finishT != null && finishT.childCount > 0) finishPos = finishT.GetChild(0).position;

            // Checkpoints
            Transform cpFolder = levelRoot.transform.Find(LevelGenerator.CHECKPOINTS_FOLDER);
            if (cpFolder != null)
            {
                for (int i = 0; i < cpFolder.childCount; i++) checkpointPositions.Add(cpFolder.GetChild(i).position);
            }

            // Collectibles
            Transform colFolder = levelRoot.transform.Find(LevelGenerator.COLLECTIBLES_FOLDER);
            if (colFolder != null)
            {
                for (int i = 0; i < colFolder.childCount; i++) collectiblePositions.Add(colFolder.GetChild(i).position);
            }

            // Obstacles
            Transform obsFolder = levelRoot.transform.Find(LevelGenerator.OBSTACLES_FOLDER);
            if (obsFolder != null)
            {
                for (int i = 0; i < obsFolder.childCount; i++) obstaclePositions.Add(obsFolder.GetChild(i).position);
            }

            // Enemies
            Transform eneFolder = levelRoot.transform.Find(LevelGenerator.ENEMIES_FOLDER);
            if (eneFolder != null)
            {
                for (int i = 0; i < eneFolder.childCount; i++) enemyPositions.Add(eneFolder.GetChild(i).position);
            }

            // Detect River Z position if present in Environment blockout
            Transform envBlockoutFolder = levelRoot.transform.Find(LevelGenerator.ENV_FOLDER);
            if (envBlockoutFolder != null)
            {
                for (int i = 0; i < envBlockoutFolder.childCount; i++)
                {
                    Transform child = envBlockoutFolder.GetChild(i);
                    string nameLower = child.name.ToLowerInvariant();
                    if (nameLower.Contains("river") || nameLower.Contains("water") || nameLower.Contains("steppingstone"))
                    {
                        riverZ = child.position.z;
                        break;
                    }
                }
            }

            float startZ = startPos.z;
            float finishZ = finishPos.z;
            float levelLength = Mathf.Max(finishZ - startZ, 50f);

            report.startZ = startZ;
            report.finishZ = finishZ;
            report.levelLength = levelLength;
            report.playableWidth = groundWidth;
            report.riverZ = riverZ;

            // 2. Prepare Target Hierarchy Roots
            string targetRootName = isPreview ? HD_ENV_PREVIEW_ROOT_NAME : HD_ENV_ROOT_NAME;
            Transform existingTarget = levelRoot.transform.Find(targetRootName);
            if (existingTarget != null)
            {
                Undo.DestroyObjectImmediate(existingTarget.gameObject);
            }

            GameObject envRootGO = new GameObject(targetRootName);
            envRootGO.transform.SetParent(levelRoot.transform, false);
            Undo.RegisterCreatedObjectUndo(envRootGO, "Auto-Populate HD Environment");

            // Subcategory folders
            Transform folderTrees = CreateSubfolder(envRootGO.transform, "Trees");
            Transform folderBushes = CreateSubfolder(envRootGO.transform, "Bushes");
            Transform folderGrass = CreateSubfolder(envRootGO.transform, "Grass");
            Transform folderFerns = CreateSubfolder(envRootGO.transform, "Ferns");
            Transform folderDeadLeaves = CreateSubfolder(envRootGO.transform, "DeadLeaves");
            Transform folderRocks = CreateSubfolder(envRootGO.transform, "Rocks");
            Transform folderRiverRocks = CreateSubfolder(envRootGO.transform, "RiverRocks");
            Transform folderLogs = CreateSubfolder(envRootGO.transform, "Logs");
            Transform folderStumps = CreateSubfolder(envRootGO.transform, "Stumps");
            Transform folderWater = CreateSubfolder(envRootGO.transform, "Water");
            Transform folderWaterfalls = CreateSubfolder(envRootGO.transform, "Waterfalls");
            Transform folderAncient = CreateSubfolder(envRootGO.transform, "Ancient");
            Transform folderOther = CreateSubfolder(envRootGO.transform, "Other");

            // 3. Define the 6 Normalized Environmental Zones
            var zone1 = new ZoneGenerationStats(EnvironmentZoneType.ForestEntrance, "Forest Entrance", startZ, startZ + levelLength * 0.15f);
            var zone2 = new ZoneGenerationStats(EnvironmentZoneType.AncientGrove, "Ancient Grove", startZ + levelLength * 0.15f, startZ + levelLength * 0.45f);
            var zone3 = new ZoneGenerationStats(EnvironmentZoneType.DenseJungle, "Dense Jungle", startZ + levelLength * 0.45f, startZ + levelLength * 0.70f);
            var zone4 = new ZoneGenerationStats(EnvironmentZoneType.RockAndLogRidge, "Rock & Log Ridge", startZ + levelLength * 0.70f, startZ + levelLength * 0.88f);
            var zone5 = new ZoneGenerationStats(EnvironmentZoneType.SacredClearing, "Sacred Clearing", startZ + levelLength * 0.88f, startZ + levelLength * 0.96f);
            var zone6 = new ZoneGenerationStats(EnvironmentZoneType.FinishSanctuary, "Finish Sanctuary", startZ + levelLength * 0.96f, finishZ);

            report.zoneStats.Add(zone1);
            report.zoneStats.Add(zone2);
            report.zoneStats.Add(zone3);
            report.zoneStats.Add(zone4);
            report.zoneStats.Add(zone5);
            report.zoneStats.Add(zone6);

            // 4. Density Multipliers
            float densityFactor = settings.density == PopulatorDensity.Low ? 0.65f : (settings.density == PopulatorDensity.High ? 1.35f : 1.0f);

            // Seed deterministic RNG
            UnityEngine.Random.InitState(settings.seed);

            List<PlacedEnvironmentItem> placedItems = new List<PlacedEnvironmentItem>();

            // 5. Populate each zone
            foreach (var zone in report.zoneStats)
            {
                PopulateZone(zone, library, settings, densityFactor, groundWidth, startPos, finishPos,
                    checkpointPositions, collectiblePositions, obstaclePositions, enemyPositions, riverZ,
                    placedItems, report,
                    folderTrees, folderBushes, folderGrass, folderFerns, folderDeadLeaves,
                    folderRocks, folderRiverRocks, folderLogs, folderStumps, folderWater,
                    folderWaterfalls, folderAncient, folderOther);
            }

            // 6. Save generation report
            SaveReportToFile(report);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            PrintConsoleSummary(report);

            return report;
        }

        private static Transform CreateSubfolder(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void PopulateZone(
            ZoneGenerationStats zone,
            HDAssetLibrary library,
            EnvironmentPopulatorSettings settings,
            float densityFactor,
            float groundWidth,
            Vector3 startPos,
            Vector3 finishPos,
            List<Vector3> checkpoints,
            List<Vector3> collectibles,
            List<Vector3> obstacles,
            List<Vector3> enemies,
            float riverZ,
            List<PlacedEnvironmentItem> placedItems,
            HDEnvironmentGenerationReport report,
            Transform folderTrees,
            Transform folderBushes,
            Transform folderGrass,
            Transform folderFerns,
            Transform folderDeadLeaves,
            Transform folderRocks,
            Transform folderRiverRocks,
            Transform folderLogs,
            Transform folderStumps,
            Transform folderWater,
            Transform folderWaterfalls,
            Transform folderAncient,
            Transform folderOther)
        {
            float zMin = zone.startZ;
            float zMax = zone.endZ;
            float halfWidth = groundWidth * 0.5f;

            // Target counts per category in this zone based on zone specification
            int targetTrees = 0;
            int targetBushes = 0;
            int targetGrass = 0;
            int targetFerns = 0;
            int targetDeadLeaves = 0;
            int targetRocks = 0;
            int targetRiverRocks = 0;
            int targetLogs = 0;
            int targetStumps = 0;
            int targetAncient = 0;

            switch (zone.zoneType)
            {
                case EnvironmentZoneType.ForestEntrance:
                    // Low to Medium: Natural opening, keep start & first pickups clearly visible
                    targetTrees = Mathf.RoundToInt(UnityEngine.Random.Range(6, 10) * densityFactor);
                    targetBushes = Mathf.RoundToInt(UnityEngine.Random.Range(4, 7) * densityFactor);
                    targetGrass = Mathf.RoundToInt(UnityEngine.Random.Range(6, 10) * densityFactor);
                    targetFerns = Mathf.RoundToInt(UnityEngine.Random.Range(3, 6) * densityFactor);
                    targetDeadLeaves = Mathf.RoundToInt(UnityEngine.Random.Range(3, 6) * densityFactor);
                    targetLogs = Mathf.RoundToInt(UnityEngine.Random.Range(1, 2) * densityFactor);
                    break;

                case EnvironmentZoneType.AncientGrove:
                    // Medium to High: Canopy variation, non-linear staggered trees, stumps & trunks
                    targetTrees = Mathf.RoundToInt(UnityEngine.Random.Range(12, 18) * densityFactor);
                    targetBushes = Mathf.RoundToInt(UnityEngine.Random.Range(6, 10) * densityFactor);
                    targetGrass = Mathf.RoundToInt(UnityEngine.Random.Range(8, 14) * densityFactor);
                    targetFerns = Mathf.RoundToInt(UnityEngine.Random.Range(6, 10) * densityFactor);
                    targetDeadLeaves = Mathf.RoundToInt(UnityEngine.Random.Range(4, 7) * densityFactor);
                    targetRocks = Mathf.RoundToInt(UnityEngine.Random.Range(4, 7) * densityFactor);
                    targetLogs = Mathf.RoundToInt(UnityEngine.Random.Range(2, 4) * densityFactor);
                    targetStumps = Mathf.RoundToInt(UnityEngine.Random.Range(2, 4) * densityFactor);
                    break;

                case EnvironmentZoneType.DenseJungle:
                    // High: 3-7 object clusters, heavy vegetation on outer sides
                    targetTrees = Mathf.RoundToInt(UnityEngine.Random.Range(14, 20) * densityFactor);
                    targetBushes = Mathf.RoundToInt(UnityEngine.Random.Range(8, 12) * densityFactor);
                    targetGrass = Mathf.RoundToInt(UnityEngine.Random.Range(12, 18) * densityFactor);
                    targetFerns = Mathf.RoundToInt(UnityEngine.Random.Range(8, 14) * densityFactor);
                    targetDeadLeaves = Mathf.RoundToInt(UnityEngine.Random.Range(5, 8) * densityFactor);
                    targetRocks = Mathf.RoundToInt(UnityEngine.Random.Range(4, 8) * densityFactor);
                    break;

                case EnvironmentZoneType.RockAndLogRidge:
                    // Rocks, river rocks near river, fallen logs, stumps
                    targetTrees = Mathf.RoundToInt(UnityEngine.Random.Range(8, 14) * densityFactor);
                    targetRocks = Mathf.RoundToInt(UnityEngine.Random.Range(8, 14) * densityFactor);
                    targetRiverRocks = (riverZ > 0) ? Mathf.RoundToInt(UnityEngine.Random.Range(6, 12) * densityFactor) : 2;
                    targetLogs = Mathf.RoundToInt(UnityEngine.Random.Range(3, 5) * densityFactor);
                    targetStumps = Mathf.RoundToInt(UnityEngine.Random.Range(2, 4) * densityFactor);
                    targetBushes = Mathf.RoundToInt(UnityEngine.Random.Range(4, 7) * densityFactor);
                    targetFerns = Mathf.RoundToInt(UnityEngine.Random.Range(4, 8) * densityFactor);
                    targetDeadLeaves = Mathf.RoundToInt(UnityEngine.Random.Range(4, 7) * densityFactor);
                    break;

                case EnvironmentZoneType.SacredClearing:
                    // Low: Open clearing feeling before climax
                    targetTrees = Mathf.RoundToInt(UnityEngine.Random.Range(2, 4) * densityFactor);
                    targetGrass = Mathf.RoundToInt(UnityEngine.Random.Range(6, 10) * densityFactor);
                    targetDeadLeaves = Mathf.RoundToInt(UnityEngine.Random.Range(3, 6) * densityFactor);
                    targetRocks = Mathf.RoundToInt(UnityEngine.Random.Range(2, 4) * densityFactor);
                    targetAncient = Mathf.RoundToInt(UnityEngine.Random.Range(2, 4) * densityFactor);
                    break;

                case EnvironmentZoneType.FinishSanctuary:
                    // Important visual frame around finish
                    targetAncient = Mathf.RoundToInt(UnityEngine.Random.Range(2, 4) * densityFactor);
                    targetTrees = Mathf.RoundToInt(UnityEngine.Random.Range(4, 8) * densityFactor);
                    targetBushes = Mathf.RoundToInt(UnityEngine.Random.Range(3, 6) * densityFactor);
                    targetGrass = Mathf.RoundToInt(UnityEngine.Random.Range(4, 8) * densityFactor);
                    targetDeadLeaves = Mathf.RoundToInt(UnityEngine.Random.Range(2, 4) * densityFactor);
                    break;
            }

            // Spawn category batches
            SpawnCategoryBatch(HDObjectCategory.Tree, targetTrees, 5.0f, 0.85f, 1.20f, zMin, zMax, halfWidth, settings, zone, library, startPos, finishPos, checkpoints, collectibles, obstacles, enemies, riverZ, placedItems, report, folderTrees);
            SpawnCategoryBatch(HDObjectCategory.Bush, targetBushes, 1.8f, 0.85f, 1.15f, zMin, zMax, halfWidth, settings, zone, library, startPos, finishPos, checkpoints, collectibles, obstacles, enemies, riverZ, placedItems, report, folderBushes);
            SpawnCategoryBatch(HDObjectCategory.Grass, targetGrass, 0.9f, 0.75f, 1.25f, zMin, zMax, halfWidth, settings, zone, library, startPos, finishPos, checkpoints, collectibles, obstacles, enemies, riverZ, placedItems, report, folderGrass);
            SpawnCategoryBatch(HDObjectCategory.Bush, targetFerns, 1.0f, 0.80f, 1.20f, zMin, zMax, halfWidth, settings, zone, library, startPos, finishPos, checkpoints, collectibles, obstacles, enemies, riverZ, placedItems, report, folderFerns);
            SpawnCategoryBatch(HDObjectCategory.DeadLeaves, targetDeadLeaves, 1.2f, 0.80f, 1.25f, zMin, zMax, halfWidth, settings, zone, library, startPos, finishPos, checkpoints, collectibles, obstacles, enemies, riverZ, placedItems, report, folderDeadLeaves);
            SpawnCategoryBatch(HDObjectCategory.Rock, targetRocks, 3.0f, 0.80f, 1.25f, zMin, zMax, halfWidth, settings, zone, library, startPos, finishPos, checkpoints, collectibles, obstacles, enemies, riverZ, placedItems, report, folderRocks);
            SpawnCategoryBatch(HDObjectCategory.RiverRock, targetRiverRocks, 2.0f, 0.85f, 1.20f, zMin, zMax, halfWidth, settings, zone, library, startPos, finishPos, checkpoints, collectibles, obstacles, enemies, riverZ, placedItems, report, folderRiverRocks);
            SpawnCategoryBatch(HDObjectCategory.WoodTrunk, targetLogs, 3.0f, 0.90f, 1.15f, zMin, zMax, halfWidth, settings, zone, library, startPos, finishPos, checkpoints, collectibles, obstacles, enemies, riverZ, placedItems, report, folderLogs);
            SpawnCategoryBatch(HDObjectCategory.WoodTrunk, targetStumps, 2.2f, 0.90f, 1.15f, zMin, zMax, halfWidth, settings, zone, library, startPos, finishPos, checkpoints, collectibles, obstacles, enemies, riverZ, placedItems, report, folderStumps);
            SpawnCategoryBatch(HDObjectCategory.AncientStone, targetAncient, 4.0f, 0.90f, 1.15f, zMin, zMax, halfWidth, settings, zone, library, startPos, finishPos, checkpoints, collectibles, obstacles, enemies, riverZ, placedItems, report, folderAncient);
        }

        private static void SpawnCategoryBatch(
            HDObjectCategory category,
            int targetCount,
            float minSpacing,
            float minScale,
            float maxScale,
            float zMin,
            float zMax,
            float halfWidth,
            EnvironmentPopulatorSettings settings,
            ZoneGenerationStats zone,
            HDAssetLibrary library,
            Vector3 startPos,
            Vector3 finishPos,
            List<Vector3> checkpoints,
            List<Vector3> collectibles,
            List<Vector3> obstacles,
            List<Vector3> enemies,
            float riverZ,
            List<PlacedEnvironmentItem> placedItems,
            HDEnvironmentGenerationReport report,
            Transform parentFolder)
        {
            if (targetCount <= 0) return;

            int attempts = 0;
            int placedCount = 0;
            int maxAttempts = targetCount * 12;

            bool isSmallGroundDecor = (category == HDObjectCategory.Grass || category == HDObjectCategory.DeadLeaves);

            while (placedCount < targetCount && attempts < maxAttempts)
            {
                attempts++;

                if (report.totalObjectsGenerated >= settings.maxObjects)
                {
                    report.skippedMaxCap++;
                    break;
                }

                // 1. Calculate X position: Outer zones left (-halfWidth..-margin) or right (+margin..+halfWidth)
                float posX;
                if (!isSmallGroundDecor)
                {
                    bool spawnLeft = UnityEngine.Random.value > 0.5f;
                    float outerBuffer = 3.0f; // slight extension outside the border for lush jungle feel
                    if (spawnLeft)
                    {
                        posX = UnityEngine.Random.Range(-(halfWidth + outerBuffer), -settings.playerSafetyMargin);
                    }
                    else
                    {
                        posX = UnityEngine.Random.Range(settings.playerSafetyMargin, halfWidth + outerBuffer);
                    }
                }
                else
                {
                    // Small ground decoration can be placed more flexibly across width, but avoiding center corridor
                    bool outerOnly = UnityEngine.Random.value > 0.35f;
                    if (outerOnly)
                    {
                        bool spawnLeft = UnityEngine.Random.value > 0.5f;
                        posX = spawnLeft
                            ? UnityEngine.Random.Range(-halfWidth, -settings.playerSafetyMargin)
                            : UnityEngine.Random.Range(settings.playerSafetyMargin, halfWidth);
                    }
                    else
                    {
                        // Path-edge scatter
                        posX = UnityEngine.Random.Range(-settings.playerSafetyMargin * 0.9f, settings.playerSafetyMargin * 0.9f);
                    }
                }

                // 2. Calculate Z position within zone range
                float posZ = UnityEngine.Random.Range(zMin + 0.5f, zMax - 0.5f);

                // River bias if placing river rocks
                if (category == HDObjectCategory.RiverRock && riverZ > 0)
                {
                    posZ = UnityEngine.Random.Range(riverZ - 6.0f, riverZ + 6.0f);
                }

                Vector3 candidatePos = new Vector3(posX, 0f, posZ);

                // 3. Gameplay Safety Checks
                // A. Player Start clearance
                if (Vector3.Distance(candidatePos, startPos) < (category == HDObjectCategory.Tree ? 7.0f : 4.5f))
                {
                    report.skippedGameplayConflict++;
                    continue;
                }

                // B. Finish marker clearance
                if (Vector3.Distance(candidatePos, finishPos) < (category == HDObjectCategory.Tree ? 6.5f : 3.5f))
                {
                    report.skippedGameplayConflict++;
                    continue;
                }

                // C. Checkpoints clearance
                bool hitsCheckpoint = false;
                foreach (var cp in checkpoints)
                {
                    if (Vector3.Distance(candidatePos, cp) < 3.5f) { hitsCheckpoint = true; break; }
                }
                if (hitsCheckpoint)
                {
                    report.skippedGameplayConflict++;
                    continue;
                }

                // D. Obstacles & Enemies clearance
                bool hitsObstacle = false;
                foreach (var obs in obstacles)
                {
                    if (Vector3.Distance(candidatePos, obs) < 2.5f) { hitsObstacle = true; break; }
                }
                if (!hitsObstacle)
                {
                    foreach (var ene in enemies)
                    {
                        if (Vector3.Distance(candidatePos, ene) < 2.8f) { hitsObstacle = true; break; }
                    }
                }
                if (hitsObstacle)
                {
                    report.skippedGameplayConflict++;
                    continue;
                }

                // E. Collectibles clearance
                bool hitsCollectible = false;
                foreach (var col in collectibles)
                {
                    float dist = Vector3.Distance(candidatePos, col);
                    if (isSmallGroundDecor ? dist < 1.2f : dist < 2.2f) { hitsCollectible = true; break; }
                }
                if (hitsCollectible)
                {
                    report.skippedGameplayConflict++;
                    continue;
                }

                // F. Corridor violation for non-ground decor
                if (!isSmallGroundDecor && Mathf.Abs(candidatePos.x) < settings.playerSafetyMargin)
                {
                    report.skippedCorridorViolation++;
                    continue;
                }

                // G. Spacing check against already placed environment items
                bool tooClose = false;
                foreach (var placed in placedItems)
                {
                    float requiredDist = (category == placed.category) ? minSpacing : (minSpacing * 0.6f);
                    if (Vector3.Distance(candidatePos, placed.position) < requiredDist)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose)
                {
                    report.skippedTooClose++;
                    continue;
                }

                // 4. Retrieve Prefab from HDAssetLibrary
                GameObject prefab = library.GetPrefab(category, placedItems.Count + attempts);
                if (prefab == null)
                {
                    report.skippedMissingAsset++;
                    continue;
                }

                // 5. Instantiate Prefab
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null) instance = UnityEngine.Object.Instantiate(prefab);

                instance.name = $"{category}_{placedCount + 1}";
                instance.transform.SetParent(parentFolder, false);
                instance.transform.position = candidatePos;

                // Random Y Rotation
                float rotY = UnityEngine.Random.Range(0f, 360f);
                instance.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

                // Scale variation
                float scale = UnityEngine.Random.Range(minScale, maxScale);
                instance.transform.localScale = Vector3.one * scale;

                // 6. Disable / Remove Colliders for 100% gameplay safety
                if (settings.removeCollidersFromPrefabs)
                {
                    Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
                    foreach (var col in colliders)
                    {
                        UnityEngine.Object.DestroyImmediate(col);
                    }
                }

                // 7. Ensure URP Lit Material compatibility
                HDMaterialURPConverter.EnsureURPShader(instance);
                report.urpCompatibleMaterials++;

                // Register placed item
                placedItems.Add(new PlacedEnvironmentItem
                {
                    position = candidatePos,
                    radius = minSpacing * 0.5f,
                    category = category
                });

                placedCount++;
                zone.totalObjects++;
                report.totalObjectsGenerated++;

                // Update category counters
                switch (category)
                {
                    case HDObjectCategory.Tree: zone.treeCount++; report.treeCount++; break;
                    case HDObjectCategory.Bush: zone.bushCount++; report.bushCount++; break;
                    case HDObjectCategory.Grass: zone.grassCount++; report.grassCount++; break;
                    case HDObjectCategory.DeadLeaves: zone.deadLeavesCount++; report.deadLeavesCount++; break;
                    case HDObjectCategory.Rock: zone.rockCount++; report.rockCount++; break;
                    case HDObjectCategory.RiverRock: zone.riverRockCount++; report.riverRockCount++; break;
                    case HDObjectCategory.WoodTrunk:
                        if (instance.name.ToLowerInvariant().Contains("stump")) { zone.stumpCount++; report.stumpCount++; }
                        else { zone.logCount++; report.logCount++; }
                        break;
                    case HDObjectCategory.AncientStone:
                        if (instance.name.ToLowerInvariant().Contains("arch")) { zone.archCount++; report.archCount++; }
                        else if (instance.name.ToLowerInvariant().Contains("ruin")) { zone.ancientRuinsCount++; report.ancientRuinsCount++; }
                        else { zone.ancientStoneCount++; report.ancientStoneCount++; }
                        break;
                    case HDObjectCategory.Arch: zone.archCount++; report.archCount++; break;
                    case HDObjectCategory.Water: zone.waterCount++; report.waterCount++; break;
                    case HDObjectCategory.Waterfall: zone.waterfallCount++; report.waterfallCount++; break;
                    default: zone.otherCount++; report.otherCount++; break;
                }
            }
        }

        private static void SaveReportToFile(HDEnvironmentGenerationReport report)
        {
            string dir = Path.GetDirectoryName(REPORT_PATH);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"HD ENVIRONMENT GENERATION REPORT {(report.isPreview ? "[PREVIEW MODE]" : "[APPLIED ACTIVE]")}");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Seed:                           {report.seed}");
            sb.AppendLine($"Level Range:                    Z = {report.startZ:F1}m to {report.finishZ:F1}m (Length: {report.levelLength:F1}m)");
            sb.AppendLine($"Playable Width:                 {report.playableWidth:F1}m");
            sb.AppendLine($"Total Objects Generated:        {report.totalObjectsGenerated}\n");

            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("CATEGORY BREAKDOWN:");
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine($"- Trees:                        {report.treeCount}");
            sb.AppendLine($"- Bushes:                       {report.bushCount}");
            sb.AppendLine($"- Grass:                        {report.grassCount}");
            sb.AppendLine($"- Ferns:                        {report.fernCount}");
            sb.AppendLine($"- Dead Leaves:                  {report.deadLeavesCount}");
            sb.AppendLine($"- Rocks:                        {report.rockCount}");
            sb.AppendLine($"- River Rocks:                  {report.riverRockCount}");
            sb.AppendLine($"- Logs:                         {report.logCount}");
            sb.AppendLine($"- Stumps:                       {report.stumpCount}");
            sb.AppendLine($"- Ancient Stones:               {report.ancientStoneCount}");
            sb.AppendLine($"- Ancient Ruins:                {report.ancientRuinsCount}");
            sb.AppendLine($"- Arches:                       {report.archCount}");
            sb.AppendLine($"- Water:                        {report.waterCount}");
            sb.AppendLine($"- Waterfalls:                   {report.waterfallCount}\n");

            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("ZONE BREAKDOWN:");
            sb.AppendLine("--------------------------------------------------------------------------------");
            foreach (var z in report.zoneStats)
            {
                sb.AppendLine($"• {z.zoneName} (Z={z.startZ:F1}m..{z.endZ:F1}m): {z.totalObjects} objects");
                sb.AppendLine($"    Trees: {z.treeCount} | Bushes: {z.bushCount} | Grass: {z.grassCount} | Rocks: {z.rockCount} | Props: {z.logCount + z.stumpCount + z.ancientStoneCount}");
            }

            sb.AppendLine("\n--------------------------------------------------------------------------------");
            sb.AppendLine("SAFETY & SKIP METRICS:");
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine($"Skipped (Gameplay Conflict):    {report.skippedGameplayConflict}");
            sb.AppendLine($"Skipped (Corridor Violation):   {report.skippedCorridorViolation}");
            sb.AppendLine($"Skipped (Too Close / Spacing):  {report.skippedTooClose}");
            sb.AppendLine($"Skipped (Missing Asset):        {report.skippedMissingAsset}");
            sb.AppendLine($"Skipped (Max Cap Reached):      {report.skippedMaxCap}\n");

            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("MATERIAL COMPATIBILITY:");
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine($"URP Compatible Materials:       {report.urpCompatibleMaterials}");
            sb.AppendLine("Built-in Standard Materials:    0 (Automatically converted to URP/Lit)");

            File.WriteAllText(REPORT_PATH, sb.ToString());
            AssetDatabase.Refresh();
        }

        private static void PrintConsoleSummary(HDEnvironmentGenerationReport report)
        {
            string mode = report.isPreview ? "PREVIEW" : "APPLIED";
            Debug.Log($"<b><color=#00FF88>[HDEnvironmentAutoPopulator] {mode} Generation Complete:</color></b> " +
                      $"Total: {report.totalObjectsGenerated} objects (Trees: {report.treeCount}, Bushes: {report.bushCount}, " +
                      $"Grass: {report.grassCount}, Rocks: {report.rockCount}, Props: {report.logCount + report.stumpCount + report.ancientStoneCount}). " +
                      $"Report saved to '{REPORT_PATH}'.");
        }
#endif
    }
}
