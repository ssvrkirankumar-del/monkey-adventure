using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonkeyAdventure.AILevelBuilder
{
    public enum MagentaSuspectSeverity
    {
        Clean,
        DefiniteMagentaRisk,
        PossibleMagenta,
        GizmoOrDebugVisual
    }

    [Serializable]
    public class FullSceneObjectAuditItem
    {
        public string gameObjectName;
        public string hierarchyPath;
        public string rootSceneObjectName;
        public string rendererType;
        public int slotIndex;
        public string materialName;
        public string materialAssetPath;
        public string shaderName;
        public string shaderAssetPath;
        public string shaderKeywords;
        public Color materialColor = Color.white;
        public Color baseColor = Color.white;
        public bool hasBaseMap;
        public bool alphaClip;
        public string surfaceType = "Opaque";
        public float cullMode = 2f;
        public MagentaSuspectSeverity severity = MagentaSuspectSeverity.Clean;
        public string reason = "";
        public string possibleFix = "";
        public GameObject targetGameObject;
    }

    [Serializable]
    public class ParticleSystemAuditInfo
    {
        public string gameObjectName;
        public string hierarchyPath;
        public string materialName;
        public string shaderName;
        public Color startColor;
        public bool isActive;
        public bool isMagentaSuspect;
        public string suspectReason;
        public GameObject targetGameObject;
    }

    [Serializable]
    public class SpriteRendererAuditInfo
    {
        public string gameObjectName;
        public string hierarchyPath;
        public string spriteName;
        public string materialName;
        public string shaderName;
        public Color color;
        public bool isMagentaSuspect;
        public string suspectReason;
        public GameObject targetGameObject;
    }

    [Serializable]
    public class GizmoDebugVisualInfo
    {
        public string gameObjectName;
        public string hierarchyPath;
        public string componentName;
        public string debugType;
        public string gizmoColorDescription;
        public GameObject targetGameObject;
    }

    [Serializable]
    public class FullSceneAuditReport
    {
        public string sceneName = "";
        public string scenePath = "";
        public int totalGameObjects = 0;
        public int totalRenderers = 0;
        public int totalMaterialSlots = 0;

        public int urpCompatible = 0;
        public int builtInStandard = 0;
        public int internalError = 0;
        public int missingShader = 0;
        public int missingMaterial = 0;
        public int unknown = 0;

        public int definiteMagentaRiskCount = 0;
        public int possibleMagentaCount = 0;

        public int particleVFXMagentaCount = 0;
        public int spriteMagentaCount = 0;
        public int trailLineMagentaCount = 0;

        public int possibleGizmoDebugCount = 0;

        public List<FullSceneObjectAuditItem> definiteSuspects = new List<FullSceneObjectAuditItem>();
        public List<FullSceneObjectAuditItem> possibleSuspects = new List<FullSceneObjectAuditItem>();
        public List<ParticleSystemAuditInfo> particleSuspects = new List<ParticleSystemAuditInfo>();
        public List<SpriteRendererAuditInfo> spriteSuspects = new List<SpriteRendererAuditInfo>();
        public List<GizmoDebugVisualInfo> gizmoVisuals = new List<GizmoDebugVisualInfo>();
        public List<FullSceneObjectAuditItem> allScannedItems = new List<FullSceneObjectAuditItem>();
    }

    /// <summary>
    /// Comprehensive read-only diagnostic that scans the ENTIRE active scene for broken shaders,
    /// incompatible materials, magenta colors, VFX/particle materials, and SceneView gizmo visualizers.
    /// </summary>
    public static class FullSceneMagentaFinder
    {
        public const string REPORT_DIR = "Assets/AILevelBuilder/Reports";
        public const string REPORT_PATH = "Assets/AILevelBuilder/Reports/FullSceneMagentaAudit.txt";

        /// <summary>
        /// Executes a full-scene read-only audit across all GameObjects and Renderers.
        /// </summary>
        public static FullSceneAuditReport RunFullSceneAudit()
        {
            FullSceneAuditReport report = new FullSceneAuditReport();
            Scene activeScene = SceneManager.GetActiveScene();
            report.sceneName = activeScene.name;
            report.scenePath = activeScene.path;

            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            // 1. Gather all transforms in scene
            List<Transform> allSceneTransforms = new List<Transform>();
            foreach (GameObject root in rootObjects)
            {
                if (root != null)
                {
                    Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
                    allSceneTransforms.AddRange(transforms);
                }
            }
            report.totalGameObjects = allSceneTransforms.Count;

            // 2. Audit all Renderers
            foreach (Transform t in allSceneTransforms)
            {
                if (t == null) continue;
                GameObject go = t.gameObject;
                string rootName = GetRootName(t);

                // Audit standard renderers
                Renderer rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    report.totalRenderers++;
                    AuditRenderer(rend, go, rootName, report);
                }

                // Audit ParticleSystem
                ParticleSystem ps = go.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    AuditParticleSystem(ps, go, report);
                }

                // Audit SpriteRenderer
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    AuditSpriteRenderer(sr, go, report);
                }

                // Audit Gizmo/Debug Visual Components
                AuditGizmoDebugComponents(go, report);
            }

            // Print formatted console summary
            PrintConsoleReport(report);

            // Save report text file
            SaveReportFile(report);

            return report;
        }

        private static void AuditRenderer(Renderer rend, GameObject go, string rootName, FullSceneAuditReport report)
        {
            string hierPath = GetHierarchyPath(go.transform);
            string rType = rend.GetType().Name;

            Material[] sharedMats = rend.sharedMaterials;
            if (sharedMats == null || sharedMats.Length == 0)
            {
                var item = new FullSceneObjectAuditItem
                {
                    gameObjectName = go.name,
                    hierarchyPath = hierPath,
                    rootSceneObjectName = rootName,
                    rendererType = rType,
                    slotIndex = 0,
                    materialName = "(None)",
                    materialAssetPath = "",
                    shaderName = "(None)",
                    shaderAssetPath = "",
                    severity = MagentaSuspectSeverity.DefiniteMagentaRisk,
                    reason = "Renderer has an empty materials array.",
                    possibleFix = "Assign a valid URP/Lit material to the Renderer component.",
                    targetGameObject = go
                };

                report.totalMaterialSlots++;
                report.missingMaterial++;
                report.definiteMagentaRiskCount++;
                report.definiteSuspects.Add(item);
                report.allScannedItems.Add(item);
                return;
            }

            for (int i = 0; i < sharedMats.Length; i++)
            {
                report.totalMaterialSlots++;
                Material m = sharedMats[i];

                var item = new FullSceneObjectAuditItem
                {
                    gameObjectName = go.name,
                    hierarchyPath = hierPath,
                    rootSceneObjectName = rootName,
                    rendererType = rType,
                    slotIndex = i,
                    targetGameObject = go
                };

                if (m == null)
                {
                    item.materialName = $"(Null Slot {i})";
                    item.materialAssetPath = "";
                    item.shaderName = "(Null)";
                    item.shaderAssetPath = "";
                    item.severity = MagentaSuspectSeverity.DefiniteMagentaRisk;
                    item.reason = $"Material slot {i} is NULL/missing.";
                    item.possibleFix = "Assign a valid URP/Lit material to this material slot.";

                    report.missingMaterial++;
                    report.definiteMagentaRiskCount++;
                    report.definiteSuspects.Add(item);
                }
                else
                {
                    item.materialName = m.name;
#if UNITY_EDITOR
                    item.materialAssetPath = AssetDatabase.GetAssetPath(m);
#endif
                    Shader s = m.shader;
                    if (s == null)
                    {
                        item.shaderName = "(Missing Shader)";
                        item.shaderAssetPath = "";
                        item.severity = MagentaSuspectSeverity.DefiniteMagentaRisk;
                        item.reason = "Material shader reference is missing/broken.";
                        item.possibleFix = "Re-assign Universal Render Pipeline/Lit shader to the material asset.";

                        report.missingShader++;
                        report.definiteMagentaRiskCount++;
                        report.definiteSuspects.Add(item);
                    }
                    else
                    {
                        item.shaderName = s.name;
#if UNITY_EDITOR
                        item.shaderAssetPath = AssetDatabase.GetAssetPath(s);
#endif
                        InspectProperties(m, item);
                        EvaluateShaderAndColor(item, m, s, report);
                    }
                }

                report.allScannedItems.Add(item);
            }
        }

        private static void EvaluateShaderAndColor(FullSceneObjectAuditItem item, Material m, Shader s, FullSceneAuditReport report)
        {
            string sName = s.name;

            // 1. Check for Definite Magenta Shader Issues
            if (sName.Contains("InternalError") || sName.Equals("Hidden/InternalErrorShader", StringComparison.OrdinalIgnoreCase))
            {
                report.internalError++;
                report.definiteMagentaRiskCount++;
                item.severity = MagentaSuspectSeverity.DefiniteMagentaRisk;
                item.reason = "Shader compilation failed (InternalErrorShader). Direct cause of bright MAGENTA rendering.";
                item.possibleFix = "Upgrade material to Universal Render Pipeline/Lit.";
                report.definiteSuspects.Add(item);
                return;
            }

            if (sName.Equals("Standard", StringComparison.OrdinalIgnoreCase) ||
                sName.Equals("Standard (Specular setup)", StringComparison.OrdinalIgnoreCase) ||
                sName.StartsWith("Mobile/", StringComparison.OrdinalIgnoreCase) ||
                sName.StartsWith("Legacy Shaders/", StringComparison.OrdinalIgnoreCase) ||
                sName.Contains("SupercyanShader"))
            {
                report.builtInStandard++;
                report.definiteMagentaRiskCount++;
                item.severity = MagentaSuspectSeverity.DefiniteMagentaRisk;
                item.reason = $"Built-in pipeline shader '{sName}' lacks URP forward passes. Direct cause of bright MAGENTA rendering.";
                item.possibleFix = "Convert material to Universal Render Pipeline/Lit using the URP Material Converter.";
                report.definiteSuspects.Add(item);
                return;
            }

            // 2. Check for URP Compatibility
            if (sName.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase) ||
                sName.StartsWith("URP/", StringComparison.OrdinalIgnoreCase) ||
                sName.StartsWith("Shader Graphs/", StringComparison.OrdinalIgnoreCase) ||
                sName.StartsWith("TextMeshPro/", StringComparison.OrdinalIgnoreCase) ||
                sName.StartsWith("Skybox/", StringComparison.OrdinalIgnoreCase))
            {
                report.urpCompatible++;
                item.severity = MagentaSuspectSeverity.Clean;
                item.reason = "Valid URP shader.";
            }
            else
            {
                if (GraphicsSettings.currentRenderPipeline != null)
                {
                    report.unknown++;
                    report.definiteMagentaRiskCount++;
                    item.severity = MagentaSuspectSeverity.DefiniteMagentaRisk;
                    item.reason = $"Custom shader '{sName}' is not targeted for Universal Render Pipeline.";
                    item.possibleFix = "Check if a URP version of this custom shader is available or use URP/Lit.";
                    report.definiteSuspects.Add(item);
                    return;
                }
                else
                {
                    report.urpCompatible++;
                    item.severity = MagentaSuspectSeverity.Clean;
                }
            }

            // 3. Check for Suspicious Pink / Magenta Material Color Values
            Color checkColor = item.baseColor;
            if (checkColor.r > 0.7f && checkColor.b > 0.7f && checkColor.g < 0.3f)
            {
                report.possibleMagentaCount++;
                item.severity = MagentaSuspectSeverity.PossibleMagenta;
                item.reason = $"Material color is tinted bright pink/magenta (R={checkColor.r:F2}, G={checkColor.g:F2}, B={checkColor.b:F2}).";
                item.possibleFix = "Inspect whether this pink tint is intended or a debug color placeholder.";
                report.possibleSuspects.Add(item);
            }
        }

        private static void InspectProperties(Material m, FullSceneObjectAuditItem item)
        {
            if (m.HasProperty("_BaseMap"))
            {
                item.hasBaseMap = m.GetTexture("_BaseMap") != null;
            }
            else if (m.HasProperty("_MainTex"))
            {
                item.hasBaseMap = m.GetTexture("_MainTex") != null;
            }

            if (m.HasProperty("_BaseColor"))
            {
                item.baseColor = m.GetColor("_BaseColor");
                item.materialColor = item.baseColor;
            }
            else if (m.HasProperty("_Color"))
            {
                item.baseColor = m.GetColor("_Color");
                item.materialColor = item.baseColor;
            }

            if (m.HasProperty("_AlphaClip"))
            {
                item.alphaClip = Mathf.Approximately(m.GetFloat("_AlphaClip"), 1f);
            }
            if (m.HasProperty("_Cull"))
            {
                item.cullMode = m.GetFloat("_Cull");
            }
            if (m.HasProperty("_Surface"))
            {
                item.surfaceType = Mathf.Approximately(m.GetFloat("_Surface"), 1f) ? "Transparent" : "Opaque";
            }

            item.shaderKeywords = string.Join(", ", m.shaderKeywords);
        }

        private static void AuditParticleSystem(ParticleSystem ps, GameObject go, FullSceneAuditReport report)
        {
            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
            Material pMat = psr != null ? psr.sharedMaterial : null;
            Shader pShader = pMat != null ? pMat.shader : null;

            Color pColor = ps.main.startColor.color;
            bool isSuspect = false;
            string reason = "";

            if (pMat == null)
            {
                isSuspect = true;
                reason = "ParticleSystemRenderer has no material assigned.";
            }
            else if (pShader == null || pShader.name.Contains("InternalError") || pShader.name.StartsWith("Mobile/") || pShader.name.Equals("Standard"))
            {
                isSuspect = true;
                reason = $"Particle shader '{pShader?.name ?? "Null"}' is incompatible with URP.";
            }
            else if (pColor.r > 0.7f && pColor.b > 0.7f && pColor.g < 0.3f)
            {
                isSuspect = true;
                reason = $"Particle startColor is tinted bright pink (R={pColor.r:F2}, G={pColor.g:F2}, B={pColor.b:F2}).";
            }

            if (isSuspect)
            {
                report.particleVFXMagentaCount++;
                report.particleSuspects.Add(new ParticleSystemAuditInfo
                {
                    gameObjectName = go.name,
                    hierarchyPath = GetHierarchyPath(go.transform),
                    materialName = pMat != null ? pMat.name : "(None)",
                    shaderName = pShader != null ? pShader.name : "(None)",
                    startColor = pColor,
                    isActive = go.activeInHierarchy,
                    isMagentaSuspect = true,
                    suspectReason = reason,
                    targetGameObject = go
                });
            }
        }

        private static void AuditSpriteRenderer(SpriteRenderer sr, GameObject go, FullSceneAuditReport report)
        {
            Material sMat = sr.sharedMaterial;
            Shader sShader = sMat != null ? sMat.shader : null;
            Color sColor = sr.color;
            bool isSuspect = false;
            string reason = "";

            if (sMat == null)
            {
                isSuspect = true;
                reason = "SpriteRenderer has no material assigned.";
            }
            else if (sShader == null || sShader.name.Contains("InternalError") || sShader.name.Equals("Standard"))
            {
                isSuspect = true;
                reason = $"Sprite material shader '{sShader?.name ?? "Null"}' is incompatible with URP.";
            }
            else if (sColor.r > 0.7f && sColor.b > 0.7f && sColor.g < 0.3f)
            {
                isSuspect = true;
                reason = $"SpriteRenderer color is tinted bright pink (R={sColor.r:F2}, G={sColor.g:F2}, B={sColor.b:F2}).";
            }

            if (isSuspect)
            {
                report.spriteMagentaCount++;
                report.spriteSuspects.Add(new SpriteRendererAuditInfo
                {
                    gameObjectName = go.name,
                    hierarchyPath = GetHierarchyPath(go.transform),
                    spriteName = sr.sprite != null ? sr.sprite.name : "(None)",
                    materialName = sMat != null ? sMat.name : "(None)",
                    shaderName = sShader != null ? sShader.name : "(None)",
                    color = sColor,
                    isMagentaSuspect = true,
                    suspectReason = reason,
                    targetGameObject = go
                });
            }
        }

        private static void AuditGizmoDebugComponents(GameObject go, FullSceneAuditReport report)
        {
            // LevelMarker
            LevelMarker lm = go.GetComponent<LevelMarker>();
            if (lm != null)
            {
                report.possibleGizmoDebugCount++;
                report.gizmoVisuals.Add(new GizmoDebugVisualInfo
                {
                    gameObjectName = go.name,
                    hierarchyPath = GetHierarchyPath(go.transform),
                    componentName = "LevelMarker",
                    debugType = $"LevelMarker [{lm.MarkerType}]",
                    gizmoColorDescription = $"Draws SceneView wire sphere & direction rays for marker type {lm.MarkerType}.",
                    targetGameObject = go
                });
            }

            // Other known debug markers
            Component[] allComps = go.GetComponents<Component>();
            foreach (var c in allComps)
            {
                if (c == null) continue;
                string cName = c.GetType().Name;
                if (cName.Contains("Checkpoint") || cName.Contains("TutorialTrigger") || cName.Contains("SafeZone") ||
                    cName.Contains("WaterCurrent") || cName.Contains("MagicUpdraft") || cName.Contains("ToxicMushroom"))
                {
                    report.possibleGizmoDebugCount++;
                    report.gizmoVisuals.Add(new GizmoDebugVisualInfo
                    {
                        gameObjectName = go.name,
                        hierarchyPath = GetHierarchyPath(go.transform),
                        componentName = cName,
                        debugType = "Gameplay Trigger / Area Gizmo",
                        gizmoColorDescription = $"Component '{cName}' draws SceneView Gizmos / trigger zone wireframes.",
                        targetGameObject = go
                    });
                }
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

        private static string GetRootName(Transform t)
        {
            if (t == null) return "";
            Transform root = t.root;
            return root != null ? root.name : t.name;
        }

        private static void PrintConsoleReport(FullSceneAuditReport report)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine("FULL SCENE MAGENTA FINDER");
            sb.AppendLine("========================================");
            sb.AppendLine($"Scene: {report.sceneName}");
            sb.AppendLine($"Scene path: {report.scenePath}\n");
            sb.AppendLine($"TOTAL GAMEOBJECTS: {report.totalGameObjects}");
            sb.AppendLine($"TOTAL RENDERERS: {report.totalRenderers}");
            sb.AppendLine($"TOTAL MATERIAL SLOTS: {report.totalMaterialSlots}\n");
            sb.AppendLine($"URP COMPATIBLE: {report.urpCompatible}");
            sb.AppendLine($"BUILT-IN STANDARD: {report.builtInStandard}");
            sb.AppendLine($"INTERNAL ERROR: {report.internalError}");
            sb.AppendLine($"MISSING SHADER: {report.missingShader}");
            sb.AppendLine($"MISSING MATERIAL: {report.missingMaterial}");
            sb.AppendLine($"UNKNOWN: {report.unknown}\n");
            sb.AppendLine($"DEFINITE MAGENTA RISK: {report.definiteMagentaRiskCount}");
            sb.AppendLine($"POSSIBLE MAGENTA: {report.possibleMagentaCount}\n");
            sb.AppendLine($"PARTICLE/VFX MAGENTA: {report.particleVFXMagentaCount}");
            sb.AppendLine($"SPRITE MAGENTA: {report.spriteMagentaCount}");
            sb.AppendLine($"TRAIL/LINE MAGENTA: {report.trailLineMagentaCount}\n");
            sb.AppendLine($"POSSIBLE GIZMO/DEBUG VISUALS: {report.possibleGizmoDebugCount}");
            sb.AppendLine("========================================");

            if (report.definiteSuspects.Count > 0)
            {
                sb.AppendLine("\nDEFINITE MAGENTA SUSPECTS:");
                for (int i = 0; i < report.definiteSuspects.Count; i++)
                {
                    var s = report.definiteSuspects[i];
                    sb.AppendLine($"\n[{i + 1}]");
                    sb.AppendLine($"GameObject: {s.gameObjectName}");
                    sb.AppendLine($"Hierarchy: {s.hierarchyPath}");
                    sb.AppendLine($"Root: {s.rootSceneObjectName}");
                    sb.AppendLine($"Renderer: {s.rendererType} (Slot {s.slotIndex})");
                    sb.AppendLine($"Material: {s.materialName}");
                    sb.AppendLine($"Material Path: {s.materialAssetPath}");
                    sb.AppendLine($"Shader: {s.shaderName}");
                    sb.AppendLine($"Shader Path: {s.shaderAssetPath}");
                    sb.AppendLine($"Reason: {s.reason}");
                    sb.AppendLine($"Classification: {s.severity}");
                    sb.AppendLine($"Possible Fix: {s.possibleFix}");
                }
            }
            else
            {
                sb.AppendLine("\n<b><color=#00FF88>NO ACTUAL MAGENTA MATERIAL/SHADER FOUND IN ACTIVE SCENE.</color></b>");
            }

            if (report.possibleGizmoDebugCount > 0)
            {
                sb.AppendLine("\nPOSSIBLE SCENEVIEW GIZMO / DEBUG VISUALS:");
                sb.AppendLine("--------------------------------------------------");
                foreach (var g in report.gizmoVisuals)
                {
                    sb.AppendLine($"• {g.gameObjectName} ({g.hierarchyPath}) -> {g.debugType}: {g.gizmoColorDescription}");
                }
            }

            Debug.Log(sb.ToString());
        }

        private static void SaveReportFile(FullSceneAuditReport report)
        {
            if (!Directory.Exists(REPORT_DIR))
            {
                Directory.CreateDirectory(REPORT_DIR);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("FULL SCENE MAGENTA AUDIT REPORT (READ-ONLY)");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Scene: {report.sceneName}");
            sb.AppendLine($"Scene Path: {report.scenePath}\n");
            sb.AppendLine($"TOTAL GAMEOBJECTS:        {report.totalGameObjects}");
            sb.AppendLine($"TOTAL RENDERERS:          {report.totalRenderers}");
            sb.AppendLine($"TOTAL MATERIAL SLOTS:     {report.totalMaterialSlots}\n");
            sb.AppendLine($"URP COMPATIBLE:           {report.urpCompatible}");
            sb.AppendLine($"BUILT-IN STANDARD:        {report.builtInStandard}");
            sb.AppendLine($"INTERNAL ERROR:           {report.internalError}");
            sb.AppendLine($"MISSING SHADER:           {report.missingShader}");
            sb.AppendLine($"MISSING MATERIAL:         {report.missingMaterial}");
            sb.AppendLine($"UNKNOWN:                  {report.unknown}\n");
            sb.AppendLine($"DEFINITE MAGENTA RISK:    {report.definiteMagentaRiskCount}");
            sb.AppendLine($"POSSIBLE MAGENTA:         {report.possibleMagentaCount}");
            sb.AppendLine($"PARTICLE/VFX MAGENTA:     {report.particleVFXMagentaCount}");
            sb.AppendLine($"SPRITE MAGENTA:           {report.spriteMagentaCount}");
            sb.AppendLine($"TRAIL/LINE MAGENTA:       {report.trailLineMagentaCount}");
            sb.AppendLine($"POSSIBLE GIZMO/DEBUG:     {report.possibleGizmoDebugCount}");
            sb.AppendLine("================================================================================\n");

            if (report.definiteSuspects.Count == 0 && report.possibleSuspects.Count == 0)
            {
                sb.AppendLine("RESULT: NO ACTUAL MAGENTA MATERIAL/SHADER FOUND.");
                sb.AppendLine("Visible pink/magenta shapes in the SceneView are editor-only Gizmo handles, trigger wireframes, or icon markers, not broken renderers.");
            }
            else
            {
                if (report.definiteSuspects.Count > 0)
                {
                    sb.AppendLine("DEFINITE MAGENTA SUSPECTS:");
                    for (int i = 0; i < report.definiteSuspects.Count; i++)
                    {
                        var s = report.definiteSuspects[i];
                        sb.AppendLine($"[{i + 1}]");
                        sb.AppendLine($"GameObject: {s.gameObjectName}");
                        sb.AppendLine($"Hierarchy: {s.hierarchyPath}");
                        sb.AppendLine($"Root: {s.rootSceneObjectName}");
                        sb.AppendLine($"Renderer: {s.rendererType} (Slot {s.slotIndex})");
                        sb.AppendLine($"Material: {s.materialName}");
                        sb.AppendLine($"Material Path: {s.materialAssetPath}");
                        sb.AppendLine($"Shader: {s.shaderName}");
                        sb.AppendLine($"Shader Path: {s.shaderAssetPath}");
                        sb.AppendLine($"Reason: {s.reason}");
                        sb.AppendLine($"Classification: {s.severity}");
                        sb.AppendLine($"Possible Fix: {s.possibleFix}");
                        sb.AppendLine();
                    }
                }
            }

            File.WriteAllText(REPORT_PATH, sb.ToString());
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

#if UNITY_EDITOR
        [MenuItem("Window/Monkey Adventure/HD Asset Material Diagnostic/🔍 FULL SCENE MAGENTA FINDER", false, 124)]
        public static void MenuFullSceneMagentaFinder()
        {
            var report = RunFullSceneAudit();
            EditorUtility.DisplayDialog("Full Scene Magenta Audit",
                $"Full Scene Audit Finished:\n\n" +
                $"• Total GameObjects: {report.totalGameObjects}\n" +
                $"• Total Renderers: {report.totalRenderers}\n" +
                $"• Material Slots: {report.totalMaterialSlots}\n" +
                $"• Definite Magenta Risk: {report.definiteMagentaRiskCount}\n" +
                $"• Possible Magenta: {report.possibleMagentaCount}\n" +
                $"• Gizmo / Debug Visuals: {report.possibleGizmoDebugCount}\n\n" +
                (report.definiteMagentaRiskCount == 0 ? "NO ACTUAL MAGENTA MATERIAL/SHADER FOUND in the active scene!" : "Suspects identified. Inspect Console for details."), "OK");
        }
#endif
    }
}
