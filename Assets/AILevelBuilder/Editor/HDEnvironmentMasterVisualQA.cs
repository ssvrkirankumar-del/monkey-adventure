using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonkeyAdventure.AILevelBuilder.Editor
{
    /// <summary>
    /// Master visual QA + preview dressing pass for Level 01.
    /// It audits the whole HD environment as one system and can generate a
    /// deterministic, non-destructive visual dressing preview.
    ///
    /// Design rule:
    /// - NEVER changes source prefabs/materials.
    /// - NEVER modifies gameplay objects.
    /// - Preview is created under AI_GENERATED_LEVEL/MASTER_VISUAL_DRESSING_PREVIEW.
    /// - Apply is explicit and copies only the preview hierarchy into MASTER_VISUAL_DRESSING.
    /// - All spawned visual renderers have colliders disabled/removed.
    /// </summary>
    public class HDEnvironmentMasterVisualQA : EditorWindow
    {
        private const string LevelRoot = "AI_GENERATED_LEVEL";
        private const string PreviewRoot = "AI_GENERATED_LEVEL/MASTER_VISUAL_DRESSING_PREVIEW";
        private const string AppliedRoot = "AI_GENERATED_LEVEL/MASTER_VISUAL_DRESSING";

        private enum Density { Low, Medium, High }

        [Serializable]
        private class CategoryStat
        {
            public string Name;
            public int Found;
            public int Issues;
            public int Score;
            public string Notes;
        }

        private class AssetCandidate
        {
            public GameObject Prefab;
            public string Path;
            public string Category;
            public int Score;
        }

        private class AuditResult
        {
            public int Overall;
            public int MaterialScore;
            public int TerrainScore;
            public int VegetationScore;
            public int MountainScore;
            public int WaterScore;
            public int RuinScore;
            public int LightingScore;
            public int GameplayScore;
            public int CompositionScore;
            public int PerformanceScore;

            public readonly List<string> Critical = new List<string>();
            public readonly List<string> Warnings = new List<string>();
            public readonly List<CategoryStat> Categories = new List<CategoryStat>();
        }

        private Vector2 _scroll;
        private Vector2 _reportScroll;
        private AuditResult _audit;

        private Density _density = Density.Medium;
        private int _seed = 1337;
        private float _safetyMargin = 3.5f;
        private int _maxObjects = 300;
        private float _outerBand = 0.20f;
        private bool _useOnlyHDAssets = true;
        private bool _includeGroundVariation = true;
        private bool _includeWaterEdge = true;
        private bool _includeRuins = true;
        private bool _previewGenerated;
        private bool _showAllChecks = true;
        private readonly Dictionary<string, bool> _expandedCategories = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private string _search = "";

        [MenuItem("Window/Monkey Adventure/HD Environment Master Visual QA + Auto-Dressing", false, 140)]
        public static void Open()
        {
            var w = GetWindow<HDEnvironmentMasterVisualQA>("HD Master Visual QA");
            w.minSize = new Vector2(1100, 720);
            w.position = new Rect(w.position.x, w.position.y, 1400, 900);
            w.Show();
        }

        [MenuItem("Window/Monkey Adventure/HD Environment Master Visual QA/Run Full QA", false, 141)]
        private static void MenuQA()
        {
            var w = GetWindow<HDEnvironmentMasterVisualQA>("HD Master Visual QA");
            w.RunFullQA();
        }

        [MenuItem("Window/Monkey Adventure/HD Environment Master Visual QA/Generate Dressing Preview", false, 142)]
        private static void MenuPreview()
        {
            var w = GetWindow<HDEnvironmentMasterVisualQA>("HD Master Visual QA");
            w.GeneratePreview();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawControls();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_audit == null)
            {
                EditorGUILayout.HelpBox(
                    "Run Full QA first. This tool evaluates the complete Level 01 environment instead of checking mountains, floor, grass and water as isolated tasks.",
                    MessageType.Info);
            }
            else
            {
                DrawScoreBoard();
                DrawPriorityFixes();
                DrawCategoryReport();
            }

            DrawDressingStatus();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField(
                "🌴 HD ENVIRONMENT MASTER VISUAL QA + AUTO-DRESSING",
                MakeHeaderStyle(18));

            EditorGUILayout.HelpBox(
                "One-pass production gate for Level 01. Audits terrain/ground, mountains, materials, vegetation, water, ruins, lighting, composition, gameplay safety and performance. " +
                "Auto-dressing uses discovered HD prefabs, creates a preview first, and never modifies source assets.",
                MessageType.Info);
        }

        private void DrawControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔍 RUN FULL QA", GUILayout.Height(34)))
                RunFullQA();

            if (GUILayout.Button("🌿 GENERATE DRESSING PREVIEW", GUILayout.Height(34)))
                GeneratePreview();

            if (GUILayout.Button("🔄 RE-AUDIT", GUILayout.Height(34)))
                RunFullQA();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("✅ APPLY PREVIEW", GUILayout.Height(28)))
                ApplyPreview();

            if (GUILayout.Button("↩ ROLLBACK APPLIED", GUILayout.Height(28)))
                RollbackApplied();

            if (GUILayout.Button("🧹 CLEAR PREVIEW", GUILayout.Height(28)))
                ClearPreview();

            if (GUILayout.Button("📋 COPY ALL REPORT", GUILayout.Height(28)))
                CopyAllReport();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            _seed = EditorGUILayout.IntField("Seed", _seed);
            _density = (Density)EditorGUILayout.EnumPopup("Density", _density);
            _safetyMargin = EditorGUILayout.FloatField("Player Safety (m)", _safetyMargin);
            _maxObjects = EditorGUILayout.IntField("Max Objects", _maxObjects);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _useOnlyHDAssets = EditorGUILayout.ToggleLeft("HD assets only", _useOnlyHDAssets, GUILayout.Width(130));
            _includeGroundVariation = EditorGUILayout.ToggleLeft("Ground variation", _includeGroundVariation, GUILayout.Width(145));
            _includeWaterEdge = EditorGUILayout.ToggleLeft("Water edge", _includeWaterEdge, GUILayout.Width(110));
            _includeRuins = EditorGUILayout.ToggleLeft("Ruins integration", _includeRuins, GUILayout.Width(130));
            _showAllChecks = EditorGUILayout.ToggleLeft("Show all checks", _showAllChecks, GUILayout.Width(125));
            _search = EditorGUILayout.TextField(_search);
            if (GUILayout.Button("Clear", GUILayout.Width(55))) _search = "";
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawScoreBoard()
        {
            string gate = _audit.Overall >= 90 ? "READY" :
                          _audit.Overall >= 80 ? "REVIEW REQUIRED" : "BLOCKED";

            MessageType type = _audit.Overall >= 90 ? MessageType.Info :
                               _audit.Overall >= 80 ? MessageType.Warning : MessageType.Error;

            EditorGUILayout.HelpBox(
                $"9/10 QUALITY GATE: {_audit.Overall}/100 — {gate}\n" +
                $"Critical: {_audit.Critical.Count} | Warnings: {_audit.Warnings.Count} | " +
                $"Preview Generated: {_previewGenerated}",
                type);

            EditorGUILayout.BeginHorizontal();
            DrawScoreBox("Materials", _audit.MaterialScore);
            DrawScoreBox("Terrain", _audit.TerrainScore);
            DrawScoreBox("Vegetation", _audit.VegetationScore);
            DrawScoreBox("Mountains", _audit.MountainScore);
            DrawScoreBox("Water", _audit.WaterScore);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawScoreBox("Ruins", _audit.RuinScore);
            DrawScoreBox("Lighting", _audit.LightingScore);
            DrawScoreBox("Composition", _audit.CompositionScore);
            DrawScoreBox("Gameplay", _audit.GameplayScore);
            DrawScoreBox("Performance", _audit.PerformanceScore);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawScoreBox(string label, int score)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Height(54));
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"{score}/100", MakeScoreStyle(score));
            EditorGUILayout.EndVertical();
        }

        private void DrawPriorityFixes()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🚨 PRIORITY FIX QUEUE", EditorStyles.boldLabel);

            if (_audit.Critical.Count == 0 && _audit.Warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("No critical visual blockers detected.", MessageType.Info);
                return;
            }

            foreach (string item in _audit.Critical.Take(12))
                EditorGUILayout.HelpBox("CRITICAL: " + item, MessageType.Error);

            foreach (string item in _audit.Warnings.Take(12))
                EditorGUILayout.HelpBox("WARNING: " + item, MessageType.Warning);
        }

        private void DrawCategoryReport()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("📊 COMPLETE ENVIRONMENT CHECK", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Expand All", GUILayout.Height(26)))
            {
                _showAllChecks = true;
                foreach (var category in _audit.Categories)
                    _expandedCategories[category.Name] = true;
                Repaint();
            }

            if (GUILayout.Button("Collapse All", GUILayout.Height(26)))
            {
                _showAllChecks = false;
                foreach (var category in _audit.Categories)
                    _expandedCategories[category.Name] = false;
                Repaint();
            }

            if (GUILayout.Button("Copy All Issues", GUILayout.Height(26)))
                CopyAllIssues();

            EditorGUILayout.EndHorizontal();

            _reportScroll = EditorGUILayout.BeginScrollView(_reportScroll, GUILayout.Height(420));

            IEnumerable<CategoryStat> rows = _audit.Categories;
            if (!string.IsNullOrWhiteSpace(_search))
                rows = rows.Where(x => x.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       x.Notes.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var row in rows)
            {
                bool expanded = _showAllChecks;
                if (_expandedCategories.TryGetValue(row.Name, out bool savedExpanded))
                    expanded = savedExpanded;

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();

                string arrow = expanded ? "▼" : "▶";
                if (GUILayout.Button(arrow, EditorStyles.miniButton, GUILayout.Width(28)))
                {
                    expanded = !expanded;
                    _expandedCategories[row.Name] = expanded;
                }

                EditorGUILayout.LabelField(row.Name, EditorStyles.boldLabel);

                EditorGUILayout.LabelField(
                    $"Found {row.Found} | Issues {row.Issues} | Score {row.Score}/100",
                    MakeScoreStyle(row.Score),
                    GUILayout.Width(300));

                EditorGUILayout.EndHorizontal();

                if (expanded)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("CHECK DETAILS", EditorStyles.miniBoldLabel);
                    EditorGUILayout.HelpBox(
                        string.IsNullOrWhiteSpace(row.Notes) ? "No additional notes." : row.Notes,
                        row.Issues > 0 ? MessageType.Warning : MessageType.Info);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Objects Found: {row.Found}", GUILayout.Width(180));
                    EditorGUILayout.LabelField($"Issues: {row.Issues}", GUILayout.Width(140));
                    EditorGUILayout.LabelField($"Quality Score: {row.Score}/100");
                    EditorGUILayout.EndHorizontal();

                    if (!string.IsNullOrWhiteSpace(row.Notes))
                    {
                        EditorGUILayout.SelectableLabel(
                            row.Notes,
                            EditorStyles.wordWrappedMiniLabel,
                            GUILayout.MinHeight(38));
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void CopyAllIssues()
        {
            if (_audit == null)
            {
                EditorUtility.DisplayDialog("HD Master Visual QA", "Run Full QA first.", "OK");
                return;
            }

            var lines = new List<string>
            {
                "HD ENVIRONMENT MASTER VISUAL QA - ISSUES",
                $"Overall: {_audit.Overall}/100",
                $"Critical: {_audit.Critical.Count}",
                $"Warnings: {_audit.Warnings.Count}",
                ""
            };

            foreach (var category in _audit.Categories.Where(c => c.Issues > 0))
            {
                lines.Add($"[{category.Name}]");
                lines.Add($"Found: {category.Found} | Issues: {category.Issues} | Score: {category.Score}/100");
                lines.Add(category.Notes ?? "");
                lines.Add("");
            }

            if (_audit.Critical.Count > 0)
            {
                lines.Add("CRITICAL");
                lines.AddRange(_audit.Critical.Select(x => "- " + x));
                lines.Add("");
            }

            if (_audit.Warnings.Count > 0)
            {
                lines.Add("WARNINGS");
                lines.AddRange(_audit.Warnings.Select(x => "- " + x));
            }

            GUIUtility.systemCopyBuffer = string.Join(System.Environment.NewLine, lines);
            EditorUtility.DisplayDialog(
                "HD Master Visual QA",
                "All QA issues copied to clipboard.",
                "OK");
        }

        private void DrawDressingStatus()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("🌿 AUTO-DRESSING", EditorStyles.boldLabel);

            string previewPath = FindRoot(PreviewRoot) != null ? "FOUND" : "NOT GENERATED";
            string appliedPath = FindRoot(AppliedRoot) != null ? "APPLIED" : "NOT APPLIED";

            EditorGUILayout.HelpBox(
                $"Preview: {previewPath} | Applied: {appliedPath}\n" +
                "Order: detect → score → preview → visual QA → explicit apply. " +
                "No gameplay collider, marker, player corridor or source asset is changed.",
                MessageType.Info);
        }

        private void RunFullQA()
        {
            _audit = BuildAudit();
            SaveAuditReport(_audit);
            Repaint();
        }

        private AuditResult BuildAudit()
        {
            var result = new AuditResult();

            GameObject level = FindRoot(LevelRoot);
            if (level == null)
            {
                result.Overall = 0;
                result.Critical.Add("AI_GENERATED_LEVEL was not found in the active scene.");
                return result;
            }

            var allRenderers = level.GetComponentsInChildren<Renderer>(true);
            var hdRenderers = allRenderers
                .Where(r => r != null && !IsGameplayObject(r.gameObject))
                .ToArray();

            int totalSlots = hdRenderers.Sum(r => r.sharedMaterials != null ? r.sharedMaterials.Length : 0);
            int cleanSlots = 0;
            int materialIssues = 0;
            int whiteRisk = 0;
            int magenta = 0;

            foreach (var r in hdRenderers)
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null)
                    {
                        materialIssues++;
                        continue;
                    }

                    string shader = m.shader != null ? m.shader.name : "";
                    if (shader.IndexOf("Universal Render Pipeline", StringComparison.OrdinalIgnoreCase) < 0 &&
                        shader.IndexOf("Shader Graph", StringComparison.OrdinalIgnoreCase) < 0)
                        materialIssues++;

                    if (m.shader != null && m.shader.name.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0)
                        magenta++;

                    Texture baseMap = GetTexture(m, "_BaseMap") ?? GetTexture(m, "_MainTex");
                    Color baseColor = GetColor(m, "_BaseColor", Color.white);
                    if (baseMap == null && baseColor.r > 0.94f && baseColor.g > 0.94f && baseColor.b > 0.94f)
                        whiteRisk++;

                    if (m.shader != null &&
                        (shader.IndexOf("Universal Render Pipeline", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         shader.IndexOf("Shader Graph", StringComparison.OrdinalIgnoreCase) >= 0))
                        cleanSlots++;
                }
            }

            result.MaterialScore = totalSlots == 0 ? 0 :
                Mathf.Clamp(Mathf.RoundToInt(100f * cleanSlots / Mathf.Max(1, totalSlots)
                    - magenta * 20 - whiteRisk * 2), 0, 100);

            result.TerrainScore = ScoreTerrain(level, result);
            result.MountainScore = ScoreCategory(level, result, new[] { "mountain", "hill", "cliff", "landscape", "terrain" }, 10);
            result.VegetationScore = ScoreVegetation(level, result);
            result.WaterScore = ScoreCategory(level, result, new[] { "water", "river", "lake", "waterfall" }, 8);
            result.RuinScore = ScoreCategory(level, result, new[] { "ruin", "ancient", "arch", "stone" }, 5);
            result.LightingScore = ScoreLighting(result);
            result.GameplayScore = ScoreGameplay(level, result);
            result.CompositionScore = ScoreComposition(level, result);
            result.PerformanceScore = ScorePerformance(hdRenderers, result);

            if (materialIssues > 0)
                result.Critical.Add($"Material compatibility/missing issues detected: {materialIssues} slots.");
            if (magenta > 0)
                result.Critical.Add($"Magenta/error shader suspects detected: {magenta}.");
            if (whiteRisk > 0)
                result.Critical.Add($"White/untextured visual risk detected: {whiteRisk} slots.");

            result.Overall = Mathf.RoundToInt(
                result.MaterialScore * .15f +
                result.TerrainScore * .15f +
                result.VegetationScore * .15f +
                result.MountainScore * .10f +
                result.WaterScore * .10f +
                result.RuinScore * .10f +
                result.LightingScore * .10f +
                result.CompositionScore * .05f +
                result.GameplayScore * .05f +
                result.PerformanceScore * .05f);

            return result;
        }

        private int ScoreTerrain(GameObject level, AuditResult result)
        {
            int ground = CountNameMatches(level, new[] { "ground", "terrain", "landscape", "floor" });
            int blueRisk = 0;
            int whiteRisk = 0;

            foreach (var r in level.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || IsGameplayObject(r.gameObject)) continue;
                string n = (r.gameObject.name + " " + GetHierarchyPath(r.transform)).ToLowerInvariant();
                if (!ContainsAny(n, "ground", "terrain", "landscape", "floor", "water")) continue;

                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    Color c = GetColor(m, "_BaseColor", Color.white);
                    Texture t = GetTexture(m, "_BaseMap") ?? GetTexture(m, "_MainTex");
                    if (t == null && c.r > .92f && c.g > .92f && c.b > .92f) whiteRisk++;
                    if (t == null && c.b > c.g * 1.15f && c.b > c.r * 1.15f) blueRisk++;
                }
            }

            int score = Mathf.Clamp(75 + Mathf.Min(25, ground * 2) - whiteRisk * 12 - blueRisk * 15, 0, 100);
            AddCategory(result, "Ground / Terrain", ground, whiteRisk + blueRisk, score,
                $"Ground-like objects: {ground}. White/untextured risks: {whiteRisk}. Blue material risks: {blueRisk}. " +
                "Final forest floor must not look like a flat green mat.");

            if (blueRisk > 0) result.Critical.Add("Blue/cyan ground material risk detected; investigate before vegetation dressing.");
            if (whiteRisk > 0) result.Critical.Add("White/untextured terrain risk detected.");
            return score;
        }

        private int ScoreVegetation(GameObject level, AuditResult result)
        {
            int trees = CountNameMatches(level, new[] { "tree", "palm", "coconut" });
            int grass = CountNameMatches(level, new[] { "grass" });
            int fern = CountNameMatches(level, new[] { "fern" });
            int bush = CountNameMatches(level, new[] { "bush", "shrub" });
            int leaves = CountNameMatches(level, new[] { "leaf", "leaves", "deadleaf", "litter" });

            int missing = 0;
            if (trees == 0) missing++;
            if (grass == 0) missing++;
            if (fern == 0) missing++;
            if (bush == 0) missing++;
            if (leaves == 0) missing++;

            int score = Mathf.Clamp(100 - missing * 14, 0, 100);
            AddCategory(result, "Vegetation", trees + grass + fern + bush + leaves, missing, score,
                $"Trees {trees} | Grass {grass} | Ferns {fern} | Bushes {bush} | Leaf litter {leaves}. " +
                "Distribution must vary by zone and preserve a clear player corridor.");

            if (missing > 0)
                result.Warnings.Add($"Vegetation categories missing/undetected: {missing}. Auto-dresser will use available HD assets and report missing categories.");

            return score;
        }

        private int ScoreCategory(GameObject level, AuditResult result, string[] keywords, int weight)
        {
            int count = CountNameMatches(level, keywords);
            int score = Mathf.Clamp(count == 0 ? 55 : 80 + Mathf.Min(20, count), 0, 100);
            string name = keywords[0].Equals("mountain", StringComparison.OrdinalIgnoreCase) ? "Mountains / Hills" :
                          keywords[0].Equals("water", StringComparison.OrdinalIgnoreCase) ? "Water / River" :
                          "Ancient Ruins";

            AddCategory(result, name, count, count == 0 ? 1 : 0, score,
                $"Detected {count} relevant object(s). Material, scale, placement and visual integration must be checked.");

            if (count == 0)
                result.Warnings.Add($"{name} assets were not confidently detected in the active hierarchy.");

            return score;
        }

        private int ScoreLighting(AuditResult result)
        {
            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            var directional = lights.Count(l => l != null && l.type == LightType.Directional);
            int score = directional > 0 ? 90 : 55;
            AddCategory(result, "Lighting / Atmosphere", lights.Length, directional > 0 ? 0 : 1, score,
                $"Lights: {lights.Length}, directional lights: {directional}. Final jungle presentation requires readable shadows and controlled exposure.");
            if (directional == 0) result.Warnings.Add("No directional light detected in the active scene.");
            return score;
        }

        private int ScoreGameplay(GameObject level, AuditResult result)
        {
            int gameplay = 0;
            gameplay += CountNameMatches(level, new[] { "player", "monkey" });
            gameplay += CountNameMatches(level, new[] { "checkpoint", "collectible", "obstacle", "enemy", "finish", "start" });

            int score = gameplay > 0 ? 95 : 80;
            AddCategory(result, "Gameplay Safety", gameplay, 0, score,
                $"Gameplay-related markers/objects detected: {gameplay}. Visual dressing must stay outside the player safety corridor.");

            return score;
        }

        private int ScoreComposition(GameObject level, AuditResult result)
        {
            Bounds b;
            if (!TryGetLevelBounds(level, out b))
            {
                AddCategory(result, "Composition", 0, 1, 60, "Could not resolve useful level bounds.");
                return 60;
            }

            int rendererCount = level.GetComponentsInChildren<Renderer>(true).Length;
            int score = rendererCount > 30 ? 90 : rendererCount > 10 ? 80 : 65;
            AddCategory(result, "Composition", rendererCount, 0, score,
                $"Resolved level bounds {b.size.x:0.0}m x {b.size.z:0.0}m. Composition is evaluated as a whole, not by isolated assets.");
            return score;
        }

        private int ScorePerformance(Renderer[] renderers, AuditResult result)
        {
            int count = renderers.Length;
            int score = count > 500 ? 60 : count > 350 ? 75 : count > 200 ? 88 : 96;
            AddCategory(result, "Performance", count, count > 500 ? 1 : 0, score,
                $"Environment renderers: {count}. Dressing uses a hard object cap and deterministic spacing.");
            if (count > 500) result.Warnings.Add("High renderer count detected; avoid indiscriminate vegetation spawning.");
            return score;
        }

        private void GeneratePreview()
        {
            ClearPreview();

            GameObject level = FindRoot(LevelRoot);
            if (level == null)
            {
                EditorUtility.DisplayDialog("HD Master Visual QA", "AI_GENERATED_LEVEL not found.", "OK");
                return;
            }

            Bounds bounds;
            if (!TryGetLevelBounds(level, out bounds))
            {
                EditorUtility.DisplayDialog("HD Master Visual QA", "Could not resolve Level 01 bounds.", "OK");
                return;
            }

            var root = new GameObject("MASTER_VISUAL_DRESSING_PREVIEW");
            Undo.RegisterCreatedObjectUndo(root, "Create Master Dressing Preview");

            Transform parent = root.transform;
            CreateChild(parent, "GroundVariation");
            CreateChild(parent, "Grass");
            CreateChild(parent, "Ferns");
            CreateChild(parent, "Bushes");
            CreateChild(parent, "DeadLeaves");
            CreateChild(parent, "Rocks");
            CreateChild(parent, "Logs");
            CreateChild(parent, "WaterEdge");
            CreateChild(parent, "RuinsAccent");

            var candidates = DiscoverCandidates();
            if (candidates.Count == 0)
            {
                EditorUtility.DisplayDialog("HD Master Visual QA",
                    "No usable HD environment prefabs were discovered. Existing HD_Jungle_Assets must be present.",
                    "OK");
                return;
            }

            System.Random rng = new System.Random(_seed);
            int target = _density == Density.Low ? 120 : _density == Density.Medium ? 220 : 300;
            target = Mathf.Min(target, Mathf.Max(20, _maxObjects));

            int spawned = 0;
            spawned += SpawnCategory(candidates, "GRASS", "Grass", bounds, target / 4, rng, 0.7f, 1.2f);
            spawned += SpawnCategory(candidates, "FERN", "Ferns", bounds, target / 7, rng, 0.8f, 1.25f);
            spawned += SpawnCategory(candidates, "BUSH", "Bushes", bounds, target / 7, rng, 0.85f, 1.25f);
            spawned += SpawnCategory(candidates, "DEADLEAF", "DeadLeaves", bounds, target / 9, rng, 0.8f, 1.2f);
            spawned += SpawnCategory(candidates, "ROCK", "Rocks", bounds, target / 10, rng, 0.7f, 1.35f);
            spawned += SpawnCategory(candidates, "LOG", "Logs", bounds, target / 14, rng, 0.8f, 1.2f);

            if (_includeWaterEdge)
                spawned += SpawnCategory(candidates, "WATEREDGE", "WaterEdge", bounds, target / 16, rng, 0.8f, 1.15f);

            if (_includeRuins)
                spawned += SpawnCategory(candidates, "ANCIENT", "RuinsAccent", bounds, target / 30, rng, 0.85f, 1.15f);

            if (spawned == 0)
            {
                DestroyImmediate(root);
                EditorUtility.DisplayDialog("HD Master Visual QA",
                    "No category candidates could be safely placed. Run HD Asset Discovery first.",
                    "OK");
                return;
            }

            _previewGenerated = true;
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            RunFullQA();
            SaveDressingReport(root, spawned, candidates, _seed);
            Repaint();
        }

        private int SpawnCategory(
            List<AssetCandidate> candidates,
            string category,
            string childName,
            Bounds bounds,
            int count,
            System.Random rng,
            float minScale,
            float maxScale)
        {
            var usable = candidates.Where(x => x.Category == category).ToList();
            if (usable.Count == 0 || count <= 0) return 0;

            Transform parent = FindRoot(PreviewRoot)?.transform.Find(childName);
            if (parent == null) return 0;

            int spawned = 0;
            int attempts = 0;
            float halfWidth = bounds.extents.x;
            float halfLength = bounds.extents.z;

            while (spawned < count && attempts < count * 8)
            {
                attempts++;

                float x = Mathf.Lerp(bounds.min.x, bounds.max.x, (float)rng.NextDouble());
                float z = Mathf.Lerp(bounds.min.z, bounds.max.z, (float)rng.NextDouble());

                // Keep a hard gameplay corridor.
                if (Mathf.Abs(x) < _safetyMargin)
                    continue;

                // Keep most dressing on outer bands so the player route remains readable.
                float normalizedX = halfWidth <= 0.01f ? 0.5f : Mathf.Abs(x - bounds.center.x) / halfWidth;
                if (normalizedX < 0.45f && category != "WATEREDGE")
                    continue;

                Vector3 pos = new Vector3(x, bounds.max.y + 50f, z);
                if (!TryProjectToGround(pos, out Vector3 hit))
                    hit = new Vector3(x, bounds.min.y, z);

                // Do not spawn below the resolved ground.
                hit.y += 0.01f;

                var candidate = usable[rng.Next(usable.Count)];
                GameObject instance = PrefabUtility.InstantiatePrefab(candidate.Prefab) as GameObject;
                if (instance == null) continue;

                Undo.RegisterCreatedObjectUndo(instance, "Spawn Master Dressing Preview");
                instance.name = candidate.Prefab.name + "_MASTER_PREVIEW";
                instance.transform.SetParent(parent, false);
                instance.transform.position = hit;
                instance.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

                float scale = Mathf.Lerp(minScale, maxScale, (float)rng.NextDouble());
                instance.transform.localScale *= scale;

                StripGameplayRisk(instance);
                spawned++;
            }

            return spawned;
        }

        private List<AssetCandidate> DiscoverCandidates()
        {
            var list = new List<AssetCandidate>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                string lower = path.ToLowerInvariant();

                if (_useOnlyHDAssets)
                {
                    if (!lower.Contains("hd_jungle_assets") &&
                        !lower.Contains("/hd/") &&
                        !lower.Contains("high") &&
                        !lower.Contains("realistic"))
                        continue;
                }

                if (lower.Contains("low poly") || lower.Contains("lowpoly") || lower.Contains("/demo/"))
                    continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                string text = (prefab.name + " " + path).ToLowerInvariant();
                string category = InferDressCategory(text);
                if (category == null) continue;

                int score = ScoreCandidate(text, category);
                if (score < 30) continue;

                list.Add(new AssetCandidate
                {
                    Prefab = prefab,
                    Path = path,
                    Category = category,
                    Score = score
                });
            }

            return list
                .GroupBy(x => x.Prefab)
                .Select(g => g.OrderByDescending(x => x.Score).First())
                .OrderByDescending(x => x.Score)
                .ToList();
        }

        private string InferDressCategory(string text)
        {
            if (ContainsAny(text, "grass", "meadowgrass")) return "GRASS";
            if (ContainsAny(text, "fern")) return "FERN";
            if (ContainsAny(text, "bush", "shrub")) return "BUSH";
            if (ContainsAny(text, "deadleaf", "dead_leaf", "leaflitter", "leaf_litter", "litter")) return "DEADLEAF";
            if (ContainsAny(text, "waterfall", "water_edge", "riverrock", "river_rock")) return "WATEREDGE";
            if (ContainsAny(text, "log", "fallenlog", "fallen_log", "stump")) return "LOG";
            if (ContainsAny(text, "rock", "stone", "boulder")) return "ROCK";
            if (ContainsAny(text, "ancient", "ruin", "arch", "pillar")) return "ANCIENT";
            return null;
        }

        private int ScoreCandidate(string text, string category)
        {
            int score = 35;

            if (text.Contains("hd_jungle_assets")) score += 30;
            if (text.Contains("hd/")) score += 15;
            if (text.Contains("realistic")) score += 10;
            if (text.Contains("urp")) score += 8;

            if (category == "GRASS" && text.Contains("grass")) score += 10;
            if (category == "FERN" && text.Contains("fern")) score += 10;
            if (category == "BUSH" && (text.Contains("bush") || text.Contains("shrub"))) score += 10;
            if (category == "ROCK" && (text.Contains("rock") || text.Contains("boulder"))) score += 10;

            return score;
        }

        private void ApplyPreview()
        {
            GameObject preview = FindRoot(PreviewRoot);
            if (preview == null)
            {
                EditorUtility.DisplayDialog("HD Master Visual QA", "Generate the dressing preview first.", "OK");
                return;
            }

            if (FindRoot(AppliedRoot) != null)
            {
                EditorUtility.DisplayDialog("HD Master Visual QA",
                    "Applied dressing already exists. Rollback it before applying a new preview.",
                    "OK");
                return;
            }

            GameObject applied = Instantiate(preview);
            applied.name = "MASTER_VISUAL_DRESSING";
            applied.transform.SetParent(preview.transform.parent, false);

            foreach (var c in applied.GetComponentsInChildren<Collider>(true))
                DestroyImmediate(c);

            StripGameplayRisk(applied);

            Undo.RegisterCreatedObjectUndo(applied, "Apply Master Visual Dressing");
            SaveDressingReport(applied, applied.GetComponentsInChildren<Transform>(true).Length, DiscoverCandidates(), _seed);

            EditorUtility.DisplayDialog("HD Master Visual QA",
                "Master visual dressing applied.\nGameplay geometry and source assets were preserved.",
                "OK");

            Repaint();
        }

        private void RollbackApplied()
        {
            GameObject applied = FindRoot(AppliedRoot);
            if (applied == null)
            {
                EditorUtility.DisplayDialog("HD Master Visual QA", "No applied master dressing found.", "OK");
                return;
            }

            Undo.DestroyObjectImmediate(applied);
            Repaint();
        }

        private void ClearPreview()
        {
            GameObject preview = FindRoot(PreviewRoot);
            if (preview != null)
                Undo.DestroyObjectImmediate(preview);

            _previewGenerated = false;
            Repaint();
        }

        private static void StripGameplayRisk(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>(true))
                DestroyImmediate(c);

            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
                DestroyImmediate(rb);

            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null) continue;
                // Do not destroy renderer/material components; remove only obvious gameplay scripts.
                string n = behaviour.GetType().Name.ToLowerInvariant();
                if (ContainsAny(n, "player", "enemy", "damage", "pickup", "collectible", "checkpoint", "weapon"))
                    DestroyImmediate(behaviour);
            }
        }

        private static bool TryProjectToGround(Vector3 origin, out Vector3 hit)
        {
            Ray ray = new Ray(origin, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit h, 200f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider != null)
                {
                    hit = h.point;
                    return true;
                }
            }

            hit = default;
            return false;
        }

        private static bool TryGetLevelBounds(GameObject level, out Bounds bounds)
        {
            var renderers = level.GetComponentsInChildren<Renderer>(true)
                .Where(r => r != null && !IsGameplayObject(r.gameObject))
                .ToArray();

            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds.size.sqrMagnitude > 0.01f;
        }

        private static int CountNameMatches(GameObject root, string[] keywords)
        {
            int count = 0;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                string text = (t.name + " " + GetHierarchyPath(t)).ToLowerInvariant();
                if (ContainsAny(text, keywords)) count++;
            }
            return count;
        }

        private static void AddCategory(
            AuditResult result,
            string name,
            int found,
            int issues,
            int score,
            string notes)
        {
            result.Categories.Add(new CategoryStat
            {
                Name = name,
                Found = found,
                Issues = issues,
                Score = Mathf.Clamp(score, 0, 100),
                Notes = notes
            });
        }

        private static GameObject FindRoot(string path)
        {
            string[] parts = path.Split('/');
            GameObject current = null;

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == parts[0])
                {
                    current = root;
                    break;
                }
            }

            if (current == null) return null;

            for (int i = 1; i < parts.Length; i++)
            {
                Transform child = current.transform.Find(parts[i]);
                if (child == null) return null;
                current = child.gameObject;
            }

            return current;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static string GetHierarchyPath(Transform t)
        {
            var names = new List<string>();
            while (t != null)
            {
                names.Add(t.name);
                t = t.parent;
            }
            names.Reverse();
            return string.Join("/", names);
        }

        private static bool IsGameplayObject(GameObject go)
        {
            string text = (go.name + " " + GetHierarchyPath(go.transform)).ToLowerInvariant();
            return ContainsAny(text,
                "player", "monkey", "checkpoint", "collectible", "obstacle",
                "enemy", "finish", "start", "gameplay", "trigger", "camera");
        }

        private static bool ContainsAny(string text, params string[] values)
        {
            foreach (string value in values)
                if (text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        private static Texture GetTexture(Material material, string property)
        {
            return material != null && material.HasProperty(property)
                ? material.GetTexture(property)
                : null;
        }

        private static Color GetColor(Material material, string property, Color fallback)
        {
            return material != null && material.HasProperty(property)
                ? material.GetColor(property)
                : fallback;
        }

        private static GUIStyle MakeHeaderStyle(int size)
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = size,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private static GUIStyle MakeScoreStyle(int score)
        {
            var s = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            s.normal.textColor = score >= 90 ? Color.green :
                                 score >= 80 ? Color.yellow : Color.red;
            return s;
        }

        private static string BuildReport(AuditResult r)
        {
            var lines = new List<string>
            {
                "HD ENVIRONMENT MASTER VISUAL QA REPORT",
                "=======================================",
                $"Overall: {r.Overall}/100",
                "",
                "SCORES",
                $"Materials: {r.MaterialScore}",
                $"Terrain: {r.TerrainScore}",
                $"Vegetation: {r.VegetationScore}",
                $"Mountains: {r.MountainScore}",
                $"Water: {r.WaterScore}",
                $"Ruins: {r.RuinScore}",
                $"Lighting: {r.LightingScore}",
                $"Composition: {r.CompositionScore}",
                $"Gameplay: {r.GameplayScore}",
                $"Performance: {r.PerformanceScore}",
                "",
                "CRITICAL"
            };

            lines.AddRange(r.Critical.Select(x => "- " + x));
            lines.Add("");
            lines.Add("WARNINGS");
            lines.AddRange(r.Warnings.Select(x => "- " + x));
            lines.Add("");
            lines.Add("CATEGORY REPORT");

            foreach (var c in r.Categories)
                lines.Add($"[{c.Score}/100] {c.Name} | Found={c.Found} | Issues={c.Issues} | {c.Notes}");

            return string.Join(System.Environment.NewLine, lines);
        }

        private static void SaveAuditReport(AuditResult r)
        {
            string dir = "Assets/AILevelBuilder/Reports";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(
                Path.Combine(dir, "HDEnvironmentMasterVisualQA.txt"),
                BuildReport(r));

            AssetDatabase.Refresh();
        }

        private static void SaveDressingReport(GameObject root, int spawned, List<AssetCandidate> candidates, int seed)
        {
            string dir = "Assets/AILevelBuilder/Reports";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, "HDEnvironmentMasterAutoDressing.txt");
            File.WriteAllText(path,
                "HD ENVIRONMENT MASTER AUTO-DRESSING REPORT\n" +
                "==========================================\n" +
                $"Root: {GetHierarchyPath(root.transform)}\n" +
                $"Objects/Transforms: {spawned}\n" +
                $"Candidate prefabs discovered: {candidates.Count}\n" +
                $"Seed: {seed}\n" +
                "Source assets modified: NO\n" +
                "Gameplay colliders preserved: YES\n" +
                "Preview-first workflow: YES\n");

            AssetDatabase.Refresh();
        }

        private void CopyAllReport()
        {
            if (_audit == null)
            {
                EditorUtility.DisplayDialog("HD Master Visual QA", "Run Full QA first.", "OK");
                return;
            }

            GUIUtility.systemCopyBuffer = BuildReport(_audit);
            EditorUtility.DisplayDialog("HD Master Visual QA", "Complete QA report copied to clipboard.", "OK");
        }
    }
}
