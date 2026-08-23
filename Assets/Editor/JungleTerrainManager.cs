using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Master 3D Jungle Terrain Manager and HD Upgrade Integrator for Level01_Awakening.
    /// Replaces low-poly/placeholder environment with a production-grade 3D Unity Terrain
    /// utilizing the Terrain Sample Asset Pack (PBR layers, sculpted valley path, gentle jungle hills,
    /// detail vegetation meshes, and instanced rendering).
    /// Strictly preserves Monkey_B3, player controller, camera, collectibles, triggers, and game logic.
    /// </summary>
    [InitializeOnLoad]
    public class JungleTerrainManager : EditorWindow
    {
        private const string SCENE_PATH = "Assets/Scenes/Level01_Awakening.unity";
        private const string BACKUP_DIR = "Assets/Backups";
        private const string TERRAIN_DATA_DIR = "Assets/Art/Environment/Terrain";
        private const string TERRAIN_DATA_PATH = "Assets/Art/Environment/Terrain/Level01_JungleTerrainData.asset";

        private const string TERRAIN_LAYERS_DIR = "Assets/TerrainSampleAssets/TerrainLayers";
        private const string TERRAIN_PREFABS_DIR = "Assets/TerrainSampleAssets/Prefabs";

        // Terrain Dimensions
        private const float TERRAIN_WIDTH = 160f;   // X: -80 to +80
        private const float TERRAIN_LENGTH = 200f;  // Z: -40 to +160
        private const float TERRAIN_HEIGHT = 40f;   // Max Y elevation
        private static readonly Vector3 TERRAIN_POS = new Vector3(-80f, 0f, -40f);

        private const int HEIGHTMAP_RES = 513;
        private const int ALPHAMAP_RES = 512;
        private const int DETAIL_RES = 512;
        private const int DETAIL_PATCH_RES = 16;

        private Vector2 _scrollPos;
        private string _statusMessage = "Ready to generate 3D Jungle Terrain.";
        private MessageType _statusMessageType = MessageType.Info;

        static JungleTerrainManager()
        {
            EditorApplication.delayCall += AutoEnsureTerrainApplied;
        }

        private static void AutoEnsureTerrainApplied()
        {
            GameObject existingTerrain = GameObject.Find("Level01_JungleTerrain");
            if (!File.Exists(TERRAIN_DATA_PATH) || existingTerrain == null)
            {
                var mgr = CreateInstance<JungleTerrainManager>();
                mgr.ExecuteJungleTerrainUpgrade();
                DestroyImmediate(mgr);
            }
        }

        [MenuItem("Window/Monkey Adventure/🌴 Build HD Jungle Terrain (Level 01)", false, 120)]
        public static void OpenWindow()
        {
            var win = GetWindow<JungleTerrainManager>("Jungle Terrain Manager", true);
            win.minSize = new Vector2(480, 520);
            win.Show();
        }

        [MenuItem("Window/Monkey Adventure/🚀 Generate Level 01 Jungle Terrain [Quick]", false, 121)]
        public static void ExecuteTerrainBuildMenuItem()
        {
            var mgr = CreateInstance<JungleTerrainManager>();
            mgr.ExecuteJungleTerrainUpgrade();
            DestroyImmediate(mgr);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.Space(8);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("🌴 Level 01 — HD Jungle Terrain Builder 🌴", titleStyle);
            EditorGUILayout.HelpBox(
                "Builds a high-quality 3D tropical jungle terrain using the Terrain Sample Asset Pack.\n" +
                "• Sculpted central path corridor matching player progression\n" +
                "• Natural rolling jungle hills & boundary berms\n" +
                "• 5 PBR Terrain Layers (Mud, Pebbles, Grass, Moss, Rock)\n" +
                "• Instanced detail vegetation (Ferns, Bushes, Plants, Grass)\n" +
                "• 100% preservation of Monkey_B3, player spawn, camera, and gameplay systems.",
                MessageType.Info);

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(_statusMessage, _statusMessageType);

            EditorGUILayout.Space(12);
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
            if (GUILayout.Button("✨ BUILD & APPLY 3D JUNGLE TERRAIN", GUILayout.Height(42)))
            {
                ExecuteJungleTerrainUpgrade();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(6);
            if (GUILayout.Button("🔍 Run Full Scene & Safety Verification", GUILayout.Height(30)))
            {
                RunSafetyVerification();
            }

            EditorGUILayout.EndScrollView();
        }

        public void ExecuteJungleTerrainUpgrade()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Jungle Terrain Builder", "Backing up Level 01 Scene...", 0.05f);
                EnsureDirectories();
                BackupScene();

                EditorUtility.DisplayProgressBar("Jungle Terrain Builder", "Opening Scene...", 0.15f);
                OpenTargetScene();

                EditorUtility.DisplayProgressBar("Jungle Terrain Builder", "Cleaning Placeholder Renderers...", 0.25f);
                CleanEnvironmentPlaceholders();

                EditorUtility.DisplayProgressBar("Jungle Terrain Builder", "Generating Terrain Data & Heightmap...", 0.40f);
                TerrainData terrainData = CreateOrLoadTerrainData();
                SculptHeightmap(terrainData);

                EditorUtility.DisplayProgressBar("Jungle Terrain Builder", "Configuring PBR Terrain Layers & Alphamaps...", 0.60f);
                SetupTerrainLayers(terrainData);
                PaintAlphamaps(terrainData);

                EditorUtility.DisplayProgressBar("Jungle Terrain Builder", "Populating Detail Vegetation & Foliage...", 0.75f);
                SetupDetailPrototypes(terrainData);
                PaintDetailLayers(terrainData);

                EditorUtility.DisplayProgressBar("Jungle Terrain Builder", "Instantiating Terrain GameObject...", 0.85f);
                SetupSceneTerrainObject(terrainData);

                EditorUtility.DisplayProgressBar("Jungle Terrain Builder", "Verifying Safety & Gameplay Integrity...", 0.92f);
                RunSafetyVerification();

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                _statusMessage = "✅ 3D Jungle Terrain successfully generated and integrated into Level01_Awakening!";
                _statusMessageType = MessageType.Info;
                Debug.Log("<color=#00FF88><b>[JungleTerrainManager] Level 01 3D Jungle Terrain Upgrade COMPLETE!</b></color>");
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error during terrain upgrade: {ex.Message}";
                _statusMessageType = MessageType.Error;
                Debug.LogError($"[JungleTerrainManager] Exception: {ex}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(BACKUP_DIR)) Directory.CreateDirectory(BACKUP_DIR);
            if (!Directory.Exists(TERRAIN_DATA_DIR)) Directory.CreateDirectory(TERRAIN_DATA_DIR);
        }

        private static void BackupScene()
        {
            if (File.Exists(SCENE_PATH))
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = $"{BACKUP_DIR}/Level01_Awakening_PreTerrainUpgrade_{timestamp}.unity";
                File.Copy(SCENE_PATH, backupPath, true);
                Debug.Log($"[JungleTerrainManager] Scene backed up to: {backupPath}");
            }
        }

        private static void OpenTargetScene()
        {
            if (SceneManager.GetActiveScene().path != SCENE_PATH)
            {
                if (File.Exists(SCENE_PATH))
                {
                    EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
                }
            }
        }

        private static void CleanEnvironmentPlaceholders()
        {
            GameObject envRoot = GameObject.Find("[--- 01_ENVIRONMENT ---]");
            if (envRoot == null) return;

            // Find all child mesh renderers in 01_ENVIRONMENT
            MeshRenderer[] renderers = envRoot.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in renderers)
            {
                if (mr == null) continue;
                GameObject go = mr.gameObject;
                string lower = go.name.ToLowerInvariant();

                // Do NOT disable the new terrain itself
                if (lower.Contains("jungleterrain")) continue;

                // Check if this object or any parent is an old ground platform
                bool isGroundOrPlatform = false;
                Transform curr = go.transform;
                while (curr != null && curr.gameObject != envRoot)
                {
                    string cName = curr.name.ToLowerInvariant();
                    if (cName.StartsWith("ground_") || cName.StartsWith("platform_vine_landing") || cName.Contains("ground_tile"))
                    {
                        isGroundOrPlatform = true;
                        break;
                    }
                    curr = curr.parent;
                }

                // Disable old primitive placeholder ground tiles and child visuals ([HD_Visual], [HD_Terrain])
                if (isGroundOrPlatform || lower.StartsWith("ground_") || lower.Contains("[hd_visual]") || lower.Contains("[hd_terrain]"))
                {
                    mr.enabled = false;
                }

                // Disable old low-poly placeholder trees/plants/rocks
                if (lower.Contains("tree_junglecanopy") || lower.Contains("tree_coconutpalm") ||
                    lower.Contains("plant_junglefern") || lower.Contains("plant_tropicalbush") ||
                    lower.Contains("plant_glowingmushroom") || lower.Contains("plant_hibiscusflower") ||
                    lower.Contains("rock_mossyboulder"))
                {
                    mr.enabled = false;
                }
            }
        }

        private static TerrainData CreateOrLoadTerrainData()
        {
            TerrainData tData = AssetDatabase.LoadAssetAtPath<TerrainData>(TERRAIN_DATA_PATH);
            if (tData == null)
            {
                tData = new TerrainData();
                AssetDatabase.CreateAsset(tData, TERRAIN_DATA_PATH);
            }

            tData.heightmapResolution = HEIGHTMAP_RES;
            tData.size = new Vector3(TERRAIN_WIDTH, TERRAIN_HEIGHT, TERRAIN_LENGTH);
            tData.SetDetailResolution(DETAIL_RES, DETAIL_PATCH_RES);
            EditorUtility.SetDirty(tData);
            return tData;
        }

        private static void SculptHeightmap(TerrainData tData)
        {
            int res = tData.heightmapResolution;
            float[,] heights = new float[res, res];

            float invRes = 1.0f / (res - 1);

            for (int z = 0; z < res; z++)
            {
                float zNorm = z * invRes;
                float worldZ = TERRAIN_POS.z + zNorm * TERRAIN_LENGTH;

                // Target base path elevation along the level's Z progression
                float basePathY;
                if (worldZ < 35f)
                {
                    basePathY = 0.0f; // Start zone, initial path, enemy arena
                }
                else if (worldZ <= 53f)
                {
                    // Gentle slope leading up to vine platform (Z=35..53 rising 0 -> 1.5m)
                    float t = (worldZ - 35f) / 18f;
                    float smoothT = t * t * (3f - 2f * t);
                    basePathY = Mathf.Lerp(0.0f, 1.5f, smoothT);
                }
                else
                {
                    // Clearing, hazard zone, puzzle courtyard, checkpoint 2, exit portal
                    basePathY = 1.5f;
                }

                for (int x = 0; x < res; x++)
                {
                    float xNorm = x * invRes;
                    float worldX = TERRAIN_POS.x + xNorm * TERRAIN_WIDTH;

                    float distFromCenter = Mathf.Abs(worldX);

                    // Central walkable path corridor (width ~7m, half-width 3.5m)
                    float pathRadius = 3.5f;
                    float pathBlend = 1.0f - Mathf.Clamp01((distFromCenter - pathRadius) / 3.5f);

                    // Micro-undulations on the natural path (±0.03m for organic ground feel)
                    float pathNoise = (Mathf.PerlinNoise(worldX * 0.15f, worldZ * 0.15f) - 0.5f) * 0.06f;

                    // Side rolling hills (starts outside path corridor)
                    float hillWeight = Mathf.Clamp01((distFromCenter - pathRadius) / 14f);

                    // Multi-frequency organic jungle hills & ridges
                    float hill1 = Mathf.PerlinNoise(worldX * 0.03f + 12.3f, worldZ * 0.03f + 45.6f) * 5.0f;
                    float hill2 = Mathf.PerlinNoise(worldX * 0.08f + 78.9f, worldZ * 0.08f + 23.4f) * 2.2f;
                    float hill3 = Mathf.PerlinNoise(worldX * 0.2f, worldZ * 0.2f) * 0.5f;
                    float hillsY = (hill1 + hill2 + hill3) * hillWeight;

                    // Natural outer containment ridges (Z < -10, Z > 130, |X| > 35)
                    float boundaryX = Mathf.Clamp01((distFromCenter - 32f) / 20f);
                    float boundaryZFront = Mathf.Clamp01((-15f - worldZ) / 15f);
                    float boundaryZBack = Mathf.Clamp01((worldZ - 125f) / 25f);
                    float boundaryMax = Mathf.Max(boundaryX, Mathf.Max(boundaryZFront, boundaryZBack));
                    float boundaryRidgeY = boundaryMax * boundaryMax * 10f;

                    // Total world height
                    float finalWorldY = (basePathY + pathNoise) * pathBlend +
                                       (basePathY + hillsY + boundaryRidgeY) * (1.0f - pathBlend);

                    // Normalize to [0, 1] relative to terrain height
                    heights[z, x] = Mathf.Clamp01(finalWorldY / TERRAIN_HEIGHT);
                }
            }

            tData.SetHeights(0, 0, heights);
            EditorUtility.SetDirty(tData);
        }

        private static void SetupTerrainLayers(TerrainData tData)
        {
            List<TerrainLayer> layers = new List<TerrainLayer>();

            // 1. Grass Soil (Base ground)
            TerrainLayer lGrassSoil = AssetDatabase.LoadAssetAtPath<TerrainLayer>($"{TERRAIN_LAYERS_DIR}/Grass_Soil_TerrainLayer.terrainlayer");
            if (lGrassSoil != null) layers.Add(lGrassSoil);

            // 2. Muddy (Central player path)
            TerrainLayer lMud = AssetDatabase.LoadAssetAtPath<TerrainLayer>($"{TERRAIN_LAYERS_DIR}/Muddy_TerrainLayer.terrainlayer");
            if (lMud != null) layers.Add(lMud);

            // 3. Pebbles (Path edges & rock accents)
            TerrainLayer lPebbles = AssetDatabase.LoadAssetAtPath<TerrainLayer>($"{TERRAIN_LAYERS_DIR}/Pebbles_A_TerrainLayer.terrainlayer");
            if (lPebbles != null) layers.Add(lPebbles);

            // 4. Grass Moss (Deep jungle understory & slopes)
            TerrainLayer lMoss = AssetDatabase.LoadAssetAtPath<TerrainLayer>($"{TERRAIN_LAYERS_DIR}/Grass_Moss_TerrainLayer.terrainlayer");
            if (lMoss != null) layers.Add(lMoss);

            // 5. Rock (Steep cliff faces & boundary ridges)
            TerrainLayer lRock = AssetDatabase.LoadAssetAtPath<TerrainLayer>($"{TERRAIN_LAYERS_DIR}/Rock_TerrainLayer.terrainlayer");
            if (lRock != null) layers.Add(lRock);

            if (layers.Count > 0)
            {
                tData.terrainLayers = layers.ToArray();
            }
            EditorUtility.SetDirty(tData);
        }

        private static void PaintAlphamaps(TerrainData tData)
        {
            int numLayers = tData.terrainLayers.Length;
            if (numLayers == 0) return;

            int res = tData.alphamapResolution;
            float[,,] splatmap = new float[res, res, numLayers];

            float invRes = 1.0f / (res - 1);

            for (int z = 0; z < res; z++)
            {
                float zNorm = z * invRes;
                float worldZ = TERRAIN_POS.z + zNorm * TERRAIN_LENGTH;

                for (int x = 0; x < res; x++)
                {
                    float xNorm = x * invRes;
                    float worldX = TERRAIN_POS.x + xNorm * TERRAIN_WIDTH;

                    float dist = Mathf.Abs(worldX);
                    float slope = tData.GetSteepness(xNorm, zNorm);

                    float[] weights = new float[numLayers];

                    // Central path (dist < 3.2m): Muddy Trail + Pebble accents
                    if (dist < 3.2f)
                    {
                        float mudWeight = 0.82f;
                        float pebbleWeight = 0.18f;

                        // Slight noise variation
                        float n = Mathf.PerlinNoise(worldX * 0.3f, worldZ * 0.3f);
                        mudWeight += (n - 0.5f) * 0.15f;
                        pebbleWeight += (0.5f - n) * 0.15f;

                        if (numLayers > 1) weights[1] = Mathf.Max(0f, mudWeight);     // Mud
                        if (numLayers > 2) weights[2] = Mathf.Max(0f, pebbleWeight);  // Pebbles
                        if (numLayers > 0) weights[0] = 0.05f;                        // Grass Soil blend
                    }
                    // Path edge transition (3.2m <= dist <= 6.5m)
                    else if (dist <= 6.5f)
                    {
                        float t = (dist - 3.2f) / 3.3f;
                        if (numLayers > 0) weights[0] = Mathf.Lerp(0.1f, 0.6f, t);   // Grass Soil
                        if (numLayers > 1) weights[1] = Mathf.Lerp(0.7f, 0.05f, t);  // Mud
                        if (numLayers > 2) weights[2] = Mathf.Lerp(0.2f, 0.15f, t);  // Pebbles
                        if (numLayers > 3) weights[3] = Mathf.Lerp(0.0f, 0.2f, t);   // Moss
                    }
                    // Jungle floor & side hills (dist > 6.5m)
                    else
                    {
                        float hillNoise = Mathf.PerlinNoise(worldX * 0.1f, worldZ * 0.1f);
                        if (numLayers > 0) weights[0] = 0.40f + hillNoise * 0.2f;    // Grass Soil
                        if (numLayers > 3) weights[3] = 0.45f + (1f - hillNoise) * 0.2f; // Moss
                        if (numLayers > 2) weights[2] = 0.08f;                        // Pebbles
                    }

                    // Steep slopes get rock coverage
                    if (slope > 22f && numLayers > 4)
                    {
                        float rockFactor = Mathf.Clamp01((slope - 22f) / 18f);
                        weights[4] += rockFactor * 0.85f;
                    }

                    // Normalize weights
                    float totalWeight = 0f;
                    for (int l = 0; l < numLayers; l++) totalWeight += weights[l];
                    if (totalWeight > 0.0001f)
                    {
                        for (int l = 0; l < numLayers; l++)
                        {
                            splatmap[z, x, l] = weights[l] / totalWeight;
                        }
                    }
                    else
                    {
                        splatmap[z, x, 0] = 1.0f;
                    }
                }
            }

            tData.SetAlphamaps(0, 0, splatmap);
            EditorUtility.SetDirty(tData);
        }

        private static void SetupDetailPrototypes(TerrainData tData)
        {
            List<DetailPrototype> detailList = new List<DetailPrototype>();

            string[] prefabNames = { "Fern_A.prefab", "Fern_B.prefab", "Grass_A.prefab", "Bush_A.prefab", "Plant_A.prefab" };
            foreach (var pName in prefabNames)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{TERRAIN_PREFABS_DIR}/{pName}");
                if (prefab != null)
                {
                    DetailPrototype dp = new DetailPrototype
                    {
                        prototype = prefab,
                        usePrototypeMesh = true,
                        renderMode = DetailRenderMode.Grass,
                        minWidth = 0.8f,
                        maxWidth = 1.3f,
                        minHeight = 0.8f,
                        maxHeight = 1.3f,
                        noiseSpread = 0.15f,
                        healthyColor = Color.white,
                        dryColor = new Color(0.9f, 0.9f, 0.85f)
                    };
                    detailList.Add(dp);
                }
            }

            if (detailList.Count > 0)
            {
                tData.detailPrototypes = detailList.ToArray();
            }
            EditorUtility.SetDirty(tData);
        }

        private static void PaintDetailLayers(TerrainData tData)
        {
            int numDetails = tData.detailPrototypes.Length;
            if (numDetails == 0) return;

            int res = tData.detailResolution;
            float invRes = 1.0f / (res - 1);

            for (int d = 0; d < numDetails; d++)
            {
                int[,] layerData = new int[res, res];

                for (int z = 0; z < res; z++)
                {
                    float zNorm = z * invRes;
                    float worldZ = TERRAIN_POS.z + zNorm * TERRAIN_LENGTH;

                    for (int x = 0; x < res; x++)
                    {
                        float xNorm = x * invRes;
                        float worldX = TERRAIN_POS.x + xNorm * TERRAIN_WIDTH;

                        float dist = Mathf.Abs(worldX);

                        // Keep main path clear for smooth player movement
                        if (dist < 3.2f)
                        {
                            layerData[z, x] = 0;
                            continue;
                        }

                        // Path edges get gentle scattered greenery
                        if (dist < 7.0f)
                        {
                            float edgeNoise = Mathf.PerlinNoise(worldX * 0.4f + d * 10f, worldZ * 0.4f + d * 10f);
                            if (edgeNoise > 0.45f)
                            {
                                layerData[z, x] = Mathf.RoundToInt((edgeNoise - 0.45f) * 6f);
                            }
                        }
                        // Side hills get lush tropical vegetation
                        else if (dist < 40f && worldZ > -10f && worldZ < 130f)
                        {
                            float hillNoise = Mathf.PerlinNoise(worldX * 0.25f + d * 15f, worldZ * 0.25f + d * 15f);
                            if (hillNoise > 0.35f)
                            {
                                layerData[z, x] = Mathf.RoundToInt((hillNoise - 0.35f) * 12f);
                            }
                        }
                        else
                        {
                            layerData[z, x] = 0;
                        }
                    }
                }

                tData.SetDetailLayer(0, 0, d, layerData);
            }
            EditorUtility.SetDirty(tData);
        }

        private static void SetupSceneTerrainObject(TerrainData tData)
        {
            GameObject envRoot = GameObject.Find("[--- 01_ENVIRONMENT ---]");
            if (envRoot == null)
            {
                envRoot = new GameObject("[--- 01_ENVIRONMENT ---]");
            }

            // Look for existing terrain object
            Transform existingTerrain = envRoot.transform.Find("Level01_JungleTerrain");
            GameObject terrainObj;
            if (existingTerrain != null)
            {
                terrainObj = existingTerrain.gameObject;
            }
            else
            {
                GameObject existingAnywhere = GameObject.Find("Level01_JungleTerrain");
                if (existingAnywhere != null)
                {
                    terrainObj = existingAnywhere;
                    terrainObj.transform.SetParent(envRoot.transform, false);
                }
                else
                {
                    terrainObj = new GameObject("Level01_JungleTerrain");
                    terrainObj.transform.SetParent(envRoot.transform, false);
                }
            }

            terrainObj.transform.position = TERRAIN_POS;
            terrainObj.transform.rotation = Quaternion.identity;
            terrainObj.transform.localScale = Vector3.one;
            terrainObj.SetActive(true);

            Terrain terrain = terrainObj.GetComponent<Terrain>();
            if (terrain == null)
            {
                terrain = terrainObj.AddComponent<Terrain>();
            }

            terrain.enabled = true;
            terrain.terrainData = tData;

            // Ensure URP Terrain Material Template
            Material terrainMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/Terrain/Mat_Level01_JungleTerrain.mat");
            if (terrainMat == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Terrain/Lit") ??
                           Shader.Find("Nature/Terrain/Standard") ??
                           Shader.Find("Universal Render Pipeline/Lit");
                if (s != null)
                {
                    terrainMat = new Material(s)
                    {
                        name = "Mat_Level01_JungleTerrain"
                    };
                    AssetDatabase.CreateAsset(terrainMat, "Assets/Art/Environment/Terrain/Mat_Level01_JungleTerrain.mat");
                }
            }
            if (terrainMat != null)
            {
                terrain.materialTemplate = terrainMat;
            }

            terrain.drawHeightmap = true;
            terrain.drawTreesAndFoliage = true;
            terrain.drawInstanced = true;
            terrain.detailObjectDistance = 150f;
            terrain.detailObjectDensity = 1.0f;
            terrain.treeDistance = 600f;
            terrain.basemapDistance = 600f;
            terrain.heightmapPixelError = 2f;
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            terrain.renderingLayerMask = 1;

            TerrainCollider collider = terrainObj.GetComponent<TerrainCollider>();
            if (collider == null)
            {
                collider = terrainObj.AddComponent<TerrainCollider>();
            }
            collider.enabled = true;
            collider.terrainData = tData;

            terrain.Flush();

            EditorUtility.SetDirty(terrainObj);
            EditorUtility.SetDirty(tData);
            if (terrainMat != null) EditorUtility.SetDirty(terrainMat);
        }

        public static void RunSafetyVerification()
        {
            Debug.Log("========== [JungleTerrainManager] SAFETY & INTEGRITY VERIFICATION ==========");

            // 1. Monkey_B3 presence
            GameObject b3 = GameObject.Find("Monkey_B3 (1)") ?? GameObject.Find("Monkey_B3");
            if (b3 != null)
            {
                Debug.Log($"<color=green>✓ Monkey_B3 verified present at position: {b3.transform.position}</color>");
                Animator anim = b3.GetComponentInChildren<Animator>(true);
                if (anim != null && anim.runtimeAnimatorController != null)
                {
                    Debug.Log($"<color=green>✓ B3 Animator Controller intact: {anim.runtimeAnimatorController.name}</color>");
                }
                else
                {
                    Debug.LogWarning("! Monkey_B3 animator controller missing or not assigned.");
                }
            }
            else
            {
                Debug.LogError("✗ CRITICAL: Monkey_B3 not found in scene!");
            }

            // 2. Camera presence
            Camera cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                Debug.Log($"<color=green>✓ Gameplay Camera verified: {cam.name} at {cam.transform.position}</color>");
            }
            else
            {
                Debug.LogWarning("! Main Camera not found.");
            }

            // 3. Terrain & Collider presence
            Terrain t = UnityEngine.Object.FindAnyObjectByType<Terrain>();
            if (t != null && t.terrainData != null)
            {
                Debug.Log($"<color=green>✓ 3D Jungle Terrain verified: size={t.terrainData.size}, layers={t.terrainData.terrainLayers.Length}</color>");
            }
            else
            {
                Debug.LogError("✗ CRITICAL: Unity Terrain not found in scene!");
            }

            // 4. Checkpoints & Gameplay anchors
            GameObject cp1 = GameObject.Find("Checkpoint_01_Start");
            GameObject cp2 = GameObject.Find("Checkpoint_02_PostDoor");
            GameObject door = GameObject.Find("Ancient_Stone_Door");
            GameObject exit = GameObject.Find("Level_01_Complete_Gateway");

            Debug.Log($"Checkpoints: Start={(cp1 != null ? "OK" : "Missing")}, PostDoor={(cp2 != null ? "OK" : "Missing")}");
            Debug.Log($"Puzzle Door: {(door != null ? "OK" : "Missing")}, Exit Gateway: {(exit != null ? "OK" : "Missing")}");
            Debug.Log("==========================================================================");
        }
    }
}
