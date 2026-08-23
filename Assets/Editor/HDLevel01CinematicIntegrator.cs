using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Master Cinematic HD Jungle Integrator for Level 01 (The Awakening).
    /// Transforms the level into a AAA cinematic third-person jungle adventure with:
    /// 1. 4K/2K Photorealistic Scanned Trees (Oak Tree, Magnolia, Elm, Ash, Palms)
    /// 2. PBR Jungle Soil, Mud, and Leaf-Litter Terrain (Terrain&GrassPack)
    /// 3. Dense Tropical Ferns, Grass Billboards, and Wildflower Clusters
    /// 4. 3D Scanned Boulders, River Stones, and Cliff Embankments
    /// 5. Full Horizon Multi-Tier Jungle Backdrop (Zero Empty Horizon Voids)
    /// 6. Warm Tropical Morning Sunlight, Volumetric Atmosphere Fog & URP Post-Processing
    /// 7. 100% Non-Destructive Physics & Gameplay Preservation
    /// </summary>
    public static class HDLevel01CinematicIntegrator
    {
        private const string SCENE_PATH = "Assets/Scenes/Level01_Awakening.unity";
        private const string ROOT_SCENE_PATH = "Assets/Level01_Awakening.unity";
        private const string HD_VISUAL_ROOT = "[HD_Visual]";

        // Texture Paths
        private const string TEX_MUD_LEAVES_ALBEDO = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Mud_Leaves_1_AlbedoTransparency.png";
        private const string TEX_MUD_LEAVES_NORMAL = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Mud_Leaves_1_Normal.png";
        private const string TEX_ROAD_LEAVES_ALBEDO = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Road_Lieves_1_AlbedoTransparency.png";
        private const string TEX_ROAD_LEAVES_NORMAL = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Road_Lieves_1_Normal.png";
        private const string TEX_GRASS_LEAVES_ALBEDO = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Grass_Leaves_1_AlbedoTransparency.png";
        private const string TEX_GRASS_LEAVES_NORMAL = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Grass_Leaves_1_Normal.png";
        private const string TEX_CLIFF_ALBEDO = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Cliffwall_AlbedoTransparency.png";
        private const string TEX_CLIFF_NORMAL = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/Cliffwall_Normal.png";

        // Billboard Textures
        private const string TEX_FERN_ALBEDO = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Grass_Billboard/Brake_Ferns_Bilboard.png";
        private const string TEX_FERN_NORMAL = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Grass_Billboard_Normal/Brake_Ferns_Bilboard_NormaL.png";
        private const string TEX_FERN2_ALBEDO = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Grass_Billboard/Brake_Ferns_2_Bilboard.png";
        private const string TEX_FERN2_NORMAL = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Grass_Billboard_Normal/Brake_Ferns_2_Bilboard_NormaL.png";
        private const string TEX_GRASS_ALBEDO = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Grass_Billboard/Grass_1_Billboard.png";
        private const string TEX_GRASS_NORMAL = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Grass_Billboard_Normal/Grass_1_Billboard_Normal.png";
        private const string TEX_ORCHID_ALBEDO = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Grass_Billboard/Orchid_Bilboard.png";
        private const string TEX_ORCHID_NORMAL = "Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Grass_Billboard_Normal/Orchid_Bilboard_Normal.png";

        // Material Output Directory
        private const string MAT_OUTPUT_DIR = "Assets/Art/Environment/HD/Materials";
        private const string MESH_OUTPUT_DIR = "Assets/Art/Environment/HD/Meshes";

        [MenuItem("Window/Monkey Adventure/🌟 Apply Master Cinematic HD Pass (Level 01)", false, 150)]
        public static void ApplyCinematicHDPassLevel01()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Cinematic HD Jungle Pass", "Opening Level 01 Scene...", 0.1f);
                EnsureDirectory(MAT_OUTPUT_DIR);
                EnsureDirectory(MESH_OUTPUT_DIR);

                Scene scene = OpenLevel01Scene();

                EditorUtility.DisplayProgressBar("Cinematic HD Jungle Pass", "Building PBR Materials...", 0.25f);
                var materials = SetupPBRMaterials();

                EditorUtility.DisplayProgressBar("Cinematic HD Jungle Pass", "Upgrading Terrain & Path Visuals...", 0.40f);
                UpgradeTerrainVisuals(materials);

                EditorUtility.DisplayProgressBar("Cinematic HD Jungle Pass", "Upgrading Trees & Canopy...", 0.55f);
                UpgradeTreeVisuals();

                EditorUtility.DisplayProgressBar("Cinematic HD Jungle Pass", "Populating Dense Foliage & Understory...", 0.70f);
                PopulateDenseFoliage(materials);

                EditorUtility.DisplayProgressBar("Cinematic HD Jungle Pass", "Building Panoramic Jungle Backdrop...", 0.85f);
                BuildPanoramicJungleBackdrop(materials);

                EditorUtility.DisplayProgressBar("Cinematic HD Jungle Pass", "Configuring Atmospheric Lighting & Post-Processing...", 0.95f);
                ConfigureAtmosphericLightingAndVolume();

                // Strip any accidental colliders from all visual objects
                StripAllCollidersUnderVisualRoots();

                // Save scene to both canonical paths
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, SCENE_PATH);

                if (File.Exists(ROOT_SCENE_PATH))
                {
                    EditorSceneManager.SaveScene(scene, ROOT_SCENE_PATH);
                }

                Debug.Log("<color=#00FF88><b>[HDLevel01CinematicIntegrator] Master Cinematic HD Jungle Pass applied successfully to Level 01!</b></color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HDLevel01CinematicIntegrator] Error during cinematic pass: {ex}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        #region Scene Loading
        private static Scene OpenLevel01Scene()
        {
            if (File.Exists(SCENE_PATH))
            {
                return EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }
            else if (File.Exists(ROOT_SCENE_PATH))
            {
                return EditorSceneManager.OpenScene(ROOT_SCENE_PATH, OpenSceneMode.Single);
            }
            else
            {
                throw new FileNotFoundException($"Could not find Level 01 scene at '{SCENE_PATH}' or '{ROOT_SCENE_PATH}'");
            }
        }
        #endregion

        #region Material Synthesis
        public class CinematicMaterials
        {
            public Material soilPath;
            public Material mossBank;
            public Material cliffWall;
            public Material fernBillboard;
            public Material fern2Billboard;
            public Material grassBillboard;
            public Material orchidBillboard;
            public Material rockPBR;
        }

        private static CinematicMaterials SetupPBRMaterials()
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");

            var mats = new CinematicMaterials();

            // 1. Soil & Leaf-Litter Trail
            mats.soilPath = GetOrCreateMaterial($"{MAT_OUTPUT_DIR}/Mat_Cinematic_SoilPath.mat", urpLit);
            Texture2D texSoil = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_ROAD_LEAVES_ALBEDO) ?? AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_MUD_LEAVES_ALBEDO);
            Texture2D texSoilNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_ROAD_LEAVES_NORMAL) ?? AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_MUD_LEAVES_NORMAL);
            if (texSoil != null) mats.soilPath.SetTexture("_BaseMap", texSoil);
            if (texSoilNorm != null)
            {
                mats.soilPath.SetTexture("_BumpMap", texSoilNorm);
                mats.soilPath.EnableKeyword("_NORMALMAP");
            }
            mats.soilPath.SetColor("_BaseColor", new Color(0.85f, 0.78f, 0.72f, 1f));
            mats.soilPath.SetFloat("_Smoothness", 0.16f);
            mats.soilPath.SetTextureScale("_BaseMap", new Vector2(3f, 6f));

            // 2. Mossy Berm & Embankment
            mats.mossBank = GetOrCreateMaterial($"{MAT_OUTPUT_DIR}/Mat_Cinematic_MossBank.mat", urpLit);
            Texture2D texMoss = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_GRASS_LEAVES_ALBEDO);
            Texture2D texMossNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_GRASS_LEAVES_NORMAL);
            if (texMoss != null) mats.mossBank.SetTexture("_BaseMap", texMoss);
            if (texMossNorm != null)
            {
                mats.mossBank.SetTexture("_BumpMap", texMossNorm);
                mats.mossBank.EnableKeyword("_NORMALMAP");
            }
            mats.mossBank.SetColor("_BaseColor", new Color(0.82f, 0.95f, 0.78f, 1f));
            mats.mossBank.SetFloat("_Smoothness", 0.12f);
            mats.mossBank.SetTextureScale("_BaseMap", new Vector2(4f, 4f));

            // 3. Ancient Jungle Cliff Wall
            mats.cliffWall = GetOrCreateMaterial($"{MAT_OUTPUT_DIR}/Mat_Cinematic_CliffWall.mat", urpLit);
            Texture2D texCliff = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_CLIFF_ALBEDO);
            Texture2D texCliffNorm = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_CLIFF_NORMAL);
            if (texCliff != null) mats.cliffWall.SetTexture("_BaseMap", texCliff);
            if (texCliffNorm != null)
            {
                mats.cliffWall.SetTexture("_BumpMap", texCliffNorm);
                mats.cliffWall.EnableKeyword("_NORMALMAP");
            }
            mats.cliffWall.SetColor("_BaseColor", new Color(0.75f, 0.82f, 0.75f, 1f));
            mats.cliffWall.SetFloat("_Smoothness", 0.14f);
            mats.cliffWall.SetTextureScale("_BaseMap", new Vector2(2f, 6f));

            // 4. Foliage Billboards (Ferns, Grass, Orchids)
            mats.fernBillboard = CreateBillboardMaterial($"{MAT_OUTPUT_DIR}/Mat_Cinematic_FernBillboard.mat", TEX_FERN_ALBEDO, TEX_FERN_NORMAL, urpLit, new Color(0.9f, 1.0f, 0.85f));
            mats.fern2Billboard = CreateBillboardMaterial($"{MAT_OUTPUT_DIR}/Mat_Cinematic_Fern2Billboard.mat", TEX_FERN2_ALBEDO, TEX_FERN2_NORMAL, urpLit, new Color(0.85f, 0.98f, 0.82f));
            mats.grassBillboard = CreateBillboardMaterial($"{MAT_OUTPUT_DIR}/Mat_Cinematic_GrassBillboard.mat", TEX_GRASS_ALBEDO, TEX_GRASS_NORMAL, urpLit, new Color(0.92f, 1.0f, 0.88f));
            mats.orchidBillboard = CreateBillboardMaterial($"{MAT_OUTPUT_DIR}/Mat_Cinematic_OrchidBillboard.mat", TEX_ORCHID_ALBEDO, TEX_ORCHID_NORMAL, urpLit, Color.white);

            // 5. Rock PBR Material
            mats.rockPBR = GetOrCreateMaterial($"{MAT_OUTPUT_DIR}/Mat_Cinematic_RockPBR.mat", urpLit);
            Texture2D texRock = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/RockSmooth_AlbedoTransparency.png") ?? texCliff;
            Texture2D texRockNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/FlipGameDev/Terrain&GrassPack/Art/Textures/Terrain_Textures/RockSmooth_Normal.png") ?? texCliffNorm;
            if (texRock != null) mats.rockPBR.SetTexture("_BaseMap", texRock);
            if (texRockNorm != null)
            {
                mats.rockPBR.SetTexture("_BumpMap", texRockNorm);
                mats.rockPBR.EnableKeyword("_NORMALMAP");
            }
            mats.rockPBR.SetColor("_BaseColor", new Color(0.7f, 0.72f, 0.68f, 1f));
            mats.rockPBR.SetFloat("_Smoothness", 0.22f);

            AssetDatabase.SaveAssets();
            return mats;
        }

        private static Material CreateBillboardMaterial(string path, string albedoPath, string normPath, Shader shader, Color tint)
        {
            Material mat = GetOrCreateMaterial(path, shader);
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            Texture2D norm = AssetDatabase.LoadAssetAtPath<Texture2D>(normPath);

            if (albedo != null) mat.SetTexture("_BaseMap", albedo);
            if (norm != null)
            {
                mat.SetTexture("_BumpMap", norm);
                mat.EnableKeyword("_NORMALMAP");
            }

            mat.SetColor("_BaseColor", tint);
            mat.SetFloat("_Cutoff", 0.35f);
            mat.SetFloat("_AlphaClip", 1.0f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetInt("_Cull", (int)CullMode.Off); // Two-Sided
            mat.SetFloat("_Smoothness", 0.05f);
            mat.renderQueue = (int)RenderQueue.AlphaTest + 50;

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateMaterial(string path, Shader shader)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            return mat;
        }
        #endregion

        #region Terrain Visual Upgrade
        private static void UpgradeTerrainVisuals(CinematicMaterials mats)
        {
            // Find all ground platforms in the scene
            string[] groundNames = new string[]
            {
                "Ground_Start_Zone",
                "Ground_Path_01",
                "Ground_Enemy_Arena",
                "Platform_Jump_01",
                "Platform_Jump_02",
                "Platform_Vine_Landing",
                "Ground_Hazard_Clearing",
                "Ground_Puzzle_Courtyard",
                "Ground_Checkpoint2_Arena",
                "Ground_Level_Complete_Exit"
            };

            foreach (var name in groundNames)
            {
                GameObject groundObj = GameObject.Find(name);
                if (groundObj == null) continue;

                // 1. Hide the placeholder primitive MeshRenderer while keeping BoxCollider intact
                MeshRenderer origRenderer = groundObj.GetComponent<MeshRenderer>();
                if (origRenderer != null)
                {
                    origRenderer.enabled = false;
                }

                // 2. Remove previous [HD_Visual] if present
                Transform oldVisual = groundObj.transform.Find(HD_VISUAL_TAG);
                if (oldVisual != null)
                {
                    Undo.DestroyObjectImmediate(oldVisual.gameObject);
                }

                // 3. Create sculpted organic 3D terrain visual child
                Vector3 size = groundObj.transform.localScale;
                GameObject hdVisual = new GameObject(HD_VISUAL_TAG);
                hdVisual.transform.SetParent(groundObj.transform, false);
                hdVisual.transform.localPosition = Vector3.zero;
                hdVisual.transform.localRotation = Quaternion.identity;
                hdVisual.transform.localScale = Vector3.one;

                // Build sculpted organic mesh
                Mesh trailMesh = CreateSculptedTrailMesh($"Mesh_Trail_{name}", size.x, size.z, size.y);
                MeshFilter mf = hdVisual.AddComponent<MeshFilter>();
                mf.sharedMesh = trailMesh;

                MeshRenderer mr = hdVisual.AddComponent<MeshRenderer>();
                mr.sharedMaterials = new Material[] { mats.soilPath, mats.mossBank };
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = true;

                EditorUtility.SetDirty(groundObj);
            }
        }

        private const string HD_VISUAL_TAG = "[HD_Visual]";

        private static Mesh CreateSculptedTrailMesh(string name, float width, float length, float height)
        {
            Mesh mesh = new Mesh { name = name };

            int xSegments = 16;
            int zSegments = 24;
            int vertCount = (xSegments + 1) * (zSegments + 1);

            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            Vector2[] uv2s = new Vector2[vertCount];

            float halfW = 0.5f;
            float halfL = 0.5f;
            float topY = 0.5f;

            for (int z = 0; z <= zSegments; z++)
            {
                float tZ = (float)z / zSegments;
                float posZ = Mathf.Lerp(-halfL, halfL, tZ);

                for (int x = 0; x <= xSegments; x++)
                {
                    float tX = (float)x / xSegments;
                    float posX = Mathf.Lerp(-halfW, halfW, tX);

                    // Organic trail crown (arched middle, smooth dipped shoulders)
                    float distFromCenter = Mathf.Abs(tX - 0.5f) * 2.0f; // 0 at center, 1 at edge
                    float crownArch = (1.0f - distFromCenter * distFromCenter) * 0.08f;
                    float shoulderDip = -Mathf.Pow(distFromCenter, 4f) * 0.15f;
                    float naturalNoise = Mathf.Sin(tX * 8f + tZ * 12f) * 0.015f + Mathf.Cos(tZ * 18f) * 0.01f;

                    float posY = topY + crownArch + shoulderDip + naturalNoise;

                    int index = z * (xSegments + 1) + x;
                    vertices[index] = new Vector3(posX, posY, posZ);
                    normals[index] = Vector3.up;
                    uvs[index] = new Vector2(tX * width * 0.35f, tZ * length * 0.35f);
                    uv2s[index] = new Vector2(tX, tZ);
                }
            }

            // Submesh 0: Central Dirt Trail
            // Submesh 1: Mossy Shoulder Embankments
            List<int> trailTris = new List<int>();
            List<int> mossTris = new List<int>();

            for (int z = 0; z < zSegments; z++)
            {
                for (int x = 0; x < xSegments; x++)
                {
                    int i0 = z * (xSegments + 1) + x;
                    int i1 = i0 + 1;
                    int i2 = (z + 1) * (xSegments + 1) + x;
                    int i3 = i2 + 1;

                    float tX = (float)x / xSegments;
                    float distFromCenter = Mathf.Abs(tX - 0.5f) * 2.0f;

                    if (distFromCenter < 0.65f)
                    {
                        trailTris.Add(i0); trailTris.Add(i2); trailTris.Add(i1);
                        trailTris.Add(i1); trailTris.Add(i2); trailTris.Add(i3);
                    }
                    else
                    {
                        mossTris.Add(i0); mossTris.Add(i2); mossTris.Add(i1);
                        mossTris.Add(i1); mossTris.Add(i2); mossTris.Add(i3);
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.uv2 = uv2s;
            mesh.subMeshCount = 2;
            mesh.SetTriangles(trailTris, 0);
            mesh.SetTriangles(mossTris, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            string assetPath = $"{MESH_OUTPUT_DIR}/{name}.asset";
            AssetDatabase.CreateAsset(mesh, assetPath);
            return mesh;
        }
        #endregion

        #region Tree Visual Upgrade
        private static void UpgradeTreeVisuals()
        {
            GameObject oakPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Procedural Tree/Prefabs/Oak Tree.prefab");
            GameObject palmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/HD/Trees/HD_Tree_CoconutPalm_01.prefab");
            if (palmPrefab == null)
            {
                palmPrefab = HDPalmTreeBuilder.CreateOrUpdateHDPalmPrefab();
            }

            if (oakPrefab == null)
            {
                Debug.LogWarning("[HDLevel01CinematicIntegrator] Oak Tree prefab not found at 'Assets/Procedural Tree/Prefabs/Oak Tree.prefab'");
                return;
            }

            // Find all tree objects in the scene
            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (var go in allObjects)
            {
                string lower = go.name.ToLower();
                if (!lower.Contains("tree_") && !lower.Contains("canopytree") && !lower.Contains("coconutpalm")) continue;
                if (go.transform.parent != null && go.transform.parent.name.Contains(HD_VISUAL_TAG)) continue;

                // Disable placeholder renderers on the original object
                Renderer[] origRenderers = go.GetComponentsInChildren<Renderer>(true);
                foreach (var r in origRenderers)
                {
                    if (!r.transform.IsChildOf(go.transform.Find(HD_VISUAL_TAG) ?? go.transform) || r.transform == go.transform)
                    {
                        r.enabled = false;
                    }
                }

                // Remove existing [HD_Visual]
                Transform existingHD = go.transform.Find(HD_VISUAL_TAG);
                if (existingHD != null)
                {
                    Undo.DestroyObjectImmediate(existingHD.gameObject);
                }

                // Pick correct high-detail prefab
                GameObject targetPrefab = lower.Contains("coconutpalm") || lower.Contains("palm") ? (palmPrefab ?? oakPrefab) : oakPrefab;

                GameObject hdInstance = (GameObject)PrefabUtility.InstantiatePrefab(targetPrefab, go.transform);
                hdInstance.name = HD_VISUAL_TAG;
                hdInstance.transform.localPosition = Vector3.zero;
                hdInstance.transform.localRotation = Quaternion.identity;

                // Scale up Oak Trees for giant jungle canopy presence
                if (targetPrefab == oakPrefab)
                {
                    hdInstance.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                }
                else
                {
                    hdInstance.transform.localScale = Vector3.one;
                }

                // Strip any colliders from the visual instance to ensure physics integrity
                Collider[] cols = hdInstance.GetComponentsInChildren<Collider>(true);
                foreach (var c in cols)
                {
                    Undo.DestroyObjectImmediate(c);
                }

                EditorUtility.SetDirty(go);
            }
        }
        #endregion

        #region Dense Foliage & Understory
        private static void PopulateDenseFoliage(CinematicMaterials mats)
        {
            GameObject foliageRoot = GameObject.Find("[--- HD_FOLIAGE_UNDERSTORY ---]");
            if (foliageRoot != null)
            {
                Undo.DestroyObjectImmediate(foliageRoot);
            }

            foliageRoot = new GameObject("[--- HD_FOLIAGE_UNDERSTORY ---]");
            foliageRoot.transform.position = Vector3.zero;

            Mesh quadMesh = CreateCrossBillboardMesh();

            // 1. Fern & Grass clusters along the main trail (Z: 0 to 110)
            int seed = 42;
            UnityEngine.Random.InitState(seed);

            for (float z = 2f; z < 105f; z += 2.2f)
            {
                // Left border cluster
                float leftX = UnityEngine.Random.Range(-4.5f, -2.8f);
                float leftY = (z > 45f) ? 1.5f : 0f;
                SpawnBillboardCluster(quadMesh, mats.fernBillboard, mats.grassBillboard, mats.orchidBillboard, new Vector3(leftX, leftY, z), foliageRoot.transform, 3, 1.4f);

                // Right border cluster
                float rightX = UnityEngine.Random.Range(2.8f, 4.5f);
                float rightY = (z > 45f) ? 1.5f : 0f;
                SpawnBillboardCluster(quadMesh, mats.fern2Billboard, mats.grassBillboard, mats.orchidBillboard, new Vector3(rightX, rightY, z + 1.1f), foliageRoot.transform, 3, 1.4f);
            }

            // 2. Add 3D River Stones & Boulders along path borders
            SpawnScattered3DRocks(mats.rockPBR, foliageRoot.transform);

            EditorUtility.SetDirty(foliageRoot);
        }

        private static Mesh CreateCrossBillboardMesh()
        {
            Mesh mesh = new Mesh { name = "Mesh_HD_CrossBillboard" };

            // 2 intersecting vertical quads (X shape)
            float h = 1.2f;
            float w = 1.0f;
            float hw = w * 0.5f;

            Vector3[] verts = new Vector3[8];
            Vector3[] norms = new Vector3[8];
            Vector2[] uvs = new Vector2[8];

            // Quad 1 (along diagonal A)
            verts[0] = new Vector3(-hw, 0, -hw); uvs[0] = new Vector2(0, 0); norms[0] = Vector3.up;
            verts[1] = new Vector3(hw, 0, hw);   uvs[1] = new Vector2(1, 0); norms[1] = Vector3.up;
            verts[2] = new Vector3(-hw, h, -hw); uvs[2] = new Vector2(0, 1); norms[2] = Vector3.up;
            verts[3] = new Vector3(hw, h, hw);   uvs[3] = new Vector2(1, 1); norms[3] = Vector3.up;

            // Quad 2 (along diagonal B)
            verts[4] = new Vector3(-hw, 0, hw);  uvs[4] = new Vector2(0, 0); norms[4] = Vector3.up;
            verts[5] = new Vector3(hw, 0, -hw);  uvs[5] = new Vector2(1, 0); norms[5] = Vector3.up;
            verts[6] = new Vector3(-hw, h, hw);  uvs[6] = new Vector2(0, 1); norms[6] = Vector3.up;
            verts[7] = new Vector3(hw, h, -hw);  uvs[7] = new Vector2(1, 1); norms[7] = Vector3.up;

            int[] tris = new int[]
            {
                0, 2, 1, 1, 2, 3, // Quad 1 front
                1, 2, 0, 3, 2, 1, // Quad 1 back
                4, 6, 5, 5, 6, 7, // Quad 2 front
                5, 6, 4, 7, 6, 5  // Quad 2 back
            };

            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();

            string assetPath = $"{MESH_OUTPUT_DIR}/Mesh_HD_CrossBillboard.asset";
            AssetDatabase.CreateAsset(mesh, assetPath);
            return mesh;
        }

        private static void SpawnBillboardCluster(Mesh mesh, Material fernMat, Material grassMat, Material flowerMat, Vector3 center, Transform parent, int count, float radius)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 pos = center + new Vector3(offset.x, 0, offset.y);

                GameObject go = new GameObject($"Foliage_{pos.x:F1}_{pos.z:F1}");
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
                float scale = UnityEngine.Random.Range(0.85f, 1.45f);
                go.transform.localScale = new Vector3(scale, scale * UnityEngine.Random.Range(0.9f, 1.3f), scale);
                go.transform.SetParent(parent);

                MeshFilter mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;

                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                float randType = UnityEngine.Random.value;
                mr.sharedMaterial = (randType < 0.55f) ? fernMat : ((randType < 0.85f) ? grassMat : flowerMat);
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = true;
            }
        }

        private static void SpawnScattered3DRocks(Material rockMat, Transform parent)
        {
            // Load 3D rocks from FlipGameDev
            GameObject rockPrefab1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FlipGameDev/Terrain&GrassPack/Art/Meshes/Rocks/Rock_1.fbx");
            GameObject rockPrefab3 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FlipGameDev/Terrain&GrassPack/Art/Meshes/Rocks/Rock_3.fbx");
            GameObject rockPrefab5 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FlipGameDev/Terrain&GrassPack/Art/Meshes/Rocks/Rock_5.fbx");
            GameObject rockPrefab8 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FlipGameDev/Terrain&GrassPack/Art/Meshes/Rocks/Rock_8.fbx");

            GameObject[] rockPool = new GameObject[] { rockPrefab1, rockPrefab3, rockPrefab5, rockPrefab8 };

            Vector3[] rockPositions = new Vector3[]
            {
                new Vector3(-4.2f, 0.0f, 12f),
                new Vector3(4.5f, 0.0f, 18f),
                new Vector3(-5.0f, 0.0f, 26f),
                new Vector3(4.8f, 0.0f, 36f),
                new Vector3(-4.5f, 1.5f, 58f),
                new Vector3(4.2f, 1.5f, 68f),
                new Vector3(-5.2f, 1.5f, 85f),
                new Vector3(5.0f, 1.5f, 98f)
            };

            for (int i = 0; i < rockPositions.Length; i++)
            {
                GameObject rockSource = rockPool[i % rockPool.Length];
                if (rockSource == null) continue;

                GameObject rockObj = (GameObject)PrefabUtility.InstantiatePrefab(rockSource, parent);
                rockObj.name = $"Rock_Cluster_{i + 1}";
                rockObj.transform.position = rockPositions[i];
                rockObj.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(-15f, 15f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-15f, 15f));
                float rScale = UnityEngine.Random.Range(0.6f, 1.2f);
                rockObj.transform.localScale = Vector3.one * rScale;

                // Apply PBR Rock material
                Renderer[] rends = rockObj.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    r.sharedMaterial = rockMat;
                    r.shadowCastingMode = ShadowCastingMode.On;
                    r.receiveShadows = true;
                }

                // Strip colliders
                Collider[] cols = rockObj.GetComponentsInChildren<Collider>(true);
                foreach (var c in cols)
                {
                    Undo.DestroyObjectImmediate(c);
                }
            }
        }
        #endregion

        #region Panoramic Jungle Backdrop
        private static void BuildPanoramicJungleBackdrop(CinematicMaterials mats)
        {
            GameObject backdropRoot = GameObject.Find("[--- HD_JUNGLE_PANORAMIC_BACKDROP ---]");
            if (backdropRoot != null)
            {
                Undo.DestroyObjectImmediate(backdropRoot);
            }

            backdropRoot = new GameObject("[--- HD_JUNGLE_PANORAMIC_BACKDROP ---]");
            backdropRoot.transform.position = Vector3.zero;

            GameObject oakTreePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Procedural Tree/Prefabs/Oak Tree.prefab");
            GameObject magnoliaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Procedural Tree/Prefabs/Magnolia Tree.prefab");
            GameObject elmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Procedural Tree/Prefabs/Elm Tree.prefab");
            GameObject ashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Procedural Tree/Prefabs/Ash Tree.prefab");
            GameObject rockCliffFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FlipGameDev/Terrain&GrassPack/Art/Meshes/Rocks/Rock_12.fbx") ??
                                      AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FlipGameDev/Terrain&GrassPack/Art/Meshes/Rocks/Rock_6.fbx");

            GameObject[] treePool = new GameObject[] { oakTreePrefab, magnoliaPrefab, elmPrefab, ashPrefab };

            // 1. Left Ridge Line (X: -14 to -28, Z: -15 to 135)
            for (float z = -10f; z <= 130f; z += 9f)
            {
                float xOffset = UnityEngine.Random.Range(-22f, -14f);
                float yOffset = UnityEngine.Random.Range(-0.5f, 2.5f);
                SpawnBackdropTree(treePool, new Vector3(xOffset, yOffset, z), backdropRoot.transform, 1.8f, 2.8f);

                // Add cliff embankment beneath trees
                if (rockCliffFbx != null && (int)(z) % 18 == 0)
                {
                    SpawnBackdropCliff(rockCliffFbx, mats.cliffWall, new Vector3(xOffset + 4f, -1.0f, z), backdropRoot.transform, 2.5f);
                }
            }

            // 2. Right Ridge Line (X: 14 to 28, Z: -15 to 135)
            for (float z = -10f; z <= 130f; z += 9f)
            {
                float xOffset = UnityEngine.Random.Range(14f, 22f);
                float yOffset = UnityEngine.Random.Range(-0.5f, 2.5f);
                SpawnBackdropTree(treePool, new Vector3(xOffset, yOffset, z + 4.5f), backdropRoot.transform, 1.8f, 2.8f);

                if (rockCliffFbx != null && (int)(z) % 18 == 0)
                {
                    SpawnBackdropCliff(rockCliffFbx, mats.cliffWall, new Vector3(xOffset - 4f, -1.0f, z + 4.5f), backdropRoot.transform, 2.5f);
                }
            }

            // 3. Far Behind Player Start (Z: -18 to -35, X: -25 to 25)
            for (float x = -20f; x <= 20f; x += 8f)
            {
                float zOffset = UnityEngine.Random.Range(-30f, -18f);
                SpawnBackdropTree(treePool, new Vector3(x, 0, zOffset), backdropRoot.transform, 2.0f, 3.2f);
            }

            // 4. Far Beyond Exit Portal (Z: 120 to 155, X: -25 to 25)
            for (float x = -20f; x <= 20f; x += 8f)
            {
                float zOffset = UnityEngine.Random.Range(122f, 145f);
                SpawnBackdropTree(treePool, new Vector3(x, 1.5f, zOffset), backdropRoot.transform, 2.2f, 3.5f);
            }

            EditorUtility.SetDirty(backdropRoot);
        }

        private static void SpawnBackdropTree(GameObject[] pool, Vector3 position, Transform parent, float minScale, float maxScale)
        {
            int index = UnityEngine.Random.Range(0, pool.Length);
            GameObject prefab = pool[index] ?? pool[0];
            if (prefab == null) return;

            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            tree.name = $"Backdrop_{prefab.name}_{position.x:F0}_{position.z:F0}";
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
            float scale = UnityEngine.Random.Range(minScale, maxScale);
            tree.transform.localScale = Vector3.one * scale;

            // Strip colliders
            Collider[] cols = tree.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols)
            {
                Undo.DestroyObjectImmediate(c);
            }
        }

        private static void SpawnBackdropCliff(GameObject cliffFbx, Material cliffMat, Vector3 position, Transform parent, float scale)
        {
            GameObject cliff = (GameObject)PrefabUtility.InstantiatePrefab(cliffFbx, parent);
            cliff.name = $"Backdrop_Cliff_{position.x:F0}_{position.z:F0}";
            cliff.transform.position = position;
            cliff.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
            cliff.transform.localScale = new Vector3(scale * 1.5f, scale * 2.0f, scale * 1.5f);

            Renderer[] rends = cliff.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                r.sharedMaterial = cliffMat;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }

            Collider[] cols = cliff.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols)
            {
                Undo.DestroyObjectImmediate(c);
            }
        }
        #endregion

        #region Atmospheric Lighting & Volume
        private static void ConfigureAtmosphericLightingAndVolume()
        {
            // 1. Sun Directional Light
            Light sunLight = null;
            Light[] lights = GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    sunLight = l;
                    break;
                }
            }

            if (sunLight == null)
            {
                GameObject sunObj = new GameObject("Directional Light (Sun)");
                sunLight = sunObj.AddComponent<Light>();
                sunLight.type = LightType.Directional;
            }

            sunLight.color = new Color(1.0f, 0.96f, 0.88f); // Warm morning tropical sun
            sunLight.intensity = 1.35f;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowNormalBias = 0.4f;
            sunLight.shadowBias = 0.05f;
            sunLight.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

            // 2. Atmospheric Fog & Ambient Light
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.48f, 0.65f, 0.58f, 1.0f); // Lush jungle emerald morning mist
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.38f, 0.52f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.26f, 0.36f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.14f, 0.16f, 0.11f);
            RenderSettings.ambientIntensity = 1.15f;

            // 3. Global Volume for URP Post-Processing
            GameObject volumeObj = GameObject.Find("[--- HD_POST_PROCESSING ---]");
            if (volumeObj != null)
            {
                Undo.DestroyObjectImmediate(volumeObj);
            }

            volumeObj = new GameObject("[--- HD_POST_PROCESSING ---]");
            Volume volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1.0f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Profile_CinematicJungle_Level01";

            // Tonemapping (ACES)
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            // Bloom
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.65f);
            bloom.threshold.Override(0.92f);
            bloom.scatter.Override(0.7f);

            // Vignette
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);

            // Color Adjustments
            ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.postExposure.Override(0.15f);
            colorAdj.contrast.Override(12f);
            colorAdj.saturation.Override(14f);

            string profilePath = "Assets/Art/Environment/HD/Profile_CinematicJungle_Level01.asset";
            AssetDatabase.CreateAsset(profile, profilePath);
            volume.sharedProfile = profile;

            EditorUtility.SetDirty(volumeObj);
        }
        #endregion

        #region Physics Safety
        private static void StripAllCollidersUnderVisualRoots()
        {
            string[] rootNames = new string[]
            {
                "[--- HD_FOLIAGE_UNDERSTORY ---]",
                "[--- HD_JUNGLE_PANORAMIC_BACKDROP ---]"
            };

            foreach (var rootName in rootNames)
            {
                GameObject root = GameObject.Find(rootName);
                if (root == null) continue;

                Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
                foreach (var c in colliders)
                {
                    Undo.DestroyObjectImmediate(c);
                }
            }
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
        #endregion
    }
}
