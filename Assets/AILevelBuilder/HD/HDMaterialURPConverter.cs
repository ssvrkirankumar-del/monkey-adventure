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
    [Serializable]
    public class SingleMaterialConversionRecord
    {
        public string sourceMaterialName;
        public string sourceMaterialPath;
        public string sourceShaderName;
        public string convertedMaterialName;
        public string convertedMaterialPath;
        public string convertedShaderName;
        public bool isSuccess;
        public string statusMessage;
        public bool originalMaterialUnchanged = true;
    }

    [Serializable]
    public class MaterialConversionReport
    {
        public int sourceMaterialsCount = 0;
        public int convertedMaterialsCount = 0;
        public int alreadyURPCount = 0;
        public int failedCount = 0;
        public int magentaRiskCount = 0;
        public List<SingleMaterialConversionRecord> records = new List<SingleMaterialConversionRecord>();

        public void AddRecord(string srcName, string srcPath, string srcShader, string convName, string convPath, string convShader, bool success, string msg)
        {
            records.Add(new SingleMaterialConversionRecord
            {
                sourceMaterialName = srcName,
                sourceMaterialPath = srcPath,
                sourceShaderName = srcShader,
                convertedMaterialName = convName,
                convertedMaterialPath = convPath,
                convertedShaderName = convShader,
                isSuccess = success,
                statusMessage = msg,
                originalMaterialUnchanged = true
            });

            sourceMaterialsCount++;
            if (success)
            {
                if (srcShader.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase))
                {
                    alreadyURPCount++;
                }
                else
                {
                    convertedMaterialsCount++;
                }
            }
            else
            {
                failedCount++;
            }
        }
    }

    /// <summary>
    /// Non-destructive material converter that generates URP-compatible material assets
    /// for Built-in Standard shader materials without modifying source project assets.
    /// </summary>
    public static class HDMaterialURPConverter
    {
        public const string URP_MATERIALS_FOLDER = "Assets/AILevelBuilder/HD/URPMaterials";

        /// <summary>
        /// Scans all materials used by the HDAssetLibrary and scene preview hierarchy,
        /// creates converted URP/Lit materials under Assets/AILevelBuilder/HD/URPMaterials/,
        /// and reports conversion results without touching source materials.
        /// </summary>
        public static MaterialConversionReport PreviewConversion(HDAssetLibrary library = null)
        {
            MaterialConversionReport report = new MaterialConversionReport();
            EnsureOutputFolderExists();

            HashSet<Material> discoveredMaterials = new HashSet<Material>();

            // 1. Gather materials from HDAssetLibrary
            if (library != null)
            {
                foreach (var mapping in library.CategoryMappings)
                {
                    if (mapping.prefabs == null) continue;
                    foreach (var prefab in mapping.prefabs)
                    {
                        if (prefab == null) continue;
                        GatherMaterialsFromGameObject(prefab, discoveredMaterials);
                    }
                }
            }

            // 2. Gather materials from Scene Preview
            GameObject previewRoot = GameObject.Find(HDAssetAutoReplacer.PREVIEW_ROOT_NAME) ?? GameObject.Find(HDAssetAutoReplacer.HD_ROOT_NAME);
            if (previewRoot != null)
            {
                GatherMaterialsFromGameObject(previewRoot, discoveredMaterials);
            }

            // 3. Convert each unique material non-destructively
            foreach (Material sourceMat in discoveredMaterials)
            {
                if (sourceMat == null) continue;
                ConvertSingleMaterial(sourceMat, report);
            }

#if UNITY_EDITOR
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif

            PrintConversionReport(report);
            return report;
        }

        /// <summary>
        /// Applies the converted URP materials to the active scene's HD_REPLACEMENTS_PREVIEW / HD_REPLACEMENTS renderers.
        /// Does NOT modify source prefabs or original source materials.
        /// </summary>
        public static MaterialConversionReport ApplyConvertedMaterialsToPreview()
        {
            // First ensure all URP materials are generated
            MaterialConversionReport report = PreviewConversion();

            GameObject previewRoot = GameObject.Find(HDAssetAutoReplacer.PREVIEW_ROOT_NAME) ?? GameObject.Find(HDAssetAutoReplacer.HD_ROOT_NAME);
            if (previewRoot == null)
            {
                Debug.LogWarning("[HDMaterialURPConverter] No HD replacement preview found in scene to apply materials to.");
                return report;
            }

            Renderer[] renderers = previewRoot.GetComponentsInChildren<Renderer>(true);
            int replacedRenderersCount = 0;

            foreach (var rend in renderers)
            {
                if (ApplyConvertedMaterialsToRenderer(rend))
                {
                    replacedRenderersCount++;
                }
            }

            Debug.Log($"<color=#00FF88><b>[HDMaterialURPConverter] Successfully applied URP materials to {replacedRenderersCount} preview renderer(s). Original source materials remain strictly unchanged.</b></color>");
            return report;
        }

        /// <summary>
        /// Applies converted URP materials to a specific instantiated GameObject and all its child renderers.
        /// </summary>
        public static void ApplyConvertedMaterialsToInstance(GameObject instance)
        {
            if (instance == null) return;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                ApplyConvertedMaterialsToRenderer(rend);
            }
        }

        /// <summary>
        /// Ensures all child renderers on the given instance use converted URP materials.
        /// </summary>
        public static void EnsureURPShader(GameObject instance)
        {
            ApplyConvertedMaterialsToInstance(instance);
        }

        /// <summary>
        /// Ensures a specific renderer uses converted URP materials.
        /// </summary>
        public static bool EnsureURPShader(Renderer rend)
        {
            return ApplyConvertedMaterialsToRenderer(rend);
        }

        public static bool ApplyConvertedMaterialsToRenderer(Renderer rend)
        {
            if (rend == null) return false;

            Material[] sharedMats = rend.sharedMaterials;
            if (sharedMats == null || sharedMats.Length == 0) return false;

            bool modified = false;

            for (int i = 0; i < sharedMats.Length; i++)
            {
                Material m = sharedMats[i];
                if (m == null)
                {
                    // If material is null, infer from GameObject name
                    Material fallbackMat = InferFallbackURPMaterial(rend.gameObject);
                    if (fallbackMat != null)
                    {
                        sharedMats[i] = fallbackMat;
                        modified = true;
                    }
                    continue;
                }

                // If already URP, keep it
                if (m.shader != null && m.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Find or create corresponding URP material
                Material urpMat = GetOrCreateConvertedURPMaterial(m);
                if (urpMat != null)
                {
                    sharedMats[i] = urpMat;
                    modified = true;
                }
            }

            if (modified)
            {
                rend.sharedMaterials = sharedMats;
            }

            return modified;
        }

        private static Material InferFallbackURPMaterial(GameObject go)
        {
            string nameLower = go.name.ToLowerInvariant();
            string parentNameLower = go.transform.parent != null ? go.transform.parent.name.ToLowerInvariant() : "";

            if (nameLower.Contains("stump") || nameLower.Contains("trunk") || nameLower.Contains("root") ||
                parentNameLower.Contains("stump") || parentNameLower.Contains("trunk") || parentNameLower.Contains("root"))
            {
                return LoadURPMaterial("forestpack_treeStumpAndRoot_URP");
            }

            if (nameLower.Contains("leaf") || nameLower.Contains("canopy") || nameLower.Contains("foliage") ||
                parentNameLower.Contains("leaf") || parentNameLower.Contains("canopy"))
            {
                return LoadURPMaterial("forestpack_tree_leaf_URP");
            }

            if (nameLower.Contains("stone") || nameLower.Contains("rock") ||
                parentNameLower.Contains("stone") || parentNameLower.Contains("rock"))
            {
                return LoadURPMaterial("forestpack_stone_URP");
            }

            if (nameLower.Contains("fir") || parentNameLower.Contains("fir"))
            {
                return LoadURPMaterial("forestpack_tree_fir_URP");
            }

            if (nameLower.Contains("grass") || parentNameLower.Contains("grass") || nameLower.Contains("bush"))
            {
                return LoadURPMaterial("forestpack_foliage_URP");
            }

            return null;
        }

        private static Material LoadURPMaterial(string matName)
        {
#if UNITY_EDITOR
            string path = $"{URP_MATERIALS_FOLDER}/{matName}.mat";
            return AssetDatabase.LoadAssetAtPath<Material>(path);
#else
            return null;
#endif
        }

        /// <summary>
        /// Restores preview renderers back to their original source materials.
        /// </summary>
        public static void RestoreOriginalPreviewMaterials()
        {
            GameObject previewRoot = GameObject.Find(HDAssetAutoReplacer.PREVIEW_ROOT_NAME) ?? GameObject.Find(HDAssetAutoReplacer.HD_ROOT_NAME);
            if (previewRoot == null) return;

            Renderer[] renderers = previewRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                Material[] sharedMats = rend.sharedMaterials;
                for (int i = 0; i < sharedMats.Length; i++)
                {
                    Material m = sharedMats[i];
                    if (m != null && m.name.EndsWith("_URP", StringComparison.OrdinalIgnoreCase))
                    {
                        string originalName = m.name.Substring(0, m.name.Length - 4);
#if UNITY_EDITOR
                        string[] guids = AssetDatabase.FindAssets($"{originalName} t:Material");
                        foreach (var g in guids)
                        {
                            string p = AssetDatabase.GUIDToAssetPath(g);
                            if (!p.Contains("URPMaterials"))
                            {
                                Material origMat = AssetDatabase.LoadAssetAtPath<Material>(p);
                                if (origMat != null)
                                {
                                    sharedMats[i] = origMat;
                                    break;
                                }
                            }
                        }
#endif
                    }
                }
                rend.sharedMaterials = sharedMats;
            }
            Debug.Log("[HDMaterialURPConverter] Restored original materials on preview renderers.");
        }

        /// <summary>
        /// Converts a single source material into a URP/Lit material in Assets/AILevelBuilder/HD/URPMaterials/.
        /// </summary>
        public static Material ConvertSingleMaterial(Material sourceMat, MaterialConversionReport report = null)
        {
            if (sourceMat == null) return null;

            string srcName = sourceMat.name;
            string srcPath = "";
#if UNITY_EDITOR
            srcPath = AssetDatabase.GetAssetPath(sourceMat);
#endif
            string srcShader = sourceMat.shader != null ? sourceMat.shader.name : "Missing";

            // If already in URPMaterials folder and uses URP shader
            if (srcPath.Contains("URPMaterials") && srcShader.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase))
            {
                if (report != null) report.AddRecord(srcName, srcPath, srcShader, srcName, srcPath, srcShader, true, "Already URP material.");
                return sourceMat;
            }

            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
            {
                urpShader = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Universal Render Pipeline/Unlit");
            }

            if (urpShader == null)
            {
                if (report != null) report.AddRecord(srcName, srcPath, srcShader, "", "", "", false, "Universal Render Pipeline/Lit shader not found in project.");
                Debug.LogError("[HDMaterialURPConverter] Universal Render Pipeline/Lit shader could not be found!");
                return null;
            }

            // Standardize canonical URP material name
            string canonicalName = srcName;
            if (canonicalName.StartsWith("Mobile_")) canonicalName = canonicalName.Substring(7);
            if (canonicalName.EndsWith("_low")) canonicalName = canonicalName.Substring(0, canonicalName.Length - 4);

            string targetFileName = $"{canonicalName}_URP.mat";
            string targetPath = $"{URP_MATERIALS_FOLDER}/{targetFileName}";

            Material urpMaterial = null;
#if UNITY_EDITOR
            urpMaterial = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
#endif
            if (urpMaterial == null)
            {
                urpMaterial = new Material(urpShader);
            }
            else
            {
                urpMaterial.shader = urpShader;
            }

            urpMaterial.name = $"{canonicalName}_URP";

            // 1. Map Base Map & Base Color
            if (sourceMat.HasProperty("_MainTex"))
            {
                Texture mainTex = sourceMat.GetTexture("_MainTex");
                if (mainTex != null)
                {
                    urpMaterial.SetTexture("_BaseMap", mainTex);
                    urpMaterial.SetTextureScale("_BaseMap", sourceMat.GetTextureScale("_MainTex"));
                    urpMaterial.SetTextureOffset("_BaseMap", sourceMat.GetTextureOffset("_MainTex"));
                }
            }

            if (sourceMat.HasProperty("_Color"))
            {
                urpMaterial.SetColor("_BaseColor", sourceMat.GetColor("_Color"));
            }

            // 2. Map Normal / Bump Map
            if (sourceMat.HasProperty("_BumpMap"))
            {
                Texture bumpMap = sourceMat.GetTexture("_BumpMap");
                if (bumpMap != null)
                {
                    urpMaterial.SetTexture("_BumpMap", bumpMap);
                    urpMaterial.EnableKeyword("_NORMALMAP");
                    if (sourceMat.HasProperty("_BumpScale"))
                    {
                        urpMaterial.SetFloat("_BumpScale", sourceMat.GetFloat("_BumpScale"));
                    }
                }
            }

            // 3. Map Smoothness / Metallic
            if (sourceMat.HasProperty("_Glossiness"))
            {
                urpMaterial.SetFloat("_Smoothness", sourceMat.GetFloat("_Glossiness"));
            }
            else
            {
                urpMaterial.SetFloat("_Smoothness", 0.2f);
            }

            if (sourceMat.HasProperty("_Metallic"))
            {
                urpMaterial.SetFloat("_Metallic", sourceMat.GetFloat("_Metallic"));
            }

            if (sourceMat.HasProperty("_MetallicGlossMap"))
            {
                Texture metallicMap = sourceMat.GetTexture("_MetallicGlossMap");
                if (metallicMap != null)
                {
                    urpMaterial.SetTexture("_MetallicGlossMap", metallicMap);
                    urpMaterial.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
            }

            // 4. Map Emission
            if (sourceMat.HasProperty("_EmissionMap"))
            {
                Texture emissionMap = sourceMat.GetTexture("_EmissionMap");
                if (emissionMap != null)
                {
                    urpMaterial.SetTexture("_EmissionMap", emissionMap);
                    urpMaterial.EnableKeyword("_EMISSION");
                }
            }
            if (sourceMat.HasProperty("_EmissionColor"))
            {
                Color emColor = sourceMat.GetColor("_EmissionColor");
                if (emColor.maxColorComponent > 0.01f)
                {
                    urpMaterial.SetColor("_EmissionColor", emColor);
                    urpMaterial.EnableKeyword("_EMISSION");
                }
            }

            // 5. Alpha Cutout / Foliage Double-Sided Handling
            bool isCutout = false;
            float cutoff = 0.5f;

            if (sourceMat.HasProperty("_Mode") && Mathf.Approximately(sourceMat.GetFloat("_Mode"), 1f))
            {
                isCutout = true;
            }
            if (sourceMat.HasProperty("_Cutoff"))
            {
                cutoff = sourceMat.GetFloat("_Cutoff");
                if (cutoff > 0.01f) isCutout = true;
            }

            string matNameLower = srcName.ToLowerInvariant();
            if (matNameLower.Contains("leaf") || matNameLower.Contains("foliage") || matNameLower.Contains("grass"))
            {
                isCutout = true;
                // Double-sided rendering for leaves and foliage
                urpMaterial.SetFloat("_Cull", 0f); // Two-sided
            }

            if (isCutout)
            {
                urpMaterial.SetFloat("_Surface", 0f); // Opaque
                urpMaterial.SetFloat("_AlphaClip", 1f); // Enable Alpha Clipping
                urpMaterial.SetFloat("_Cutoff", cutoff);
                urpMaterial.EnableKeyword("_ALPHATEST_ON");
                urpMaterial.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                urpMaterial.SetFloat("_AlphaClip", 0f);
                urpMaterial.DisableKeyword("_ALPHATEST_ON");
                urpMaterial.renderQueue = (int)RenderQueue.Geometry;
            }

#if UNITY_EDITOR
            if (!AssetDatabase.Contains(urpMaterial))
            {
                AssetDatabase.CreateAsset(urpMaterial, targetPath);
            }
            else
            {
                EditorUtility.SetDirty(urpMaterial);
            }
#endif

            if (report != null)
            {
                report.AddRecord(srcName, srcPath, srcShader, urpMaterial.name, targetPath, urpShader.name, true, "Successfully converted to URP/Lit (Original unchanged).");
            }

            return urpMaterial;
        }

        private static Material GetOrCreateConvertedURPMaterial(Material sourceMat)
        {
            if (sourceMat == null) return null;

            string canonicalName = sourceMat.name;
            if (canonicalName.StartsWith("Mobile_")) canonicalName = canonicalName.Substring(7);
            if (canonicalName.EndsWith("_low")) canonicalName = canonicalName.Substring(0, canonicalName.Length - 4);

            string targetFileName = $"{canonicalName}_URP.mat";
            string targetPath = $"{URP_MATERIALS_FOLDER}/{targetFileName}";

#if UNITY_EDITOR
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
            if (existing != null) return existing;
#endif

            return ConvertSingleMaterial(sourceMat);
        }

        private static void GatherMaterialsFromGameObject(GameObject go, HashSet<Material> set)
        {
            if (go == null) return;
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                Material[] mats = rend.sharedMaterials;
                if (mats == null) continue;
                foreach (var m in mats)
                {
                    if (m != null) set.Add(m);
                }
            }
        }

        private static void EnsureOutputFolderExists()
        {
            if (!Directory.Exists(URP_MATERIALS_FOLDER))
            {
                Directory.CreateDirectory(URP_MATERIALS_FOLDER);
#if UNITY_EDITOR
                AssetDatabase.Refresh();
#endif
            }
        }

        private static void PrintConversionReport(MaterialConversionReport report)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b><color=#00FF88>[HD URP Material Conversion Report]</color></b>");
            sb.AppendLine($"Source Materials: {report.sourceMaterialsCount} | Converted to URP: {report.convertedMaterialsCount} | Already URP: {report.alreadyURPCount} | Failed: {report.failedCount}");
            sb.AppendLine($"Original Source Materials Unchanged: <b>YES</b> | Output: '{URP_MATERIALS_FOLDER}'");
            sb.AppendLine("--------------------------------------------------");

            foreach (var rec in report.records)
            {
                string tag = rec.isSuccess ? "<color=#00FF88>[SUCCESS]</color>" : "<color=#FF3366>[FAILED]</color>";
                sb.AppendLine($"{tag} <b>{rec.sourceMaterialName}</b> ({rec.sourceShaderName}) → <b>{rec.convertedMaterialName}</b> ({rec.convertedShaderName})\n   Path: {rec.convertedMaterialPath}");
            }

            Debug.Log(sb.ToString());
        }
    }
}
