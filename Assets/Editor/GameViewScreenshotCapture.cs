using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Captures actual Game View screenshots from the gameplay camera perspective
    /// across 4 required viewpoints in Level01_Awakening:
    /// 1. 01_Start_GameView.png (Player starting position)
    /// 2. 02_Forward_GameView.png (Moved forward along trail)
    /// 3. 03_Left_GameView.png (Looking towards left ridge & cliff)
    /// 4. 04_Right_GameView.png (Looking towards right ridge & canopy)
    /// </summary>
    [InitializeOnLoad]
    public static class GameViewScreenshotCapture
    {
        private const string SCENE_PATH = "Assets/Scenes/Level01_Awakening.unity";
        private const string PROOF_DIR = "Assets/Documentation/HDAssetAudit/GameViewProof";

        static GameViewScreenshotCapture()
        {
            EditorApplication.delayCall += EnsureProofDirectory;
        }

        private static void EnsureProofDirectory()
        {
            if (!Directory.Exists(PROOF_DIR))
            {
                Directory.CreateDirectory(PROOF_DIR);
            }
        }

        [MenuItem("Window/Monkey Adventure/📸 Capture Game View Proof Screenshots", false, 145)]
        public static void CaptureAllGameViewScreenshots()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Game View Capture", "Opening Level 01 Scene...", 0.1f);
                EnsureProofDirectory();

                if (SceneManager.GetActiveScene().path != SCENE_PATH)
                {
                    if (File.Exists(SCENE_PATH))
                    {
                        EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
                    }
                }

                // Ensure HD pass is applied to the active scene
                HDSceneDirectInjector.ExecuteFullDirectInjectionAndValidation();

                Camera cam = Camera.main;
                if (cam == null)
                {
                    cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
                }

                if (cam == null)
                {
                    GameObject camObj = new GameObject("Gameplay_Capture_Camera");
                    cam = camObj.AddComponent<Camera>();
                    cam.tag = "MainCamera";
                }

                Vector3 originalPos = cam.transform.position;
                Quaternion originalRot = cam.transform.rotation;
                Transform originalParent = cam.transform.parent;

                // 1. Capture Start Position (Z = 0, facing forward Z+)
                EditorUtility.DisplayProgressBar("Game View Capture", "Capturing 01_Start_GameView.png...", 0.3f);
                cam.transform.SetParent(null);
                cam.transform.position = new Vector3(0f, 2.2f, -3.5f);
                cam.transform.rotation = Quaternion.Euler(14f, 0f, 0f);
                CaptureCameraToPNG(cam, $"{PROOF_DIR}/01_Start_GameView.png", 1920, 1080);

                // 2. Capture Forward Position (Z = 22, looking along path)
                EditorUtility.DisplayProgressBar("Game View Capture", "Capturing 02_Forward_GameView.png...", 0.55f);
                cam.transform.position = new Vector3(0f, 2.4f, 18f);
                cam.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
                CaptureCameraToPNG(cam, $"{PROOF_DIR}/02_Forward_GameView.png", 1920, 1080);

                // 3. Capture Left View (Looking at left ridge canopy and cliffs)
                EditorUtility.DisplayProgressBar("Game View Capture", "Capturing 03_Left_GameView.png...", 0.75f);
                cam.transform.position = new Vector3(0f, 2.2f, 25f);
                cam.transform.rotation = Quaternion.Euler(10f, -55f, 0f);
                CaptureCameraToPNG(cam, $"{PROOF_DIR}/03_Left_GameView.png", 1920, 1080);

                // 4. Capture Right View (Looking at right ridge canopy and understory)
                EditorUtility.DisplayProgressBar("Game View Capture", "Capturing 04_Right_GameView.png...", 0.90f);
                cam.transform.position = new Vector3(0f, 2.2f, 25f);
                cam.transform.rotation = Quaternion.Euler(10f, 55f, 0f);
                CaptureCameraToPNG(cam, $"{PROOF_DIR}/04_Right_GameView.png", 1920, 1080);

                // Restore original camera transform
                cam.transform.SetParent(originalParent);
                cam.transform.position = originalPos;
                cam.transform.rotation = originalRot;

                AssetDatabase.Refresh();
                Debug.Log($"<color=#00FF88><b>[GameViewScreenshotCapture] Successfully captured and saved all 4 Game View proof screenshots to '{PROOF_DIR}/'!</b></color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameViewScreenshotCapture] Exception during screenshot capture: {ex}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void CaptureCameraToPNG(Camera cam, string outputPath, int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4
            };

            RenderTexture prevRT = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(outputPath, bytes);

            cam.targetTexture = prevRT;
            RenderTexture.active = prevActive;

            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);

            Debug.Log($"[GameViewScreenshotCapture] Saved: {outputPath} ({bytes.Length / 1024} KB)");
        }
    }
}
