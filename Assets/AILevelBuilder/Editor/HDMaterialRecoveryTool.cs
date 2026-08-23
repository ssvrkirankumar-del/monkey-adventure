using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MonkeyAdventure.AILevelBuilder.Editor
{
    public enum MaterialSemanticCategory
    {
        Water,
        Waterfall,
        River,
        Lake,
        Foliage,
        Leaf,
        Grass,
        Bush,
        Fern,
        Bark,
        Trunk,
        Wood,
        Rock,
        Stone,
        Cliff,
        Mountain,
        Ground,
        Terrain,
        Dirt,
        Sand,
        Snow,
        Ice,
        Ancient,
        Ruin,
        Metal,
        Other
    }

    public enum CategoryFamily
    {
        Water,
        SnowIce,
        WoodBark,
        Vegetation,
        RockStone,
        GroundTerrain,
        AncientStructure,
        Metal,
        Other
    }

    /// <summary>
    /// Robust, non-destructive HD Material Recovery tool.
    /// Provides deterministic semantic scoring, strict category exclusions,
    /// deep diagnostics inspection, and resilient null-safe GUI lifecycle handling.
    /// </summary>
    public class HDMaterialRecoveryTool : EditorWindow
    {
        public const string REPORT_OUTPUT_PATH = "Assets/AILevelBuilder/Reports/HDMaterialRecoveryReport.txt";
        private const string RecoveryFolder = "Assets/AILevelBuilder/HD/URPMaterials/Recovered";

        public enum Scope
        {
            Preview,
            Active,
            Both
        }

        public enum RecoveryStatus
        {
            NoCandidate,      // 0 - 54%
            LowConfidence,    // 55 - 74%
            Review,           // 75 - 89%
            HighConfidence,   // 90 - 100%
            Recovered
        }

        [Serializable]
        public class RecoveryItem
        {
            public Renderer Renderer;
            public string TargetName;
            public int Slot;
            public Material CurrentMaterial;
            public string CurrentMaterialName;
            public Material Candidate;
            public string CandidateName;
            public string CandidateAssetPath;
            public float Confidence;
            public RecoveryStatus Status;
            public MaterialSemanticCategory Category;
            public MaterialSemanticCategory CandidateCategory;
            public string EnvironmentContext;
            public int CategoryScore;
            public int NameScore;
            public int SemanticScore;
            public int TextureScore;
            public int CompatibilityScore;
            public int FinalScore;
            public string Reason;
            public string Path;
            public bool IsExpanded = false;
            public List<MaterialScore> Alternatives = new List<MaterialScore>();
            public List<MaterialScore> RejectedAlternatives = new List<MaterialScore>();
        }

        [Serializable]
        public class MaterialScore
        {
            public Material Material;
            public MaterialSemanticCategory CandidateCategory;
            public int CategoryScore;
            public int NameScore;
            public int SemanticScore;
            public int TextureScore;
            public int CompatibilityScore;
            public int FinalScore;
            public float Confidence;
            public string Explanation;
            public bool IsRejected;
            public string RejectionReason;
        }

        private Vector2 _listScroll;
        private Vector2 _detailScroll;

        private Scope _scope = Scope.Both;
        private bool _onlyVisualRisk = true;
        private bool _includeAlreadyTextured = false;
        private bool _showHighConfidenceOnly = false;
        private int _filterTab = 0; // 0=All, 1=High, 2=Review, 3=Low, 4=NoCandidate

        private string _search = "";
        private List<RecoveryItem> _items = new List<RecoveryItem>();
        private List<Material> _projectMaterials = new List<Material>();
        private RecoveryItem _selected;
        private int _selectedIndex = -1;

        private int _highConfidence;
        private int _review;
        private int _lowConfidence;
        private int _noCandidate;

        private string _clipboardStatusNotice = "";
        private double _clipboardNoticeExpireTime = 0;

        [MenuItem("Window/Monkey Adventure/HD Material Recovery Tool")]
        public static void OpenWindow()
        {
            var window = GetWindow<HDMaterialRecoveryTool>("HD Material Recovery");
            window.minSize = new Vector2(1100f, 680f);
            window.position = new Rect(window.position.x, window.position.y, 1250f, 820f);
            window.Show();
        }

        [MenuItem("Window/Monkey Adventure/HD Material Recovery Tool/Scan Preview")]
        public static void ScanPreviewMenu()
        {
            var window = GetWindow<HDMaterialRecoveryTool>("HD Material Recovery");
            window._scope = Scope.Preview;
            window.Scan(true);
        }

        private void OnEnable()
        {
            ValidateSelection();
        }

        private void ValidateSelection()
        {
            if (_items == null || _items.Count == 0)
            {
                _selected = null;
                _selectedIndex = -1;
                return;
            }

            if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
            {
                _selected = _items[_selectedIndex];
            }
            else if (_selected != null)
            {
                _selectedIndex = _items.IndexOf(_selected);
                if (_selectedIndex < 0)
                {
                    _selected = _items[0];
                    _selectedIndex = 0;
                }
            }
            else
            {
                _selected = _items[0];
                _selectedIndex = 0;
            }
        }

        private void OnGUI()
        {
            ValidateSelection();

            DrawHeader();
            DrawToolbar();

            if (_items == null || _items.Count == 0)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox(
                    "No recovery candidates are loaded. Click '🔍 Scan' to inspect the selected HD environment scope.",
                    MessageType.Info);
                return;
            }

            DrawSummary();

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            try
            {
                DrawListPanel();
                DrawDetailPanel();
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            try
            {
                EditorGUILayout.LabelField("🎨 HD MATERIAL RECOVERY TOOL", EditorStyles.boldLabel);

                if (EditorApplication.timeSinceStartup < _clipboardNoticeExpireTime && !string.IsNullOrEmpty(_clipboardStatusNotice))
                {
                    GUILayout.FlexibleSpace();
                    GUIStyle noticeStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.2f, 0.9f, 0.4f) } };
                    EditorGUILayout.LabelField(_clipboardStatusNotice, noticeStyle, GUILayout.Width(260));
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.HelpBox(
                "Deterministic visual recovery tool for HD environment materials with strict category exclusions.\n" +
                "Proposes high-confidence visual material candidates from project assets without modifying source files.\n" +
                "Never changes scene renderer slots automatically; requires explicit user action.",
                MessageType.Info);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical("box");
            try
            {
                // Row 1: Scope & Filters
                EditorGUILayout.BeginHorizontal();
                try
                {
                    _scope = (Scope)EditorGUILayout.EnumPopup("Scope", _scope, GUILayout.Width(220));

                    _onlyVisualRisk = EditorGUILayout.ToggleLeft(
                        "Only missing/white BaseMap risk",
                        _onlyVisualRisk,
                        GUILayout.Width(220));

                    _includeAlreadyTextured = EditorGUILayout.ToggleLeft(
                        "Include textured slots",
                        _includeAlreadyTextured,
                        GUILayout.Width(170));

                    _showHighConfidenceOnly = EditorGUILayout.ToggleLeft(
                        "High confidence only (≥90%)",
                        _showHighConfidenceOnly,
                        GUILayout.Width(190));
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(2);

                // Row 2: Search, Scan & Clear
                EditorGUILayout.BeginHorizontal();
                try
                {
                    EditorGUILayout.LabelField("🔍 Filter:", GUILayout.Width(55));
                    _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                    if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(45)))
                    {
                        _search = "";
                        GUI.FocusControl(null);
                    }

                    GUI.backgroundColor = new Color(0.2f, 0.7f, 1.0f);
                    if (GUILayout.Button("🔍 Scan", GUILayout.Height(26), GUILayout.Width(90)))
                        Scan(true);

                    GUI.backgroundColor = Color.white;
                    if (GUILayout.Button("Reset", GUILayout.Height(26), GUILayout.Width(65)))
                    {
                        _items.Clear();
                        _selected = null;
                        _selectedIndex = -1;
                        Repaint();
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(2);

                // Row 3: Apply & Export Actions
                EditorGUILayout.BeginHorizontal();
                try
                {
                    bool hasHigh = _items.Any(i => i != null && i.Status == RecoveryStatus.HighConfidence && i.Candidate != null && i.Candidate);
                    GUI.backgroundColor = hasHigh ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
                    EditorGUI.BeginDisabledGroup(!hasHigh);
                    if (GUILayout.Button("⚡ Apply High Confidence (≥90%)", GUILayout.Height(26)))
                        ApplyHighConfidence();
                    EditorGUI.EndDisabledGroup();

                    bool canApplySelected = _selected != null && _selected.Renderer != null && _selected.Renderer &&
                                            _selected.Candidate != null && _selected.Candidate &&
                                            _selected.Status != RecoveryStatus.NoCandidate;
                    GUI.backgroundColor = canApplySelected ? new Color(0.9f, 0.6f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
                    EditorGUI.BeginDisabledGroup(!canApplySelected);
                    if (GUILayout.Button("🎯 Apply Selected Candidate", GUILayout.Height(26)))
                        ApplySelected();
                    EditorGUI.EndDisabledGroup();

                    GUI.backgroundColor = Color.white;
                    if (GUILayout.Button("📄 Export Recovery Report", GUILayout.Height(26)))
                        ExportReport();
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(2);

                // Row 4: Clipboard & Bulk Expand Controls
                EditorGUILayout.BeginHorizontal();
                try
                {
                    if (GUILayout.Button("📋 Copy All Recovery Items", EditorStyles.miniButton, GUILayout.Width(160)))
                    {
                        CopyItemsToClipboard(_items, "All Recovery Items");
                    }
                    if (GUILayout.Button("📋 Copy High", EditorStyles.miniButton, GUILayout.Width(90)))
                    {
                        CopyItemsToClipboard(_items.Where(i => i != null && i.Status == RecoveryStatus.HighConfidence).ToList(), "High Confidence Items");
                    }
                    if (GUILayout.Button("📋 Copy Review", EditorStyles.miniButton, GUILayout.Width(95)))
                    {
                        CopyItemsToClipboard(_items.Where(i => i != null && i.Status == RecoveryStatus.Review).ToList(), "Review Items");
                    }
                    if (GUILayout.Button("📋 Copy Low", EditorStyles.miniButton, GUILayout.Width(85)))
                    {
                        CopyItemsToClipboard(_items.Where(i => i != null && i.Status == RecoveryStatus.LowConfidence).ToList(), "Low Confidence Items");
                    }
                    if (GUILayout.Button("📋 Copy No Candidate", EditorStyles.miniButton, GUILayout.Width(130)))
                    {
                        CopyItemsToClipboard(_items.Where(i => i != null && i.Status == RecoveryStatus.NoCandidate).ToList(), "No Candidate Items");
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Expand All", EditorStyles.miniButton, GUILayout.Width(75)))
                    {
                        foreach (var it in _items) if (it != null) it.IsExpanded = true;
                    }
                    if (GUILayout.Button("Collapse All", EditorStyles.miniButton, GUILayout.Width(75)))
                    {
                        foreach (var it in _items) if (it != null) it.IsExpanded = false;
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawSummary()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            try
            {
                EditorGUILayout.BeginHorizontal();
                try
                {
                    EditorGUILayout.LabelField($"Total Scanned Slots: <b>{_items.Count}</b>", new GUIStyle(EditorStyles.label) { richText = true });
                    EditorGUILayout.LabelField($"High (90-100%): <color=#00FF88><b>{_highConfidence}</b></color>", new GUIStyle(EditorStyles.label) { richText = true });
                    EditorGUILayout.LabelField($"Review (75-89%): <color=#FFCC00><b>{_review}</b></color>", new GUIStyle(EditorStyles.label) { richText = true });
                    EditorGUILayout.LabelField($"Low (55-74%): <color=#FF8800><b>{_lowConfidence}</b></color>", new GUIStyle(EditorStyles.label) { richText = true });
                    EditorGUILayout.LabelField($"No Candidate (0-54%): <color=#FF3366><b>{_noCandidate}</b></color>", new GUIStyle(EditorStyles.label) { richText = true });
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }

                // Status Tabs
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                try
                {
                    if (GUILayout.Toggle(_filterTab == 0, $"All ({_items.Count})", EditorStyles.toolbarButton)) _filterTab = 0;
                    if (GUILayout.Toggle(_filterTab == 1, $"High ({_highConfidence})", EditorStyles.toolbarButton)) _filterTab = 1;
                    if (GUILayout.Toggle(_filterTab == 2, $"Review ({_review})", EditorStyles.toolbarButton)) _filterTab = 2;
                    if (GUILayout.Toggle(_filterTab == 3, $"Low ({_lowConfidence})", EditorStyles.toolbarButton)) _filterTab = 3;
                    if (GUILayout.Toggle(_filterTab == 4, $"No Candidate ({_noCandidate})", EditorStyles.toolbarButton)) _filterTab = 4;
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawListPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.52f));
            try
            {
                EditorGUILayout.LabelField("RECOVERY QUEUE", EditorStyles.boldLabel);

                _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.ExpandHeight(true));
                try
                {
                    var filtered = FilterItems().ToList();

                    if (filtered.Count == 0)
                    {
                        EditorGUILayout.Space(20);
                        EditorGUILayout.HelpBox("No items match the current tab filter and search query.", MessageType.Info);
                    }
                    else
                    {
                        for (int i = 0; i < filtered.Count; i++)
                        {
                            var item = filtered[i];
                            if (item == null) continue;

                            bool selected = ReferenceEquals(item, _selected) || (_selectedIndex >= 0 && _selectedIndex < _items.Count && ReferenceEquals(item, _items[_selectedIndex]));

                            GUI.backgroundColor = selected ? new Color(0.7f, 0.9f, 1.0f) : Color.white;
                            EditorGUILayout.BeginVertical("box");
                            GUI.backgroundColor = Color.white;
                            try
                            {
                                // Header Row
                                EditorGUILayout.BeginHorizontal();
                                try
                                {
                                    item.IsExpanded = EditorGUILayout.Foldout(item.IsExpanded, GUIContent.none, true);

                                    string statusColorHex = GetStatusColorHex(item.Status);
                                    bool isAlive = item.Renderer != null && item.Renderer;
                                    string targetName = isAlive ? item.Renderer.name : (!string.IsNullOrEmpty(item.TargetName) ? item.TargetName : "Unknown Target");

                                    string headerLabel = $"<color={statusColorHex}><b>{StatusLabel(item.Status)}</b></color> <b>{targetName}</b> [Slot {item.Slot}]";
                                    GUIStyle titleStyle = new GUIStyle(EditorStyles.label) { richText = true };
                                    if (GUILayout.Button(headerLabel, titleStyle, GUILayout.ExpandWidth(true)))
                                    {
                                        _selected = item;
                                        _selectedIndex = _items.IndexOf(item);
                                        if (isAlive && item.Renderer.gameObject != null) Selection.activeGameObject = item.Renderer.gameObject;
                                    }

                                    if (GUILayout.Button("Inspect", EditorStyles.miniButton, GUILayout.Width(55)))
                                    {
                                        _selected = item;
                                        _selectedIndex = _items.IndexOf(item);
                                    }

                                    if (isAlive)
                                    {
                                        if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                                        {
                                            _selected = item;
                                            _selectedIndex = _items.IndexOf(item);
                                            if (item.Renderer.gameObject != null)
                                            {
                                                Selection.activeGameObject = item.Renderer.gameObject;
                                                EditorGUIUtility.PingObject(item.Renderer.gameObject);
                                            }
                                        }
                                        if (GUILayout.Button("Focus", EditorStyles.miniButton, GUILayout.Width(48)))
                                        {
                                            _selected = item;
                                            _selectedIndex = _items.IndexOf(item);
                                            if (item.Renderer.gameObject != null)
                                            {
                                                Selection.activeGameObject = item.Renderer.gameObject;
                                                SceneView.lastActiveSceneView?.Frame(item.Renderer.bounds, false);
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    EditorGUILayout.EndHorizontal();
                                }

                                // Summary Line
                                EditorGUILayout.BeginHorizontal();
                                try
                                {
                                    EditorGUILayout.LabelField($"Category: <b>{item.Category}</b> | Context: <i>{item.EnvironmentContext}</i> | Confidence: <b>{item.Confidence:0}%</b>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                                }
                                finally
                                {
                                    EditorGUILayout.EndHorizontal();
                                }

                                string currentMatName = (item.CurrentMaterial != null && item.CurrentMaterial) ? item.CurrentMaterial.name : (!string.IsNullOrEmpty(item.CurrentMaterialName) ? item.CurrentMaterialName : "<Missing>");
                                string candidateMatName = (item.Candidate != null && item.Candidate) ? item.Candidate.name : (!string.IsNullOrEmpty(item.CandidateName) ? item.CandidateName : "<NO SAFE CANDIDATE>");

                                EditorGUILayout.LabelField(
                                    $"Current: <b>{currentMatName}</b> -> Candidate: <b>{candidateMatName}</b>",
                                    new GUIStyle(EditorStyles.miniLabel) { richText = true });

                                if (item.IsExpanded)
                                {
                                    EditorGUILayout.Space(2);
                                    EditorGUILayout.LabelField("Hierarchy Path:", !string.IsNullOrEmpty(item.Path) ? item.Path : "<None>", EditorStyles.miniLabel);

                                    if (!string.IsNullOrEmpty(item.Reason))
                                        EditorGUILayout.HelpBox(item.Reason, item.Status == RecoveryStatus.HighConfidence ? MessageType.Info :
                                                                             item.Status == RecoveryStatus.Review ? MessageType.Warning :
                                                                             item.Status == RecoveryStatus.LowConfidence ? MessageType.Warning : MessageType.Error);

                                    EditorGUILayout.BeginHorizontal();
                                    try
                                    {
                                        if (GUILayout.Button("📋 Copy Details", EditorStyles.miniButton, GUILayout.Width(110)))
                                        {
                                            GUIUtility.systemCopyBuffer = FormatRecoveryItemDetails(item);
                                            ShowClipboardNotice($"Copied details for slot {item.Slot}!");
                                        }

                                        if (item.Candidate != null && item.Candidate && GUILayout.Button("Ping Candidate", EditorStyles.miniButton, GUILayout.Width(110)))
                                        {
                                            EditorGUIUtility.PingObject(item.Candidate);
                                        }
                                    }
                                    finally
                                    {
                                        EditorGUILayout.EndHorizontal();
                                    }
                                }
                            }
                            finally
                            {
                                EditorGUILayout.EndVertical();
                            }
                            EditorGUILayout.Space(1);
                        }
                    }
                }
                finally
                {
                    EditorGUILayout.EndScrollView();
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
            try
            {
                EditorGUILayout.LabelField("🔍 RECOVERY DEEP INSPECTOR", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                if (_selected == null)
                {
                    EditorGUILayout.HelpBox(
                        "No recovery item selected.\n\nSelect an item from the recovery queue on the left to inspect its target, classification, and candidates.",
                        MessageType.Info);
                    return;
                }

                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll, GUILayout.ExpandHeight(true));
                try
                {
                    DrawDetailHeader(_selected);
                    EditorGUILayout.Space(4);
                    DrawDetailActionButtons(_selected);
                    EditorGUILayout.Space(6);
                    DrawScoreBreakdownSection(_selected);
                    EditorGUILayout.Space(6);
                    DrawObjectSection(_selected);
                    EditorGUILayout.Space(6);
                    DrawCurrentMaterialSection(_selected);
                    EditorGUILayout.Space(6);
                    DrawCandidateSection(_selected);
                    EditorGUILayout.Space(6);
                    DrawAlternativesSection(_selected);
                    EditorGUILayout.Space(6);
                    DrawRejectedSection(_selected);
                }
                finally
                {
                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.Space(4);
                DrawBottomApplyBar(_selected);
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawDetailHeader(RecoveryItem item)
        {
            string statusColorHex = GetStatusColorHex(item.Status);
            bool hasTarget = item.Renderer != null && item.Renderer;
            string targetName = hasTarget ? item.Renderer.name : (!string.IsNullOrEmpty(item.TargetName) ? item.TargetName : "Unknown Target");

            EditorGUILayout.BeginVertical("box");
            try
            {
                EditorGUILayout.LabelField(
                    $"<color={statusColorHex}><b>{StatusLabel(item.Status)}  -  {item.Confidence:0}% CONFIDENCE</b></color>",
                    new GUIStyle(EditorStyles.boldLabel) { richText = true, fontSize = 13 });
                EditorGUILayout.LabelField($"Target: <b>{targetName}</b> (Slot {item.Slot})", new GUIStyle(EditorStyles.label) { richText = true });

                if (!hasTarget)
                {
                    EditorGUILayout.HelpBox("Target object is no longer available in the scene (may have been deleted or scene reloaded).", MessageType.Warning);
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawDetailActionButtons(RecoveryItem item)
        {
            bool hasTarget = item.Renderer != null && item.Renderer;
            string targetName = hasTarget ? item.Renderer.name : (!string.IsNullOrEmpty(item.TargetName) ? item.TargetName : "Unknown Target");

            EditorGUILayout.BeginHorizontal();
            try
            {
                if (GUILayout.Button("📋 Copy Item Details", GUILayout.Height(24)))
                {
                    GUIUtility.systemCopyBuffer = FormatRecoveryItemDetails(item);
                    ShowClipboardNotice($"Copied {targetName} Details!");
                }

                bool hasCandidate = item.Candidate != null && item.Candidate;
                EditorGUI.BeginDisabledGroup(!hasCandidate);
                if (GUILayout.Button("Ping Candidate Asset", GUILayout.Height(24)))
                {
                    if (hasCandidate)
                        EditorGUIUtility.PingObject(item.Candidate);
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!hasTarget);
                if (GUILayout.Button("Select Scene Target", GUILayout.Height(24)))
                {
                    if (hasTarget && item.Renderer.gameObject != null)
                    {
                        Selection.activeGameObject = item.Renderer.gameObject;
                        EditorGUIUtility.PingObject(item.Renderer.gameObject);
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawScoreBreakdownSection(RecoveryItem item)
        {
            EditorGUILayout.LabelField("Semantic Score Breakdown (Deterministic):", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            try
            {
                EditorGUILayout.LabelField($"Target Category: <b>{item.Category}</b> | Environment Context: <b>{item.EnvironmentContext}</b>", new GUIStyle(EditorStyles.label) { richText = true });
                EditorGUILayout.LabelField($"Candidate Category: <b>{item.CandidateCategory}</b>", new GUIStyle(EditorStyles.label) { richText = true });
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField($"Category Score: <b>{item.CategoryScore} / 30</b>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                EditorGUILayout.LabelField($"Name Score: <b>{item.NameScore} / 20</b>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                EditorGUILayout.LabelField($"Semantic Score: <b>{item.SemanticScore} / 30</b>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                EditorGUILayout.LabelField($"Texture Score: <b>{item.TextureScore} / 20</b>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                EditorGUILayout.LabelField($"Compatibility Score: <b>{item.CompatibilityScore} / 20</b>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField($"Final Score: <b>{item.FinalScore} / 100</b>  ->  Confidence: <b>{item.Confidence:0}%</b>", new GUIStyle(EditorStyles.boldLabel) { richText = true });
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawObjectSection(RecoveryItem item)
        {
            EditorGUILayout.LabelField("Target Object Hierarchy:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            try
            {
                bool hasTarget = item.Renderer != null && item.Renderer;
                if (hasTarget)
                {
                    EditorGUILayout.ObjectField("Renderer", item.Renderer, typeof(Renderer), true);
                }
                else
                {
                    EditorGUILayout.HelpBox("Target object is no longer available in the active scene.", MessageType.Warning);
                    EditorGUILayout.LabelField("Saved Target Name", !string.IsNullOrEmpty(item.TargetName) ? item.TargetName : "Unknown");
                }

                EditorGUILayout.TextField("Hierarchy Path", !string.IsNullOrEmpty(item.Path) ? item.Path : "<None>");
                EditorGUILayout.TextField("Target Category", item.Category.ToString());
                EditorGUILayout.TextField("Environment Context", !string.IsNullOrEmpty(item.EnvironmentContext) ? item.EnvironmentContext : "<None>");
                EditorGUILayout.IntField("Material Slot", item.Slot);

                if (hasTarget)
                {
                    Material[] shared = item.Renderer.sharedMaterials;
                    if (shared == null || item.Slot < 0 || item.Slot >= shared.Length)
                    {
                        EditorGUILayout.HelpBox($"Renderer currently has {shared?.Length ?? 0} material slot(s). Slot {item.Slot} is out of range.", MessageType.Warning);
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawCurrentMaterialSection(RecoveryItem item)
        {
            EditorGUILayout.LabelField("Current Slot Material:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            try
            {
                bool hasMat = item.CurrentMaterial != null && item.CurrentMaterial;
                if (hasMat)
                {
                    EditorGUILayout.ObjectField("Material Asset", item.CurrentMaterial, typeof(Material), false);
                    EditorGUILayout.TextField("Material Name", item.CurrentMaterial.name);
                    EditorGUILayout.TextField("Shader", item.CurrentMaterial.shader != null ? item.CurrentMaterial.shader.name : "<Missing Shader>");
                    EditorGUILayout.TextField("BaseMap Texture", DescribeTexture(item.CurrentMaterial, "_BaseMap", "_MainTex"));
                    EditorGUILayout.TextField("BaseColor Hex", DescribeColor(item.CurrentMaterial, "_BaseColor", "_Color"));
                }
                else
                {
                    EditorGUILayout.HelpBox("Current material unavailable (null or missing).", MessageType.Info);
                    if (!string.IsNullOrEmpty(item.CurrentMaterialName))
                    {
                        EditorGUILayout.TextField("Saved Material Name", item.CurrentMaterialName);
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawCandidateSection(RecoveryItem item)
        {
            EditorGUILayout.LabelField("Proposed Recovery Candidate:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            try
            {
                bool hasCandidate = item.Candidate != null && item.Candidate;
                if (hasCandidate)
                {
                    EditorGUILayout.ObjectField("Candidate Material", item.Candidate, typeof(Material), false);
                    EditorGUILayout.TextField("Candidate Category", item.CandidateCategory.ToString());
                    EditorGUILayout.TextField("Candidate Shader", item.Candidate.shader != null ? item.Candidate.shader.name : "<Missing Shader>");
                    EditorGUILayout.TextField("BaseMap Texture", DescribeTexture(item.Candidate, "_BaseMap", "_MainTex"));
                    EditorGUILayout.TextField("BaseColor Hex", DescribeColor(item.Candidate, "_BaseColor", "_Color"));

                    string assetPath = !string.IsNullOrEmpty(item.CandidateAssetPath) ? item.CandidateAssetPath : AssetDatabase.GetAssetPath(item.Candidate);
                    EditorGUILayout.TextField("Source Asset Path", !string.IsNullOrEmpty(assetPath) ? assetPath : "<Unknown>");

                    if (!string.IsNullOrEmpty(item.Reason))
                    {
                        EditorGUILayout.HelpBox(
                            item.Reason,
                            item.Status == RecoveryStatus.HighConfidence ? MessageType.Info :
                            item.Status == RecoveryStatus.Review ? MessageType.Warning : MessageType.Error);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "NO SAFE CANDIDATE FOUND (Score < 55% or Category Conflict).\nNo unrelated materials will be suggested.",
                        MessageType.Warning);

                    if (!string.IsNullOrEmpty(item.Reason))
                    {
                        EditorGUILayout.HelpBox($"Rejection/Evaluation Reason:\n{item.Reason}", MessageType.None);
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawAlternativesSection(RecoveryItem item)
        {
            EditorGUILayout.LabelField("Best Ranked Candidates (Max 3):", EditorStyles.boldLabel);

            var validAlternatives = (item.Alternatives ?? new List<MaterialScore>())
                .Where(a => a != null && a.Material != null && a.Material)
                .Take(3)
                .ToList();

            if (validAlternatives.Count == 0)
            {
                EditorGUILayout.HelpBox("No additional safe candidates reached the threshold (≥55%) for this slot.", MessageType.None);
                return;
            }

            foreach (var alternative in validAlternatives)
            {
                EditorGUILayout.BeginVertical("box");
                try
                {
                    EditorGUILayout.LabelField(
                        $"{alternative.Material.name}  |  Confidence: {alternative.Confidence:0}% (Score: {alternative.FinalScore})",
                        EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Category: {alternative.CandidateCategory} | {alternative.Explanation}", EditorStyles.wordWrappedMiniLabel);

                    if (GUILayout.Button("Use This Candidate", EditorStyles.miniButton))
                    {
                        item.Candidate = alternative.Material;
                        item.CandidateName = alternative.Material.name;
                        item.CandidateAssetPath = AssetDatabase.GetAssetPath(alternative.Material);
                        item.CandidateCategory = alternative.CandidateCategory;
                        item.CategoryScore = alternative.CategoryScore;
                        item.NameScore = alternative.NameScore;
                        item.SemanticScore = alternative.SemanticScore;
                        item.TextureScore = alternative.TextureScore;
                        item.CompatibilityScore = alternative.CompatibilityScore;
                        item.FinalScore = alternative.FinalScore;
                        item.Confidence = alternative.Confidence;
                        item.Status = ClassifyStatus(item.Confidence);
                        item.Reason = BuildReason(item);
                        RecalculateSummary();
                        Repaint();
                    }
                }
                finally
                {
                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void DrawRejectedSection(RecoveryItem item)
        {
            var rejected = (item.RejectedAlternatives ?? new List<MaterialScore>())
                .Where(r => r != null && r.Material != null && r.Material)
                .Take(5)
                .ToList();

            if (rejected.Count == 0)
                return;

            EditorGUILayout.LabelField("Rejected Candidates (Category Conflicts / Low Score):", EditorStyles.boldLabel);

            foreach (var rej in rejected)
            {
                EditorGUILayout.BeginVertical("box");
                try
                {
                    EditorGUILayout.LabelField($"<color=#FF3366><b>[REJECTED] {rej.Material.name}</b></color> ({rej.CandidateCategory})", new GUIStyle(EditorStyles.boldLabel) { richText = true });
                    EditorGUILayout.LabelField($"Reason: {rej.RejectionReason}", EditorStyles.wordWrappedMiniLabel);
                }
                finally
                {
                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void DrawBottomApplyBar(RecoveryItem item)
        {
            EditorGUILayout.BeginHorizontal();
            try
            {
                bool hasTarget = item.Renderer != null && item.Renderer;
                bool hasCandidate = item.Candidate != null && item.Candidate;
                bool canApply = hasTarget && hasCandidate && item.Status != RecoveryStatus.NoCandidate;

                GUI.backgroundColor = canApply ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
                EditorGUI.BeginDisabledGroup(!canApply);
                if (GUILayout.Button("Apply Selected Candidate", GUILayout.Height(30)))
                {
                    ApplySelected();
                }
                EditorGUI.EndDisabledGroup();
                GUI.backgroundColor = Color.white;
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private IEnumerable<RecoveryItem> FilterItems()
        {
            if (_items == null) return Enumerable.Empty<RecoveryItem>();

            IEnumerable<RecoveryItem> query = _items.Where(i => i != null);

            switch (_filterTab)
            {
                case 1: query = query.Where(i => i.Status == RecoveryStatus.HighConfidence); break;
                case 2: query = query.Where(i => i.Status == RecoveryStatus.Review); break;
                case 3: query = query.Where(i => i.Status == RecoveryStatus.LowConfidence); break;
                case 4: query = query.Where(i => i.Status == RecoveryStatus.NoCandidate); break;
            }

            if (_showHighConfidenceOnly)
                query = query.Where(i => i.Status == RecoveryStatus.HighConfidence);

            if (!string.IsNullOrWhiteSpace(_search))
            {
                string q = _search.ToLowerInvariant();
                query = query.Where(i =>
                    (
                        (i.Renderer != null && i.Renderer && i.Renderer.name.ToLowerInvariant().Contains(q)) ||
                        (!string.IsNullOrEmpty(i.TargetName) && i.TargetName.ToLowerInvariant().Contains(q)) ||
                        (!string.IsNullOrEmpty(i.Path) && i.Path.ToLowerInvariant().Contains(q)) ||
                        i.Category.ToString().ToLowerInvariant().Contains(q) ||
                        (i.EnvironmentContext != null && i.EnvironmentContext.ToLowerInvariant().Contains(q)) ||
                        (i.CurrentMaterial != null && i.CurrentMaterial && i.CurrentMaterial.name.ToLowerInvariant().Contains(q)) ||
                        (!string.IsNullOrEmpty(i.CurrentMaterialName) && i.CurrentMaterialName.ToLowerInvariant().Contains(q)) ||
                        (i.Candidate != null && i.Candidate && i.Candidate.name.ToLowerInvariant().Contains(q)) ||
                        (!string.IsNullOrEmpty(i.CandidateName) && i.CandidateName.ToLowerInvariant().Contains(q)) ||
                        (!string.IsNullOrEmpty(i.Reason) && i.Reason.ToLowerInvariant().Contains(q))
                    ));
            }

            return query;
        }

        public void Scan(bool showCompletionDialog = false)
        {
            _items.Clear();
            _selected = null;
            _selectedIndex = -1;
            _projectMaterials.Clear();

            BuildMaterialDatabase();

            var roots = ResolveTargetRoots();

            if (roots.Count == 0)
            {
                if (showCompletionDialog)
                {
                    EditorUtility.DisplayDialog(
                        "HD Material Recovery",
                        "No requested HD environment root (e.g. AI_GENERATED_LEVEL/HD_REPLACEMENTS or HD_ENVIRONMENT) was found in the active scene.",
                        "OK");
                }
                else
                {
                    Debug.LogWarning("[HDMaterialRecoveryTool] No HD environment roots found in active scene.");
                }
                return;
            }

            int rendererCount = 0;
            try
            {
                for (int rIdx = 0; rIdx < roots.Count; rIdx++)
                {
                    var root = roots[rIdx];
                    if (root == null) continue;

                    EditorUtility.DisplayProgressBar(
                        "Scanning HD Materials",
                        $"Indexing hierarchy: {root.name} ({rIdx + 1}/{roots.Count})...",
                        (float)rIdx / roots.Count);

                    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer is ParticleSystemRenderer)
                            continue;

                        rendererCount++;

                        var materials = renderer.sharedMaterials;
                        if (materials == null) continue;

                        for (int slot = 0; slot < materials.Length; slot++)
                        {
                            var current = materials[slot];

                            if (!_includeAlreadyTextured && !IsVisualRisk(current))
                                continue;

                            if (_onlyVisualRisk && !IsVisualRisk(current))
                                continue;

                            var item = BuildRecoveryItem(renderer, slot, current);
                            _items.Add(item);
                        }
                    }
                }

                RecalculateSummary();
                ValidateSelection();

                ExportReport();

                Debug.Log($"<color=#00FF88><b>[HDMaterialRecoveryTool] Scan Complete. Scanned Renderers: {rendererCount}, Recovery Slots: {_items.Count} (High: {_highConfidence}, Review: {_review}, Low: {_lowConfidence}, No Candidate: {_noCandidate}). No scene changes applied.</b></color>");

                if (showCompletionDialog)
                {
                    EditorUtility.DisplayDialog(
                        "HD Material Recovery",
                        $"Scan complete.\n\nRenderers scanned: {rendererCount}\nRecovery slots: {_items.Count}\n\nHigh (≥90%): {_highConfidence}\nReview (75-89%): {_review}\nLow (55-74%): {_lowConfidence}\nNo Candidate: {_noCandidate}\n\nNo material changes were made.",
                        "OK");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private List<Transform> ResolveTargetRoots()
        {
            string resolvedPath;
            var allRoots = HDEnvironmentVisualMaterialAudit.FindEnvironmentRoots(out resolvedPath);

            if (_scope == Scope.Both)
                return allRoots;

            var filtered = new List<Transform>();
            foreach (var r in allRoots)
            {
                if (r == null) continue;
                string path = GetHierarchyPath(r);
                if (_scope == Scope.Preview && path.IndexOf("PREVIEW", StringComparison.OrdinalIgnoreCase) >= 0)
                    filtered.Add(r);
                else if (_scope == Scope.Active && path.IndexOf("PREVIEW", StringComparison.OrdinalIgnoreCase) < 0)
                    filtered.Add(r);
            }

            return filtered.Count > 0 ? filtered : allRoots;
        }

        private void BuildMaterialDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material");

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material != null)
                    _projectMaterials.Add(material);
            }
        }

        private RecoveryItem BuildRecoveryItem(Renderer renderer, int slot, Material current)
        {
            bool isAlive = renderer != null && renderer;
            string hierPath = isAlive ? GetHierarchyPath(renderer.transform) : "";
            string envContext = ExtractEnvironmentContext(hierPath);
            var targetCategory = ClassifyTargetCategory(renderer, slot, current, hierPath);

            var item = new RecoveryItem
            {
                Renderer = renderer,
                TargetName = isAlive ? renderer.name : "Unknown",
                Slot = slot,
                CurrentMaterial = current,
                CurrentMaterialName = (current != null && current) ? current.name : "<Missing>",
                Path = hierPath,
                EnvironmentContext = envContext,
                Category = targetCategory
            };

            var scoredList = RankCandidates(item);

            var validCandidates = scoredList.Where(s => !s.IsRejected && s.FinalScore >= 55).ToList();
            var rejectedList = scoredList.Where(s => s.IsRejected || s.FinalScore < 55).ToList();

            item.Alternatives = validCandidates.Take(3).ToList();
            item.RejectedAlternatives = rejectedList.Take(6).ToList();

            if (validCandidates.Count > 0)
            {
                var best = validCandidates[0];
                item.Candidate = best.Material;
                item.CandidateName = best.Material != null ? best.Material.name : "<Unknown>";
                item.CandidateAssetPath = best.Material != null ? AssetDatabase.GetAssetPath(best.Material) : "";
                item.CandidateCategory = best.CandidateCategory;
                item.CategoryScore = best.CategoryScore;
                item.NameScore = best.NameScore;
                item.SemanticScore = best.SemanticScore;
                item.TextureScore = best.TextureScore;
                item.CompatibilityScore = best.CompatibilityScore;
                item.FinalScore = best.FinalScore;
                item.Confidence = best.Confidence;
                item.Status = ClassifyStatus(item.Confidence);
            }
            else
            {
                item.Candidate = null;
                item.CandidateName = "<NO SAFE CANDIDATE>";
                item.CandidateAssetPath = "";
                item.CandidateCategory = MaterialSemanticCategory.Other;
                item.CategoryScore = 0;
                item.NameScore = 0;
                item.SemanticScore = 0;
                item.TextureScore = 0;
                item.CompatibilityScore = 0;
                item.Status = RecoveryStatus.NoCandidate;
                item.Confidence = 0f;
                item.FinalScore = 0;
            }

            item.Reason = BuildReason(item);
            return item;
        }

        private List<MaterialScore> RankCandidates(RecoveryItem item)
        {
            var ranked = new List<MaterialScore>();

            string targetText = BuildTargetText(item);
            string normalizedCurrent = NormalizeMaterialName((item.CurrentMaterial != null && item.CurrentMaterial) ? item.CurrentMaterial.name : (!string.IsNullOrEmpty(item.CurrentMaterialName) ? item.CurrentMaterialName : ""));

            foreach (var candidate in _projectMaterials)
            {
                if (candidate == null || candidate == item.CurrentMaterial)
                    continue;

                string path = AssetDatabase.GetAssetPath(candidate);

                if (path.IndexOf("/URPMaterials/Recovered/", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                if (candidate.shader == null)
                    continue;

                if (candidate.shader.name.IndexOf("Hidden/", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                // Inspect candidate material category
                MaterialSemanticCategory candCat = ClassifyCandidateCategory(candidate);
                string candFullText = $"{candidate.name} {path} {candidate.shader.name} {DescribeTexture(candidate, "_BaseMap", "_MainTex")}";

                var scoreObj = new MaterialScore
                {
                    Material = candidate,
                    CandidateCategory = candCat
                };

                // Check Hard Category Exclusions
                string conflictReason;
                if (IsHardCategoryExclusion(item.Category, candCat, targetText, candFullText, out conflictReason))
                {
                    scoreObj.IsRejected = true;
                    scoreObj.RejectionReason = conflictReason;
                    scoreObj.CategoryScore = 0;
                    scoreObj.NameScore = 0;
                    scoreObj.SemanticScore = 0;
                    scoreObj.TextureScore = 0;
                    scoreObj.CompatibilityScore = 0;
                    scoreObj.FinalScore = 0;
                    scoreObj.Confidence = 0f;
                    scoreObj.Explanation = $"REJECTED: {conflictReason}";
                    ranked.Add(scoreObj);
                    continue;
                }

                var reasons = new List<string>();

                // 1. Category Score (0 - 30 pts)
                int catScore = ComputeCategoryScore(item.Category, candCat, targetText, candFullText, reasons);
                scoreObj.CategoryScore = catScore;

                // 2. Name Score (0 - 20 pts)
                int nameScore = ComputeNameScore(targetText, candidate, normalizedCurrent, reasons);
                scoreObj.NameScore = nameScore;

                // 3. Semantic Score (0 - 30 pts)
                int semanticScore = ComputeSemanticScore(item.EnvironmentContext, item.Path, candidate, item.Category, candCat, reasons);
                scoreObj.SemanticScore = semanticScore;

                // 4. Texture Score (0 - 20 pts)
                int texScore = ComputeTextureScore(item.Category, candidate, reasons);
                scoreObj.TextureScore = texScore;

                // 5. Compatibility Score (0 - 20 pts)
                int compatScore = ComputeCompatibilityScore(candidate, reasons);
                scoreObj.CompatibilityScore = compatScore;

                // Total raw score out of 120, normalized to 0-100%
                int rawTotal = catScore + nameScore + semanticScore + texScore + compatScore;
                int finalScore = Mathf.Clamp(Mathf.RoundToInt((float)rawTotal / 120f * 100f), 0, 100);

                scoreObj.FinalScore = finalScore;
                scoreObj.Confidence = (float)finalScore;
                scoreObj.Explanation = string.Join(" | ", reasons);

                if (finalScore < 55)
                {
                    scoreObj.IsRejected = true;
                    scoreObj.RejectionReason = $"Score {finalScore}% below minimum 55% threshold.";
                }

                ranked.Add(scoreObj);
            }

            return ranked.OrderByDescending(s => s.FinalScore).ThenByDescending(s => s.TextureScore).ToList();
        }

        // =========================================================================
        // DETERMINISTIC CATEGORY CLASSIFIERS
        // =========================================================================

        public static string ExtractEnvironmentContext(string hierarchyPath)
        {
            if (string.IsNullOrEmpty(hierarchyPath)) return "Generic Environment";
            string lower = hierarchyPath.ToLowerInvariant();
            if (lower.Contains("/water/") || lower.Contains("/lake/") || lower.Contains("/river/")) return "Water / Lake";
            if (lower.Contains("/rock/")) return "Rock / Mountain";
            if (lower.Contains("/tree/")) return "Tree / Forest";
            if (lower.Contains("/ground/")) return "Ground / Terrain";
            if (lower.Contains("/snow/")) return "Snow / Alpine";
            if (lower.Contains("/ruin/") || lower.Contains("/ancient/")) return "Ancient / Ruin";
            return "Generic Environment";
        }

        public static MaterialSemanticCategory ClassifyTargetCategory(Renderer renderer, int slot, Material currentMaterial, string hierarchyPath)
        {
            string objName = (renderer != null && renderer) ? renderer.name.ToLowerInvariant() : "";
            string hierPath = (hierarchyPath ?? "").ToLowerInvariant();
            string matName = (currentMaterial != null && currentMaterial) ? currentMaterial.name.ToLowerInvariant() : "";
            string texName = DescribeTexture(currentMaterial, "_BaseMap", "_MainTex").ToLowerInvariant();

            // ---------------------------------------------------------------------
            // PRIORITY 1: Explicit slot material semantics
            // ---------------------------------------------------------------------
            if (matName.Contains("bark"))
            {
                return MaterialSemanticCategory.Bark;
            }
            if (matName.Contains("trunk"))
            {
                return MaterialSemanticCategory.Trunk;
            }
            if (matName.Contains("leaf") || matName.Contains("leaves") || matName.Contains("needle") || matName.Contains("frond"))
            {
                return MaterialSemanticCategory.Leaf;
            }
            if (matName.Contains("foliage") || matName.Contains("plant") || matName.Contains("bush") || matName.Contains("fern"))
            {
                if (matName.Contains("fern")) return MaterialSemanticCategory.Fern;
                if (matName.Contains("bush")) return MaterialSemanticCategory.Bush;
                return MaterialSemanticCategory.Foliage;
            }
            if (matName.Contains("grass"))
            {
                return MaterialSemanticCategory.Grass;
            }
            if (matName.Contains("rock") || matName.Contains("stone") || matName.Contains("cliff"))
            {
                if (objName.Contains("mountain") || hierPath.Contains("mountain")) return MaterialSemanticCategory.Mountain;
                if (objName.Contains("stone") || matName.Contains("stone")) return MaterialSemanticCategory.Stone;
                if (objName.Contains("cliff") || matName.Contains("cliff")) return MaterialSemanticCategory.Cliff;
                return MaterialSemanticCategory.Rock;
            }
            if (matName.Contains("snow") || matName.Contains("ice") || matName.Contains("frost"))
            {
                if (matName.Contains("ice")) return MaterialSemanticCategory.Ice;
                return MaterialSemanticCategory.Snow;
            }
            if (matName.Contains("sand") || matName.Contains("dune"))
            {
                return MaterialSemanticCategory.Sand;
            }
            if (matName.Contains("dirt") || matName.Contains("mud") || matName.Contains("soil"))
            {
                return MaterialSemanticCategory.Dirt;
            }
            if (matName.Contains("water") || matName.Contains("river") || matName.Contains("lake") || matName.Contains("waterfall"))
            {
                if (matName.Contains("waterlily") || matName.Contains("lily")) return MaterialSemanticCategory.Leaf;
                if (matName.Contains("waterfall")) return MaterialSemanticCategory.Waterfall;
                if (matName.Contains("river")) return MaterialSemanticCategory.River;
                if (matName.Contains("lake")) return MaterialSemanticCategory.Lake;
                return MaterialSemanticCategory.Water;
            }

            // ---------------------------------------------------------------------
            // PRIORITY 2: Object / Mesh Name Semantics
            // ---------------------------------------------------------------------
            if (objName.Contains("trunk") || objName.Contains("stump") || objName.Contains("log"))
            {
                return MaterialSemanticCategory.Trunk;
            }
            if (objName.Contains("bark"))
            {
                return MaterialSemanticCategory.Bark;
            }
            if (objName.Contains("leaf") || objName.Contains("leaves") || objName.Contains("needle") || objName.Contains("frond") || objName.Contains("canopy"))
            {
                return MaterialSemanticCategory.Leaf;
            }
            if (objName.Contains("foliage") || objName.Contains("plant") || objName.Contains("bush") || objName.Contains("fern") || objName.Contains("shrub"))
            {
                if (objName.Contains("fern")) return MaterialSemanticCategory.Fern;
                if (objName.Contains("bush") || objName.Contains("shrub")) return MaterialSemanticCategory.Bush;
                return MaterialSemanticCategory.Foliage;
            }
            if (objName.Contains("grass"))
            {
                return MaterialSemanticCategory.Grass;
            }
            if (objName.Contains("stone") || objName.Contains("boulder") || objName.Contains("pebble"))
            {
                return MaterialSemanticCategory.Stone;
            }
            if (objName.Contains("mountain"))
            {
                return MaterialSemanticCategory.Mountain;
            }
            if (objName.Contains("rock") || objName.Contains("cliff") || objName.Contains("crag"))
            {
                if (objName.Contains("cliff")) return MaterialSemanticCategory.Cliff;
                return MaterialSemanticCategory.Rock;
            }
            if (objName.Contains("snow") || objName.Contains("ice"))
            {
                return objName.Contains("ice") ? MaterialSemanticCategory.Ice : MaterialSemanticCategory.Snow;
            }
            if (objName.Contains("water") || objName.Contains("lake") || objName.Contains("river") || objName.Contains("waterfall") || objName.Contains("ocean") || objName.Contains("pond"))
            {
                if (objName.Contains("waterlily") || objName.Contains("lily")) return MaterialSemanticCategory.Leaf;
                if (objName.Contains("waterfall")) return MaterialSemanticCategory.Waterfall;
                if (objName.Contains("river")) return MaterialSemanticCategory.River;
                if (objName.Contains("lake")) return MaterialSemanticCategory.Lake;
                return MaterialSemanticCategory.Water;
            }
            if (objName.Contains("wood") || objName.Contains("plank") || objName.Contains("branch"))
            {
                return MaterialSemanticCategory.Wood;
            }
            if (objName.Contains("terrain") || objName.Contains("ground") || objName.Contains("dirt") || objName.Contains("mud") || objName.Contains("soil"))
            {
                if (objName.Contains("dirt") || objName.Contains("mud")) return MaterialSemanticCategory.Dirt;
                return MaterialSemanticCategory.Terrain;
            }
            if (objName.Contains("ruin") || objName.Contains("temple") || objName.Contains("pillar") || objName.Contains("statue") || objName.Contains("monument"))
            {
                return MaterialSemanticCategory.Ancient;
            }

            // ---------------------------------------------------------------------
            // PRIORITY 3: Hierarchy Path Semantics
            // ---------------------------------------------------------------------
            if (hierPath.Contains("/water/") || hierPath.Contains("/lake/") || hierPath.Contains("/river/"))
            {
                if (hierPath.Contains("waterlily") || hierPath.Contains("lily")) return MaterialSemanticCategory.Leaf;
                if (hierPath.Contains("waterfall")) return MaterialSemanticCategory.Waterfall;
                if (hierPath.Contains("river")) return MaterialSemanticCategory.River;
                if (hierPath.Contains("lake")) return MaterialSemanticCategory.Lake;
                return MaterialSemanticCategory.Water;
            }
            if (hierPath.Contains("/rock/"))
            {
                if (hierPath.Contains("stone")) return MaterialSemanticCategory.Stone;
                if (hierPath.Contains("mountain")) return MaterialSemanticCategory.Mountain;
                if (hierPath.Contains("cliff")) return MaterialSemanticCategory.Cliff;
                return MaterialSemanticCategory.Rock;
            }
            if (hierPath.Contains("/tree/"))
            {
                if (objName.Contains("trunk") || objName.Contains("wood") || matName.Contains("bark") || matName.Contains("trunk"))
                    return MaterialSemanticCategory.Trunk;
                return MaterialSemanticCategory.Leaf;
            }
            if (hierPath.Contains("/snow/"))
            {
                return MaterialSemanticCategory.Snow;
            }
            if (hierPath.Contains("/ground/") || hierPath.Contains("/terrain/"))
            {
                if (hierPath.Contains("mountain")) return MaterialSemanticCategory.Mountain;
                return MaterialSemanticCategory.Ground;
            }

            // ---------------------------------------------------------------------
            // PRIORITY 4: BaseMap Texture Semantics
            // ---------------------------------------------------------------------
            if (texName.Contains("bark")) return MaterialSemanticCategory.Bark;
            if (texName.Contains("trunk")) return MaterialSemanticCategory.Trunk;
            if (texName.Contains("leaf") || texName.Contains("leaves")) return MaterialSemanticCategory.Leaf;
            if (texName.Contains("grass")) return MaterialSemanticCategory.Grass;
            if (texName.Contains("rock") || texName.Contains("stone")) return MaterialSemanticCategory.Rock;
            if (texName.Contains("snow")) return MaterialSemanticCategory.Snow;
            if (texName.Contains("water")) return MaterialSemanticCategory.Water;

            return MaterialSemanticCategory.Other;
        }

        public static MaterialSemanticCategory ClassifyCandidateCategory(Material material)
        {
            if (material == null) return MaterialSemanticCategory.Other;

            string name = material.name.ToLowerInvariant();
            string path = AssetDatabase.GetAssetPath(material).ToLowerInvariant();
            string texName = DescribeTexture(material, "_BaseMap", "_MainTex").ToLowerInvariant();
            string full = $"{name} {path} {texName}";

            // Water Lily / Aquatic Plant check
            if (full.Contains("waterlily") || full.Contains("water_lily") || full.Contains("lilypad"))
            {
                return MaterialSemanticCategory.Leaf;
            }

            // Water categories
            if (full.Contains("waterfall")) return MaterialSemanticCategory.Waterfall;
            if (full.Contains("river")) return MaterialSemanticCategory.River;
            if (full.Contains("lake") && !full.Contains("landscape")) return MaterialSemanticCategory.Lake;
            if (name.Contains("water") || path.Contains("/water/")) return MaterialSemanticCategory.Water;

            // Bark & Trunk (WoodBark Family)
            if (name.Contains("palmtrunk") || name.Contains("palm_trunk")) return MaterialSemanticCategory.Trunk;
            if (full.Contains("bark")) return MaterialSemanticCategory.Bark;
            if (full.Contains("trunk") || full.Contains("log") || full.Contains("stump")) return MaterialSemanticCategory.Trunk;
            if (full.Contains("wood") || full.Contains("plank") || full.Contains("timber")) return MaterialSemanticCategory.Wood;

            // Foliage & Vegetation Family
            if (full.Contains("grass") || full.Contains("lawn") || full.Contains("meadow")) return MaterialSemanticCategory.Grass;
            if (full.Contains("fern")) return MaterialSemanticCategory.Fern;
            if (full.Contains("bush") || full.Contains("shrub") || full.Contains("hedge")) return MaterialSemanticCategory.Bush;
            if (full.Contains("leaf") || full.Contains("leaves") || full.Contains("needle") || full.Contains("frond") || full.Contains("palmfrond") || full.Contains("canopy"))
                return MaterialSemanticCategory.Leaf;
            if (full.Contains("foliage") || full.Contains("plant") || full.Contains("flora") || full.Contains("branch"))
                return MaterialSemanticCategory.Foliage;

            // Rock & Stone Family
            if (full.Contains("mountain") || full.Contains("alpine")) return MaterialSemanticCategory.Mountain;
            if (full.Contains("stone") || full.Contains("boulder") || full.Contains("pebble") || full.Contains("cobble")) return MaterialSemanticCategory.Stone;
            if (full.Contains("cliff") || full.Contains("crag")) return MaterialSemanticCategory.Cliff;
            if (full.Contains("rock")) return MaterialSemanticCategory.Rock;

            // Snow & Ice Family
            if (full.Contains("ice") || full.Contains("glacier")) return MaterialSemanticCategory.Ice;
            if (full.Contains("snow") || full.Contains("frost") || full.Contains("winter")) return MaterialSemanticCategory.Snow;

            // Ground & Terrain Family
            if (full.Contains("sand") || full.Contains("dune") || full.Contains("beach")) return MaterialSemanticCategory.Sand;
            if (full.Contains("dirt") || full.Contains("mud") || full.Contains("soil") || full.Contains("earth")) return MaterialSemanticCategory.Dirt;
            if (full.Contains("ground") || full.Contains("terrain") || full.Contains("path") || full.Contains("road")) return MaterialSemanticCategory.Terrain;

            // Ancient Structures
            if (full.Contains("ancient") || full.Contains("ruin") || full.Contains("temple") || full.Contains("monument") || full.Contains("statue"))
                return MaterialSemanticCategory.Ancient;

            // Metal
            if (full.Contains("metal") || full.Contains("iron") || full.Contains("steel") || full.Contains("bronze") || full.Contains("gold"))
                return MaterialSemanticCategory.Metal;

            return MaterialSemanticCategory.Other;
        }

        public static CategoryFamily GetCategoryFamily(MaterialSemanticCategory category)
        {
            switch (category)
            {
                case MaterialSemanticCategory.Water:
                case MaterialSemanticCategory.Waterfall:
                case MaterialSemanticCategory.River:
                case MaterialSemanticCategory.Lake:
                    return CategoryFamily.Water;

                case MaterialSemanticCategory.Snow:
                case MaterialSemanticCategory.Ice:
                    return CategoryFamily.SnowIce;

                case MaterialSemanticCategory.Bark:
                case MaterialSemanticCategory.Trunk:
                case MaterialSemanticCategory.Wood:
                    return CategoryFamily.WoodBark;

                case MaterialSemanticCategory.Foliage:
                case MaterialSemanticCategory.Leaf:
                case MaterialSemanticCategory.Grass:
                case MaterialSemanticCategory.Bush:
                case MaterialSemanticCategory.Fern:
                    return CategoryFamily.Vegetation;

                case MaterialSemanticCategory.Rock:
                case MaterialSemanticCategory.Stone:
                case MaterialSemanticCategory.Cliff:
                case MaterialSemanticCategory.Mountain:
                    return CategoryFamily.RockStone;

                case MaterialSemanticCategory.Ground:
                case MaterialSemanticCategory.Terrain:
                case MaterialSemanticCategory.Dirt:
                case MaterialSemanticCategory.Sand:
                    return CategoryFamily.GroundTerrain;

                case MaterialSemanticCategory.Ancient:
                case MaterialSemanticCategory.Ruin:
                    return CategoryFamily.AncientStructure;

                case MaterialSemanticCategory.Metal:
                    return CategoryFamily.Metal;

                default:
                    return CategoryFamily.Other;
            }
        }

        // =========================================================================
        // NEGATIVE MATCHING & HARD EXCLUSIONS
        // =========================================================================

        public static bool IsHardCategoryExclusion(
            MaterialSemanticCategory targetCat,
            MaterialSemanticCategory candCat,
            string targetText,
            string candText,
            out string conflictReason)
        {
            conflictReason = "";
            string tText = (targetText ?? "").ToLowerInvariant();
            string cText = (candText ?? "").ToLowerInvariant();

            CategoryFamily targetFam = GetCategoryFamily(targetCat);
            CategoryFamily candFam = GetCategoryFamily(candCat);

            // 1. PALM TRUNK / COCONUT vs. PINE / OAK / GENERIC TRUNK:
            bool isCandidatePalm = cText.Contains("palmtrunk") || cText.Contains("palm_trunk") || cText.Contains("palm trunk") || cText.Contains("coconut");
            bool isTargetPalm = tText.Contains("palm") || tText.Contains("coconut");
            bool isTargetPineOrGeneralTree = tText.Contains("pine") || tText.Contains("pine5") || tText.Contains("pine6") || tText.Contains("oak") || tText.Contains("conifer") || (tText.Contains("tree") && !isTargetPalm);

            if (isCandidatePalm && !isTargetPalm && isTargetPineOrGeneralTree)
            {
                conflictReason = $"Palm trunk material cannot be assigned to non-palm tree/pine trunk.";
                return true;
            }

            // 2. WATER SURFACE vs. WATER LILY / AQUATIC PLANT:
            if (targetFam == CategoryFamily.Water)
            {
                if (cText.Contains("waterlily") || cText.Contains("water_lily") || cText.Contains("lily") || candFam == CategoryFamily.Vegetation || candFam == CategoryFamily.WoodBark || candFam == CategoryFamily.RockStone)
                {
                    conflictReason = $"Water surface target cannot accept '{candCat}' or aquatic plant/lily material.";
                    return true;
                }
            }
            if (candFam == CategoryFamily.Water && targetFam != CategoryFamily.Water)
            {
                conflictReason = $"Water material cannot be applied to non-water target '{targetCat}'.";
                return true;
            }

            // 3. WOOD / BARK vs. NON-WOOD:
            if (targetFam == CategoryFamily.WoodBark)
            {
                if (candFam == CategoryFamily.Vegetation || candFam == CategoryFamily.RockStone || candFam == CategoryFamily.Water || candFam == CategoryFamily.SnowIce)
                {
                    conflictReason = $"Wood/Bark target cannot accept '{candCat}' material.";
                    return true;
                }
            }
            if (candFam == CategoryFamily.WoodBark && targetFam != CategoryFamily.WoodBark)
            {
                conflictReason = $"Wood/Bark material cannot be assigned to non-wood target '{targetCat}'.";
                return true;
            }

            // 4. ROCK / STONE / MOUNTAIN TARGET:
            if (targetFam == CategoryFamily.RockStone)
            {
                if (candFam == CategoryFamily.Vegetation || candFam == CategoryFamily.WoodBark || candFam == CategoryFamily.Water)
                {
                    conflictReason = $"Rock/Stone target cannot accept '{candCat}' material.";
                    return true;
                }
            }
            if (candFam == CategoryFamily.RockStone && (targetFam == CategoryFamily.Vegetation || targetFam == CategoryFamily.WoodBark || targetFam == CategoryFamily.Water))
            {
                conflictReason = $"Rock/Stone material cannot be assigned to '{targetCat}' target.";
                return true;
            }

            // 5. VEGETATION (FOLIAGE/LEAF/GRASS) TARGET:
            if (targetFam == CategoryFamily.Vegetation)
            {
                if (candFam == CategoryFamily.WoodBark || candFam == CategoryFamily.RockStone || candFam == CategoryFamily.Water || candFam == CategoryFamily.SnowIce)
                {
                    conflictReason = $"Vegetation target cannot accept '{candCat}' material.";
                    return true;
                }
            }

            // 6. SNOW / ICE TARGET:
            if (targetFam == CategoryFamily.SnowIce)
            {
                if (candFam != CategoryFamily.SnowIce && candFam != CategoryFamily.RockStone)
                {
                    conflictReason = $"Snow/Ice target requires snow/ice material, but candidate is '{candCat}'.";
                    return true;
                }
            }
            if (candFam == CategoryFamily.SnowIce && targetFam != CategoryFamily.SnowIce)
            {
                conflictReason = $"Snow/Ice material rejected because target '{targetCat}' has no snow semantics.";
                return true;
            }

            return false;
        }

        // =========================================================================
        // SCORE COMPUTATIONS (DETERMINISTIC & TRANSPARENT)
        // =========================================================================

        public static int ComputeCategoryScore(MaterialSemanticCategory targetCat, MaterialSemanticCategory candCat, string targetText, string candText, List<string> reasons)
        {
            CategoryFamily targetFam = GetCategoryFamily(targetCat);
            CategoryFamily candFam = GetCategoryFamily(candCat);

            // Exact category match: +30
            if (targetCat == candCat)
            {
                reasons.Add($"Exact category match (+30 pts): {targetCat}");
                return 30;
            }

            // Same family match:
            if (targetFam == candFam)
            {
                reasons.Add($"Same category family (+25 pts): {targetFam}");
                return 25;
            }

            // Cross-family rules:
            if (targetFam == CategoryFamily.RockStone && candFam == CategoryFamily.GroundTerrain)
            {
                reasons.Add($"Plausible geology pairing (+15 pts): {targetCat} with {candCat}");
                return 15;
            }
            if (targetFam == CategoryFamily.GroundTerrain && candFam == CategoryFamily.RockStone)
            {
                reasons.Add($"Plausible geology pairing (+15 pts): {targetCat} with {candCat}");
                return 15;
            }
            if (targetFam == CategoryFamily.SnowIce && candFam == CategoryFamily.RockStone)
            {
                reasons.Add($"Alpine rock/snow compatibility (+10 pts)");
                return 10;
            }
            if (targetFam == CategoryFamily.AncientStructure && candFam == CategoryFamily.RockStone)
            {
                reasons.Add($"Stone structure compatibility (+20 pts)");
                return 20;
            }
            if (targetFam == CategoryFamily.Vegetation && candFam == CategoryFamily.GroundTerrain)
            {
                reasons.Add($"Ground/vegetation margin (+5 pts)");
                return 5;
            }

            reasons.Add($"Category mismatch (0 pts): {targetCat} vs {candCat}");
            return 0;
        }

        public static int ComputeNameScore(string targetText, Material candidate, string normalizedCurrent, List<string> reasons)
        {
            int score = 0;
            string candName = (candidate != null && candidate) ? candidate.name.ToLowerInvariant() : "";
            string normCand = NormalizeMaterialName(candName);

            // Exact normalized name match (e.g. Pine Bark -> Pine Bark_URP)
            if (!string.IsNullOrEmpty(normalizedCurrent) && !string.IsNullOrEmpty(normCand))
            {
                if (normCand == normalizedCurrent)
                {
                    score += 20;
                    reasons.Add($"Exact name match (+20 pts): '{candName}' == '{normalizedCurrent}'");
                    return score;
                }
                if (normCand.Contains(normalizedCurrent) || normalizedCurrent.Contains(normCand))
                {
                    score += 15;
                    reasons.Add($"Sub-string material name match (+15 pts): '{candName}'");
                    return score;
                }
            }

            var targetTokens = Tokenize(targetText);
            var candTokens = Tokenize(candName);

            int matchingSpecificTokens = 0;
            foreach (var t in candTokens)
            {
                if (IsGenericToken(t)) continue;
                if (targetTokens.Contains(t))
                {
                    matchingSpecificTokens++;
                }
            }

            if (matchingSpecificTokens >= 2)
            {
                score += 18;
                reasons.Add($"Multiple specific token match (+18 pts): {matchingSpecificTokens} tokens");
            }
            else if (matchingSpecificTokens == 1)
            {
                score += 10;
                reasons.Add($"Specific token match (+10 pts)");
            }

            return Mathf.Clamp(score, 0, 20);
        }

        public static int ComputeSemanticScore(string environmentContext, string targetPath, Material candidate, MaterialSemanticCategory targetCat, MaterialSemanticCategory candCat, List<string> reasons)
        {
            int score = 0;
            string candPath = candidate != null ? AssetDatabase.GetAssetPath(candidate).ToLowerInvariant() : "";
            string candName = (candidate != null && candidate) ? candidate.name.ToLowerInvariant() : "";
            string baseMapName = DescribeTexture(candidate, "_BaseMap", "_MainTex").ToLowerInvariant();
            string candFull = $"{candName} {candPath} {baseMapName}";

            string envLower = (environmentContext ?? "").ToLowerInvariant();
            if (envLower.Contains("water") && (candFull.Contains("water") || candFull.Contains("lake") || candFull.Contains("river") || candFull.Contains("waterfall")))
            {
                if (!candFull.Contains("lily"))
                {
                    score += 15;
                    reasons.Add($"Environment context match (+15 pts): Water/Lake");
                }
            }
            else if (envLower.Contains("tree") && (candFull.Contains("tree") || candFull.Contains("bark") || candFull.Contains("pine") || candFull.Contains("foliage") || candFull.Contains("leaf")))
            {
                score += 15;
                reasons.Add($"Environment context match (+15 pts): Tree/Forest");
            }
            else if (envLower.Contains("rock") && (candFull.Contains("rock") || candFull.Contains("stone") || candFull.Contains("cliff")))
            {
                score += 15;
                reasons.Add($"Environment context match (+15 pts): Rock/Mountain");
            }
            else if (envLower.Contains("ground") && (candFull.Contains("ground") || candFull.Contains("terrain") || candFull.Contains("dirt") || candFull.Contains("sand")))
            {
                score += 15;
                reasons.Add($"Environment context match (+15 pts): Ground/Terrain");
            }
            else if (envLower.Contains("snow") && (candFull.Contains("snow") || candFull.Contains("ice") || candFull.Contains("frost")))
            {
                score += 15;
                reasons.Add($"Environment context match (+15 pts): Snow/Alpine");
            }

            // Specific folder / asset naming match
            CategoryFamily targetFam = GetCategoryFamily(targetCat);
            if (targetFam == CategoryFamily.RockStone)
            {
                if (candPath.Contains("/rock") || candPath.Contains("/stone") || candName.Contains("rock") || candName.Contains("stone"))
                {
                    score += 15;
                    reasons.Add($"Asset semantic folder/naming match (+15 pts): Rock");
                }
            }
            else if (targetFam == CategoryFamily.Vegetation)
            {
                if (candPath.Contains("/foliage") || candPath.Contains("/leaves") || candName.Contains("foliage") || candName.Contains("leaf") || candName.Contains("leaves"))
                {
                    score += 15;
                    reasons.Add($"Asset semantic folder/naming match (+15 pts): Foliage");
                }
            }
            else if (targetFam == CategoryFamily.WoodBark)
            {
                if (candPath.Contains("/bark") || candPath.Contains("/trunk") || candPath.Contains("/tree") || candName.Contains("bark") || candName.Contains("trunk"))
                {
                    score += 15;
                    reasons.Add($"Asset semantic folder/naming match (+15 pts): Bark/Trunk");
                }
            }
            else if (targetFam == CategoryFamily.Water)
            {
                if (candPath.Contains("/water") || candName.Contains("water"))
                {
                    score += 15;
                    reasons.Add($"Asset semantic folder/naming match (+15 pts): Water");
                }
            }

            return Mathf.Clamp(score, 0, 30);
        }

        public static int ComputeTextureScore(MaterialSemanticCategory targetCat, Material candidate, List<string> reasons)
        {
            if (candidate == null) return 0;

            Texture baseMap = null;
            if (candidate.HasProperty("_BaseMap")) baseMap = candidate.GetTexture("_BaseMap");
            else if (candidate.HasProperty("_MainTex")) baseMap = candidate.GetTexture("_MainTex");

            if (baseMap == null)
            {
                reasons.Add("No BaseMap texture present (0 pts)");
                return 0;
            }

            int score = 15;
            reasons.Add($"Valid BaseMap texture '{baseMap.name}' (+15 pts)");

            var candCatFromTex = ClassifyCandidateCategory(candidate);
            if (GetCategoryFamily(candCatFromTex) == GetCategoryFamily(targetCat))
            {
                score += 5;
                reasons.Add($"BaseMap texture semantically matches target category (+5 pts)");
            }

            return Mathf.Clamp(score, 0, 20);
        }

        public static int ComputeCompatibilityScore(Material candidate, List<string> reasons)
        {
            if (candidate == null) return 0;
            int score = 0;
            if (candidate.shader != null)
            {
                string sName = candidate.shader.name;
                if (sName.StartsWith("Universal Render Pipeline/") || sName.StartsWith("URP/") || sName.Contains("Shader Graphs/"))
                {
                    score += 15;
                    reasons.Add($"URP native shader '{sName}' (+15 pts)");
                }
                else
                {
                    score += 5;
                    reasons.Add($"Shader '{sName}' requires URP adaptation (+5 pts)");
                }

                if (candidate.HasProperty("_BumpMap") && candidate.GetTexture("_BumpMap") != null)
                {
                    score += 5;
                    reasons.Add("Includes Normal/Bump map (+5 pts)");
                }
            }
            return Mathf.Clamp(score, 0, 20);
        }

        // =========================================================================
        // RECOVERY APPLICATION (NON-DESTRUCTIVE / SAFE)
        // =========================================================================

        public void ApplyHighConfidence()
        {
            var highItems = _items.Where(i => i != null && i.Status == RecoveryStatus.HighConfidence && i.Candidate != null && i.Candidate).ToList();

            if (highItems.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "HD Material Recovery",
                    "No High Confidence (≥90%) items are available to apply.",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Apply High Confidence Materials",
                $"Are you sure you want to apply {highItems.Count} High Confidence (≥90%) recovered material(s) to scene renderers?\n\nThis will create URP materials and assign them safely with Undo support.",
                "Apply High Confidence",
                "Cancel"))
            {
                return;
            }

            int applied = 0;
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Apply High Confidence HD Materials");
            int group = Undo.GetCurrentGroup();

            foreach (var item in highItems)
            {
                if (ApplyItemInternal(item))
                    applied++;
            }

            Undo.CollapseUndoOperations(group);
            Scan(false);

            EditorUtility.DisplayDialog(
                "HD Material Recovery",
                $"Successfully applied {applied} high-confidence recovered material(s).",
                "OK");
        }

        public void ApplySelected()
        {
            ValidateSelection();

            if (_selected == null || _selected.Candidate == null || !_selected.Candidate || _selected.Status == RecoveryStatus.NoCandidate)
            {
                EditorUtility.DisplayDialog(
                    "HD Material Recovery",
                    "No valid candidate is selected to apply.",
                    "OK");
                return;
            }

            if (_selected.Renderer == null || !_selected.Renderer)
            {
                EditorUtility.DisplayDialog(
                    "HD Material Recovery",
                    "The target object for this item is no longer available in the scene.",
                    "OK");
                return;
            }

            string targetName = _selected.Renderer.name;
            string currentMatName = (_selected.CurrentMaterial != null && _selected.CurrentMaterial) ? _selected.CurrentMaterial.name : "<None>";
            string candidateName = _selected.Candidate.name;

            if (!EditorUtility.DisplayDialog(
                "Apply Selected Material Recovery",
                $"Apply Candidate Material to Target Slot?\n\n" +
                $"• Target: {targetName} (Slot {_selected.Slot})\n" +
                $"• Current: {currentMatName}\n" +
                $"• Candidate: {candidateName}\n" +
                $"• Confidence: {_selected.Confidence:0}%\n" +
                $"• Category: {_selected.Category} -> {_selected.CandidateCategory}\n\n" +
                $"Reason:\n{_selected.Reason}\n\nThis action can be undone.",
                "Apply",
                "Cancel"))
            {
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName($"Apply Material Recovery to {targetName}");
            int group = Undo.GetCurrentGroup();

            bool success = ApplyItemInternal(_selected);

            Undo.CollapseUndoOperations(group);

            if (success)
            {
                _selected.Status = RecoveryStatus.Recovered;
                Scan(false);
                EditorUtility.DisplayDialog("HD Material Recovery", $"Successfully applied '{candidateName}' to {targetName} [Slot {_selected.Slot}].", "OK");
            }
        }

        private bool ApplyItemInternal(RecoveryItem item)
        {
            if (item == null || item.Renderer == null || !item.Renderer || item.Candidate == null || !item.Candidate)
                return false;

            EnsureFolder(RecoveryFolder);

            Material finalMaterial;
            if (IsURPCompatible(item.Candidate))
            {
                finalMaterial = item.Candidate;
            }
            else
            {
                finalMaterial = CreateRecoveredMaterialAsset(item.Candidate);
                if (finalMaterial == null)
                    return false;
            }

            Undo.RecordObject(item.Renderer, "Apply Recovered Material");
            Material[] mats = item.Renderer.sharedMaterials;
            if (mats != null && item.Slot >= 0 && item.Slot < mats.Length)
            {
                mats[item.Slot] = finalMaterial;
                item.Renderer.sharedMaterials = mats;
                EditorUtility.SetDirty(item.Renderer);
                return true;
            }

            return false;
        }

        private Material CreateRecoveredMaterialAsset(Material source)
        {
            if (source == null) return null;

            string safeName = Sanitize(source.name);
            string assetPath = $"{RecoveryFolder}/M_Recovered_{safeName}_URP.mat";

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
                return existing;

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit") ??
                               Shader.Find("Universal Render Pipeline/Simple Lit") ??
                               Shader.Find("Universal Render Pipeline/Unlit");

            if (urpShader == null)
            {
                Debug.LogError("[HDMaterialRecoveryTool] Universal Render Pipeline/Lit shader not found!");
                return null;
            }

            Material urpMat = new Material(urpShader);
            CopyTextureIfExists(source, "_BaseMap", urpMat, "_BaseMap");
            CopyTextureIfExists(source, "_MainTex", urpMat, "_BaseMap");
            CopyTextureIfExists(source, "_BumpMap", urpMat, "_BumpMap");
            CopyTextureIfExists(source, "_NormalMap", urpMat, "_BumpMap");
            CopyTextureIfExists(source, "_MetallicGlossMap", urpMat, "_MetallicGlossMap");
            CopyTextureIfExists(source, "_EmissionMap", urpMat, "_EmissionMap");

            if (source.HasProperty("_BaseColor"))
                urpMat.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            else if (source.HasProperty("_Color"))
                urpMat.SetColor("_BaseColor", source.GetColor("_Color"));

            if (urpMat.GetTexture("_BumpMap") != null)
                urpMat.EnableKeyword("_NORMALMAP");

            if (urpMat.GetTexture("_EmissionMap") != null)
                urpMat.EnableKeyword("_EMISSION");

            AssetDatabase.CreateAsset(urpMat, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return urpMat;
        }

        private static bool IsURPCompatible(Material material)
        {
            if (material == null || material.shader == null) return false;
            string s = material.shader.name;
            return s.StartsWith("Universal Render Pipeline/") || s.StartsWith("URP/") || s.Contains("Shader Graphs/");
        }

        private static void CopyTextureIfExists(Material source, string sourceProperty, Material target, string targetProperty)
        {
            if (source.HasProperty(sourceProperty) && target.HasProperty(targetProperty))
            {
                Texture tex = source.GetTexture(sourceProperty);
                if (tex != null)
                    target.SetTexture(targetProperty, tex);
            }
        }

        // =========================================================================
        // REPORT EXPORT & CLIPBOARD
        // =========================================================================

        public void ExportReport()
        {
            EnsureFolder("Assets/AILevelBuilder/Reports");

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("HD MATERIAL RECOVERY REPORT");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Scope: {_scope}");
            sb.AppendLine($"Total Items Scanned: {_items.Count}");
            sb.AppendLine($"High Confidence (90-100%): {_highConfidence}");
            sb.AppendLine($"Review (75-89%): {_review}");
            sb.AppendLine($"Low Confidence (55-74%): {_lowConfidence}");
            sb.AppendLine($"No Candidate (0-54%): {_noCandidate}");
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("");

            foreach (var item in _items)
            {
                if (item == null) continue;
                sb.AppendLine(FormatRecoveryItemDetails(item));
                sb.AppendLine("--------------------------------------------------------------------------------");
            }

            try
            {
                File.WriteAllText(Path.GetFullPath(REPORT_OUTPUT_PATH), sb.ToString());
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HDMaterialRecoveryTool] Failed to save report: {ex.Message}");
            }
        }

        private static string FormatRecoveryItemDetails(RecoveryItem item)
        {
            if (item == null) return "No item details.";
            var sb = new StringBuilder();
            bool isAlive = item.Renderer != null && item.Renderer;
            string targetName = isAlive ? item.Renderer.name : (!string.IsNullOrEmpty(item.TargetName) ? item.TargetName : "Unknown Target");
            string currentMat = (item.CurrentMaterial != null && item.CurrentMaterial) ? item.CurrentMaterial.name : (!string.IsNullOrEmpty(item.CurrentMaterialName) ? item.CurrentMaterialName : "<Missing>");
            string candMat = (item.Candidate != null && item.Candidate) ? item.Candidate.name : (!string.IsNullOrEmpty(item.CandidateName) ? item.CandidateName : "NO SAFE CANDIDATE");
            string candPath = (item.Candidate != null && item.Candidate) ? AssetDatabase.GetAssetPath(item.Candidate) : (!string.IsNullOrEmpty(item.CandidateAssetPath) ? item.CandidateAssetPath : "None");

            sb.AppendLine($"Target: {targetName} [Slot {item.Slot}]");
            sb.AppendLine($"Target Status: {(isAlive ? "Active in Scene" : "Unavailable / Deleted")}");
            sb.AppendLine($"Hierarchy: {item.Path}");
            sb.AppendLine($"Target Category: {item.Category}");
            sb.AppendLine($"Environment Context: {item.EnvironmentContext}");
            sb.AppendLine($"Current Material: {currentMat}");
            sb.AppendLine($"Candidate: {candMat}");
            sb.AppendLine($"Candidate Category: {item.CandidateCategory}");
            sb.AppendLine($"Candidate Path: {candPath}");
            sb.AppendLine($"Confidence: {item.Confidence:0}%");
            sb.AppendLine($"Category Score: {item.CategoryScore}");
            sb.AppendLine($"Name Score: {item.NameScore}");
            sb.AppendLine($"Semantic Score: {item.SemanticScore}");
            sb.AppendLine($"Texture Score: {item.TextureScore}");
            sb.AppendLine($"Compatibility Score: {item.CompatibilityScore}");
            sb.AppendLine($"Final Score: {item.FinalScore}");
            sb.AppendLine($"Decision: {StatusLabel(item.Status)}");
            sb.AppendLine($"Reason: {item.Reason}");
            return sb.ToString();
        }

        private void CopyItemsToClipboard(List<RecoveryItem> items, string label)
        {
            if (items == null || items.Count == 0)
            {
                GUIUtility.systemCopyBuffer = $"No {label} available.";
                ShowClipboardNotice($"No {label} to copy.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"HD MATERIAL RECOVERY - {label.ToUpper()} ({items.Count})");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("================================================================================");
            sb.AppendLine("");

            foreach (var it in items)
            {
                if (it == null) continue;
                sb.AppendLine(FormatRecoveryItemDetails(it));
                sb.AppendLine("--------------------------------------------------------------------------------");
            }

            GUIUtility.systemCopyBuffer = sb.ToString();
            ShowClipboardNotice($"Copied {items.Count} {label}!");
        }

        private void ShowClipboardNotice(string message)
        {
            _clipboardStatusNotice = $"✓ {message}";
            _clipboardNoticeExpireTime = EditorApplication.timeSinceStartup + 3.0;
            Repaint();
        }

        private void RecalculateSummary()
        {
            if (_items == null) return;
            _highConfidence = _items.Count(i => i != null && i.Status == RecoveryStatus.HighConfidence);
            _review = _items.Count(i => i != null && i.Status == RecoveryStatus.Review);
            _lowConfidence = _items.Count(i => i != null && i.Status == RecoveryStatus.LowConfidence);
            _noCandidate = _items.Count(i => i != null && i.Status == RecoveryStatus.NoCandidate);
        }

        public static string StatusLabel(RecoveryStatus status)
        {
            switch (status)
            {
                case RecoveryStatus.HighConfidence: return "[HIGH]";
                case RecoveryStatus.Review: return "[REVIEW]";
                case RecoveryStatus.LowConfidence: return "[LOW]";
                case RecoveryStatus.Recovered: return "[RECOVERED]";
                default: return "[NO CANDIDATE]";
            }
        }

        public static string GetStatusColorHex(RecoveryStatus status)
        {
            switch (status)
            {
                case RecoveryStatus.HighConfidence: return "#00FF88";
                case RecoveryStatus.Review: return "#FFCC00";
                case RecoveryStatus.LowConfidence: return "#FF8800";
                case RecoveryStatus.Recovered: return "#00FFFF";
                default: return "#FF3366";
            }
        }

        public static RecoveryStatus ClassifyStatus(float confidence)
        {
            if (confidence >= 90f) return RecoveryStatus.HighConfidence;
            if (confidence >= 75f) return RecoveryStatus.Review;
            if (confidence >= 55f) return RecoveryStatus.LowConfidence;
            return RecoveryStatus.NoCandidate;
        }

        private static string BuildReason(RecoveryItem item)
        {
            if (item == null || item.Candidate == null || !item.Candidate || item.Status == RecoveryStatus.NoCandidate)
            {
                return $"NO SAFE CANDIDATE (Score < 55% or category conflict). Target '{(item != null ? item.Category.ToString() : "Unknown")}' has no safe project material matches.";
            }

            return $"Deterministic match ({item.Confidence:0}%). Target [{item.Category}] -> Candidate [{item.CandidateCategory}]. Source: {AssetDatabase.GetAssetPath(item.Candidate)}";
        }

        private static string BuildTargetText(RecoveryItem item)
        {
            if (item == null) return "";
            string targetName = (item.Renderer != null && item.Renderer) ? item.Renderer.name : (!string.IsNullOrEmpty(item.TargetName) ? item.TargetName : "");
            string current = (item.CurrentMaterial != null && item.CurrentMaterial) ? item.CurrentMaterial.name : (!string.IsNullOrEmpty(item.CurrentMaterialName) ? item.CurrentMaterialName : "");
            return $"{targetName} {item.Path} {item.Category} {item.EnvironmentContext} {current}";
        }

        private static HashSet<string> Tokenize(string text)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(text)) return set;

            char[] splitters = { ' ', '_', '-', '/', '\\', '.', '(', ')', '[', ']' };
            string[] raw = text.Split(splitters, StringSplitOptions.RemoveEmptyEntries);

            foreach (var r in raw)
            {
                string clean = r.Trim().ToLowerInvariant();
                if (clean.Length >= 3)
                    set.Add(clean);
            }
            return set;
        }

        private static bool IsGenericToken(string token)
        {
            string t = token.ToLowerInvariant();
            return t == "material" || t == "mat" || t == "urp" || t == "hdrp" ||
                   t == "srp" || t == "mesh" || t == "renderer" || t == "preview" ||
                   t == "level" || t == "hd" || t == "path" || t == "slot";
        }

        private static bool ContainsWord(string text, params string[] words)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (var w in words)
            {
                if (text.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static string DescribeTexture(Material material, params string[] properties)
        {
            if (material == null) return "(None)";

            foreach (string property in properties)
            {
                if (material.HasProperty(property))
                {
                    Texture texture = material.GetTexture(property);
                    if (texture != null) return texture.name;
                }
            }

            return "(None)";
        }

        private static string DescribeColor(Material material, params string[] properties)
        {
            if (material == null) return "(None)";

            foreach (string property in properties)
            {
                if (material.HasProperty(property))
                {
                    Color c = material.GetColor(property);
                    return $"#{ColorUtility.ToHtmlStringRGBA(c)}";
                }
            }

            return "(None)";
        }

        private static string NormalizeMaterialName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            string result = value.ToLowerInvariant();

            string[] removable =
            {
                "_urp", "_hdrp", "_srp", "_visualaudit", "_recovered",
                "material", ".mat", "copy", "instance", "preview"
            };

            foreach (string token in removable)
                result = result.Replace(token, "");

            return result
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "")
                .Replace("/", "")
                .Replace("\\", "");
        }

        private static bool IsVisualRisk(Material material)
        {
            if (material == null)
                return true;

            if (material.HasProperty("_BaseMap"))
            {
                Texture tex = material.GetTexture("_BaseMap");
                Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : Color.white;
                return tex == null && color.r > 0.94f && color.g > 0.94f && color.b > 0.94f;
            }

            if (material.HasProperty("_MainTex"))
            {
                Texture tex = material.GetTexture("_MainTex");
                Color color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                return tex == null && color.r > 0.94f && color.g > 0.94f && color.b > 0.94f;
            }

            return true;
        }

        private static string GetHierarchyPath(Transform target)
        {
            if (target == null) return "";
            var parts = new List<string>();
            Transform current = target;

            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Material";

            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c.ToString(), "_");

            return value.Replace("/", "_").Replace("\\", "_").Replace(" ", "_");
        }
    }
}
