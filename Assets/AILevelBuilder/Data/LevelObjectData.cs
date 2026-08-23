using System;
using UnityEngine;
using MonkeyAdventure.AILevelBuilder;

namespace MonkeyAdventure.AILevelBuilder
{
    /// <summary>
    /// Categorization of objects that can be placed by the AI Level Builder.
    /// </summary>
    public enum ObjectCategory
    {
        Environment,
        Tree,
        Rock,
        Bush,
        Grass,
        Fruit,
        Collectible,
        Obstacle,
        Enemy,
        Checkpoint,
        StartMarker,
        FinishMarker,
        Custom
    }

    /// <summary>
    /// Represents an individual object or spawn point definition within a level.
    /// Serializable for Unity Inspector editing, JSON serialization, and procedural level assembly.
    /// </summary>
    [Serializable]
    public class LevelObjectData
    {
        [Header("Identity")]
        [Tooltip("Human-readable identifier for this object instance.")]
        public string objectName = "Object";

        [Tooltip("The semantic category of this object.")]
        public ObjectCategory category = ObjectCategory.Environment;

        [Header("Prefab Reference")]
        [Tooltip("Optional prefab reference to instantiate. If null, a labeled LevelMarker placeholder is created.")]
        public GameObject prefab;

        [Header("Transform")]
        [Tooltip("World space position where this object should be placed.")]
        public Vector3 position = Vector3.zero;

        [Tooltip("Euler rotation angles for this object.")]
        public Vector3 rotation = Vector3.zero;

        [Tooltip("Scale for this object.")]
        public Vector3 scale = Vector3.one;

        [Header("Randomization Options")]
        [Tooltip("If true, a random Y-axis rotation within the specified range is applied on generation.")]
        public bool randomRotationY = false;
        public Vector2 randomRotationYRange = new Vector2(0f, 360f);

        [Tooltip("If true, a uniform random scale multiplier within the specified range is applied on generation.")]
        public bool randomScale = false;
        public Vector2 randomScaleRange = new Vector2(0.85f, 1.15f);

        [Header("State & Gameplay Meta")]
        [Tooltip("Whether this object is active in the scene upon generation.")]
        public bool isActive = true;

        [Tooltip("Optional gameplay section name or notes (e.g. 'Fruit Collection Area', 'River Crossing').")]
        public string sectionName = "";

        [Tooltip("Additional parameters or metadata for custom gameplay systems.")]
        [TextArea(1, 3)]
        public string notes = "";

        public LevelObjectData()
        {
            scale = Vector3.one;
            isActive = true;
        }

        public LevelObjectData(string name, ObjectCategory cat, GameObject prefabRef, Vector3 pos, Vector3 rot, Vector3 scl, string section = "")
        {
            objectName = name;
            category = cat;
            prefab = prefabRef;
            position = pos;
            rotation = rot;
            scale = scl;
            isActive = true;
            sectionName = section;
        }
    }
}
