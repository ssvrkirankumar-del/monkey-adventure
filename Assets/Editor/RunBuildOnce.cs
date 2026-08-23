using System.IO;
using UnityEditor;
using UnityEngine;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Manual helper for Level 01 build verification.
    /// Automatic domain-reload / startup triggers have been disabled to prevent editor hangs and unexpected builds.
    /// </summary>
    public static class RunBuildOnce
    {
        // Automatic execution on InitializeOnLoad / delayCall is disabled.
        // Builds can only be initiated explicitly via Window > Monkey Adventure menu items.

        public static void ManualBuildCheck()
        {
            string scenePath1 = "Assets/Scenes/Level01_Awakening.unity";
            string scenePath2 = "Assets/Level01_Awakening.unity";

            if (File.Exists(scenePath1) || File.Exists(scenePath2))
            {
                Debug.Log("[RunBuildOnce] Level 01 scene already exists. No automatic rebuild needed.");
                return;
            }

            Debug.Log("[RunBuildOnce] Level 01 scene not found. You can build it from 'Window > Monkey Adventure > Build Level 01 (Playable)'.");
        }
    }
}
