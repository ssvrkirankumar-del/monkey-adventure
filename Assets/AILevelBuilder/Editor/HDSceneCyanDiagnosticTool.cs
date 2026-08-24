using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class HDSceneCyanDiagnosticTool : EditorWindow
{
    private class DiagnosticItem
    {
        public Renderer renderer;
        public int slot;
        public Material material;
        public string reason;
        public float cyanScore;
        public bool hasNavMeshSurface;
        public string navMeshInfo;
    }

    private readonly List<DiagnosticItem> _items = new List<DiagnosticItem>();
    private Vector2 _scroll;
    private string _status = "Ready. Run Scan.";
    private int _rendererCount;
    private int _materialSlotCount;
    private int _cyanCount;
    private int _navMeshSurfaceCount;

    [MenuItem("Window/Monkey Adventure/HD Scene Cyan Diagnostic", priority = 250)]
    public static void Open()
    {
        var window = GetWindow<HDSceneCyanDiagnosticTool>("HD Scene Cyan Diagnostic");
        window.minSize = new Vector2(900f, 600f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("HD SCENE CYAN DIAGNOSTIC", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Diagnoses visible cyan/blue surfaces without changing any scene materials. " +
            "It reports renderer, material slot, shader, BaseMap/BaseColor, hierarchy and possible NavMeshSurface components.",
            MessageType.Info);

        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("SCAN CURRENT SCENE", GUILayout.Height(34)))
                Scan();

            if (GUILayout.Button("CLEAR", GUILayout.Width(120), GUILayout.Height(34)))
            {
                _items.Clear();
                _status = "Cleared.";
            }

            if (GUILayout.Button("COPY ALL", GUILayout.Width(120), GUILayout.Height(34)))
                CopyAll();
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(
            $"Status: {_status}    Renderers: {_rendererCount}    Material Slots: {_materialSlotCount}    " +
            $"Cyan/Blue Candidates: {_cyanCount}    NavMeshSurface: {_navMeshSurfaceCount}",
            EditorStyles.helpBox);

        EditorGUILayout.Space(5);

        if (_items.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No cyan/blue material candidates detected yet. Click SCAN CURRENT SCENE.",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("CYAN / BLUE CANDIDATES", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        for (int i = 0; i < _items.Count; i++)
        {
            DrawItem(_items[i], i);
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();
    }

    private void Scan()
    {
        _items.Clear();
        _rendererCount = 0;
        _materialSlotCount = 0;
        _cyanCount = 0;
        _navMeshSurfaceCount = 0;

        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include);

        _rendererCount = renderers.Length;

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;

            Material[] materials = r.sharedMaterials;
            _materialSlotCount += materials.Length;

            bool rendererHasNavMeshSurface = HasNavMeshSurfaceOnObject(r.gameObject, out string navInfo);
            if (rendererHasNavMeshSurface)
                _navMeshSurfaceCount++;

            for (int slot = 0; slot < materials.Length; slot++)
            {
                Material mat = materials[slot];
                if (mat == null)
                    continue;

                float score = CalculateCyanScore(mat, r);
                if (score < 0.45f)
                    continue;

                string reason = BuildReason(mat, r, score);

                _items.Add(new DiagnosticItem
                {
                    renderer = r,
                    slot = slot,
                    material = mat,
                    reason = reason,
                    cyanScore = score,
                    hasNavMeshSurface = rendererHasNavMeshSurface,
                    navMeshInfo = navInfo
                });

                _cyanCount++;
            }
        }

        _items.Sort((a, b) => b.cyanScore.CompareTo(a.cyanScore));

        _status = _cyanCount == 0
            ? "Scan complete. No cyan/blue material candidates found."
            : $"Scan complete. {_cyanCount} cyan/blue material candidates found.";

        Repaint();
    }

    private void DrawItem(DiagnosticItem item, int index)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            string objectName = item.renderer != null ? item.renderer.gameObject.name : "<Missing Renderer>";
            string materialName = item.material != null ? item.material.name : "<Missing Material>";

            EditorGUILayout.LabelField(
                $"[{index + 1}] {objectName} [Slot {item.slot}]  |  Cyan Score: {item.cyanScore:P0}",
                EditorStyles.boldLabel);

            if (item.renderer != null)
            {
                EditorGUILayout.LabelField("Hierarchy", GetHierarchyPath(item.renderer.transform));
                EditorGUILayout.LabelField("Renderer", item.renderer.GetType().Name);
            }

            EditorGUILayout.LabelField("Material", materialName);

            if (item.material != null)
            {
                EditorGUILayout.LabelField("Shader",
                    item.material.shader != null ? item.material.shader.name : "<None>");

                Texture baseMap = GetBaseMap(item.material);
                EditorGUILayout.LabelField("BaseMap",
                    baseMap != null ? baseMap.name : "<None>");

                Color baseColor = GetBaseColor(item.material);
                EditorGUILayout.LabelField("BaseColor",
                    $"R {baseColor.r:F2}  G {baseColor.g:F2}  B {baseColor.b:F2}  A {baseColor.a:F2}");
            }

            EditorGUILayout.LabelField("Why flagged", item.reason);

            if (item.hasNavMeshSurface)
            {
                EditorGUILayout.HelpBox(
                    "Possible NavMeshSurface component detected on this GameObject. " +
                    "This does NOT prove the cyan surface is NavMesh visualization.",
                    MessageType.Warning);

                EditorGUILayout.LabelField("NavMesh info", item.navMeshInfo);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (item.renderer != null && GUILayout.Button("SELECT OBJECT"))
                {
                    Selection.activeGameObject = item.renderer.gameObject;
                    EditorGUIUtility.PingObject(item.renderer.gameObject);
                }

                if (item.material != null && GUILayout.Button("SELECT MATERIAL"))
                {
                    Selection.activeObject = item.material;
                    EditorGUIUtility.PingObject(item.material);
                }

                if (item.renderer != null && GUILayout.Button("FOCUS SCENE"))
                {
                    Selection.activeGameObject = item.renderer.gameObject;
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
            }
        }
    }

    private static float CalculateCyanScore(Material mat, Renderer renderer)
    {
        float score = 0f;

        Color c = GetBaseColor(mat);

        // Strong cyan/blue material-color detection.
        float max = Mathf.Max(c.r, c.g, c.b);
        float min = Mathf.Min(c.r, c.g, c.b);

        if (c.b > c.r * 1.35f && c.g > c.r * 1.20f && c.b > 0.35f)
            score += 0.55f;

        if (c.g > 0.45f && c.b > 0.45f && Mathf.Abs(c.g - c.b) < 0.35f)
            score += 0.20f;

        // Shader/material naming hints are secondary evidence only.
        string matName = mat.name.ToLowerInvariant();
        string shaderName = mat.shader != null ? mat.shader.name.ToLowerInvariant() : "";

        if (ContainsAny(matName, "cyan", "blue", "water", "navmesh"))
            score += 0.12f;

        if (ContainsAny(shaderName, "water", "navmesh"))
            score += 0.08f;

        // Avoid flagging ordinary dark/natural green materials.
        if (max < 0.30f)
            score *= 0.5f;

        return Mathf.Clamp01(score);
    }

    private static string BuildReason(Material mat, Renderer renderer, float score)
    {
        List<string> reasons = new List<string>();
        Color c = GetBaseColor(mat);

        if (c.b > c.r * 1.35f && c.g > c.r * 1.20f)
            reasons.Add("BaseColor is strongly cyan/blue");

        if (ContainsAny(mat.name.ToLowerInvariant(), "cyan", "blue", "water", "navmesh"))
            reasons.Add("material name suggests blue/water/NavMesh");

        if (mat.shader != null &&
            ContainsAny(mat.shader.name.ToLowerInvariant(), "water", "navmesh"))
            reasons.Add("shader name suggests water/NavMesh");

        Texture baseMap = GetBaseMap(mat);
        if (baseMap == null)
            reasons.Add("no BaseMap texture");

        if (reasons.Count == 0)
            reasons.Add("color/shader evidence");

        return string.Join(", ", reasons);
    }

    private static Color GetBaseColor(Material mat)
    {
        if (mat == null)
            return Color.white;

        string[] names =
        {
            "_BaseColor",
            "_Color",
            "_BaseColorHDR",
            "_ColorHDR"
        };

        foreach (string property in names)
        {
            if (mat.HasProperty(property))
                return mat.GetColor(property);
        }

        return Color.white;
    }

    private static Texture GetBaseMap(Material mat)
    {
        if (mat == null)
            return null;

        string[] names =
        {
            "_BaseMap",
            "_BaseMapTexture",
            "_MainTex",
            "_Albedo"
        };

        foreach (string property in names)
        {
            if (mat.HasProperty(property))
                return mat.GetTexture(property);
        }

        return null;
    }

    private static bool HasNavMeshSurfaceOnObject(GameObject go, out string info)
    {
        info = "";

        // Reflection avoids a hard assembly dependency on AI Navigation.
        Component[] components = go.GetComponents<Component>();

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            Type type = component.GetType();
            string fullName = type.FullName ?? type.Name;

            if (fullName.IndexOf("NavMeshSurface", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                info = fullName;
                return true;
            }
        }

        return false;
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (t == null)
            return "<None>";

        string path = t.name;
        Transform current = t.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private static bool ContainsAny(string value, params string[] terms)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (string term in terms)
        {
            if (value.Contains(term))
                return true;
        }

        return false;
    }

    private void CopyAll()
    {
        if (_items.Count == 0)
        {
            EditorGUIUtility.systemCopyBuffer = "HD Scene Cyan Diagnostic: no candidates.";
            _status = "Nothing to copy.";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine("HD SCENE CYAN DIAGNOSTIC");
        sb.AppendLine($"Renderers: {_rendererCount}");
        sb.AppendLine($"Material Slots: {_materialSlotCount}");
        sb.AppendLine($"Cyan/Blue Candidates: {_cyanCount}");
        sb.AppendLine($"Possible NavMeshSurface Objects: {_navMeshSurfaceCount}");
        sb.AppendLine();

        foreach (DiagnosticItem item in _items)
        {
            sb.AppendLine($"[{item.cyanScore:P0}] {item.renderer.gameObject.name} [Slot {item.slot}]");
            sb.AppendLine($"Hierarchy: {GetHierarchyPath(item.renderer.transform)}");
            sb.AppendLine($"Renderer: {item.renderer.GetType().Name}");
            sb.AppendLine($"Material: {item.material.name}");
            sb.AppendLine($"Shader: {(item.material.shader != null ? item.material.shader.name : "<None>")}");
            sb.AppendLine($"BaseMap: {(GetBaseMap(item.material) != null ? GetBaseMap(item.material).name : "<None>")}");
            sb.AppendLine($"BaseColor: {GetBaseColor(item.material)}");
            sb.AppendLine($"Reason: {item.reason}");
            sb.AppendLine($"NavMeshSurface on object: {item.hasNavMeshSurface}");
            if (item.hasNavMeshSurface)
                sb.AppendLine($"NavMesh info: {item.navMeshInfo}");
            sb.AppendLine();
        }

        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        _status = "Full diagnostic copied to clipboard.";
    }
}
