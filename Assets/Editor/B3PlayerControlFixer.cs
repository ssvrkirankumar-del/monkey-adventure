using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MonkeyAdventure.Player;
using MonkeyAdventure.Animation;
using MonkeyAdventure.Cameras;
using MonkeyAdventure.Mechanics;
using GuardianSystem.Combat;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Master Player Control Integrator for Level01_Awakening.
    /// Configures 3D Monkey_B3 (1) as the single active playable character with:
    /// - CharacterController & MonkeyPlayerController (WASD/Arrow 3D movement & jump)
    /// - B3_Monkey Animator Controller with Speed and Jump parameters
    /// - MonkeySetupBinder, PlayerHealth, VineClimb
    /// - ThirdPersonCamera target tracking
    /// - Disables obsolete/duplicate 2D player controllers without deleting assets
    /// </summary>
    [InitializeOnLoad]
    public class B3PlayerControlFixer : EditorWindow
    {
        private const string SCENE_PATH = "Assets/Scenes/Level01_Awakening.unity";
        private const string CONTROLLER_PATH = "Assets/Art/player/B3_Monkey/Materials/B3_Monkey.controller";

        static B3PlayerControlFixer()
        {
            EditorApplication.delayCall += AutoFixOnLoad;
        }

        private static void AutoFixOnLoad()
        {
            FixPlayerControlIntegration();
        }

        [MenuItem("Window/Monkey Adventure/🐒 Fix B3 Player Controls (Level 01)", false, 105)]
        public static void FixPlayerControlIntegrationMenuItem()
        {
            FixPlayerControlIntegration();
        }

        public static void FixPlayerControlIntegration()
        {
            try
            {
                if (SceneManager.GetActiveScene().path != SCENE_PATH)
                {
                    if (File.Exists(SCENE_PATH))
                    {
                        EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
                    }
                }

                Debug.Log("========== [B3PlayerControlFixer] Starting Player Control Integration ==========");

                // 1. Find Monkey_B3 (1) or Monkey_B3
                GameObject monkeyB3 = GameObject.Find("Monkey_B3 (1)");
                if (monkeyB3 == null) monkeyB3 = GameObject.Find("Monkey_B3");

                if (monkeyB3 == null)
                {
                    Debug.LogError("[B3PlayerControlFixer] Monkey_B3 (1) not found in scene!");
                    return;
                }

                // 2. Find any old 2D / placeholder player objects that currently have player controllers or Player tag
                var allGameObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
                foreach (var go in allGameObjects)
                {
                    if (go == monkeyB3) continue;

                    // If it's a separate "Player" or legacy 2D monkey object
                    if (go.name.Equals("Player", StringComparison.OrdinalIgnoreCase) || 
                        go.CompareTag("Player") ||
                        (go.name.Contains("Monkey") && !go.name.Contains("B3")))
                    {
                        // Check if it has active player controller components
                        MonkeyPlayerController oldMPC = go.GetComponent<MonkeyPlayerController>();
                        if (oldMPC != null)
                        {
                            Debug.Log($"[B3PlayerControlFixer] Disabling old player controller on '{go.name}'");
                            oldMPC.enabled = false;
                            UnityEngine.Object.DestroyImmediate(oldMPC);
                        }

                        CharacterController oldCC = go.GetComponent<CharacterController>();
                        if (oldCC != null)
                        {
                            oldCC.enabled = false;
                            UnityEngine.Object.DestroyImmediate(oldCC);
                        }

                        MonkeySetupBinder oldBinder = go.GetComponent<MonkeySetupBinder>();
                        if (oldBinder != null)
                        {
                            UnityEngine.Object.DestroyImmediate(oldBinder);
                        }

                        // Ensure old object is untagged so camera and enemies don't target it
                        if (go.CompareTag("Player"))
                        {
                            go.tag = "Untagged";
                        }

                        // Disable old sprite/mesh renderers if it's the old 2D placeholder
                        if (go.name.Equals("Player", StringComparison.OrdinalIgnoreCase))
                        {
                            Renderer[] oldRends = go.GetComponentsInChildren<Renderer>(true);
                            foreach (var r in oldRends)
                            {
                                r.enabled = false;
                            }
                        }
                    }
                }

                // 3. Make Monkey_B3 (1) the active player
                monkeyB3.tag = "Player";
                monkeyB3.SetActive(true);

                // Preserve or set spawn position
                if (monkeyB3.transform.position.y < 0.5f)
                {
                    monkeyB3.transform.position = new Vector3(monkeyB3.transform.position.x, 1.0f, monkeyB3.transform.position.z);
                }

                // Ensure CharacterController
                CharacterController cc = monkeyB3.GetComponent<CharacterController>();
                if (cc == null)
                {
                    cc = monkeyB3.AddComponent<CharacterController>();
                }
                cc.enabled = true;
                cc.height = 1.7f;
                cc.radius = 0.42f;
                cc.center = new Vector3(0f, 0.85f, 0f);
                cc.minMoveDistance = 0.001f;
                cc.stepOffset = 0.35f;
                cc.slopeLimit = 45f;

                // Ensure Animator & B3_Monkey.controller
                Animator anim = monkeyB3.GetComponent<Animator>();
                if (anim == null)
                {
                    anim = monkeyB3.AddComponent<Animator>();
                }
                anim.enabled = true;
                anim.applyRootMotion = false;

                RuntimeAnimatorController b3Controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CONTROLLER_PATH);
                if (b3Controller != null)
                {
                    anim.runtimeAnimatorController = b3Controller;
                }
                else
                {
                    Debug.LogWarning($"[B3PlayerControlFixer] Controller not found at: {CONTROLLER_PATH}");
                }

                // Ensure MonkeyPlayerController
                MonkeyPlayerController mpc = monkeyB3.GetComponent<MonkeyPlayerController>();
                if (mpc == null)
                {
                    mpc = monkeyB3.AddComponent<MonkeyPlayerController>();
                }
                mpc.enabled = true;

                // Find Main Camera
                Camera mainCam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
                if (mainCam != null)
                {
                    SerializedObject mpcSO = new SerializedObject(mpc);
                    mpcSO.FindProperty("cameraTransform").objectReferenceValue = mainCam.transform;
                    mpcSO.FindProperty("moveSpeed").floatValue = 7.0f;
                    mpcSO.FindProperty("jumpHeight").floatValue = 2.2f;
                    mpcSO.FindProperty("gravityMultiplier").floatValue = 2.0f;
                    mpcSO.ApplyModifiedProperties();
                }

                // Ensure MonkeySetupBinder
                MonkeySetupBinder binder = monkeyB3.GetComponent<MonkeySetupBinder>();
                if (binder == null)
                {
                    binder = monkeyB3.AddComponent<MonkeySetupBinder>();
                }
                binder.enabled = true;

                SerializedObject binderSO = new SerializedObject(binder);
                binderSO.FindProperty("animator").objectReferenceValue = anim;
                binderSO.FindProperty("speedParam").stringValue = "Speed";
                binderSO.FindProperty("isGroundedParam").stringValue = "IsGrounded";
                binderSO.FindProperty("jumpTriggerParam").stringValue = "Jump";
                binderSO.FindProperty("attackTriggerParam").stringValue = "Attack";
                binderSO.FindProperty("dieTriggerParam").stringValue = "Die";
                binderSO.ApplyModifiedProperties();

                // Ensure PlayerHealth
                PlayerHealth health = monkeyB3.GetComponent<PlayerHealth>();
                if (health == null)
                {
                    health = monkeyB3.AddComponent<PlayerHealth>();
                }
                health.enabled = true;

                // Ensure VineClimb
                VineClimb vine = monkeyB3.GetComponent<VineClimb>();
                if (vine == null)
                {
                    vine = monkeyB3.AddComponent<VineClimb>();
                }
                vine.enabled = true;

                // 4. Update ThirdPersonCamera Target to follow Monkey_B3 (1)
                if (mainCam != null)
                {
                    ThirdPersonCamera tpCam = mainCam.GetComponent<ThirdPersonCamera>();
                    if (tpCam == null)
                    {
                        tpCam = mainCam.gameObject.AddComponent<ThirdPersonCamera>();
                    }
                    tpCam.Target = monkeyB3.transform;
                    EditorUtility.SetDirty(tpCam);
                }

                // 5. Ensure Rigidbody is NOT attached to player
                Rigidbody rb = monkeyB3.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    UnityEngine.Object.DestroyImmediate(rb);
                }

                EditorUtility.SetDirty(monkeyB3);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

                Debug.Log($"<color=#00FF88><b>[B3PlayerControlFixer] Successfully configured '{monkeyB3.name}' as the active 3D player!</b></color>");
                Debug.Log("Active Player Details: Tag=Player, CharacterController=Active, MonkeyPlayerController=Active, Animator=B3_Monkey, CameraTarget=Assigned");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[B3PlayerControlFixer] Exception during player setup: {ex}");
            }
        }
    }
}
