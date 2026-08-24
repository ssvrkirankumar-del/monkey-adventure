using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MonkeyAdventure.AILevelBuilder.Editor
{
    /// <summary>
    /// Level 01 cyan/blue ground recovery.
    /// Finds ground/terrain renderers with cyan/blue material risk, searches existing
    /// project materials for textured URP ground candidates, creates no source assets,
    /// and only changes scene renderer slots after explicit Apply.
    /// </summary>
    public sealed class HDGroundTerrainCyanFixer : EditorWindow
    {
        private const string ActiveRoot = "AI_GENERATED_LEVEL/HD_REPLACEMENTS";
        private const string PreviewRoot = "AI_GENERATED_LEVEL/HD_REPLACEMENTS_PREVIEW";
        private const string ReportPath = "Assets/AILevelBuilder/Reports/HDGroundTerrainCyanFixer.txt";

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
            public string reason;
            public List<Candidate> alternatives = new List<Candidate>();
        }

        private Scope _scope = Scope.Preview;
        private bool _includeNonCyanGroundRisk = true;
        private string _filter = "";
        private bool _expandAll;
        private Vector2 _queueScroll;
        private Vector2 _inspectorScroll;
        private Item _selected;
        private List<Item> _items = new List<Item>();
        private List<Material> _materials = new List<Material>();

        [MenuItem("Window/Monkey Adventure/HD Ground-Terrain Cyan Fixer", false, 146)]
        public static void Open()
        {
            var w = GetWindow<HDGroundTerrainCyanFixer>("HD Ground Cyan Fixer");
            w.minSize = new Vector2(1050, 650);
            w.position = new Rect(w.position.x, w.position.y, 1250, 760);
            w.Show();
        }

        [MenuItem("Window/Monkey Adventure/HD Ground-Terrain Cyan Fixer/Scan", false, 147)]
        private static void ScanMenu()
        {
            var w = GetWindow<HDGroundTerrainCyanFixer>("HD Ground Cyan Fixer");
            w.Scan();
        }

        [MenuItem("Window/Monkey Adventure/HD Ground-Terrain Cyan Fixer/Auto-Fix Preview Cyan Ground", false, 148)]
        private static void AutoFixPreviewMenu()
        {
            var w = GetWindow<HDGroundTerrainCyanFixer>("HD Ground Cyan Fixer");
            w._scope = Scope.Preview;
            w.Scan();
            w.ApplyHigh();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();

            if (_items.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No cyan/blue ground targets loaded. Click Scan. The scanner ignores water and gameplay objects and searches only ground/terrain-like renderers.",
                    MessageType.Info);
                return;
            }

            DrawSummary();
            DrawQueueAndInspector();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("HD GROUND / TERRAIN CYAN FIXER", MakeHeaderStyle(17));
            EditorGUILayout.HelpBox(
                "Fixes the visible cyan/blue ground problem using existing project materials. " +
                "Water is excluded. Source material assets are never modified. Scene changes require explicit Apply and are Undo-recorded.",
                MessageType.Info);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            _scope = (Scope)EditorGUILayout.EnumPopup("Scope", _scope, GUILayout.Width(260));
            _includeNonCyanGroundRisk = EditorGUILayout.ToggleLeft("Include untextured/white ground risk", _includeNonCyanGroundRisk, GUILayout.Width(250));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("SCAN", GUILayout.Width(100), GUILayout.Height(28))) Scan();
            if (GUILayout.Button("RESET", GUILayout.Width(100), GUILayout.Height(28))) { _items.Clear(); _selected = null; Repaint(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Filter", GUILayout.Width(45));
            _filter = EditorGUILayout.TextField(_filter);
            if (GUILayout.Button("Clear", GUILayout.Width(60))) _filter = "";
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool anyHigh = _items.Any(i => i.status == Status.High && i.candidate != null);
            GUI.enabled = anyHigh;
            if (GUILayout.Button("APPLY HIGH CONFIDENCE (>=90%)", GUILayout.Height(30))) ApplyHigh();
            GUI.enabled = true;
            GUI.enabled = _selected != null && _selected.candidate != null;
            if (GUILayout.Button("APPLY SELECTED", GUILayout.Height(30))) ApplySelected();
            GUI.enabled = true;
            if (GUILayout.Button("COPY ALL", GUILayout.Height(30))) CopyItems(FilterItems().ToList());
            if (GUILayout.Button("EXPORT REPORT", GUILayout.Height(30))) ExportReport();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Expand All")) _expandAll = true;
            if (GUILayout.Button("Collapse All")) _expandAll = false;
            if (GUILayout.Button("Copy High")) CopyItems(_items.Where(i => i.status == Status.High).ToList());
            if (GUILayout.Button("Copy Review")) CopyItems(_items.Where(i => i.status == Status.Review).ToList());
            if (GUILayout.Button("Copy Low")) CopyItems(_items.Where(i => i.status == Status.Low).ToList());
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
                $"Ground cyan/blue targets: {_items.Count} | High: {high} | Review: {review} | Low: {low} | No Candidate: {none}\n" +
                "Recommended order: High first, then Review. No material is changed during Scan.",
                high > 0 ? MessageType.Warning : MessageType.Info);
        }

        private void DrawQueueAndInspector()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.57f));
            EditorGUILayout.LabelField("GROUND / TERRAIN FIX QUEUE", EditorStyles.boldLabel);
            _queueScroll = EditorGUILayout.BeginScrollView(_queueScroll);
            foreach (Item item in FilterItems()) DrawItem(item);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.41f));
            EditorGUILayout.LabelField("RECOVERY INSPECTOR", EditorStyles.boldLabel);
            _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);
            if (_selected == null)
            {
                EditorGUILayout.HelpBox("Select Inspect to review the proposed ground material.", MessageType.Info);
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
            EditorGUILayout.LabelField($"[{item.status.ToString().ToUpperInvariant()}] {item.renderer.name} [Slot {item.slot}]", EditorStyles.boldLabel);
            if (GUILayout.Button("Inspect", GUILayout.Width(65))) { _selected = item; Repaint(); }
            if (GUILayout.Button("Select", GUILayout.Width(60))) SelectTarget(item);
            if (GUILayout.Button("Focus", GUILayout.Width(60))) FocusTarget(item);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField($"Path: {item.path}");
            EditorGUILayout.LabelField($"Current: {(item.current == null ? "<Missing>" : item.current.name)}  ->  Candidate: {(item.candidate == null ? "<None>" : item.candidate.name)} | Score: {item.score}%");

            if (_expandAll || selected)
            {
                EditorGUILayout.HelpBox(item.reason, item.status == Status.NoCandidate ? MessageType.Warning : MessageType.None);
                if (item.alternatives.Count > 0)
                {
                    EditorGUILayout.LabelField("Alternatives", EditorStyles.boldLabel);
                    foreach (Candidate c in item.alternatives.Take(6))
                    {
                        EditorGUILayout.BeginHorizontal("box");
                        EditorGUILayout.LabelField($"{c.material.name} | {c.score}%", GUILayout.Width(220));
                        EditorGUILayout.LabelField(c.reason, EditorStyles.wordWrappedMiniLabel);
                        if (GUILayout.Button("Use", GUILayout.Width(50)))
                        {
                            item.candidate = c.material;
                            item.score = c.score;
                            item.status = Classify(c.score);
                            item.reason = c.reason;
                            _selected = item;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                if (GUILayout.Button("Copy Item Details")) CopyItems(new List<Item> { item });
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawInspector(Item item)
        {
            EditorGUILayout.LabelField($"{item.status.ToString().ToUpperInvariant()} — {item.score}%", MakeScoreStyle(item.score));
            EditorGUILayout.ObjectField("Renderer", item.renderer, typeof(Renderer), true);
            EditorGUILayout.LabelField("Hierarchy", item.path);
            EditorGUILayout.LabelField("Current Material", item.current != null ? item.current.name : "<Missing>");
            EditorGUILayout.LabelField("Candidate", item.candidate != null ? item.candidate.name : "<None>");
            EditorGUILayout.HelpBox(item.reason, item.status == Status.High ? MessageType.Info : MessageType.Warning);
            if (item.candidate != null)
            {
                EditorGUILayout.LabelField("Candidate Asset", AssetDatabase.GetAssetPath(item.candidate));
                EditorGUILayout.LabelField("Candidate Shader", item.candidate.shader != null ? item.candidate.shader.name : "<Missing>");
                EditorGUILayout.LabelField("Candidate BaseMap", DescribeTexture(item.candidate));
                EditorGUILayout.ColorField("Candidate BaseColor", GetBaseColor(item.candidate));
            }
            EditorGUILayout.Space(8);
            GUI.enabled = item.candidate != null;
            if (GUILayout.Button("APPLY SELECTED CANDIDATE", GUILayout.Height(34))) ApplySelected();
            GUI.enabled = true;
            if (GUILayout.Button("Ping Candidate"))
                if (item.candidate != null) EditorGUIUtility.PingObject(item.candidate);
            if (GUILayout.Button("Select Scene Target")) SelectTarget(item);
            if (GUILayout.Button("Focus Scene View")) FocusTarget(item);
        }

        private IEnumerable<Item> FilterItems()
        {
            IEnumerable<Item> q = _items;
            string s = (_filter ?? "").Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(s))
            {
                q = q.Where(i =>
                    i.renderer != null &&
                    (i.renderer.name.ToLowerInvariant().Contains(s) ||
                     i.path.ToLowerInvariant().Contains(s) ||
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

            foreach (Transform root in ResolveRoots()) ScanRoot(root);
            _items = _items.OrderByDescending(i => i.score).ThenBy(i => i.path, StringComparer.OrdinalIgnoreCase).ToList();
            Repaint();

            EditorUtility.DisplayDialog(
                "HD Ground Cyan Fixer",
                $"Scan complete.\n\nGround cyan/blue targets: {_items.Count}\nHigh: {_items.Count(i => i.status == Status.High)}\nReview: {_items.Count(i => i.status == Status.Review)}\nLow: {_items.Count(i => i.status == Status.Low)}\nNo candidate: {_items.Count(i => i.status == Status.NoCandidate)}\n\nNo material changes were made.",
                "OK");
        }

        private IEnumerable<Transform> ResolveRoots()
        {
            var roots = new List<Transform>();
            if (_scope == Scope.Preview || _scope == Scope.Both) AddRoot(PreviewRoot, roots);
            if (_scope == Scope.Active || _scope == Scope.Both) AddRoot(ActiveRoot, roots);
            return roots;
        }

        private static void AddRoot(string path, List<Transform> roots)
        {
            GameObject go = GameObject.Find(path);
            if (go != null && !roots.Contains(go.transform)) roots.Add(go.transform);
        }

        private void ScanRoot(Transform root)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (IsGameplayObject(r.gameObject)) continue;

                string path = GetHierarchyPath(r.transform);
                string objectName = r.gameObject.name.ToLowerInvariant();
                string text = (objectName + " " + path).ToLowerInvariant();

                // Classify by the actual renderer object name first. A Landscape
                // under .../Lake/... is valid ground, but Tree6 and Water Plane
                // under the same parent are NOT ground.
                if (IsActualWaterObject(objectName)) continue;
                if (!IsGroundLike(r.gameObject.name, path)) continue;

                Material[] mats = r.sharedMaterials;
                for (int slot = 0; slot < mats.Length; slot++)
                {
                    Material current = mats[slot];
                    if (!IsCyanOrBlueGroundRisk(current) && !(_includeNonCyanGroundRisk && IsWhiteUntexturedRisk(current))) continue;
                    _items.Add(BuildItem(r, slot, current, path));
                }
            }
        }

        private Item BuildItem(Renderer r, int slot, Material current, string path)
        {
            var candidates = new List<Candidate>();
            foreach (Material m in _materials)
            {
                if (m == null || m == current) continue;
                int score = ScoreGroundCandidate(m, path, out string reason);
                if (score > 0) candidates.Add(new Candidate { material = m, score = score, reason = reason });
            }
            candidates = candidates.OrderByDescending(c => c.score).ThenBy(c => c.material.name, StringComparer.OrdinalIgnoreCase).ToList();

            var item = new Item { renderer = r, slot = slot, current = current, path = path, alternatives = candidates };
            if (candidates.Count == 0)
            {
                item.status = Status.NoCandidate;
                item.reason = "No sufficiently strong existing textured ground material was found. Left unchanged.";
                return item;
            }
            item.candidate = candidates[0].material;
            item.score = candidates[0].score;
            item.status = Classify(item.score);
            item.reason = candidates[0].reason;
            return item;
        }

        private static int ScoreGroundCandidate(Material m, string targetPath, out string reason)
        {
            string n = m.name.ToLowerInvariant();
            string p = AssetDatabase.GetAssetPath(m).ToLowerInvariant();
            string shader = m.shader != null ? m.shader.name.ToLowerInvariant() : "";

            if (ContainsAny(n,
                "rock_arc", "rockarc", "arch", "ruin", "ruins",
                "rock", "stone", "boulder", "cliff", "mountain",
                "water", "river", "lake", "ocean",
                "leaf", "leaves", "foliage", "fern", "grassblade",
                "bark", "trunk", "snow", "ice", "metal", "glass"))
            {
                reason = "Category-excluded candidate.";
                return 0;
            }

            Texture tex = GetBaseMap(m);
            if (tex == null) { reason = "Candidate has no BaseMap texture."; return 0; }

            bool urp = shader.Contains("universal render pipeline") || shader.Contains("shader graph");
            // Strict semantic evidence. Do NOT treat a generic "forestpack" or
            // "stone" asset as ground merely because it belongs to the same pack.
            bool groundName = ContainsAny(n,
                "ground", "terrain", "forestfloor", "forest_floor",
                "earth", "dirt", "soil", "mud", "floor", "path",
                "trail", "landscape", "grassground", "meadow", "mossground");

            bool groundPath = false;
            foreach (string rawSegment in p.Split('/'))
            {
                string s = rawSegment.Trim();
                if (s == "ground" || s == "terrain" || s == "landscape" ||
                    s == "forestfloor" || s == "forest_floor" ||
                    s == "groundtextures" || s == "terraintextures")
                {
                    groundPath = true;
                    break;
                }
            }
            Color c = GetBaseColor(m);
            bool natural = IsNaturalGroundTint(c);

            int score = 0;
            var reasons = new List<string>();

            // Generic pack membership is never enough for a high-confidence ground
            // replacement. Explicit ground semantics are required.
            if (groundName) { score += 48; reasons.Add("explicit ground/terrain keyword"); }
            if (groundPath) { score += 16; reasons.Add("ground-like asset path"); }
            if (tex != null) { score += 24; reasons.Add("BaseMap texture present"); }
            if (urp) { score += 12; reasons.Add("URP/Shader Graph compatible"); }
            if (natural) { score += 6; reasons.Add("natural ground tint"); }
            if (IsNearlyWhite(c)) { score -= 15; reasons.Add("near-white tint"); }

            // Prevent generic rock/stone/forestpack materials from becoming a
            // 90-100% ground candidate just because they are textured and URP.
            if (!groundName && !groundPath)
            {
                reason = "Rejected: candidate lacks explicit ground/terrain semantics.";
                return 0;
            }

            reason = reasons.Count == 0 ? "Weak ground evidence." : string.Join(", ", reasons) + ".";
            return Mathf.Clamp(score, 0, 100);
        }

        private static bool IsGroundLike(string objectName, string hierarchyPath)
        {
            string n = objectName.ToLowerInvariant();
            string p = hierarchyPath.ToLowerInvariant();

            // No size-based fallback. Large rocks/arches must never become ground.
            if (ContainsAny(n,
                "rock_arc", "rockarc", "arch", "ruin", "ruins",
                "rock", "stone", "boulder", "cliff", "mountain",
                "tree", "leaf", "leaves", "foliage", "fern", "grassblade",
                "bark", "trunk", "bush", "shrub", "water", "water plane",
                "waterplane"))
                return false;

            // Strongest evidence: the renderer's own object name.
            if (ContainsAny(n,
                "ground", "terrain", "landscape", "forestfloor", "forest_floor",
                "forestground", "forest_ground", "riverbank", "lakebank",
                "river_bank", "lake_bank", "grassground", "meadowground"))
                return true;

            // Path evidence is accepted only from explicit directory/object
            // segments, never from a generic parent such as Water/Lake.
            string[] segments = p.Split('/');
            foreach (string raw in segments)
            {
                string s = raw.Trim();
                if (s == "ground" || s == "terrain" || s == "landscape" ||
                    s == "forestfloor" || s == "forest_floor" ||
                    s == "forestground" || s == "forest_ground" ||
                    s == "riverbank" || s == "lakebank" ||
                    s == "river_bank" || s == "lake_bank")
                    return true;
            }

            return false;
        }

        private static bool IsActualWaterObject(string objectName)
        {
            string n = objectName.ToLowerInvariant();

            // Only the renderer/object itself determines water. A Lake/River
            // parent does not make a child Landscape or bank a water surface.
            return ContainsAny(n,
                "water plane", "waterplane", "water_surface",
                "watersurface", "watermesh", "water_surface_mesh",
                "ocean", "pond");
        }

        private static bool IsCyanOrBlueGroundRisk(Material m)
        {
            if (m == null || m.shader == null) return true;
            if (GetBaseMap(m) != null) return false;
            Color c = GetBaseColor(m);
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            bool saturated = max - min > 0.18f;
            bool blue = c.b > c.r * 1.30f && c.b > 0.42f;
            bool cyan = c.g > c.r * 1.20f && c.b > c.r * 1.20f && ((c.g + c.b) * 0.5f) > 0.42f;
            return saturated && (blue || cyan);
        }

        private static bool IsWhiteUntexturedRisk(Material m)
        {
            if (m == null || m.shader == null) return true;
            return GetBaseMap(m) == null && IsNearlyWhite(GetBaseColor(m));
        }

        private static bool IsNaturalGroundTint(Color c)
        {
            return c.r > 0.10f && c.g > 0.10f && c.b > 0.05f && c.r > c.b * 0.85f;
        }

        private static bool IsNearlyWhite(Color c)
        {
            return c.r > 0.88f && c.g > 0.88f && c.b > 0.88f;
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
            if (m == null || m.shader == null) return Color.white;
            if (HasColorProperty(m, "_BaseColor")) return m.GetColor("_BaseColor");
            if (HasColorProperty(m, "_Color")) return m.GetColor("_Color");
            return Color.white;
        }

        private static bool HasColorProperty(Material m, string propName)
        {
            if (m == null || m.shader == null) return false;
            Shader s = m.shader;
            int count = s.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                if (s.GetPropertyName(i) == propName)
                {
                    return s.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Color;
                }
            }
            return false;
        }

        private static List<Material> LoadProjectMaterials()
        {
            var list = new List<Material>();
            foreach (string guid in AssetDatabase.FindAssets("t:Material"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m != null) list.Add(m);
            }
            return list;
        }

        private static Status Classify(int score)
        {
            if (score >= 90) return Status.High;
            if (score >= 75) return Status.Review;
            if (score >= 55) return Status.Low;
            return Status.NoCandidate;
        }

        private void ApplyHigh()
        {
            var high = _items.Where(i => i.status == Status.High && i.candidate != null).ToList();
            if (high.Count == 0) return;
            if (!EditorUtility.DisplayDialog("Apply Ground Cyan Fix", $"Apply {high.Count} high-confidence ground material replacements?\n\nOnly scene renderer slots will change.", "Apply", "Cancel")) return;
            int applied = 0;
            foreach (Item item in high) if (ApplyItem(item)) applied++;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Scan();
            EditorUtility.DisplayDialog("HD Ground Cyan Fixer", $"Applied {applied} ground material replacements. Re-scan complete.", "OK");
        }

        private void ApplySelected()
        {
            if (_selected == null || _selected.candidate == null) return;
            if (!EditorUtility.DisplayDialog("Apply Selected Ground Material", $"Apply '{_selected.candidate.name}' to '{_selected.renderer.name}' Slot {_selected.slot}?\n\nSource material will not be modified.", "Apply", "Cancel")) return;
            if (ApplyItem(_selected))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Scan();
            }
        }

        private static bool ApplyItem(Item item)
        {
            if (item.renderer == null || item.candidate == null) return false;
            Material[] mats = item.renderer.sharedMaterials;
            if (item.slot < 0 || item.slot >= mats.Length) return false;
            Undo.RecordObject(item.renderer, "HD Ground Terrain Cyan Fix");
            mats[item.slot] = item.candidate;
            item.renderer.sharedMaterials = mats;
            EditorUtility.SetDirty(item.renderer);
            return true;
        }

        private static void SelectTarget(Item item)
        {
            if (item.renderer == null) return;
            Selection.activeGameObject = item.renderer.gameObject;
            EditorGUIUtility.PingObject(item.renderer.gameObject);
        }

        private static void FocusTarget(Item item)
        {
            if (item.renderer == null) return;
            Selection.activeGameObject = item.renderer.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        private static void CopyItems(List<Item> items)
        {
            if (items == null || items.Count == 0) return;
            GUIUtility.systemCopyBuffer = string.Join(System.Environment.NewLine + System.Environment.NewLine, items.Select(FormatItem));
        }

        private static string FormatItem(Item i)
        {
            return $"[{i.status}] {i.renderer?.name} [Slot {i.slot}]\nPath: {i.path}\nCurrent: {i.current?.name ?? "<Missing>"}\nCandidate: {i.candidate?.name ?? "<None>"}\nScore: {i.score}%\nReason: {i.reason}";
        }

        private void ExportReport()
        {
            string dir = "Assets/AILevelBuilder/Reports";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var lines = new List<string>
            {
                "HD GROUND / TERRAIN CYAN FIXER REPORT",
                "=======================================",
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"Targets: {_items.Count}",
                $"High: {_items.Count(i => i.status == Status.High)}",
                $"Review: {_items.Count(i => i.status == Status.Review)}",
                $"Low: {_items.Count(i => i.status == Status.Low)}",
                $"No Candidate: {_items.Count(i => i.status == Status.NoCandidate)}",
                ""
            };
            lines.AddRange(_items.Select(FormatItem));
            File.WriteAllLines(ReportPath, lines);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("HD Ground Cyan Fixer", $"Report saved to {ReportPath}", "OK");
        }

        private static string DescribeTexture(Material m)
        {
            Texture t = GetBaseMap(m);
            return t != null ? t.name : "<None>";
        }

        private static string GetHierarchyPath(Transform t)
        {
            var names = new List<string>();
            while (t != null) { names.Add(t.name); t = t.parent; }
            names.Reverse();
            return string.Join("/", names);
        }

        private static bool IsGameplayObject(GameObject go)
        {
            string text = (go.name + " " + GetHierarchyPath(go.transform)).ToLowerInvariant();
            return ContainsAny(text, "player", "monkey", "checkpoint", "collectible", "obstacle", "enemy", "finish", "start", "gameplay", "trigger", "camera");
        }

        private static bool ContainsAny(string value, params string[] terms)
        {
            foreach (string t in terms)
                if (!string.IsNullOrEmpty(t) && value.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static GUIStyle MakeHeaderStyle(int size)
        {
            return new GUIStyle(EditorStyles.boldLabel) { fontSize = size, alignment = TextAnchor.MiddleCenter };
        }

        private static GUIStyle MakeScoreStyle(int score)
        {
            var s = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            s.normal.textColor = score >= 90 ? Color.green : score >= 75 ? Color.yellow : Color.red;
            return s;
        }
    }
}
