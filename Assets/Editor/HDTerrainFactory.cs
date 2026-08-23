using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Procedurally synthesizes high-definition organic terrain PBR textures, URP Lit materials,
    /// sculpted 3D terrain meshes (with natural berms, exposed roots, pebble paths, and mossy banks),
    /// and game-ready terrain prefabs for Level 01 (The Awakening).
    /// </summary>
    public static class HDTerrainFactory
    {
        private const string BASE_DIR = "Assets/Art/Environment/HD/Terrain";
        private const string TEX_DIR = BASE_DIR + "/Textures";
        private const string MAT_DIR = BASE_DIR + "/Materials";
        private const string MESH_DIR = BASE_DIR + "/Meshes";
        private const string PREFAB_DIR = BASE_DIR + "/Prefabs";

        [MenuItem("Window/Monkey Adventure/Generate HD Terrain Assets (Textures, Meshes & Prefabs)")]
        public static void GenerateAllHDTerrainAssets()
        {
            EnsureDirectories();
            GeneratePBRTextures();
            GeneratePBRMaterials();
            GenerateSculptedTerrainMeshesAndPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HDTerrainFactory] ✅ All HD Terrain textures, materials, sculpted meshes, and prefabs successfully generated!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(BASE_DIR)) Directory.CreateDirectory(BASE_DIR);
            if (!Directory.Exists(TEX_DIR)) Directory.CreateDirectory(TEX_DIR);
            if (!Directory.Exists(MAT_DIR)) Directory.CreateDirectory(MAT_DIR);
            if (!Directory.Exists(MESH_DIR)) Directory.CreateDirectory(MESH_DIR);
            if (!Directory.Exists(PREFAB_DIR)) Directory.CreateDirectory(PREFAB_DIR);
        }

        #region 1. PBR Terrain Textures
        private static void GeneratePBRTextures()
        {
            // 1. Jungle Soil / Dirt Trail (Albedo, Normal, Smoothness)
            CreateTexturePair(
                "Tex_HD_JungleSoil",
                (u, v) =>
                {
                    float noise1 = Mathf.PerlinNoise(u * 8f, v * 8f);
                    float noise2 = Mathf.PerlinNoise(u * 24f + 3.1f, v * 24f + 7.7f) * 0.4f;
                    float leafNoise = Mathf.PerlinNoise(u * 40f + 11.2f, v * 40f + 15.3f);
                    float r = 0.28f + noise1 * 0.12f + (leafNoise > 0.65f ? 0.15f : 0f);
                    float g = 0.22f + noise1 * 0.08f + (leafNoise > 0.65f ? 0.05f : 0f);
                    float b = 0.14f + noise1 * 0.06f;
                    return new Color(r + noise2 * 0.05f, g + noise2 * 0.03f, b, 1f);
                },
                512, 1.8f, 0.22f
            );

            // 2. Mossy Bank & Embankment
            CreateTexturePair(
                "Tex_HD_MossyBank",
                (u, v) =>
                {
                    float n1 = Mathf.PerlinNoise(u * 12f, v * 12f);
                    float n2 = Mathf.PerlinNoise(u * 32f + 5.5f, v * 32f + 9.2f) * 0.25f;
                    float r = 0.15f + n1 * 0.08f;
                    float g = 0.42f + n1 * 0.22f + n2;
                    float b = 0.12f + n1 * 0.06f;
                    return new Color(r, g, b, 1f);
                },
                512, 2.2f, 0.15f
            );

            // 3. Exposed Gnarled Tree Root
            CreateTexturePair(
                "Tex_HD_TreeRoot",
                (u, v) =>
                {
                    float grain = Mathf.Sin((u * 40f + Mathf.PerlinNoise(u * 5f, v * 5f) * 8f) * Mathf.PI);
                    float n = Mathf.PerlinNoise(u * 10f, v * 10f);
                    float val = 0.32f + grain * 0.08f + n * 0.12f;
                    return new Color(val, val * 0.72f, val * 0.45f, 1f);
                },
                512, 2.5f, 0.35f
            );

            // 4. Stepping Stone / Ancient Courtyard Flagstone
            CreateTexturePair(
                "Tex_HD_SteppingStone",
                (u, v) =>
                {
                    float n = Mathf.PerlinNoise(u * 14f, v * 14f);
                    float crack = Mathf.PerlinNoise(u * 36f, v * 36f);
                    float val = 0.45f + n * 0.18f - (crack > 0.75f ? 0.2f : 0f);
                    float moss = Mathf.PerlinNoise(u * 6f + 2f, v * 6f + 2f);
                    if (moss > 0.6f)
                        return new Color(val * 0.7f, val * 1.15f, val * 0.6f, 1f);
                    return new Color(val * 0.95f, val * 0.95f, val * 0.9f, 1f);
                },
                512, 2.0f, 0.28f
            );
        }

        private static void CreateTexturePair(string baseName, Func<float, float, Color> colorFunc, int size, float bumpStrength, float baseSmoothness)
        {
            string albedoPath = $"{TEX_DIR}/{baseName}_Albedo.png";
            string normalPath = $"{TEX_DIR}/{baseName}_Normal.png";
            string smoothPath = $"{TEX_DIR}/{baseName}_Smoothness.png";

            Texture2D albedoTex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Texture2D normalTex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Texture2D smoothTex = new Texture2D(size, size, TextureFormat.RGBA32, true);

            Color[] albedoPixels = new Color[size * size];
            Color[] normalPixels = new Color[size * size];
            Color[] smoothPixels = new Color[size * size];
            float[] heights = new float[size * size];

            for (int y = 0; y < size; y++)
            {
                float v = (float)y / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    int idx = y * size + x;
                    Color c = colorFunc(u, v);
                    albedoPixels[idx] = c;
                    heights[idx] = (c.r * 0.299f + c.g * 0.587f + c.b * 0.114f);
                    smoothPixels[idx] = new Color(baseSmoothness, baseSmoothness, baseSmoothness, 1f);
                }
            }

            // Sobel Filter for Tangent-Space Normal Map
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int xL = (x - 1 + size) % size;
                    int xR = (x + 1) % size;
                    int yD = (y - 1 + size) % size;
                    int yU = (y + 1) % size;

                    float dX = (heights[y * size + xR] - heights[y * size + xL]) * bumpStrength;
                    float dY = (heights[yU * size + x] - heights[yD * size + x]) * bumpStrength;

                    Vector3 normal = new Vector3(-dX, -dY, 1.0f).normalized;
                    Color nColor = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1.0f);
                    normalPixels[y * size + x] = nColor;
                }
            }

            albedoTex.SetPixels(albedoPixels);
            albedoTex.Apply();
            File.WriteAllBytes(albedoPath, albedoTex.EncodeToPNG());

            normalTex.SetPixels(normalPixels);
            normalTex.Apply();
            File.WriteAllBytes(normalPath, normalTex.EncodeToPNG());

            smoothTex.SetPixels(smoothPixels);
            smoothTex.Apply();
            File.WriteAllBytes(smoothPath, smoothTex.EncodeToPNG());

            AssetDatabase.ImportAsset(albedoPath);
            AssetDatabase.ImportAsset(normalPath);
            AssetDatabase.ImportAsset(smoothPath);

            TextureImporter normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (normalImporter != null)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }
        }
        #endregion

        #region 2. PBR Materials
        private static void GeneratePBRMaterials()
        {
            CreateURPLitMaterial("Mat_HD_Terrain_JungleSoil", "Tex_HD_JungleSoil", new Color(0.95f, 0.95f, 0.95f), 0.25f);
            CreateURPLitMaterial("Mat_HD_Terrain_MossyBank", "Tex_HD_MossyBank", new Color(0.9f, 1.0f, 0.9f), 0.15f);
            CreateURPLitMaterial("Mat_HD_Terrain_TreeRoots", "Tex_HD_TreeRoot", new Color(0.9f, 0.85f, 0.8f), 0.35f);
            CreateURPLitMaterial("Mat_HD_Terrain_SteppingStone", "Tex_HD_SteppingStone", new Color(0.95f, 0.95f, 0.95f), 0.28f);
        }

        private static Material CreateURPLitMaterial(string matName, string texBaseName, Color tint, float smoothness)
        {
            string matPath = $"{MAT_DIR}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                Shader uShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
                mat = new Material(uShader);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            mat.color = tint;
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0.0f);

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX_DIR}/{texBaseName}_Albedo.png");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX_DIR}/{texBaseName}_Normal.png");

            if (albedo != null) mat.SetTexture("_BaseMap", albedo);
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }
        #endregion

        #region 3. Sculpted Terrain Meshes & Prefabs
        private static void GenerateSculptedTerrainMeshesAndPrefabs()
        {
            Material matSoil = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/Mat_HD_Terrain_JungleSoil.mat");
            Material matMoss = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/Mat_HD_Terrain_MossyBank.mat");
            Material matRoots = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/Mat_HD_Terrain_TreeRoots.mat");
            Material matStone = AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/Mat_HD_Terrain_SteppingStone.mat");

            // 1. Start Zone Terrain (10m x 16m)
            CreateTerrainPrefab("HD_Terrain_StartZone", 10f, 16f, 20, 32, matSoil, matRoots, (x, z) =>
            {
                // Winding beaten dirt path in center (lower elevation) with raised mossy side berms
                float distFromCenter = Mathf.Abs(x);
                float pathWidth = 2.8f + Mathf.Sin(z * 0.4f) * 0.6f;
                float bermElevation = Mathf.Clamp01((distFromCenter - pathWidth) / 2.5f) * 0.45f;
                float microNoise = Mathf.PerlinNoise(x * 1.5f + 5f, z * 1.5f) * 0.12f;
                return bermElevation + microNoise;
            }, true);

            // 2. Main Jungle Path (7m x 10m)
            CreateTerrainPrefab("HD_Terrain_Path", 7f, 10f, 16, 22, matSoil, matRoots, (x, z) =>
            {
                float dist = Mathf.Abs(x);
                float berm = Mathf.Clamp01((dist - 2.0f) / 1.5f) * 0.38f;
                float micro = Mathf.PerlinNoise(x * 2f, z * 2f) * 0.08f;
                return berm + micro;
            }, true);

            // 3. Enemy Arena Clearing (12m x 10m)
            CreateTerrainPrefab("HD_Terrain_Arena", 12f, 10f, 24, 20, matSoil, matMoss, (x, z) =>
            {
                float radius = Mathf.Sqrt(x * x + z * z);
                float rim = Mathf.Clamp01((radius - 4.5f) / 2.0f) * 0.42f;
                float micro = Mathf.PerlinNoise(x * 1.2f, z * 1.2f) * 0.1f;
                return rim + micro;
            }, false);

            // 4. Stepping Jump Platform (4m x 4m)
            CreateTerrainPrefab("HD_Terrain_JumpPlatform", 4f, 4f, 12, 12, matStone, matMoss, (x, z) =>
            {
                float dist = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
                float edgeBevel = Mathf.Clamp01((dist - 1.4f) / 0.6f) * -0.15f;
                float micro = Mathf.PerlinNoise(x * 3f, z * 3f) * 0.06f;
                return edgeBevel + micro;
            }, false);

            // 5. Vine Landing Terrace (9m x 10m)
            CreateTerrainPrefab("HD_Terrain_VineLanding", 9f, 10f, 18, 20, matSoil, matRoots, (x, z) =>
            {
                float berm = Mathf.Clamp01((Mathf.Abs(x) - 2.8f) / 1.7f) * 0.4f;
                float step = (z > 2f ? 0.15f : 0f);
                float micro = Mathf.PerlinNoise(x * 1.8f, z * 1.8f) * 0.09f;
                return berm + step + micro;
            }, true);

            // 6. Hazard Clearing (10m x 14m)
            CreateTerrainPrefab("HD_Terrain_HazardClearing", 10f, 14f, 20, 28, matSoil, matMoss, (x, z) =>
            {
                float dist = Mathf.Abs(x);
                float berm = Mathf.Clamp01((dist - 2.6f) / 2.4f) * 0.38f;
                float micro = Mathf.PerlinNoise(x * 1.5f, z * 1.5f) * 0.1f;
                return berm + micro;
            }, true);

            // 7. Puzzle Courtyard (14m x 14m)
            CreateTerrainPrefab("HD_Terrain_Courtyard", 14f, 14f, 28, 28, matStone, matMoss, (x, z) =>
            {
                float dist = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
                float perimeterBerm = Mathf.Clamp01((dist - 5.2f) / 1.8f) * 0.45f;
                float slabNoise = Mathf.PerlinNoise(x * 0.8f, z * 0.8f) * 0.08f;
                return perimeterBerm + slabNoise;
            }, false);

            // 8. Level Exit Gateway Area (8m x 10m)
            CreateTerrainPrefab("HD_Terrain_ExitArea", 8f, 10f, 16, 20, matStone, matSoil, (x, z) =>
            {
                float berm = Mathf.Clamp01((Mathf.Abs(x) - 2.2f) / 1.8f) * 0.35f;
                float micro = Mathf.PerlinNoise(x * 2f, z * 2f) * 0.07f;
                return berm + micro;
            }, false);
        }

        private static void CreateTerrainPrefab(string prefabName, float width, float length, int resX, int resZ, Material mainMat, Material detailMat, Func<float, float, float> heightFunc, bool addRootMeshes)
        {
            string meshPath = $"{MESH_DIR}/{prefabName}_Mesh.asset";
            string prefabPath = $"{PREFAB_DIR}/{prefabName}.prefab";

            Mesh terrainMesh = GenerateTerrainGridMesh(width, length, resX, resZ, heightFunc);
            AssetDatabase.CreateAsset(terrainMesh, meshPath);

            GameObject root = new GameObject(prefabName);

            // Main Terrain Body
            GameObject mainGround = new GameObject("Terrain_Surface");
            mainGround.transform.SetParent(root.transform);
            mainGround.transform.localPosition = Vector3.zero;
            MeshFilter mf = mainGround.AddComponent<MeshFilter>();
            mf.sharedMesh = terrainMesh;
            MeshRenderer mr = mainGround.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mainMat;

            // Optional Exposed Roots across path
            if (addRootMeshes)
            {
                AddExposedRoots(root.transform, width, length, detailMat);
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            GameObject.DestroyImmediate(root);
        }

        private static Mesh GenerateTerrainGridMesh(float width, float length, int resX, int resZ, Func<float, float, float> heightFunc)
        {
            Mesh mesh = new Mesh();
            mesh.name = "HD_Sculpted_Terrain_Mesh";

            int vertCount = (resX + 1) * (resZ + 1);
            Vector3[] vertices = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            Vector3[] normals = new Vector3[vertCount];

            float dx = width / resX;
            float dz = length / resZ;
            float halfW = width * 0.5f;
            float halfL = length * 0.5f;

            for (int z = 0; z <= resZ; z++)
            {
                float pz = -halfL + z * dz;
                for (int x = 0; x <= resX; x++)
                {
                    float px = -halfW + x * dx;
                    int idx = z * (resX + 1) + x;

                    float py = heightFunc(px, pz);
                    // Match top surface of 1m deep bounding platform (top at Y = 0.5m)
                    vertices[idx] = new Vector3(px, py + 0.5f, pz);
                    uvs[idx] = new Vector2((float)x / resX * (width * 0.5f), (float)z / resZ * (length * 0.5f));
                    normals[idx] = Vector3.up;
                }
            }

            int triCount = resX * resZ * 6;
            int[] triangles = new int[triCount];
            int tIdx = 0;

            for (int z = 0; z < resZ; z++)
            {
                for (int x = 0; x < resX; x++)
                {
                    int v0 = z * (resX + 1) + x;
                    int v1 = v0 + 1;
                    int v2 = (z + 1) * (resX + 1) + x;
                    int v3 = v2 + 1;

                    triangles[tIdx++] = v0;
                    triangles[tIdx++] = v2;
                    triangles[tIdx++] = v1;

                    triangles[tIdx++] = v1;
                    triangles[tIdx++] = v2;
                    triangles[tIdx++] = v3;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static void AddExposedRoots(Transform parent, float width, float length, Material rootMat)
        {
            // Create 2-3 organic curved root arches traversing the path
            int rootCount = UnityEngine.Random.Range(2, 4);
            float[] zOffsets = new float[] { -length * 0.25f, length * 0.1f, length * 0.35f };

            for (int r = 0; r < rootCount && r < zOffsets.Length; r++)
            {
                GameObject rootObj = new GameObject($"ExposedRoot_{r + 1}");
                rootObj.transform.SetParent(parent);
                rootObj.transform.localPosition = new Vector3(0f, 0.52f, zOffsets[r]);

                MeshFilter rmf = rootObj.AddComponent<MeshFilter>();
                MeshRenderer rmr = rootObj.AddComponent<MeshRenderer>();
                rmr.sharedMaterial = rootMat;

                rmf.sharedMesh = GenerateCurvedRootMesh(width * 0.45f, 0.12f);
            }
        }

        private static Mesh GenerateCurvedRootMesh(float span, float radius)
        {
            Mesh mesh = new Mesh();
            mesh.name = "HD_CurvedRoot_Mesh";

            int segments = 12;
            int ringSegments = 6;
            int vertCount = (segments + 1) * ringSegments;

            Vector3[] verts = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] tris = new int[segments * ringSegments * 6];

            float dx = span / segments;
            float startX = -span * 0.5f;

            for (int s = 0; s <= segments; s++)
            {
                float x = startX + s * dx;
                float archY = Mathf.Sin((float)s / segments * Mathf.PI) * 0.08f;
                float curveZ = Mathf.Sin((float)s / segments * Mathf.PI * 2f) * 0.15f;
                Vector3 center = new Vector3(x, archY, curveZ);

                for (int r = 0; r < ringSegments; r++)
                {
                    float angle = (float)r / ringSegments * Mathf.PI * 2f;
                    float y = Mathf.Sin(angle) * radius;
                    float z = Mathf.Cos(angle) * radius;

                    int idx = s * ringSegments + r;
                    verts[idx] = center + new Vector3(0, y, z);
                    uvs[idx] = new Vector2((float)s / segments * 3f, (float)r / ringSegments);
                }
            }

            int t = 0;
            for (int s = 0; s < segments; s++)
            {
                for (int r = 0; r < ringSegments; r++)
                {
                    int rNext = (r + 1) % ringSegments;
                    int v0 = s * ringSegments + r;
                    int v1 = s * ringSegments + rNext;
                    int v2 = (s + 1) * ringSegments + r;
                    int v3 = (s + 1) * ringSegments + rNext;

                    tris[t++] = v0;
                    tris[t++] = v2;
                    tris[t++] = v1;

                    tris[t++] = v1;
                    tris[t++] = v2;
                    tris[t++] = v3;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }
        #endregion
    }
}
