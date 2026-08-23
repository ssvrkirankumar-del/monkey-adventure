using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonkeyAdventure.AILevelBuilder
{
    public enum HDMaterialClassification
    {
        URPLit,
        URPSimpleLit,
        URPCompatible,
        BuiltInStandard,
        InternalErrorShader,
        MissingShader,
        MissingMaterial,
        Unknown
    }

    [Serializable]
    public class ActiveHDMaterialSlotInfo
    {
        public string gameObjectName;
        public string hierarchyPath;
        public bool isActiveInHierarchy;
        public string rendererName;
        public string rendererType;
        public int slotIndex;
        public string materialName;
        public string materialAssetPath;
        public string shaderName;
        public string shaderAssetPath;
        public HDMaterialClassification classification;
        public bool isSuspect;
        public string suspectReason;
        public string recommendedURPMaterial;

        // Render States & Keywords
        public bool hasBaseMap;
        public Color baseColor = Color.white;
        public bool alphaClip;
        public float cutoff = 0.5f;
        public float cullMode = 2f; // 0=Off/TwoSided, 1=Front, 2=Back
        public string surfaceType = "Opaque";
        public string activeKeywords = "";
    }

    [Serializable]
    public class ActiveHDAuditReport
    {
        public int totalActiveHDObjects = 0;
        public int totalRenderers = 0;
        public int totalMaterialSlots = 0;
        public int urpLitSlots = 0;
        public int urpSimpleLitSlots = 0;
        public int urpCompatibleSlots = 0;
        public int builtInStandardSlots = 0;
        public int internalErrorShaderSlots = 0;
        public int missingShaderSlots = 0;
        public int missingMaterialSlots = 0;
        public int unknownShaderSlots = 0;
        public int magentaSuspects = 0;

        public List<ActiveHDMaterialSlotInfo> allSlots = new List<ActiveHDMaterialSlotInfo>();
        public List<ActiveHDMaterialSlotInfo> suspects = new List<ActiveHDMaterialSlotInfo>();

        public void AddSlot(ActiveHDMaterialSlotInfo slot)
        {
            allSlots.Add(slot);
            totalMaterialSlots++;

            switch (slot.classification)
            {
                case HDMaterialClassification.URPLit:
                    urpLitSlots++;
                    urpCompatibleSlots++;
                    break;
                case HDMaterialClassification.URPSimpleLit:
                    urpSimpleLitSlots++;
                    urpCompatibleSlots++;
                    break;
                case HDMaterialClassification.URPCompatible:
                    urpCompatibleSlots++;
                    break;
                case HDMaterialClassification.BuiltInStandard:
                    builtInStandardSlots++;
                    break;
                case HDMaterialClassification.InternalErrorShader:
                    internalErrorShaderSlots++;
                    break;
                case HDMaterialClassification.MissingShader:
                    missingShaderSlots++;
                    break;
                case HDMaterialClassification.MissingMaterial:
                    missingMaterialSlots++;
                    break;
                default:
                    unknownShaderSlots++;
                    break;
            }

            if (slot.isSuspect)
            {
                magentaSuspects++;
                suspects.Add(slot);
            }
        }
    }

    /// <summary>
    /// Read-only diagnostic system that audits the active AI_GENERATED_LEVEL/HD_REPLACEMENTS hierarchy
    /// to report renderer types, LOD groups, material slots, shader states, keywords, and magenta suspects.
    /// </summary>
    public static class HDActiveReplacementAudit
    {
        public const string REPORT_DIR = "Assets/AILevelBuilder/Reports";
        public const string REPORT_PATH = "Assets/AILevelBuilder/Reports/ActiveHDMaterialAudit.txt";

        /// <summary>
        /// Executes a read-only audit on AI_GENERATED_LEVEL/HD_REPLACEMENTS and saves the report.
        /// </summary>
        public static ActiveHDAuditReport RunAudit()
        {
            ActiveHDAuditReport report = new ActiveHDAuditReport();

            GameObject levelRoot = GameObject.Find(LevelGenerator.ROOT_NAME);
            Transform hdRoot = null;

            if (levelRoot != null)
            {
                hdRoot = levelRoot.transform.Find(HDAssetAutoReplacer.HD_ROOT_NAME);
            }
            else
            {
                GameObject standalone = GameObject.Find(HDAssetAutoReplacer.HD_ROOT_NAME);
                if (standalone != null) hdRoot = standalone.transform;
            }

            if (hdRoot == null)
            {
                Debug.LogWarning($"[HDActiveReplacementAudit] '{HDAssetAutoReplacer.HD_ROOT_NAME}' hierarchy not found under '{LevelGenerator.ROOT_NAME}'.");
                SaveReportToFile(report, false);
                return report;
            }

            // Count total GameObjects recursively
            Transform[] allTransforms = hdRoot.GetComponentsInChildren<Transform>(true);
            report.totalActiveHDObjects = allTransforms.Length;

            // Recursively inspect all Renderers (MeshRenderer, SkinnedMeshRenderer, etc.)
            Renderer[] renderers = hdRoot.GetComponentsInChildren<Renderer>(true);
            report.totalRenderers = renderers.Length;

            foreach (Renderer rend in renderers)
            {
                AuditRenderer(rend, report);
            }

            // Print summary to console
            PrintAuditSummary(report);

            // Save report text file
            SaveReportToFile(report, true);

            return report;
        }

        private static void AuditRenderer(Renderer rend, ActiveHDAuditReport report)
        {
            if (rend == null) return;

            GameObject go = rend.gameObject;
            string hierPath = GetHierarchyPath(go.transform);

            Material[] sharedMats = rend.sharedMaterials;
            if (sharedMats == null || sharedMats.Length == 0)
            {
                var slotInfo = new ActiveHDMaterialSlotInfo
                {
                    gameObjectName = go.name,
                    hierarchyPath = hierPath,
                    isActiveInHierarchy = go.activeInHierarchy && rend.enabled,
                    rendererName = rend.name,
                    rendererType = rend.GetType().Name,
                    slotIndex = 0,
                    materialName = "(None)",
                    materialAssetPath = "",
                    shaderName = "(None)",
                    shaderAssetPath = "",
                    classification = HDMaterialClassification.MissingMaterial,
                    isSuspect = true,
                    suspectReason = "Renderer has no materials assigned.",
                    recommendedURPMaterial = RecommendURPMaterial(go.name, "")
                };
                report.AddSlot(slotInfo);
                return;
            }

            for (int i = 0; i < sharedMats.Length; i++)
            {
                Material m = sharedMats[i];
                var slotInfo = new ActiveHDMaterialSlotInfo
                {
                    gameObjectName = go.name,
                    hierarchyPath = hierPath,
                    isActiveInHierarchy = go.activeInHierarchy && rend.enabled,
                    rendererName = rend.name,
                    rendererType = rend.GetType().Name,
                    slotIndex = i
                };

                if (m == null)
                {
                    slotInfo.materialName = $"(Null Material Slot {i})";
                    slotInfo.materialAssetPath = "";
                    slotInfo.shaderName = "(Null)";
                    slotInfo.shaderAssetPath = "";
                    slotInfo.classification = HDMaterialClassification.MissingMaterial;
                    slotInfo.isSuspect = true;
                    slotInfo.suspectReason = $"Material slot {i} is NULL.";
                    slotInfo.recommendedURPMaterial = RecommendURPMaterial(go.name, "");
                }
                else
                {
                    slotInfo.materialName = m.name;
#if UNITY_EDITOR
                    slotInfo.materialAssetPath = AssetDatabase.GetAssetPath(m);
#endif
                    Shader s = m.shader;
                    if (s == null)
                    {
                        slotInfo.shaderName = "(Missing Shader)";
                        slotInfo.shaderAssetPath = "";
                        slotInfo.classification = HDMaterialClassification.MissingShader;
                        slotInfo.isSuspect = true;
                        slotInfo.suspectReason = "Material has missing/null shader reference.";
                        slotInfo.recommendedURPMaterial = RecommendURPMaterial(go.name, m.name);
                    }
                    else
                    {
                        slotInfo.shaderName = s.name;
#if UNITY_EDITOR
                        slotInfo.shaderAssetPath = AssetDatabase.GetAssetPath(s);
#endif
                        ClassifyShader(m, s, slotInfo);
                        InspectMaterialProperties(m, slotInfo);
                    }
                }

                report.AddSlot(slotInfo);
            }
        }

        private static void ClassifyShader(Material m, Shader s, ActiveHDMaterialSlotInfo info)
        {
            string sName = s.name;

            if (sName.Contains("InternalError") || sName.Equals("Hidden/InternalErrorShader", StringComparison.OrdinalIgnoreCase))
            {
                info.classification = HDMaterialClassification.InternalErrorShader;
                info.isSuspect = true;
                info.suspectReason = "Shader compilation failed (InternalErrorShader). Renders as bright MAGENTA.";
                info.recommendedURPMaterial = RecommendURPMaterial(info.gameObjectName, m.name);
            }
            else if (sName.Equals("Universal Render Pipeline/Lit", StringComparison.OrdinalIgnoreCase))
            {
                info.classification = HDMaterialClassification.URPLit;
                info.isSuspect = false;
                info.suspectReason = "Valid URP Lit shader.";
            }
            else if (sName.Equals("Universal Render Pipeline/Simple Lit", StringComparison.OrdinalIgnoreCase))
            {
                info.classification = HDMaterialClassification.URPSimpleLit;
                info.isSuspect = false;
                info.suspectReason = "Valid URP Simple Lit shader.";
            }
            else if (sName.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase) ||
                     sName.StartsWith("URP/", StringComparison.OrdinalIgnoreCase) ||
                     sName.StartsWith("Shader Graphs/", StringComparison.OrdinalIgnoreCase))
            {
                info.classification = HDMaterialClassification.URPCompatible;
                info.isSuspect = false;
                info.suspectReason = "Valid URP compatible shader.";
            }
            else if (sName.Equals("Standard", StringComparison.OrdinalIgnoreCase) ||
                     sName.Equals("Standard (Specular setup)", StringComparison.OrdinalIgnoreCase) ||
                     sName.StartsWith("Mobile/", StringComparison.OrdinalIgnoreCase) ||
                     sName.StartsWith("Legacy Shaders/", StringComparison.OrdinalIgnoreCase) ||
                     sName.Contains("SupercyanShader"))
            {
                info.classification = HDMaterialClassification.BuiltInStandard;
                info.isSuspect = true;
                info.suspectReason = $"Built-in pipeline shader '{sName}' is incompatible with URP. Renders as bright MAGENTA.";
                info.recommendedURPMaterial = RecommendURPMaterial(info.gameObjectName, m.name);
            }
            else
            {
                if (GraphicsSettings.currentRenderPipeline != null)
                {
                    info.classification = HDMaterialClassification.Unknown;
                    info.isSuspect = true;
                    info.suspectReason = $"Non-URP shader '{sName}' in active URP project.";
                    info.recommendedURPMaterial = RecommendURPMaterial(info.gameObjectName, m.name);
                }
                else
                {
                    info.classification = HDMaterialClassification.Unknown;
                    info.isSuspect = false;
                }
            }
        }

        private static void InspectMaterialProperties(Material m, ActiveHDMaterialSlotInfo info)
        {
            if (m.HasProperty("_BaseMap"))
            {
                info.hasBaseMap = m.GetTexture("_BaseMap") != null;
            }
            else if (m.HasProperty("_MainTex"))
            {
                info.hasBaseMap = m.GetTexture("_MainTex") != null;
            }

            if (m.HasProperty("_BaseColor"))
            {
                info.baseColor = m.GetColor("_BaseColor");
            }
            else if (m.HasProperty("_Color"))
            {
                info.baseColor = m.GetColor("_Color");
            }

            if (m.HasProperty("_AlphaClip"))
            {
                info.alphaClip = Mathf.Approximately(m.GetFloat("_AlphaClip"), 1f);
            }
            if (m.HasProperty("_Cutoff"))
            {
                info.cutoff = m.GetFloat("_Cutoff");
            }
            if (m.HasProperty("_Cull"))
            {
                info.cullMode = m.GetFloat("_Cull");
            }
            if (m.HasProperty("_Surface"))
            {
                info.surfaceType = Mathf.Approximately(m.GetFloat("_Surface"), 1f) ? "Transparent" : "Opaque";
            }

            info.activeKeywords = string.Join(", ", m.shaderKeywords);
        }

        private static string RecommendURPMaterial(string goName, string matName)
        {
            string combined = (goName + " " + matName).ToLowerInvariant();

            if (combined.Contains("leaf") || combined.Contains("canopy"))
                return "Assets/AILevelBuilder/HD/URPMaterials/forestpack_tree_leaf_URP.mat";

            if (combined.Contains("stump") || combined.Contains("trunk") || combined.Contains("root"))
                return "Assets/AILevelBuilder/HD/URPMaterials/forestpack_treeStumpAndRoot_URP.mat";

            if (combined.Contains("stone") || combined.Contains("rock"))
                return "Assets/AILevelBuilder/HD/URPMaterials/forestpack_stone_URP.mat";

            if (combined.Contains("fir"))
                return "Assets/AILevelBuilder/HD/URPMaterials/forestpack_tree_fir_URP.mat";

            if (combined.Contains("grass") || combined.Contains("foliage") || combined.Contains("bush") || combined.Contains("mushroom"))
                return "Assets/AILevelBuilder/HD/URPMaterials/forestpack_foliage_URP.mat";

            return "Universal Render Pipeline/Lit Material";
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

        private static void PrintAuditSummary(ActiveHDAuditReport report)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b><color=#FFCC00>==================================================</color></b>");
            sb.AppendLine("<b><color=#FFCC00>ACTIVE HD MATERIAL AUDIT</color></b>");
            sb.AppendLine("<b><color=#FFCC00>==================================================</color></b>");
            sb.AppendLine($"Status: <b>{(report.magentaSuspects == 0 ? "<color=#00FF88>CLEAN</color>" : "<color=#FF3366>NEEDS FIX</color>")}</b>");
            sb.AppendLine($"Total Active HD Objects: {report.totalActiveHDObjects}");
            sb.AppendLine($"Renderers: {report.totalRenderers}");
            sb.AppendLine($"Material Slots: {report.totalMaterialSlots}");
            sb.AppendLine($"URP Compatible: <color=#00FF88>{report.urpCompatibleSlots}</color> (Lit: {report.urpLitSlots}, Simple Lit: {report.urpSimpleLitSlots})");
            sb.AppendLine($"Standard: <color=#FF3366>{report.builtInStandardSlots}</color>");
            sb.AppendLine($"InternalError: <color=#FF0000>{report.internalErrorShaderSlots}</color>");
            sb.AppendLine($"Missing: <color=#FF8800>{report.missingMaterialSlots + report.missingShaderSlots}</color>");
            sb.AppendLine($"<b>Magenta Suspects: <color={(report.magentaSuspects > 0 ? "#FF0000" : "#00FF88")}>{report.magentaSuspects}</color></b>");
            sb.AppendLine("--------------------------------------------------");

            if (report.suspects.Count > 0)
            {
                sb.AppendLine("<b>SUSPECTS IDENTIFIED:</b>");
                foreach (var s in report.suspects)
                {
                    sb.AppendLine($"• GameObject: {s.gameObjectName}");
                    sb.AppendLine($"  Hierarchy: {s.hierarchyPath}");
                    sb.AppendLine($"  Renderer: {s.rendererName} [{s.rendererType}] (Slot {s.slotIndex})");
                    sb.AppendLine($"  Material: {s.materialName}");
                    sb.AppendLine($"  Material Asset Path: {s.materialAssetPath}");
                    sb.AppendLine($"  Shader: {s.shaderName}");
                    sb.AppendLine($"  Reason: <color=#FF3366>{s.suspectReason}</color>");
                    sb.AppendLine($"  Recommended URP Material: {s.recommendedURPMaterial}\n");
                }
            }
            else
            {
                sb.AppendLine("<color=#00FF88>✓ 0 Magenta Suspects. All active HD environment renderers are 100% URP compliant.</color>");
            }

            Debug.Log(sb.ToString());
        }

        private static void SaveReportToFile(ActiveHDAuditReport report, bool foundRoot)
        {
            if (!Directory.Exists(REPORT_DIR))
            {
                Directory.CreateDirectory(REPORT_DIR);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("ACTIVE HD MATERIAL AUDIT REPORT (READ-ONLY)");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("================================================================================");

            if (!foundRoot)
            {
                sb.AppendLine($"STATUS: NOT FOUND");
                sb.AppendLine($"Hierarchy 'AI_GENERATED_LEVEL/{HDAssetAutoReplacer.HD_ROOT_NAME}' not present in scene.");
            }
            else
            {
                sb.AppendLine($"Status: {(report.magentaSuspects == 0 ? "CLEAN" : "NEEDS FIX")}");
                sb.AppendLine($"Total Active HD Objects: {report.totalActiveHDObjects}");
                sb.AppendLine($"Renderers: {report.totalRenderers}");
                sb.AppendLine($"Material Slots: {report.totalMaterialSlots}");
                sb.AppendLine($"URP Compatible: {report.urpCompatibleSlots} (Lit: {report.urpLitSlots}, Simple Lit: {report.urpSimpleLitSlots})");
                sb.AppendLine($"Standard: {report.builtInStandardSlots}");
                sb.AppendLine($"InternalError: {report.internalErrorShaderSlots}");
                sb.AppendLine($"Missing Shader: {report.missingShaderSlots}");
                sb.AppendLine($"Missing Material: {report.missingMaterialSlots}");
                sb.AppendLine($"Unknown: {report.unknownShaderSlots}");
                sb.AppendLine($"Magenta Suspects: {report.magentaSuspects}");
                sb.AppendLine("--------------------------------------------------------------------------------");

                if (report.suspects.Count > 0)
                {
                    sb.AppendLine("SUSPECT DETAILS:");
                    foreach (var s in report.suspects)
                    {
                        sb.AppendLine($"GameObject: {s.gameObjectName}");
                        sb.AppendLine($"Hierarchy: {s.hierarchyPath}");
                        sb.AppendLine($"Renderer: {s.rendererName} [{s.rendererType}] (Slot {s.slotIndex})");
                        sb.AppendLine($"Material: {s.materialName}");
                        sb.AppendLine($"Material Asset Path: {s.materialAssetPath}");
                        sb.AppendLine($"Shader: {s.shaderName}");
                        sb.AppendLine($"Reason: {s.suspectReason}");
                        sb.AppendLine($"Recommended URP Material: {s.recommendedURPMaterial}");
                        sb.AppendLine();
                    }
                }
                else
                {
                    sb.AppendLine("All material slots in AI_GENERATED_LEVEL/HD_REPLACEMENTS are 100% URP compliant.");
                }

                sb.AppendLine("--------------------------------------------------------------------------------");
                sb.AppendLine("DETAILED SLOT INVENTORY:");
                foreach (var slot in report.allSlots)
                {
                    sb.AppendLine($"[{slot.classification,-18}] {slot.gameObjectName,-24} | Rend: {slot.rendererName,-20} Slot: {slot.slotIndex} | Mat: {slot.materialName,-30} | Shader: {slot.shaderName}");
                }
            }

            File.WriteAllText(REPORT_PATH, sb.ToString());
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

#if UNITY_EDITOR
        [MenuItem("Window/Monkey Adventure/HD Asset Material Diagnostic/Scan ACTIVE HD REPLACEMENTS", false, 123)]
        public static void MenuScanActiveHDReplacements()
        {
            var report = RunAudit();
            EditorUtility.DisplayDialog("Active HD Replacements Audit",
                $"Active HD Audit Complete:\n\n" +
                $"• Status: {(report.magentaSuspects == 0 ? "CLEAN" : "NEEDS FIX")}\n" +
                $"• Renderers: {report.totalRenderers}\n" +
                $"• Material Slots: {report.totalMaterialSlots}\n" +
                $"• URP Compatible: {report.urpCompatibleSlots}\n" +
                $"• Built-in Standard: {report.builtInStandardSlots}\n" +
                $"• Magenta Suspects: {report.magentaSuspects}\n\n" +
                $"Report saved to:\n{REPORT_PATH}", "OK");
        }
#endif
    }
}
