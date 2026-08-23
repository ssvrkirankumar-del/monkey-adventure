using UnityEngine;
using UnityEditor;

namespace MonkeyAdventure.AILevelBuilder.Editor
{
    [InitializeOnLoad]
    public static class LevelMarkerDiagnostic
    {
        static LevelMarkerDiagnostic()
        {
            EditorApplication.delayCall += RunDiagnostic;
        }

        [MenuItem("Monkey Adventure/Diagnostic/Check LevelMarker")]
        public static void RunDiagnostic()
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/AILevelBuilder/Scripts/LevelMarker.cs");
            if (script == null)
            {
                Debug.LogError("[LevelMarkerDiagnostic] AssetDatabase could not find 'Assets/AILevelBuilder/Scripts/LevelMarker.cs'!");
                return;
            }

            System.Type scriptClass = script.GetClass();
            if (scriptClass == null)
            {
                Debug.LogError($"[LevelMarkerDiagnostic] MonoScript for 'LevelMarker.cs' found, but GetClass() returned NULL! Script text:\n{script.text}");
            }
            else
            {
                Debug.Log($"<color=#00FF88><b>[LevelMarkerDiagnostic] SUCCESS: MonoScript.GetClass() = {scriptClass.FullName}, Assembly = {scriptClass.Assembly.GetName().Name}</b></color>");
            }
        }
    }
}
