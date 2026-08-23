using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonkeyAdventure.AILevelBuilder
{
    [Serializable]
    public class SceneSlotAuditItem
    {
        public string gameObjectName;
        public string hierarchyPath;
        public string prefabSourceName;
        public string rendererName;
        public string rendererType;
        public int slotIndex;
        public string materialName;
        public string materialPath;
        public string shaderName;
        public string shaderPath;
        public bool isShaderNull;
        public bool isMaterialNull;
        public bool isStandardShader;
        public bool isInternalErrorShader;
        public bool isURPLit;
        public bool isVisible;
        public bool isMagentaSuspect;
        public string suspectReason;
    }

    [Serializable]
    public class ScenePreviewMagentaAuditReport
    {
        public int totalRenderers = 0;
        public int totalMaterialSlots = 0;
        public int urpCompatibleSlots = 0;
        public int builtInStandardSlots = 0;
        public int missingShaderSlots = 0;
        public int missingMaterialSlots = 0;
        public int internalErrorShaderSlots = 0;
        public int otherIncompatibleSlots = 0;
        public int magentaSuspects = 0;

        public List<SceneSlotAuditItem> slotItems = new List<SceneSlotAuditItem>();
        public List<SceneSlotAuditItem> magentaSuspectItems = new List<SceneSlotAuditItem>();

        public void AddSlot(SceneSlotAuditItem item)
        {
            slotItems.Add(item);
            totalMaterialSlots++;

            if (item.isMagentaSuspect)
            {
                magentaSuspects++;
                magentaSuspectItems.Add(item);
            }

            if (item.isURPLit)
            {
                urpCompatibleSlots++;
            }
            else if (item.isMaterialNull)
            {
                missingMaterialSlots++;
            }
            else if (item.isShaderNull)
            {
                missingShaderSlots++;
            }
            else if (item.isInternalErrorShader)
            {
                internalErrorShaderSlots++;
            }
            else if (item.isStandardShader)
            {
                builtInStandardSlots++;
            }
            else
            {
                otherIncompatibleSlots++;
            }
        }
    }

    /// <summary>
    /// Deep recursive diagnostic tool that inspects every child GameObject and Renderer material slot
    /// in the active scene preview (AI_GENERATED_LEVEL/HD_REPLACEMENTS_PREVIEW) to identify exact magenta causes.
    /// </summary>
    public static class HDAssetMaterialDiagnostic
    {
        /// <summary>
        /// Audits the instantiated scene preview hierarchy (HD_REPLACEMENTS_PREVIEW or HD_REPLACEMENTS)
        /// and evaluates EVERY material slot on EVERY Renderer component.
        /// </summary>
        public static ScenePreviewMagentaAuditReport ScanCurrentPreview()
        {
            ScenePreviewMagentaAuditReport report = new ScenePreviewMagentaAuditReport();

            GameObject levelRoot = GameObject.Find(LevelGenerator.ROOT_NAME);
            Transform previewRoot = null;

            if (levelRoot != null)
            {
                previewRoot = levelRoot.transform.Find(HDAssetAutoReplacer.PREVIEW_ROOT_NAME) ??
                              levelRoot.transform.Find(HDAssetAutoReplacer.HD_ROOT_NAME);
            }

            if (previewRoot == null)
            {
                GameObject standaloneRoot = GameObject.Find(HDAssetAutoReplacer.PREVIEW_ROOT_NAME) ??
                                           GameObject.Find(HDAssetAutoReplacer.HD_ROOT_NAME);
                if (standaloneRoot != null) previewRoot = standaloneRoot.transform;
            }

            if (previewRoot == null)
            {
                Debug.LogWarning("[HDAssetMaterialDiagnostic] No active HD_REPLACEMENTS_PREVIEW or HD_REPLACEMENTS hierarchy found in scene.");
                return report;
            }

            Renderer[] renderers = previewRoot.GetComponentsInChildren<Renderer>(true);
            report.totalRenderers = renderers.Length;

            foreach (var rend in renderers)
            {
                AuditRendererMaterialSlots(rend, report);
            }

            PrintSceneAuditReport(report);
            return report;
        }

        private static void AuditRendererMaterialSlots(Renderer rend, ScenePreviewMagentaAuditReport report)
        {
            if (rend == null) return;

            GameObject go = rend.gameObject;
            string hierPath = GetHierarchyPath(go.transform);
            string prefabSrc = "";

#if UNITY_EDITOR
            var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (src != null) prefabSrc = src.name;
#endif

            Material[] sharedMats = rend.sharedMaterials;
            if (sharedMats == null || sharedMats.Length == 0)
            {
                var item = new SceneSlotAuditItem
                {
                    gameObjectName = go.name,
                    hierarchyPath = hierPath,
                    prefabSourceName = prefabSrc,
                    rendererName = rend.name,
                    rendererType = rend.GetType().Name,
                    slotIndex = 0,
                    materialName = "(None)",
                    materialPath = "",
                    shaderName = "(None)",
                    shaderPath = "",
                    isShaderNull = true,
                    isMaterialNull = true,
                    isStandardShader = false,
                    isInternalErrorShader = false,
                    isURPLit = false,
                    isVisible = rend.enabled && go.activeInHierarchy,
                    isMagentaSuspect = true,
                    suspectReason = "Renderer has empty materials array."
                };
                report.AddSlot(item);
                return;
            }

            for (int i = 0; i < sharedMats.Length; i++)
            {
                Material m = sharedMats[i];
                var item = new SceneSlotAuditItem
                {
                    gameObjectName = go.name,
                    hierarchyPath = hierPath,
                    prefabSourceName = prefabSrc,
                    rendererName = rend.name,
                    rendererType = rend.GetType().Name,
                    slotIndex = i,
                    isVisible = rend.enabled && go.activeInHierarchy
                };

                if (m == null)
                {
                    item.materialName = $"(Null Material Slot {i})";
                    item.materialPath = "";
                    item.shaderName = "(Null)";
                    item.shaderPath = "";
                    item.isMaterialNull = true;
                    item.isShaderNull = true;
                    item.isMagentaSuspect = true;
                    item.suspectReason = $"Material slot {i} is NULL/missing.";
                }
                else
                {
                    item.materialName = m.name;
#if UNITY_EDITOR
                    item.materialPath = AssetDatabase.GetAssetPath(m);
#endif
                    Shader s = m.shader;
                    if (s == null)
                    {
                        item.shaderName = "(Missing Shader)";
                        item.shaderPath = "";
                        item.isShaderNull = true;
                        item.isMagentaSuspect = true;
                        item.suspectReason = "Material has missing/null shader reference.";
                    }
                    else
                    {
                        item.shaderName = s.name;
#if UNITY_EDITOR
                        item.shaderPath = AssetDatabase.GetAssetPath(s);
#endif
                        string sLower = s.name.ToLowerInvariant();

                        item.isInternalErrorShader = s.name.Contains("InternalError") || s.name.Equals("Hidden/InternalErrorShader");
                        item.isStandardShader = s.name.Equals("Standard", StringComparison.OrdinalIgnoreCase) ||
                                                s.name.Equals("Standard (Specular setup)", StringComparison.OrdinalIgnoreCase) ||
                                                s.name.StartsWith("Mobile/", StringComparison.OrdinalIgnoreCase) ||
                                                s.name.StartsWith("Legacy Shaders/", StringComparison.OrdinalIgnoreCase) ||
                                                s.name.Contains("SupercyanShader");

                        item.isURPLit = s.name.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase) ||
                                        s.name.StartsWith("URP/", StringComparison.OrdinalIgnoreCase) ||
                                        s.name.StartsWith("Shader Graphs/", StringComparison.OrdinalIgnoreCase);

                        if (item.isInternalErrorShader)
                        {
                            item.isMagentaSuspect = true;
                            item.suspectReason = "Shader compilation failed (InternalErrorShader). Direct cause of MAGENTA pixel rendering.";
                        }
                        else if (item.isStandardShader)
                        {
                            item.isMagentaSuspect = true;
                            item.suspectReason = $"Built-in pipeline shader '{s.name}' lacks URP forward pass. Direct cause of MAGENTA pixel rendering.";
                        }
                        else if (!item.isURPLit && GraphicsSettings.currentRenderPipeline != null)
                        {
                            item.isMagentaSuspect = true;
                            item.suspectReason = $"Custom shader '{s.name}' not targeting URP pipeline.";
                        }
                        else
                        {
                            item.isMagentaSuspect = false;
                            item.suspectReason = "Valid URP shader.";
                        }
                    }
                }

                report.AddSlot(item);
            }
        }

        private static string GetHierarchyPath(Transform t)
        {
            if (t == null) return "";
            string path = t.name;
            Transform parent = t.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        private static void PrintSceneAuditReport(ScenePreviewMagentaAuditReport report)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b><color=#FF5555>==================================================</color></b>");
            sb.AppendLine("<b><color=#FFCC00>MAGENTA RENDERER AUDIT (SCENE PREVIEW)</color></b>");
            sb.AppendLine("<b><color=#FF5555>==================================================</color></b>");
            sb.AppendLine($"Total Renderers: {report.totalRenderers}");
            sb.AppendLine($"Total Material Slots: {report.totalMaterialSlots}");
            sb.AppendLine($"URP Compatible: <color=#00FF88>{report.urpCompatibleSlots}</color>");
            sb.AppendLine($"Built-in Standard: <color=#FF3366>{report.builtInStandardSlots}</color>");
            sb.AppendLine($"Missing Shader: <color=#FF3366>{report.missingShaderSlots}</color>");
            sb.AppendLine($"Missing Material: <color=#FF8800>{report.missingMaterialSlots}</color>");
            sb.AppendLine($"Internal Error Shader: <color=#FF0000>{report.internalErrorShaderSlots}</color>");
            sb.AppendLine($"Other Incompatible: <color=#FF8800>{report.otherIncompatibleSlots}</color>");
            sb.AppendLine($"<b>Magenta Suspects: <color={(report.magentaSuspects > 0 ? "#FF0000" : "#00FF88")}>{report.magentaSuspects}</color></b>");
            sb.AppendLine("--------------------------------------------------");

            if (report.magentaSuspectItems.Count > 0)
            {
                sb.AppendLine("<b>SUSPECT DETAILS:</b>");
                foreach (var item in report.magentaSuspectItems)
                {
                    sb.AppendLine($"\n• <b>OBJECT:</b> {item.gameObjectName} (Path: {item.hierarchyPath})");
                    sb.AppendLine($"  <b>RENDERER:</b> {item.rendererName} [{item.rendererType}] (Slot {item.slotIndex})");
                    sb.AppendLine($"  <b>MATERIAL:</b> {item.materialName}");
                    sb.AppendLine($"  <b>SHADER:</b> {item.shaderName}");
                    sb.AppendLine($"  <b>MATERIAL PATH:</b> {item.materialPath}");
                    sb.AppendLine($"  <b>REASON:</b> <color=#FF3366>{item.suspectReason}</color>");
                }
            }
            else
            {
                sb.AppendLine("<color=#00FF88><b>✓ 0 Magenta Suspects! All preview renderer material slots are 100% URP-compatible.</b></color>");
            }

            Debug.Log(sb.ToString());
        }

#if UNITY_EDITOR
        [MenuItem("Window/Monkey Adventure/HD Asset Material Diagnostic/Scan CURRENT PREVIEW", false, 121)]
        public static void MenuScanCurrentPreview()
        {
            var report = ScanCurrentPreview();
            EditorUtility.DisplayDialog("Scene Preview Magenta Audit",
                $"Audit Complete:\n\n" +
                $"• Total Renderers: {report.totalRenderers}\n" +
                $"• Total Material Slots: {report.totalMaterialSlots}\n" +
                $"• URP Compatible: {report.urpCompatibleSlots}\n" +
                $"• Built-in Standard: {report.builtInStandardSlots}\n" +
                $"• Missing Material/Shader: {report.missingMaterialSlots + report.missingShaderSlots}\n" +
                $"• Magenta Suspects: {report.magentaSuspects}\n\n" +
                (report.magentaSuspects > 0 ? "Inspect Unity Console for itemized suspect list." : "Preview is 100% clean of magenta rendering!"), "OK");
        }

        [MenuItem("Window/Monkey Adventure/HD Asset Material Diagnostic/Auto-Fix Preview Materials", false, 122)]
        public static void MenuAutoFixPreviewMaterials()
        {
            HDMaterialURPConverter.ApplyConvertedMaterialsToPreview();
            var report = ScanCurrentPreview();
            EditorUtility.DisplayDialog("Auto-Fix Preview Materials",
                $"Auto-Fix Executed:\n\n" +
                $"• Magenta Suspects Remaining: {report.magentaSuspects}\n" +
                $"• URP Compatible Slots: {report.urpCompatibleSlots}/{report.totalMaterialSlots}\n\n" +
                (report.magentaSuspects == 0 ? "All preview materials successfully upgraded to URP!" : "Some slots need manual inspection."), "OK");
        }
#endif
    }
}
