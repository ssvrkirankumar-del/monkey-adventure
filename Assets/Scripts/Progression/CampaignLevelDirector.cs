using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using MonkeyAdventure.Core;
using MonkeyAdventure.Bosses;
using MonkeyAdventure.Environment;
using MonkeyAdventure.Hazards;
using MonkeyAdventure.Puzzles;
using MonkeyAdventure.Collectibles;
using MonkeyAdventure.Progression;

namespace MonkeyAdventure.Progression
{
    /// <summary>
    /// Runtime Campaign Level Director.
    /// Automatically detects the active level index, configures Act-specific environmental mechanics,
    /// obstacles, enemies, collectibles, checkpoints, and boss encounters for Levels 1 to 50.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Progression/Campaign Level Director")]
    [DisallowMultipleComponent]
    public class CampaignLevelDirector : MonoBehaviour
    {
        public static CampaignLevelDirector Instance { get; private set; }

        [Header("Level Configuration")]
        [SerializeField] private int activeLevel = 1;
        [SerializeField] private string actName = "Act 1: The Awakening";
        [SerializeField] private bool isBossLevel = false;

        [Header("Act 1-5 Environmental Theme Toggles")]
        [Tooltip("Active in Act 3 (The Rise) levels.")]
        [SerializeField] private bool enableWaterAndUpdrafts = false;

        [Tooltip("Active in Act 4 (The Dark Forest) levels.")]
        [SerializeField] private bool enableDarknessAndLightAura = false;

        [Tooltip("Active in Act 5 (Final Guardian) levels.")]
        [SerializeField] private bool enableCelestialMechanics = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            InitializeLevelSettings();
        }

        private void Start()
        {
            ApplyActThemeSettings();
        }

        private void InitializeLevelSettings()
        {
            if (LevelProgressionManager.Instance != null)
            {
                activeLevel = LevelProgressionManager.Instance.CurrentLevelIndex;
            }
            else
            {
                // Detect from scene name
                string sceneName = SceneManager.GetActiveScene().name;
                if (sceneName.Contains("10_AlphaJaguar")) activeLevel = 10;
                else if (sceneName.Contains("20_StoneGolem")) activeLevel = 20;
                else if (sceneName.Contains("30_RiverSerpent")) activeLevel = 30;
                else if (sceneName.Contains("40_ShadowBeast")) activeLevel = 40;
                else if (sceneName.Contains("50_FinalBoss")) activeLevel = 50;
                else activeLevel = 1;
            }

            actName = LevelProgressionManager.GetActName(activeLevel);
            isBossLevel = LevelProgressionManager.IsBossLevel(activeLevel);

            int actIndex = LevelProgressionManager.GetActIndex(activeLevel);
            enableWaterAndUpdrafts = (actIndex == 3);
            enableDarknessAndLightAura = (actIndex == 4);
            enableCelestialMechanics = (actIndex == 5);

            Debug.Log($"[CampaignLevelDirector] Initialized Level {activeLevel} ({actName}) | Boss Level: {isBossLevel}");
        }

        private void ApplyActThemeSettings()
        {
            // Adjust lighting or ambient settings per Act
            int actIndex = LevelProgressionManager.GetActIndex(activeLevel);

            Light dirLight = FindAnyObjectByType<Light>();
            if (dirLight != null && dirLight.type == LightType.Directional)
            {
                switch (actIndex)
                {
                    case 1: // The Awakening: Warm golden sunlight
                        dirLight.color = new Color(1f, 0.96f, 0.88f);
                        dirLight.intensity = 1.2f;
                        RenderSettings.ambientLight = new Color(0.3f, 0.35f, 0.25f);
                        break;
                    case 2: // The Lost Forest: Ancient mossy amber
                        dirLight.color = new Color(0.95f, 0.9f, 0.75f);
                        dirLight.intensity = 1.0f;
                        RenderSettings.ambientLight = new Color(0.25f, 0.3f, 0.25f);
                        break;
                    case 3: // The Rise: Bright tropical sky blue
                        dirLight.color = new Color(0.85f, 0.95f, 1f);
                        dirLight.intensity = 1.3f;
                        RenderSettings.ambientLight = new Color(0.2f, 0.35f, 0.45f);
                        break;
                    case 4: // The Dark Forest: Mysterious dark violet/indigo
                        dirLight.color = new Color(0.45f, 0.35f, 0.65f);
                        dirLight.intensity = 0.45f;
                        RenderSettings.ambientLight = new Color(0.08f, 0.05f, 0.15f);
                        break;
                    case 5: // Final Guardian: Celestial ethereal gold/white
                        dirLight.color = new Color(1f, 0.98f, 0.9f);
                        dirLight.intensity = 1.5f;
                        RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.45f);
                        break;
                }
            }
        }
    }
}
