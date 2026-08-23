using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Builds the authentic HD Coconut Palm Tree (HD_Tree_CoconutPalm_01.prefab)
    /// featuring:
    /// - Organic curved segmented fibrous trunk mesh with natural tropical lean
    /// - Realistic crown with 10 curved drooping feather-pinnae fronds
    /// - URP Lit materials with alpha clipping, normal mapping, and two-sided rendering
    /// </summary>
    [InitializeOnLoad]
    public static class HDPalmTreeBuilder
    {
        private const string TREE_DIR = "Assets/Art/Environment/HD/Trees";
        private const string MESH_DIR = "Assets/Art/Environment/HD/Meshes";
        private const string MAT_DIR = "Assets/Art/Environment/HD/Materials";
        public const string PREFAB_PATH = "Assets/Art/Environment/HD/Trees/HD_Tree_CoconutPalm_01.prefab";

        static HDPalmTreeBuilder()
        {
            EditorApplication.delayCall += EnsureHDPalmPrefabExists;
        }

        [MenuItem("Window/Monkey Adventure/🌴 Build & Verify HD Coconut Palm Prefab", false, 160)]
        public static void EnsureHDPalmPrefabExists()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Environment");
            EnsureFolder("Assets/Art/Environment/HD");
            EnsureFolder(TREE_DIR);
            EnsureFolder(MESH_DIR);
            EnsureFolder(MAT_DIR);

            CreateOrUpdateHDPalmPrefab();
        }

        public static GameObject CreateOrUpdateHDPalmPrefab()
        {
            EnsureFolder(TREE_DIR);
            EnsureFolder(MESH_DIR);
            EnsureFolder(MAT_DIR);

            // 1. Materials
            Material trunkMat = GetOrCreatePalmTrunkMaterial();
            Material frondMat = GetOrCreatePalmFrondMaterial();

            // 2. Trunk Mesh (Curved, segmented, 7.2m tall with natural tropical bend)
            Mesh trunkMesh = CreateCurvedPalmTrunkMesh();
            string trunkMeshPath = $"{MESH_DIR}/Mesh_HD_CoconutPalmTrunk.asset";
            AssetDatabase.CreateAsset(trunkMesh, trunkMeshPath);

            // 3. Frond Mesh (Curved arching feather frond with double curvature)
            Mesh frondMesh = CreateCurvedPalmFrondMesh();
            string frondMeshPath = $"{MESH_DIR}/Mesh_HD_CoconutPalmFrond.asset";
            AssetDatabase.CreateAsset(frondMesh, frondMeshPath);

            // 4. Construct Prefab Hierarchy
            GameObject root = new GameObject("HD_Tree_CoconutPalm_01");

            // Trunk GameObject
            GameObject trunkObj = new GameObject("Palm_Trunk");
            trunkObj.transform.SetParent(root.transform, false);
            MeshFilter trunkMF = trunkObj.AddComponent<MeshFilter>();
            MeshRenderer trunkMR = trunkObj.AddComponent<MeshRenderer>();
            trunkMF.sharedMesh = trunkMesh;
            trunkMR.sharedMaterial = trunkMat;
            trunkMR.shadowCastingMode = ShadowCastingMode.On;
            trunkMR.receiveShadows = true;

            // Crown GameObject at top of curved trunk (offset to match trunk curvature)
            GameObject crownObj = new GameObject("Palm_Crown");
            crownObj.transform.SetParent(root.transform, false);
            crownObj.transform.localPosition = new Vector3(0.85f, 6.8f, 0.45f);

            // 10 Draped Fronds in natural radial arrangement with layered pitch & roll
            int frondCount = 10;
            float[] pitches = new float[] { 22f, 34f, 18f, 40f, 25f, 32f, 15f, 38f, 28f, 30f };
            float[] scales = new float[] { 1.0f, 0.95f, 1.05f, 0.9f, 1.02f, 0.98f, 1.05f, 0.92f, 1.0f, 0.96f };

            for (int i = 0; i < frondCount; i++)
            {
                float angle = i * (360f / frondCount) + UnityEngine.Random.Range(-6f, 6f);
                GameObject fObj = new GameObject($"Frond_{i:D2}");
                fObj.transform.SetParent(crownObj.transform, false);
                fObj.transform.localRotation = Quaternion.Euler(pitches[i], angle, (i % 2 == 0 ? 8f : -8f));
                fObj.transform.localScale = Vector3.one * scales[i];

                MeshFilter fMF = fObj.AddComponent<MeshFilter>();
                MeshRenderer fMR = fObj.AddComponent<MeshRenderer>();
                fMF.sharedMesh = frondMesh;
                fMR.sharedMaterial = frondMat;
                fMR.shadowCastingMode = ShadowCastingMode.On;
                fMR.receiveShadows = true;
            }

            // Save Prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            UnityEngine.Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=#00FF88><b>[HDPalmTreeBuilder] Authentic HD Coconut Palm Prefab created at '{PREFAB_PATH}'!</b></color>");
            return prefab;
        }

        private static Material GetOrCreatePalmTrunkMaterial()
        {
            string path = $"{MAT_DIR}/Mat_HD_PalmTrunk.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }

            // Use Road_Lieves / Bark textures or custom PBR
            Texture2D barkTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Road_Lieves_1_AlbedoTransparency.png");
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Road_Lieves_1_Normal.png");

            mat.shader = Shader.Find("Universal Render Pipeline/Lit");
            if (barkTex != null) mat.SetTexture("_BaseMap", barkTex);
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }
            mat.SetTextureScale("_BaseMap", new Vector2(1.5f, 6f));
            mat.SetTextureScale("_BumpMap", new Vector2(1.5f, 6f));
            mat.SetColor("_BaseColor", new Color(0.68f, 0.58f, 0.48f, 1f));
            mat.SetFloat("_Smoothness", 0.18f);

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreatePalmFrondMaterial()
        {
            string path = $"{MAT_DIR}/Mat_HD_PalmFrond.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }

            Texture2D frondTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Grass_Billboard/Brake_Ferns_Bilboard.png");
            Texture2D frondNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Grass_Billboard_Normal/Brake_Ferns_Bilboard_NormaL.png");

            mat.shader = Shader.Find("Universal Render Pipeline/Lit");
            if (frondTex != null) mat.SetTexture("_BaseMap", frondTex);
            if (frondNorm != null)
            {
                mat.SetTexture("_BumpMap", frondNorm);
                mat.EnableKeyword("_NORMALMAP");
            }

            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.3f);
            mat.SetFloat("_Cull", 0f); // Two-sided fronds
            mat.SetColor("_BaseColor", new Color(0.82f, 0.95f, 0.75f, 1f));
            mat.SetFloat("_Smoothness", 0.25f);
            mat.renderQueue = (int)RenderQueue.AlphaTest;

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Mesh CreateCurvedPalmTrunkMesh()
        {
            Mesh mesh = new Mesh { name = "Mesh_HD_CoconutPalmTrunk" };

            int heightSegments = 20;
            int radialSegments = 14;
            float totalHeight = 7.2f;

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int y = 0; y <= heightSegments; y++)
            {
                float t = (float)y / heightSegments;
                float currentHeight = t * totalHeight;

                // Organic palm lean curve (leans out and gently arches up at top)
                float xOffset = Mathf.Sin(t * Mathf.PI * 0.55f) * 0.95f;
                float zOffset = Mathf.Sin(t * Mathf.PI * 0.45f) * 0.5f;

                // Taper from flared root base (0.55m) to slender trunk (0.28m) to neck (0.32m)
                float radius;
                if (t < 0.15f)
                    radius = Mathf.Lerp(0.55f, 0.34f, t / 0.15f);
                else if (t < 0.85f)
                    radius = Mathf.Lerp(0.34f, 0.28f, (t - 0.15f) / 0.7f);
                else
                    radius = Mathf.Lerp(0.28f, 0.32f, (t - 0.85f) / 0.15f);

                // Segment ring bulge for natural palm texture rings
                float ringBulge = Mathf.Sin(t * heightSegments * Mathf.PI) * 0.015f;
                radius += ringBulge;

                Vector3 center = new Vector3(xOffset, currentHeight, zOffset);

                for (int r = 0; r <= radialSegments; r++)
                {
                    float angle = (float)r / radialSegments * Mathf.PI * 2f;
                    Vector3 normal = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                    Vector3 pos = center + normal * radius;

                    verts.Add(pos);
                    normals.Add(normal);
                    uvs.Add(new Vector2((float)r / radialSegments, t * 6f));
                }
            }

            for (int y = 0; y < heightSegments; y++)
            {
                for (int r = 0; r < radialSegments; r++)
                {
                    int i0 = y * (radialSegments + 1) + r;
                    int i1 = i0 + 1;
                    int i2 = (y + 1) * (radialSegments + 1) + r;
                    int i3 = i2 + 1;

                    tris.Add(i0); tris.Add(i2); tris.Add(i1);
                    tris.Add(i1); tris.Add(i2); tris.Add(i3);
                }
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static Mesh CreateCurvedPalmFrondMesh()
        {
            Mesh mesh = new Mesh { name = "Mesh_HD_CoconutPalmFrond" };

            int lengthSegments = 14;
            float frondLength = 3.8f;
            float maxHalfWidth = 0.65f;

            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> tris = new List<int>();

            for (int z = 0; z <= lengthSegments; z++)
            {
                float t = (float)z / lengthSegments;
                float zPos = t * frondLength;

                // Natural arching curve (starts horizontal, curves down towards the tip)
                float yPos = -Mathf.Pow(t, 2.1f) * 1.45f;

                // Feather width profile (tapers at stem and tip, widest at 40% length)
                float widthFactor;
                if (t < 0.4f)
                    widthFactor = Mathf.Lerp(0.08f, 1.0f, t / 0.4f);
                else
                    widthFactor = Mathf.Lerp(1.0f, 0.05f, (t - 0.4f) / 0.6f);

                float halfW = maxHalfWidth * widthFactor;

                // V-shaped frond cross-section (drooping leaflets on both sides)
                float leafletDroop = -halfW * 0.45f;

                Vector3 leftPos = new Vector3(-halfW, yPos + leafletDroop, zPos);
                Vector3 spinePos = new Vector3(0, yPos, zPos);
                Vector3 rightPos = new Vector3(halfW, yPos + leafletDroop, zPos);

                verts.Add(leftPos);
                verts.Add(spinePos);
                verts.Add(rightPos);

                Vector3 nL = Vector3.Normalize(new Vector3(-0.4f, 0.9f, -0.1f));
                Vector3 nC = Vector3.up;
                Vector3 nR = Vector3.Normalize(new Vector3(0.4f, 0.9f, -0.1f));

                normals.Add(nL);
                normals.Add(nC);
                normals.Add(nR);

                uvs.Add(new Vector2(0f, t));
                uvs.Add(new Vector2(0.5f, t));
                uvs.Add(new Vector2(1f, t));
            }

            for (int z = 0; z < lengthSegments; z++)
            {
                int row0 = z * 3;
                int row1 = (z + 1) * 3;

                // Left wing
                tris.Add(row0 + 0); tris.Add(row1 + 1); tris.Add(row0 + 1);
                tris.Add(row0 + 0); tris.Add(row1 + 0); tris.Add(row1 + 1);

                // Right wing
                tris.Add(row0 + 1); tris.Add(row1 + 2); tris.Add(row0 + 2);
                tris.Add(row0 + 1); tris.Add(row1 + 1); tris.Add(row1 + 2);

                // Backfaces for two-sided lighting
                tris.Add(row0 + 1); tris.Add(row1 + 1); tris.Add(row0 + 0);
                tris.Add(row1 + 1); tris.Add(row1 + 0); tris.Add(row0 + 0);

                tris.Add(row0 + 2); tris.Add(row1 + 2); tris.Add(row0 + 1);
                tris.Add(row1 + 2); tris.Add(row1 + 1); tris.Add(row0 + 1);
            }

            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            return mesh;
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string folderName = Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent) && parent != "Assets")
                {
                    EnsureFolder(parent);
                }
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
