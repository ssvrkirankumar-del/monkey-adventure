using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class B3Validator
{
    [MenuItem("Tools/B3 Validator/Validate Selected B3")]
    public static void ValidateSelectedB3()
    {
        GameObject root = Selection.activeGameObject;

        if (root == null)
        {
            Debug.LogError("[B3 Validator] Select 'Monkey_B3 (1)' in the Hierarchy first.");
            return;
        }

        Debug.Log("========== B3 VALIDATION START ==========");

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        SkinnedMeshRenderer[] skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        Animator animator = root.GetComponentInChildren<Animator>(true);

        bool bodyFound = false, eyeFound = false;
        bool bodyRenderer = false, eyeRenderer = false;
        bool bodyMaterial = false, eyeMaterial = false;
        bool bodyTexture = false, eyeTexture = false;

        foreach (Renderer r in renderers)
        {
            string n = r.gameObject.name.ToLowerInvariant();

            if (n.Contains("body"))
            {
                bodyFound = true;
                bodyRenderer = r is SkinnedMeshRenderer;
                if (r.sharedMaterial != null)
                {
                    bodyMaterial = true;
                    bodyTexture = HasTexture(r.sharedMaterial);
                }
            }

            if (n.Contains("eye"))
            {
                eyeFound = true;
                eyeRenderer = r is SkinnedMeshRenderer || r is MeshRenderer;
                if (r.sharedMaterial != null)
                {
                    eyeMaterial = true;
                    eyeTexture = HasTexture(r.sharedMaterial);
                }
            }
        }

        bool bonesFound = skinned.Length > 0;
        bool animationsFound = false;

        if (animator != null && animator.runtimeAnimatorController != null)
            animationsFound = animator.runtimeAnimatorController.animationClips.Length > 0;

        LogCheck("Body object", bodyFound);
        LogCheck("Body Skinned Mesh Renderer", bodyRenderer);
        LogCheck("Body material", bodyMaterial);
        LogCheck("Body texture", bodyTexture);

        LogCheck("Eye object", eyeFound);
        LogCheck("Eye renderer", eyeRenderer);
        LogCheck("Eye material", eyeMaterial);
        LogCheck("Eye texture", eyeTexture);

        LogCheck("Skinned mesh / bones", bonesFound);
        LogCheck("Animator component", animator != null);
        LogCheck("Animation clips", animationsFound);

        Debug.Log("[B3] Renderers found: " + renderers.Length);
        Debug.Log("[B3] Skinned Mesh Renderers found: " + skinned.Length);

        foreach (Renderer r in renderers)
        {
            Debug.Log("[B3] Renderer: " + GetRelativePath(root.transform, r.transform) +
                      " | Material: " + (r.sharedMaterial != null ? r.sharedMaterial.name : "NONE"));
        }

        bool ready = bodyFound && bodyRenderer && bodyMaterial &&
                     eyeFound && eyeRenderer && eyeMaterial &&
                     bonesFound && animator != null && animationsFound;

        if (ready)
            Debug.Log("<color=green>========== B3 READY: YES ==========</color>");
        else
            Debug.LogWarning("========== B3 READY: NOT YET - CHECK FAILED ITEMS ABOVE ==========");

        Debug.Log("========== B3 VALIDATION END ==========");
    }

    private static bool HasTexture(Material material)
    {
        if (material == null) return false;

        string[] properties = { "_BaseMap", "_MainTex", "_BaseColorMap", "_AlbedoMap" };

        foreach (string property in properties)
        {
            if (material.HasProperty(property) && material.GetTexture(property) != null)
                return true;
        }

        return false;
    }

    private static void LogCheck(string label, bool ok)
    {
        if (ok)
            Debug.Log("[B3] ✓ " + label);
        else
            Debug.LogWarning("[B3] ✗ " + label);
    }

    private static string GetRelativePath(Transform root, Transform target)
    {
        List<string> parts = new List<string>();
        Transform current = target;

        while (current != null && current != root)
        {
            parts.Add(current.name);
            current = current.parent;
        }

        parts.Add(root.name);
        parts.Reverse();
        return string.Join("/", parts);
    }

    [MenuItem("Tools/B3 Validator/Validate Selected B3", true)]
    private static bool ValidateSelectedB3Enabled()
    {
        return Selection.activeGameObject != null;
    }
}
