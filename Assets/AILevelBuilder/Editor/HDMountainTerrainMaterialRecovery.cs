using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonkeyAdventure.AILevelBuilder.Editor
{
    /// <summary>
    /// HD Mountain / Terrain Material Recovery.
    ///
    /// Targets large terrain-like meshes that are visually white/untextured or have
    /// suspiciously flat URP materials. It proposes existing project materials,
    /// but NEVER changes anything until the user explicitly applies a candidate.
    ///
    /// Safety:
    /// - Source materials/prefabs/textures are never modified.
    /// - Renderer sharedMaterials are changed only by explicit Apply actions.
    /// - Every scene change is Undo-recorded.
    /// - Conservative candidate scoring; weak matches are left for review.
    /// </summary>
    public sealed class HDMountainTerrainMaterialRecovery : EditorWindow
    {
        private const string ActiveRoot = "AI_GENERATED_LEVEL/HD_REPLACEMENTS";
        private const string PreviewRoot = "AI_GENERATED_LEVEL/HD_REPLACEMENTS_PREVIEW";
        private const string ReportPath = "Assets/AILevelBuilder/Reports/HDMountainTerrainMaterialRecovery.txt";

        private enum Scope { Preview, Active, Both }
        private enum Status { High, Review, Low, NoCandidate }

        [Serializable]
        private sealed class Candidate
        {
            public Material material;
            public int score;
            public string reason;
        }

        [Serializable]
        private sealed class Item
        {
            public Renderer renderer;
            public int slot;
            public Material current;
            public Material candidate;
            public int score;
            public Status status;
            public string path;
            public string context;
            public string reason;
            public float size;
            public List<Candidate> alternatives = new List<Candidate>();
        }

        private Scope _scope = Scope.Both;
        private bool _onlyWhiteRisk = true;
        private bool _largeOnly = true;
        private bool _highOnly;
        private string _filter = "";

        private List<Item> _items = new List<Item>();
        private List<Material> _materials = new List<Material>();
        private Item _selected;

        private Vector2 _queueScroll;
        private Vector2 _inspectorScroll;
        private bool _expandAll;

        [MenuItem("Window/Monkey Adventure/HD Mountain-Terrain Material Recovery")]
        public static void Open()
        {
            var w = GetWindow<HDMountainTerrainMaterialRecovery>("HD Mountain/Terrain Recovery");
            w.minSize = new Vector2(1100, 700);
            w.position = new Rect(w.position.x, w.position.y, 1300, 820);
            w.Show();
        }

        [MenuItem("Window/Monkey Adventure/HD Mountain-Terrain Material Recovery/Scan")]
        private static void ScanMenu()
        {
            var w = GetWindow<HDMountainTerrainMaterialRecovery>("HD Mountain/Terrain Recovery");
            w.Scan();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();

            if (_items.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No mountain/terrain recovery items loaded. Click Scan. " +
                    "The scanner targets large terrain-like meshes and white/untextured materials.",
                    MessageType.Info);
                return;
            }

            DrawSummary();
            DrawQueueAndInspector();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("HD MOUNTAIN / TERRAIN MATERIAL RECOVERY", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Conservative recovery pass for white mountain, hill, cliff and terrain meshes. " +
                "It searches existing project materials, proposes a visual match, and waits for explicit approval. " +
                "It does not modify source material assets.",
                MessageType.Info);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            _scope = (Scope)EditorGUILayout.EnumPopup("Scope", _scope, GUILayout.Width(230));
            _onlyWhiteRisk = EditorGUILayout.ToggleLeft("Only white/untextured risk", _onlyWhiteRisk, GUILayout.Width(190));
            _largeOnly = EditorGUILayout.ToggleLeft("Large terrain meshes only", _largeOnly, GUILayout.Width(190));
            _highOnly = EditorGUILayout.ToggleLeft("High only (>=90%)", _highOnly, GUILayout.Width(140));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _filter = EditorGUILayout.TextField("Filter", _filter);
            if (GUILayout.Button("Clear", GUILayout.Width(65)))
                _filter = "";

            if (GUILayout.Button("Scan", GUILayout.Width(100), GUILayout.Height(26)))
                Scan();

            if (GUILayout.Button("Reset", GUILayout.Width(80), GUILayout.Height(26)))
            {
                _items.Clear();
                _selected = null;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _items.Any(i => i.status == Status.High);
            if (GUILayout.Button("Apply High Confidence (>=90%)", GUILayout.Height(28)))
                ApplyHigh();
            GUI.enabled = true;

            GUI.enabled = _selected != null && _selected.candidate != null;
            if (GUILayout.Button("Apply Selected Candidate", GUILayout.Height(28)))
                ApplySelected();
            GUI.enabled = true;

            if (GUILayout.Button("Copy All Recovery Items", GUILayout.Height(28)))
                CopyItems(FilterItems().ToList());

            if (GUILayout.Button("Copy Selected", GUILayout.Height(28)))
                CopyItems(_selected == null ? new List<Item>() : new List<Item> { _selected });

            if (GUILayout.Button("Export Report", GUILayout.Height(28)))
                ExportReport();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Expand All"))
                _expandAll = true;
            if (GUILayout.Button("Collapse All"))
                _expandAll = false;
            if (GUILayout.Button("Copy High"))
                CopyItems(_items.Where(i => i.status == Status.High).ToList());
            if (GUILayout.Button("Copy Review"))
                CopyItems(_items.Where(i => i.status == Status.Review).ToList());
            if (GUILayout.Button("Copy Low"))
                CopyItems(_items.Where(i => i.status == Status.Low).ToList());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSummary()
        {
            int high = _items.Count(i => i.status == Status.High);
            int review = _items.Count(i => i.status == Status.Review);
            int low = _items.Count(i => i.status == Status.Low);
            int none = _items.Count(i => i.status == Status.NoCandidate);

            EditorGUILayout.HelpBox(
                $"Terrain Candidates: {_items.Count}   |   High: {high}   |   Review: {review}   |   Low: {low}   |   No Candidate: {none}\n" +
                "Only explicit Apply actions modify scene renderer slots. Source assets remain untouched.",
                high > 0 ? MessageType.Info : MessageType.Warning);
        }

        private void DrawQueueAndInspector()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.56f));
            EditorGUILayout.LabelField("MOUNTAIN / TERRAIN RECOVERY QUEUE", EditorStyles.boldLabel);
            _queueScroll = EditorGUILayout.BeginScrollView(_queueScroll);

            foreach (var item in FilterItems())
                DrawItem(item);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.42f));
            EditorGUILayout.LabelField("RECOVERY INSPECTOR", EditorStyles.boldLabel);
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

            if (_selected == null)
            {
                EditorGUILayout.HelpBox("Select Inspect on a queue item to view the proposed terrain material.", MessageType.Info);
            }
            else
            {
                DrawInspector(_selected);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawItem(Item item)
        {
            bool selected = ReferenceEquals(item, _selected);
            EditorGUILayout.BeginVertical(selected ? "selectionRect" : "box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"[{item.status.ToString().ToUpperInvariant()}] {item.renderer.name} [Slot {item.slot}]",
                EditorStyles.boldLabel);

            if (GUILayout.Button("Inspect", GUILayout.Width(65)))
            {
                _selected = item;
                Repaint();
            }
            if (GUILayout.Button("Select", GUILayout.Width(60)))
                SelectTarget(item);
            if (GUILayout.Button("Focus", GUILayout.Width(60)))
                FocusTarget(item);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Context: {item.context} | Score: {item.score}% | Bounds size: {item.size:F1}m");
            EditorGUILayout.LabelField($"Path: {item.path}");
            EditorGUILayout.LabelField(
                $"Current: {(item.current == null ? "<Missing>" : item.current.name)}  ->  Candidate: {(item.candidate == null ? "<None>" : item.candidate.name)}");

            if (_expandAll || selected)
            {
                if (!string.IsNullOrEmpty(item.reason))
                    EditorGUILayout.HelpBox(item.reason, item.status == Status.NoCandidate ? MessageType.Warning : MessageType.None);

                if (item.alternatives.Count > 0)
                {
                    EditorGUILayout.LabelField("Alternatives:", EditorStyles.boldLabel);
                    foreach (var alt in item.alternatives.Take(5))
                        EditorGUILayout.LabelField($"• {alt.material.name} — {alt.score}% — {alt.reason}");
                }

                EditorGUILayout.BeginHorizontal();
                GUI.enabled = item.candidate != null;
                if (GUILayout.Button("Apply This Candidate"))
                {
                    _selected = item;
                    ApplySelected();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawInspector(Item item)
        {
            EditorGUILayout.HelpBox(
                $"{item.status.ToString().ToUpperInvariant()} — {item.score}% CONFIDENCE\n" +
                $"Context: {item.context}\n" +
                $"Bounds: {item.size:F2}m",
                item.status == Status.High ? MessageType.Info : MessageType.Warning);

            EditorGUILayout.LabelField("Target", item.renderer != null ? item.renderer.name : "<Missing>");
            EditorGUILayout.LabelField("Hierarchy", item.path);
            EditorGUILayout.LabelField("Current Material", item.current == null ? "<Missing>" : item.current.name);
            EditorGUILayout.LabelField("Candidate Material", item.candidate == null ? "<None>" : item.candidate.name);

            if (item.candidate != null)
            {
                string shader = item.candidate.shader == null ? "<Missing>" : item.candidate.shader.name;
                Texture tex = GetBaseMap(item.candidate);
                Color color = GetBaseColor(item.candidate);

                EditorGUILayout.LabelField("Candidate Shader", shader);
                EditorGUILayout.LabelField("BaseMap", tex == null ? "<None>" : tex.name);
                EditorGUILayout.LabelField("BaseColor", $"#{ColorUtility.ToHtmlStringRGBA(color)}");

                if (!string.IsNullOrEmpty(item.reason))
                    EditorGUILayout.HelpBox(item.reason, MessageType.Info);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Ping Candidate Asset"))
                    EditorGUIUtility.PingObject(item.candidate);
                if (GUILayout.Button("Select Scene Target"))
                    SelectTarget(item);
                if (GUILayout.Button("Focus SceneView"))
                    FocusTarget(item);
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Apply Selected Candidate", GUILayout.Height(30)))
                    ApplySelected();
            }
        }

        private IEnumerable<Item> FilterItems()
        {
            IEnumerable<Item> q = _items;

            if (_highOnly)
                q = q.Where(i => i.status == Status.High);

            if (!string.IsNullOrWhiteSpace(_filter))
            {
                string s = _filter.Trim().ToLowerInvariant();
                q = q.Where(i =>
                    i.renderer != null &&
                    (i.renderer.name.ToLowerInvariant().Contains(s) ||
                     i.path.ToLowerInvariant().Contains(s) ||
                     i.context.ToLowerInvariant().Contains(s) ||
                     (i.current != null && i.current.name.ToLowerInvariant().Contains(s)) ||
                     (i.candidate != null && i.candidate.name.ToLowerInvariant().Contains(s)) ||
                     i.reason.ToLowerInvariant().Contains(s)));
            }

            return q;
        }

        private void Scan()
        {
            _items.Clear();
            _selected = null;

            _materials = LoadProjectMaterials();
            var roots = ResolveRoots();

            foreach (var root in roots)
                ScanRoot(root);

            Repaint();

            EditorUtility.DisplayDialog(
                "HD Mountain/Terrain Recovery",
                $"Scan complete.\n\n" +
                $"Terrain recovery slots: {_items.Count}\n" +
                $"High (90–100%): {_items.Count(i => i.status == Status.High)}\n" +
                $"Review (75–89%): {_items.Count(i => i.status == Status.Review)}\n" +
                $"Low (55–74%): {_items.Count(i => i.status == Status.Low)}\n" +
                $"No candidate: {_items.Count(i => i.status == Status.NoCandidate)}\n\n" +
                "No material changes were made.",
                "OK");
        }

        private List<Transform> ResolveRoots()
        {
            var roots = new List<Transform>();
            if (_scope == Scope.Preview || _scope == Scope.Both)
                AddRoot(PreviewRoot, roots);
            if (_scope == Scope.Active || _scope == Scope.Both)
                AddRoot(ActiveRoot, roots);

            return roots;
        }

        private static void AddRoot(string path, List<Transform> roots)
        {
            GameObject go = GameObject.Find(path);
            if (go != null && !roots.Contains(go.transform))
                roots.Add(go.transform);
        }

        private void ScanRoot(Transform root)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
                    continue;

                Bounds b = r.bounds;
                float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
                string path = GetHierarchyPath(r.transform);
                string lowerPath = path.ToLowerInvariant();
                string lowerName = r.name.ToLowerInvariant();

                bool terrainName =
                    lowerName.Contains("mountain") || lowerName.Contains("hill") ||
                    lowerName.Contains("cliff") || lowerName.Contains("terrain") ||
                    lowerName.Contains("landscape") || lowerName.Contains("plateau") ||
                    lowerName.Contains("slope") || lowerPath.Contains("/mountain/") ||
                    lowerPath.Contains("/cliff/") || lowerPath.Contains("/terrain/") ||
                    lowerPath.Contains("/landscape/");

                Material[] mats = r.sharedMaterials;
                for (int slot = 0; slot < mats.Length; slot++)
                {
                    Material current = mats[slot];
                    bool whiteRisk = IsWhiteOrUntexturedRisk(current);

                    if (_largeOnly && size < 4f && !terrainName)
                        continue;

                    if (!terrainName && size < 8f)
                        continue;

                    if (_onlyWhiteRisk && !whiteRisk)
                        continue;

                    Item item = BuildItem(r, slot, current, path, size, terrainName);
                    _items.Add(item);
                }
            }
        }

        private Item BuildItem(Renderer r, int slot, Material current, string path, float size, bool terrainName)
        {
            var candidates = new List<Candidate>();

            foreach (Material m in _materials)
            {
                if (m == null || m == current)
                    continue;

                int score = ScoreMaterial(r, current, m, path, size, terrainName, out string reason);
                if (score > 0)
                    candidates.Add(new Candidate { material = m, score = score, reason = reason });
            }

            candidates = candidates
                .OrderByDescending(c => c.score)
                .ThenBy(c => c.material.name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var item = new Item
            {
                renderer = r,
                slot = slot,
                current = current,
                path = path,
                size = size,
                context = InferContext(path, r.name)
            };

            if (candidates.Count == 0)
            {
                item.status = Status.NoCandidate;
                item.score = 0;
                item.reason = "No sufficiently relevant terrain/mountain material candidate was found. Left unchanged.";
                return item;
            }

            item.candidate = candidates[0].material;
            item.score = candidates[0].score;
            item.reason = candidates[0].reason;
            item.alternatives = candidates;

            item.status = item.score >= 90 ? Status.High :
                          item.score >= 75 ? Status.Review :
                          item.score >= 55 ? Status.Low : Status.NoCandidate;

            return item;
        }

        private int ScoreMaterial(Renderer r, Material current, Material candidate, string path,
            float size, bool terrainName, out string reason)
        {
            string n = candidate.name.ToLowerInvariant();
            string p = AssetDatabase.GetAssetPath(candidate).ToLowerInvariant();
            string ctx = (path + "/" + r.name).ToLowerInvariant();

            bool terrainKeyword =
                ContainsAny(n, "terrain", "mountain", "mount", "hill", "cliff", "landscape",
                    "ground", "earth", "dirt", "soil", "rock", "stone", "rockface", "cliffrock",
                    "boulder", "forestpack");

            bool badKeyword =
                ContainsAny(n, "leaf", "leaves", "foliage", "grass", "fern", "flower", "water",
                    "bark", "trunk", "wood", "log", "stump", "glass", "metal");

            bool pathTerrain = ContainsAny(p, "terrain", "mountain", "rock", "cliff", "ground", "landscape");
            Texture baseMap = GetBaseMap(candidate);
            bool hasTexture = baseMap != null;
            bool urp = candidate.shader != null && IsUrpShader(candidate.shader.name);
            Color c = GetBaseColor(candidate);

            int score = 0;
            var reasons = new List<string>();

            if (terrainKeyword) { score += 28; reasons.Add("terrain/rock keyword"); }
            if (pathTerrain) { score += 18; reasons.Add("terrain-like asset path"); }
            if (hasTexture) { score += 20; reasons.Add("BaseMap texture present"); }
            if (urp) { score += 12; reasons.Add("URP-compatible"); }

            if (terrainName) { score += 10; reasons.Add("terrain-like target"); }
            if (size >= 12f) { score += 6; reasons.Add("large mesh"); }
            else if (size >= 6f) { score += 4; reasons.Add("medium-large mesh"); }

            if (IsEarthRockColor(c)) { score += 6; reasons.Add("natural earth/rock tint"); }

            if (badKeyword)
            {
                score -= 45;
                reasons.Add("category exclusion keyword");
            }

            if (!hasTexture && IsNearlyWhite(c))
            {
                score -= 15;
                reasons.Add("white untextured candidate");
            }

            score = Mathf.Clamp(score, 0, 100);
            reason = reasons.Count == 0
                ? "Weak visual evidence."
                : string.Join(", ", reasons) + ".";

            return score;
        }

        private static bool IsWhiteOrUntexturedRisk(Material m)
        {
            if (m == null || m.shader == null)
                return true;

            Texture tex = GetBaseMap(m);
            Color c = GetBaseColor(m);

            return tex == null && IsNearlyWhite(c);
        }

        private static Texture GetBaseMap(Material m)
        {
            if (m == null) return null;
            if (m.HasProperty("_BaseMap")) return m.GetTexture("_BaseMap");
            if (m.HasProperty("_MainTex")) return m.GetTexture("_MainTex");
            return null;
        }

        private static Color GetBaseColor(Material m)
        {
            if (m == null) return Color.white;
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            if (m.HasProperty("_Color")) return m.GetColor("_Color");
            return Color.white;
        }

        private static bool IsNearlyWhite(Color c)
        {
            return c.r > 0.88f && c.g > 0.88f && c.b > 0.88f;
        }

        private static bool IsEarthRockColor(Color c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return max > 0.15f && max < 0.9f && (max - min) > 0.04f;
        }

        private static bool IsUrpShader(string shaderName)
        {
            return !string.IsNullOrEmpty(shaderName) &&
                   (shaderName.Contains("Universal Render Pipeline") ||
                    shaderName.Contains("Shader Graph"));
        }

        private static bool ContainsAny(string value, params string[] terms)
        {
            foreach (string t in terms)
                if (value.Contains(t))
                    return true;
            return false;
        }

        private static string InferContext(string path, string name)
        {
            string s = (path + "/" + name).ToLowerInvariant();
            if (ContainsAny(s, "mountain", "hill", "cliff", "terrain", "landscape", "plateau"))
                return "Mountain / Terrain";
            if (ContainsAny(s, "rock", "boulder", "stone"))
                return "Rock / Cliff";
            return "Large Environment Mesh";
        }

        private static List<Material> LoadProjectMaterials()
        {
            var list = new List<Material>();
            foreach (string guid in AssetDatabase.FindAssets("t:Material"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m != null)
                    list.Add(m);
            }
            return list;
        }

        private void ApplyHigh()
        {
            var high = _items.Where(i => i.status == Status.High && i.candidate != null).ToList();
            if (high.Count == 0)
            {
                EditorUtility.DisplayDialog("HD Mountain/Terrain Recovery", "No High-confidence candidates are available.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Apply High-confidence Terrain Recovery",
                    $"Apply {high.Count} mountain/terrain material candidates?\n\n" +
                    "Only scene renderer material slots will change. Source materials are not modified.",
                    "Apply", "Cancel"))
                return;

            int applied = 0;
            foreach (Item item in high)
            {
                if (ApplyItem(item))
                    applied++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Scan();
            EditorUtility.DisplayDialog("HD Mountain/Terrain Recovery",
                $"Applied {applied} terrain/mountain material recoveries.\n\nRun Scan again to verify remaining items.",
                "OK");
        }

        private void ApplySelected()
        {
            if (_selected == null || _selected.candidate == null)
                return;

            if (!EditorUtility.DisplayDialog(
                    "Apply Selected Terrain Material",
                    $"Apply '{_selected.candidate.name}' to '{_selected.renderer.name}' Slot {_selected.slot}?\n\n" +
                    "The source material asset will not be modified.",
                    "Apply", "Cancel"))
                return;

            if (ApplyItem(_selected))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Scan();
            }
        }

        private static bool ApplyItem(Item item)
        {
            if (item.renderer == null || item.candidate == null)
                return false;

            Material[] mats = item.renderer.sharedMaterials;
            if (item.slot < 0 || item.slot >= mats.Length)
                return false;

            Undo.RecordObject(item.renderer, "HD Mountain Terrain Material Recovery");
            mats[item.slot] = item.candidate;
            item.renderer.sharedMaterials = mats;
            EditorUtility.SetDirty(item.renderer);

            return true;
        }

        private void SelectTarget(Item item)
        {
            if (item.renderer == null) return;
            Selection.activeGameObject = item.renderer.gameObject;
            EditorGUIUtility.PingObject(item.renderer.gameObject);
        }

        private void FocusTarget(Item item)
        {
            if (item.renderer == null) return;
            Selection.activeGameObject = item.renderer.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        private static string GetHierarchyPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }

        private void CopyItems(List<Item> items)
        {
            if (items == null || items.Count == 0)
            {
                GUIUtility.systemCopyBuffer = "";
                return;
            }

            var lines = new List<string>
            {
                "HD MOUNTAIN / TERRAIN MATERIAL RECOVERY",
                "======================================"
            };

            foreach (Item i in items)
            {
                lines.Add(
                    $"[{i.status}] {i.renderer.name} | {i.score}% | {i.context}\n" +
                    $"Path: {i.path}\n" +
                    $"Current: {(i.current == null ? "<Missing>" : i.current.name)}\n" +
                    $"Candidate: {(i.candidate == null ? "<None>" : i.candidate.name)}\n" +
                    $"Reason: {i.reason}\n");
            }

            GUIUtility.systemCopyBuffer = string.Join("\n", lines);
            ShowNotification(new GUIContent($"Copied {items.Count} recovery items."));
        }

        private void ExportReport()
        {
            Directory.CreateDirectory("Assets/AILevelBuilder/Reports");

            var lines = new List<string>
            {
                "HD MOUNTAIN / TERRAIN MATERIAL RECOVERY REPORT",
                "================================================",
                $"Date: {DateTime.Now}",
                $"Items: {_items.Count}",
                ""
            };

            foreach (Item i in _items)
            {
                lines.Add(
                    $"[{i.status}] {i.renderer.name} | Score {i.score}% | {i.context}\n" +
                    $"Path: {i.path}\n" +
                    $"Current: {(i.current == null ? "<Missing>" : i.current.name)}\n" +
                    $"Candidate: {(i.candidate == null ? "<None>" : i.candidate.name)}\n" +
                    $"Reason: {i.reason}\n");
            }

            File.WriteAllLines(ReportPath, lines);
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TextAsset>(ReportPath));
            ShowNotification(new GUIContent("Mountain/Terrain recovery report saved."));
        }
    }
}
