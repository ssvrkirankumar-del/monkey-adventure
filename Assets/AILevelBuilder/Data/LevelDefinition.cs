using System;
using System.Collections.Generic;
using UnityEngine;
using MonkeyAdventure.AILevelBuilder;

namespace MonkeyAdventure.AILevelBuilder
{
    /// <summary>
    /// ScriptableObject storing the complete configuration and object placement data for a level.
    /// Used by the AI Level Builder to generate, inspect, and modify level layouts.
    /// </summary>
    [CreateAssetMenu(fileName = "New_LevelDefinition", menuName = "Monkey Adventure/AI Level Builder/Level Definition", order = 10)]
    public class LevelDefinition : ScriptableObject
    {
        [Header("Level Identity")]
        [Tooltip("Unique numerical identifier for the level (e.g. 1 for Level 1).")]
        public int levelId = 1;

        [Tooltip("Name of the level (e.g. 'The Awakening').")]
        public string levelName = "Level 01";

        [Tooltip("Detailed description of the level, story context, and design goals.")]
        [TextArea(3, 6)]
        public string levelDescription = "Level 01: The Awakening";

        [Header("Navigation & Boundary Points")]
        [Tooltip("Player spawn point at level start.")]
        public Vector3 startPosition = new Vector3(0f, 1f, 0f);

        [Tooltip("Level completion goal / exit portal position.")]
        public Vector3 finishPosition = new Vector3(0f, 1f, 108f);

        [Tooltip("Intermediate progression checkpoint positions.")]
        public List<Vector3> checkpointPositions = new List<Vector3>();

        [Header("Gameplay Sequence Data")]
        [Tooltip("Collectible items (Bananas, Golden Idols, Power Orbs).")]
        public List<LevelObjectData> collectibleSpawnData = new List<LevelObjectData>();

        [Tooltip("Obstacles (Spikes, Rolling Boulders, Fire Traps, Puzzle Gates).")]
        public List<LevelObjectData> obstacleSpawnData = new List<LevelObjectData>();

        [Tooltip("Enemy spawn points and patrolling creatures (Jungle Spiders, Wild Boars, Bosses).")]
        public List<LevelObjectData> enemySpawnData = new List<LevelObjectData>();

        [Tooltip("Environment decorative objects (Ancient Trees, Boulders, Tropical Bushes, Foliage).")]
        public List<LevelObjectData> environmentObjectSpawnData = new List<LevelObjectData>();

        [Header("Custom / Miscellaneous Objects")]
        [Tooltip("Any additional custom gameplay objects or triggers.")]
        public List<LevelObjectData> customObjects = new List<LevelObjectData>();

        /// <summary>
        /// Total number of objects defined in this level configuration.
        /// </summary>
        public int TotalObjectCount =>
            (collectibleSpawnData != null ? collectibleSpawnData.Count : 0) +
            (obstacleSpawnData != null ? obstacleSpawnData.Count : 0) +
            (enemySpawnData != null ? enemySpawnData.Count : 0) +
            (environmentObjectSpawnData != null ? environmentObjectSpawnData.Count : 0) +
            (customObjects != null ? customObjects.Count : 0);

        /// <summary>
        /// Clears all spawn data lists.
        /// </summary>
        public void ClearAllData()
        {
            checkpointPositions.Clear();
            collectibleSpawnData.Clear();
            obstacleSpawnData.Clear();
            enemySpawnData.Clear();
            environmentObjectSpawnData.Clear();
            customObjects.Clear();
        }
    }
}
