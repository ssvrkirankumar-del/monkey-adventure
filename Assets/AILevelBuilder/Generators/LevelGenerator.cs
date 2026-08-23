using System;
using System.Collections.Generic;
using UnityEngine;
using MonkeyAdventure.AILevelBuilder;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonkeyAdventure.AILevelBuilder
{
    /// <summary>
    /// Core procedural assembler for the AI Level Builder system.
    /// Reads a LevelDefinition and instantiates/positions all gameplay elements
    /// under a dedicated, non-destructive scene hierarchy root ('AI_GENERATED_LEVEL').
    /// </summary>
    public static class LevelGenerator
    {
        public const string ROOT_NAME = "AI_GENERATED_LEVEL";
        public const string ENV_FOLDER = "Environment";
        public const string COLLECTIBLES_FOLDER = "Collectibles";
        public const string OBSTACLES_FOLDER = "Obstacles";
        public const string ENEMIES_FOLDER = "Enemies";
        public const string CHECKPOINTS_FOLDER = "Checkpoints";
        public const string START_FOLDER = "Start";
        public const string FINISH_FOLDER = "Finish";
        public const string CUSTOM_FOLDER = "Custom";

        /// <summary>
        /// Clears ONLY objects located under the AI_GENERATED_LEVEL root in the active scene.
        /// Preserves all other scene objects, cameras, player controllers, and managers.
        /// </summary>
        public static void ClearGeneratedLevel()
        {
            GameObject existingRoot = GameObject.Find(ROOT_NAME);
            if (existingRoot != null)
            {
#if UNITY_EDITOR
                Undo.DestroyObjectImmediate(existingRoot);
#else
                UnityEngine.Object.Destroy(existingRoot);
#endif
                Debug.Log($"[LevelGenerator] Cleared previous '{ROOT_NAME}' hierarchy.");
            }
            else
            {
                Debug.Log($"[LevelGenerator] No existing '{ROOT_NAME}' found to clear.");
            }
        }

        /// <summary>
        /// Generates the level in the active scene based on the supplied LevelDefinition.
        /// </summary>
        public static GameObject GenerateLevel(LevelDefinition definition, PrefabLibrary library = null)
        {
            if (definition == null)
            {
                Debug.LogError("[LevelGenerator] Cannot generate level: LevelDefinition is null.");
                return null;
            }

            // 1. Clear any previously generated level hierarchy
            ClearGeneratedLevel();

            // 2. Create the master root
            GameObject root = new GameObject(ROOT_NAME);
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(root, $"Generate Level: {definition.levelName}");
#endif

            // 3. Create organized category folders
            Transform startFolder = CreateCategoryFolder(root.transform, START_FOLDER);
            Transform finishFolder = CreateCategoryFolder(root.transform, FINISH_FOLDER);
            Transform checkpointsFolder = CreateCategoryFolder(root.transform, CHECKPOINTS_FOLDER);
            Transform envFolder = CreateCategoryFolder(root.transform, ENV_FOLDER);
            Transform collectiblesFolder = CreateCategoryFolder(root.transform, COLLECTIBLES_FOLDER);
            Transform obstaclesFolder = CreateCategoryFolder(root.transform, OBSTACLES_FOLDER);
            Transform enemiesFolder = CreateCategoryFolder(root.transform, ENEMIES_FOLDER);
            Transform customFolder = CreateCategoryFolder(root.transform, CUSTOM_FOLDER);

            // 4. Create Start Marker
            CreateNavigationMarker("Start_Point", definition.startPosition, LevelMarkerType.Start, startFolder, "Player Spawn");

            // 5. Create Finish Marker
            CreateNavigationMarker("Finish_Portal", definition.finishPosition, LevelMarkerType.Finish, finishFolder, "Level Exit");

            // 6. Create Checkpoints
            if (definition.checkpointPositions != null)
            {
                for (int i = 0; i < definition.checkpointPositions.Count; i++)
                {
                    Vector3 cpPos = definition.checkpointPositions[i];
                    CreateNavigationMarker($"Checkpoint_{i + 1:D2}", cpPos, LevelMarkerType.Checkpoint, checkpointsFolder, $"Checkpoint {i + 1}", i + 1);
                }
            }

            // 7. Place Environment Objects
            if (definition.environmentObjectSpawnData != null)
            {
                foreach (var objData in definition.environmentObjectSpawnData)
                {
                    PlaceObject(objData, envFolder, LevelMarkerType.EnvironmentObject);
                }
            }

            // 8. Place Collectibles
            if (definition.collectibleSpawnData != null)
            {
                foreach (var objData in definition.collectibleSpawnData)
                {
                    PlaceObject(objData, collectiblesFolder, LevelMarkerType.CollectibleSpawn);
                }
            }

            // 9. Place Obstacles
            if (definition.obstacleSpawnData != null)
            {
                foreach (var objData in definition.obstacleSpawnData)
                {
                    PlaceObject(objData, obstaclesFolder, LevelMarkerType.ObstacleSpawn);
                }
            }

            // 10. Place Enemies
            if (definition.enemySpawnData != null)
            {
                foreach (var objData in definition.enemySpawnData)
                {
                    PlaceObject(objData, enemiesFolder, LevelMarkerType.EnemySpawn);
                }
            }

            // 11. Place Custom Objects
            if (definition.customObjects != null)
            {
                foreach (var objData in definition.customObjects)
                {
                    PlaceObject(objData, customFolder, LevelMarkerType.Custom);
                }
            }

            Debug.Log($"<color=#00FF88><b>[LevelGenerator] Successfully assembled Level {definition.levelId}: '{definition.levelName}' ({definition.TotalObjectCount} objects placed).</b></color>");
            return root;
        }

        private static Transform CreateCategoryFolder(Transform parent, string name)
        {
            GameObject folder = new GameObject(name);
            folder.transform.SetParent(parent, false);
            return folder.transform;
        }

        private static GameObject CreateNavigationMarker(string name, Vector3 position, LevelMarkerType type, Transform parent, string label, int index = 0)
        {
            GameObject markerObj = new GameObject(name);
            markerObj.transform.position = position;
            markerObj.transform.rotation = Quaternion.identity;
            markerObj.transform.SetParent(parent, true);

            LevelMarker marker = markerObj.AddComponent<LevelMarker>();
            marker.MarkerType = type;
            marker.MarkerLabel = label;
            marker.MarkerIndex = index;

            return markerObj;
        }

        private static GameObject PlaceObject(LevelObjectData data, Transform parent, LevelMarkerType defaultMarkerType)
        {
            if (data == null) return null;

            Vector3 finalPos = data.position;
            Vector3 finalRotEuler = data.rotation;
            Vector3 finalScale = data.scale;

            // Apply optional random rotation
            if (data.randomRotationY)
            {
                float randomY = UnityEngine.Random.Range(data.randomRotationYRange.x, data.randomRotationYRange.y);
                finalRotEuler.y += randomY;
            }

            // Apply optional random uniform scale
            if (data.randomScale)
            {
                float scaleMult = UnityEngine.Random.Range(data.randomScaleRange.x, data.randomScaleRange.y);
                finalScale *= scaleMult;
            }

            GameObject instance;

            if (data.prefab != null)
            {
#if UNITY_EDITOR
                instance = (GameObject)PrefabUtility.InstantiatePrefab(data.prefab, parent);
                if (instance == null)
                {
                    instance = UnityEngine.Object.Instantiate(data.prefab, parent);
                }
#else
                instance = UnityEngine.Object.Instantiate(data.prefab, parent);
#endif
                instance.name = data.objectName;
                instance.transform.position = finalPos;
                instance.transform.rotation = Quaternion.Euler(finalRotEuler);
                instance.transform.localScale = finalScale;
            }
            else
            {
                // Create lightweight placeholder marker
                instance = new GameObject(data.objectName);
                instance.transform.position = finalPos;
                instance.transform.rotation = Quaternion.Euler(finalRotEuler);
                instance.transform.localScale = finalScale;
                instance.transform.SetParent(parent, true);
            }

            // Ensure LevelMarker component is present for inspector tracking and scene gizmos
            LevelMarker marker = instance.GetComponent<LevelMarker>();
            if (marker == null)
            {
                marker = instance.AddComponent<LevelMarker>();
            }
            marker.MarkerType = defaultMarkerType;
            marker.MarkerLabel = string.IsNullOrEmpty(data.objectName) ? data.category.ToString() : data.objectName;
            marker.SectionName = data.sectionName;

            instance.SetActive(data.isActive);
            return instance;
        }
    }
}
