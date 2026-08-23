using System;
using UnityEngine;
using MonkeyAdventure.AILevelBuilder;

namespace MonkeyAdventure.AILevelBuilder
{
    /// <summary>
    /// Central manager and controller for the AI Level Builder system.
    /// Provides programmatic hooks to generate, clear, and validate levels,
    /// as well as converting AI-structured level plans into serialized LevelDefinition assets.
    /// </summary>
    public class AILevelBuilderManager : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Active LevelDefinition to build or edit.")]
        [SerializeField] private LevelDefinition activeLevelDefinition;

        [Tooltip("Optional PrefabLibrary containing reusable game assets.")]
        [SerializeField] private PrefabLibrary prefabLibrary;

        [Header("Runtime State")]
        [SerializeField] private bool isLevelGenerated = false;

        public LevelDefinition ActiveLevelDefinition
        {
            get => activeLevelDefinition;
            set => activeLevelDefinition = value;
        }

        public PrefabLibrary PrefabLibrary
        {
            get => prefabLibrary;
            set => prefabLibrary = value;
        }

        public bool IsLevelGenerated => isLevelGenerated;

        /// <summary>
        /// Assembles the active level in the scene.
        /// </summary>
        public void BuildLevel()
        {
            if (activeLevelDefinition == null)
            {
                Debug.LogError("[AILevelBuilderManager] Active LevelDefinition is not assigned.");
                return;
            }

            GameObject root = LevelGenerator.GenerateLevel(activeLevelDefinition, prefabLibrary);
            isLevelGenerated = (root != null);
        }

        /// <summary>
        /// Clears all AI-generated level objects from the scene.
        /// </summary>
        public void ClearLevel()
        {
            LevelGenerator.ClearGeneratedLevel();
            isLevelGenerated = false;
        }

        /// <summary>
        /// Validates whether the active level definition contains valid start, finish, and essential navigation points.
        /// </summary>
        public bool ValidateActiveDefinition(out string validationMessage)
        {
            if (activeLevelDefinition == null)
            {
                validationMessage = "No LevelDefinition assigned.";
                return false;
            }

            if (activeLevelDefinition.startPosition == activeLevelDefinition.finishPosition)
            {
                validationMessage = "Warning: Start Position and Finish Position are identical.";
                return false;
            }

            validationMessage = $"Level {activeLevelDefinition.levelId} ('{activeLevelDefinition.levelName}') is valid with {activeLevelDefinition.TotalObjectCount} configured objects.";
            return true;
        }
    }
}
