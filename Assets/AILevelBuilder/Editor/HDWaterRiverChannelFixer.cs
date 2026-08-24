using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HDWaterRiverChannelFixer
{
    [MenuItem("Window/Monkey Adventure/HD Scene/Apply HD Water to River Channel", priority = 230)]
    public static void Apply()
    {
        GameObject target = FindRiverChannel();
        if (target == null)
        {
            EditorUtility.DisplayDialog("HD Water Fix",
                "Water_RiverChannel was not found in the currently loaded scenes.", "OK");
            return;
        }

        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            EditorUtility.DisplayDialog("HD Water Fix",
                "Water_RiverChannel does not have a MeshRenderer.", "OK");
            return;
        }

        Material waterMaterial = FindExactMaterial("Water Material");
        if (waterMaterial == null)
        {
            EditorUtility.DisplayDialog("HD Water Fix",
                "Material named 'Water Material' was not found in Assets.", "OK");
            return;
        }

        Undo.RecordObject(renderer, "Apply HD Water Material to River Channel");

        Material[] materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
            materials = new Material[1];

        materials[0] = waterMaterial;
        renderer.sharedMaterials = materials;
        EditorUtility.SetDirty(renderer);

        Debug.Log(
            "[HD Water Fix] Applied 'Water Material' to " +
            GetHierarchyPath(target.transform) +
            ". Source material asset was not modified.");

        SceneView.RepaintAll();

        EditorUtility.DisplayDialog("HD Water Fix — Applied",
            "Water_RiverChannel Slot 0 now uses 'Water Material'.\n\n" +
            "Source material was not modified.\n" +
            "Renderer change is Undo-recorded.", "OK");
    }

    private static GameObject FindRiverChannel()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!string.Equals(t.name, "Water_RiverChannel",
                        StringComparison.OrdinalIgnoreCase))
                        continue;

                    string path = GetHierarchyPath(t);
                    if (path.IndexOf("/Environment/Water/",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                        return t.gameObject;
                }
            }
        }
        return null;
    }

    private static Material FindExactMaterial(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Material " + name))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null &&
                string.Equals(mat.name, name, StringComparison.OrdinalIgnoreCase))
                return mat;
        }
        return null;
    }

    private static string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
