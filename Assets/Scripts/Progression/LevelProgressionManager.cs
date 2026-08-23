using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MonkeyAdventure.Core;
using MonkeyAdventure.Monetization;

namespace MonkeyAdventure.Progression
{
    /// <summary>
    /// Master Level Progression Architecture for Monkey Adventure (Levels 1 to 50).
    /// Manages unlocked levels, act progression, boss thresholds, save/load via PlayerPrefs,
    /// dynamic scene loading, and safe procedural fallback when transitioning between levels.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Progression/Level Progression Manager")]
    [DisallowMultipleComponent]
    public class LevelProgressionManager : MonoBehaviour
    {
        public static LevelProgressionManager Instance { get; private set; }

        public const int TOTAL_LEVELS = 50;
        private const string PREFS_UNLOCKED_LEVEL = "MonkeyAdventure_MaxUnlockedLevel";
        private const string PREFS_CURRENT_LEVEL = "MonkeyAdventure_CurrentLevelIndex";
        private const string PREFS_HIGH_SCORE_PREFIX = "MonkeyAdventure_LevelScore_";

        [Header("Progression State")]
        [Tooltip("The currently active level index (1 to 50).")]
        [SerializeField] private int currentLevelIndex = 1;

        [Tooltip("The highest level unlocked by the player.")]
        [SerializeField] private int maxUnlockedLevel = 1;

        [Header("Act Settings")]
        [SerializeField] private bool autoLoadNextOnComplete = true;
        [SerializeField] private float levelCompleteDelay = 2.0f;

        #region Events
        public event Action<int> OnLevelStarted;
        public event Action<int, int> OnLevelCompleted; // (levelIndex, stars/score)
        public event Action<int> OnLevelUnlocked;
        #endregion

        #region Public Properties
        public int CurrentLevelIndex => currentLevelIndex;
        public int MaxUnlockedLevel => maxUnlockedLevel;
        public int TotalLevels => TOTAL_LEVELS;
        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadProgression();
        }

        private void Start()
        {
            // Determine level from active scene name if possible
            string activeScene = SceneManager.GetActiveScene().name;
            int detectedLevel = ExtractLevelFromSceneName(activeScene);
            if (detectedLevel > 0)
            {
                currentLevelIndex = detectedLevel;
            }

            OnLevelStarted?.Invoke(currentLevelIndex);
        }

        #region Save / Load Progression
        /// <summary>
        /// Loads unlocked level and current level index from PlayerPrefs.
        /// </summary>
        public void LoadProgression()
        {
            maxUnlockedLevel = PlayerPrefs.GetInt(PREFS_UNLOCKED_LEVEL, 1);
            currentLevelIndex = PlayerPrefs.GetInt(PREFS_CURRENT_LEVEL, 1);
            maxUnlockedLevel = Mathf.Clamp(maxUnlockedLevel, 1, TOTAL_LEVELS);
            currentLevelIndex = Mathf.Clamp(currentLevelIndex, 1, TOTAL_LEVELS);
        }

        /// <summary>
        /// Persists current progression to PlayerPrefs.
        /// </summary>
        public void SaveProgression()
        {
            PlayerPrefs.SetInt(PREFS_UNLOCKED_LEVEL, maxUnlockedLevel);
            PlayerPrefs.SetInt(PREFS_CURRENT_LEVEL, currentLevelIndex);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Resets all saved progression to Level 01 for debugging / testing.
        /// </summary>
        public void ResetProgression()
        {
            maxUnlockedLevel = 1;
            currentLevelIndex = 1;
            PlayerPrefs.DeleteKey(PREFS_UNLOCKED_LEVEL);
            PlayerPrefs.DeleteKey(PREFS_CURRENT_LEVEL);
            PlayerPrefs.Save();
            Debug.Log("[LevelProgressionManager] Progression successfully reset to Level 01.");
        }
        #endregion

        #region Level Completion & Flow
        /// <summary>
        /// Triggered when the player reaches the exit gateway portal of the current level.
        /// </summary>
        public void CompleteCurrentLevel(int score = 100)
        {
            Debug.Log($"[LevelProgressionManager] 🎉 Level {currentLevelIndex} COMPLETED with score {score}!");

            // Record score
            string scoreKey = $"{PREFS_HIGH_SCORE_PREFIX}{currentLevelIndex}";
            int existingBest = PlayerPrefs.GetInt(scoreKey, 0);
            if (score > existingBest)
            {
                PlayerPrefs.SetInt(scoreKey, score);
            }

            // Unlock next level if applicable
            if (currentLevelIndex >= maxUnlockedLevel && maxUnlockedLevel < TOTAL_LEVELS)
            {
                maxUnlockedLevel = currentLevelIndex + 1;
                OnLevelUnlocked?.Invoke(maxUnlockedLevel);
                Debug.Log($"[LevelProgressionManager] 🔓 Unlocked Level {maxUnlockedLevel}!");
            }

            SaveProgression();
            OnLevelCompleted?.Invoke(currentLevelIndex, score);

            if (autoLoadNextOnComplete)
            {
                if (currentLevelIndex < TOTAL_LEVELS)
                {
                    StartCoroutine(DelayedLoadNextLevel(currentLevelIndex + 1));
                }
                else
                {
                    Debug.Log("[LevelProgressionManager] 🏆 CONGRATULATIONS! ALL 50 LEVELS COMPLETED!");
                }
            }
        }

        private System.Collections.IEnumerator DelayedLoadNextLevel(int nextLevel)
        {
            if (levelCompleteDelay > 0f)
            {
                yield return new WaitForSeconds(levelCompleteDelay);
            }
            LoadLevel(nextLevel);
        }

        /// <summary>
        /// Loads the specified level by index (1 to 50).
        /// </summary>
        public void LoadLevel(int levelIndex)
        {
            if (levelIndex < 1 || levelIndex > TOTAL_LEVELS)
            {
                Debug.LogWarning($"[LevelProgressionManager] Invalid level index: {levelIndex}");
                return;
            }

            currentLevelIndex = levelIndex;
            SaveProgression();

            string sceneName = GetSceneNameForLevel(levelIndex);

            // Check if scene is in build settings
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.Log($"[LevelProgressionManager] Loading scene '{sceneName}'...");
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning($"[LevelProgressionManager] Scene '{sceneName}' is not in BuildSettings. Falling back to dynamic Level 01 with Act parameters...");
                // If scene is missing, load Level 01 as playable template
                if (Application.CanStreamedLevelBeLoaded("Level01_Awakening"))
                {
                    SceneManager.LoadScene("Level01_Awakening");
                }
                else if (SceneManager.sceneCountInBuildSettings > 0)
                {
                    SceneManager.LoadScene(0);
                }
            }

            OnLevelStarted?.Invoke(currentLevelIndex);
        }

        /// <summary>
        /// Replays a previously completed or currently active level.
        /// </summary>
        public void ReplayLevel(int levelIndex)
        {
            if (IsLevelUnlocked(levelIndex))
            {
                LoadLevel(levelIndex);
            }
            else
            {
                Debug.LogWarning($"[LevelProgressionManager] Level {levelIndex} is locked! Max unlocked: {maxUnlockedLevel}");
            }
        }

        /// <summary>
        /// Returns true if the requested level is unlocked.
        /// </summary>
        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex <= maxUnlockedLevel && levelIndex >= 1;
        }
        #endregion

        #region Campaign Act Mapping & Metadata
        /// <summary>
        /// Returns the Act index (1 to 5) for a given level.
        /// </summary>
        public static int GetActIndex(int levelIndex)
        {
            if (levelIndex <= 10) return 1;
            if (levelIndex <= 20) return 2;
            if (levelIndex <= 30) return 3;
            if (levelIndex <= 40) return 4;
            return 5;
        }

        /// <summary>
        /// Returns the human-readable Act title.
        /// </summary>
        public static string GetActName(int levelIndex)
        {
            int act = GetActIndex(levelIndex);
            switch (act)
            {
                case 1: return "Act 1: The Awakening";
                case 2: return "Act 2: The Lost Forest";
                case 3: return "Act 3: The Rise";
                case 4: return "Act 4: The Dark Forest";
                case 5: return "Act 5: Final Guardian";
                default: return "Act 1: The Awakening";
            }
        }

        /// <summary>
        /// Returns true if the level is a climax Boss encounter (Levels 10, 20, 30, 40, 50).
        /// </summary>
        public static bool IsBossLevel(int levelIndex)
        {
            return levelIndex % 10 == 0;
        }

        /// <summary>
        /// Returns the expected boss type/name for a given climax level.
        /// </summary>
        public static string GetBossNameForLevel(int levelIndex)
        {
            switch (levelIndex)
            {
                case 10: return "Alpha Jaguar";
                case 20: return "Stone Golem";
                case 30: return "River Serpent";
                case 40: return "Shadow Beast";
                case 50: return "Final Corruptor";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Converts level index to formatted scene filename.
        /// </summary>
        public static string GetSceneNameForLevel(int levelIndex)
        {
            if (levelIndex == 1) return "Level01_Awakening";
            if (levelIndex == 10) return "Level10_AlphaJaguarBoss";
            if (levelIndex == 20) return "Level20_StoneGolemBoss";
            if (levelIndex == 30) return "Level30_RiverSerpentBoss";
            if (levelIndex == 40) return "Level40_ShadowBeastBoss";
            if (levelIndex == 50) return "Level50_FinalBossCorruptor";

            int act = GetActIndex(levelIndex);
            return $"Level{levelIndex:D2}_Act{act}";
        }

        private int ExtractLevelFromSceneName(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return -1;
            if (sceneName.StartsWith("Level", StringComparison.OrdinalIgnoreCase) && sceneName.Length >= 7)
            {
                string numPart = sceneName.Substring(5, 2);
                if (int.TryParse(numPart, out int lvl))
                {
                    return lvl;
                }
            }
            return -1;
        }
        #endregion
    }
}
