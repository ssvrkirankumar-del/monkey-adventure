using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Generates high-quality PBR textures (Albedo, Tangent-Space Normal Maps, Smoothness/Metallic)
    /// and configures Universal Render Pipeline (URP) Lit materials for realistic tropical environment rendering.
    /// </summary>
    public static class HDPBRTextureFactory
    {
        public const string HD_ROOT = "Assets/Art/Environment/HD";
        public const string TEX_DIR = "Assets/Art/Environment/HD/Textures";
        public const string MAT_DIR = "Assets/Art/Environment/HD/Materials";
        public const string MESH_DIR = "Assets/Art/Environment/HD/Meshes";

        public static void EnsureDirectories()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Environment");
            EnsureFolder(HD_ROOT);
            EnsureFolder(TEX_DIR);
            EnsureFolder(MAT_DIR);
            EnsureFolder(MESH_DIR);
            EnsureFolder($"{HD_ROOT}/Trees");
            EnsureFolder($"{HD_ROOT}/Rocks");
            EnsureFolder($"{HD_ROOT}/Plants");
            EnsureFolder($"{HD_ROOT}/Ruins");
            EnsureFolder("Assets/Documentation/HDAssetAudit");
        }

        public static void GenerateAllHDPBRMaterials()
        {
            EnsureDirectories();

            // 1. Bark PBR (Ancient Canopy & Tropical Palms)
            CreatePBRMaterial("Mat_HD_Bark_Canopy",
                GenerateBarkAlbedo(512, new Color(0.28f, 0.18f, 0.11f), new Color(0.42f, 0.32f, 0.18f), new Color(0.25f, 0.40f, 0.15f)),
                GenerateHeightNormalMap(512, BarkHeightFunc, 3.5f),
                0.22f, 0.0f);

            CreatePBRMaterial("Mat_HD_Bark_Palm",
                GeneratePalmBarkAlbedo(512, new Color(0.38f, 0.28f, 0.18f), new Color(0.55f, 0.42f, 0.26f)),
                GenerateHeightNormalMap(512, PalmBarkHeightFunc, 3.0f),
                0.28f, 0.0f);

            // 2. Foliage PBR (Canopy, Palms, Broadleaf, Ferns, Flowers)
            CreatePBRMaterial("Mat_HD_Foliage_Canopy",
                GenerateLeafAlbedo(512, new Color(0.12f, 0.38f, 0.10f), new Color(0.28f, 0.58f, 0.16f), new Color(0.38f, 0.68f, 0.18f)),
                GenerateHeightNormalMap(512, LeafHeightFunc, 2.2f),
                0.45f, 0.0f, true);

            CreatePBRMaterial("Mat_HD_Foliage_PalmFrond",
                GeneratePalmFrondAlbedo(512, new Color(0.14f, 0.44f, 0.10f), new Color(0.32f, 0.65f, 0.14f)),
                GenerateHeightNormalMap(512, PalmFrondHeightFunc, 2.5f),
                0.48f, 0.0f, true);

            CreatePBRMaterial("Mat_HD_Foliage_Fern",
                GenerateFernAlbedo(512, new Color(0.16f, 0.48f, 0.12f), new Color(0.35f, 0.72f, 0.18f)),
                GenerateHeightNormalMap(512, FernHeightFunc, 2.0f),
                0.42f, 0.0f, true);

            CreatePBRMaterial("Mat_HD_Foliage_BroadLeaf",
                GenerateBroadLeafAlbedo(512, new Color(0.08f, 0.32f, 0.10f), new Color(0.22f, 0.54f, 0.18f)),
                GenerateHeightNormalMap(512, BroadLeafHeightFunc, 2.8f),
                0.52f, 0.0f, true);

            CreatePBRMaterial("Mat_HD_Foliage_Flowers",
                GenerateFlowerAlbedo(512, new Color(0.92f, 0.18f, 0.28f), new Color(1.0f, 0.75f, 0.15f), new Color(0.15f, 0.42f, 0.12f)),
                GenerateHeightNormalMap(512, FlowerHeightFunc, 2.0f),
                0.38f, 0.0f, true);

            // 3. Stone & Rock PBR (Mossy Granite, Cliffs, Weathered Outcrops)
            CreatePBRMaterial("Mat_HD_Rock_MossyGranite",
                GenerateRockAlbedo(512, new Color(0.32f, 0.34f, 0.32f), new Color(0.48f, 0.46f, 0.42f), new Color(0.22f, 0.44f, 0.14f)),
                GenerateHeightNormalMap(512, RockHeightFunc, 4.0f),
                0.30f, 0.05f);

            CreatePBRMaterial("Mat_HD_Rock_CliffBasalt",
                GenerateCliffAlbedo(512, new Color(0.22f, 0.24f, 0.26f), new Color(0.38f, 0.36f, 0.35f), new Color(0.18f, 0.35f, 0.12f)),
                GenerateHeightNormalMap(512, CliffHeightFunc, 4.5f),
                0.25f, 0.08f);

            // 4. Ancient Ruins PBR (Carved Masonry, Runic Altar with Cyan Emission)
            CreatePBRMaterial("Mat_HD_Ruin_AncientMasonry",
                GenerateMasonryAlbedo(512, new Color(0.42f, 0.40f, 0.38f), new Color(0.28f, 0.26f, 0.24f), new Color(0.20f, 0.38f, 0.15f)),
                GenerateHeightNormalMap(512, MasonryHeightFunc, 4.2f),
                0.32f, 0.10f);

            CreatePBRMaterial("Mat_HD_Ruin_RuneGoldCyan",
                GenerateMasonryAlbedo(512, new Color(0.35f, 0.34f, 0.32f), new Color(0.85f, 0.72f, 0.25f), new Color(0.18f, 0.32f, 0.14f)),
                GenerateHeightNormalMap(512, MasonryHeightFunc, 4.0f),
                0.60f, 0.35f, false, true, new Color(0f, 0.85f, 1f) * 3.5f);

            AssetDatabase.SaveAssets();
            Debug.Log("[HDPBRTextureFactory] All HD PBR Textures and URP Lit Materials successfully generated!");
        }

        public static Material GetMaterial(string matName)
        {
            string path = $"{MAT_DIR}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                GenerateAllHDPBRMaterials();
                mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            return mat;
        }

        private static void CreatePBRMaterial(string name, Texture2D albedoTex, Texture2D normalTex, float smoothness, float metallic, bool twoSided = false, bool isEmissive = false, Color emissionColor = default)
        {
            string matPath = $"{MAT_DIR}/{name}.mat";
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("URP/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(litShader);
                mat.name = name;
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat.shader = litShader;
            }

            if (albedoTex != null)
            {
                mat.SetTexture("_BaseMap", albedoTex);
                mat.SetTexture("_MainTex", albedoTex);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetColor("_Color", Color.white);
            }

            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }

            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);

            if (twoSided)
            {
                mat.SetFloat("_Cull", (float)CullMode.Off);
            }

            if (isEmissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(mat);
        }

        #region Procedural Height & Noise Generators
        private static float BarkHeightFunc(float u, float v)
        {
            float n1 = Mathf.PerlinNoise(u * 18f, v * 3.5f);
            float n2 = Mathf.PerlinNoise(u * 45f, v * 8.0f) * 0.4f;
            float ridge = Mathf.Abs(Mathf.Sin(u * 35f + n1 * 6f));
            return Mathf.Clamp01(ridge * 0.6f + n1 * 0.3f + n2 * 0.1f);
        }

        private static float PalmBarkHeightFunc(float u, float v)
        {
            float rings = Mathf.Abs(Mathf.Sin(v * 40f + Mathf.PerlinNoise(u * 8f, v * 8f) * 2f));
            float rough = Mathf.PerlinNoise(u * 20f, v * 20f) * 0.3f;
            return Mathf.Clamp01(rings * 0.7f + rough);
        }

        private static float LeafHeightFunc(float u, float v)
        {
            float centerVein = 1f - Mathf.Clamp01(Mathf.Abs(u - 0.5f) * 12f);
            float sideVeins = Mathf.Abs(Mathf.Sin((v + Mathf.Abs(u - 0.5f) * 0.8f) * 35f));
            return Mathf.Clamp01(centerVein * 0.5f + sideVeins * 0.3f + Mathf.PerlinNoise(u * 15f, v * 15f) * 0.2f);
        }

        private static float PalmFrondHeightFunc(float u, float v)
        {
            float spine = 1f - Mathf.Clamp01(Mathf.Abs(u - 0.5f) * 16f);
            float strips = Mathf.Abs(Mathf.Sin(u * 80f));
            return Mathf.Clamp01(spine * 0.6f + strips * 0.4f);
        }

        private static float FernHeightFunc(float u, float v)
        {
            float spine = 1f - Mathf.Clamp01(Mathf.Abs(u - 0.5f) * 20f);
            float pinnules = Mathf.Abs(Mathf.Sin(v * 60f)) * (1f - Mathf.Abs(u - 0.5f) * 1.5f);
            return Mathf.Clamp01(spine * 0.4f + pinnules * 0.6f);
        }

        private static float BroadLeafHeightFunc(float u, float v)
        {
            float distCenter = Mathf.Abs(u - 0.5f);
            float rib = Mathf.Exp(-distCenter * 15f);
            float secondary = Mathf.Sin((v * 12f - distCenter * 8f) * Mathf.PI) * 0.3f;
            return Mathf.Clamp01(rib * 0.6f + Mathf.Max(0, secondary) + Mathf.PerlinNoise(u * 10f, v * 10f) * 0.15f);
        }

        private static float FlowerHeightFunc(float u, float v)
        {
            float radial = Mathf.Sin(u * Mathf.PI * 5f) * Mathf.Cos(v * Mathf.PI * 5f);
            return Mathf.Clamp01(0.5f + radial * 0.4f + Mathf.PerlinNoise(u * 20f, v * 20f) * 0.1f);
        }

        private static float RockHeightFunc(float u, float v)
        {
            float n1 = Mathf.PerlinNoise(u * 6f, v * 6f);
            float n2 = Mathf.PerlinNoise(u * 16f, v * 16f) * 0.5f;
            float n3 = Mathf.PerlinNoise(u * 38f, v * 38f) * 0.25f;
            float cracks = Mathf.Pow(Mathf.Abs(Mathf.PerlinNoise(u * 12f, v * 12f) - 0.5f) * 2f, 3f);
            return Mathf.Clamp01((n1 + n2 + n3) * 0.55f + (1f - cracks) * 0.25f);
        }

        private static float CliffHeightFunc(float u, float v)
        {
            float strata = Mathf.Sin(v * 28f + Mathf.PerlinNoise(u * 5f, v * 5f) * 4f) * 0.5f + 0.5f;
            float cracks = Mathf.PerlinNoise(u * 22f, v * 8f);
            return Mathf.Clamp01(strata * 0.6f + cracks * 0.4f);
        }

        private static float MasonryHeightFunc(float u, float v)
        {
            float blockX = Mathf.Abs(Mathf.Sin(u * 12f * Mathf.PI));
            float blockY = Mathf.Abs(Mathf.Sin(v * 16f * Mathf.PI));
            float mortar = Mathf.Clamp01((1f - Mathf.Pow(blockX, 0.2f)) + (1f - Mathf.Pow(blockY, 0.2f)));
            float stoneRough = Mathf.PerlinNoise(u * 25f, v * 25f) * 0.35f;
            return Mathf.Clamp01((1f - mortar * 0.6f) + stoneRough);
        }
        #endregion

        #region Texture Generators
        private static Texture2D GenerateBarkAlbedo(int size, Color darkWood, Color lightWood, Color mossGreen)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float h = BarkHeightFunc(u, v);
                    float moss = Mathf.Clamp01(Mathf.PerlinNoise(u * 4f, v * 4f) * 1.6f - 0.6f);

                    Color wood = Color.Lerp(darkWood, lightWood, h);
                    Color final = Color.Lerp(wood, mossGreen, moss * 0.65f);
                    pixels[y * size + x] = final;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return SaveTexturePNG(tex, "Tex_HD_Bark_Canopy_Albedo");
        }

        private static Texture2D GeneratePalmBarkAlbedo(int size, Color darkWood, Color lightWood)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float h = PalmBarkHeightFunc(u, v);
                    Color wood = Color.Lerp(darkWood, lightWood, h);
                    pixels[y * size + x] = wood;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return SaveTexturePNG(tex, "Tex_HD_Bark_Palm_Albedo");
        }

        private static Texture2D GenerateLeafAlbedo(int size, Color darkGreen, Color midGreen, Color lightGreen)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float h = LeafHeightFunc(u, v);
                    Color col = (h < 0.5f) ? Color.Lerp(darkGreen, midGreen, h * 2f) : Color.Lerp(midGreen, lightGreen, (h - 0.5f) * 2f);
                    pixels[y * size + x] = col;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return SaveTexturePNG(tex, "Tex_HD_Foliage_Canopy_Albedo");
        }

        private static Texture2D GeneratePalmFrondAlbedo(int size, Color darkGreen, Color brightGreen)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float h = PalmFrondHeightFunc(u, v);
                    Color col = Color.Lerp(darkGreen, brightGreen, h);
                    pixels[y * size + x] = col;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return SaveTexturePNG(tex, "Tex_HD_Foliage_PalmFrond_Albedo");
        }

        private static Texture2D GenerateFernAlbedo(int size, Color darkGreen, Color brightGreen)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float h = FernHeightFunc(u, v);
                    Color col = Color.Lerp(darkGreen, brightGreen, h);
                    pixels[y * size + x] = col;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return SaveTexturePNG(tex, "Tex_HD_Foliage_Fern_Albedo");
        }

        private static Texture2D GenerateBroadLeafAlbedo(int size, Color darkGreen, Color emeraldGreen)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float h = BroadLeafHeightFunc(u, v);
                    Color col = Color.Lerp(darkGreen, emeraldGreen, h);
                    pixels[y * size + x] = col;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return SaveTexturePNG(tex, "Tex_HD_Foliage_BroadLeaf_Albedo");
        }

        private static Texture2D GenerateFlowerAlbedo(int size, Color petalRed, Color petalYellow, Color stemGreen)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float h = FlowerHeightFunc(u, v);
                    Color col = (v < 0.2f) ? stemGreen : Color.Lerp(petalRed, petalYellow, h);
                    pixels[y * size + x] = col;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return SaveTexturePNG(tex, "Tex_HD_Foliage_Flowers_Albedo");
        }

        private static Texture2D GenerateRockAlbedo(int size, Color darkRock, Color lightRock, Color mossGreen)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float h = RockHeightFunc(u, v);
                    float mossNoise = Mathf.Clamp01(Mathf.PerlinNoise(u * 5f, v * 5f) * 1.5f - 0.4f);

                    Color rock = Color.Lerp(darkRock, lightRock, h);
                    Color final = Color.Lerp(rock, mossGreen, mossNoise * 0.7f);
                    pixels[y * size + x] = final;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return SaveTexturePNG(tex, "Tex_HD_Rock_MossyGranite_Albedo");
        }

        private static Texture2D GenerateCliffAlbedo(int size, Color darkBasalt, Color lightBasalt, Color mossGreen)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float h = CliffHeightFunc(u, v);
                    float moss = Mathf.Clamp01(Mathf.PerlinNoise(u * 4f, v * 4f) * 1.4f - 0.5f);

                    Color rock = Color.Lerp(darkBasalt, lightBasalt, h);
                    Color final = Color.Lerp(rock, mossGreen, moss * 0.55f);
                    pixels[y * size + x] = final;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return SaveTexturePNG(tex, "Tex_HD_Rock_CliffBasalt_Albedo");
        }

        private static Texture2D GenerateMasonryAlbedo(int size, Color stoneLight, Color stoneDark, Color mossGreen)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float h = MasonryHeightFunc(u, v);
                    float moss = Mathf.Clamp01(Mathf.PerlinNoise(u * 6f, v * 6f) * 1.5f - 0.5f);

                    Color stone = Color.Lerp(stoneDark, stoneLight, h);
                    Color final = Color.Lerp(stone, mossGreen, moss * 0.6f);
                    pixels[y * size + x] = final;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return SaveTexturePNG(tex, "Tex_HD_Ruin_AncientMasonry_Albedo");
        }

        private static Texture2D GenerateHeightNormalMap(int size, Func<float, float, float> heightFunc, float bumpStrength)
        {
            Texture2D normalTex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] pixels = new Color[size * size];
            float invSize = 1.0f / size;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x * invSize;
                    float v = (float)y * invSize;

                    float uL = ((x - 1 + size) % size) * invSize;
                    float uR = ((x + 1) % size) * invSize;
                    float vD = ((y - 1 + size) % size) * invSize;
                    float vU = ((y + 1) % size) * invSize;

                    float hL = heightFunc(uL, v);
                    float hR = heightFunc(uR, v);
                    float hD = heightFunc(u, vD);
                    float hU = heightFunc(u, vU);

                    float dX = (hR - hL) * bumpStrength;
                    float dY = (hU - hD) * bumpStrength;

                    Vector3 n = new Vector3(-dX, -dY, 1.0f).normalized;
                    Color normalColor = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1.0f);
                    pixels[y * size + x] = normalColor;
                }
            }

            normalTex.SetPixels(pixels);
            normalTex.Apply();
            string normalName = $"Tex_HD_Normal_{Guid.NewGuid().ToString().Substring(0, 8)}";
            return SaveTexturePNG(normalTex, normalName, true);
        }

        private static Texture2D SaveTexturePNG(Texture2D tex, string name, bool isNormal = false)
        {
            string path = $"{TEX_DIR}/{name}.png";
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !isNormal;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.anisoLevel = 4;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void EnsureFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
                string name = Path.GetFileName(folder);
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    EnsureFolder(parent);
                }
                AssetDatabase.CreateFolder(parent, name);
            }
        }
        #endregion
    }
}
