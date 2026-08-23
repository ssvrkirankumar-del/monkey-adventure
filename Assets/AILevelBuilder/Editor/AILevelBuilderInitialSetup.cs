using System.IO;
using UnityEngine;
using UnityEditor;
using MonkeyAdventure.AILevelBuilder;

namespace MonkeyAdventure.AILevelBuilder.Editor
{
    /// <summary>
    /// Initializer script that creates required folder structures and prepares the
    /// initial Level 01 LevelDefinition ScriptableObject asset ('The Awakening').
    /// </summary>
    [InitializeOnLoad]
    public static class AILevelBuilderInitialSetup
    {
        private const string LEVEL_01_DEF_PATH = "Assets/Levels/Level_01/Level01_Definition.asset";

        static AILevelBuilderInitialSetup()
        {
            EditorApplication.delayCall += Initialize;
        }

        [MenuItem("Window/Monkey Adventure/Initialize Folders & Level 01 Asset", false, 110)]
        public static void Initialize()
        {
            CreateFolders();
            CreateOrUpdateLevel01Definition();
        }

        private static void CreateFolders()
        {
            string[] folders = new string[]
            {
                "Assets/AILevelBuilder",
                "Assets/AILevelBuilder/Scripts",
                "Assets/AILevelBuilder/Data",
                "Assets/AILevelBuilder/Prefabs",
                "Assets/AILevelBuilder/Generators",
                "Assets/AILevelBuilder/Editor",
                "Assets/Game",
                "Assets/Game/Player",
                "Assets/Game/Environment",
                "Assets/Game/Enemies",
                "Assets/Game/Collectibles",
                "Assets/Levels",
                "Assets/Levels/Level_01"
            };

            bool createdAny = false;
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                    createdAny = true;
                }
            }

            if (createdAny)
            {
                AssetDatabase.Refresh();
                Debug.Log("[AILevelBuilder] Verified and created all required project folder structures.");
            }
        }

        public static void CreateOrUpdateLevel01Definition()
        {
            if (File.Exists(LEVEL_01_DEF_PATH))
            {
                return; // Asset already exists, avoid overwriting user edits
            }

            LevelDefinition def = ScriptableObject.CreateInstance<LevelDefinition>();
            def.levelId = 1;
            def.levelName = "The Awakening";
            def.levelDescription = "Level 01: The Awakening\n" +
                                   "Journey through the lush tropical jungle path, gather ripe fruits, overcome basic terrain obstacles and the first predator encounter, cross the jungle river, and explore the ancient forest sanctuary to reach the sacred completion portal.";

            // 1. START
            def.startPosition = new Vector3(0f, 1f, 0f);

            // 2. CHECKPOINT
            def.checkpointPositions.Add(new Vector3(0f, 1.5f, 87f));

            // 3. FINISH
            def.finishPosition = new Vector3(0f, 1.5f, 108f);

            // 4. JUNGLE PATH (Z: 0 - 15)
            def.environmentObjectSpawnData.Add(new LevelObjectData(
                "JunglePath_Foliage_Left",
                ObjectCategory.Environment,
                null,
                new Vector3(-4f, 0f, 7.5f),
                Vector3.zero,
                Vector3.one,
                "Jungle Path"));

            def.environmentObjectSpawnData.Add(new LevelObjectData(
                "JunglePath_Foliage_Right",
                ObjectCategory.Environment,
                null,
                new Vector3(4f, 0f, 7.5f),
                Vector3.zero,
                Vector3.one,
                "Jungle Path"));

            // 5. FRUIT COLLECTION AREA (Z: 15 - 25)
            def.collectibleSpawnData.Add(new LevelObjectData(
                "Fruit_Cluster_01",
                ObjectCategory.Fruit,
                null,
                new Vector3(-1.5f, 1.2f, 18f),
                Vector3.zero,
                Vector3.one,
                "Fruit Collection Area"));

            def.collectibleSpawnData.Add(new LevelObjectData(
                "Fruit_Cluster_02",
                ObjectCategory.Fruit,
                null,
                new Vector3(0f, 1.2f, 20f),
                Vector3.zero,
                Vector3.one,
                "Fruit Collection Area"));

            def.collectibleSpawnData.Add(new LevelObjectData(
                "Fruit_Cluster_03",
                ObjectCategory.Fruit,
                null,
                new Vector3(1.5f, 1.2f, 22f),
                Vector3.zero,
                Vector3.one,
                "Fruit Collection Area"));

            // 6. BASIC OBSTACLE (Z: 25 - 35)
            def.obstacleSpawnData.Add(new LevelObjectData(
                "Basic_Jungle_Obstacle_Boulders",
                ObjectCategory.Obstacle,
                null,
                new Vector3(0f, 0.5f, 30f),
                Vector3.zero,
                new Vector3(1.5f, 1.5f, 1.5f),
                "Basic Obstacle"));

            // 7. FIRST ENEMY ENCOUNTER (Z: 35 - 45)
            def.enemySpawnData.Add(new LevelObjectData(
                "Enemy_Encounter_01_JungleCreature",
                ObjectCategory.Enemy,
                null,
                new Vector3(0f, 0.5f, 40f),
                new Vector3(0f, 180f, 0f),
                Vector3.one,
                "First Enemy Encounter"));

            // 8. RIVER / CROSSING AREA (Z: 45 - 60)
            def.environmentObjectSpawnData.Add(new LevelObjectData(
                "River_Crossing_SteppingStones",
                ObjectCategory.Environment,
                null,
                new Vector3(0f, 0.2f, 52f),
                Vector3.zero,
                Vector3.one,
                "River / Crossing Area"));

            // 9. ANCIENT FOREST AREA (Z: 60 - 85)
            def.environmentObjectSpawnData.Add(new LevelObjectData(
                "Ancient_Forest_Canopy_Trees",
                ObjectCategory.Tree,
                null,
                new Vector3(-6f, 1.5f, 72f),
                Vector3.zero,
                Vector3.one,
                "Ancient Forest Area"));

            def.collectibleSpawnData.Add(new LevelObjectData(
                "AncientForest_GoldenFruit",
                ObjectCategory.Collectible,
                null,
                new Vector3(0f, 2f, 75f),
                Vector3.zero,
                Vector3.one,
                "Ancient Forest Area"));

            // 10. CHECKPOINT (Z: 87)
            def.customObjects.Add(new LevelObjectData(
                "Checkpoint_Marker_Sanctuary",
                ObjectCategory.Checkpoint,
                null,
                new Vector3(0f, 1.5f, 87f),
                Vector3.zero,
                Vector3.one,
                "Checkpoint"));

            // 11. FINISH (Z: 108)
            def.customObjects.Add(new LevelObjectData(
                "Finish_Portal_Gateway",
                ObjectCategory.FinishMarker,
                null,
                new Vector3(0f, 1.5f, 108f),
                Vector3.zero,
                Vector3.one,
                "Finish"));

            AssetDatabase.CreateAsset(def, LEVEL_01_DEF_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00FF88><b>[AILevelBuilder] Successfully created Level 01 Definition asset at: {LEVEL_01_DEF_PATH}</b></color>");
        }
    }
}
