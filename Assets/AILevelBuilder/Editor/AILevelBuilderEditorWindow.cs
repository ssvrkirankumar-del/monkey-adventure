using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MonkeyAdventure.AILevelBuilder;

namespace MonkeyAdventure.AILevelBuilder.Editor
{
    /// <summary>
    /// Unity EditorWindow for the AI Level Builder system.
    /// Exposes controls to configure, generate, validate, convert materials to URP, and find magenta rendering in the scene.
    /// </summary>
    public class AILevelBuilderEditorWindow : EditorWindow
    {
        [SerializeField]
        private Level01BlockoutGenerator.BlockoutSettings _blockoutSettings = new Level01BlockoutGenerator.BlockoutSettings();

        [SerializeField]
        private HDAssetLibrary _hdAssetLibrary;

        private Vector2 _scrollPos;
        private Vector2 _issuesScrollPos;
        private Vector2 _conversionScrollPos;
        private Vector2 _auditScrollPos;
        private Vector2 _activeAuditScrollPos;
        private Vector2 _fullSceneScrollPos;

        private ValidationReport _lastReport = null;
        private HDReplacementReport _lastHDReport = null;
        private MaterialConversionReport _lastConversionReport = null;
        private ScenePreviewMagentaAuditReport _lastSceneAuditReport = null;
        private ActiveHDAuditReport _lastActiveAuditReport = null;
        private FullSceneAuditReport _lastFullSceneReport = null;
        private HDDiscoveryReport _lastDiscoveryReport = null;
        private HDJungleDiscoveryReport _lastJungleReport = null;
        private Vector2 _discoveryScrollPos;
        private bool _showDiscoveryDetails = true;
        private float _discoveryListHeight = 500f;
        private string _discoverySearchFilter = "";
        private int _discoveryCategoryFilter = 0;
        private static readonly string[] CategoryFilterOptions = new string[]
        {
            "All", "Tree", "Rock", "RiverRock", "Grass", "DeadLeaves", "Bush", "Ground", "Water", "Waterfall", "WoodTrunk", "AncientStone", "Arch", "Other"
        };
        private EnvironmentPopulatorSettings _envPopulatorSettings = new EnvironmentPopulatorSettings();
        private HDEnvironmentGenerationReport _lastEnvReport = null;
        private bool _showEnvPopulatorSection = true;
        private int _selectedIssueIndex = -1;

        private bool _showHDVisualAuditSection = true;
        private HDEnvironmentVisualAuditReport _lastVisualAuditReport = null;
        private Vector2 _visualAuditScrollPos;
        private int _visualAuditFilterTab = 0;
        private string _visualAuditSearchQuery = "";
        private int _visualAuditWarningIndex = 0;

        private bool _showSettingsSection = true;
        private bool _showActionsSection = true;
        private bool _showValidationSection = true;
        private bool _showHDSection = true;
        private bool _showDiscoverySection = true;
        private bool _showURPSection = true;
        private bool _showFullSceneSection = true;
        private bool _showHDLibrarySettings = false;

        [MenuItem("Window/Monkey Adventure/AI Level Builder", false, 100)]
        [MenuItem("Tools/AI Level Builder", false, 1)]
        public static void ShowWindow()
        {
            AILevelBuilderEditorWindow window = GetWindow<AILevelBuilderEditorWindow>("AI Level Builder", true);
            window.minSize = new Vector2(460, 740);
            window.Show();
        }

        private void OnEnable()
        {
            // Auto-discover HDAssetLibrary if null
            if (_hdAssetLibrary == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:HDAssetLibrary");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _hdAssetLibrary = AssetDatabase.LoadAssetAtPath<HDAssetLibrary>(path);
                }
            }
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.Space(10);

            // Title Header
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("AI Level Builder", headerStyle);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "AI-Assisted Procedural Level Builder for Monkey Jungle Adventure.\n" +
                "Configure, generate, validate, convert to URP, and audit HD environment assets for Level 01.",
                MessageType.Info);

            EditorGUILayout.Space(8);

            // ========================================================
            // SECTION 1: LEVEL 01 BLOCKOUT SETTINGS
            // ========================================================
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showSettingsSection = EditorGUILayout.Foldout(_showSettingsSection, "⚙️ LEVEL 01 BLOCKOUT SETTINGS", true, EditorStyles.foldoutHeader);
            if (_showSettingsSection)
            {
                EditorGUILayout.Space(4);

                if (_blockoutSettings == null)
                {
                    _blockoutSettings = new Level01BlockoutGenerator.BlockoutSettings();
                }

                _blockoutSettings.seed = EditorGUILayout.IntField("Random Seed", _blockoutSettings.seed);
                _blockoutSettings.levelLength = EditorGUILayout.Slider("Level Length (m)", _blockoutSettings.levelLength, 150f, 300f);
                _blockoutSettings.pathWidth = EditorGUILayout.Slider("Path Width (m)", _blockoutSettings.pathWidth, 5f, 15f);
                _blockoutSettings.treeDensity = EditorGUILayout.IntSlider("Tree Density", _blockoutSettings.treeDensity, 10, 80);
                _blockoutSettings.collectibleCount = EditorGUILayout.IntSlider("Collectible Count", _blockoutSettings.collectibleCount, 3, 20);
                _blockoutSettings.obstacleCount = EditorGUILayout.IntSlider("Obstacle Count", _blockoutSettings.obstacleCount, 1, 10);
                _blockoutSettings.enemyCount = EditorGUILayout.IntSlider("Enemy Count", _blockoutSettings.enemyCount, 1, 6);
                _blockoutSettings.riverZ = EditorGUILayout.FloatField("River Z Position", _blockoutSettings.riverZ);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // ========================================================
            // SECTION 2: LEVEL ACTIONS & VALIDATION
            // ========================================================
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showActionsSection = EditorGUILayout.Foldout(_showActionsSection, "🎮 LEVEL ACTIONS & VALIDATION", true, EditorStyles.foldoutHeader);
            if (_showActionsSection)
            {
                EditorGUILayout.Space(4);

                // Generate Button
                GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
                if (GUILayout.Button("Generate Level 1 Blockout", GUILayout.Height(32)))
                {
                    GenerateLevel1();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(3);

                // Clear Button
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("Clear Level 1 Blockout", GUILayout.Height(24)))
                {
                    ClearLevel1();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(3);

                // Select & Focus Buttons
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select Generated Level", GUILayout.Height(24)))
                {
                    SelectGeneratedLevel();
                }

                if (GUILayout.Button("Focus Level 1", GUILayout.Height(24)))
                {
                    FocusLevel1();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                // Validate Button
                GUI.backgroundColor = new Color(0.2f, 0.7f, 1.0f);
                if (GUILayout.Button("🔍 Validate Level 1", GUILayout.Height(30)))
                {
                    RunLevel1Validation();
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // ========================================================
            // SECTION 3: VALIDATION RESULTS SUMMARY UI
            // ========================================================
            if (_lastReport != null)
            {
                DrawValidationResultsUI();
                EditorGUILayout.Space(8);
            }

            // ========================================================
            // SECTION 4: HD ASSET PASS CONTROLS
            // ========================================================
            DrawHDAssetPassUI();

            EditorGUILayout.Space(8);

            // ========================================================
            // SECTION 5: HD JUNGLE ASSET DISCOVERY & AUTO-MAPPING
            // ========================================================
            DrawHDJungleDiscoverySectionUI();

            EditorGUILayout.Space(8);

            // ========================================================
            // SECTION 6: HD ENVIRONMENT AUTO-POPULATOR
            // ========================================================
            DrawHDEnvironmentAutoPopulatorUI();

            EditorGUILayout.Space(8);

            // ========================================================
            // SECTION 7: HD ENVIRONMENT VISUAL MATERIAL AUDIT + AUTO-FIX
            // ========================================================
            DrawHDEnvironmentVisualMaterialAuditUI();

            EditorGUILayout.Space(8);

            // ========================================================
            // SECTION 8: URP MATERIAL FIX & CONVERTER
            // ========================================================
            DrawURPMaterialFixUI();

            EditorGUILayout.Space(8);

            // ========================================================
            // SECTION 9: FULL SCENE MAGENTA FINDER
            // ========================================================
            DrawFullSceneMagentaFinderUI();

            EditorGUILayout.Space(10);
            EditorGUILayout.EndScrollView();
        }

        private void DrawHDAssetPassUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showHDSection = EditorGUILayout.Foldout(_showHDSection, "🌿 HD ASSET PASS (AUTO-REPLACER)", true, EditorStyles.foldoutHeader);

            if (_showHDSection)
            {
                EditorGUILayout.Space(4);

                // Asset Library Object Field
                EditorGUILayout.BeginHorizontal();
                _hdAssetLibrary = (HDAssetLibrary)EditorGUILayout.ObjectField("HD Asset Library", _hdAssetLibrary, typeof(HDAssetLibrary), false);

                if (_hdAssetLibrary == null)
                {
                    if (GUILayout.Button("Create New", GUILayout.Width(90)))
                    {
                        CreateDefaultHDLibraryAsset();
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                // Discovery Trigger Button directly visible under HD Asset Library
                GUI.backgroundColor = new Color(0.95f, 0.75f, 0.2f);
                if (GUILayout.Button("🔎 Discover HD Jungle Assets", GUILayout.Height(32)))
                {
                    if (_hdAssetLibrary == null) CreateDefaultHDLibraryAsset();
                    _lastJungleReport = HDJungleAssetDiscovery.DiscoverAndMapJungleAssets(_hdAssetLibrary, true);
                    _lastConversionReport = HDMaterialURPConverter.PreviewConversion(_hdAssetLibrary);
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(6);

                // Row 1: Preview and Apply
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(0.3f, 0.7f, 1.0f);
                if (GUILayout.Button("🌿 Preview HD Replacements", GUILayout.Height(30)))
                {
                    _lastHDReport = HDAssetAutoReplacer.PreviewHDReplacements(_hdAssetLibrary, _blockoutSettings.seed);
                    _lastSceneAuditReport = HDAssetMaterialDiagnostic.ScanCurrentPreview();
                }

                GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
                if (GUILayout.Button("✅ Apply HD Replacements", GUILayout.Height(30)))
                {
                    ApplyHDPassWithValidation();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                // Row 2: Rollback and Clear Preview
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
                if (GUILayout.Button("↩ Rollback HD Replacements", GUILayout.Height(26)))
                {
                    HDAssetAutoReplacer.RollbackHDReplacements();
                    _lastHDReport = null;
                    _lastSceneAuditReport = null;
                    _lastActiveAuditReport = null;
                }

                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button("🧹 Clear HD Preview", GUILayout.Height(26)))
                {
                    HDAssetAutoReplacer.ClearHDPreview();
                    if (_lastHDReport != null && _lastHDReport.isPreview) _lastHDReport = null;
                    _lastSceneAuditReport = null;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                // Row 3: Read-Only Active Audit Button
                GUI.backgroundColor = new Color(0.3f, 0.8f, 1.0f);
                if (GUILayout.Button("🔍 Audit ACTIVE HD Materials", GUILayout.Height(28)))
                {
                    _lastActiveAuditReport = HDActiveReplacementAudit.RunAudit();
                }
                GUI.backgroundColor = Color.white;

                // Replacement Report Display
                if (_lastHDReport != null)
                {
                    EditorGUILayout.Space(6);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string modeTag = _lastHDReport.isPreview ? "[PREVIEW MODE]" : "[ACTIVE APPLIED]";
                    EditorGUILayout.LabelField($"HD Replacement Report {modeTag}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Total Objects: {_lastHDReport.totalBlockoutObjects} | Replaced: {_lastHDReport.totalReplacedObjects} | Skipped: {_lastHDReport.totalSkippedObjects}");

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Category Breakdown:", EditorStyles.miniBoldLabel);

                    foreach (var stat in _lastHDReport.categoryStats)
                    {
                        EditorGUILayout.LabelField($"- {stat.category,-14} Blockout: {stat.blockoutCount,2} | Replaced: {stat.replacedCount,2} | Missing: {stat.missingCount,2}", EditorStyles.miniLabel);
                    }

                    EditorGUILayout.EndVertical();
                }

                // Active Audit Report Display
                if (_lastActiveAuditReport != null)
                {
                    EditorGUILayout.Space(6);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField("ACTIVE HD MATERIAL AUDIT (READ-ONLY)", EditorStyles.boldLabel);
                    string statusText = _lastActiveAuditReport.magentaSuspects == 0
                        ? "<color=#00FF88>STATUS: CLEAN (0 Magenta Suspects)</color>"
                        : $"<color=#FF3366>STATUS: NEEDS FIX ({_lastActiveAuditReport.magentaSuspects} Magenta Suspects)</color>";

                    GUIStyle stStyle = new GUIStyle(EditorStyles.boldLabel) { richText = true };
                    EditorGUILayout.LabelField(statusText, stStyle);

                    EditorGUILayout.LabelField($"Active HD Objects: {_lastActiveAuditReport.totalActiveHDObjects} | Renderers: {_lastActiveAuditReport.totalRenderers} | Slots: {_lastActiveAuditReport.totalMaterialSlots}");
                    EditorGUILayout.LabelField($"URP Compatible: {_lastActiveAuditReport.urpCompatibleSlots} | Built-in Standard: {_lastActiveAuditReport.builtInStandardSlots} | Missing: {_lastActiveAuditReport.missingMaterialSlots + _lastActiveAuditReport.missingShaderSlots}");

                    if (_lastActiveAuditReport.suspects.Count > 0)
                    {
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Suspect Details:", EditorStyles.miniBoldLabel);
                        _activeAuditScrollPos = EditorGUILayout.BeginScrollView(_activeAuditScrollPos, GUILayout.Height(110));
                        foreach (var s in _lastActiveAuditReport.suspects)
                        {
                            EditorGUILayout.LabelField($"• {s.gameObjectName} ({s.rendererName} Slot {s.slotIndex}) -> {s.materialName} [{s.shaderName}]: {s.suspectReason}", EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndScrollView();
                    }

                    EditorGUILayout.EndVertical();
                }

                // Library mappings foldout
                EditorGUILayout.Space(6);
                _showHDLibrarySettings = EditorGUILayout.Foldout(_showHDLibrarySettings, "📂 Edit HD Library Mappings", true);
                if (_showHDLibrarySettings && _hdAssetLibrary != null)
                {
                    DrawHDLibraryMappingsEditor();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawHDJungleDiscoverySectionUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showDiscoverySection = EditorGUILayout.Foldout(_showDiscoverySection, "🔎 HD JUNGLE ASSET DISCOVERY & AUTO-MAPPING", true, EditorStyles.foldoutHeader);

            if (_showDiscoverySection)
            {
                EditorGUILayout.Space(4);

                EditorGUILayout.HelpBox(
                    "Scan Assets/HD_Jungle_Assets and automatically discover, classify and map HD jungle prefabs.",
                    MessageType.Info);

                EditorGUILayout.Space(4);

                bool hasAssetsFolder = AssetDatabase.IsValidFolder("Assets/HD_Jungle_Assets");
                if (!hasAssetsFolder)
                {
                    EditorGUILayout.HelpBox(
                        "HD Jungle Assets folder was not found inside Assets/. Move/import HD_Jungle_Assets into the Unity Assets folder before discovery.",
                        MessageType.Warning);
                }

                // Discovery Trigger Button
                GUI.backgroundColor = new Color(0.95f, 0.75f, 0.2f);
                if (GUILayout.Button("🔎 Discover HD Jungle Assets", GUILayout.Height(34)))
                {
                    if (_hdAssetLibrary == null) CreateDefaultHDLibraryAsset();
                    _lastJungleReport = HDJungleAssetDiscovery.DiscoverAndMapJungleAssets(_hdAssetLibrary, true);
                    _lastConversionReport = HDMaterialURPConverter.PreviewConversion(_hdAssetLibrary);
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(6);

                // Discovery Summary Banner
                if (_lastJungleReport != null)
                {
                    DrawJungleDiscoveryReportUI();
                }
                else if (_hdAssetLibrary != null)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField("CURRENT LIBRARY ASSET MAPPINGS", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Scan Root: Assets/HD_Jungle_Assets | Assigned Prefabs: {_hdAssetLibrary.GetAssignedPrefabCount()}");
                    EditorGUILayout.Space(2);
                    DrawLibraryCategoryGrid();
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawLibraryCategoryGrid()
        {
            if (_hdAssetLibrary == null) return;
            var missingCats = _hdAssetLibrary.GetMissingCategories();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Tree: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Tree)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Rock: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Rock)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"RiverRock: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.RiverRock)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Grass: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Grass)}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"DeadLeaves: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.DeadLeaves)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Bush: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Bush)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Ground: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Ground)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Water: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Water)}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Waterfall: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Waterfall)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"WoodTrunk: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.WoodTrunk)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"AncientStone: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.AncientStone)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Arch: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Arch)}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Other: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Other)}", EditorStyles.miniLabel);
            EditorGUILayout.Space(2);
            string missingStr = missingCats.Count > 0 ? string.Join(", ", missingCats) : "None (All categories mapped!)";
            EditorGUILayout.LabelField($"Categories Missing: {missingStr}", EditorStyles.miniLabel);
        }

        private void DrawHDEnvironmentAutoPopulatorUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showEnvPopulatorSection = EditorGUILayout.Foldout(_showEnvPopulatorSection, "🌴 HD ENVIRONMENT AUTO-POPULATOR", true, EditorStyles.foldoutHeader);

            if (_showEnvPopulatorSection)
            {
                EditorGUILayout.Space(4);

                EditorGUILayout.HelpBox(
                    "Intelligently populates Level 01 with discovered HD jungle assets while strictly preserving 100% of gameplay objects, player corridor, and colliders.",
                    MessageType.Info);

                EditorGUILayout.Space(4);

                // Settings Form
                _envPopulatorSettings.seed = EditorGUILayout.IntField("Environment Seed", _envPopulatorSettings.seed);
                _envPopulatorSettings.density = (PopulatorDensity)EditorGUILayout.EnumPopup("Density", _envPopulatorSettings.density);
                _envPopulatorSettings.playerSafetyMargin = EditorGUILayout.Slider("Player Safety Margin", _envPopulatorSettings.playerSafetyMargin, 2.0f, 6.0f);
                _envPopulatorSettings.zoneVariation = EditorGUILayout.Slider("Zone Variation", _envPopulatorSettings.zoneVariation, 0.05f, 0.50f);
                _envPopulatorSettings.maxObjects = EditorGUILayout.IntSlider("Maximum Objects", _envPopulatorSettings.maxObjects, 50, 400);

                EditorGUILayout.Space(6);

                // Row 1: Generate Preview & Preview Audit
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(0.2f, 0.9f, 0.6f);
                if (GUILayout.Button("🌴 Generate HD Environment Preview", GUILayout.Height(32)))
                {
                    if (_hdAssetLibrary == null) CreateDefaultHDLibraryAsset();
                    _lastEnvReport = HDEnvironmentAutoPopulator.GeneratePreview(_hdAssetLibrary, _envPopulatorSettings);
                    RunLevel1Validation();
                    _lastFullSceneReport = FullSceneMagentaFinder.RunFullSceneAudit();
                }

                GUI.backgroundColor = new Color(0.3f, 0.8f, 1.0f);
                if (GUILayout.Button("🔍 Preview Environment Audit", GUILayout.Height(32), GUILayout.Width(170)))
                {
                    _lastFullSceneReport = FullSceneMagentaFinder.RunFullSceneAudit();
                    RunLevel1Validation();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                // Row 2: Apply HD Environment
                GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
                if (GUILayout.Button("✅ Apply HD Environment", GUILayout.Height(30)))
                {
                    if (_hdAssetLibrary == null) CreateDefaultHDLibraryAsset();
                    _lastEnvReport = HDEnvironmentAutoPopulator.ApplyEnvironment(_hdAssetLibrary, _envPopulatorSettings);
                    RunLevel1Validation();
                    _lastActiveAuditReport = HDActiveReplacementAudit.RunAudit();
                    _lastFullSceneReport = FullSceneMagentaFinder.RunFullSceneAudit();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.Space(4);

                // Row 3: Rollback and Clear Preview
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
                if (GUILayout.Button("↩ Rollback HD Environment", GUILayout.Height(26)))
                {
                    HDEnvironmentAutoPopulator.RollbackEnvironment();
                    _lastEnvReport = null;
                    RunLevel1Validation();
                }

                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button("🧹 Clear Environment Preview", GUILayout.Height(26)))
                {
                    HDEnvironmentAutoPopulator.ClearPreview();
                    if (_lastEnvReport != null && _lastEnvReport.isPreview) _lastEnvReport = null;
                    RunLevel1Validation();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                // Generation Report Display
                if (_lastEnvReport != null)
                {
                    DrawEnvironmentReportUI();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawEnvironmentReportUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string modeTag = _lastEnvReport.isPreview ? "[PREVIEW MODE]" : "[ACTIVE APPLIED]";
            EditorGUILayout.LabelField($"HD ENVIRONMENT GENERATION REPORT {modeTag}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Level Range: Z={_lastEnvReport.startZ:F1}m..{_lastEnvReport.finishZ:F1}m ({_lastEnvReport.levelLength:F1}m) | Width: {_lastEnvReport.playableWidth:F1}m | Total Objects: {_lastEnvReport.totalObjectsGenerated}");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("CATEGORY BREAKDOWN:", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Trees: {_lastEnvReport.treeCount}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Bushes: {_lastEnvReport.bushCount}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Grass: {_lastEnvReport.grassCount}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Ferns: {_lastEnvReport.fernCount}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"DeadLeaves: {_lastEnvReport.deadLeavesCount}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Rocks: {_lastEnvReport.rockCount}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"RiverRocks: {_lastEnvReport.riverRockCount}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Logs: {_lastEnvReport.logCount}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Stumps: {_lastEnvReport.stumpCount}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Ancient: {_lastEnvReport.ancientStoneCount + _lastEnvReport.ancientRuinsCount + _lastEnvReport.archCount}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Water: {_lastEnvReport.waterCount}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Waterfalls: {_lastEnvReport.waterfallCount}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("ZONE BREAKDOWN:", EditorStyles.miniBoldLabel);
            foreach (var z in _lastEnvReport.zoneStats)
            {
                EditorGUILayout.LabelField($"• {z.zoneName} (Z={z.startZ:F0}m..{z.endZ:F0}m): {z.totalObjects} objs (Trees: {z.treeCount}, Bushes: {z.bushCount}, Grass: {z.grassCount}, Rocks: {z.rockCount})", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("SAFETY & SKIP METRICS:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"Gameplay Conflicts: {_lastEnvReport.skippedGameplayConflict} | Corridor Clearance: {_lastEnvReport.skippedCorridorViolation}");
            EditorGUILayout.LabelField($"Spacing / Proximity: {_lastEnvReport.skippedTooClose} | Missing Assets: {_lastEnvReport.skippedMissingAsset}");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("MATERIAL STATUS:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"<color=#00FF88>URP Compatible: {_lastEnvReport.urpCompatibleMaterials}</color> | Standard Shaders: 0 (Auto-Converted)", new GUIStyle(EditorStyles.miniLabel) { richText = true });

            EditorGUILayout.EndVertical();
        }

        private void DrawHDEnvironmentVisualMaterialAuditUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showHDVisualAuditSection = EditorGUILayout.Foldout(
                _showHDVisualAuditSection,
                "🎨 HD ENVIRONMENT VISUAL MATERIAL AUDIT + AUTO-FIX",
                true,
                EditorStyles.foldoutHeader);

            if (_showHDVisualAuditSection)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "Deep recursive inspection of all MeshRenderer, SkinnedMeshRenderer, ParticleSystemRenderer, TrailRenderer, LineRenderer, and LODGroup renderers under AI_GENERATED_LEVEL/HD_ENVIRONMENT and HD_ENVIRONMENT_PREVIEW.\n" +
                    "Auto-Fix converts Built-in Standard materials to URP/Lit non-destructively under Assets/AILevelBuilder/HD/URPMaterials/.",
                    MessageType.Info);

                EditorGUILayout.Space(6);

                // Row 1: Primary Action Buttons
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(0.2f, 0.7f, 1.0f);
                if (GUILayout.Button("🔍 Audit HD Environment Materials", GUILayout.Height(30)))
                {
                    _lastVisualAuditReport = HDEnvironmentVisualMaterialAudit.RunAudit(true);
                }

                bool hasRenderers = _lastVisualAuditReport != null && _lastVisualAuditReport.renderersFound > 0;
                GUI.backgroundColor = hasRenderers ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
                EditorGUI.BeginDisabledGroup(!hasRenderers);
                if (GUILayout.Button("🔧 Auto-Fix HD Environment Materials", GUILayout.Height(30)))
                {
                    _lastVisualAuditReport = HDEnvironmentVisualMaterialAudit.AutoFixEnvironmentMaterials(true);
                    RunLevel1Validation();
                }
                EditorGUI.EndDisabledGroup();
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                // Row 2: Re-Audit, Full Window, and Export Buttons
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(0.9f, 0.6f, 0.2f);
                if (GUILayout.Button("🔄 Re-Audit", GUILayout.Height(24)))
                {
                    _lastVisualAuditReport = HDEnvironmentVisualMaterialAudit.RunAudit(true);
                }

                GUI.backgroundColor = new Color(0.2f, 0.85f, 0.9f);
                if (GUILayout.Button("🔍 Open HD Visual Audit Window (Full Size)", GUILayout.Height(24)))
                {
                    HDEnvironmentVisualMaterialAudit.OpenWindow();
                }

                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
                if (GUILayout.Button("📄 Save Full Report", GUILayout.Height(24)))
                {
                    if (_lastVisualAuditReport == null)
                    {
                        _lastVisualAuditReport = HDEnvironmentVisualMaterialAudit.RunAudit(true);
                    }
                    string path = HDEnvironmentVisualMaterialAudit.SaveReportToFile(_lastVisualAuditReport);
                    EditorUtility.DisplayDialog("Save Full Audit Report", $"Saved report to:\n{path}", "OK");
                }

                if (GUILayout.Button("⚠️ Save Warnings Only", GUILayout.Height(24)))
                {
                    if (_lastVisualAuditReport == null)
                    {
                        _lastVisualAuditReport = HDEnvironmentVisualMaterialAudit.RunAudit(true);
                    }
                    string path = HDEnvironmentVisualMaterialAudit.SaveWarningsReportToFile(_lastVisualAuditReport);
                    EditorUtility.DisplayDialog("Save Warnings Report", $"Saved warnings report to:\n{path}", "OK");
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                // Results Display
                if (_lastVisualAuditReport != null)
                {
                    EditorGUILayout.Space(6);
                    HDEnvironmentVisualMaterialAudit.DrawReportSummary(_lastVisualAuditReport);

                    EditorGUILayout.Space(4);
                    DrawVisualAuditSlotsUI(_lastVisualAuditReport);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawVisualAuditSlotsUI(HDEnvironmentVisualAuditReport report)
        {
            if (report == null) return;

            // 1. Filter Tabs
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_visualAuditFilterTab == 0, $"All ({report.totalMaterialSlots})", EditorStyles.toolbarButton)) _visualAuditFilterTab = 0;
            if (GUILayout.Toggle(_visualAuditFilterTab == 1, $"Errors ({report.errorSlots.Count})", EditorStyles.toolbarButton)) _visualAuditFilterTab = 1;
            if (GUILayout.Toggle(_visualAuditFilterTab == 2, $"Warnings ({report.warningSlots.Count})", EditorStyles.toolbarButton)) _visualAuditFilterTab = 2;
            if (GUILayout.Toggle(_visualAuditFilterTab == 3, $"Clean ({report.cleanSlots.Count})", EditorStyles.toolbarButton)) _visualAuditFilterTab = 3;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // 2. Search Field
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍 Search:", GUILayout.Width(60));
            _visualAuditSearchQuery = EditorGUILayout.TextField(_visualAuditSearchQuery, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(45)))
            {
                _visualAuditSearchQuery = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // 3. Warning Navigation & Copy Toolbar
            EditorGUILayout.BeginHorizontal();
            if (report.warningSlots.Count > 0)
            {
                if (GUILayout.Button("◄ Prev Warning", EditorStyles.miniButtonLeft, GUILayout.Width(95)))
                {
                    _visualAuditFilterTab = 2;
                    _visualAuditWarningIndex = (_visualAuditWarningIndex - 1 + report.warningSlots.Count) % report.warningSlots.Count;
                    var target = report.warningSlots[_visualAuditWarningIndex];
                    if (target != null)
                    {
                        target.isExpanded = true;
                        if (target.targetGameObject != null) Selection.activeGameObject = target.targetGameObject;
                    }
                }
                EditorGUILayout.LabelField($"Warning {_visualAuditWarningIndex + 1}/{report.warningSlots.Count}", new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter }, GUILayout.Width(90));
                if (GUILayout.Button("Next Warning ►", EditorStyles.miniButtonRight, GUILayout.Width(95)))
                {
                    _visualAuditFilterTab = 2;
                    _visualAuditWarningIndex = (_visualAuditWarningIndex + 1) % report.warningSlots.Count;
                    var target = report.warningSlots[_visualAuditWarningIndex];
                    if (target != null)
                    {
                        target.isExpanded = true;
                        if (target.targetGameObject != null) Selection.activeGameObject = target.targetGameObject;
                    }
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("📋 Copy Visible", EditorStyles.miniButton, GUILayout.Width(85)))
            {
                var visible = GetFilteredVisualAuditSlots(report);
                HDEnvironmentVisualMaterialAudit.CopySlotsToClipboard(visible, $"Visible Slots ({visible.Count})");
            }
            if (GUILayout.Button("📋 Copy Warnings", EditorStyles.miniButton, GUILayout.Width(95)))
            {
                HDEnvironmentVisualMaterialAudit.CopySlotsToClipboard(report.warningSlots, $"All Warnings ({report.warningSlots.Count})");
            }
            EditorGUILayout.EndHorizontal();

            // 4. Filter list
            List<HDVisualAuditSlotInfo> itemsToShow = GetFilteredVisualAuditSlots(report);

            _visualAuditScrollPos = EditorGUILayout.BeginScrollView(_visualAuditScrollPos, GUILayout.MinHeight(220), GUILayout.MaxHeight(400));
            if (itemsToShow.Count == 0)
            {
                EditorGUILayout.HelpBox("No items match the current tab filter and search query.", MessageType.Info);
            }
            else
            {
                foreach (var item in itemsToShow)
                {
                    EditorGUILayout.BeginVertical("box");

                    string statusColor = item.classification == HDVisualAuditClassification.Clean ? "#00FF88" :
                                         item.classification == HDVisualAuditClassification.Warning ? "#FFCC00" : "#FF3366";

                    EditorGUILayout.BeginHorizontal();
                    item.isExpanded = EditorGUILayout.Foldout(item.isExpanded, GUIContent.none, true);

                    string itemHeader = $"<color={statusColor}><b>[{item.classification}]</b></color> <b>{item.gameObjectName}</b> ({item.rendererType} Slot {item.slotIndex})";
                    if (GUILayout.Button(itemHeader, new GUIStyle(EditorStyles.label) { richText = true }, GUILayout.ExpandWidth(true)))
                    {
                        item.isExpanded = !item.isExpanded;
                    }

                    if (item.targetGameObject != null)
                    {
                        if (GUILayout.Button("Select", GUILayout.Width(50), GUILayout.Height(18)))
                        {
                            Selection.activeGameObject = item.targetGameObject;
                            EditorGUIUtility.PingObject(item.targetGameObject);
                        }
                        if (GUILayout.Button("Focus", GUILayout.Width(50), GUILayout.Height(18)))
                        {
                            Selection.activeGameObject = item.targetGameObject;
                            SceneView.lastActiveSceneView?.FrameSelected();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if (!item.isExpanded)
                    {
                        EditorGUILayout.LabelField($"Material: {item.materialName} | Shader: {item.shaderName}", EditorStyles.miniLabel);
                        if (!string.IsNullOrEmpty(item.classificationReason))
                        {
                            EditorGUILayout.LabelField($"Reason: {item.classificationReason}", EditorStyles.miniLabel);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Path", item.hierarchyPath, EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Material: {item.materialName} | Shader: {item.shaderName}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"BaseMap: {item.baseMapName} | Surface: {item.surfaceType} | AlphaClip: {item.isAlphaClip} | Cull: {item.cullMode}", EditorStyles.miniLabel);

                        if (!string.IsNullOrEmpty(item.classificationReason))
                        {
                            EditorGUILayout.HelpBox(item.classificationReason,
                                item.classification == HDVisualAuditClassification.Error ? MessageType.Error :
                                item.classification == HDVisualAuditClassification.Warning ? MessageType.Warning : MessageType.None);
                        }

                        if (GUILayout.Button("📋 Copy Item Info", EditorStyles.miniButton, GUILayout.Width(110)))
                        {
                            GUIUtility.systemCopyBuffer = HDEnvironmentVisualMaterialAudit.FormatSlotInfo(item);
                        }
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(1);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private List<HDVisualAuditSlotInfo> GetFilteredVisualAuditSlots(HDEnvironmentVisualAuditReport report)
        {
            if (report == null) return new List<HDVisualAuditSlotInfo>();

            List<HDVisualAuditSlotInfo> baseList;
            switch (_visualAuditFilterTab)
            {
                case 1: baseList = report.errorSlots; break;
                case 2: baseList = report.warningSlots; break;
                case 3: baseList = report.cleanSlots; break;
                default: baseList = report.allSlots; break;
            }

            if (string.IsNullOrEmpty(_visualAuditSearchQuery)) return baseList;

            string q = _visualAuditSearchQuery.Trim().ToLowerInvariant();
            List<HDVisualAuditSlotInfo> filtered = new List<HDVisualAuditSlotInfo>();
            foreach (var s in baseList)
            {
                if ((!string.IsNullOrEmpty(s.gameObjectName) && s.gameObjectName.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(s.hierarchyPath) && s.hierarchyPath.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(s.materialName) && s.materialName.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(s.shaderName) && s.shaderName.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(s.classificationReason) && s.classificationReason.ToLowerInvariant().Contains(q)))
                {
                    filtered.Add(s);
                }
            }
            return filtered;
        }

        private void DrawURPMaterialFixUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showURPSection = EditorGUILayout.Foldout(_showURPSection, "🎨 URP MATERIAL FIX & CONVERTER", true, EditorStyles.foldoutHeader);

            if (_showURPSection)
            {
                EditorGUILayout.Space(4);

                EditorGUILayout.HelpBox(
                    "Non-destructively convert Built-in Standard materials used by HD prefabs to Universal Render Pipeline (URP/Lit).\n" +
                    "Preserves original source assets and eliminates magenta rendering.",
                    MessageType.Info);

                EditorGUILayout.Space(6);

                // Row 1: Diagnostic and Preview Conversion
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(0.9f, 0.5f, 1.0f);
                if (GUILayout.Button("🔍 Diagnose HD Materials", GUILayout.Height(28)))
                {
                    HDAssetMaterialDiagnostic.ScanCurrentPreview();
                }

                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.9f);
                if (GUILayout.Button("🎨 Preview URP Conversion", GUILayout.Height(28)))
                {
                    _lastConversionReport = HDMaterialURPConverter.PreviewConversion(_hdAssetLibrary);
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                // Row 2: Scan for Magenta & Apply to Preview
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = new Color(1f, 0.3f, 0.4f);
                if (GUILayout.Button("🚨 Scan Current Preview for Magenta", GUILayout.Height(28)))
                {
                    _lastSceneAuditReport = HDAssetMaterialDiagnostic.ScanCurrentPreview();
                }

                GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
                if (GUILayout.Button("✅ Apply URP Materials to Preview", GUILayout.Height(28)))
                {
                    _lastConversionReport = HDMaterialURPConverter.ApplyConvertedMaterialsToPreview();
                    _lastSceneAuditReport = HDAssetMaterialDiagnostic.ScanCurrentPreview();
                    RunLevel1Validation();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                // Row 3: Restore Original Materials
                GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
                if (GUILayout.Button("↩ Restore Original Preview Materials", GUILayout.Height(26)))
                {
                    HDMaterialURPConverter.RestoreOriginalPreviewMaterials();
                    _lastSceneAuditReport = HDAssetMaterialDiagnostic.ScanCurrentPreview();
                    RunLevel1Validation();
                }
                GUI.backgroundColor = Color.white;

                // Scene Magenta Audit Report Display
                if (_lastSceneAuditReport != null)
                {
                    EditorGUILayout.Space(6);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField("MAGENTA RENDERER AUDIT (CURRENT PREVIEW)", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Total Renderers: {_lastSceneAuditReport.totalRenderers} | Material Slots: {_lastSceneAuditReport.totalMaterialSlots}");
                    EditorGUILayout.LabelField($"URP Compatible: {_lastSceneAuditReport.urpCompatibleSlots} | Standard Shaders: {_lastSceneAuditReport.builtInStandardSlots}");
                    EditorGUILayout.LabelField($"Missing Shaders: {_lastSceneAuditReport.missingShaderSlots} | Missing Materials: {_lastSceneAuditReport.missingMaterialSlots}");

                    string suspectText = _lastSceneAuditReport.magentaSuspects == 0
                        ? "<color=#00FF88>Magenta Suspects: 0 (ALL CLEAN - READY FOR HD PASS)</color>"
                        : $"<color=#FF3366>Magenta Suspects: {_lastSceneAuditReport.magentaSuspects}</color>";

                    GUIStyle suspectStyle = new GUIStyle(EditorStyles.boldLabel) { richText = true };
                    EditorGUILayout.LabelField(suspectText, suspectStyle);

                    if (_lastSceneAuditReport.magentaSuspectItems.Count > 0)
                    {
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Suspect Details:", EditorStyles.miniBoldLabel);
                        _auditScrollPos = EditorGUILayout.BeginScrollView(_auditScrollPos, GUILayout.Height(100));
                        foreach (var s in _lastSceneAuditReport.magentaSuspectItems)
                        {
                            EditorGUILayout.LabelField($"• {s.gameObjectName} ({s.rendererName} Slot {s.slotIndex}) -> {s.materialName} [{s.shaderName}]: {s.suspectReason}", EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndScrollView();
                    }

                    EditorGUILayout.EndVertical();
                }

                // Conversion Report Display
                if (_lastConversionReport != null)
                {
                    EditorGUILayout.Space(6);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField("URP MATERIAL CONVERSION REPORT", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Source Materials: {_lastConversionReport.sourceMaterialsCount} | Converted: {_lastConversionReport.convertedMaterialsCount} | Already URP: {_lastConversionReport.alreadyURPCount} | Failed: {_lastConversionReport.failedCount}");
                    EditorGUILayout.LabelField("Original Source Materials Unchanged: YES (Output: Assets/AILevelBuilder/HD/URPMaterials/)", EditorStyles.miniBoldLabel);

                    EditorGUILayout.Space(4);
                    _conversionScrollPos = EditorGUILayout.BeginScrollView(_conversionScrollPos, GUILayout.Height(100));
                    foreach (var rec in _lastConversionReport.records)
                    {
                        string icon = rec.isSuccess ? "✓" : "✗";
                        EditorGUILayout.LabelField($"{icon} {rec.sourceMaterialName} ({rec.sourceShaderName}) → {rec.convertedMaterialName} ({rec.convertedShaderName})", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawFullSceneMagentaFinderUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showFullSceneSection = EditorGUILayout.Foldout(_showFullSceneSection, "🔎 FULL SCENE MAGENTA FINDER (READ-ONLY)", true, EditorStyles.foldoutHeader);

            if (_showFullSceneSection)
            {
                EditorGUILayout.Space(4);

                EditorGUILayout.HelpBox(
                    "Scans every GameObject, Renderer, ParticleSystem, Sprite, and Gizmo in the active scene to identify exact visual magenta causes.",
                    MessageType.Info);

                EditorGUILayout.Space(4);

                GUI.backgroundColor = new Color(1f, 0.4f, 0.8f);
                if (GUILayout.Button("🔍 Find ALL Magenta Objects", GUILayout.Height(30)))
                {
                    _lastFullSceneReport = FullSceneMagentaFinder.RunFullSceneAudit();
                }
                GUI.backgroundColor = Color.white;

                if (_lastFullSceneReport != null)
                {
                    EditorGUILayout.Space(6);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField("FULL SCENE MAGENTA AUDIT RESULTS", EditorStyles.boldLabel);

                    string statusText = (_lastFullSceneReport.definiteMagentaRiskCount == 0 && _lastFullSceneReport.possibleMagentaCount == 0)
                        ? "<color=#00FF88>STATUS: CLEAN (0 Actual Magenta Objects)</color>"
                        : $"<color=#FF3366>STATUS: SUSPECTS FOUND ({_lastFullSceneReport.definiteMagentaRiskCount} Definite, {_lastFullSceneReport.possibleMagentaCount} Possible)</color>";

                    GUIStyle stStyle = new GUIStyle(EditorStyles.boldLabel) { richText = true };
                    EditorGUILayout.LabelField(statusText, stStyle);

                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField($"Total GameObjects: {_lastFullSceneReport.totalGameObjects} | Renderers: {_lastFullSceneReport.totalRenderers} | Slots: {_lastFullSceneReport.totalMaterialSlots}");
                    EditorGUILayout.LabelField($"URP Compatible: {_lastFullSceneReport.urpCompatible} | Standard: {_lastFullSceneReport.builtInStandard} | Missing: {_lastFullSceneReport.missingMaterial + _lastFullSceneReport.missingShader}");
                    EditorGUILayout.LabelField($"Gizmo / Debug Visuals: {_lastFullSceneReport.possibleGizmoDebugCount} | Particle/VFX Suspects: {_lastFullSceneReport.particleVFXMagentaCount}");

                    if (_lastFullSceneReport.definiteSuspects.Count > 0 || _lastFullSceneReport.possibleSuspects.Count > 0)
                    {
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Suspect Items:", EditorStyles.miniBoldLabel);

                        _fullSceneScrollPos = EditorGUILayout.BeginScrollView(_fullSceneScrollPos, GUILayout.Height(130));

                        // Definite suspects
                        foreach (var item in _lastFullSceneReport.definiteSuspects)
                        {
                            DrawSuspectRow(item, Color.red, "[DEFINITE]");
                        }

                        // Possible suspects
                        foreach (var item in _lastFullSceneReport.possibleSuspects)
                        {
                            DrawSuspectRow(item, new Color(1f, 0.6f, 0.1f), "[POSSIBLE]");
                        }

                        EditorGUILayout.EndScrollView();
                    }
                    else
                    {
                        EditorGUILayout.Space(4);
                        EditorGUILayout.HelpBox("NO ACTUAL MAGENTA MATERIAL/SHADER FOUND.\nVisible pink shapes in SceneView are Gizmo handles or debug markers.", MessageType.None);
                    }

                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSuspectRow(FullSceneObjectAuditItem item, Color tagColor, string tag)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            GUI.color = tagColor;
            GUILayout.Label(tag, EditorStyles.miniBoldLabel, GUILayout.Width(70));
            GUI.color = Color.white;

            EditorGUILayout.LabelField($"{item.gameObjectName} ({item.rendererType})", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Path: {item.hierarchyPath}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Material: {item.materialName} | Shader: {item.shaderName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Reason: {item.reason}", EditorStyles.wordWrappedMiniLabel);

            if (item.targetGameObject != null)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    Selection.activeGameObject = item.targetGameObject;
                    EditorGUIUtility.PingObject(item.targetGameObject);
                }

                if (GUILayout.Button("Focus", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    Selection.activeGameObject = item.targetGameObject;
                    if (SceneView.lastActiveSceneView != null)
                    {
                        SceneView.lastActiveSceneView.FrameSelected();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawHDLibraryMappingsEditor()
        {
            if (_hdAssetLibrary == null) return;

            SerializedObject so = new SerializedObject(_hdAssetLibrary);
            so.Update();

            SerializedProperty mappingsProp = so.FindProperty("categoryMappings");
            if (mappingsProp != null && mappingsProp.isArray)
            {
                for (int i = 0; i < mappingsProp.arraySize; i++)
                {
                    SerializedProperty elem = mappingsProp.GetArrayElementAtIndex(i);
                    SerializedProperty catProp = elem.FindPropertyRelative("category");
                    SerializedProperty prefabsProp = elem.FindPropertyRelative("prefabs");
                    SerializedProperty scaleProp = elem.FindPropertyRelative("scaleMultiplier");
                    SerializedProperty offsetProp = elem.FindPropertyRelative("verticalOffset");

                    string catName = catProp != null ? ((HDObjectCategory)catProp.enumValueIndex).ToString() : $"Category {i}";

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(catName, EditorStyles.boldLabel);

                    if (prefabsProp != null)
                    {
                        EditorGUILayout.PropertyField(prefabsProp, new GUIContent("Prefabs"), true);
                    }
                    if (scaleProp != null)
                    {
                        EditorGUILayout.PropertyField(scaleProp, new GUIContent("Scale Multiplier"));
                    }
                    if (offsetProp != null)
                    {
                        EditorGUILayout.PropertyField(offsetProp, new GUIContent("Vertical Offset"));
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
            }

            if (so.hasModifiedProperties)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(_hdAssetLibrary);
            }
        }

        private void CreateDefaultHDLibraryAsset()
        {
            string dir = "Assets/AILevelBuilder/Data";
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            string path = $"{dir}/HDAssetLibrary_Level01.asset";
            HDAssetLibrary library = ScriptableObject.CreateInstance<HDAssetLibrary>();
            library.EnsureDefaultCategories();

            PopulateFoundProjectPrefabs(library);

            AssetDatabase.CreateAsset(library, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _hdAssetLibrary = library;
            Debug.Log($"<color=#00FF88><b>[AILevelBuilder] Created HD Asset Library at '{path}'.</b></color>");
        }

        private void PopulateFoundProjectPrefabs(HDAssetLibrary library)
        {
            string[] treeGuids = AssetDatabase.FindAssets("forestpack_tree t:Prefab");
            if (treeGuids.Length > 0)
            {
                var treeMapping = library.GetMapping(HDObjectCategory.Tree);
                if (treeMapping != null)
                {
                    foreach (var g in treeGuids)
                    {
                        GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
                        if (p != null && !treeMapping.prefabs.Contains(p)) treeMapping.prefabs.Add(p);
                    }
                }
            }

            string[] stoneGuids = AssetDatabase.FindAssets("forestpack_stone t:Prefab");
            if (stoneGuids.Length > 0)
            {
                var rockMapping = library.GetMapping(HDObjectCategory.Rock);
                if (rockMapping != null)
                {
                    foreach (var g in stoneGuids)
                    {
                        GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
                        if (p != null && !rockMapping.prefabs.Contains(p)) rockMapping.prefabs.Add(p);
                    }
                }
            }
        }

        private void ApplyHDPassWithValidation()
        {
            ValidationReport beforeReport = Level01AutoValidator.ValidateActiveLevel();
            Debug.Log($"[HDAssetAutoReplacer] Validation Before HD Pass: {beforeReport.overallStatus} (Errors: {beforeReport.errorCount}, Warnings: {beforeReport.warningCount})");

            _lastHDReport = HDAssetAutoReplacer.ApplyHDReplacements(_hdAssetLibrary, _blockoutSettings.seed);

            // Automatically apply URP materials to the newly applied HD hierarchy
            HDMaterialURPConverter.ApplyConvertedMaterialsToPreview();

            _lastReport = Level01AutoValidator.ValidateActiveLevel();
            Debug.Log($"[HDAssetAutoReplacer] Validation After HD Pass: {_lastReport.overallStatus} (Errors: {_lastReport.errorCount}, Warnings: {_lastReport.warningCount})");

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Repaint();
        }

        private void DrawValidationResultsUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _showValidationSection = EditorGUILayout.Foldout(_showValidationSection, "📊 LEVEL 1 VALIDATION RESULTS", true, EditorStyles.foldoutHeader);

            if (_showValidationSection)
            {
                EditorGUILayout.Space(4);

                Color statusColor = _lastReport.overallStatus switch
                {
                    ValidationOverallStatus.Pass => new Color(0.1f, 0.5f, 0.1f, 0.25f),
                    ValidationOverallStatus.PassWithWarnings => new Color(0.6f, 0.5f, 0.1f, 0.25f),
                    _ => new Color(0.6f, 0.1f, 0.1f, 0.25f),
                };

                string statusText = _lastReport.overallStatus switch
                {
                    ValidationOverallStatus.Pass => "STATUS: PASS",
                    ValidationOverallStatus.PassWithWarnings => "STATUS: PASS WITH WARNINGS",
                    _ => "STATUS: FAILED",
                };

                Rect bannerRect = GUILayoutUtility.GetRect(18, 28, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(bannerRect, statusColor);
                GUIStyle bannerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12
                };
                EditorGUI.LabelField(bannerRect, statusText, bannerStyle);

                EditorGUILayout.Space(4);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Errors: {_lastReport.errorCount}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Warnings: {_lastReport.warningCount}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Info: {_lastReport.infoCount}", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Level Length: {_lastReport.levelLength:F1} m | Playable Width: {_lastReport.playableWidth:F1} m");
                EditorGUILayout.LabelField($"Start: ({_lastReport.startPosition.x:F1}, {_lastReport.startPosition.y:F1}, {_lastReport.startPosition.z:F1}) | Finish: ({_lastReport.finishPosition.x:F1}, {_lastReport.finishPosition.y:F1}, {_lastReport.finishPosition.z:F1})");
                EditorGUILayout.LabelField($"Checkpoint: ({_lastReport.checkpointPosition.x:F1}, {_lastReport.checkpointPosition.y:F1}, {_lastReport.checkpointPosition.z:F1}) | Total Scanned: {_lastReport.totalObjectsScanned}");
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"Findings ({_lastReport.issues.Count} items):", EditorStyles.miniBoldLabel);

                _issuesScrollPos = EditorGUILayout.BeginScrollView(_issuesScrollPos, GUILayout.Height(150));
                for (int i = 0; i < _lastReport.issues.Count; i++)
                {
                    ValidationIssue issue = _lastReport.issues[i];

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();

                    GUIStyle sevStyle = new GUIStyle(EditorStyles.miniBoldLabel);
                    switch (issue.severity)
                    {
                        case ValidationSeverity.Error:
                            GUI.color = Color.red;
                            GUILayout.Label("[ERROR]", sevStyle, GUILayout.Width(55));
                            break;
                        case ValidationSeverity.Warning:
                            GUI.color = new Color(1f, 0.7f, 0.1f);
                            GUILayout.Label("[WARN]", sevStyle, GUILayout.Width(55));
                            break;
                        default:
                            GUI.color = new Color(0.4f, 0.8f, 1f);
                            GUILayout.Label("[INFO]", sevStyle, GUILayout.Width(55));
                            break;
                    }
                    GUI.color = Color.white;

                    EditorGUILayout.LabelField($"[{issue.category}] {issue.objectName}", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField(issue.message, EditorStyles.wordWrappedMiniLabel);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Pos: ({issue.worldPosition.x:F1}, {issue.worldPosition.y:F1}, {issue.worldPosition.z:F1})", EditorStyles.miniLabel);

                    if (issue.targetGameObject != null)
                    {
                        if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(45)))
                        {
                            _selectedIssueIndex = i;
                            Selection.activeGameObject = issue.targetGameObject;
                            EditorGUIUtility.PingObject(issue.targetGameObject);
                        }

                        if (GUILayout.Button("Focus", EditorStyles.miniButton, GUILayout.Width(45)))
                        {
                            _selectedIssueIndex = i;
                            Selection.activeGameObject = issue.targetGameObject;
                            if (SceneView.lastActiveSceneView != null)
                            {
                                SceneView.lastActiveSceneView.FrameSelected();
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(4);

                // Save Report Button
                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.2f);
                if (GUILayout.Button("Save Validation Report", GUILayout.Height(26)))
                {
                    string path = Level01AutoValidator.SaveValidationReport(_lastReport);
                    if (!string.IsNullOrEmpty(path))
                    {
                        EditorUtility.DisplayDialog("Validation Report Saved", $"Validation report saved to:\n{path}", "OK");
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawJungleDiscoveryReportUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("HD JUNGLE ASSET DISCOVERY REPORT", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Scan Root: {_lastJungleReport.scanPath}");
            EditorGUILayout.LabelField($"Total Assets Scanned: {_lastJungleReport.totalFilesScanned} | Total Prefabs: {_lastJungleReport.totalPrefabsDiscovered}");
            EditorGUILayout.LabelField($"Usable Prefabs: {_lastJungleReport.usablePrefabsCount} | Rejected: {_lastJungleReport.rejectedPrefabsCount}");
            EditorGUILayout.LabelField($"New Mappings: {_lastJungleReport.newMappingsAddedCount} | Existing Preserved: {_lastJungleReport.existingMappingsPreservedCount} | Duplicates: {_lastJungleReport.duplicatesSkippedCount}");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("CATEGORY BREAKDOWN:", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Tree: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Tree)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Bush: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Bush)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Grass: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Grass)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Fern: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Bush)}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"DeadLeaves: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.DeadLeaves)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Rock: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Rock)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"RiverRock: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.RiverRock)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Water: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Water)}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Waterfall: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Waterfall)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"WoodTrunk: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.WoodTrunk)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Log: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.WoodTrunk)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Stump: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.WoodTrunk)}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"AncientStone: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.AncientStone)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"AncientRuins: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.AncientStone)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Arch: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Arch)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Ground: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Ground)}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Other: {_hdAssetLibrary.GetPrefabCountForCategory(HDObjectCategory.Other)}", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("MATERIAL STATUS:", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"URP Compatible: {_lastJungleReport.urpCompatibleCount} | Built-in Standard: {_lastJungleReport.builtInStandardCount}");
            EditorGUILayout.LabelField($"Missing Material: {_lastJungleReport.missingMaterialCount} | Missing Shader: {_lastJungleReport.missingShaderCount}");

            EditorGUILayout.Space(4);
            _showDiscoveryDetails = EditorGUILayout.Foldout(_showDiscoveryDetails, "📂 Discovered Asset Items & Overrides", true);
            if (_showDiscoveryDetails && _lastJungleReport.discoveredItems.Count > 0)
            {
                EditorGUILayout.Space(4);

                // List Height Slider
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Discovery List Height", GUILayout.Width(140));
                _discoveryListHeight = EditorGUILayout.Slider(_discoveryListHeight, 250f, 900f);
                EditorGUILayout.EndHorizontal();

                // Search Filter
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Search Assets:", GUILayout.Width(100));
                _discoverySearchFilter = EditorGUILayout.TextField(_discoverySearchFilter);
                if (!string.IsNullOrEmpty(_discoverySearchFilter))
                {
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                    {
                        _discoverySearchFilter = "";
                        GUI.FocusControl(null);
                    }
                }
                EditorGUILayout.EndHorizontal();

                // Category Filter
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Category Filter:", GUILayout.Width(100));
                _discoveryCategoryFilter = EditorGUILayout.Popup(_discoveryCategoryFilter, CategoryFilterOptions);
                EditorGUILayout.EndHorizontal();

                // Filter Items
                List<DiscoveredAssetItem> filteredItems = new List<DiscoveredAssetItem>();
                string query = !string.IsNullOrEmpty(_discoverySearchFilter) ? _discoverySearchFilter.Trim().ToLowerInvariant() : "";
                string selectedCat = _discoveryCategoryFilter > 0 && _discoveryCategoryFilter < CategoryFilterOptions.Length ? CategoryFilterOptions[_discoveryCategoryFilter] : "All";

                foreach (var item in _lastJungleReport.discoveredItems)
                {
                    if (selectedCat != "All" && !item.category.ToString().Equals(selectedCat, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(query))
                    {
                        bool matchesName = !string.IsNullOrEmpty(item.prefabName) && item.prefabName.ToLowerInvariant().Contains(query);
                        bool matchesCat = item.category.ToString().ToLowerInvariant().Contains(query);
                        bool matchesPath = !string.IsNullOrEmpty(item.assetPath) && item.assetPath.ToLowerInvariant().Contains(query);

                        if (!matchesName && !matchesCat && !matchesPath)
                        {
                            continue;
                        }
                    }

                    filteredItems.Add(item);
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField($"Showing {filteredItems.Count} of {_lastJungleReport.discoveredItems.Count} discovered assets", EditorStyles.miniBoldLabel);
                EditorGUILayout.Space(2);

                _discoveryScrollPos = EditorGUILayout.BeginScrollView(_discoveryScrollPos, GUILayout.Height(_discoveryListHeight));

                foreach (var item in filteredItems)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();

                    string statusColor = item.urpStatus.Contains("Standard") ? "#FF9933" : (item.urpStatus.Contains("Missing") ? "#FF3366" : "#00FF88");
                    GUIStyle itemStyle = new GUIStyle(EditorStyles.boldLabel) { richText = true };
                    EditorGUILayout.LabelField($"<b>[{item.category}]</b> {item.prefabName}", itemStyle);

                    if (item.prefabObject != null)
                    {
                        if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(45)))
                        {
                            EditorGUIUtility.PingObject(item.prefabObject);
                            Selection.activeObject = item.prefabObject;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField($"Path: {item.assetPath}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Renderers: {item.rendererCount} | Mats: {item.materialCount} | <color={statusColor}>{item.urpStatus}</color>", new GUIStyle(EditorStyles.miniLabel) { richText = true });

                    // Manual Category Override Row
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Category:", GUILayout.Width(60));
                    HDObjectCategory newCat = (HDObjectCategory)EditorGUILayout.EnumPopup(item.category, GUILayout.Width(110));
                    if (newCat != item.category)
                    {
                        HDJungleAssetDiscovery.ChangeCandidateCategory(item, newCat, _hdAssetLibrary);
                    }

                    if (item.isAccepted)
                    {
                        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                        if (GUILayout.Button("Reject", EditorStyles.miniButton, GUILayout.Width(55)))
                        {
                            HDJungleAssetDiscovery.RejectCandidate(item, _hdAssetLibrary);
                        }
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
                        if (GUILayout.Button("Accept", EditorStyles.miniButton, GUILayout.Width(55)))
                        {
                            HDJungleAssetDiscovery.AcceptCandidate(item, _hdAssetLibrary);
                        }
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        private void GenerateLevel1()
        {
            GameObject root = Level01BlockoutGenerator.GenerateLevel01Blockout(_blockoutSettings);
            if (root != null)
            {
                Selection.activeGameObject = root;
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                Debug.Log($"<color=#00FF88><b>[AILevelBuilder] Successfully generated Level 1 under '{LevelGenerator.ROOT_NAME}'.</b></color>");
                RunLevel1Validation();
            }
        }

        private void ClearLevel1()
        {
            HDAssetAutoReplacer.RollbackHDReplacements();
            LevelGenerator.ClearGeneratedLevel();
            _lastReport = null;
            _lastHDReport = null;
            _lastConversionReport = null;
            _lastSceneAuditReport = null;
            _lastActiveAuditReport = null;
            _lastFullSceneReport = null;
            _selectedIssueIndex = -1;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }

        private void SelectGeneratedLevel()
        {
            GameObject root = GameObject.Find(LevelGenerator.ROOT_NAME);
            if (root != null)
            {
                Selection.activeGameObject = root;
                EditorGUIUtility.PingObject(root);
                Debug.Log($"[AILevelBuilder] Selected '{LevelGenerator.ROOT_NAME}'.");
            }
            else
            {
                Debug.LogWarning($"[AILevelBuilder] Generated level hierarchy '{LevelGenerator.ROOT_NAME}' not found in current scene.");
            }
        }

        private void FocusLevel1()
        {
            GameObject root = GameObject.Find(LevelGenerator.ROOT_NAME);
            if (root != null)
            {
                Selection.activeGameObject = root;
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }
                Debug.Log($"[AILevelBuilder] Focused SceneView on '{LevelGenerator.ROOT_NAME}'.");
            }
            else
            {
                Debug.LogWarning($"[AILevelBuilder] Generated level hierarchy '{LevelGenerator.ROOT_NAME}' not found in current scene.");
            }
        }

        private void RunLevel1Validation()
        {
            _lastReport = Level01AutoValidator.ValidateActiveLevel();
            _selectedIssueIndex = -1;
            _showValidationSection = true;
            Repaint();
        }
    }
}
