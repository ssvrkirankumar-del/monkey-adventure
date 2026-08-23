using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonkeyAdventure.EditorTools
{
    public class HDAssetMatchPlanner : EditorWindow
    {
        const string Folder = "Assets/HDAssetAudit";

        [MenuItem("Window/Monkey Adventure/HD Asset Match Planner")]
        static void Open() => GetWindow<HDAssetMatchPlanner>("HD Asset Match Planner");

        bool includeInactive = true;
        int maxCandidates = 5;

        void OnGUI()
        {
            EditorGUILayout.LabelField("HD Asset Match & Replacement Planner",
                EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "NON-DESTRUCTIVE: scans the active scene and existing Assets only. " +
                "It does NOT replace, delete, move, or save scene objects.",
                MessageType.Info);

            includeInactive = EditorGUILayout.ToggleLeft("Include inactive objects", includeInactive);
            maxCandidates = EditorGUILayout.IntSlider("Candidates per object", maxCandidates, 1, 10);

            if (GUILayout.Button("SCAN & GENERATE MATCH REPORT", GUILayout.Height(35)))
                Generate();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Reports:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Folder + "/HD_Asset_Match_Report.csv");
            EditorGUILayout.LabelField(Folder + "/HD_Asset_Match_Report.md");
        }

        class Target
        {
            public GameObject go;
            public string primitive, category, material, path;
        }

        class Candidate
        {
            public string path, kind, reason;
            public int score;
        }

        void Generate()
        {
            EnsureFolder();

            var targets = FindTargets();
            var assets = AssetDatabase.FindAssets("t:Prefab t:Model", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Where(p => new[] { ".prefab", ".fbx", ".obj", ".gltf", ".glb", ".blend" }
                    .Contains(Path.GetExtension(p).ToLowerInvariant()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var all = new List<(Target t, List<Candidate> c)>();
            foreach (var t in targets)
            {
                var cs = assets.Select(p => Score(t, p))
                    .Where(x => x != null && x.score > 0)
                    .OrderByDescending(x => x.score)
                    .ThenBy(x => x.path)
                    .Take(maxCandidates)
                    .ToList();
                all.Add((t, cs));
            }

            string csv = Path.Combine(Application.dataPath, "HDAssetAudit/HD_Asset_Match_Report.csv");
            string md = Path.Combine(Application.dataPath, "HDAssetAudit/HD_Asset_Match_Report.md");

            File.WriteAllText(csv, MakeCsv(all), Encoding.UTF8);
            File.WriteAllText(md, MakeMd(all, assets.Count), Encoding.UTF8);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("HD Asset Match Planner",
                $"Done.\n\nPrimitive targets: {targets.Count}\nAssets scanned: {assets.Count}\n\n" +
                "No scene changes were made.\n\nReports saved under Assets/HDAssetAudit.",
                "OK");
        }

        List<Target> FindTargets()
        {
            var result = new List<Target>();

            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || EditorUtility.IsPersistent(go)) continue;
                if (go.scene != SceneManager.GetActiveScene()) continue;
                if (!includeInactive && !go.activeInHierarchy) continue;

                var mf = go.GetComponent<MeshFilter>();
                var mr = go.GetComponent<MeshRenderer>();
                if (mf == null || mr == null || mf.sharedMesh == null) continue;

                string primitive = PrimitiveName(mf.sharedMesh.name);
                if (primitive == null) continue;

                string path = Hierarchy(go.transform);
                string s = (path + "/" + go.name).ToLowerInvariant();

                result.Add(new Target
                {
                    go = go,
                    path = path,
                    primitive = primitive,
                    category = Category(s),
                    material = mr.sharedMaterial ? mr.sharedMaterial.name : ""
                });
            }

            return result.OrderBy(x => x.path).ToList();
        }

        Candidate Score(Target t, string path)
        {
            string n = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            string p = path.ToLowerInvariant();
            string s = (t.go.name + " " + t.path + " " + t.material).ToLowerInvariant();

            int score = 0;
            var reasons = new List<string>();

            var words = new[]
            {
                "tree","palm","canopy","fern","grass","bush","shrub","flower","mushroom",
                "rock","stone","boulder","cliff","ruin","ancient","door","rune","pedestal",
                "monkey","guardian","predator","enemy","boar","deer","parrot","frog","butterfly",
                "banana","coin","relic","vine","log","checkpoint","portal"
            };

            foreach (var w in words)
            {
                if (s.Contains(w) && (n.Contains(w) || p.Contains("/" + w) || p.Contains(w)))
                {
                    score += 25;
                    reasons.Add("semantic:" + w);
                }
            }

            if (t.category == "tree" && (n.Contains("tree") || n.Contains("palm")))
            { score += 30; reasons.Add("tree-category"); }

            if (t.category == "foliage" &&
                new[] {"leaf","foliage","grass","fern","bush","shrub","plant","flower","mushroom"}
                .Any(x => n.Contains(x)))
            { score += 30; reasons.Add("foliage-category"); }

            if (t.category == "rock" &&
                new[] {"rock","stone","boulder","cliff"}
                .Any(x => n.Contains(x)))
            { score += 30; reasons.Add("rock-category"); }

            if ((t.category == "character" || t.category == "enemy") &&
                new[] {"monkey","guardian","predator","enemy","boar","deer","parrot","frog","butterfly"}
                .Any(x => n.Contains(x)))
            { score += 35; reasons.Add("character-category"); }

            if (p.StartsWith("assets/art/"))
            { score += 10; reasons.Add("project-art"); }

            if (p.Contains("high quality"))
            { score += 20; reasons.Add("high-quality"); }

            if (p.Contains("low poly") || p.Contains("lowpoly") || p.Contains("/mobile/"))
            { score -= 50; reasons.Add("low-poly/mobile penalty"); }

            if (score <= 0) return null;

            return new Candidate
            {
                path = path,
                kind = path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ? "Prefab" : "Model",
                score = score,
                reason = string.Join("; ", reasons.Distinct())
            };
        }

        static string Category(string s)
        {
            if (new[] {"predator","enemy","boar","reptile","guard"}.Any(s.Contains)) return "enemy";
            if (new[] {"monkey","deer","parrot","frog","butterfly","guardian"}.Any(s.Contains)) return "character";
            if (new[] {"tree","palm","canopy"}.Any(s.Contains)) return "tree";
            if (new[] {"fern","grass","bush","shrub","flower","mushroom","plant","foliage"}.Any(s.Contains)) return "foliage";
            if (new[] {"rock","boulder","cliff","stone"}.Any(s.Contains)) return "rock";
            if (new[] {"ruin","arch","door","rune","pedestal"}.Any(s.Contains)) return "ruins";
            if (new[] {"banana","coin","relic","vine","log","checkpoint","gateway","portal"}.Any(s.Contains)) return "prop";
            return "environment";
        }

        static string PrimitiveName(string n)
        {
            n = n.ToLowerInvariant();
            if (n.Contains("cube")) return "Cube";
            if (n.Contains("sphere")) return "Sphere";
            if (n.Contains("capsule")) return "Capsule";
            if (n.Contains("cylinder")) return "Cylinder";
            if (n.Contains("plane")) return "Plane";
            if (n.Contains("quad")) return "Quad";
            return null;
        }

        static string Hierarchy(Transform t)
        {
            var a = new Stack<string>();
            while (t) { a.Push(t.name); t = t.parent; }
            return string.Join("/", a);
        }

        static string Q(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        static string MakeCsv(List<(Target t, List<Candidate> c)> rows)
        {
            var b = new StringBuilder();
            b.AppendLine("SceneObject,HierarchyPath,Primitive,Category,Material,Rank,CandidateAsset,Kind,Score,Reason");

            foreach (var r in rows)
            {
                if (r.c.Count == 0)
                {
                    b.AppendLine(string.Join(",", Q(r.t.go.name), Q(r.t.path), Q(r.t.primitive),
                        Q(r.t.category), Q(r.t.material), "", "", "", "", "NO_MATCH"));
                    continue;
                }

                for (int i = 0; i < r.c.Count; i++)
                {
                    var c = r.c[i];
                    b.AppendLine(string.Join(",", Q(r.t.go.name), Q(r.t.path), Q(r.t.primitive),
                        Q(r.t.category), Q(r.t.material), i + 1, Q(c.path), Q(c.kind),
                        c.score, Q(c.reason)));
                }
            }
            return b.ToString();
        }

        static string MakeMd(List<(Target t, List<Candidate> c)> rows, int assetCount)
        {
            var b = new StringBuilder();
            b.AppendLine("# HD Asset Match & Replacement Planner");
            b.AppendLine();
            b.AppendLine("> NON-DESTRUCTIVE. This report does not modify the Unity scene.");
            b.AppendLine();
            b.AppendLine($"- Primitive targets: **{rows.Count}**");
            b.AppendLine($"- Prefab/model assets scanned: **{assetCount}**");
            b.AppendLine();
            b.AppendLine("| # | Scene Object | Primitive | Category | Best Candidate | Score |");
            b.AppendLine("|---:|---|---|---|---|---:|");

            int i = 1;
            foreach (var r in rows)
            {
                string best = r.c.Count > 0 ? r.c[0].path : "NO MATCH";
                int score = r.c.Count > 0 ? r.c[0].score : 0;
                b.AppendLine($"| {i++} | `{r.t.go.name}` | `{r.t.primitive}` | `{r.t.category}` | `{best}` | {score} |");
            }

            b.AppendLine();
            b.AppendLine("## Ranking");
            b.AppendLine("Semantic match > project Art assets > High Quality assets. Low-poly/mobile assets are penalized.");
            b.AppendLine();
            b.AppendLine("## Next stage");
            b.AppendLine("Review this report before any replacement automation. Replacement must preserve gameplay components, colliders, transforms, Animator bindings, and material/URP compatibility.");
            return b.ToString();
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets", "HDAssetAudit");
        }
    }
}
