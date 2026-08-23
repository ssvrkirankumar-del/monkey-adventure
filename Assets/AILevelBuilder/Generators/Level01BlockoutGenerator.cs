using System;
using System.Collections.Generic;
using UnityEngine;
using MonkeyAdventure.AILevelBuilder;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonkeyAdventure.AILevelBuilder
{
    /// <summary>
    /// Automatic procedural blockout generator for Level 01: 'The Awakening'.
    /// Assembles a complete, playable 9-section prototype level using clean Unity primitives
    /// organized strictly under the 'AI_GENERATED_LEVEL' hierarchy.
    /// </summary>
    public static class Level01BlockoutGenerator
    {
        [Serializable]
        public class BlockoutSettings
        {
            [Tooltip("Random seed for deterministic procedural generation.")]
            public int seed = 1337;

            [Tooltip("Total length of the level in meters.")]
            [Range(150f, 300f)]
            public float levelLength = 200f;

            [Tooltip("Standard width of the main walkable path in meters.")]
            [Range(5f, 15f)]
            public float pathWidth = 8f;

            [Tooltip("Number of decorative perimeter trees.")]
            [Range(10, 80)]
            public int treeDensity = 36;

            [Tooltip("Number of collectible fruits placed along the route.")]
            [Range(3, 20)]
            public int collectibleCount = 8;

            [Tooltip("Number of jumpable obstacles along the path.")]
            [Range(1, 10)]
            public int obstacleCount = 4;

            [Tooltip("Number of enemy encounter placeholders.")]
            [Range(1, 6)]
            public int enemyCount = 2;

            [Tooltip("Z-coordinate where the river crossing section is located.")]
            public float riverZ = 130f;
        }

        // Cached Materials
        private static Material s_MatGround;
        private static Material s_MatTreeTrunk;
        private static Material s_MatTreeFoliage;
        private static Material s_MatRock;
        private static Material s_MatFruit;
        private static Material s_MatObstacle;
        private static Material s_MatEnemy;
        private static Material s_MatWater;
        private static Material s_MatRuin;
        private static Material s_MatCheckpoint;
        private static Material s_MatFinish;
        private static Material s_MatStart;

        /// <summary>
        /// Generates the complete Level 1 Blockout in the active scene.
        /// </summary>
        public static GameObject GenerateLevel01Blockout(BlockoutSettings settings = null)
        {
            if (settings == null) settings = new BlockoutSettings();

            // 1. Non-destructively clear previous generated hierarchy
            LevelGenerator.ClearGeneratedLevel();

            // 2. Initialize deterministic RNG
            UnityEngine.Random.InitState(settings.seed);

            // 3. Initialize materials
            InitMaterials();

            // 4. Create Master Root
            GameObject root = new GameObject(LevelGenerator.ROOT_NAME);
#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(root, "Generate Level 01 Blockout");
#endif

            // 5. Create Organized Folders
            Transform envRoot = CreateFolder(root.transform, LevelGenerator.ENV_FOLDER);
            Transform groundFolder = CreateFolder(envRoot, "Ground");
            Transform treesFolder = CreateFolder(envRoot, "Trees");
            Transform rocksFolder = CreateFolder(envRoot, "Rocks");
            Transform ancientFolder = CreateFolder(envRoot, "AncientArea");
            Transform waterFolder = CreateFolder(envRoot, "Water");
            Transform crossingFolder = CreateFolder(envRoot, "Crossing");

            Transform collectiblesFolder = CreateFolder(root.transform, LevelGenerator.COLLECTIBLES_FOLDER);
            Transform obstaclesFolder = CreateFolder(root.transform, LevelGenerator.OBSTACLES_FOLDER);
            Transform enemiesFolder = CreateFolder(root.transform, LevelGenerator.ENEMIES_FOLDER);
            Transform checkpointsFolder = CreateFolder(root.transform, LevelGenerator.CHECKPOINTS_FOLDER);
            Transform startFolder = CreateFolder(root.transform, LevelGenerator.START_FOLDER);
            Transform finishFolder = CreateFolder(root.transform, LevelGenerator.FINISH_FOLDER);

            // 6. Build Sections
            BuildSection1_Start(startFolder, groundFolder, settings);
            BuildSection2_JunglePath(groundFolder, treesFolder, rocksFolder, settings);
            BuildSection3_FruitArea(groundFolder, collectiblesFolder, treesFolder, settings);
            BuildSection4_BasicObstacle(groundFolder, obstaclesFolder, rocksFolder, settings);
            BuildSection5_EnemyEncounter(groundFolder, enemiesFolder, rocksFolder, settings);
            BuildSection6_RiverCrossing(groundFolder, waterFolder, crossingFolder, settings);
            BuildSection7_AncientForest(groundFolder, ancientFolder, treesFolder, collectiblesFolder, settings);
            BuildSection8_Checkpoint(groundFolder, checkpointsFolder, settings);
            BuildSection9_Finish(groundFolder, finishFolder, settings);

            // 7. Scatter Additional Boundary Trees & Rocks
            ScatterBoundaryProps(treesFolder, rocksFolder, settings);

            // 8. Safely position player at Start if found
            TryPositionPlayerAtStart(new Vector3(0f, 0.5f, 0f));

            Debug.Log($"<color=#00FF88><b>[Level01BlockoutGenerator] Generated Level 01 'The Awakening' Blockout ({settings.levelLength}m length, Seed: {settings.seed}).</b></color>");
            return root;
        }

        #region Section Builders

        // SECTION 1 — START AREA (Z: -5 to 15)
        private static void BuildSection1_Start(Transform startFolder, Transform groundFolder, BlockoutSettings s)
        {
            // Start ground
            CreateBox("Ground_StartZone", groundFolder, new Vector3(0f, -0.5f, 5f), new Vector3(12f, 1f, 20f), s_MatGround);

            // PlayerStart marker
            GameObject startMarker = CreateMarkerObject("PlayerStart", startFolder, new Vector3(0f, 0.1f, 0f), LevelMarkerType.Start, "Player Spawn Point", 0, "Start Area");
            CreateDiscVisual("StartPad_Visual", startMarker.transform, Vector3.zero, new Vector3(3f, 0.08f, 3f), s_MatStart);

            // Start arch pillars
            CreateCylinder("StartPillar_Left", startFolder, new Vector3(-3.5f, 2f, 0f), new Vector3(0.8f, 2f, 0.8f), s_MatStart);
            CreateCylinder("StartPillar_Right", startFolder, new Vector3(3.5f, 2f, 0f), new Vector3(0.8f, 2f, 0.8f), s_MatStart);
            CreateBox("StartArch_Top", startFolder, new Vector3(0f, 4.2f, 0f), new Vector3(8f, 0.5f, 0.8f), s_MatStart);
        }

        // SECTION 2 — JUNGLE PATH (Z: 15 to 45)
        private static void BuildSection2_JunglePath(Transform groundFolder, Transform treesFolder, Transform rocksFolder, BlockoutSettings s)
        {
            CreateBox("Ground_JunglePath", groundFolder, new Vector3(0f, -0.5f, 30f), new Vector3(s.pathWidth, 1f, 30f), s_MatGround);

            // Edge trees
            for (float z = 18f; z <= 42f; z += 8f)
            {
                CreateTree($"Tree_Path_L_{z:F0}", treesFolder, new Vector3(-(s.pathWidth * 0.5f + 2.5f), 0f, z + UnityEngine.Random.Range(-1f, 1f)), 1f);
                CreateTree($"Tree_Path_R_{z:F0}", treesFolder, new Vector3((s.pathWidth * 0.5f + 2.5f), 0f, z + UnityEngine.Random.Range(-1f, 1f)), 1f);
            }

            // Path border rocks
            CreateRock("Rock_Border_01", rocksFolder, new Vector3(-(s.pathWidth * 0.5f - 0.5f), 0.4f, 25f), new Vector3(1.2f, 0.8f, 1.2f));
            CreateRock("Rock_Border_02", rocksFolder, new Vector3((s.pathWidth * 0.5f - 0.5f), 0.5f, 35f), new Vector3(1.4f, 1.0f, 1.3f));
        }

        // SECTION 3 — FRUIT COLLECTION AREA (Z: 45 to 75)
        private static void BuildSection3_FruitArea(Transform groundFolder, Transform collectiblesFolder, Transform treesFolder, BlockoutSettings s)
        {
            float wideWidth = s.pathWidth + 6f;
            CreateBox("Ground_FruitClearing", groundFolder, new Vector3(0f, -0.5f, 60f), new Vector3(wideWidth, 1f, 30f), s_MatGround);

            // Fruit collectibles spread naturally
            Vector3[] fruitOffsets = new Vector3[]
            {
                new Vector3(-2.5f, 1.2f, 50f),
                new Vector3(1.8f, 1.2f, 53f),
                new Vector3(-1.0f, 1.2f, 58f),
                new Vector3(2.8f, 1.2f, 62f),
                new Vector3(-2.2f, 1.2f, 67f),
                new Vector3(0.5f, 1.2f, 71f)
            };

            int countToSpawn = Mathf.Min(s.collectibleCount, fruitOffsets.Length);
            for (int i = 0; i < countToSpawn; i++)
            {
                CreateFruit($"Collectible_Fruit_{i + 1:D2}", collectiblesFolder, fruitOffsets[i], "Fruit Collection Area");
            }

            // Surrounding glade trees
            CreateTree("Tree_FruitGlade_L1", treesFolder, new Vector3(-(wideWidth * 0.5f + 2f), 0f, 52f), 1.2f);
            CreateTree("Tree_FruitGlade_R1", treesFolder, new Vector3((wideWidth * 0.5f + 2f), 0f, 55f), 1.1f);
            CreateTree("Tree_FruitGlade_L2", treesFolder, new Vector3(-(wideWidth * 0.5f + 2.5f), 0f, 68f), 1.3f);
            CreateTree("Tree_FruitGlade_R2", treesFolder, new Vector3((wideWidth * 0.5f + 2f), 0f, 70f), 1.1f);
        }

        // SECTION 4 — BASIC OBSTACLE (Z: 75 to 95)
        private static void BuildSection4_BasicObstacle(Transform groundFolder, Transform obstaclesFolder, Transform rocksFolder, BlockoutSettings s)
        {
            CreateBox("Ground_ObstacleSection", groundFolder, new Vector3(0f, -0.5f, 85f), new Vector3(s.pathWidth, 1f, 20f), s_MatGround);

            // Jumpable fallen log hurdle at Z = 85 (Height 0.55m — easily cleared by 2.2m jump)
            GameObject logObstacle = CreateBox("Obstacle_FallenLog_01", obstaclesFolder, new Vector3(0f, 0.35f, 85f), new Vector3(s.pathWidth - 1.2f, 0.7f, 1.0f), s_MatObstacle);
            LevelMarker m = logObstacle.AddComponent<LevelMarker>();
            m.MarkerType = LevelMarkerType.ObstacleSpawn;
            m.MarkerLabel = "Jumpable Log Hurdle";
            m.SectionName = "Basic Obstacle";

            // Warning side boulders
            CreateRock("Obstacle_SideRock_L", rocksFolder, new Vector3(-(s.pathWidth * 0.5f - 0.2f), 0.6f, 84.5f), new Vector3(1.5f, 1.2f, 1.5f));
            CreateRock("Obstacle_SideRock_R", rocksFolder, new Vector3((s.pathWidth * 0.5f - 0.2f), 0.6f, 85.5f), new Vector3(1.5f, 1.2f, 1.5f));
        }

        // SECTION 5 — FIRST ENEMY ENCOUNTER (Z: 95 to 120)
        private static void BuildSection5_EnemyEncounter(Transform groundFolder, Transform enemiesFolder, Transform rocksFolder, BlockoutSettings s)
        {
            float arenaWidth = s.pathWidth + 3f;
            CreateBox("Ground_EnemyArena", groundFolder, new Vector3(0f, -0.5f, 107.5f), new Vector3(arenaWidth, 1f, 25f), s_MatGround);

            // Enemy placeholder 1 (patrolling blocker)
            CreateEnemyPlaceholder("Enemy_Placeholder_01", enemiesFolder, new Vector3(0f, 1.0f, 107f), "First Enemy Encounter");

            if (s.enemyCount > 1)
            {
                CreateEnemyPlaceholder("Enemy_Placeholder_02", enemiesFolder, new Vector3(2.2f, 1.0f, 114f), "First Enemy Encounter");
            }

            // Boundary ambush rocks
            CreateRock("Rock_Arena_L", rocksFolder, new Vector3(-(arenaWidth * 0.5f), 0.75f, 105f), new Vector3(2f, 1.5f, 2f));
            CreateRock("Rock_Arena_R", rocksFolder, new Vector3((arenaWidth * 0.5f), 0.75f, 110f), new Vector3(2.2f, 1.4f, 1.8f));
        }

        // SECTION 6 — RIVER / CROSSING (Z: 120 to 145)
        private static void BuildSection6_RiverCrossing(Transform groundFolder, Transform waterFolder, Transform crossingFolder, BlockoutSettings s)
        {
            float riverZ = s.riverZ; // e.g. 130
            float riverWidth = 14f;

            // Approach bank (Z: 120 to riverZ - 7)
            float approachLen = (riverZ - 7f) - 120f;
            CreateBox("Ground_RiverApproach", groundFolder, new Vector3(0f, -0.5f, 120f + approachLen * 0.5f), new Vector3(s.pathWidth, 1f, approachLen), s_MatGround);

            // Water channel (Recessed at Y = -0.4f)
            CreateBox("Water_RiverChannel", waterFolder, new Vector3(0f, -0.4f, riverZ), new Vector3(28f, 0.4f, riverWidth), s_MatWater);

            // Stepping stones crossing the river safely
            float startZ = riverZ - 5f;
            float stepDist = 3.3f;
            for (int i = 0; i < 4; i++)
            {
                float stoneZ = startZ + i * stepDist;
                float stoneX = Mathf.Sin(i * 1.5f) * 1.2f;
                CreateCylinder($"SteppingStone_{i + 1}", crossingFolder, new Vector3(stoneX, 0.12f, stoneZ), new Vector3(1.8f, 0.25f, 1.8f), s_MatRock);
            }

            // Departure bank (Z: riverZ + 7 to 145)
            float exitLen = 145f - (riverZ + 7f);
            CreateBox("Ground_RiverExitBank", groundFolder, new Vector3(0f, -0.5f, (riverZ + 7f) + exitLen * 0.5f), new Vector3(s.pathWidth, 1f, exitLen), s_MatGround);
        }

        // SECTION 7 — ANCIENT FOREST AREA (Z: 145 to 175)
        private static void BuildSection7_AncientForest(Transform groundFolder, Transform ancientFolder, Transform treesFolder, Transform collectiblesFolder, BlockoutSettings s)
        {
            float clearingWidth = s.pathWidth + 8f;
            CreateBox("Ground_AncientClearing", groundFolder, new Vector3(0f, -0.5f, 160f), new Vector3(clearingWidth, 1f, 30f), s_MatGround);

            // Ancient Ruin Pillars framing the sanctuary
            CreateBox("Ruin_Pillar_L1", ancientFolder, new Vector3(-4.5f, 2.5f, 153f), new Vector3(1.2f, 5f, 1.2f), s_MatRuin);
            CreateBox("Ruin_Pillar_R1", ancientFolder, new Vector3(4.5f, 2.5f, 153f), new Vector3(1.2f, 5f, 1.2f), s_MatRuin);
            CreateBox("Ruin_Arch_Top1", ancientFolder, new Vector3(0f, 5.2f, 153f), new Vector3(10.2f, 0.8f, 1.4f), s_MatRuin);

            CreateBox("Ruin_Pillar_L2", ancientFolder, new Vector3(-4.5f, 2.0f, 165f), new Vector3(1.2f, 4f, 1.2f), s_MatRuin);
            CreateBox("Ruin_Pillar_R2", ancientFolder, new Vector3(4.5f, 2.0f, 165f), new Vector3(1.2f, 4f, 1.2f), s_MatRuin);

            // Giant Ancient Canopy Trees
            CreateTree("Tree_Ancient_L1", treesFolder, new Vector3(-(clearingWidth * 0.5f + 3f), 0f, 150f), 1.8f);
            CreateTree("Tree_Ancient_R1", treesFolder, new Vector3((clearingWidth * 0.5f + 3f), 0f, 152f), 1.7f);
            CreateTree("Tree_Ancient_L2", treesFolder, new Vector3(-(clearingWidth * 0.5f + 3.5f), 0f, 168f), 1.9f);
            CreateTree("Tree_Ancient_R2", treesFolder, new Vector3((clearingWidth * 0.5f + 3f), 0f, 170f), 1.8f);

            // Ancient Golden Relic Fruit
            CreateFruit("Collectible_GoldenFruit_Ancient", collectiblesFolder, new Vector3(0f, 1.6f, 160f), "Ancient Forest Area");
        }

        // SECTION 8 — CHECKPOINT (Z: 175 to 185)
        private static void BuildSection8_Checkpoint(Transform groundFolder, Transform checkpointsFolder, BlockoutSettings s)
        {
            CreateBox("Ground_CheckpointZone", groundFolder, new Vector3(0f, -0.5f, 180f), new Vector3(s.pathWidth + 2f, 1f, 10f), s_MatGround);

            // Checkpoint Pad
            GameObject cpMarker = CreateMarkerObject("Checkpoint_01", checkpointsFolder, new Vector3(0f, 0.15f, 180f), LevelMarkerType.Checkpoint, "Checkpoint 01", 1, "Checkpoint");
            CreateDiscVisual("CheckpointPad_Visual", cpMarker.transform, Vector3.zero, new Vector3(4f, 0.1f, 4f), s_MatCheckpoint);
            CreateCylinder("CheckpointTotem_L", cpMarker.transform, new Vector3(-2.2f, 1.2f, 0f), new Vector3(0.5f, 2.4f, 0.5f), s_MatCheckpoint);
            CreateCylinder("CheckpointTotem_R", cpMarker.transform, new Vector3(2.2f, 1.2f, 0f), new Vector3(0.5f, 2.4f, 0.5f), s_MatCheckpoint);
        }

        // SECTION 9 — FINISH (Z: 185 to 205)
        private static void BuildSection9_Finish(Transform groundFolder, Transform finishFolder, BlockoutSettings s)
        {
            float finishZ = s.levelLength; // e.g. 200
            CreateBox("Ground_FinishPlaza", groundFolder, new Vector3(0f, -0.5f, finishZ - 5f), new Vector3(14f, 1f, 20f), s_MatGround);

            // Finish Portal Gateway
            GameObject finishObj = CreateMarkerObject("Finish_Gateway", finishFolder, new Vector3(0f, 0.1f, finishZ), LevelMarkerType.Finish, "Level 01 Finish Portal", 0, "Finish");

            CreateDiscVisual("FinishDais_Visual", finishObj.transform, Vector3.zero, new Vector3(6f, 0.15f, 6f), s_MatFinish);

            // Golden Gateway Pillars & Arch
            CreateBox("FinishPillar_L", finishObj.transform, new Vector3(-3f, 2.5f, 0f), new Vector3(1f, 5f, 1f), s_MatFinish);
            CreateBox("FinishPillar_R", finishObj.transform, new Vector3(3f, 2.5f, 0f), new Vector3(1f, 5f, 1f), s_MatFinish);
            CreateBox("FinishArch_Top", finishObj.transform, new Vector3(0f, 5.2f, 0f), new Vector3(7.5f, 0.8f, 1.2f), s_MatFinish);
            CreateCylinder("FinishPortal_EnergyCore", finishObj.transform, new Vector3(0f, 2.5f, 0f), new Vector3(2.5f, 0.05f, 2.5f), s_MatFinish);
        }

        #endregion

        #region Helper Scatter & Props

        private static void ScatterBoundaryProps(Transform treesFolder, Transform rocksFolder, BlockoutSettings s)
        {
            int extraTrees = Mathf.Max(0, s.treeDensity - 16);
            for (int i = 0; i < extraTrees; i++)
            {
                float z = UnityEngine.Random.Range(5f, s.levelLength - 10f);

                // Avoid water area
                if (z > s.riverZ - 10f && z < s.riverZ + 10f) continue;

                float side = (UnityEngine.Random.value > 0.5f) ? 1f : -1f;
                float x = side * (s.pathWidth * 0.5f + UnityEngine.Random.Range(3.5f, 12f));

                CreateTree($"Tree_Perimeter_{i + 1:D2}", treesFolder, new Vector3(x, 0f, z), UnityEngine.Random.Range(0.85f, 1.4f));
            }
        }

        private static void TryPositionPlayerAtStart(Vector3 spawnPos)
        {
            GameObject player = GameObject.Find("Monkey_B3 (1)") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                player.transform.position = spawnPos + Vector3.up * 0.5f;
                player.transform.rotation = Quaternion.identity;

                if (cc != null) cc.enabled = true;
                Debug.Log($"[Level01BlockoutGenerator] Positioned active player '{player.name}' at Start spawn: {player.transform.position}");
            }
            else
            {
                Debug.Log("[Level01BlockoutGenerator] Player object not in scene; PlayerStart marker created for manual alignment.");
            }
        }

        #endregion

        #region Primitive Spawners & Material Cache

        private static void InitMaterials()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            s_MatGround = CreateTempMaterial(s, new Color(0.24f, 0.42f, 0.22f), "Mat_Blockout_Ground");
            s_MatTreeTrunk = CreateTempMaterial(s, new Color(0.42f, 0.26f, 0.14f), "Mat_Blockout_Trunk");
            s_MatTreeFoliage = CreateTempMaterial(s, new Color(0.18f, 0.58f, 0.22f), "Mat_Blockout_Foliage");
            s_MatRock = CreateTempMaterial(s, new Color(0.52f, 0.54f, 0.56f), "Mat_Blockout_Rock");
            s_MatFruit = CreateTempMaterial(s, new Color(1.0f, 0.85f, 0.1f), "Mat_Blockout_Fruit");
            s_MatObstacle = CreateTempMaterial(s, new Color(0.82f, 0.42f, 0.12f), "Mat_Blockout_Obstacle");
            s_MatEnemy = CreateTempMaterial(s, new Color(0.88f, 0.15f, 0.15f), "Mat_Blockout_Enemy");
            s_MatWater = CreateTempMaterial(s, new Color(0.15f, 0.6f, 0.9f, 0.85f), "Mat_Blockout_Water");
            s_MatRuin = CreateTempMaterial(s, new Color(0.62f, 0.60f, 0.58f), "Mat_Blockout_Ruin");
            s_MatCheckpoint = CreateTempMaterial(s, new Color(0.1f, 0.9f, 0.95f), "Mat_Blockout_Checkpoint");
            s_MatFinish = CreateTempMaterial(s, new Color(1.0f, 0.82f, 0.0f), "Mat_Blockout_Finish");
            s_MatStart = CreateTempMaterial(s, new Color(0.25f, 0.92f, 0.35f), "Mat_Blockout_Start");
        }

        private static Material CreateTempMaterial(Shader shader, Color color, string name)
        {
            Material mat = new Material(shader)
            {
                name = name,
                color = color
            };
            return mat;
        }

        private static Transform CreateFolder(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject CreateBox(string name, Transform parent, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = size;
            go.transform.SetParent(parent, true);

            if (mat != null)
            {
                var rend = go.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = mat;
            }
            return go;
        }

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.transform.SetParent(parent, true);

            if (mat != null)
            {
                var rend = go.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = mat;
            }
            return go;
        }

        private static GameObject CreateDiscVisual(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            // Remove collider on purely visual sub-element
            Collider c = go.GetComponent<Collider>();
            if (c != null) UnityEngine.Object.DestroyImmediate(c);

            if (mat != null)
            {
                var rend = go.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial = mat;
            }
            return go;
        }

        private static GameObject CreateTree(string name, Transform parent, Vector3 rootPos, float scaleMult = 1f)
        {
            GameObject treeRoot = new GameObject(name);
            treeRoot.transform.position = rootPos;
            treeRoot.transform.SetParent(parent, true);

            // Trunk (Cylinder)
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(treeRoot.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 2f * scaleMult, 0f);
            trunk.transform.localScale = new Vector3(0.6f * scaleMult, 2f * scaleMult, 0.6f * scaleMult);
            if (s_MatTreeTrunk != null) trunk.GetComponent<Renderer>().sharedMaterial = s_MatTreeTrunk;

            // Canopy (Sphere)
            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(treeRoot.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 4.5f * scaleMult, 0f);
            canopy.transform.localScale = new Vector3(3.2f * scaleMult, 2.5f * scaleMult, 3.2f * scaleMult);
            if (s_MatTreeFoliage != null) canopy.GetComponent<Renderer>().sharedMaterial = s_MatTreeFoliage;

            LevelMarker m = treeRoot.AddComponent<LevelMarker>();
            m.MarkerType = LevelMarkerType.EnvironmentObject;
            m.MarkerLabel = "Jungle Tree";

            return treeRoot;
        }

        private static GameObject CreateRock(string name, Transform parent, Vector3 pos, Vector3 size)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = name;
            rock.transform.position = pos;
            rock.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(5f, 25f), UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(5f, 25f));
            rock.transform.localScale = size;
            rock.transform.SetParent(parent, true);

            if (s_MatRock != null) rock.GetComponent<Renderer>().sharedMaterial = s_MatRock;

            LevelMarker m = rock.AddComponent<LevelMarker>();
            m.MarkerType = LevelMarkerType.EnvironmentObject;
            m.MarkerLabel = "Jungle Boulder";

            return rock;
        }

        private static GameObject CreateFruit(string name, Transform parent, Vector3 pos, string section)
        {
            GameObject fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fruit.name = name;
            fruit.transform.position = pos;
            fruit.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            fruit.transform.SetParent(parent, true);

            SphereCollider col = fruit.GetComponent<SphereCollider>();
            if (col != null) col.isTrigger = true;

            if (s_MatFruit != null) fruit.GetComponent<Renderer>().sharedMaterial = s_MatFruit;

            LevelMarker m = fruit.AddComponent<LevelMarker>();
            m.MarkerType = LevelMarkerType.CollectibleSpawn;
            m.MarkerLabel = "Collectible Fruit";
            m.SectionName = section;

            return fruit;
        }

        private static GameObject CreateEnemyPlaceholder(string name, Transform parent, Vector3 pos, string section)
        {
            GameObject enemy = new GameObject(name);
            enemy.transform.position = pos;
            enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            enemy.transform.SetParent(parent, true);

            // Body Capsule
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(enemy.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            if (s_MatEnemy != null) body.GetComponent<Renderer>().sharedMaterial = s_MatEnemy;

            // Forward Visor / Eye
            GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor_Forward";
            visor.transform.SetParent(enemy.transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.4f, 0.45f);
            visor.transform.localScale = new Vector3(0.5f, 0.2f, 0.2f);
            Collider vc = visor.GetComponent<Collider>();
            if (vc != null) UnityEngine.Object.DestroyImmediate(vc);
            if (s_MatFruit != null) visor.GetComponent<Renderer>().sharedMaterial = s_MatFruit;

            LevelMarker m = enemy.AddComponent<LevelMarker>();
            m.MarkerType = LevelMarkerType.EnemySpawn;
            m.MarkerLabel = "Jungle Creature Enemy";
            m.SectionName = section;

            return enemy;
        }

        private static GameObject CreateMarkerObject(string name, Transform parent, Vector3 pos, LevelMarkerType type, string label, int index = 0, string section = "")
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.identity;
            go.transform.SetParent(parent, true);

            LevelMarker m = go.AddComponent<LevelMarker>();
            m.MarkerType = type;
            m.MarkerLabel = label;
            m.MarkerIndex = index;
            m.SectionName = section;

            return go;
        }

        #endregion
    }
}
