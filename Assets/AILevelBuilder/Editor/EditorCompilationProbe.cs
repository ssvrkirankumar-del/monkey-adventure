using UnityEditor;
using UnityEngine;

namespace MonkeyAdventure.AILevelBuilder.Editor
{
    public static class EditorCompilationProbe
    {
        // Passive verification menu item only - does NOT run on load.
        [MenuItem("Window/Monkey Adventure/Developer/Verify Recovery Compilation")]
        public static void VerifyCompilation()
        {
            Debug.Log("[EditorCompilationProbe] Code compilation verified successfully.");
        }
    }
}
