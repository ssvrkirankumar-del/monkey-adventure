using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonkeyAdventure.AILevelBuilder.Editor
{
    public static class HDWhiteTerrainRiskInspector
    {
        [MenuItem("Tools/Monkey Adventure/Inspect White Terrain Risk", false, 150)]
        public static void InspectActiveScene()
        {
            var level = GameObject.Find("AI_GENERATED_LEVEL");
            if (level == null)
            {
                Debug.LogWarning("[HDWhiteTerrainRiskInspector] AI_GENERATED_LEVEL root not found in active scene.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("HD WHITE TERRAIN RISK DIAGNOSTIC INSPECTION REPORT");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Active Scene: {SceneManager.GetActiveScene().path}");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            var renderers = level.GetComponentsInChildren<Renderer>(true);
            int totalRenderers = renderers.Length;
            int totalSlots = 0;
            int flaggedSlots = 0;

            sb.AppendLine($"Total Renderers Under AI_GENERATED_LEVEL: {totalRenderers}");
            sb.AppendLine("--------------------------------------------------------------------------------");

            foreach (var r in renderers)
            {
                if (r == null) continue;
                string objName = r.gameObject.name;
                string path = GetHierarchyPath(r.transform);
                string pathLower = (objName + " " + path).ToLowerInvariant();

                bool isTerrainCandidate = ContainsAny(pathLower, "ground", "terrain", "landscape", "floor", "water");

                Material[] mats = r.sharedMaterials;
                if (mats == null) continue;

                for (int slot = 0; slot < mats.Length; slot++)
                {
                    totalSlots++;
                    Material m = mats[slot];
                    if (m == null)
                    {
                        sb.AppendLine($"[NULL MATERIAL] Object: {objName} | Path: {path} | Slot: {slot}");
                        continue;
                    }

                    string matName = m.name;
                    string assetPath = AssetDatabase.GetAssetPath(m);
                    string shaderName = m.shader != null ? m.shader.name : "None";

                    bool hasBaseMapProp = m.HasProperty("_BaseMap");
                    bool hasMainTexProp = m.HasProperty("_MainTex");
                    Texture baseMapTex = hasBaseMapProp ? m.GetTexture("_BaseMap") : null;
                    Texture mainTex = hasMainTexProp ? m.GetTexture("_MainTex") : null;
                    Texture effectiveTex = baseMapTex ?? mainTex;

                    bool hasBaseColorProp = HasColorProperty(m, "_BaseColor");
                    bool hasColorProp = HasColorProperty(m, "_Color");
                    Color baseColorVal = hasBaseColorProp ? m.GetColor("_BaseColor") : Color.clear;
                    Color colorVal = hasColorProp ? m.GetColor("_Color") : Color.clear;

                    // Fallback simulated evaluation (as in original HDEnvironmentMasterVisualQA)
                    Color legacyEvalColor = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                    bool legacyWhiteRisk = effectiveTex == null && legacyEvalColor.r > 0.92f && legacyEvalColor.g > 0.92f && legacyEvalColor.b > 0.92f;

                    // Check all texture and color properties on material
                    var allTexProps = new List<string>();
                    var allColorProps = new List<string>();
                    if (m.shader != null)
                    {
                        int propCount = m.shader.GetPropertyCount();
                        for (int p = 0; p < propCount; p++)
                        {
                            var pType = m.shader.GetPropertyType(p);
                            string pName = m.shader.GetPropertyName(p);
                            if (pType == UnityEngine.Rendering.ShaderPropertyType.Texture)
                            {
                                Texture t = m.GetTexture(pName);
                                if (t != null) allTexProps.Add($"{pName}={t.name}");
                            }
                            else if (pType == UnityEngine.Rendering.ShaderPropertyType.Color)
                            {
                                Color c = m.GetColor(pName);
                                allColorProps.Add($"{pName}=#{ColorUtility.ToHtmlStringRGBA(c)}");
                            }
                        }
                    }

                    bool isDecal = shaderName.IndexOf("Decal", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool isWater = shaderName.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 || pathLower.Contains("water");
                    bool isTerrain = pathLower.Contains("ground") || pathLower.Contains("terrain") || pathLower.Contains("landscape") || pathLower.Contains("floor");

                    if (legacyWhiteRisk)
                    {
                        flaggedSlots++;
                        sb.AppendLine($"[FLAGGED WHITE/UNTEXTURED RISK - Slot #{slot}]");
                        sb.AppendLine($"  Object:          {objName}");
                        sb.AppendLine($"  Hierarchy Path:  {path}");
                        sb.AppendLine($"  Renderer Type:   {r.GetType().Name}");
                        sb.AppendLine($"  Material:        {matName} (Path: {(string.IsNullOrEmpty(assetPath) ? "Instance / Built-in" : assetPath)})");
                        sb.AppendLine($"  Shader:          {shaderName}");
                        sb.AppendLine($"  Is Terrain Match:{isTerrainCandidate} (isTerrain={isTerrain}, isWater={isWater}, isDecal={isDecal})");
                        sb.AppendLine($"  BaseMap Prop:    {hasBaseMapProp} (Tex: {(baseMapTex != null ? baseMapTex.name : "null")})");
                        sb.AppendLine($"  MainTex Prop:    {hasMainTexProp} (Tex: {(mainTex != null ? mainTex.name : "null")})");
                        sb.AppendLine($"  BaseColor Prop:  {hasBaseColorProp} (Color: #{ColorUtility.ToHtmlStringRGBA(baseColorVal)})");
                        sb.AppendLine($"  Color Prop:      {hasColorProp} (Color: #{ColorUtility.ToHtmlStringRGBA(colorVal)})");
                        sb.AppendLine($"  Legacy Eval Col: #{ColorUtility.ToHtmlStringRGBA(legacyEvalColor)} (Fallback=Color.white used: {!m.HasProperty("_BaseColor")})");
                        sb.AppendLine($"  Assigned Textures: [{(allTexProps.Count > 0 ? string.Join(", ", allTexProps) : "None")}]");
                        sb.AppendLine($"  Color Properties:  [{(allColorProps.Count > 0 ? string.Join(", ", allColorProps) : "None")}]");
                        sb.AppendLine();
                    }
                }
            }

            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine($"Summary: Total Slots Audited = {totalSlots}, Flagged White Risk Slots = {flaggedSlots}");
            sb.AppendLine("================================================================================");

            string reportDir = "Assets/AILevelBuilder/Reports";
            if (!Directory.Exists(reportDir)) Directory.CreateDirectory(reportDir);
            string reportPath = Path.Combine(reportDir, "HDWhiteTerrainRiskInspection.txt");
            File.WriteAllText(reportPath, sb.ToString());

            Debug.Log($"<color=#00FF88><b>[HDWhiteTerrainRiskInspector] Inspection Complete. Flagged slots: {flaggedSlots}. Report saved to {reportPath}</b></color>");
        }

        private static bool HasColorProperty(Material m, string propName)
        {
            if (m == null || m.shader == null) return false;
            Shader s = m.shader;
            int count = s.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                if (s.GetPropertyName(i) == propName)
                    return s.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Color;
            }
            return false;
        }

        private static string GetHierarchyPath(Transform t)
        {
            if (t == null) return "";
            string p = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                p = t.name + "/" + p;
            }
            return p;
        }

        private static bool ContainsAny(string text, params string[] values)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (var v in values)
                if (text.IndexOf(v, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
    }
}
