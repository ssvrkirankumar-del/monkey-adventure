using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MonkeyAdventure.Player;
using MonkeyAdventure.Cameras;
using MonkeyAdventure.Core;
using MonkeyAdventure.Audio;
using MonkeyAdventure.Monetization;
using MonkeyAdventure.Skins;
using MonkeyAdventure.Collectibles;
using MonkeyAdventure.Hazards;
using MonkeyAdventure.Puzzles;
using MonkeyAdventure.Environment;
using MonkeyAdventure.Mechanics;
using MonkeyAdventure.AI;
using MonkeyAdventure.UI;
using MonkeyAdventure.Tutorial;
using MonkeyAdventure.Combat;
using MonkeyAdventure.Animation;
using MonkeyAdventure.Progression;
using MonkeyAdventure.Bosses;
using GuardianSystem.Combat;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Master 1-Click Game Builder & Production Android Pipeline for Monkey Adventure.
    /// Synthesizes and integrates real 3D stylized character models, bosses, enemies, wildlife,
    /// environment assets, particle VFX, custom audio clips, and mobile UI into playable scenes.
    /// Accessible via: Window > Monkey Adventure > Auto Setup & Build
    /// </summary>
    public class AutoGameBuilder : EditorWindow
    {
        private const string SCENE_PATH_L01 = "Assets/Scenes/Level01_Awakening.unity";
        private const string PREFAB_DIR = "Assets/Prefabs";
        private const string MATERIAL_DIR = "Assets/Materials";
        private const string SCENE_DIR = "Assets/Scenes";

        private static bool _isBuilding = false;

        [Header("Android APK Settings")]
        [SerializeField] private string apkFileName = "MonkeyAdventure_TestBuild.apk";
        [SerializeField] private bool openFolderAfterBuild = true;
        [SerializeField] private bool autoConnectProfiler = false;

        private Vector2 _scrollPos;
        private string _lastValidationSummary = "No validation run yet. Click 'Validate Current Game'.";
        private MessageType _validationMessageType = MessageType.Info;

        [MenuItem("Window/Monkey Adventure/Auto Setup & Build", false, 101)]
        public static void OpenWindow()
        {
            AutoGameBuilder window = GetWindow<AutoGameBuilder>("Auto Setup & Build", true);
            window.minSize = new Vector2(440, 640);
            window.Show();
        }

        [MenuItem("Window/Monkey Adventure/Build Level 01 (Playable)", false, 102)]
        public static void BuildLevel01PlayableFromCommandLine()
        {
            if (_isBuilding)
            {
                Debug.LogWarning("[AutoGameBuilder] A build is already in progress. Ignoring duplicate request.");
                return;
            }

            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Debug.LogWarning("[AutoGameBuilder] Build aborted: Editor is busy, compiling, or in/entering Play Mode.");
                return;
            }

            AutoGameBuilder builder = CreateInstance<AutoGameBuilder>();
            builder.BuildPlayableLevel01();
        }

        [MenuItem("Window/Monkey Adventure/Build All 50 Campaign Levels", false, 103)]
        public static void BuildAllCampaignLevelsFromCommandLine()
        {
            if (_isBuilding)
            {
                Debug.LogWarning("[AutoGameBuilder] A build is already in progress. Ignoring duplicate request.");
                return;
            }

            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Debug.LogWarning("[AutoGameBuilder] Build aborted: Editor is busy, compiling, or in/entering Play Mode.");
                return;
            }

            AutoGameBuilder builder = CreateInstance<AutoGameBuilder>();
            builder.BuildAllCampaignLevels();
        }

        [MenuItem("Window/Monkey Adventure/Validate Current Game", false, 104)]
        public static void ValidateCurrentGameFromCommandLine()
        {
            AutoGameBuilder builder = CreateInstance<AutoGameBuilder>();
            builder.RunSceneValidation();
        }

        [MenuItem("Window/Monkey Adventure/Fix All Materials and URP Pipeline", false, 105)]
        public static void FixAllMaterialsFromCommandLine()
        {
            EnsureURPRenderPipelineAsset();
            FixAllProjectMaterials();
        }

        [MenuItem("Window/Monkey Adventure/Clear Stuck Progress Bar", false, 999)]
        public static void ForceClearProgressBar()
        {
            _isBuilding = false;
            EditorUtility.ClearProgressBar();
            Debug.Log("[AutoGameBuilder] Progress bar force-cleared.");
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();

            EditorGUILayout.Space(10);
            DrawPlayableLevelBuilderSection();

            EditorGUILayout.Space(12);
            DrawHDEnvironmentSection();

            EditorGUILayout.Space(12);
            DrawCampaignBossLevelsSection();

            EditorGUILayout.Space(12);
            DrawValidationSection();

            EditorGUILayout.Space(12);
            DrawAndroidBuildSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.Space(6);
            GUILayout.Label("🐒 Monkey Adventure: Master Game Builder 🌴", headerStyle);
            EditorGUILayout.HelpBox("1-Click Playable Game Generation (Levels 1–50), 3D Asset Synthesis, Act Boss Arenas, Diagnostic Validation, and Android APK Pipeline.", MessageType.Info);
        }

        private void DrawPlayableLevelBuilderSection()
        {
            EditorGUILayout.LabelField("1. Level 01 Playable Generator", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox("Synthesizes Real 3D Models, Materials, VFX, Audio & UI, then Assembles Level 01 ('The Awakening') with Player Controller, Smooth Camera, Combat, Hazards, Enemies, Wildlife, Collectibles, Puzzles, Checkpoints, and UI Canvas.", MessageType.None);

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
            if (GUILayout.Button("🚀 BUILD LEVEL 01 - PLAYABLE (1-Click)", GUILayout.Height(40)))
            {
                BuildPlayableLevel01();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawHDEnvironmentSection()
        {
            EditorGUILayout.LabelField("2. Environment-Only HD Visual Pass (Level 01)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox("Upgrades Level 01 with high-fidelity PBR trees, rocks, plants, and ruins (23 distinct HD assets) while strictly preserving gameplay colliders, scripts, and anchors.", MessageType.None);

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
            if (GUILayout.Button("🌟 Apply Master Cinematic HD Pass (Level 01)", GUILayout.Height(36)))
            {
                HDLevel01CinematicIntegrator.ApplyCinematicHDPassLevel01();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🌿 Open HD Builder Tool", GUILayout.Height(28)))
            {
                HDEnvironmentBuilder.OpenWindow();
            }
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
            if (GUILayout.Button("✨ Apply HD Visual Pass (Full)", GUILayout.Height(28)))
            {
                HDEnvironmentBuilder.ApplyHDPremadePassCommandLine();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.35f, 0.75f, 0.9f);
            if (GUILayout.Button("🌱 Apply HD Terrain Pass", GUILayout.Height(28)))
            {
                HDTerrainBuilder.ApplyHDTerrainPassCommandLine();
            }
            GUI.backgroundColor = new Color(0.25f, 0.85f, 0.5f);
            if (GUILayout.Button("🌳 Apply Phase 3 Tree Pass", GUILayout.Height(28)))
            {
                HDTreeFoliageIntegrator.ApplyTreeFoliagePassCommandLine();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawCampaignBossLevelsSection()
        {
            EditorGUILayout.LabelField("3. Campaign Act Boss Arenas & Full Campaign", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🐾 Act 1: Level 10 (Jaguar Boss)")) BuildLevel10_AlphaJaguarBoss();
            if (GUILayout.Button("🗿 Act 2: Level 20 (Golem Boss)")) BuildLevel20_StoneGolemBoss();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🐍 Act 3: Level 30 (Serpent Boss)")) BuildLevel30_RiverSerpentBoss();
            if (GUILayout.Button("🌑 Act 4: Level 40 (Shadow Beast)")) BuildLevel40_ShadowBeastBoss();
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("⚡ Act 5: Level 50 (Final Corruptor Boss)")) BuildLevel50_FinalBossCorruptor();

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(0.85f, 0.45f, 1f);
            if (GUILayout.Button("🌟 BUILD ALL 50 CAMPAIGN LEVELS & REGISTER (1-Click)", GUILayout.Height(36)))
            {
                BuildAllCampaignLevels();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawValidationSection()
        {
            EditorGUILayout.LabelField("3. Diagnostic Scene Validation", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.HelpBox(_lastValidationSummary, _validationMessageType);

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(1f, 0.75f, 0.2f);
            if (GUILayout.Button("🔍 VALIDATE CURRENT GAME", GUILayout.Height(32)))
            {
                RunSceneValidation();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawAndroidBuildSection()
        {
            EditorGUILayout.LabelField("4. Android APK Builder", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            apkFileName = EditorGUILayout.TextField("APK Output Name", apkFileName);
            openFolderAfterBuild = EditorGUILayout.Toggle("Open Folder on Finish", openFolderAfterBuild);
            autoConnectProfiler = EditorGUILayout.Toggle("Profiler / Development", autoConnectProfiler);

            string currentSceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(currentSceneName)) currentSceneName = "Untitled (Unsaved)";
            EditorGUILayout.LabelField("Active Build Scene:", currentSceneName, EditorStyles.miniBoldLabel);

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.25f, 0.65f, 1f);
            if (GUILayout.Button("📦 Build Android APK", GUILayout.Height(36)))
            {
                BuildAndroidAPK();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        #region Common Setup Helpers & URP Configuration
        public static void EnsureURPRenderPipelineAsset()
        {
            string settingsDir = "Assets/Settings";
            if (!AssetDatabase.IsValidFolder(settingsDir))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            string rendererPath = $"{settingsDir}/UniversalRendererData.asset";
            string urpAssetPath = $"{settingsDir}/UniversalRenderPipelineAsset.asset";

            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            UniversalRenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpAssetPath);
            if (urpAsset == null)
            {
                urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urpAsset, urpAssetPath);
            }

            bool changed = false;
            if (GraphicsSettings.defaultRenderPipeline != urpAsset)
            {
                GraphicsSettings.defaultRenderPipeline = urpAsset;
                changed = true;
            }

            if (QualitySettings.renderPipeline != urpAsset)
            {
                QualitySettings.renderPipeline = urpAsset;
                changed = true;
            }

            if (changed)
            {
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    int cur = QualitySettings.GetQualityLevel();
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = urpAsset;
                    QualitySettings.SetQualityLevel(cur, false);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("[AutoGameBuilder] URP UniversalRenderPipelineAsset verified and assigned to GraphicsSettings & QualitySettings!");
            }
        }

        public static void FixAllProjectMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("URP/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            bool anyModified = false;
            string[] matFolders = { "Assets/Materials", "Assets/Art/Materials" };
            foreach (var folder in matFolders)
            {
                if (!Directory.Exists(folder)) continue;
                string[] files = Directory.GetFiles(folder, "*.mat", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    string assetPath = file.Replace("\\", "/");
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
                    if (mat != null)
                    {
                        bool matChanged = false;
                        if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader" || mat.shader != litShader)
                        {
                            mat.shader = litShader;
                            matChanged = true;
                        }

                        Color baseCol = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                        if (baseCol.a == 0 && baseCol.r == 0 && baseCol.g == 0 && baseCol.b == 0)
                        {
                            baseCol = Color.white;
                            matChanged = true;
                        }

                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
                        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseCol);
                        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);

                        if (matChanged)
                        {
                            EditorUtility.SetDirty(mat);
                            anyModified = true;
                        }
                    }
                }
            }

            if (anyModified)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[AutoGameBuilder] Project materials verified and updated with URP shaders!");
            }
        }

        private void InitializeProjectDirectoriesAndTags()
        {
            EnsureFolderExists("Assets/Scenes");
            EnsureFolderExists("Assets/Prefabs");
            EnsureFolderExists("Assets/Materials");
            EnsureFolderExists("Assets/Settings");

            EnsureURPRenderPipelineAsset();

            EnsureTagExists("Player");
            EnsureTagExists("Enemy");
            EnsureTagExists("Vine");
            EnsureTagExists("Wall");
            EnsureTagExists("Food");
            EnsureTagExists("Coin");
            EnsureTagExists("MainCamera");
        }
        #endregion

        #region Master 1-Click Level 01 Generator
        public void BuildPlayableLevel01()
        {
            if (_isBuilding)
            {
                Debug.LogWarning("[AutoGameBuilder] A build is already in progress. Ignoring concurrent request.");
                return;
            }

            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Debug.LogWarning("[AutoGameBuilder] Scene generation is disabled during Play Mode or Editor busy states.");
                return;
            }

            try
            {
                _isBuilding = true;

                ShowProgress("Initializing directories, tags & URP...", 0.05f);
                InitializeProjectDirectoriesAndTags();

                ShowProgress("Synthesizing Audio, UI & Particle VFX...", 0.15f);
                ProceduralAudioSynthesizer.SynthesizeAllAudioClips();
                ProceduralTextureFactory.GenerateAllUISprites();
                ProceduralVFXFactory.GenerateAllVFXPrefabs();

                ShowProgress("Synthesizing 3D Meshes and Character Prefabs...", 0.25f);
                AssetMeshFactory.GenerateAll3DAssetsAndPrefabs();

                Material matGround = GetOrCreateMaterial("Mat_Jungle_Ground", new Color(0.22f, 0.45f, 0.2f));
                Material matWood = GetOrCreateMaterial("Mat_Wood_Platform", new Color(0.48f, 0.32f, 0.18f));
                Material matFire = GetOrCreateMaterial("Mat_Fire_Hazard", new Color(1f, 0.25f, 0.05f), true, new Color(1f, 0.2f, 0f) * 3.5f);
                Material matMushroom = GetOrCreateMaterial("Mat_Toxic_Mushroom", new Color(0.55f, 0.1f, 0.75f), true, new Color(0.4f, 0.9f, 0.1f) * 2.0f);
                Material matRuneInactive = GetOrCreateMaterial("Mat_Rune_Inactive", new Color(0.35f, 0.38f, 0.42f));
                Material matRuneActive = GetOrCreateMaterial("Mat_Rune_Active", new Color(0f, 0.85f, 1f), true, new Color(0f, 0.8f, 1f) * 3.5f);
                Material matCheckpoint = GetOrCreateMaterial("Mat_Checkpoint_Active", new Color(0.1f, 0.95f, 0.35f), true, new Color(0.1f, 0.9f, 0.3f) * 3f);
                Material matGateway = GetOrCreateMaterial("Mat_Gateway_Portal", new Color(1f, 0.85f, 0.2f), true, new Color(1f, 0.8f, 0.2f) * 3.5f);
                Material matProjectile = GetOrCreateMaterial("Mat_Magic_Projectile", new Color(0f, 0.9f, 1f), true, new Color(0.1f, 0.85f, 1f) * 4f);

                GameObject projectilePrefab = CreateMagicProjectilePrefab(matProjectile);

                ShowProgress("Creating Level 01 Scene...", 0.35f);
                Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                GameObject envRoot = new GameObject("[--- 01_ENVIRONMENT ---]");
                GameObject gameplayRoot = new GameObject("[--- 02_GAMEPLAY ---]");
                GameObject managersRoot = new GameObject("[--- 03_MANAGERS ---]");
                GameObject uiRoot = new GameObject("[--- 04_UI_CANVAS ---]");

                // 1. Environment Platforms (Z: 0 to 110)
                ShowProgress("Building Jungle Path Geometry...", 0.45f);
                CreateGroundTile("Ground_Start_Zone", new Vector3(0, 0, 7), new Vector3(10, 1, 16), matGround, envRoot.transform);
                CreateGroundTile("Ground_Path_01", new Vector3(0, 0, 20), new Vector3(7, 1, 10), matGround, envRoot.transform);
                CreateGroundTile("Ground_Enemy_Arena", new Vector3(0, 0, 30), new Vector3(12, 1, 10), matGround, envRoot.transform);
                CreateGroundTile("Platform_Jump_01", new Vector3(-1.5f, 0.3f, 38f), new Vector3(4, 1, 4), matWood, envRoot.transform);
                CreateGroundTile("Platform_Jump_02", new Vector3(1.5f, 0.9f, 44f), new Vector3(4, 1, 4), matWood, envRoot.transform);
                CreateGroundTile("Platform_Vine_Landing", new Vector3(0, 1.5f, 53f), new Vector3(9, 1, 10), matGround, envRoot.transform);

                // Climbable Vine
                SpawnPrefabOrPrimitive("Assets/Art/Props/Prop_ClimbableVine.prefab", "Climbable_Jungle_Vine", new Vector3(0, 1.5f, 53f), envRoot.transform);

                CreateGroundTile("Ground_Hazard_Clearing", new Vector3(0, 1.5f, 65f), new Vector3(10, 1, 14), matGround, envRoot.transform);
                CreateGroundTile("Ground_Puzzle_Courtyard", new Vector3(0, 1.5f, 79f), new Vector3(14, 1, 14), matGround, envRoot.transform);
                CreateGroundTile("Ground_Checkpoint2_Arena", new Vector3(0, 1.5f, 93f), new Vector3(12, 1, 14), matGround, envRoot.transform);
                CreateGroundTile("Ground_Level_Complete_Exit", new Vector3(0, 1.5f, 105f), new Vector3(8, 1, 10), matGround, envRoot.transform);

                // 1b. Environment Foliage, Trees, and Rocks
                ShowProgress("Decorating Jungle Trees, Plants & Boulders...", 0.52f);
                SpawnFoliageDeco("Assets/Art/Environment/Trees/Tree_JungleCanopy.prefab", new Vector3(-6f, 0, 8f), envRoot.transform);
                SpawnFoliageDeco("Assets/Art/Environment/Trees/Tree_CoconutPalm.prefab", new Vector3(6f, 0, 15f), envRoot.transform);
                SpawnFoliageDeco("Assets/Art/Environment/Trees/Tree_JungleCanopy.prefab", new Vector3(-7f, 0, 30f), envRoot.transform);
                SpawnFoliageDeco("Assets/Art/Environment/Trees/Tree_CoconutPalm.prefab", new Vector3(7f, 0, 32f), envRoot.transform);
                SpawnFoliageDeco("Assets/Art/Environment/Trees/Tree_JungleCanopy.prefab", new Vector3(-8f, 1.5f, 79f), envRoot.transform);
                SpawnFoliageDeco("Assets/Art/Environment/Trees/Tree_CoconutPalm.prefab", new Vector3(8f, 1.5f, 82f), envRoot.transform);

                SpawnFoliageDeco("Assets/Art/Environment/Plants/Plant_JungleFern.prefab", new Vector3(-3f, 0.5f, 6f), envRoot.transform);
                SpawnFoliageDeco("Assets/Art/Environment/Plants/Plant_TropicalBush.prefab", new Vector3(3.5f, 0.5f, 10f), envRoot.transform);
                SpawnFoliageDeco("Assets/Art/Environment/Plants/Plant_GlowingMushroom.prefab", new Vector3(-2.5f, 0.5f, 22f), envRoot.transform);
                SpawnFoliageDeco("Assets/Art/Environment/Plants/Plant_HibiscusFlower.prefab", new Vector3(3f, 0.5f, 25f), envRoot.transform);
                SpawnFoliageDeco("Assets/Art/Environment/Rocks/Rock_MossyBoulder.prefab", new Vector3(-4.5f, 0, 18f), envRoot.transform);
                SpawnFoliageDeco("Assets/Art/Environment/Rocks/Rock_MossyBoulder.prefab", new Vector3(4.5f, 0, 28f), envRoot.transform);

                // Hollow Log Prop
                SpawnFoliageDeco("Assets/Art/Props/Prop_HollowFallenLog.prefab", new Vector3(3.5f, 0.5f, 16f), envRoot.transform);

                // 2. Real 3D Player Character
                ShowProgress("Setting Up 3D Player Character...", 0.60f);
                GameObject player = CreatePlayerInstance(new Vector3(0, 1.0f, 0), projectilePrefab, gameplayRoot.transform);

                // 3. Camera
                ShowProgress("Configuring Third Person Camera...", 0.70f);
                CreateMainCameraInstance(player.transform);

                // 4. Lighting
                CreateSunLightInstance(envRoot.transform);

                // 5. Collectibles & Hazards
                ShowProgress("Placing Collectibles & Hazards...", 0.78f);
                Spawn3DCollectible("Assets/Art/Props/Prop_GoldenBanana.prefab", "Banana_01", new Vector3(-1.2f, 1.2f, 8f), gameplayRoot.transform);
                Spawn3DCollectible("Assets/Art/Props/Prop_GoldenBanana.prefab", "Banana_02", new Vector3(0f, 1.2f, 12f), gameplayRoot.transform);
                Spawn3DCollectible("Assets/Art/Props/Prop_GoldenBanana.prefab", "Banana_03", new Vector3(1.2f, 1.2f, 16f), gameplayRoot.transform);
                Spawn3DCollectible("Assets/Art/Props/Prop_GoldenBanana.prefab", "Banana_04", new Vector3(0f, 2.7f, 53f), gameplayRoot.transform);

                Spawn3DCollectible("Assets/Art/Props/Prop_AncientCoin.prefab", "Coin_01", new Vector3(-1.5f, 1.2f, 10f), gameplayRoot.transform);
                Spawn3DCollectible("Assets/Art/Props/Prop_AncientCoin.prefab", "Coin_02", new Vector3(1.5f, 1.2f, 10f), gameplayRoot.transform);
                Spawn3DCollectible("Assets/Art/Props/Prop_AncientCoin.prefab", "Coin_03", new Vector3(-1.5f, 1.6f, 38f), gameplayRoot.transform);
                Spawn3DCollectible("Assets/Art/Props/Prop_AncientCoin.prefab", "Coin_04", new Vector3(1.5f, 2.2f, 44f), gameplayRoot.transform);
                Spawn3DCollectible("Assets/Art/Props/Prop_AncientCoin.prefab", "Coin_05", new Vector3(0f, 2.7f, 93f), gameplayRoot.transform);

                // Water Extinguisher Buff
                GameObject extObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                extObj.name = "Water_Extinguisher_Buff";
                extObj.transform.position = new Vector3(-2.2f, 2.3f, 60f);
                extObj.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
                extObj.GetComponent<Collider>().isTrigger = true;
                extObj.AddComponent<Extinguisher>();
                extObj.transform.SetParent(gameplayRoot.transform);

                // Fire Hazard Zone + Flame VFX
                GameObject fireObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fireObj.name = "Fire_Hazard_Zone";
                fireObj.transform.position = new Vector3(0f, 2.2f, 63f);
                fireObj.transform.localScale = new Vector3(4f, 0.8f, 2f);
                fireObj.GetComponent<Renderer>().sharedMaterial = matFire;
                fireObj.GetComponent<Collider>().isTrigger = true;
                fireObj.AddComponent<FireHazard>();
                fireObj.transform.SetParent(gameplayRoot.transform);
                AttachVFX("Assets/Art/VFX/VFX_FireHazard_Flames.prefab", fireObj.transform);

                // Toxic Mushroom Hazard + Spore VFX
                GameObject mushObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mushObj.name = "Toxic_Mushroom_Hazard";
                mushObj.transform.position = new Vector3(2.5f, 2.3f, 68f);
                mushObj.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                mushObj.GetComponent<Renderer>().sharedMaterial = matMushroom;
                mushObj.GetComponent<Collider>().isTrigger = true;
                mushObj.AddComponent<ToxicMushroom>();
                mushObj.transform.SetParent(gameplayRoot.transform);
                AttachVFX("Assets/Art/VFX/VFX_Poison_SporeCloud.prefab", mushObj.transform);

                // 6. Ancient Ruins & 3-Rune Door Puzzle
                ShowProgress("Assembling 3-Rune Door Puzzle...", 0.85f);
                SpawnFoliageDeco("Assets/Art/Environment/Ruins/Ruins_AncientArch.prefab", new Vector3(0, 1.5f, 74f), gameplayRoot.transform);

                GameObject doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Ruins/Ruins_HeavyStoneDoor.prefab");
                GameObject doorObj = (doorPrefab != null) ? (GameObject)PrefabUtility.InstantiatePrefab(doorPrefab) : new GameObject("Ancient_Stone_Door");
                doorObj.name = "Ancient_Stone_Door";
                doorObj.transform.position = new Vector3(0f, 1.5f, 82f);
                doorObj.transform.SetParent(gameplayRoot.transform);
                AncientDoor ancientDoor = doorObj.GetComponent<AncientDoor>() ?? doorObj.AddComponent<AncientDoor>();

                RuneSwitch rune1 = Create3DRunePedestal("Rune_Switch_Left", new Vector3(-3.5f, 1.5f, 76f), gameplayRoot.transform);
                RuneSwitch rune2 = Create3DRunePedestal("Rune_Switch_Center", new Vector3(0f, 1.5f, 78f), gameplayRoot.transform);
                RuneSwitch rune3 = Create3DRunePedestal("Rune_Switch_Right", new Vector3(3.5f, 1.5f, 76f), gameplayRoot.transform);

                SerializedObject doorSO = new SerializedObject(ancientDoor);
                SerializedProperty reqSwitchesProp = doorSO.FindProperty("requiredSwitches");
                reqSwitchesProp.ClearArray();
                reqSwitchesProp.InsertArrayElementAtIndex(0);
                reqSwitchesProp.GetArrayElementAtIndex(0).objectReferenceValue = rune1;
                reqSwitchesProp.InsertArrayElementAtIndex(1);
                reqSwitchesProp.GetArrayElementAtIndex(1).objectReferenceValue = rune2;
                reqSwitchesProp.InsertArrayElementAtIndex(2);
                reqSwitchesProp.GetArrayElementAtIndex(2).objectReferenceValue = rune3;
                doorSO.ApplyModifiedProperties();

                // 7. Checkpoints & Relics
                CreateCheckpointWithBeam("Checkpoint_01_Start", new Vector3(0, 0.6f, 1f), matCheckpoint, gameplayRoot.transform);
                CreateCheckpointWithBeam("Checkpoint_02_PostDoor", new Vector3(0, 2.1f, 87f), matCheckpoint, gameplayRoot.transform);

                Spawn3DCollectible("Assets/Art/Props/Prop_BreakableRelic.prefab", "Breakable_Celestial_Relic", new Vector3(-2f, 1.5f, 95f), gameplayRoot.transform);

                // 8. Real 3D Enemies & Wildlife
                ShowProgress("Spawning 3D Enemies & Wildlife...", 0.88f);
                Create3DEnemyInstance("Jungle_Predator_01", new Vector3(0f, 0.5f, 30f), gameplayRoot.transform);
                Create3DEnemyInstance("Jungle_Guard_02", new Vector3(2.5f, 2.0f, 96f), gameplayRoot.transform);

                SpawnWildlifeDeco("Assets/Art/Wildlife/Wildlife_Deer.prefab", new Vector3(-3.5f, 0.5f, 14f), gameplayRoot.transform);
                SpawnWildlifeDeco("Assets/Art/Wildlife/Wildlife_Parrot.prefab", new Vector3(3.0f, 2.5f, 20f), gameplayRoot.transform);
                SpawnWildlifeDeco("Assets/Art/Wildlife/Wildlife_TreeFrog.prefab", new Vector3(2.0f, 0.5f, 26f), gameplayRoot.transform);
                SpawnWildlifeDeco("Assets/Art/Wildlife/Wildlife_Butterfly.prefab", new Vector3(0f, 1.8f, 35f), gameplayRoot.transform);
                SpawnWildlifeDeco("Assets/Art/Wildlife/Wildlife_Monkey.prefab", new Vector3(-2.5f, 2.0f, 53f), gameplayRoot.transform);

                // 9. Exit Gateway with Vortex VFX
                CreateExitPortalInstance("Level_01_Complete_Gateway", new Vector3(0f, 3.8f, 108f), matGateway, gameplayRoot.transform);

                // 10. Managers Root (Configured with Audio & System References)
                CreateManagersRoot(managersRoot, player.transform);

                // 11. UI Canvas
                CreateUICanvas(uiRoot.transform, player);

                // 12. Bake Navigation Mesh for Enemies
                ShowProgress("Baking Navigation Mesh...", 0.93f);
                try
                {
#pragma warning disable CS0618
                    UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
#pragma warning restore CS0618
                    Debug.Log("[AutoGameBuilder] NavMesh baked successfully for Level 01.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AutoGameBuilder] NavMesh baking notice: {ex.Message}");
                }

                // 13. Apply Cinematic HD Environment Pass & Save Scene
                ShowProgress("Applying Cinematic HD Environment Pass...", 0.94f);
                HDSceneDirectInjector.ExecuteFullDirectInjectionAndValidation();

                EditorSceneManager.SaveScene(newScene, SCENE_PATH_L01);
                RegisterSceneInBuildSettings(SCENE_PATH_L01);

                RunSceneValidation();
                Debug.Log($"[AutoGameBuilder] Cinematic HD 3D Scene saved to '{SCENE_PATH_L01}'!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoGameBuilder] Exception during Level 01 build: {ex}");
            }
            finally
            {
                _isBuilding = false;
                ClearProgress();
            }
        }
        #endregion

        #region Act 1–5 Boss Scene Generators
        public void BuildLevel10_AlphaJaguarBoss()
        {
            if (_isBuilding) return;
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            try
            {
                _isBuilding = true;
                string path = "Assets/Scenes/Level10_AlphaJaguarBoss.unity";
                InitializeProjectDirectoriesAndTags();

                Material matGround = GetOrCreateMaterial("Mat_Jungle_Ground", new Color(0.22f, 0.45f, 0.2f));
                Material matWood = GetOrCreateMaterial("Mat_Wood_Platform", new Color(0.48f, 0.32f, 0.18f));
                Material matGateway = GetOrCreateMaterial("Mat_Gateway_Portal", new Color(1f, 0.85f, 0.2f), true, new Color(1f, 0.8f, 0.2f) * 3.5f);
                Material matProjectile = GetOrCreateMaterial("Mat_Magic_Projectile", new Color(0f, 0.9f, 1f), true, new Color(0.1f, 0.85f, 1f) * 4f);
                GameObject projectilePrefab = CreateMagicProjectilePrefab(matProjectile);

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject env = new GameObject("[--- 01_ENVIRONMENT ---]");
                GameObject gameplay = new GameObject("[--- 02_GAMEPLAY ---]");
                GameObject managers = new GameObject("[--- 03_MANAGERS ---]");
                GameObject ui = new GameObject("[--- 04_UI_CANVAS ---]");

                CreateGroundTile("Arena_Floor", new Vector3(0, 0, 15), new Vector3(24, 1, 24), matGround, env.transform);
                CreateWallBoundary("Wall_North", new Vector3(0, 2.5f, 27), new Vector3(24, 5, 1), matWood, env.transform);
                CreateWallBoundary("Wall_South", new Vector3(0, 2.5f, 3), new Vector3(24, 5, 1), matWood, env.transform);
                CreateWallBoundary("Wall_East", new Vector3(12, 2.5f, 15), new Vector3(1, 5, 24), matWood, env.transform);
                CreateWallBoundary("Wall_West", new Vector3(-12, 2.5f, 15), new Vector3(1, 5, 24), matWood, env.transform);

                GameObject player = CreatePlayerInstance(new Vector3(0, 1f, 6), projectilePrefab, gameplay.transform);
                CreateMainCameraInstance(player.transform);
                CreateSunLightInstance(env.transform);

                // Real 3D Alpha Jaguar Boss
                SpawnBossPrefab("Assets/Art/Bosses/Boss_AlphaJaguar.prefab", "Boss_AlphaJaguar", new Vector3(0, 0.5f, 20), gameplay.transform);

                CreateExitPortalInstance("Act2_Unlock_Portal", new Vector3(0, 2.5f, 25f), matGateway, gameplay.transform);
                CreateManagersRoot(managers, player.transform);
                CreateUICanvas(ui.transform, player);

                EditorSceneManager.SaveScene(scene, path);
                RegisterSceneInBuildSettings(path);
                Debug.Log($"[AutoGameBuilder] Act 1 Boss Scene saved to '{path}'!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoGameBuilder] Exception building Level 10: {ex}");
            }
            finally
            {
                _isBuilding = false;
                ClearProgress();
            }
        }

        public void BuildLevel20_StoneGolemBoss()
        {
            if (_isBuilding) return;
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            try
            {
                _isBuilding = true;
                string path = "Assets/Scenes/Level20_StoneGolemBoss.unity";
                InitializeProjectDirectoriesAndTags();

                Material matGround = GetOrCreateMaterial("Mat_Ancient_Door", new Color(0.28f, 0.3f, 0.34f));
                Material matGateway = GetOrCreateMaterial("Mat_Gateway_Portal", new Color(1f, 0.85f, 0.2f), true, new Color(1f, 0.8f, 0.2f) * 3.5f);
                Material matProjectile = GetOrCreateMaterial("Mat_Magic_Projectile", new Color(0f, 0.9f, 1f), true, new Color(0.1f, 0.85f, 1f) * 4f);
                GameObject projectilePrefab = CreateMagicProjectilePrefab(matProjectile);

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject env = new GameObject("[--- 01_ENVIRONMENT ---]");
                GameObject gameplay = new GameObject("[--- 02_GAMEPLAY ---]");
                GameObject managers = new GameObject("[--- 03_MANAGERS ---]");
                GameObject ui = new GameObject("[--- 04_UI_CANVAS ---]");

                CreateGroundTile("Stone_Ruins_Arena", new Vector3(0, 0, 15), new Vector3(26, 1, 26), matGround, env.transform);

                GameObject player = CreatePlayerInstance(new Vector3(0, 1f, 5), projectilePrefab, gameplay.transform);
                CreateMainCameraInstance(player.transform);
                CreateSunLightInstance(env.transform);

                // Real 3D Stone Golem Boss
                SpawnBossPrefab("Assets/Art/Bosses/Boss_StoneGolem.prefab", "Boss_StoneGolem", new Vector3(0, 0.5f, 20), gameplay.transform);

                CreateExitPortalInstance("Act3_Unlock_Portal", new Vector3(0, 2.5f, 25f), matGateway, gameplay.transform);
                CreateManagersRoot(managers, player.transform);
                CreateUICanvas(ui.transform, player);

                EditorSceneManager.SaveScene(scene, path);
                RegisterSceneInBuildSettings(path);
                Debug.Log($"[AutoGameBuilder] Act 2 Boss Scene saved to '{path}'!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoGameBuilder] Exception building Level 20: {ex}");
            }
            finally
            {
                _isBuilding = false;
                ClearProgress();
            }
        }

        public void BuildLevel30_RiverSerpentBoss()
        {
            if (_isBuilding) return;
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            try
            {
                _isBuilding = true;
                string path = "Assets/Scenes/Level30_RiverSerpentBoss.unity";
                InitializeProjectDirectoriesAndTags();

                Material matGround = GetOrCreateMaterial("Mat_Jungle_Ground", new Color(0.22f, 0.45f, 0.2f));
                Material matGateway = GetOrCreateMaterial("Mat_Gateway_Portal", new Color(1f, 0.85f, 0.2f), true, new Color(1f, 0.8f, 0.2f) * 3.5f);
                Material matProjectile = GetOrCreateMaterial("Mat_Magic_Projectile", new Color(0f, 0.9f, 1f), true, new Color(0.1f, 0.85f, 1f) * 4f);
                GameObject projectilePrefab = CreateMagicProjectilePrefab(matProjectile);

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject env = new GameObject("[--- 01_ENVIRONMENT ---]");
                GameObject gameplay = new GameObject("[--- 02_GAMEPLAY ---]");
                GameObject managers = new GameObject("[--- 03_MANAGERS ---]");
                GameObject ui = new GameObject("[--- 04_UI_CANVAS ---]");

                CreateGroundTile("River_Island_Center", new Vector3(0, 0, 15), new Vector3(18, 1, 18), matGround, env.transform);

                GameObject player = CreatePlayerInstance(new Vector3(0, 1f, 8), projectilePrefab, gameplay.transform);
                CreateMainCameraInstance(player.transform);
                CreateSunLightInstance(env.transform);

                // Real 3D River Serpent Boss
                SpawnBossPrefab("Assets/Art/Bosses/Boss_RiverSerpent.prefab", "Boss_RiverSerpent", new Vector3(0, 0.5f, 22), gameplay.transform);

                CreateExitPortalInstance("Act4_Unlock_Portal", new Vector3(0, 2.5f, 24f), matGateway, gameplay.transform);
                CreateManagersRoot(managers, player.transform);
                CreateUICanvas(ui.transform, player);

                EditorSceneManager.SaveScene(scene, path);
                RegisterSceneInBuildSettings(path);
                Debug.Log($"[AutoGameBuilder] Act 3 Boss Scene saved to '{path}'!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoGameBuilder] Exception building Level 30: {ex}");
            }
            finally
            {
                _isBuilding = false;
                ClearProgress();
            }
        }

        public void BuildLevel40_ShadowBeastBoss()
        {
            if (_isBuilding) return;
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            try
            {
                _isBuilding = true;
                string path = "Assets/Scenes/Level40_ShadowBeastBoss.unity";
                InitializeProjectDirectoriesAndTags();

                Material matGround = GetOrCreateMaterial("Mat_Ancient_Door", new Color(0.15f, 0.12f, 0.2f));
                Material matGateway = GetOrCreateMaterial("Mat_Gateway_Portal", new Color(1f, 0.85f, 0.2f), true, new Color(1f, 0.8f, 0.2f) * 3.5f);
                Material matProjectile = GetOrCreateMaterial("Mat_Magic_Projectile", new Color(0f, 0.9f, 1f), true, new Color(0.1f, 0.85f, 1f) * 4f);
                GameObject projectilePrefab = CreateMagicProjectilePrefab(matProjectile);

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject env = new GameObject("[--- 01_ENVIRONMENT ---]");
                GameObject gameplay = new GameObject("[--- 02_GAMEPLAY ---]");
                GameObject managers = new GameObject("[--- 03_MANAGERS ---]");
                GameObject ui = new GameObject("[--- 04_UI_CANVAS ---]");

                CreateGroundTile("Dark_Forest_Arena", new Vector3(0, 0, 15), new Vector3(26, 1, 26), matGround, env.transform);

                GameObject player = CreatePlayerInstance(new Vector3(0, 1.2f, 5), projectilePrefab, gameplay.transform);
                CreateMainCameraInstance(player.transform);
                CreateSunLightInstance(env.transform);

                // Real 3D Shadow Beast Boss
                SpawnBossPrefab("Assets/Art/Bosses/Boss_ShadowBeast.prefab", "Boss_ShadowBeast", new Vector3(0, 0.5f, 22), gameplay.transform);

                CreateExitPortalInstance("Act5_Unlock_Portal", new Vector3(0, 1.5f, 27f), matGateway, gameplay.transform);
                CreateManagersRoot(managers, player.transform);
                CreateUICanvas(ui.transform, player);

                EditorSceneManager.SaveScene(scene, path);
                RegisterSceneInBuildSettings(path);
                Debug.Log($"[AutoGameBuilder] Act 4 Boss Scene saved to '{path}'!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoGameBuilder] Exception building Level 40: {ex}");
            }
            finally
            {
                _isBuilding = false;
                ClearProgress();
            }
        }

        public void BuildLevel50_FinalBossCorruptor()
        {
            if (_isBuilding) return;
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            try
            {
                _isBuilding = true;
                string path = "Assets/Scenes/Level50_FinalBossCorruptor.unity";
                InitializeProjectDirectoriesAndTags();

                Material matGround = GetOrCreateMaterial("Mat_Breakable_Relic", new Color(0.85f, 0.65f, 0.2f));
                Material matGateway = GetOrCreateMaterial("Mat_Gateway_Portal", new Color(1f, 0.85f, 0.2f), true, new Color(1f, 0.8f, 0.2f) * 3.5f);
                Material matProjectile = GetOrCreateMaterial("Mat_Magic_Projectile", new Color(0f, 0.9f, 1f), true, new Color(0.1f, 0.85f, 1f) * 4f);
                GameObject projectilePrefab = CreateMagicProjectilePrefab(matProjectile);

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject env = new GameObject("[--- 01_ENVIRONMENT ---]");
                GameObject gameplay = new GameObject("[--- 02_GAMEPLAY ---]");
                GameObject managers = new GameObject("[--- 03_MANAGERS ---]");
                GameObject ui = new GameObject("[--- 04_UI_CANVAS ---]");

                CreateGroundTile("Celestial_Sky_Arena", new Vector3(0, 0, 18), new Vector3(32, 1, 32), matGround, env.transform);

                GameObject player = CreatePlayerInstance(new Vector3(0, 1.2f, 4), projectilePrefab, gameplay.transform);
                CreateMainCameraInstance(player.transform);
                CreateSunLightInstance(env.transform);

                // Real 3D Final Boss Corruptor
                SpawnBossPrefab("Assets/Art/Bosses/Boss_FinalCorruptor.prefab", "Boss_FinalCorruptor", new Vector3(0, 1.0f, 22), gameplay.transform);

                CreateExitPortalInstance("Grand_Victory_Portal", new Vector3(0, 2f, 30f), matGateway, gameplay.transform);
                CreateManagersRoot(managers, player.transform);
                CreateUICanvas(ui.transform, player);

                EditorSceneManager.SaveScene(scene, path);
                RegisterSceneInBuildSettings(path);
                Debug.Log($"[AutoGameBuilder] Act 5 Final Boss Scene saved to '{path}'!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoGameBuilder] Exception building Level 50: {ex}");
            }
            finally
            {
                _isBuilding = false;
                ClearProgress();
            }
        }
        #endregion

        #region Master 50-Level Campaign Builder
        public void BuildAllCampaignLevels()
        {
            if (_isBuilding) return;
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            try
            {
                _isBuilding = true;

                ShowProgress("Building Level 01 with 3D Assets...", 0.1f);
                BuildPlayableLevel01();

                ShowProgress("Building Act 1 Boss (Level 10)...", 0.25f);
                BuildLevel10_AlphaJaguarBoss();

                ShowProgress("Building Act 2 Boss (Level 20)...", 0.45f);
                BuildLevel20_StoneGolemBoss();

                ShowProgress("Building Act 3 Boss (Level 30)...", 0.65f);
                BuildLevel30_RiverSerpentBoss();

                ShowProgress("Building Act 4 Boss (Level 40)...", 0.80f);
                BuildLevel40_ShadowBeastBoss();

                ShowProgress("Building Act 5 Final Boss (Level 50)...", 0.95f);
                BuildLevel50_FinalBossCorruptor();

                Debug.Log("[AutoGameBuilder] Master 50-Level Campaign generation completed!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoGameBuilder] Exception during full campaign build: {ex}");
            }
            finally
            {
                _isBuilding = false;
                ClearProgress();
            }
        }
        #endregion

        #region Helper Creators
        private GameObject CreatePlayerInstance(Vector3 pos, GameObject projectilePrefab, Transform parent)
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = pos;
            player.transform.rotation = Quaternion.identity;

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0, 0.9f, 0);

            player.AddComponent<MonkeyPlayerController>();
            player.AddComponent<PlayerHealth>();

            // Evolution Skin Manager & 3D Skin Prefabs
            GameObject modelHolder = new GameObject("ModelHolder");
            modelHolder.transform.SetParent(player.transform, false);

            GameObject monkeyBasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Monkey_Base.prefab");
            if (monkeyBasePrefab != null)
            {
                GameObject baseInstance = (GameObject)PrefabUtility.InstantiatePrefab(monkeyBasePrefab, modelHolder.transform);
                baseInstance.transform.localPosition = Vector3.zero;
            }

            EvolutionSkinManager skinMgr = player.AddComponent<EvolutionSkinManager>();
            SerializedObject skinSO = new SerializedObject(skinMgr);
            skinSO.FindProperty("modelHolder").objectReferenceValue = modelHolder.transform;

            SerializedProperty skinsProp = skinSO.FindProperty("skins");
            if (skinsProp != null && skinsProp.arraySize >= 4)
            {
                skinsProp.GetArrayElementAtIndex(0).FindPropertyRelative("meshPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Monkey_Base.prefab");
                skinsProp.GetArrayElementAtIndex(1).FindPropertyRelative("meshPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Monkey_Guardian.prefab");
                skinsProp.GetArrayElementAtIndex(2).FindPropertyRelative("meshPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Monkey_PrimalTitan.prefab");
                skinsProp.GetArrayElementAtIndex(3).FindPropertyRelative("meshPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Monkey_DivineGuardian.prefab");
            }
            skinSO.ApplyModifiedProperties();

            GameObject firePoint = new GameObject("FirePoint");
            firePoint.transform.SetParent(player.transform);
            firePoint.transform.localPosition = new Vector3(0, 1.1f, 0.85f);
            firePoint.transform.localRotation = Quaternion.identity;

            GuardianCombat combat = player.AddComponent<GuardianCombat>();
            SerializedObject combatSO = new SerializedObject(combat);
            combatSO.FindProperty("magicProjectilePrefab").objectReferenceValue = projectilePrefab;
            combatSO.FindProperty("firePoint").objectReferenceValue = firePoint.transform;
            combatSO.FindProperty("isGuardianForm").boolValue = true;
            combatSO.ApplyModifiedProperties();

            player.AddComponent<MonkeySetupBinder>();
            player.AddComponent<VineClimb>();
            player.AddComponent<LightAura>();

            player.transform.SetParent(parent);
            return player;
        }

        private void CreateMainCameraInstance(Transform target)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera mainCam = camObj.AddComponent<Camera>();
            mainCam.nearClipPlane = 0.2f;
            camObj.AddComponent<AudioListener>();

            ThirdPersonCamera tpCam = camObj.AddComponent<ThirdPersonCamera>();
            tpCam.Target = target;
            tpCam.SnapBehindTarget();
        }

        private void CreateSunLightInstance(Transform parent)
        {
            GameObject lightObj = new GameObject("Directional Light (Sun)");
            Light sun = lightObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0);
            lightObj.transform.SetParent(parent);
        }

        private void CreateGroundTile(string name, Vector3 position, Vector3 scale, Material mat, Transform parent)
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = name;
            tile.transform.position = position;
            tile.transform.localScale = scale;
            if (mat != null) tile.GetComponent<Renderer>().sharedMaterial = mat;
            tile.transform.SetParent(parent);
        }

        private void CreateWallBoundary(string name, Vector3 position, Vector3 scale, Material mat, Transform parent)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.tag = "Wall";
            wall.transform.position = position;
            wall.transform.localScale = scale;
            if (mat != null) wall.GetComponent<Renderer>().sharedMaterial = mat;
            wall.transform.SetParent(parent);
        }

        private void SpawnFoliageDeco(string prefabPath, Vector3 position, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = position;
                instance.transform.SetParent(parent);
            }
        }

        private void SpawnWildlifeDeco(string prefabPath, Vector3 position, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = position;
                instance.transform.SetParent(parent);
            }
        }

        private void Spawn3DCollectible(string prefabPath, string name, Vector3 position, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = name;
                instance.transform.position = position;
                instance.transform.SetParent(parent);
            }
        }

        private void Create3DEnemyInstance(string name, Vector3 position, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Enemies/Enemy_JunglePredator.prefab");
            GameObject enemy = (prefab != null) ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = name;
            enemy.tag = "Enemy";
            enemy.transform.position = position;
            enemy.transform.SetParent(parent);

            EnemyAI ai = enemy.GetComponent<EnemyAI>() ?? enemy.AddComponent<EnemyAI>();
            if (enemy.GetComponent<EnemyTarget>() == null) enemy.AddComponent<EnemyTarget>();

            GameObject wp1 = new GameObject($"{name}_WP1");
            wp1.transform.position = position + new Vector3(-3f, 0, 0);
            wp1.transform.SetParent(parent);

            GameObject wp2 = new GameObject($"{name}_WP2");
            wp2.transform.position = position + new Vector3(3f, 0, 0);
            wp2.transform.SetParent(parent);

            SerializedObject aiSO = new SerializedObject(ai);
            SerializedProperty waypointsProp = aiSO.FindProperty("patrolWaypoints");
            waypointsProp.ClearArray();
            waypointsProp.InsertArrayElementAtIndex(0);
            waypointsProp.GetArrayElementAtIndex(0).objectReferenceValue = wp1.transform;
            waypointsProp.InsertArrayElementAtIndex(1);
            waypointsProp.GetArrayElementAtIndex(1).objectReferenceValue = wp2.transform;
            aiSO.ApplyModifiedProperties();
        }

        private void SpawnBossPrefab(string prefabPath, string name, Vector3 position, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = name;
                instance.transform.position = position;
                instance.transform.SetParent(parent);
            }
        }

        private void SpawnPrefabOrPrimitive(string prefabPath, string fallbackName, Vector3 position, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = position;
                instance.transform.SetParent(parent);
            }
            else
            {
                GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                obj.name = fallbackName;
                obj.transform.position = position;
                obj.transform.SetParent(parent);
            }
        }

        private RuneSwitch Create3DRunePedestal(string name, Vector3 position, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Ruins/Ruins_RunePedestal.prefab");
            GameObject runeObj = (prefab != null) ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            runeObj.name = name;
            runeObj.transform.position = position;
            runeObj.transform.SetParent(parent);

            RuneSwitch rune = runeObj.GetComponent<RuneSwitch>() ?? runeObj.AddComponent<RuneSwitch>();
            return rune;
        }

        private void CreateCheckpointWithBeam(string name, Vector3 position, Material mat, Transform parent)
        {
            GameObject cp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cp.name = name;
            cp.transform.position = position;
            cp.transform.localScale = new Vector3(1.8f, 0.15f, 1.8f);
            if (mat != null) cp.GetComponent<Renderer>().sharedMaterial = mat;

            Collider col = cp.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            cp.AddComponent<Checkpoint>();
            AttachVFX("Assets/Art/VFX/VFX_Checkpoint_Beam.prefab", cp.transform);
            cp.transform.SetParent(parent);
        }

        private void CreateExitPortalInstance(string name, Vector3 position, Material mat, Transform parent)
        {
            GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            portal.name = name;
            portal.transform.position = position;
            portal.transform.localScale = new Vector3(2.5f, 0.1f, 2.5f);
            portal.transform.rotation = Quaternion.Euler(90f, 0, 0);
            if (mat != null) portal.GetComponent<Renderer>().sharedMaterial = mat;

            Collider col = portal.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            portal.AddComponent<LevelExitPortal>();
            AttachVFX("Assets/Art/VFX/VFX_Portal_Vortex.prefab", portal.transform);
            portal.transform.SetParent(parent);
        }

        private void AttachVFX(string vfxPrefabPath, Transform parent)
        {
            GameObject vfxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(vfxPrefabPath);
            if (vfxPrefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(vfxPrefab);
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = Vector3.zero;
            }
        }

        private GameObject CreateMagicProjectilePrefab(Material mat)
        {
            string prefabPath = $"{PREFAB_DIR}/MagicProjectile.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) return existing;

            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            temp.name = "MagicProjectile";
            temp.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            if (mat != null) temp.GetComponent<Renderer>().sharedMaterial = mat;

            SphereCollider sc = temp.GetComponent<SphereCollider>();
            if (sc != null) sc.isTrigger = true;

            Rigidbody rb = temp.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            temp.AddComponent<MagicProjectile>();
            AttachVFX("Assets/Art/VFX/VFX_Projectile_Trail.prefab", temp.transform);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
            DestroyImmediate(temp);
            return prefab;
        }

        private void CreateManagersRoot(GameObject managersRoot, Transform playerTransform)
        {
            GameManager gm = managersRoot.AddComponent<GameManager>();
            SerializedObject gmSO = new SerializedObject(gm);
            gmSO.FindProperty("playerTransform").objectReferenceValue = playerTransform;
            gmSO.FindProperty("fallDeathYThreshold").floatValue = -10f;
            gmSO.ApplyModifiedProperties();

            AudioManager am = managersRoot.AddComponent<AudioManager>();
            ConfigureAudioManagerClips(am);

            managersRoot.AddComponent<CurrencyManager>();
            managersRoot.AddComponent<MonetizationManager>();
            managersRoot.AddComponent<GameAssetInitializer>();
            managersRoot.AddComponent<LevelProgressionManager>();
            managersRoot.AddComponent<CampaignLevelDirector>();
        }

        private void ConfigureAudioManagerClips(AudioManager am)
        {
            SerializedObject amSO = new SerializedObject(am);

            string audioDir = "Assets/Art/Audio";
            string[] bgmNames = { "BGM_Act1", "BGM_Act2", "BGM_Act3", "BGM_Act4", "BGM_Act5", "BGM_Boss" };
            string[] sfxNames = {
                "SFX_Jump", "SFX_Land", "SFX_Attack", "SFX_HeavyAttack", "SFX_EnergyBlast",
                "SFX_Footstep", "SFX_Coin", "SFX_Banana", "SFX_Hurt", "SFX_Death",
                "SFX_Checkpoint", "SFX_RuneActivate", "SFX_DoorOpen", "SFX_LevelComplete",
                "SFX_EnemyHit", "SFX_BossRoar", "SFX_UIClick", "SFX_WaterSplash",
                "SFX_FireCrackle", "SFX_PoisonBubble"
            };

            SerializedProperty bgmProp = amSO.FindProperty("bgmSounds");
            bgmProp.ClearArray();
            for (int i = 0; i < bgmNames.Length; i++)
            {
                bgmProp.InsertArrayElementAtIndex(i);
                SerializedProperty elem = bgmProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("name").stringValue = bgmNames[i];
                elem.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{audioDir}/{bgmNames[i]}.wav");
                elem.FindPropertyRelative("volume").floatValue = 0.8f;
                elem.FindPropertyRelative("loop").boolValue = true;
            }

            SerializedProperty sfxProp = amSO.FindProperty("sfxSounds");
            sfxProp.ClearArray();
            for (int i = 0; i < sfxNames.Length; i++)
            {
                sfxProp.InsertArrayElementAtIndex(i);
                SerializedProperty elem = sfxProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("name").stringValue = sfxNames[i];
                elem.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{audioDir}/{sfxNames[i]}.wav");
                elem.FindPropertyRelative("volume").floatValue = 1.0f;
            }

            amSO.ApplyModifiedProperties();
        }

        private void CreateUICanvas(Transform parent, GameObject player)
        {
            GameObject canvasObj = new GameObject("UI_Canvas");
            canvasObj.transform.SetParent(parent);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
                esObj.transform.SetParent(parent);
            }

            MobileButtonLinker linker = canvasObj.AddComponent<MobileButtonLinker>();
            SerializedObject linkerSO = new SerializedObject(linker);
            linkerSO.FindProperty("playerObject").objectReferenceValue = player;
            linkerSO.FindProperty("playerCombat").objectReferenceValue = player.GetComponent<GuardianCombat>();
            linkerSO.ApplyModifiedProperties();

            canvasObj.AddComponent<ReviveUIManager>();
            canvasObj.AddComponent<TutorialManager>();
        }

        private Material GetOrCreateMaterial(string name, Color albedoColor, bool isEmission = false, Color emissionColor = default)
        {
            string path = $"{MATERIAL_DIR}/{name}.mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("URP/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.name = name;
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                if (mat.shader != shader && shader != null)
                {
                    mat.shader = shader;
                }
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", albedoColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", albedoColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);

            if (isEmission)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private void EnsureFolderExists(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
                string folderName = Path.GetFileName(folderPath);
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    EnsureFolderExists(parent);
                }
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private void EnsureTagExists(string tag)
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            bool found = false;
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tag)) { found = true; break; }
            }

            if (!found)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                SerializedProperty n = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
                n.stringValue = tag;
                tagManager.ApplyModifiedProperties();
            }
        }

        private void RegisterSceneInBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
            bool exists = false;
            foreach (var s in originalScenes)
            {
                if (s.path.Equals(scenePath, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[originalScenes.Length + 1];
                for (int i = 0; i < originalScenes.Length; i++)
                {
                    newScenes[i] = originalScenes[i];
                }
                newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = newScenes;
            }
        }

        private void ShowProgress(string info, float progress)
        {
            if (!Application.isBatchMode && !Application.isPlaying)
            {
                EditorUtility.DisplayProgressBar("Monkey Adventure Builder", info, progress);
            }
        }

        private void ClearProgress()
        {
            if (!Application.isBatchMode)
            {
                EditorUtility.ClearProgressBar();
            }
        }
        #endregion

        #region Diagnostic Validation Engine
        public void RunSceneValidation()
        {
            int errorCount = 0;
            int warningCount = 0;
            List<string> logs = new List<string>();

            // 1. Validate Player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                errorCount++;
                logs.Add("❌ [ERROR] No GameObject tagged 'Player' found in the active scene!");
            }
            else
            {
                logs.Add("✅ [OK] Player GameObject found with tag 'Player'.");

                if (player.GetComponent<CharacterController>() == null)
                {
                    errorCount++;
                    logs.Add("❌ [ERROR] Player is missing CharacterController!");
                }
                else logs.Add("✅ [OK] Player CharacterController verified.");

                if (player.GetComponent<MonkeyPlayerController>() == null)
                {
                    errorCount++;
                    logs.Add("❌ [ERROR] Player is missing MonkeyPlayerController!");
                }
                else logs.Add("✅ [OK] MonkeyPlayerController verified.");

                if (player.GetComponent<GuardianCombat>() == null)
                {
                    errorCount++;
                    logs.Add("❌ [ERROR] Player is missing GuardianCombat!");
                }
                else logs.Add("✅ [OK] GuardianCombat verified.");

                if (player.GetComponent<PlayerHealth>() == null)
                {
                    errorCount++;
                    logs.Add("❌ [ERROR] Player is missing PlayerHealth!");
                }
                else logs.Add("✅ [OK] PlayerHealth verified.");
            }

            // 2. Validate Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                errorCount++;
                logs.Add("❌ [ERROR] No Main Camera found in scene!");
            }
            else
            {
                logs.Add("✅ [OK] Main Camera verified.");
                if (cam.GetComponent<ThirdPersonCamera>() == null)
                {
                    warningCount++;
                    logs.Add("⚠️ [WARN] Main Camera is missing ThirdPersonCamera script.");
                }
                else logs.Add("✅ [OK] ThirdPersonCamera verified.");
            }

            // 3. Validate Singletons
            if (FindAnyObjectByType<GameManager>() == null)
            {
                errorCount++;
                logs.Add("❌ [ERROR] GameManager is missing from the scene!");
            }
            else logs.Add("✅ [OK] GameManager verified.");

            if (FindAnyObjectByType<AudioManager>() == null)
            {
                warningCount++;
                logs.Add("⚠️ [WARN] AudioManager is missing from the scene.");
            }
            else logs.Add("✅ [OK] AudioManager verified.");

            // Update Validation Summary
            if (errorCount == 0 && warningCount == 0)
            {
                _validationMessageType = MessageType.Info;
                _lastValidationSummary = "✅ [PERFECT] All systems, components, and references passed validation!";
            }
            else if (errorCount == 0)
            {
                _validationMessageType = MessageType.Warning;
                _lastValidationSummary = $"⚠️ [WARNING] Passed with {warningCount} warning(s).";
            }
            else
            {
                _validationMessageType = MessageType.Error;
                _lastValidationSummary = $"❌ [FAILED] Scene has {errorCount} error(s) and {warningCount} warning(s)!";
            }

            Debug.Log($"[AutoGameBuilder Diagnostics]\n{string.Join("\n", logs)}");
        }
        #endregion

        #region Android APK Pipeline
        private void BuildAndroidAPK()
        {
            string buildFolder = "Builds/Android";
            if (!Directory.Exists(buildFolder))
            {
                Directory.CreateDirectory(buildFolder);
            }

            string fullPath = Path.Combine(buildFolder, apkFileName).Replace("\\", "/");

            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.development = autoConnectProfiler;
            EditorUserBuildSettings.connectProfiler = autoConnectProfiler;

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = GetEnabledScenePaths(),
                locationPathName = fullPath,
                target = BuildTarget.Android,
                options = autoConnectProfiler ? (BuildOptions.Development | BuildOptions.ConnectWithProfiler) : BuildOptions.None
            };

            Debug.Log($"[AutoGameBuilder] Initiating Android APK build to '{fullPath}'...");
            var report = BuildPipeline.BuildPlayer(buildOptions);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[AutoGameBuilder] Android Build SUCCEEDED! Total size: {report.summary.totalSize} bytes.");
                if (openFolderAfterBuild)
                {
                    EditorUtility.RevealInFinder(fullPath);
                }
            }
            else
            {
                Debug.LogError($"[AutoGameBuilder] Android Build FAILED with {report.summary.totalErrors} errors!");
            }
        }

        private string[] GetEnabledScenePaths()
        {
            List<string> scenes = new List<string>();
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.enabled && !string.IsNullOrEmpty(s.path))
                {
                    scenes.Add(s.path);
                }
            }

            if (scenes.Count == 0)
            {
                scenes.Add(SCENE_PATH_L01);
            }

            return scenes.ToArray();
        }
        #endregion
    }
}
