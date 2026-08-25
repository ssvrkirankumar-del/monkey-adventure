using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MonkeyAdventure.AILevelBuilder.Editor;

namespace MonkeyAdventure.Editor
{
    /// <summary>
    /// Level 01 Mountain and Cliff Perimeter Tool.
    /// Runs master visual QA and verifies perimeter mountain enclosure.
    /// </summary>
    [InitializeOnLoad]
    public static class Level01MountainEnclosure
    {
        private const string SCENE_PATH = "Assets/Scenes/Level01_Awakening.unity";

        static Level01MountainEnclosure()
        {
            EditorApplication.delayCall += RunQAPass;
        }

        [MenuItem("Monkey Adventure/Run Master Visual QA", false, 30)]
        public static void RunQAPass()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != SCENE_PATH)
            {
                activeScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            }

            HDEnvironmentMasterVisualQA.RunMasterQAHeadless();
        }
    }
}
