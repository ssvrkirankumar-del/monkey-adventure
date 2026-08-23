using System.IO;
using UnityEditor;
using UnityEngine;
using MonkeyAdventure.AI;
using MonkeyAdventure.Bosses;
using MonkeyAdventure.Collectibles;
using MonkeyAdventure.Combat;
using MonkeyAdventure.Core;
using MonkeyAdventure.Environment;
using MonkeyAdventure.Hazards;
using MonkeyAdventure.Mechanics;
using MonkeyAdventure.Player;
using MonkeyAdventure.Progression;
using MonkeyAdventure.Puzzles;
using MonkeyAdventure.Skins;
using MonkeyAdventure.Animation;
using GuardianSystem.Combat;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Master 3D Stylized Mesh & Prefab Synthesis Factory.
    /// Procedurally constructs low-poly, mobile-optimized 3D meshes, materials, and prefabs
    /// for Characters, Evolution Skins, Bosses, Normal Enemies, Wildlife, Environment, Ruins, and Props.
    /// Saves all assets into organized folders under Assets/Art/.
    /// </summary>
    public static class AssetMeshFactory
    {
        private const string ART_ROOT = "Assets/Art";
        private const string CHAR_DIR = "Assets/Art/Characters";
        private const string ENEMY_DIR = "Assets/Art/Enemies";
        private const string BOSS_DIR = "Assets/Art/Bosses";
        private const string WILD_DIR = "Assets/Art/Wildlife";
        private const string ENV_TREE_DIR = "Assets/Art/Environment/Trees";
        private const string ENV_PLANT_DIR = "Assets/Art/Environment/Plants";
        private const string ENV_ROCK_DIR = "Assets/Art/Environment/Rocks";
        private const string ENV_RUINS_DIR = "Assets/Art/Environment/Ruins";
        private const string PROP_DIR = "Assets/Art/Props";
        private const string MAT_DIR = "Assets/Art/Materials";

        public static void GenerateAll3DAssetsAndPrefabs()
        {
            EnsureDirectories();

            // 1. Generate Core URP Materials
            CreateArtMaterials();

            // 2. Characters & Evolution Skins
            CreateBaseMonkeyPrefab();
            CreateGuardianMonkeyPrefab();
            CreatePrimalTitanPrefab();
            CreateDivineGuardianPrefab();

            // 3. Bosses
            CreateAlphaJaguarBossPrefab();
            CreateStoneGolemBossPrefab();
            CreateRiverSerpentBossPrefab();
            CreateShadowBeastBossPrefab();
            CreateFinalCorruptorBossPrefab();

            // 4. Normal Enemies
            CreateJunglePredatorPrefab();
            CreateWildBoarPrefab();
            CreateToxicReptilePrefab();

            // 5. Wildlife
            CreateDeerPrefab();
            CreateParrotPrefab();
            CreateTreeFrogPrefab();
            CreateButterflyPrefab();
            CreateSmallMonkeyPrefab();

            // 6. Environment (Trees, Plants, Rocks, Ruins, Props)
            CreateJungleTrees();
            CreateJunglePlants();
            CreateJungleRocks();
            CreateJungleRuinsAndPuzzles();
            CreateJungleProps();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AssetMeshFactory] All Real 3D Models, Materials, and Prefabs generated in Assets/Art/!");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = {
                ART_ROOT, CHAR_DIR, ENEMY_DIR, BOSS_DIR, WILD_DIR,
                ENV_TREE_DIR, ENV_PLANT_DIR, ENV_ROCK_DIR, ENV_RUINS_DIR,
                PROP_DIR, MAT_DIR, "Assets/Documentation"
            };

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            AssetDatabase.Refresh();
        }

        #region Material Generation
        private static void CreateArtMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("URP/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            CreateMat("Mat_MonkeyFur", litShader, new Color(0.48f, 0.28f, 0.12f));
            CreateMat("Mat_MonkeySkin", litShader, new Color(0.92f, 0.74f, 0.58f));
            CreateMat("Mat_GuardianGold", litShader, new Color(1.0f, 0.82f, 0.1f), true, new Color(0.5f, 0.4f, 0.05f));
            CreateMat("Mat_TitanFur", litShader, new Color(0.18f, 0.18f, 0.22f));
            CreateMat("Mat_DivineCyan", litShader, new Color(0.2f, 0.9f, 1.0f), true, new Color(0.1f, 0.6f, 0.8f));

            CreateMat("Mat_JaguarFur", litShader, new Color(0.95f, 0.65f, 0.15f));
            CreateMat("Mat_GolemBasalt", litShader, new Color(0.32f, 0.34f, 0.36f));
            CreateMat("Mat_SerpentScale", litShader, new Color(0.1f, 0.55f, 0.45f));
            CreateMat("Mat_ShadowEther", litShader, new Color(0.25f, 0.1f, 0.35f), true, new Color(0.3f, 0.1f, 0.45f));
            CreateMat("Mat_CorruptVoid", litShader, new Color(0.12f, 0.05f, 0.18f), true, new Color(0.6f, 0.1f, 0.2f));

            CreateMat("Mat_PredatorSkin", litShader, new Color(0.85f, 0.25f, 0.15f));
            CreateMat("Mat_BoarHide", litShader, new Color(0.38f, 0.25f, 0.15f));
            CreateMat("Mat_ReptileScale", litShader, new Color(0.2f, 0.7f, 0.25f));

            CreateMat("Mat_DeerFur", litShader, new Color(0.65f, 0.42f, 0.25f));
            CreateMat("Mat_ParrotFeather", litShader, new Color(0.95f, 0.2f, 0.15f));
            CreateMat("Mat_FrogSkin", litShader, new Color(0.15f, 0.85f, 0.2f));
            CreateMat("Mat_ButterflyWing", litShader, new Color(0.2f, 0.8f, 0.95f), true, new Color(0.1f, 0.4f, 0.5f));

            CreateMat("Mat_JungleWood", litShader, new Color(0.35f, 0.22f, 0.12f));
            CreateMat("Mat_JungleLeaves", litShader, new Color(0.18f, 0.58f, 0.15f));
            CreateMat("Mat_PalmLeaves", litShader, new Color(0.25f, 0.68f, 0.2f));
            CreateMat("Mat_MossyRock", litShader, new Color(0.4f, 0.45f, 0.35f));
            CreateMat("Mat_AncientStone", litShader, new Color(0.5f, 0.52f, 0.48f));
            CreateMat("Mat_RuneActiveGlow", litShader, new Color(0.1f, 0.95f, 0.85f), true, new Color(0.2f, 1.0f, 0.9f));
        }

        private static Material CreateMat(string name, Shader shader, Color color, bool emissive = false, Color emitColor = default)
        {
            string path = $"{MAT_DIR}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.name = name;
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                if (mat.shader != shader && shader != null)
                {
                    mat.shader = shader;
                }
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);

            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emitColor);
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetMat(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{MAT_DIR}/{name}.mat");
        }
        #endregion

        #region Character Models (Monkey & 4 Evolution Skins)
        private static void CreateBaseMonkeyPrefab()
        {
            GameObject root = new GameObject("Monkey_Base");
            Material furMat = GetMat("Mat_MonkeyFur");
            Material skinMat = GetMat("Mat_MonkeySkin");

            // Head
            GameObject head = CreateSubMesh(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 1.25f, 0), new Vector3(0.5f, 0.45f, 0.45f), furMat);
            // Snout
            CreateSubMesh(PrimitiveType.Sphere, "Snout", head.transform, new Vector3(0, -0.08f, 0.22f), new Vector3(0.25f, 0.2f, 0.25f), skinMat);
            // Ears
            CreateSubMesh(PrimitiveType.Sphere, "Ear_L", head.transform, new Vector3(-0.26f, 0.05f, 0), new Vector3(0.18f, 0.18f, 0.08f), skinMat);
            CreateSubMesh(PrimitiveType.Sphere, "Ear_R", head.transform, new Vector3(0.26f, 0.05f, 0), new Vector3(0.18f, 0.18f, 0.08f), skinMat);

            // Torso
            GameObject body = CreateSubMesh(PrimitiveType.Capsule, "Torso", root.transform, new Vector3(0, 0.75f, 0), new Vector3(0.45f, 0.45f, 0.4f), furMat);
            // Chest patch
            CreateSubMesh(PrimitiveType.Sphere, "Chest", body.transform, new Vector3(0, 0.05f, 0.16f), new Vector3(0.3f, 0.35f, 0.15f), skinMat);

            // Limbs
            CreateSubMesh(PrimitiveType.Capsule, "Arm_L", root.transform, new Vector3(-0.35f, 0.75f, 0.05f), new Vector3(0.14f, 0.35f, 0.14f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Arm_R", root.transform, new Vector3(0.35f, 0.75f, 0.05f), new Vector3(0.14f, 0.35f, 0.14f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_L", root.transform, new Vector3(-0.18f, 0.25f, 0), new Vector3(0.16f, 0.28f, 0.16f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_R", root.transform, new Vector3(0.18f, 0.25f, 0), new Vector3(0.16f, 0.28f, 0.16f), furMat);

            // Tail
            GameObject tail = CreateSubMesh(PrimitiveType.Cylinder, "Tail", root.transform, new Vector3(0, 0.45f, -0.3f), new Vector3(0.08f, 0.4f, 0.08f), furMat);
            tail.transform.localRotation = Quaternion.Euler(-55f, 0, 0);

            // Animator
            Animator anim = root.AddComponent<Animator>();

            SaveAndDestroyPrefab(root, $"{CHAR_DIR}/Monkey_Base.prefab");
        }

        private static void CreateGuardianMonkeyPrefab()
        {
            GameObject root = new GameObject("Monkey_Guardian");
            Material furMat = GetMat("Mat_MonkeyFur");
            Material goldMat = GetMat("Mat_GuardianGold");
            Material skinMat = GetMat("Mat_MonkeySkin");

            // Head with gold headband
            GameObject head = CreateSubMesh(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 1.3f, 0), new Vector3(0.52f, 0.48f, 0.48f), furMat);
            CreateSubMesh(PrimitiveType.Cylinder, "Headband", head.transform, new Vector3(0, 0.12f, 0), new Vector3(0.55f, 0.06f, 0.55f), goldMat);
            CreateSubMesh(PrimitiveType.Sphere, "Snout", head.transform, new Vector3(0, -0.08f, 0.24f), new Vector3(0.25f, 0.2f, 0.25f), skinMat);

            // Torso with golden shoulder armor
            GameObject body = CreateSubMesh(PrimitiveType.Capsule, "Torso", root.transform, new Vector3(0, 0.8f, 0), new Vector3(0.5f, 0.5f, 0.45f), furMat);
            CreateSubMesh(PrimitiveType.Sphere, "ChestPlate", body.transform, new Vector3(0, 0.08f, 0.2f), new Vector3(0.35f, 0.38f, 0.15f), goldMat);
            CreateSubMesh(PrimitiveType.Sphere, "Shoulder_L", root.transform, new Vector3(-0.4f, 0.95f, 0), new Vector3(0.24f, 0.24f, 0.24f), goldMat);
            CreateSubMesh(PrimitiveType.Sphere, "Shoulder_R", root.transform, new Vector3(0.4f, 0.95f, 0), new Vector3(0.24f, 0.24f, 0.24f), goldMat);

            // Limbs with bracers
            CreateSubMesh(PrimitiveType.Capsule, "Arm_L", root.transform, new Vector3(-0.38f, 0.75f, 0.05f), new Vector3(0.16f, 0.38f, 0.16f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Arm_R", root.transform, new Vector3(0.38f, 0.75f, 0.05f), new Vector3(0.16f, 0.38f, 0.16f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_L", root.transform, new Vector3(-0.2f, 0.28f, 0), new Vector3(0.18f, 0.3f, 0.18f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_R", root.transform, new Vector3(0.2f, 0.28f, 0), new Vector3(0.18f, 0.3f, 0.18f), furMat);

            // Gold Tail Ring
            GameObject tail = CreateSubMesh(PrimitiveType.Cylinder, "Tail", root.transform, new Vector3(0, 0.5f, -0.32f), new Vector3(0.09f, 0.42f, 0.09f), furMat);
            tail.transform.localRotation = Quaternion.Euler(-55f, 0, 0);
            CreateSubMesh(PrimitiveType.Cylinder, "TailRing", tail.transform, new Vector3(0, 0.15f, 0), new Vector3(0.12f, 0.05f, 0.12f), goldMat);

            root.AddComponent<Animator>();
            SaveAndDestroyPrefab(root, $"{CHAR_DIR}/Monkey_Guardian.prefab");
        }

        private static void CreatePrimalTitanPrefab()
        {
            GameObject root = new GameObject("Monkey_PrimalTitan");
            Material titanFur = GetMat("Mat_TitanFur");
            Material skinMat = GetMat("Mat_MonkeySkin");

            // Heavy Head & Brow
            GameObject head = CreateSubMesh(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 1.45f, 0.15f), new Vector3(0.65f, 0.6f, 0.6f), titanFur);
            CreateSubMesh(PrimitiveType.Cube, "Brow", head.transform, new Vector3(0, 0.15f, 0.28f), new Vector3(0.5f, 0.15f, 0.2f), titanFur);
            CreateSubMesh(PrimitiveType.Sphere, "Snout", head.transform, new Vector3(0, -0.1f, 0.32f), new Vector3(0.35f, 0.28f, 0.32f), skinMat);

            // Broad Muscular Torso
            GameObject body = CreateSubMesh(PrimitiveType.Capsule, "Torso", root.transform, new Vector3(0, 0.95f, 0), new Vector3(0.8f, 0.65f, 0.65f), titanFur);
            CreateSubMesh(PrimitiveType.Sphere, "Pecs", body.transform, new Vector3(0, 0.1f, 0.28f), new Vector3(0.55f, 0.45f, 0.2f), skinMat);

            // Heavy Arms & Fists
            CreateSubMesh(PrimitiveType.Capsule, "Arm_L", root.transform, new Vector3(-0.6f, 0.85f, 0.15f), new Vector3(0.28f, 0.55f, 0.28f), titanFur);
            CreateSubMesh(PrimitiveType.Capsule, "Arm_R", root.transform, new Vector3(0.6f, 0.85f, 0.15f), new Vector3(0.28f, 0.55f, 0.28f), titanFur);
            CreateSubMesh(PrimitiveType.Sphere, "Fist_L", root.transform, new Vector3(-0.6f, 0.25f, 0.35f), new Vector3(0.32f, 0.32f, 0.32f), skinMat);
            CreateSubMesh(PrimitiveType.Sphere, "Fist_R", root.transform, new Vector3(0.6f, 0.25f, 0.35f), new Vector3(0.32f, 0.32f, 0.32f), skinMat);

            // Legs
            CreateSubMesh(PrimitiveType.Capsule, "Leg_L", root.transform, new Vector3(-0.3f, 0.35f, -0.05f), new Vector3(0.25f, 0.35f, 0.25f), titanFur);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_R", root.transform, new Vector3(0.3f, 0.35f, -0.05f), new Vector3(0.25f, 0.35f, 0.25f), titanFur);

            root.AddComponent<Animator>();
            SaveAndDestroyPrefab(root, $"{CHAR_DIR}/Monkey_PrimalTitan.prefab");
        }

        private static void CreateDivineGuardianPrefab()
        {
            GameObject root = new GameObject("Monkey_DivineGuardian");
            Material furMat = GetMat("Mat_MonkeyFur");
            Material cyanMat = GetMat("Mat_DivineCyan");
            Material goldMat = GetMat("Mat_GuardianGold");

            // Head with Floating Halo
            GameObject head = CreateSubMesh(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 1.35f, 0), new Vector3(0.55f, 0.5f, 0.5f), furMat);
            GameObject halo = CreateSubMesh(PrimitiveType.Cylinder, "CelestialHalo", head.transform, new Vector3(0, 0.45f, -0.1f), new Vector3(0.7f, 0.04f, 0.7f), cyanMat);
            halo.transform.localRotation = Quaternion.Euler(20f, 0, 0);

            // Torso with Radiant Armor
            GameObject body = CreateSubMesh(PrimitiveType.Capsule, "Torso", root.transform, new Vector3(0, 0.85f, 0), new Vector3(0.52f, 0.52f, 0.48f), furMat);
            CreateSubMesh(PrimitiveType.Sphere, "LotusChest", body.transform, new Vector3(0, 0.1f, 0.22f), new Vector3(0.38f, 0.38f, 0.18f), cyanMat);

            // Arms with Lotus Bracers
            CreateSubMesh(PrimitiveType.Capsule, "Arm_L", root.transform, new Vector3(-0.4f, 0.8f, 0.05f), new Vector3(0.18f, 0.4f, 0.18f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Arm_R", root.transform, new Vector3(0.4f, 0.8f, 0.05f), new Vector3(0.18f, 0.4f, 0.18f), furMat);
            CreateSubMesh(PrimitiveType.Cylinder, "Bracer_L", root.transform, new Vector3(-0.4f, 0.55f, 0.05f), new Vector3(0.24f, 0.1f, 0.24f), goldMat);
            CreateSubMesh(PrimitiveType.Cylinder, "Bracer_R", root.transform, new Vector3(0.4f, 0.55f, 0.05f), new Vector3(0.24f, 0.1f, 0.24f), goldMat);

            // Legs
            CreateSubMesh(PrimitiveType.Capsule, "Leg_L", root.transform, new Vector3(-0.22f, 0.3f, 0), new Vector3(0.2f, 0.32f, 0.2f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_R", root.transform, new Vector3(0.22f, 0.3f, 0), new Vector3(0.2f, 0.32f, 0.2f), furMat);

            root.AddComponent<Animator>();
            SaveAndDestroyPrefab(root, $"{CHAR_DIR}/Monkey_DivineGuardian.prefab");
        }
        #endregion

        #region Boss Prefabs
        private static void CreateAlphaJaguarBossPrefab()
        {
            GameObject root = new GameObject("Boss_AlphaJaguar");
            Material furMat = GetMat("Mat_JaguarFur");
            Material goldMat = GetMat("Mat_GuardianGold");

            // Body
            CreateSubMesh(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0, 0.9f, 0), new Vector3(0.8f, 1.4f, 0.8f), furMat).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            // Head & Fangs
            GameObject head = CreateSubMesh(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 1.2f, 0.95f), new Vector3(0.6f, 0.55f, 0.65f), furMat);
            CreateConeSubMesh("Fang_L", head.transform, new Vector3(-0.15f, -0.2f, 0.3f), new Vector3(0.08f, 0.2f, 0.08f), goldMat);
            CreateConeSubMesh("Fang_R", head.transform, new Vector3(0.15f, -0.2f, 0.3f), new Vector3(0.08f, 0.2f, 0.08f), goldMat);

            // 4 Powerful Paws
            CreateSubMesh(PrimitiveType.Capsule, "Leg_FL", root.transform, new Vector3(-0.45f, 0.45f, 0.6f), new Vector3(0.22f, 0.45f, 0.22f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_FR", root.transform, new Vector3(0.45f, 0.45f, 0.6f), new Vector3(0.22f, 0.45f, 0.22f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_BL", root.transform, new Vector3(-0.45f, 0.45f, -0.6f), new Vector3(0.22f, 0.45f, 0.22f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_BR", root.transform, new Vector3(0.45f, 0.45f, -0.6f), new Vector3(0.22f, 0.45f, 0.22f), furMat);

            // Tail
            GameObject tail = CreateSubMesh(PrimitiveType.Cylinder, "Tail", root.transform, new Vector3(0, 0.9f, -0.9f), new Vector3(0.08f, 0.6f, 0.08f), furMat);
            tail.transform.localRotation = Quaternion.Euler(45f, 0, 0);

            // Setup Boss Components
            root.AddComponent<CapsuleCollider>().height = 2.0f;
            root.AddComponent<AlphaJaguarBoss>();
            root.tag = "Enemy";

            SaveAndDestroyPrefab(root, $"{BOSS_DIR}/Boss_AlphaJaguar.prefab");
        }

        private static void CreateStoneGolemBossPrefab()
        {
            GameObject root = new GameObject("Boss_StoneGolem");
            Material rockMat = GetMat("Mat_GolemBasalt");
            Material runeMat = GetMat("Mat_RuneActiveGlow");

            // Heavy Torso & Rune Core
            GameObject body = CreateSubMesh(PrimitiveType.Cube, "Torso", root.transform, new Vector3(0, 1.8f, 0), new Vector3(1.8f, 1.5f, 1.2f), rockMat);
            CreateSubMesh(PrimitiveType.Sphere, "RuneCore", body.transform, new Vector3(0, 0, 0.55f), new Vector3(0.6f, 0.6f, 0.3f), runeMat);

            // Head
            CreateSubMesh(PrimitiveType.Cube, "Head", root.transform, new Vector3(0, 2.8f, 0.1f), new Vector3(0.9f, 0.7f, 0.8f), rockMat);

            // Boulder Shoulders & Arms
            CreateSubMesh(PrimitiveType.Sphere, "Shoulder_L", root.transform, new Vector3(-1.3f, 2.2f, 0), new Vector3(0.9f, 0.9f, 0.9f), rockMat);
            CreateSubMesh(PrimitiveType.Sphere, "Shoulder_R", root.transform, new Vector3(1.3f, 2.2f, 0), new Vector3(0.9f, 0.9f, 0.9f), rockMat);
            CreateSubMesh(PrimitiveType.Cube, "Arm_L", root.transform, new Vector3(-1.3f, 1.3f, 0.2f), new Vector3(0.5f, 1.1f, 0.5f), rockMat);
            CreateSubMesh(PrimitiveType.Cube, "Arm_R", root.transform, new Vector3(1.3f, 1.3f, 0.2f), new Vector3(0.5f, 1.1f, 0.5f), rockMat);

            // Legs
            CreateSubMesh(PrimitiveType.Cube, "Leg_L", root.transform, new Vector3(-0.6f, 0.55f, 0), new Vector3(0.6f, 1.1f, 0.6f), rockMat);
            CreateSubMesh(PrimitiveType.Cube, "Leg_R", root.transform, new Vector3(0.6f, 0.55f, 0), new Vector3(0.6f, 1.1f, 0.6f), rockMat);

            // Setup Boss Components
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.8f, 0);
            col.size = new Vector3(2.5f, 3.6f, 2.0f);
            root.AddComponent<StoneGolemBoss>();
            root.tag = "Enemy";

            SaveAndDestroyPrefab(root, $"{BOSS_DIR}/Boss_StoneGolem.prefab");
        }

        private static void CreateRiverSerpentBossPrefab()
        {
            GameObject root = new GameObject("Boss_RiverSerpent");
            Material scaleMat = GetMat("Mat_SerpentScale");
            Material cyanMat = GetMat("Mat_DivineCyan");

            // Segmented Serpent Body
            for (int i = 0; i < 5; i++)
            {
                float z = -i * 0.9f;
                float scale = 1.0f - i * 0.12f;
                GameObject seg = CreateSubMesh(PrimitiveType.Sphere, $"Segment_{i}", root.transform, new Vector3(0, 1.0f + Mathf.Sin(i * 0.8f) * 0.4f, z), new Vector3(1.1f * scale, 1.0f * scale, 1.2f * scale), scaleMat);
                // Aquatic Fin
                CreateSubMesh(PrimitiveType.Cube, "Fin", seg.transform, new Vector3(0, 0.6f, 0), new Vector3(0.08f, 0.5f, 0.6f), cyanMat);
            }

            // Head & Crest
            GameObject head = CreateSubMesh(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 1.8f, 1.0f), new Vector3(1.3f, 1.1f, 1.4f), scaleMat);
            CreateSubMesh(PrimitiveType.Cube, "Crest", head.transform, new Vector3(0, 0.7f, 0), new Vector3(0.1f, 0.7f, 0.9f), cyanMat);

            root.AddComponent<CapsuleCollider>().height = 5.0f;
            root.AddComponent<RiverSerpentBoss>();
            root.tag = "Enemy";

            SaveAndDestroyPrefab(root, $"{BOSS_DIR}/Boss_RiverSerpent.prefab");
        }

        private static void CreateShadowBeastBossPrefab()
        {
            GameObject root = new GameObject("Boss_ShadowBeast");
            Material etherMat = GetMat("Mat_ShadowEther");
            Material voidMat = GetMat("Mat_CorruptVoid");

            // Phantom Feline Torso
            CreateSubMesh(PrimitiveType.Capsule, "Torso", root.transform, new Vector3(0, 1.1f, 0), new Vector3(0.9f, 1.5f, 0.9f), etherMat).transform.localRotation = Quaternion.Euler(90f, 0, 0);

            // Horned Head
            GameObject head = CreateSubMesh(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 1.4f, 1.0f), new Vector3(0.7f, 0.65f, 0.75f), etherMat);
            GameObject hornL = CreateSubMesh(PrimitiveType.Cylinder, "Horn_L", head.transform, new Vector3(-0.25f, 0.45f, -0.1f), new Vector3(0.08f, 0.4f, 0.08f), voidMat);
            hornL.transform.localRotation = Quaternion.Euler(-25f, -15f, 0);
            GameObject hornR = CreateSubMesh(PrimitiveType.Cylinder, "Horn_R", head.transform, new Vector3(0.25f, 0.45f, -0.1f), new Vector3(0.08f, 0.4f, 0.08f), voidMat);
            hornR.transform.localRotation = Quaternion.Euler(-25f, 15f, 0);

            // 4 Legs with Shadow Claws
            CreateSubMesh(PrimitiveType.Capsule, "Leg_FL", root.transform, new Vector3(-0.5f, 0.5f, 0.65f), new Vector3(0.24f, 0.5f, 0.24f), etherMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_FR", root.transform, new Vector3(0.5f, 0.5f, 0.65f), new Vector3(0.24f, 0.5f, 0.24f), etherMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_BL", root.transform, new Vector3(-0.5f, 0.5f, -0.65f), new Vector3(0.24f, 0.5f, 0.24f), etherMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_BR", root.transform, new Vector3(0.5f, 0.5f, -0.65f), new Vector3(0.24f, 0.5f, 0.24f), etherMat);

            root.AddComponent<CapsuleCollider>().height = 2.4f;
            root.AddComponent<ShadowBeastBoss>();
            root.tag = "Enemy";

            SaveAndDestroyPrefab(root, $"{BOSS_DIR}/Boss_ShadowBeast.prefab");
        }

        private static void CreateFinalCorruptorBossPrefab()
        {
            GameObject root = new GameObject("Boss_FinalCorruptor");
            Material voidMat = GetMat("Mat_CorruptVoid");
            Material etherMat = GetMat("Mat_ShadowEther");

            // Massive Floating Core
            GameObject core = CreateSubMesh(PrimitiveType.Sphere, "Core", root.transform, new Vector3(0, 2.8f, 0), new Vector3(1.8f, 2.2f, 1.8f), voidMat);
            CreateSubMesh(PrimitiveType.Sphere, "VoidEye", core.transform, new Vector3(0, 0.2f, 0.85f), new Vector3(0.7f, 0.7f, 0.35f), etherMat);

            // 4 Corrupted Spire Horns
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Sin(angle) * 1.1f, 1.2f, Mathf.Cos(angle) * 1.1f);
                GameObject spire = CreateSubMesh(PrimitiveType.Cylinder, $"Spire_{i}", core.transform, pos, new Vector3(0.18f, 1.0f, 0.18f), voidMat);
                spire.transform.localRotation = Quaternion.Euler(Mathf.Cos(angle) * 30f, 0, -Mathf.Sin(angle) * 30f);
            }

            // Floating Void Tentacles
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Sin(angle) * 0.9f, -1.2f, Mathf.Cos(angle) * 0.9f);
                CreateSubMesh(PrimitiveType.Capsule, $"Tentacle_{i}", core.transform, pos, new Vector3(0.22f, 1.2f, 0.22f), voidMat);
            }

            SphereCollider col = root.AddComponent<SphereCollider>();
            col.center = new Vector3(0, 2.8f, 0);
            col.radius = 2.0f;
            root.AddComponent<FinalBossCorruptor>();
            root.tag = "Enemy";

            SaveAndDestroyPrefab(root, $"{BOSS_DIR}/Boss_FinalCorruptor.prefab");
        }
        #endregion

        #region Normal Enemies
        private static void CreateJunglePredatorPrefab()
        {
            GameObject root = new GameObject("Enemy_JunglePredator");
            Material skinMat = GetMat("Mat_PredatorSkin");

            // Body
            CreateSubMesh(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0, 0.6f, 0), new Vector3(0.45f, 0.8f, 0.45f), skinMat).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            // Head
            CreateSubMesh(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 0.85f, 0.5f), new Vector3(0.38f, 0.35f, 0.42f), skinMat);
            // Legs
            CreateSubMesh(PrimitiveType.Capsule, "Leg_FL", root.transform, new Vector3(-0.25f, 0.3f, 0.3f), new Vector3(0.12f, 0.3f, 0.12f), skinMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_FR", root.transform, new Vector3(0.25f, 0.3f, 0.3f), new Vector3(0.12f, 0.3f, 0.12f), skinMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_BL", root.transform, new Vector3(-0.25f, 0.3f, -0.3f), new Vector3(0.12f, 0.3f, 0.12f), skinMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_BR", root.transform, new Vector3(0.25f, 0.3f, -0.3f), new Vector3(0.12f, 0.3f, 0.12f), skinMat);

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.5f, 0);
            col.radius = 0.4f;
            col.height = 1.0f;

            root.AddComponent<UnityEngine.AI.NavMeshAgent>().speed = 3.5f;
            root.AddComponent<EnemyAI>();
            root.tag = "Enemy";

            SaveAndDestroyPrefab(root, $"{ENEMY_DIR}/Enemy_JunglePredator.prefab");
        }

        private static void CreateWildBoarPrefab()
        {
            GameObject root = new GameObject("Enemy_WildBoar");
            Material hideMat = GetMat("Mat_BoarHide");
            Material goldMat = GetMat("Mat_GuardianGold");

            // Stout Body
            CreateSubMesh(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0, 0.55f, 0), new Vector3(0.55f, 0.75f, 0.55f), hideMat).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            // Snout & Tusks
            GameObject head = CreateSubMesh(PrimitiveType.Cube, "Head", root.transform, new Vector3(0, 0.6f, 0.45f), new Vector3(0.35f, 0.35f, 0.4f), hideMat);
            CreateSubMesh(PrimitiveType.Cylinder, "Tusk_L", head.transform, new Vector3(-0.2f, -0.1f, 0.2f), new Vector3(0.05f, 0.18f, 0.05f), goldMat);
            CreateSubMesh(PrimitiveType.Cylinder, "Tusk_R", head.transform, new Vector3(0.2f, -0.1f, 0.2f), new Vector3(0.05f, 0.18f, 0.05f), goldMat);

            // Legs
            CreateSubMesh(PrimitiveType.Capsule, "Leg_FL", root.transform, new Vector3(-0.24f, 0.25f, 0.28f), new Vector3(0.14f, 0.25f, 0.14f), hideMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_FR", root.transform, new Vector3(0.24f, 0.25f, 0.28f), new Vector3(0.14f, 0.25f, 0.14f), hideMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_BL", root.transform, new Vector3(-0.24f, 0.25f, -0.28f), new Vector3(0.14f, 0.25f, 0.14f), hideMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_BR", root.transform, new Vector3(0.24f, 0.25f, -0.28f), new Vector3(0.14f, 0.25f, 0.14f), hideMat);

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.5f, 0);
            col.radius = 0.45f;
            col.height = 1.1f;

            root.AddComponent<UnityEngine.AI.NavMeshAgent>().speed = 4.0f;
            root.AddComponent<EnemyAI>();
            root.tag = "Enemy";

            SaveAndDestroyPrefab(root, $"{ENEMY_DIR}/Enemy_WildBoar.prefab");
        }

        private static void CreateToxicReptilePrefab()
        {
            GameObject root = new GameObject("Enemy_ToxicReptile");
            Material scaleMat = GetMat("Mat_ReptileScale");
            Material etherMat = GetMat("Mat_ShadowEther");

            // Slender Body & Neck
            CreateSubMesh(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0, 0.4f, 0), new Vector3(0.35f, 0.9f, 0.35f), scaleMat).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            // Frilled Head
            GameObject head = CreateSubMesh(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 0.55f, 0.55f), new Vector3(0.3f, 0.25f, 0.38f), scaleMat);
            CreateSubMesh(PrimitiveType.Cylinder, "Frill", head.transform, new Vector3(0, 0.08f, -0.05f), new Vector3(0.5f, 0.03f, 0.35f), etherMat);

            // Splayed Legs
            CreateSubMesh(PrimitiveType.Capsule, "Leg_FL", root.transform, new Vector3(-0.3f, 0.2f, 0.25f), new Vector3(0.1f, 0.22f, 0.1f), scaleMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_FR", root.transform, new Vector3(0.3f, 0.2f, 0.25f), new Vector3(0.1f, 0.22f, 0.1f), scaleMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_BL", root.transform, new Vector3(-0.3f, 0.2f, -0.25f), new Vector3(0.1f, 0.22f, 0.1f), scaleMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_BR", root.transform, new Vector3(0.3f, 0.2f, -0.25f), new Vector3(0.1f, 0.22f, 0.1f), scaleMat);

            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.35f, 0);
            col.radius = 0.35f;
            col.height = 1.0f;

            root.AddComponent<UnityEngine.AI.NavMeshAgent>().speed = 3.2f;
            root.AddComponent<EnemyAI>();
            root.tag = "Enemy";

            SaveAndDestroyPrefab(root, $"{ENEMY_DIR}/Enemy_ToxicReptile.prefab");
        }
        #endregion

        #region Wildlife
        private static void CreateDeerPrefab()
        {
            GameObject root = new GameObject("Wildlife_Deer");
            Material furMat = GetMat("Mat_DeerFur");
            Material antlerMat = GetMat("Mat_JungleWood");

            // Body
            CreateSubMesh(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0, 0.85f, 0), new Vector3(0.45f, 0.9f, 0.45f), furMat).transform.localRotation = Quaternion.Euler(90f, 0, 0);
            // Head & Antlers
            GameObject head = CreateSubMesh(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 1.3f, 0.5f), new Vector3(0.28f, 0.35f, 0.35f), furMat);
            CreateSubMesh(PrimitiveType.Cylinder, "Antler_L", head.transform, new Vector3(-0.15f, 0.35f, -0.05f), new Vector3(0.04f, 0.3f, 0.04f), antlerMat);
            CreateSubMesh(PrimitiveType.Cylinder, "Antler_R", head.transform, new Vector3(0.15f, 0.35f, -0.05f), new Vector3(0.04f, 0.3f, 0.04f), antlerMat);

            // Slender Legs
            CreateSubMesh(PrimitiveType.Capsule, "Leg_FL", root.transform, new Vector3(-0.18f, 0.4f, 0.35f), new Vector3(0.1f, 0.45f, 0.1f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_FR", root.transform, new Vector3(0.18f, 0.4f, 0.35f), new Vector3(0.1f, 0.45f, 0.1f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_BL", root.transform, new Vector3(-0.18f, 0.4f, -0.35f), new Vector3(0.1f, 0.45f, 0.1f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_BR", root.transform, new Vector3(0.18f, 0.4f, -0.35f), new Vector3(0.1f, 0.45f, 0.1f), furMat);

            root.AddComponent<WildlifeAI>();
            SaveAndDestroyPrefab(root, $"{WILD_DIR}/Wildlife_Deer.prefab");
        }

        private static void CreateParrotPrefab()
        {
            GameObject root = new GameObject("Wildlife_Parrot");
            Material featherMat = GetMat("Mat_ParrotFeather");
            Material goldMat = GetMat("Mat_GuardianGold");
            Material wingMat = GetMat("Mat_DivineCyan");

            // Body
            GameObject body = CreateSubMesh(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0, 0.35f, 0), new Vector3(0.2f, 0.35f, 0.2f), featherMat);
            // Beak
            CreateSubMesh(PrimitiveType.Cube, "Beak", body.transform, new Vector3(0, 0.12f, 0.15f), new Vector3(0.08f, 0.1f, 0.12f), goldMat);
            // Wings
            CreateSubMesh(PrimitiveType.Cube, "Wing_L", body.transform, new Vector3(-0.14f, 0.02f, 0), new Vector3(0.04f, 0.25f, 0.22f), wingMat);
            CreateSubMesh(PrimitiveType.Cube, "Wing_R", body.transform, new Vector3(0.14f, 0.02f, 0), new Vector3(0.04f, 0.25f, 0.22f), wingMat);

            WildlifeAI ai = root.AddComponent<WildlifeAI>();
            SaveAndDestroyPrefab(root, $"{WILD_DIR}/Wildlife_Parrot.prefab");
        }

        private static void CreateTreeFrogPrefab()
        {
            GameObject root = new GameObject("Wildlife_TreeFrog");
            Material frogMat = GetMat("Mat_FrogSkin");

            // Frog Body & Big Eyes
            GameObject body = CreateSubMesh(PrimitiveType.Sphere, "Body", root.transform, new Vector3(0, 0.12f, 0), new Vector3(0.22f, 0.14f, 0.28f), frogMat);
            CreateSubMesh(PrimitiveType.Sphere, "Eye_L", body.transform, new Vector3(-0.08f, 0.08f, 0.08f), new Vector3(0.07f, 0.07f, 0.07f), frogMat);
            CreateSubMesh(PrimitiveType.Sphere, "Eye_R", body.transform, new Vector3(0.08f, 0.08f, 0.08f), new Vector3(0.07f, 0.07f, 0.07f), frogMat);

            // Folded Legs
            CreateSubMesh(PrimitiveType.Sphere, "Leg_L", root.transform, new Vector3(-0.12f, 0.06f, -0.06f), new Vector3(0.08f, 0.08f, 0.14f), frogMat);
            CreateSubMesh(PrimitiveType.Sphere, "Leg_R", root.transform, new Vector3(0.12f, 0.06f, -0.06f), new Vector3(0.08f, 0.08f, 0.14f), frogMat);

            root.AddComponent<WildlifeAI>();
            SaveAndDestroyPrefab(root, $"{WILD_DIR}/Wildlife_TreeFrog.prefab");
        }

        private static void CreateButterflyPrefab()
        {
            GameObject root = new GameObject("Wildlife_Butterfly");
            Material wingMat = GetMat("Mat_ButterflyWing");
            Material bodyMat = GetMat("Mat_TitanFur");

            // Body
            CreateSubMesh(PrimitiveType.Cylinder, "Body", root.transform, Vector3.zero, new Vector3(0.03f, 0.15f, 0.03f), bodyMat);
            // Wings
            CreateSubMesh(PrimitiveType.Cube, "Wing_L", root.transform, new Vector3(-0.16f, 0.02f, 0), new Vector3(0.25f, 0.01f, 0.18f), wingMat);
            CreateSubMesh(PrimitiveType.Cube, "Wing_R", root.transform, new Vector3(0.16f, 0.02f, 0), new Vector3(0.25f, 0.01f, 0.18f), wingMat);

            root.AddComponent<WildlifeAI>();
            SaveAndDestroyPrefab(root, $"{WILD_DIR}/Wildlife_Butterfly.prefab");
        }

        private static void CreateSmallMonkeyPrefab()
        {
            GameObject root = new GameObject("Wildlife_Monkey");
            Material furMat = GetMat("Mat_MonkeyFur");
            Material skinMat = GetMat("Mat_MonkeySkin");

            // Small Monkey Body
            CreateSubMesh(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 0.65f, 0), new Vector3(0.26f, 0.24f, 0.24f), furMat);
            CreateSubMesh(PrimitiveType.Sphere, "Snout", root.transform, new Vector3(0, 0.6f, 0.12f), new Vector3(0.12f, 0.1f, 0.12f), skinMat);
            CreateSubMesh(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0, 0.38f, 0), new Vector3(0.22f, 0.25f, 0.2f), furMat);

            CreateSubMesh(PrimitiveType.Capsule, "Arm_L", root.transform, new Vector3(-0.16f, 0.38f, 0), new Vector3(0.07f, 0.18f, 0.07f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Arm_R", root.transform, new Vector3(0.16f, 0.38f, 0), new Vector3(0.07f, 0.18f, 0.07f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_L", root.transform, new Vector3(-0.09f, 0.12f, 0), new Vector3(0.08f, 0.15f, 0.08f), furMat);
            CreateSubMesh(PrimitiveType.Capsule, "Leg_R", root.transform, new Vector3(0.09f, 0.12f, 0), new Vector3(0.08f, 0.15f, 0.08f), furMat);

            root.AddComponent<WildlifeAI>();
            SaveAndDestroyPrefab(root, $"{WILD_DIR}/Wildlife_Monkey.prefab");
        }
        #endregion

        #region Environment, Ruins & Props
        private static void CreateJungleTrees()
        {
            Material woodMat = GetMat("Mat_JungleWood");
            Material leavesMat = GetMat("Mat_JungleLeaves");
            Material palmMat = GetMat("Mat_PalmLeaves");

            // 1. Giant Jungle Canopy Tree
            GameObject canopyTree = new GameObject("Tree_JungleCanopy");
            CreateSubMesh(PrimitiveType.Cylinder, "Trunk", canopyTree.transform, new Vector3(0, 3.0f, 0), new Vector3(0.9f, 3.0f, 0.9f), woodMat);
            CreateSubMesh(PrimitiveType.Sphere, "Foliage_Center", canopyTree.transform, new Vector3(0, 6.5f, 0), new Vector3(3.8f, 2.5f, 3.8f), leavesMat);
            CreateSubMesh(PrimitiveType.Sphere, "Foliage_L", canopyTree.transform, new Vector3(-1.8f, 5.8f, 0.5f), new Vector3(2.5f, 2.0f, 2.5f), leavesMat);
            CreateSubMesh(PrimitiveType.Sphere, "Foliage_R", canopyTree.transform, new Vector3(1.8f, 5.8f, -0.5f), new Vector3(2.5f, 2.0f, 2.5f), leavesMat);
            canopyTree.AddComponent<CapsuleCollider>().height = 7.0f;
            SaveAndDestroyPrefab(canopyTree, $"{ENV_TREE_DIR}/Tree_JungleCanopy.prefab");

            // 2. Coconut Palm Tree
            GameObject palm = new GameObject("Tree_CoconutPalm");
            GameObject palmTrunk = CreateSubMesh(PrimitiveType.Cylinder, "Trunk", palm.transform, new Vector3(0, 2.5f, 0.2f), new Vector3(0.4f, 2.5f, 0.4f), woodMat);
            palmTrunk.transform.localRotation = Quaternion.Euler(6f, 0, 0);

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f;
                GameObject frond = CreateSubMesh(PrimitiveType.Cube, $"Frond_{i}", palm.transform, new Vector3(0, 4.8f, 0.4f), new Vector3(0.4f, 0.08f, 2.4f), palmMat);
                frond.transform.localRotation = Quaternion.Euler(25f, angle, 0);
            }
            palm.AddComponent<CapsuleCollider>().height = 5.2f;
            SaveAndDestroyPrefab(palm, $"{ENV_TREE_DIR}/Tree_CoconutPalm.prefab");
        }

        private static void CreateJunglePlants()
        {
            Material leafMat = GetMat("Mat_JungleLeaves");
            Material glowMat = GetMat("Mat_DivineCyan");
            Material redMat = GetMat("Mat_ParrotFeather");

            // 1. Jungle Fern
            GameObject fern = new GameObject("Plant_JungleFern");
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f;
                GameObject frond = CreateSubMesh(PrimitiveType.Cube, $"Leaf_{i}", fern.transform, new Vector3(0, 0.35f, 0), new Vector3(0.25f, 0.04f, 1.1f), leafMat);
                frond.transform.localRotation = Quaternion.Euler(-35f, angle, 0);
            }
            SaveAndDestroyPrefab(fern, $"{ENV_PLANT_DIR}/Plant_JungleFern.prefab");

            // 2. Tropical Bush
            GameObject bush = new GameObject("Plant_TropicalBush");
            CreateSubMesh(PrimitiveType.Sphere, "Foliage1", bush.transform, new Vector3(0, 0.6f, 0), new Vector3(1.2f, 0.9f, 1.2f), leafMat);
            CreateSubMesh(PrimitiveType.Sphere, "Foliage2", bush.transform, new Vector3(0.4f, 0.5f, 0.2f), new Vector3(0.8f, 0.7f, 0.8f), leafMat);
            SaveAndDestroyPrefab(bush, $"{ENV_PLANT_DIR}/Plant_TropicalBush.prefab");

            // 3. Bioluminescent Mushroom
            GameObject mushroom = new GameObject("Plant_GlowingMushroom");
            Material woodMat = GetMat("Mat_JungleWood");
            CreateSubMesh(PrimitiveType.Cylinder, "Stem", mushroom.transform, new Vector3(0, 0.35f, 0), new Vector3(0.18f, 0.35f, 0.18f), woodMat);
            CreateSubMesh(PrimitiveType.Sphere, "Cap", mushroom.transform, new Vector3(0, 0.7f, 0), new Vector3(0.85f, 0.35f, 0.85f), glowMat);
            SaveAndDestroyPrefab(mushroom, $"{ENV_PLANT_DIR}/Plant_GlowingMushroom.prefab");

            // 4. Tropical Hibiscus Flower
            GameObject flower = new GameObject("Plant_HibiscusFlower");
            CreateSubMesh(PrimitiveType.Cylinder, "Stem", flower.transform, new Vector3(0, 0.25f, 0), new Vector3(0.06f, 0.25f, 0.06f), leafMat);
            CreateSubMesh(PrimitiveType.Sphere, "Petals", flower.transform, new Vector3(0, 0.52f, 0), new Vector3(0.5f, 0.15f, 0.5f), redMat);
            SaveAndDestroyPrefab(flower, $"{ENV_PLANT_DIR}/Plant_HibiscusFlower.prefab");
        }

        private static void CreateJungleRocks()
        {
            Material rockMat = GetMat("Mat_MossyRock");

            // 1. Mossy Boulder
            GameObject boulder = new GameObject("Rock_MossyBoulder");
            CreateSubMesh(PrimitiveType.Sphere, "Mesh", boulder.transform, new Vector3(0, 0.8f, 0), new Vector3(1.8f, 1.5f, 1.6f), rockMat);
            boulder.AddComponent<SphereCollider>().radius = 0.85f;
            SaveAndDestroyPrefab(boulder, $"{ENV_ROCK_DIR}/Rock_MossyBoulder.prefab");

            // 2. Cliff Formation
            GameObject cliff = new GameObject("Rock_CliffFormation");
            CreateSubMesh(PrimitiveType.Cube, "CliffBlock", cliff.transform, new Vector3(0, 2.5f, 0), new Vector3(4.0f, 5.0f, 2.5f), rockMat);
            cliff.AddComponent<BoxCollider>().size = new Vector3(4.0f, 5.0f, 2.5f);
            SaveAndDestroyPrefab(cliff, $"{ENV_ROCK_DIR}/Rock_CliffFormation.prefab");

            // 3. Floating Island
            GameObject island = new GameObject("Rock_FloatingIsland");
            CreateSubMesh(PrimitiveType.Sphere, "IslandTop", island.transform, new Vector3(0, 0, 0), new Vector3(4.5f, 0.8f, 4.5f), rockMat);
            GameObject bottom = CreateSubMesh(PrimitiveType.Cylinder, "IslandBase", island.transform, new Vector3(0, -1.2f, 0), new Vector3(3.2f, 1.2f, 3.2f), rockMat);
            bottom.transform.localScale = new Vector3(2.5f, 1.2f, 2.5f);
            island.AddComponent<BoxCollider>().size = new Vector3(4.5f, 1.5f, 4.5f);
            island.AddComponent<FloatingIsland>();
            SaveAndDestroyPrefab(island, $"{ENV_ROCK_DIR}/Rock_FloatingIsland.prefab");
        }

        private static void CreateJungleRuinsAndPuzzles()
        {
            Material stoneMat = GetMat("Mat_AncientStone");
            Material runeGlow = GetMat("Mat_RuneActiveGlow");

            // 1. Ancient Stone Arch
            GameObject arch = new GameObject("Ruins_AncientArch");
            CreateSubMesh(PrimitiveType.Cube, "Pillar_L", arch.transform, new Vector3(-2.0f, 2.5f, 0), new Vector3(0.8f, 5.0f, 0.8f), stoneMat);
            CreateSubMesh(PrimitiveType.Cube, "Pillar_R", arch.transform, new Vector3(2.0f, 2.5f, 0), new Vector3(0.8f, 5.0f, 0.8f), stoneMat);
            CreateSubMesh(PrimitiveType.Cube, "Lintel", arch.transform, new Vector3(0, 5.2f, 0), new Vector3(5.2f, 0.7f, 1.0f), stoneMat);
            SaveAndDestroyPrefab(arch, $"{ENV_RUINS_DIR}/Ruins_AncientArch.prefab");

            // 2. Rune Switch Pedestal
            GameObject rune = new GameObject("Ruins_RunePedestal");
            CreateSubMesh(PrimitiveType.Cylinder, "Pedestal", rune.transform, new Vector3(0, 0.5f, 0), new Vector3(0.9f, 0.5f, 0.9f), stoneMat);
            CreateSubMesh(PrimitiveType.Sphere, "RuneGem", rune.transform, new Vector3(0, 1.1f, 0), new Vector3(0.45f, 0.45f, 0.45f), runeGlow);
            rune.AddComponent<BoxCollider>().size = new Vector3(1.2f, 1.4f, 1.2f);
            rune.AddComponent<RuneSwitch>();
            SaveAndDestroyPrefab(rune, $"{ENV_RUINS_DIR}/Ruins_RunePedestal.prefab");

            // 3. Ancient Heavy Stone Door
            GameObject door = new GameObject("Ruins_HeavyStoneDoor");
            GameObject slabL = CreateSubMesh(PrimitiveType.Cube, "DoorSlab_Left", door.transform, new Vector3(-1.25f, 2.5f, 0), new Vector3(2.4f, 5.0f, 0.6f), stoneMat);
            GameObject slabR = CreateSubMesh(PrimitiveType.Cube, "DoorSlab_Right", door.transform, new Vector3(1.25f, 2.5f, 0), new Vector3(2.4f, 5.0f, 0.6f), stoneMat);
            CreateSubMesh(PrimitiveType.Sphere, "RuneInlay_L", slabL.transform, new Vector3(0.6f, 0, 0.35f), new Vector3(0.5f, 0.5f, 0.1f), runeGlow);
            CreateSubMesh(PrimitiveType.Sphere, "RuneInlay_R", slabR.transform, new Vector3(-0.6f, 0, 0.35f), new Vector3(0.5f, 0.5f, 0.1f), runeGlow);

            door.AddComponent<BoxCollider>().size = new Vector3(5.0f, 5.0f, 0.8f);
            door.AddComponent<AncientDoor>();
            SaveAndDestroyPrefab(door, $"{ENV_RUINS_DIR}/Ruins_HeavyStoneDoor.prefab");
        }

        private static void CreateJungleProps()
        {
            Material goldMat = GetMat("Mat_GuardianGold");
            Material cyanMat = GetMat("Mat_DivineCyan");
            Material woodMat = GetMat("Mat_JungleWood");

            // 1. Golden Banana Prop
            GameObject banana = new GameObject("Prop_GoldenBanana");
            CreateSubMesh(PrimitiveType.Capsule, "BananaMesh", banana.transform, new Vector3(0, 0.4f, 0), new Vector3(0.18f, 0.45f, 0.18f), goldMat).transform.localRotation = Quaternion.Euler(30f, 0, 20f);
            SphereCollider bCol = banana.AddComponent<SphereCollider>();
            bCol.isTrigger = true;
            bCol.radius = 0.6f;
            banana.AddComponent<MonkeyAdventure.Collectibles.CollectibleItem>();
            banana.tag = "Food";
            SaveAndDestroyPrefab(banana, $"{PROP_DIR}/Prop_GoldenBanana.prefab");

            // 2. Ancient Coin Prop
            GameObject coin = new GameObject("Prop_AncientCoin");
            GameObject coinMesh = CreateSubMesh(PrimitiveType.Cylinder, "CoinMesh", coin.transform, new Vector3(0, 0.4f, 0), new Vector3(0.45f, 0.06f, 0.45f), goldMat);
            coinMesh.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            SphereCollider cCol = coin.AddComponent<SphereCollider>();
            cCol.isTrigger = true;
            cCol.radius = 0.55f;
            coin.AddComponent<MonkeyAdventure.Collectibles.CollectibleItem>();
            coin.tag = "Coin";
            SaveAndDestroyPrefab(coin, $"{PROP_DIR}/Prop_AncientCoin.prefab");

            // 3. Breakable Relic Prop
            GameObject relic = new GameObject("Prop_BreakableRelic");
            CreateSubMesh(PrimitiveType.Cylinder, "Pedestal", relic.transform, new Vector3(0, 0.3f, 0), new Vector3(0.6f, 0.3f, 0.6f), GetMat("Mat_AncientStone"));
            CreateSubMesh(PrimitiveType.Sphere, "Crystal", relic.transform, new Vector3(0, 0.9f, 0), new Vector3(0.55f, 0.8f, 0.55f), cyanMat);
            relic.AddComponent<BoxCollider>().size = new Vector3(0.8f, 1.4f, 0.8f);
            relic.AddComponent<BreakableRelic>();
            SaveAndDestroyPrefab(relic, $"{PROP_DIR}/Prop_BreakableRelic.prefab");

            // 4. Hollow Fallen Log
            GameObject log = new GameObject("Prop_HollowFallenLog");
            GameObject logCyl = CreateSubMesh(PrimitiveType.Cylinder, "LogCylinder", log.transform, new Vector3(0, 0.5f, 0), new Vector3(1.1f, 2.5f, 1.1f), woodMat);
            logCyl.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            log.AddComponent<CapsuleCollider>().height = 5.0f;
            SaveAndDestroyPrefab(log, $"{PROP_DIR}/Prop_HollowFallenLog.prefab");

            // 5. Climbable Jungle Vine
            GameObject vine = new GameObject("Prop_ClimbableVine");
            CreateSubMesh(PrimitiveType.Cylinder, "VineStem", vine.transform, new Vector3(0, 4.0f, 0), new Vector3(0.18f, 4.0f, 0.18f), GetMat("Mat_JungleLeaves"));
            CapsuleCollider vCol = vine.AddComponent<CapsuleCollider>();
            vCol.isTrigger = true;
            vCol.height = 8.0f;
            vine.tag = "Vine";
            SaveAndDestroyPrefab(vine, $"{PROP_DIR}/Prop_ClimbableVine.prefab");
        }
        #endregion

        #region SubMesh Helper
        private static GameObject CreateSubMesh(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            // Remove default collider from sub-meshes to avoid hierarchy collider clutter
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            if (mat != null)
            {
                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = mat;
            }

            return go;
        }

        private static GameObject CreateConeSubMesh(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            MeshFilter mf = go.AddComponent<MeshFilter>();
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;

            Mesh mesh = new Mesh();
            mesh.name = $"{name}_Mesh";

            int segments = 8;
            Vector3[] vertices = new Vector3[segments + 2];
            int[] triangles = new int[segments * 6];

            // Apex at bottom (pointing down) for sharp fangs
            vertices[0] = new Vector3(0, -0.5f, 0);
            // Base center at top
            vertices[segments + 1] = new Vector3(0, 0.5f, 0);

            float radius = 0.5f;
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0.5f, Mathf.Sin(angle) * radius);
            }

            // Side triangles (Apex to base ring)
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = next + 1;
            }

            // Base cap triangles (Top disk)
            int baseOffset = segments * 3;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles[baseOffset + i * 3] = segments + 1;
                triangles[baseOffset + i * 3 + 1] = next + 1;
                triangles[baseOffset + i * 3 + 2] = i + 1;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mf.sharedMesh = mesh;
            return go;
        }

        private static void SaveAndDestroyPrefab(GameObject go, string prefabPath)
        {
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }
        #endregion
    }
}
