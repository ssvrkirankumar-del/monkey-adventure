using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonkeyAdventure.AILevelBuilder.Editor
{
    public enum HDVisualAuditClassification
    {
        Clean,
        Warning,
        Error
    }

    [Serializable]
    public class HDVisualAuditSlotInfo
    {
        public GameObject targetGameObject;
        public string gameObjectName;
        public string hierarchyPath;
        public string rendererType;
        public int slotIndex;
        public Material material;
        public string materialName;
        public string shaderName;
        public string baseMapName;
        public string surfaceType;
        public bool isAlphaClip;
        public string cullMode;
        public HDVisualAuditClassification classification;
        public string classificationReason;
        public bool isExpanded;
        public bool isURP;
        public bool isBuiltinStandard;
        public bool isInternalError;
        public bool isMissing;
        public bool isMagentaRisk;
    }

    [Serializable]
    public class HDEnvironmentVisualAuditReport
    {
        public int environmentObjectsFound;
        public int renderersFound;
        public int totalMaterialSlots;
        public int urpCompatible;
        public int builtinStandard;
        public int internalErrorShader;
        public int missingMaterialOrShader;
        public int magentaSuspects;
        public int totalErrors;
        public int totalWarnings;
        public int totalClean;
        public string targetHierarchyPath = "";
        public List<HDVisualAuditSlotInfo> allSlots = new List<HDVisualAuditSlotInfo>();
        public List<HDVisualAuditSlotInfo> errorSlots = new List<HDVisualAuditSlotInfo>();
        public List<HDVisualAuditSlotInfo> warningSlots = new List<HDVisualAuditSlotInfo>();
        public List<HDVisualAuditSlotInfo> cleanSlots = new List<HDVisualAuditSlotInfo>();
    }

    public class HDEnvironmentVisualMaterialAudit : EditorWindow
    {
        private HDEnvironmentVisualAuditReport _report;
        private Vector2 _scrollPos;
        private int _filterTab = 0; // 0=All, 1=Errors, 2=Warnings, 3=Clean
        private string _searchQuery = "";
        private int _warningNavIndex = 0;

        [MenuItem("Window/Monkey Adventure/HD Asset Material Diagnostic/HD Environment Visual Material Audit", false, 100)]
        public static void OpenWindow()
        {
            var window = GetWindow<HDEnvironmentVisualMaterialAudit>("HD Visual Material Audit");
            window.minSize = new Vector2(1000f, 650f);
            window.position = new Rect(window.position.x, window.position.y, 1200f, 800f);
            window.Show();
            window._report = RunAudit(true);
        }

        [MenuItem("Window/Monkey Adventure/HD Asset Material Diagnostic/Auto-Fix HD Environment Materials", false, 101)]
        public static void MenuAutoFix()
        {
            AutoFixEnvironmentMaterials(true);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            try
            {
                EditorGUILayout.LabelField("🎨 HD ENVIRONMENT VISUAL MATERIAL AUDIT", EditorStyles.boldLabel);

                GUI.backgroundColor = new Color(0.2f, 0.7f, 1.0f);
                if (GUILayout.Button("🔍 Re-Audit", GUILayout.Width(100), GUILayout.Height(24)))
                {
                    _report = RunAudit(true);
                }

                GUI.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
                if (GUILayout.Button("🔧 Auto-Fix Materials", GUILayout.Width(140), GUILayout.Height(24)))
                {
                    _report = AutoFixEnvironmentMaterials(true);
                }

                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("📄 Save Full Report", GUILayout.Width(130), GUILayout.Height(24)))
                {
                    if (_report == null) _report = RunAudit(true);
                    string path = SaveReportToFile(_report);
                    EditorUtility.DisplayDialog("Save Audit Report", $"Saved report to:\n{path}", "OK");
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            if (_report == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox("Click 'Re-Audit' to scan HD environment materials.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(4);
            DrawReportSummary(_report);

            EditorGUILayout.Space(6);
            DrawFilterAndSearchToolbar();

            EditorGUILayout.Space(4);
            DrawSlotsList();
        }

        private void DrawFilterAndSearchToolbar()
        {
            EditorGUILayout.BeginVertical("box");
            try
            {
                // Tab Selection
                EditorGUILayout.BeginHorizontal();
                try
                {
                    if (GUILayout.Toggle(_filterTab == 0, $"All ({_report.totalMaterialSlots})", EditorStyles.toolbarButton)) _filterTab = 0;
                    if (GUILayout.Toggle(_filterTab == 1, $"Errors ({_report.errorSlots.Count})", EditorStyles.toolbarButton)) _filterTab = 1;
                    if (GUILayout.Toggle(_filterTab == 2, $"Warnings ({_report.warningSlots.Count})", EditorStyles.toolbarButton)) _filterTab = 2;
                    if (GUILayout.Toggle(_filterTab == 3, $"Clean ({_report.cleanSlots.Count})", EditorStyles.toolbarButton)) _filterTab = 3;
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(2);

                // Search & Navigation
                EditorGUILayout.BeginHorizontal();
                try
                {
                    EditorGUILayout.LabelField("🔍 Search:", GUILayout.Width(60));
                    _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField);
                    if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(45)))
                    {
                        _searchQuery = "";
                        GUI.FocusControl(null);
                    }

                    if (_report.warningSlots.Count > 0)
                    {
                        if (GUILayout.Button("◄ Prev Warning", EditorStyles.miniButtonLeft, GUILayout.Width(100)))
                        {
                            _filterTab = 2;
                            _warningNavIndex = (_warningNavIndex - 1 + _report.warningSlots.Count) % _report.warningSlots.Count;
                            var target = _report.warningSlots[_warningNavIndex];
                            if (target != null)
                            {
                                target.isExpanded = true;
                                if (target.targetGameObject != null) Selection.activeGameObject = target.targetGameObject;
                            }
                        }
                        EditorGUILayout.LabelField($"Warning {_warningNavIndex + 1}/{_report.warningSlots.Count}", new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter }, GUILayout.Width(95));
                        if (GUILayout.Button("Next Warning ►", EditorStyles.miniButtonRight, GUILayout.Width(100)))
                        {
                            _filterTab = 2;
                            _warningNavIndex = (_warningNavIndex + 1) % _report.warningSlots.Count;
                            var target = _report.warningSlots[_warningNavIndex];
                            if (target != null)
                            {
                                target.isExpanded = true;
                                if (target.targetGameObject != null) Selection.activeGameObject = target.targetGameObject;
                            }
                        }
                    }

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("📋 Copy Filtered", EditorStyles.miniButton, GUILayout.Width(100)))
                    {
                        var visible = GetFilteredSlots();
                        CopySlotsToClipboard(visible, $"Filtered Slots ({visible.Count})");
                    }
                    if (GUILayout.Button("📋 Copy Warnings", EditorStyles.miniButton, GUILayout.Width(105)))
                    {
                        CopySlotsToClipboard(_report.warningSlots, $"Warnings ({_report.warningSlots.Count})");
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

        private void DrawSlotsList()
        {
            var itemsToShow = GetFilteredSlots();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
            try
            {
                if (itemsToShow.Count == 0)
                {
                    EditorGUILayout.Space(20);
                    EditorGUILayout.HelpBox("No items match the current filter and search query.", MessageType.Info);
                }
                else
                {
                    foreach (var item in itemsToShow)
                    {
                        if (item == null) continue;

                        string statusColor = item.classification == HDVisualAuditClassification.Clean ? "#00FF88" :
                                             item.classification == HDVisualAuditClassification.Warning ? "#FFCC00" : "#FF3366";

                        EditorGUILayout.BeginVertical("box");
                        try
                        {
                            EditorGUILayout.BeginHorizontal();
                            try
                            {
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
                            }
                            finally
                            {
                                EditorGUILayout.EndHorizontal();
                            }

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
                                EditorGUILayout.LabelField("Hierarchy Path:", item.hierarchyPath, EditorStyles.miniLabel);
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
                                    GUIUtility.systemCopyBuffer = FormatSlotInfo(item);
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

        private List<HDVisualAuditSlotInfo> GetFilteredSlots()
        {
            if (_report == null) return new List<HDVisualAuditSlotInfo>();

            List<HDVisualAuditSlotInfo> baseList;
            switch (_filterTab)
            {
                case 1: baseList = _report.errorSlots; break;
                case 2: baseList = _report.warningSlots; break;
                case 3: baseList = _report.cleanSlots; break;
                default: baseList = _report.allSlots; break;
            }

            if (string.IsNullOrEmpty(_searchQuery)) return baseList;

            string q = _searchQuery.ToLowerInvariant();
            return baseList.Where(i =>
                (i.gameObjectName != null && i.gameObjectName.ToLowerInvariant().Contains(q)) ||
                (i.materialName != null && i.materialName.ToLowerInvariant().Contains(q)) ||
                (i.shaderName != null && i.shaderName.ToLowerInvariant().Contains(q)) ||
                (i.hierarchyPath != null && i.hierarchyPath.ToLowerInvariant().Contains(q)) ||
                (i.classificationReason != null && i.classificationReason.ToLowerInvariant().Contains(q))
            ).ToList();
        }

        // =========================================================================
        // AUDIT & AUTO-FIX ENGINE
        // =========================================================================

        public static HDEnvironmentVisualAuditReport RunAudit(bool showLog = true)
        {
            var report = new HDEnvironmentVisualAuditReport();
            string targetPath;
            var roots = FindEnvironmentRoots(out targetPath);
            report.targetHierarchyPath = targetPath;

            if (roots.Count == 0)
            {
                if (showLog)
                {
                    Debug.LogWarning("[HDEnvironmentVisualMaterialAudit] No HD Environment hierarchy root found in scene.");
                }
                return report;
            }

            var inspectedObjects = new HashSet<GameObject>();

            foreach (var root in roots)
            {
                if (root == null) continue;

                var renderers = root.GetComponentsInChildren<Renderer>(true);
                foreach (var rend in renderers)
                {
                    if (rend == null) continue;

                    inspectedObjects.Add(rend.gameObject);
                    report.renderersFound++;

                    string rType = rend.GetType().Name;
                    string hPath = GetHierarchyPath(rend.transform);
                    Material[] shared = rend.sharedMaterials;

                    if (shared == null || shared.Length == 0)
                    {
                        var slotInfo = new HDVisualAuditSlotInfo
                        {
                            targetGameObject = rend.gameObject,
                            gameObjectName = rend.gameObject.name,
                            hierarchyPath = hPath,
                            rendererType = rType,
                            slotIndex = 0,
                            material = null,
                            materialName = "<Missing Material Array>",
                            shaderName = "<None>",
                            baseMapName = "<None>",
                            surfaceType = "Unknown",
                            isAlphaClip = false,
                            cullMode = "Unknown",
                            isMissing = true,
                            classification = HDVisualAuditClassification.Error,
                            classificationReason = "Renderer has missing material array (null or empty)."
                        };
                        report.missingMaterialOrShader++;
                        report.totalErrors++;
                        report.errorSlots.Add(slotInfo);
                        report.allSlots.Add(slotInfo);
                        report.totalMaterialSlots++;
                        continue;
                    }

                    for (int slot = 0; slot < shared.Length; slot++)
                    {
                        report.totalMaterialSlots++;
                        Material m = shared[slot];
                        var info = InspectMaterialSlot(rend.gameObject, rType, hPath, slot, m);

                        report.allSlots.Add(info);

                        if (info.classification == HDVisualAuditClassification.Error)
                        {
                            report.totalErrors++;
                            report.errorSlots.Add(info);
                        }
                        else if (info.classification == HDVisualAuditClassification.Warning)
                        {
                            report.totalWarnings++;
                            report.warningSlots.Add(info);
                        }
                        else
                        {
                            report.totalClean++;
                            report.cleanSlots.Add(info);
                        }

                        if (info.isURP) report.urpCompatible++;
                        if (info.isBuiltinStandard) report.builtinStandard++;
                        if (info.isInternalError) report.internalErrorShader++;
                        if (info.isMissing) report.missingMaterialOrShader++;
                        if (info.isMagentaRisk) report.magentaSuspects++;
                    }
                }
            }

            report.environmentObjectsFound = inspectedObjects.Count;

            if (showLog)
            {
                Debug.Log($"<color=#00FF88><b>[HDEnvironmentVisualMaterialAudit] Audit Complete. Target: '{report.targetHierarchyPath}'. Renderers: {report.renderersFound}, Slots: {report.totalMaterialSlots} (URP: {report.urpCompatible}, Standard: {report.builtinStandard}, Magenta Risk: {report.magentaSuspects}, Warnings: {report.totalWarnings}, Errors: {report.totalErrors}).</b></color>");
            }

            return report;
        }

        public static HDEnvironmentVisualAuditReport AutoFixEnvironmentMaterials(bool showLog = true)
        {
            if (showLog)
            {
                Debug.Log("[HDEnvironmentVisualMaterialAudit] Running Auto-Fix: Converting Standard materials to URP/Lit non-destructively...");
            }

            HDMaterialURPConverter.ApplyConvertedMaterialsToPreview();

            var report = RunAudit(showLog);

            if (showLog)
            {
                Debug.Log($"<color=#00FF88><b>[HDEnvironmentVisualMaterialAudit] Auto-Fix Complete. Remaining Warnings: {report.totalWarnings}, Errors: {report.totalErrors}.</b></color>");
            }

            return report;
        }

        private static HDVisualAuditSlotInfo InspectMaterialSlot(GameObject go, string rendererType, string hierPath, int slot, Material m)
        {
            var info = new HDVisualAuditSlotInfo
            {
                targetGameObject = go,
                gameObjectName = go.name,
                hierarchyPath = hierPath,
                rendererType = rendererType,
                slotIndex = slot,
                material = m,
                surfaceType = "Opaque",
                cullMode = "Back",
                isAlphaClip = false
            };

            if (m == null)
            {
                info.materialName = "<Missing Material>";
                info.shaderName = "<None>";
                info.baseMapName = "<None>";
                info.isMissing = true;
                info.classification = HDVisualAuditClassification.Error;
                info.classificationReason = "Missing material reference (renders pink/magenta).";
                return info;
            }

            info.materialName = m.name;

            if (m.shader == null)
            {
                info.shaderName = "<Missing Shader>";
                info.baseMapName = "<None>";
                info.isMissing = true;
                info.classification = HDVisualAuditClassification.Error;
                info.classificationReason = "Material has missing/null shader reference.";
                return info;
            }

            info.shaderName = m.shader.name;

            // Check InternalErrorShader / Hidden / Pink
            if (info.shaderName.Contains("InternalErrorShader") || info.shaderName.Contains("Error") || info.shaderName.StartsWith("Hidden/"))
            {
                info.isInternalError = true;
                info.isMagentaRisk = true;
                info.classification = HDVisualAuditClassification.Error;
                info.classificationReason = $"Magenta suspect: Invalid or internal error shader '{info.shaderName}'.";
                return info;
            }

            // Inspect Texture properties
            Texture tex = null;
            if (m.HasProperty("_BaseMap")) tex = m.GetTexture("_BaseMap");
            else if (m.HasProperty("_MainTex")) tex = m.GetTexture("_MainTex");
            info.baseMapName = tex != null ? tex.name : "<None>";

            // Inspect Alpha & Cull
            if (m.HasProperty("_Surface"))
            {
                float surface = m.GetFloat("_Surface");
                info.surfaceType = surface > 0.5f ? "Transparent" : "Opaque";
            }
            if (m.HasProperty("_Cutoff") || m.IsKeywordEnabled("_ALPHATEST_ON"))
            {
                info.isAlphaClip = true;
            }
            if (m.HasProperty("_Cull"))
            {
                int cull = m.GetInt("_Cull");
                info.cullMode = cull == 0 ? "Off (DoubleSided)" : cull == 1 ? "Front" : "Back";
            }

            // Classification logic
            if (info.shaderName.StartsWith("Universal Render Pipeline/") || info.shaderName.StartsWith("URP/") || info.shaderName.Contains("Shader Graphs/"))
            {
                info.isURP = true;

                // Check for pure white base color without texture (visual risk)
                Color baseCol = Color.white;
                if (m.HasProperty("_BaseColor")) baseCol = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color")) baseCol = m.GetColor("_Color");

                if (tex == null && baseCol.r > 0.94f && baseCol.g > 0.94f && baseCol.b > 0.94f)
                {
                    info.classification = HDVisualAuditClassification.Warning;
                    info.classificationReason = "URP shader but missing BaseMap texture with untextured white albedo.";
                }
                else
                {
                    info.classification = HDVisualAuditClassification.Clean;
                    info.classificationReason = "Valid URP shader with configured properties.";
                }
            }
            else if (info.shaderName == "Standard" || info.shaderName == "Standard (Specular setup)" || info.shaderName.StartsWith("Legacy Shaders/"))
            {
                info.isBuiltinStandard = true;
                info.classification = HDVisualAuditClassification.Warning;
                info.classificationReason = $"Built-in Pipeline shader '{info.shaderName}'. Auto-Fix can convert this to URP/Lit.";
            }
            else
            {
                info.classification = HDVisualAuditClassification.Warning;
                info.classificationReason = $"Non-standard shader '{info.shaderName}' may require manual verification in URP.";
            }

            return info;
        }

        // =========================================================================
        // HIERARCHY ROOT RESOLUTION
        // =========================================================================

        public static List<Transform> FindEnvironmentRoots(out string resolvedPath)
        {
            resolvedPath = "";
            List<Transform> roots = new List<Transform>();

            string[] preferredPaths = new string[]
            {
                "AI_GENERATED_LEVEL/HD_ENVIRONMENT",
                "AI_GENERATED_LEVEL/HD_ENVIRONMENT_PREVIEW",
                "AI_GENERATED_LEVEL/HD_REPLACEMENTS",
                "AI_GENERATED_LEVEL/HD_REPLACEMENTS_PREVIEW"
            };

            foreach (string path in preferredPaths)
            {
                GameObject go = GameObject.Find(path);
                if (go != null)
                {
                    Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
                    if (renderers != null && renderers.Length > 0)
                    {
                        roots.Add(go.transform);
                        if (string.IsNullOrEmpty(resolvedPath)) resolvedPath = path;
                    }
                }
            }

            if (roots.Count > 0) return roots;

            // Search scene roots recursively
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.isLoaded)
            {
                GameObject[] rootObjects = activeScene.GetRootGameObjects();
                foreach (var rootObj in rootObjects)
                {
                    if (rootObj == null) continue;
                    if (rootObj.name == "Environment" && rootObj.transform.parent == null) continue;

                    SearchHDHierarchyRecursively(rootObj.transform, roots);
                }
            }

            if (roots.Count > 0 && string.IsNullOrEmpty(resolvedPath))
            {
                resolvedPath = GetHierarchyPath(roots[0]);
            }

            return roots;
        }

        private static void SearchHDHierarchyRecursively(Transform current, List<Transform> results)
        {
            if (current == null) return;

            string n = current.name.ToUpperInvariant();
            if (n.Contains("HD_ENVIRONMENT") || n.Contains("HD_REPLACEMENTS"))
            {
                Renderer[] rends = current.GetComponentsInChildren<Renderer>(true);
                if (rends != null && rends.Length > 0)
                {
                    results.Add(current);
                    return;
                }
            }

            for (int i = 0; i < current.childCount; i++)
            {
                SearchHDHierarchyRecursively(current.GetChild(i), results);
            }
        }

        // =========================================================================
        // REPORT UI & FORMATTING HELPERS
        // =========================================================================

        public static void DrawReportSummary(HDEnvironmentVisualAuditReport report)
        {
            if (report == null) return;

            EditorGUILayout.BeginVertical("box");
            try
            {
                string statusColor = report.totalErrors > 0 ? "#FF3366" : report.totalWarnings > 0 ? "#FFCC00" : "#00FF88";
                string statusText = report.totalErrors > 0 ? "ERRORS DETECTED" : report.totalWarnings > 0 ? "WARNINGS DETECTED" : "100% CLEAN";

                EditorGUILayout.LabelField(
                    $"Audit Status: <color={statusColor}><b>{statusText}</b></color> (Target: <b>{(!string.IsNullOrEmpty(report.targetHierarchyPath) ? report.targetHierarchyPath : "Scene Roots")}</b>)",
                    new GUIStyle(EditorStyles.label) { richText = true, fontSize = 12 });

                EditorGUILayout.Space(2);

                EditorGUILayout.BeginHorizontal();
                try
                {
                    EditorGUILayout.LabelField($"Objects: <b>{report.environmentObjectsFound}</b>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                    EditorGUILayout.LabelField($"Renderers: <b>{report.renderersFound}</b>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                    EditorGUILayout.LabelField($"Material Slots: <b>{report.totalMaterialSlots}</b>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                    EditorGUILayout.LabelField($"URP Compatible: <color=#00FF88><b>{report.urpCompatible}</b></color>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                    EditorGUILayout.LabelField($"Built-in Standard: <color=#FFCC00><b>{report.builtinStandard}</b></color>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                try
                {
                    EditorGUILayout.LabelField($"Magenta Risk: <color={(report.magentaSuspects > 0 ? "#FF3366" : "#00FF88")}><b>{report.magentaSuspects}</b></color>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                    EditorGUILayout.LabelField($"Missing Mat/Shader: <color={(report.missingMaterialOrShader > 0 ? "#FF3366" : "#00FF88")}><b>{report.missingMaterialOrShader}</b></color>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                    EditorGUILayout.LabelField($"Clean: <color=#00FF88><b>{report.totalClean}</b></color>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                    EditorGUILayout.LabelField($"Warnings: <color=#FFCC00><b>{report.totalWarnings}</b></color>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
                    EditorGUILayout.LabelField($"Errors: <color=#FF3366><b>{report.totalErrors}</b></color>", new GUIStyle(EditorStyles.miniLabel) { richText = true });
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

        public static string FormatSlotInfo(HDVisualAuditSlotInfo item)
        {
            if (item == null) return "No item info.";
            var sb = new StringBuilder();
            sb.AppendLine($"[{item.classification}] {item.gameObjectName} ({item.rendererType} Slot {item.slotIndex})");
            sb.AppendLine($"Hierarchy: {item.hierarchyPath}");
            sb.AppendLine($"Material: {item.materialName}");
            sb.AppendLine($"Shader: {item.shaderName}");
            sb.AppendLine($"BaseMap: {item.baseMapName}");
            sb.AppendLine($"Surface Type: {item.surfaceType}");
            sb.AppendLine($"Alpha Clip: {item.isAlphaClip}");
            sb.AppendLine($"Cull Mode: {item.cullMode}");
            sb.AppendLine($"Classification Reason: {item.classificationReason}");
            return sb.ToString();
        }

        public static void CopySlotsToClipboard(List<HDVisualAuditSlotInfo> slots, string label)
        {
            if (slots == null || slots.Count == 0)
            {
                GUIUtility.systemCopyBuffer = $"No {label} to copy.";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"HD VISUAL MATERIAL AUDIT - {label.ToUpper()}");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("================================================================================");
            sb.AppendLine("");

            foreach (var s in slots)
            {
                if (s == null) continue;
                sb.AppendLine(FormatSlotInfo(s));
                sb.AppendLine("--------------------------------------------------------------------------------");
            }

            GUIUtility.systemCopyBuffer = sb.ToString();
            EditorUtility.DisplayDialog("Copy to Clipboard", $"Copied {slots.Count} items ({label}) to system clipboard.", "OK");
        }

        public static string SaveReportToFile(HDEnvironmentVisualAuditReport report)
        {
            string dir = "Assets/AILevelBuilder/Reports";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "HDEnvironmentVisualMaterialAuditReport.txt");
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("HD ENVIRONMENT VISUAL MATERIAL AUDIT REPORT");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Target: {report.targetHierarchyPath}");
            sb.AppendLine($"Total Renderers: {report.renderersFound}");
            sb.AppendLine($"Total Material Slots: {report.totalMaterialSlots}");
            sb.AppendLine($"URP Compatible: {report.urpCompatible}");
            sb.AppendLine($"Built-in Standard: {report.builtinStandard}");
            sb.AppendLine($"Magenta Risk: {report.magentaSuspects}");
            sb.AppendLine($"Total Warnings: {report.totalWarnings}");
            sb.AppendLine($"Total Errors: {report.totalErrors}");
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("");

            foreach (var s in report.allSlots)
            {
                if (s == null) continue;
                sb.AppendLine(FormatSlotInfo(s));
                sb.AppendLine("--------------------------------------------------------------------------------");
            }

            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
            return path;
        }

        public static string SaveWarningsReportToFile(HDEnvironmentVisualAuditReport report)
        {
            string dir = "Assets/AILevelBuilder/Reports";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "HDEnvironmentVisualMaterialAudit_WarningsOnly.txt");
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("HD ENVIRONMENT VISUAL MATERIAL AUDIT - WARNINGS & ERRORS ONLY");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Total Warnings: {report.totalWarnings}");
            sb.AppendLine($"Total Errors: {report.totalErrors}");
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("");

            foreach (var s in report.errorSlots.Concat(report.warningSlots))
            {
                if (s == null) continue;
                sb.AppendLine(FormatSlotInfo(s));
                sb.AppendLine("--------------------------------------------------------------------------------");
            }

            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
            return path;
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
    }
}
