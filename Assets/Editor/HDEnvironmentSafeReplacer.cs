using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Safe Level 01 environment-only HD visual pass.
    ///
    /// Scope:
    ///   - Trees, rocks, plants and ruins only.
    ///   - Does NOT touch characters, enemies, gameplay props, scripts, or gameplay colliders.
    ///   - Keeps the original scene GameObject as the gameplay/reference anchor.
    ///   - Instantiates the HD prefab as a visual sibling and disables only the original renderers.
    ///   - HD instance colliders are disabled so original collision stays authoritative.
    ///
    /// No assets are downloaded or created by this tool. It only uses existing project prefabs.
    /// </summary>
    public sealed class HDEnvironmentSafeReplacer : EditorWindow
    {
        private const string EnvironmentRootName = "[--- 01_ENVIRONMENT ---]";
        private const string VisualPrefix = "__HDVISUAL__";
        private const string SceneName = "Level01_Awakening";

        private static readonly Mapping[] Mappings =
        {
            new Mapping("Tree_CoconutPalm", "Assets/Art/Environment/HD/Trees/HD_Tree_CoconutPalm_01.prefab", "tree", 95, "[--- 01_ENVIRONMENT ---]"),
            new Mapping("Tree_JungleCanopy", "Assets/Art/Environment/HD/Trees/HD_Tree_JungleCanopy_01.prefab", "tree", 95, "[--- 01_ENVIRONMENT ---]"),
            new Mapping("Rock_MossyBoulder", "Assets/Art/Environment/HD/Rocks/HD_Rock_MossyBoulder_01.prefab", "rock", 95, "[--- 01_ENVIRONMENT ---]"),
            new Mapping("Plant_HibiscusFlower", "Assets/Art/Environment/HD/Plants/HD_Plant_FloweringBush_01.prefab", "plant", 90, "[--- 01_ENVIRONMENT ---]"),
            new Mapping("Plant_JungleFern", "Assets/Art/Environment/HD/Plants/HD_Plant_JungleFern_01.prefab", "plant", 95, "[--- 01_ENVIRONMENT ---]"),
            new Mapping("Plant_TropicalBush", "Assets/Art/Environment/HD/Plants/HD_Plant_TropicalBush_01.prefab", "plant", 90, "[--- 01_ENVIRONMENT ---]"),
            new Mapping("Plant_GlowingMushroom", "Assets/Art/Environment/HD/Plants/HD_Plant_FloweringBush_01.prefab", "plant", 90, "[--- 01_ENVIRONMENT ---]"),
            new Mapping("Prop_HollowFallenLog", "Assets/Art/Environment/HD/Trees/HD_Tree_FallenLog_01.prefab", "tree", 95, "[--- 01_ENVIRONMENT ---]"),
            new Mapping("Ruins_AncientArch", "Assets/Art/Environment/HD/Ruins/HD_Ruin_AncientArch_01.prefab", "ruins", 95, "[--- 02_GAMEPLAY ---]"),
            new Mapping("Ruins_RunePedestal", "Assets/Art/Environment/HD/Ruins/HD_Ruin_RunePedestal_01.prefab", "ruins", 95, "[--- 02_GAMEPLAY ---]"),
            new Mapping("Ruins_HeavyStoneDoor", "Assets/Art/Environment/HD/Ruins/HD_Ruin_AncientArch_01.prefab", "ruins", 90, "[--- 02_GAMEPLAY ---]"),
        };

        private struct Mapping
        {
            public readonly string RootName;
            public readonly string PrefabPath;
            public readonly string Category;
            public readonly int PlannerScore;
            public readonly string SectionName;

            public Mapping(string rootName, string prefabPath, string category, int plannerScore, string sectionName)
            {
                RootName = rootName;
                PrefabPath = prefabPath;
                Category = category;
                PlannerScore = plannerScore;
                SectionName = sectionName;
            }
        }

        private Vector2 _scroll;
        private bool _includeInactive = true;
        private bool _showExistingVisuals = true;

        [MenuItem("Window/Monkey Adventure/HD Environment Safe Replacer")]
        public static void Open()
        {
            var window = GetWindow<HDEnvironmentSafeReplacer>("HD Environment Safe Replacer");
            window.minSize = new Vector2(520, 500);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("HD Environment Safe Replacer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "LEVEL 01 ONLY. Environment-only visual pass: Trees + Rocks + Plants + Ruins.\n\n" +
                "Characters, enemies, gameplay props, scripts and original gameplay colliders are NOT replaced. " +
                "The original scene objects remain as anchors; only their renderers are hidden after a successful HD visual instance is created.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            _includeInactive = EditorGUILayout.ToggleLeft("Include inactive environment roots", _includeInactive);
            _showExistingVisuals = EditorGUILayout.ToggleLeft("Count existing HD visual instances", _showExistingVisuals);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Approved mappings", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(285));
            foreach (var m in Mappings)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(m.RootName + "  →  " + m.PrefabPath, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField($"Category: {m.Category}   Planner score: {m.PlannerScore}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("PREVIEW", GUILayout.Height(34)))
                    Preview();

                if (GUILayout.Button("APPLY SAFE HD ENVIRONMENT PASS", GUILayout.Height(34)))
                    Apply();
            }

            EditorGUILayout.Space(5);
            if (GUILayout.Button("REMOVE HD VISUALS / RESTORE ORIGINAL RENDERERS", GUILayout.Height(30)))
                Restore();
        }

        private static Transform FindEnvironmentRoot()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return null;

            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || EditorUtility.IsPersistent(go)) continue;
                if (go.scene != scene) continue;
                if (go.transform.parent != null) continue;
                if (go.name == EnvironmentRootName) return go.transform;
            }

            // Some generated scenes may use a non-root environment section.
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || EditorUtility.IsPersistent(go)) continue;
                if (go.scene != scene) continue;
                if (go.name == EnvironmentRootName) return go.transform;
            }

            return null;
        }

        private List<GameObject> FindTargets(Transform envRoot)
        {
            var targets = new List<GameObject>();
            var scene = SceneManager.GetActiveScene();

            foreach (var m in Mappings)
            {
                Transform section = FindSection(scene, m.SectionName);
                if (section == null) continue;

                foreach (Transform child in section.GetComponentsInChildren<Transform>(true))
                {
                    if (child == null || child.parent != section) continue;
                    if (child.name != m.RootName) continue;
                    if (!_includeInactive && !child.gameObject.activeInHierarchy) continue;

                    // Ruins live under the gameplay section in the current Level 01 scene.
                    // Only allow a ruins root when it is purely visual: no scripts and no colliders.
                    if (m.Category == "ruins" && !IsPureVisualEnvironmentRoot(child.gameObject))
                        continue;

                    targets.Add(child.gameObject);
                }
            }

            return targets;
        }

        private static List<GameObject> FindTargetsIncludingInactive(Transform ignored)
        {
            var targets = new List<GameObject>();
            var scene = SceneManager.GetActiveScene();

            foreach (var m in Mappings)
            {
                Transform section = FindSection(scene, m.SectionName);
                if (section == null) continue;

                foreach (Transform child in section.GetComponentsInChildren<Transform>(true))
                {
                    if (child == null || child.parent != section) continue;
                    if (child.name != m.RootName) continue;
                    if (m.Category == "ruins" && !IsPureVisualEnvironmentRoot(child.gameObject)) continue;
                    targets.Add(child.gameObject);
                }
            }

            return targets;
        }

        private static Transform FindSection(Scene scene, string sectionName)
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || EditorUtility.IsPersistent(go)) continue;
                if (go.scene != scene) continue;
                if (go.name != sectionName) continue;
                if (go.transform.parent == null) return go.transform;
            }

            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || EditorUtility.IsPersistent(go)) continue;
                if (go.scene != scene) continue;
                if (go.name == sectionName) return go.transform;
            }

            return null;
        }

        private static bool IsPureVisualEnvironmentRoot(GameObject root)
        {
            if (root == null) return false;
            if (root.GetComponentsInChildren<MonoBehaviour>(true).Length > 0) return false;
            if (root.GetComponentsInChildren<Collider>(true).Length > 0) return false;
            return true;
        }

        private void Preview()
        {
            var envRoot = FindEnvironmentRoot();
            if (envRoot == null)
            {
                EditorUtility.DisplayDialog("HD Environment Safe Replacer",
                    $"Could not find {EnvironmentRootName} in the active scene.\n\n" +
                    "Open Level01_Awakening.unity first.", "OK");
                return;
            }

            var targets = FindTargets(envRoot);
            int existing = 0;
            foreach (var go in targets)
            {
                foreach (Transform child in go.transform.parent.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.StartsWith(VisualPrefix, StringComparison.Ordinal)) existing++;
                }
            }

            var missingPrefabs = new List<string>();
            foreach (var m in Mappings)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(m.PrefabPath) == null)
                    missingPrefabs.Add(m.PrefabPath);
            }

            string missing = missingPrefabs.Count == 0
                ? "All mapped prefabs exist."
                : "Missing prefabs:\n- " + string.Join("\n- ", missingPrefabs);

            EditorUtility.DisplayDialog("HD Environment Preview",
                $"Scene: {SceneManager.GetActiveScene().name}\n" +
                $"Environment roots found: {targets.Count}\n" +
                $"Existing HD visual instances: {existing}\n\n" +
                missing +
                "\n\nNo scene changes were made.", "OK");
        }

        private void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.name != SceneName)
            {
                EditorUtility.DisplayDialog("HD Environment Safe Replacer",
                    $"Active scene must be {SceneName}.unity.\n\nCurrent scene: {scene.name}", "OK");
                return;
            }

            var envRoot = FindEnvironmentRoot();
            if (envRoot == null)
            {
                EditorUtility.DisplayDialog("HD Environment Safe Replacer",
                    $"Could not find {EnvironmentRootName}.", "OK");
                return;
            }

            var targets = FindTargets(envRoot);
            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("HD Environment Safe Replacer",
                    "No approved environment roots were found.\n\nNothing was changed.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Apply Safe HD Environment Pass",
                    "This will modify the active Level 01 scene.\n\n" +
                    "Only approved environment roots will receive visual HD instances. " +
                    "Original scene objects and gameplay colliders remain in place.\n\n" +
                    "Characters, enemies and gameplay props are excluded.",
                    "APPLY", "CANCEL"))
                return;

            int applied = 0;
            int skipped = 0;
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Level 01 HD Environment Pass");

            foreach (var target in targets)
            {
                var mapping = FindMapping(target.name);
                if (mapping == null)
                {
                    skipped++;
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(mapping.Value.PrefabPath);
                if (prefab == null)
                {
                    Debug.LogWarning($"[HD Environment] Missing prefab: {mapping.Value.PrefabPath}");
                    skipped++;
                    continue;
                }

                // Avoid duplicate HD visuals for this exact target.
                var markerName = VisualPrefix + target.name;
                Transform existing = target.transform.parent.Find(markerName);
                if (existing != null)
                {
                    if (!existing.gameObject.activeSelf)
                        existing.gameObject.SetActive(true);

                    DisableOriginalRenderers(target);
                    DisableColliders(existing.gameObject);
                    applied++;
                    continue;
                }

                var instance = PrefabUtility.InstantiatePrefab(prefab, target.transform.parent) as GameObject;
                if (instance == null)
                {
                    Debug.LogWarning($"[HD Environment] Could not instantiate {mapping.Value.PrefabPath}");
                    skipped++;
                    continue;
                }

                Undo.RegisterCreatedObjectUndo(instance, "Create HD environment visual");
                instance.name = markerName;
                instance.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
                instance.transform.localScale = target.transform.localScale;

                // Original object remains authoritative for colliders/gameplay references.
                DisableColliders(instance);
                DisableOriginalRenderers(target);

                EditorUtility.SetDirty(target);
                EditorUtility.SetDirty(instance);
                applied++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("HD Environment Safe Replacer",
                $"Done.\n\nHD visuals applied: {applied}\nSkipped: {skipped}\n\n" +
                "Original environment objects remain in the scene as gameplay/reference anchors.", "OK");
        }

        private void Restore()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            int removed = 0;
            int restored = 0;
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Restore Level 01 Original Environment");

            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go == null || EditorUtility.IsPersistent(go)) continue;
                if (go.scene != scene) continue;
                if (!go.name.StartsWith(VisualPrefix, StringComparison.Ordinal)) continue;

                Undo.DestroyObjectImmediate(go);
                removed++;
            }

            var envRoot = FindEnvironmentRoot();
            if (envRoot != null)
            {
                foreach (var target in FindTargetsIncludingInactive(envRoot))
                {
                    EnableOriginalRenderers(target);
                    restored++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.DisplayDialog("HD Environment Safe Replacer",
                $"Restored original environment visuals.\n\nHD visual instances removed: {removed}\nOriginal roots restored: {restored}", "OK");
        }

        private static Mapping? FindMapping(string rootName)
        {
            foreach (var m in Mappings)
                if (m.RootName == rootName) return m;
            return null;
        }

        private static void DisableOriginalRenderers(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                Undo.RecordObject(r, "Disable original environment renderer");
                r.enabled = false;
            }
        }

        private static void EnableOriginalRenderers(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                Undo.RecordObject(r, "Restore original environment renderer");
                r.enabled = true;
            }
        }

        private static void DisableColliders(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>(true))
            {
                if (c == null) continue;
                c.enabled = false;
            }
        }
    }
}
