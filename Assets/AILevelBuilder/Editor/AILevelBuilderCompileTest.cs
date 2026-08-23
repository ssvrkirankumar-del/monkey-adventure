using UnityEditor;
using UnityEngine;

public class AILevelBuilderCompileTest : EditorWindow
{
    [MenuItem("Tools/AI Level Builder COMPILE TEST")]
    public static void Open()
    {
        GetWindow<AILevelBuilderCompileTest>("Compile Test");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("AI Level Builder Editor scripting is compiling.");
    }
}
