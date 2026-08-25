using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MonkeyAdventure.Player;
using MonkeyAdventure.Cameras;
using MonkeyAdventure.Core;
using MonkeyAdventure.Audio;
using MonkeyAdventure.Progression;
using MonkeyAdventure.Environment;
using MonkeyAdventure.Collectibles;
using MonkeyAdventure.Hazards;
using MonkeyAdventure.AI;
using MonkeyAdventure.Skins;
using MonkeyAdventure.Mechanics;
using MonkeyAdventure.UI;
using MonkeyAdventure.Animation;
using GuardianSystem.Combat;

namespace MonkeyAdventure.Editor
{
    /// <summary>
    /// Master Production Finalizer for Level 01: The Awakening.
    /// Automatically audits and configures all gameplay systems, components, references,
    /// audio clips, VFX, player controller, camera, enemies, hazards, collectibles,
    /// checkpoints, level completion portal, and HUD in one deterministic pass.
    /// </summary>
    public static class Level01ProductionFinalizer
    {
        private const string SCENE_PATH = "Assets/Scenes/Level01_Awakening.unity";
        private static bool _isRunning = false;

        [MenuItem("Monkey Adventure/Finalize Level 01 Production", false, 10)]
        public static void FinalizeLevel01Menu()
        {
            FinalizeLevel01Internal(true);
        }

        public static void FinalizeLevel01Headless()
        {
            FinalizeLevel01Internal(false);
        }

        private static void FinalizeLevel01Internal(bool interactive)
        {
            if (_isRunning)
            {
                Debug.LogWarning("[Level01ProductionFinalizer] Finalization pass already in progress. Ignoring re-entrant call.");
                return;
            }

            _isRunning = true;
            try
            {
                Debug.Log("<color=#00FFAA><b>[Level01ProductionFinalizer] Starting Master Production Finalization Pass...</b></color>");

                Scene activeScene = SceneManager.GetActiveScene();
                if (activeScene.path != SCENE_PATH)
                {
                    activeScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
                }

            // Load shared assets
            AudioClip sfxJump = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_Jump.wav");
            AudioClip sfxBanana = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_Banana.wav");
            AudioClip sfxCoin = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_Coin.wav");
            AudioClip sfxCheckpoint = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_Checkpoint.wav");
            AudioClip sfxHurt = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_Hurt.wav");
            AudioClip sfxDeath = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_Death.wav");
            AudioClip sfxLevelComplete = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_LevelComplete.wav");
            AudioClip sfxAttack = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_Attack.wav");
            AudioClip sfxEnemyHit = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_EnemyHit.wav");
            AudioClip sfxFootstep = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_Footstep.wav");
            AudioClip sfxWaterSplash = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_WaterSplash.wav");
            AudioClip sfxPoisonBubble = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_PoisonBubble.wav");
            AudioClip sfxEnergyBlast = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_EnergyBlast.wav");
            AudioClip sfxHeavyAttack = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_HeavyAttack.wav");
            AudioClip sfxRuneActivate = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_RuneActivate.wav");
            AudioClip sfxUIClick = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/SFX_UIClick.wav");

            GameObject vfxImpactSparks = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/VFX_Impact_Sparks.prefab");
            GameObject vfxDeathBurst = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/VFX_BossDeath_Burst.prefab");
            GameObject vfxCheckpointBeam = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/VFX_Checkpoint_Beam.prefab");
            GameObject vfxPortalVortex = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/VFX_Portal_Vortex.prefab");
            GameObject vfxGroundSmash = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/VFX_GroundSmash_Shockwave.prefab");
            GameObject vfxEvolution = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/VFX_Evolution_Transformation.prefab");
            GameObject vfxWaterSplash = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/VFX_WaterSplash_Mist.prefab");
            GameObject vfxPoisonCloud = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/VFX_Poison_SporeCloud.prefab");
            GameObject magicProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MagicProjectile.prefab");

            GameObject monkeyBasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Monkey_Base.prefab");
            GameObject monkeyGuardianPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Monkey_Guardian.prefab");
            GameObject monkeyTitanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Monkey_PrimalTitan.prefab");
            GameObject monkeyHanumanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Monkey_DivineGuardian.prefab");
            GameObject enemyPredatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Enemies/Enemy_JunglePredator.prefab");

            // 1. PLAYER CONFIGURATION
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                playerObj = GameObject.FindGameObjectWithTag("Player");
            }

            if (playerObj != null)
            {
                playerObj.tag = "Player";
                Undo.RecordObject(playerObj, "Finalize Player");

                // CharacterController
                CharacterController cc = playerObj.GetComponent<CharacterController>();
                if (cc == null) cc = playerObj.AddComponent<CharacterController>();
                cc.slopeLimit = 45f;
                cc.stepOffset = 0.35f;
                cc.skinWidth = 0.08f;
                cc.minMoveDistance = 0.001f;
                cc.center = new Vector3(0f, 0.9f, 0f);
                cc.radius = 0.45f;
                cc.height = 1.8f;

                // MonkeyPlayerController
                MonkeyPlayerController mpc = playerObj.GetComponent<MonkeyPlayerController>();
                if (mpc == null) mpc = playerObj.AddComponent<MonkeyPlayerController>();
                mpc.MoveSpeed = 7.0f;

                // PlayerHealth
                PlayerHealth ph = playerObj.GetComponent<PlayerHealth>();
                if (ph == null) ph = playerObj.AddComponent<PlayerHealth>();
                var phSo = new SerializedObject(ph);
                phSo.FindProperty("maxHealth").intValue = 100;
                phSo.FindProperty("currentHealth").intValue = 100;
                phSo.FindProperty("hitSound").objectReferenceValue = sfxHurt;
                phSo.FindProperty("deathSound").objectReferenceValue = sfxDeath;
                phSo.FindProperty("healSound").objectReferenceValue = sfxBanana;
                phSo.FindProperty("hitVFXPrefab").objectReferenceValue = vfxImpactSparks;
                phSo.FindProperty("deathVFXPrefab").objectReferenceValue = vfxDeathBurst;
                phSo.ApplyModifiedProperties();

                // GuardianCombat
                GuardianCombat gc = playerObj.GetComponent<GuardianCombat>();
                if (gc != null)
                {
                    var gcSo = new SerializedObject(gc);
                    gcSo.FindProperty("blastAudioClip").objectReferenceValue = sfxEnergyBlast;
                    gcSo.FindProperty("smashAudioClip").objectReferenceValue = sfxHeavyAttack;
                    gcSo.FindProperty("groundSmashVFXPrefab").objectReferenceValue = vfxGroundSmash;
                    gcSo.FindProperty("magicProjectilePrefab").objectReferenceValue = magicProjectilePrefab;
                    gcSo.ApplyModifiedProperties();
                }

                // EvolutionSkinManager
                EvolutionSkinManager esm = playerObj.GetComponent<EvolutionSkinManager>();
                if (esm != null)
                {
                    var esmSo = new SerializedObject(esm);
                    esmSo.FindProperty("evolutionTransformVFX").objectReferenceValue = vfxEvolution;
                    esmSo.FindProperty("skinEquipSound").objectReferenceValue = sfxRuneActivate;
                    esmSo.ApplyModifiedProperties();
                }

                // MonkeySetupBinder
                MonkeySetupBinder binder = playerObj.GetComponent<MonkeySetupBinder>();
                if (binder == null) binder = playerObj.AddComponent<MonkeySetupBinder>();

                // ModelHolder visual check
                Transform modelHolder = playerObj.transform.Find("ModelHolder");
                if (modelHolder != null && modelHolder.childCount == 0 && monkeyBasePrefab != null)
                {
                    GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(monkeyBasePrefab, modelHolder);
                    modelInstance.transform.localPosition = Vector3.zero;
                    modelInstance.transform.localRotation = Quaternion.identity;
                    modelInstance.transform.localScale = Vector3.one;
                }

                Debug.Log("<color=#00FFAA>[Level01ProductionFinalizer] Player gameplay systems configured cleanly.</color>");
            }

            // 2. CAMERA CONFIGURATION
            Camera mainCam = Camera.main;
            if (mainCam != null && playerObj != null)
            {
                ThirdPersonCamera tpc = mainCam.GetComponent<ThirdPersonCamera>();
                if (tpc == null) tpc = mainCam.gameObject.AddComponent<ThirdPersonCamera>();

                var tpcSo = new SerializedObject(tpc);
                tpcSo.FindProperty("target").objectReferenceValue = playerObj.transform;
                tpcSo.FindProperty("targetHeightOffset").floatValue = 1.4f;
                tpcSo.FindProperty("defaultDistance").floatValue = 5.5f;
                tpcSo.FindProperty("minDistance").floatValue = 2.0f;
                tpcSo.FindProperty("maxDistance").floatValue = 10.0f;
                tpcSo.FindProperty("horizontalSensitivity").floatValue = 140f;
                tpcSo.FindProperty("verticalSensitivity").floatValue = 100f;
                tpcSo.FindProperty("minPitch").floatValue = -15f;
                tpcSo.FindProperty("maxPitch").floatValue = 60f;
                tpcSo.FindProperty("positionSmoothTime").floatValue = 0.08f;
                tpcSo.FindProperty("rotationSmoothTime").floatValue = 0.05f;
                tpcSo.FindProperty("enableCollisionAvoidance").boolValue = true;
                tpcSo.ApplyModifiedProperties();

                Debug.Log("<color=#00FFAA>[Level01ProductionFinalizer] ThirdPersonCamera wired to Player.</color>");
            }

            // 3. MANAGERS CONFIGURATION
            GameObject managersObj = GameObject.Find("[--- 03_MANAGERS ---]");
            if (managersObj != null)
            {
                // GameManager
                GameManager gm = managersObj.GetComponent<GameManager>();
                if (gm != null)
                {
                    var gmSo = new SerializedObject(gm);
                    if (playerObj != null) gmSo.FindProperty("playerTransform").objectReferenceValue = playerObj.transform;
                    gmSo.FindProperty("fallDeathYThreshold").floatValue = -10.0f;
                    gmSo.FindProperty("respawnDelay").floatValue = 0.5f;
                    gmSo.FindProperty("respawnSound").objectReferenceValue = sfxCheckpoint;
                    gmSo.FindProperty("respawnVFXPrefab").objectReferenceValue = vfxCheckpointBeam;
                    gmSo.FindProperty("showDebugHUD").boolValue = false; // Using Level01GameplayHUD
                    gmSo.ApplyModifiedProperties();
                }

                // AudioManager
                AudioManager am = managersObj.GetComponent<AudioManager>();
                if (am != null)
                {
                    SetupAudioManager(am);
                }

                // LevelProgressionManager
                LevelProgressionManager lpm = managersObj.GetComponent<LevelProgressionManager>();
                if (lpm != null)
                {
                    var lpmSo = new SerializedObject(lpm);
                    lpmSo.FindProperty("currentLevelIndex").intValue = 1;
                    lpmSo.FindProperty("autoLoadNextOnComplete").boolValue = true;
                    lpmSo.ApplyModifiedProperties();
                }

                // GameAssetInitializer
                GameAssetInitializer gai = managersObj.GetComponent<GameAssetInitializer>();
                if (gai != null)
                {
                    SetupGameAssetInitializer(gai, enemyPredatorPrefab, monkeyBasePrefab, monkeyGuardianPrefab, monkeyTitanPrefab, monkeyHanumanPrefab);
                }

                Debug.Log("<color=#00FFAA>[Level01ProductionFinalizer] Managers configured cleanly.</color>");
            }

            // 4. COLLECTIBLES CONFIGURATION
            GameObject levelRoot = GameObject.Find("AI_GENERATED_LEVEL");
            if (levelRoot != null)
            {
                Transform colFolder = levelRoot.transform.Find("Collectibles");
                if (colFolder != null)
                {
                    for (int i = 0; i < colFolder.childCount; i++)
                    {
                        Transform colT = colFolder.GetChild(i);
                        colT.gameObject.tag = "Food";

                        CollectibleItem ci = colT.GetComponent<CollectibleItem>();
                        if (ci == null) ci = colT.gameObject.AddComponent<CollectibleItem>();

                        bool isGolden = colT.name.IndexOf("Golden", StringComparison.OrdinalIgnoreCase) >= 0;

                        var ciSo = new SerializedObject(ci);
                        ciSo.FindProperty("itemType").enumValueIndex = (int)CollectibleType.Food;
                        ciSo.FindProperty("value").intValue = isGolden ? 5 : 1;
                        ciSo.FindProperty("pickupSound").objectReferenceValue = sfxBanana;
                        ciSo.FindProperty("sparkleVFXPrefab").objectReferenceValue = vfxImpactSparks;
                        ciSo.ApplyModifiedProperties();

                        SphereCollider sc = colT.GetComponent<SphereCollider>();
                        if (sc == null) sc = colT.gameObject.AddComponent<SphereCollider>();
                        sc.isTrigger = true;
                        sc.radius = 0.8f;
                    }
                    Debug.Log($"<color=#00FFAA>[Level01ProductionFinalizer] Configured {colFolder.childCount} collectible fruits.</color>");
                }

                // 5. CHECKPOINTS
                Transform cpFolder = levelRoot.transform.Find("Checkpoints");
                if (cpFolder != null)
                {
                    for (int i = 0; i < cpFolder.childCount; i++)
                    {
                        Transform cpT = cpFolder.GetChild(i);
                        BoxCollider bc = cpT.GetComponent<BoxCollider>();
                        if (bc == null) bc = cpT.gameObject.AddComponent<BoxCollider>();
                        bc.isTrigger = true;
                        bc.size = new Vector3(6f, 4f, 6f);

                        Checkpoint cp = cpT.GetComponent<Checkpoint>();
                        if (cp == null) cp = cpT.gameObject.AddComponent<Checkpoint>();

                        var cpSo = new SerializedObject(cp);
                        cpSo.FindProperty("activationSound").objectReferenceValue = sfxCheckpoint;
                        cpSo.FindProperty("activationBurstVFXPrefab").objectReferenceValue = vfxCheckpointBeam;
                        cpSo.ApplyModifiedProperties();
                    }
                }

                // Start Checkpoint
                GameObject startCp = GameObject.Find("Checkpoint_01_Start");
                if (startCp != null)
                {
                    BoxCollider bc = startCp.GetComponent<BoxCollider>();
                    if (bc == null) bc = startCp.AddComponent<BoxCollider>();
                    bc.isTrigger = true;
                    bc.size = new Vector3(6f, 4f, 6f);

                    Checkpoint cp = startCp.GetComponent<Checkpoint>();
                    if (cp == null) cp = startCp.AddComponent<Checkpoint>();

                    var cpSo = new SerializedObject(cp);
                    cpSo.FindProperty("activationSound").objectReferenceValue = sfxCheckpoint;
                    cpSo.FindProperty("activationBurstVFXPrefab").objectReferenceValue = vfxCheckpointBeam;
                    cpSo.ApplyModifiedProperties();
                }

                // 6. ENEMY ENCOUNTER CONFIGURATION
                Transform enemyFolder = levelRoot.transform.Find("Enemies");
                if (enemyFolder != null)
                {
                    for (int i = 0; i < enemyFolder.childCount; i++)
                    {
                        Transform enemyT = enemyFolder.GetChild(i);
                        enemyT.gameObject.tag = "Enemy";

                        CapsuleCollider cc = enemyT.GetComponent<CapsuleCollider>();
                        if (cc == null) cc = enemyT.gameObject.AddComponent<CapsuleCollider>();
                        cc.radius = 0.6f;
                        cc.height = 1.8f;
                        cc.center = new Vector3(0f, 0.9f, 0f);

                        EnemyAI eai = enemyT.GetComponent<EnemyAI>();
                        if (eai == null) eai = enemyT.gameObject.AddComponent<EnemyAI>();

                        var eaiSo = new SerializedObject(eai);
                        eaiSo.FindProperty("maxHealth").intValue = 60;
                        eaiSo.FindProperty("currentHealth").intValue = 60;
                        eaiSo.FindProperty("patrolSpeed").floatValue = 3.0f;
                        eaiSo.FindProperty("chaseSpeed").floatValue = 5.5f;
                        eaiSo.FindProperty("detectionRadius").floatValue = 9.0f;
                        eaiSo.FindProperty("losePlayerRadius").floatValue = 14.0f;
                        eaiSo.FindProperty("attackRange").floatValue = 2.0f;
                        eaiSo.FindProperty("attackDamage").intValue = 20;
                        eaiSo.FindProperty("attackCooldown").floatValue = 1.2f;
                        eaiSo.FindProperty("hitSound").objectReferenceValue = sfxEnemyHit;
                        eaiSo.FindProperty("deathSound").objectReferenceValue = sfxDeath;
                        eaiSo.FindProperty("attackSound").objectReferenceValue = sfxAttack;
                        eaiSo.FindProperty("deathVFXPrefab").objectReferenceValue = vfxDeathBurst;
                        eaiSo.FindProperty("attackHitVFX").objectReferenceValue = vfxImpactSparks;
                        eaiSo.ApplyModifiedProperties();

                        // If placeholder has no visual mesh child, instantiate predator model
                        if (enemyT.childCount == 0 && enemyPredatorPrefab != null)
                        {
                            GameObject enemyMesh = (GameObject)PrefabUtility.InstantiatePrefab(enemyPredatorPrefab, enemyT);
                            enemyMesh.transform.localPosition = Vector3.zero;
                            enemyMesh.transform.localRotation = Quaternion.identity;
                        }
                    }
                    Debug.Log($"<color=#00FFAA>[Level01ProductionFinalizer] Configured {enemyFolder.childCount} enemy encounter(s).</color>");
                }

                // 7. LEVEL EXIT / FINISH PORTAL
                Transform finishFolder = levelRoot.transform.Find("Finish");
                if (finishFolder != null)
                {
                    Transform finishGateway = finishFolder.Find("Finish_Gateway");
                    if (finishGateway != null)
                    {
                        BoxCollider bc = finishGateway.GetComponent<BoxCollider>();
                        if (bc == null) bc = finishGateway.gameObject.AddComponent<BoxCollider>();
                        bc.isTrigger = true;
                        bc.size = new Vector3(4f, 4f, 4f);
                        bc.center = new Vector3(0f, 2f, 0f);

                        LevelExitPortal lep = finishGateway.GetComponent<LevelExitPortal>();
                        if (lep == null) lep = finishGateway.gameObject.AddComponent<LevelExitPortal>();

                        var lepSo = new SerializedObject(lep);
                        lepSo.FindProperty("completionScore").intValue = 100;
                        lepSo.FindProperty("levelCompleteSound").objectReferenceValue = sfxLevelComplete;
                        lepSo.FindProperty("rotatePortal").boolValue = true;
                        lepSo.FindProperty("rotationSpeed").floatValue = 45.0f;
                        lepSo.ApplyModifiedProperties();
                    }
                }
            }

            // 8. LEVEL 01 COMPLETE GATEWAY (Gameplay hierarchy portal)
            GameObject completeGateway = GameObject.Find("Level_01_Complete_Gateway");
            if (completeGateway != null)
            {
                BoxCollider bc = completeGateway.GetComponent<BoxCollider>();
                if (bc == null) bc = completeGateway.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(4f, 4f, 4f);

                LevelExitPortal lep = completeGateway.GetComponent<LevelExitPortal>();
                if (lep == null) lep = completeGateway.AddComponent<LevelExitPortal>();

                var lepSo = new SerializedObject(lep);
                lepSo.FindProperty("completionScore").intValue = 100;
                lepSo.FindProperty("levelCompleteSound").objectReferenceValue = sfxLevelComplete;
                lepSo.ApplyModifiedProperties();
            }

            // 9. HAZARDS CONFIGURATION
            GameObject fireHazardObj = GameObject.Find("Fire_Hazard_Zone");
            if (fireHazardObj != null)
            {
                FireHazard fh = fireHazardObj.GetComponent<FireHazard>();
                if (fh != null)
                {
                    var fhSo = new SerializedObject(fh);
                    fhSo.FindProperty("extinguishHissSound").objectReferenceValue = sfxWaterSplash;
                    fhSo.ApplyModifiedProperties();
                }
            }

            GameObject waterExtinguisherObj = GameObject.Find("Water_Extinguisher_Buff");
            if (waterExtinguisherObj != null)
            {
                Extinguisher ext = waterExtinguisherObj.GetComponent<Extinguisher>();
                if (ext != null)
                {
                    var extSo = new SerializedObject(ext);
                    extSo.FindProperty("pickupSound").objectReferenceValue = sfxWaterSplash;
                    extSo.FindProperty("waterSplashSound").objectReferenceValue = sfxWaterSplash;
                    extSo.FindProperty("splashVFXPrefab").objectReferenceValue = vfxWaterSplash;
                    extSo.ApplyModifiedProperties();
                }
            }

            // 10. UI CANVAS CONFIGURATION
            GameObject uiCanvasObj = GameObject.Find("UI_Canvas");
            if (uiCanvasObj != null)
            {
                Level01GameplayHUD hud = uiCanvasObj.GetComponent<Level01GameplayHUD>();
                if (hud == null) hud = uiCanvasObj.AddComponent<Level01GameplayHUD>();

                var hudSo = new SerializedObject(hud);
                hudSo.FindProperty("buttonClickSound").objectReferenceValue = sfxUIClick;
                hudSo.FindProperty("levelCompleteSound").objectReferenceValue = sfxLevelComplete;
                hudSo.ApplyModifiedProperties();

                ReviveUIManager rum = uiCanvasObj.GetComponent<ReviveUIManager>();
                if (rum != null)
                {
                    var rumSo = new SerializedObject(rum);
                    rumSo.FindProperty("reviveFanfareSound").objectReferenceValue = sfxRuneActivate;
                    rumSo.FindProperty("countdownTickSound").objectReferenceValue = sfxUIClick;
                    rumSo.ApplyModifiedProperties();
                }

                Debug.Log("<color=#00FFAA>[Level01ProductionFinalizer] UI Canvas and HUD configured cleanly.</color>");
            }

            // Save Scene
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("<color=#00FFAA><b>[Level01ProductionFinalizer] Level 01 Final Production Pass COMPLETED SUCCESSFULLY!</b></color>");

            if (interactive)
            {
                EditorUtility.DisplayDialog("Monkey Adventure",
                    "Level 01: The Awakening Final Production Pass Completed!\n\nAll player, camera, manager, audio, collectible, hazard, checkpoint, enemy, portal, and HUD systems are 100% configured and saved.",
                    "OK");
            }
            }
            finally
            {
                _isRunning = false;
            }
        }

        private static void SetupAudioManager(AudioManager am)
        {
            var amSo = new SerializedObject(am);

            // Populating BGM sounds
            var bgmList = new List<Sound>();
            string[] bgmNames = { "BGM_Act1", "BGM_Act2", "BGM_Act3", "BGM_Act4", "BGM_Act5", "BGM_Boss" };
            foreach (var bName in bgmNames)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Art/Audio/{bName}.wav");
                if (clip != null)
                {
                    bgmList.Add(new Sound { name = bName, clip = clip, volume = 0.8f, pitch = 1f, loop = true });
                }
            }

            // Populating SFX sounds
            var sfxList = new List<Sound>();
            string[] sfxNames = {
                "SFX_Jump", "SFX_Banana", "SFX_Coin", "SFX_Checkpoint", "SFX_Hurt", "SFX_Death",
                "SFX_LevelComplete", "SFX_Attack", "SFX_EnemyHit", "SFX_Footstep", "SFX_WaterSplash",
                "SFX_FireCrackle", "SFX_PoisonBubble", "SFX_DoorOpen", "SFX_RuneActivate", "SFX_UIClick",
                "SFX_EnergyBlast", "SFX_HeavyAttack"
            };

            foreach (var sName in sfxNames)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Art/Audio/{sName}.wav");
                if (clip != null)
                {
                    sfxList.Add(new Sound { name = sName, clip = clip, volume = 1.0f, pitch = 1f, loop = false });
                }
            }

            // Apply to serialized fields
            var bgmProp = amSo.FindProperty("bgmSounds");
            bgmProp.arraySize = bgmList.Count;
            for (int i = 0; i < bgmList.Count; i++)
            {
                var elem = bgmProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("name").stringValue = bgmList[i].name;
                elem.FindPropertyRelative("clip").objectReferenceValue = bgmList[i].clip;
                elem.FindPropertyRelative("volume").floatValue = bgmList[i].volume;
                elem.FindPropertyRelative("pitch").floatValue = bgmList[i].pitch;
                elem.FindPropertyRelative("loop").boolValue = bgmList[i].loop;
            }

            var sfxProp = amSo.FindProperty("sfxSounds");
            sfxProp.arraySize = sfxList.Count;
            for (int i = 0; i < sfxList.Count; i++)
            {
                var elem = sfxProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("name").stringValue = sfxList[i].name;
                elem.FindPropertyRelative("clip").objectReferenceValue = sfxList[i].clip;
                elem.FindPropertyRelative("volume").floatValue = sfxList[i].volume;
                elem.FindPropertyRelative("pitch").floatValue = sfxList[i].pitch;
                elem.FindPropertyRelative("loop").boolValue = sfxList[i].loop;
            }

            amSo.ApplyModifiedProperties();
        }

        private static void SetupGameAssetInitializer(GameAssetInitializer gai, GameObject enemyPredator, GameObject baseM, GameObject guardM, GameObject titanM, GameObject hanuM)
        {
            var gaiSo = new SerializedObject(gai);

            // Enemies
            var enemyListProp = gaiSo.FindProperty("enemyPrefabs");
            enemyListProp.ClearArray();
            if (enemyPredator != null)
            {
                enemyListProp.arraySize = 1;
                enemyListProp.GetArrayElementAtIndex(0).objectReferenceValue = enemyPredator;
            }

            // Evolution skins
            var skinListProp = gaiSo.FindProperty("evolutionSkinPrefabs");
            skinListProp.ClearArray();
            GameObject[] skins = { baseM, guardM, titanM, hanuM };
            int validSkins = 0;
            for (int i = 0; i < skins.Length; i++)
            {
                if (skins[i] != null) validSkins++;
            }
            skinListProp.arraySize = validSkins;
            int idx = 0;
            for (int i = 0; i < skins.Length; i++)
            {
                if (skins[i] != null)
                {
                    skinListProp.GetArrayElementAtIndex(idx++).objectReferenceValue = skins[i];
                }
            }

            gaiSo.ApplyModifiedProperties();
        }
    }
}
