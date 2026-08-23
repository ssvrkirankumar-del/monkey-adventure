
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HDAssetAuditTool : EditorWindow
{
    const string SceneName = "Level01_Awakening";

    [MenuItem("Window/Monkey Adventure/HD Asset Audit")]
    static void Open() => GetWindow<HDAssetAuditTool>("HD Asset Audit");

    [MenuItem("Window/Monkey Adventure/HD Asset Audit/Run Full Audit")]
    static void RunMenu() => RunFullAudit();

    [MenuItem("Window/Monkey Adventure/HD Asset Audit/Backup Current Level 01")]
    static void BackupMenu() => BackupScene(true);

    Vector2 scroll;
    static string report = "";
    static int objects, primitives, pink, models, prefabs, materials, textures, animations, audio;

    void OnGUI()
    {
        EditorGUILayout.LabelField("Monkey Adventure - HD Asset Audit", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "READ-ONLY audit. Creates a timestamped Level 01 backup first, then scans the whole Assets folder. No asset replacement or deletion is performed.",
            MessageType.Info);

        if (GUILayout.Button("BACKUP CURRENT LEVEL 01", GUILayout.Height(30)))
            BackupScene(true);

        if (GUILayout.Button("RUN FULL HD ASSET AUDIT", GUILayout.Height(40)))
            RunFullAudit();

        EditorGUILayout.Space(8);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Last result", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Scene objects scanned: {objects}");
        EditorGUILayout.LabelField($"Primitive placeholders: {primitives}");
        EditorGUILayout.LabelField($"Pink/broken material candidates: {pink}");
        EditorGUILayout.LabelField($"Model files: {models}");
        EditorGUILayout.LabelField($"Prefabs: {prefabs}");
        EditorGUILayout.LabelField($"Materials: {materials}");
        EditorGUILayout.LabelField($"Textures: {textures}");
        EditorGUILayout.LabelField($"Animations: {animations}");
        EditorGUILayout.LabelField($"Audio files: {audio}");
        if (!string.IsNullOrEmpty(report))
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.TextArea(report, GUILayout.MinHeight(180));
        }
        EditorGUILayout.EndScrollView();
    }

    static string FindLevel01()
    {
        foreach (var guid in AssetDatabase.FindAssets("Level01_Awakening t:Scene"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p).Equals(SceneName, StringComparison.OrdinalIgnoreCase))
                return p;
        }
        return "";
    }

    static bool BackupScene(bool dialog)
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (dialog) EditorUtility.DisplayDialog("Backup", "Stop Play Mode first.", "OK");
            return false;
        }

        string scenePath = SceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(scenePath)) scenePath = FindLevel01();

        if (string.IsNullOrEmpty(scenePath))
        {
            if (dialog) EditorUtility.DisplayDialog("Backup", "Level01_Awakening.unity was not found inside Assets.", "OK");
            return false;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty)
        {
            if (!EditorUtility.DisplayDialog(
                "Unsaved Changes",
                "Level 01 has unsaved changes. Save before creating the backup?",
                "Save & Backup", "Cancel"))
                return false;

            EditorSceneManager.SaveScene(scene);
        }

        string folder = "Assets/Backups/Scenes";
        EnsureFolder(folder);

        string name = Path.GetFileNameWithoutExtension(scenePath);
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupAsset = $"{folder}/{name}_{stamp}.unity";

        File.Copy(Path.GetFullPath(scenePath), Path.GetFullPath(backupAsset), true);
        AssetDatabase.ImportAsset(backupAsset, ImportAssetOptions.ForceUpdate);

        Debug.Log($"[HDAssetAudit] BACKUP CREATED: {backupAsset}");
        if (dialog) EditorUtility.DisplayDialog("Backup Created", backupAsset, "OK");
        return true;
    }

    static void RunFullAudit()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("HD Asset Audit", "Stop Play Mode before running the audit.", "OK");
            return;
        }

        try
        {
            if (!BackupScene(true)) return;

            string scenePath = FindLevel01();
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("HD Asset Audit", "Level01_Awakening.unity not found.", "OK");
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var primitiveRows = new List<string>();
            var pinkRows = new List<string>();

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    objects++;

                    Mesh mesh = null;
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null) mesh = mf.sharedMesh;
                    var smr = r as SkinnedMeshRenderer;
                    if (smr != null) mesh = smr.sharedMesh;

                    if (mesh != null && IsPrimitive(mesh))
                    {
                        primitives++;
                        primitiveRows.Add($"{HierarchyPath(r.transform)} | {mesh.name} | {Materials(r)}");
                    }

                    if (IsPink(r))
                    {
                        pink++;
                        pinkRows.Add($"{HierarchyPath(r.transform)} | {mesh?.name} | {Materials(r)}");
                    }
                }
            }

            var all = AssetDatabase.FindAssets("", new[] { "Assets" });
            var modelList = new List<string>();
            var prefabList = new List<string>();
            var materialList = new List<string>();
            var textureList = new List<string>();
            var animationList = new List<string>();
            var audioList = new List<string>();

            for (int i = 0; i < all.Length; i++)
            {
                if (i % 50 == 0)
                    EditorUtility.DisplayProgressBar("HD Asset Audit", $"Scanning Assets... {i}/{all.Length}", (float)i / Math.Max(1, all.Length));

                string p = AssetDatabase.GUIDToAssetPath(all[i]);
                string e = Path.GetExtension(p).ToLowerInvariant();

                if (new[] { ".fbx", ".obj", ".gltf", ".glb", ".dae", ".blend" }.Contains(e)) modelList.Add(p);
                else if (e == ".prefab") prefabList.Add(p);
                else if (e == ".mat") materialList.Add(p);
                else if (new[] { ".png", ".jpg", ".jpeg", ".tga", ".tif", ".tiff", ".exr", ".psd", ".hdr" }.Contains(e)) textureList.Add(p);
                else if (e == ".anim" || e == ".controller") animationList.Add(p);
                else if (new[] { ".wav", ".mp3", ".ogg", ".aiff" }.Contains(e)) audioList.Add(p);
            }

            models = modelList.Count; prefabs = prefabList.Count; materials = materialList.Count;
            textures = textureList.Count; animations = animationList.Count; audio = audioList.Count;

            string folder = "Assets/Documentation/HDAssetAudit";
            EnsureFolder(folder);
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string txt = $"{folder}/HDAssetAudit_{stamp}.txt";
            string csv = $"{folder}/HDAssetAudit_{stamp}.csv";

            var sb = new StringBuilder();
            sb.AppendLine("MONKEY ADVENTURE - HD ASSET AUDIT");
            sb.AppendLine($"Generated: {DateTime.Now:O}");
            sb.AppendLine($"Scene: {scenePath}");
            sb.AppendLine();
            sb.AppendLine($"Scene renderer objects scanned: {objects}");
            sb.AppendLine($"Primitive placeholders: {primitives}");
            sb.AppendLine($"Pink/broken material candidates: {pink}");
            sb.AppendLine($"Model files: {models}");
            sb.AppendLine($"Prefabs: {prefabs}");
            sb.AppendLine($"Materials: {materials}");
            sb.AppendLine($"Textures: {textures}");
            sb.AppendLine($"Animations/controllers: {animations}");
            sb.AppendLine($"Audio files: {audio}");
            sb.AppendLine();
            sb.AppendLine("=== PRIMITIVE PLACEHOLDERS ===");
            foreach (var x in primitiveRows) sb.AppendLine(x);
            sb.AppendLine();
            sb.AppendLine("=== PINK/BROKEN MATERIAL CANDIDATES ===");
            foreach (var x in pinkRows) sb.AppendLine(x);
            sb.AppendLine();
            sb.AppendLine("=== MODELS ===");
            foreach (var x in modelList) sb.AppendLine(x);
            sb.AppendLine();
            sb.AppendLine("=== PREFABS ===");
            foreach (var x in prefabList) sb.AppendLine(x);
            sb.AppendLine();
            sb.AppendLine("=== MATERIALS ===");
            foreach (var x in materialList) sb.AppendLine(x);
            sb.AppendLine();
            sb.AppendLine("=== TEXTURES ===");
            foreach (var x in textureList) sb.AppendLine(x);
            sb.AppendLine();
            sb.AppendLine("=== ANIMATIONS / CONTROLLERS ===");
            foreach (var x in animationList) sb.AppendLine(x);
            sb.AppendLine();
            sb.AppendLine("=== AUDIO ===");
            foreach (var x in audioList) sb.AppendLine(x);

            File.WriteAllText(Path.GetFullPath(txt), sb.ToString(), Encoding.UTF8);

            var csvText = new StringBuilder("SECTION,TYPE,PATH,DETAIL\n");
            foreach (var x in primitiveRows) csvText.AppendLine($"SCENE,PRIMITIVE,\"{Csv(x)}\",\"\"");
            foreach (var x in pinkRows) csvText.AppendLine($"SCENE,PINK_MATERIAL,\"{Csv(x)}\",\"\"");
            foreach (var x in modelList) csvText.AppendLine($"ASSET,MODEL,\"{Csv(x)}\",\"\"");
            foreach (var x in prefabList) csvText.AppendLine($"ASSET,PREFAB,\"{Csv(x)}\",\"\"");
            foreach (var x in materialList) csvText.AppendLine($"ASSET,MATERIAL,\"{Csv(x)}\",\"\"");
            foreach (var x in textureList) csvText.AppendLine($"ASSET,TEXTURE,\"{Csv(x)}\",\"\"");
            foreach (var x in animationList) csvText.AppendLine($"ASSET,ANIMATION,\"{Csv(x)}\",\"\"");
            foreach (var x in audioList) csvText.AppendLine($"ASSET,AUDIO,\"{Csv(x)}\",\"\"");
            File.WriteAllText(Path.GetFullPath(csv), csvText.ToString(), Encoding.UTF8);

            AssetDatabase.Refresh();

            GetWindow<HDAssetAuditTool>("HD Asset Audit").Repaint();
            report = $"BACKUP: {LatestBackup(scenePath)}\n\nTEXT REPORT: {txt}\nCSV REPORT: {csv}\n\nNO ASSET WAS REPLACED OR DELETED.";

            Debug.Log($"[HDAssetAudit] COMPLETE\n{report}");
            EditorUtility.DisplayDialog(
                "HD Asset Audit Complete",
                $"Backup created.\n\nScene objects: {objects}\nPrimitive placeholders: {primitives}\nPink/broken candidates: {pink}\nModels found: {models}\n\nReports saved in:\n{folder}",
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError("[HDAssetAudit] " + ex);
            EditorUtility.DisplayDialog("HD Asset Audit Failed", ex.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static bool IsPrimitive(Mesh m)
    {
        string n = m.name.Trim().ToLowerInvariant();
        return n == "cube" || n == "sphere" || n == "capsule" || n == "cylinder" || n == "plane" || n == "quad";
    }

    static bool IsPink(Renderer r)
    {
        foreach (var m in r.sharedMaterials)
        {
            if (m == null || m.shader == null) return true;
            Color c = m.color;
            if (c.r > .85f && c.b > .85f && c.g < .25f) return true;
            if (m.shader.name.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    static string Materials(Renderer r) =>
        string.Join(" | ", r.sharedMaterials.Where(x => x != null).Select(x => x.name));

    static string HierarchyPath(Transform t)
    {
        var p = new List<string>();
        while (t != null) { p.Add(t.name); t = t.parent; }
        p.Reverse();
        return string.Join("/", p);
    }

    static string LatestBackup(string scenePath)
    {
        string dir = Path.Combine(Application.dataPath, "Backups/Scenes");
        if (!Directory.Exists(dir)) return "(none)";
        string baseName = Path.GetFileNameWithoutExtension(scenePath);
        var files = Directory.GetFiles(dir, baseName + "_*.unity");
        if (files.Length == 0) return "(none)";
        return files.OrderByDescending(File.GetLastWriteTimeUtc).First().Replace(Application.dataPath, "Assets").Replace('\\', '/');
    }

    static string Csv(string x) => (x ?? "").Replace("\"", "\"\"");

    static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
