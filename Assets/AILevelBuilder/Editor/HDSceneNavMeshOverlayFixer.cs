using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class HDSceneNavMeshOverlayFixer
{
    private const string MenuRoot = "Window/Monkey Adventure/HD Scene/";

    [MenuItem(MenuRoot + "Hide NavMesh Visualization", priority = 240)]
    public static void HideNavMeshVisualization()
    {
        if (SetLegacyShowNavigation(0))
        {
            SceneView.RepaintAll();
            Debug.Log("[HD Scene] NavMesh visualization hidden. No scene materials were changed.");
        }
        else
        {
            Debug.LogWarning("[HD Scene] Could not access the NavMesh visualization state. If the blue/cyan overlay remains, close the Navigation/AI Navigation debug view in the Scene view.");
        }
    }

    [MenuItem(MenuRoot + "Show NavMesh Visualization", priority = 241)]
    public static void ShowNavMeshVisualization()
    {
        if (SetLegacyShowNavigation(1))
        {
            SceneView.RepaintAll();
            Debug.Log("[HD Scene] NavMesh visualization shown.");
        }
        else
        {
            Debug.LogWarning("[HD Scene] NavMesh visualization API was not found.");
        }
    }

    [MenuItem(MenuRoot + "Diagnose NavMesh Overlay", priority = 242)]
    public static void DiagnoseNavMeshOverlay()
    {
        Type t = FindType("UnityEditor.AI.NavMeshVisualizationSettings");
        if (t == null)
        {
            Debug.LogWarning("[HD Scene] UnityEditor.AI.NavMeshVisualizationSettings was not found.");
            return;
        }

        FieldInfo f = t.GetField(
            "showNavigation",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        if (f == null)
        {
            Debug.LogWarning("[HD Scene] NavMeshVisualizationSettings.showNavigation was not found.");
            return;
        }

        object value = f.GetValue(null);
        Debug.Log($"[HD Scene] NavMesh visualization request count = {value}. " +
                  "A positive value means Unity is being asked to display NavMesh debug graphics.");
    }

    private static bool SetLegacyShowNavigation(int value)
    {
        try
        {
            Type t = FindType("UnityEditor.AI.NavMeshVisualizationSettings");
            if (t == null)
                return false;

            FieldInfo f = t.GetField(
                "showNavigation",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (f == null || !f.FieldType.IsAssignableFrom(typeof(int)))
                return false;

            f.SetValue(null, value);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[HD Scene] Failed to change NavMesh visualization: " + ex.Message);
            return false;
        }
    }

    private static Type FindType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null)
                    return type;
            }
            catch
            {
                // Ignore assemblies that cannot be inspected.
            }
        }

        return null;
    }
}
