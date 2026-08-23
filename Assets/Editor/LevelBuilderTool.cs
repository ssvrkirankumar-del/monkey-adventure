using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Custom Unity Editor Window Tool to procedurally generate level segments,
    /// ground paths, jungle decorations, collectibles, and enemy layouts.
    /// Accessible via: Window > Monkey Adventure > Level Builder
    /// </summary>
    public class LevelBuilderTool : EditorWindow
    {
        [Header("Prefab References")]
        [SerializeField] private GameObject groundTilePrefab;
        [SerializeField] private GameObject obstaclePrefab;
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private GameObject bananaPrefab;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject treePrefab;

        [Header("Path Generation Parameters")]
        [SerializeField] private int numberOfTiles = 20;
        [SerializeField] private float tileLength = 5.0f;
        [SerializeField] private float pathWidth = 4.0f;
        [SerializeField] private Vector3 startPosition = Vector3.zero;

        [Header("Decoration Settings")]
        [SerializeField] private bool spawnTrees = true;
        [SerializeField] [Range(0.1f, 1f)] private float treeSpawnChance = 0.75f;
        [SerializeField] private float treeSideOffset = 4.5f;

        [Header("Gameplay Elements Distribution")]
        [SerializeField] private int coinCount = 25;
        [SerializeField] private int bananaCount = 10;
        [SerializeField] private int obstacleCount = 6;
        [SerializeField] private int enemyCount = 4;

        // Editor Scroll position & foldouts
        private Vector2 _scrollPos;
        private bool _showPrefabs = true;
        private bool _showPathSettings = true;
        private bool _showGameplaySettings = true;
        private GameObject _lastGeneratedLevelRoot;

        [MenuItem("Window/Monkey Adventure/Level Builder", false, 100)]
        public static void OpenWindow()
        {
            LevelBuilderTool window = GetWindow<LevelBuilderTool>("Level Builder", true);
            window.minSize = new Vector2(380, 560);
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeader();

            EditorGUILayout.Space(10);
            DrawPrefabsSection();

            EditorGUILayout.Space(10);
            DrawPathSettingsSection();

            EditorGUILayout.Space(10);
            DrawGameplaySettingsSection();

            EditorGUILayout.Space(15);
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.Space(6);
            GUILayout.Label("🌴 Monkey Jungle - Level Builder 🌴", headerStyle);
            EditorGUILayout.HelpBox("Procedurally assemble level paths, jungle foliage, coins, and enemies in seconds.", MessageType.Info);
        }

        private void DrawPrefabsSection()
        {
            _showPrefabs = EditorGUILayout.BeginFoldoutHeaderGroup(_showPrefabs, "1. Prefab Slots");
            if (_showPrefabs)
            {
                EditorGUI.indentLevel++;
                groundTilePrefab = (GameObject)EditorGUILayout.ObjectField("Ground Tile", groundTilePrefab, typeof(GameObject), false);
                treePrefab = (GameObject)EditorGUILayout.ObjectField("Tree / Foliage", treePrefab, typeof(GameObject), false);
                coinPrefab = (GameObject)EditorGUILayout.ObjectField("Coin Collectible", coinPrefab, typeof(GameObject), false);
                bananaPrefab = (GameObject)EditorGUILayout.ObjectField("Banana (Food)", bananaPrefab, typeof(GameObject), false);
                obstaclePrefab = (GameObject)EditorGUILayout.ObjectField("Obstacle / Spike", obstaclePrefab, typeof(GameObject), false);
                enemyPrefab = (GameObject)EditorGUILayout.ObjectField("Predator / Enemy", enemyPrefab, typeof(GameObject), false);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawPathSettingsSection()
        {
            _showPathSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showPathSettings, "2. Path & Decoration Parameters");
            if (_showPathSettings)
            {
                EditorGUI.indentLevel++;
                numberOfTiles = EditorGUILayout.IntSlider("Number of Tiles", numberOfTiles, 5, 100);
                tileLength = EditorGUILayout.FloatField("Tile Length (Z)", tileLength);
                pathWidth = EditorGUILayout.FloatField("Path Width (X)", pathWidth);
                startPosition = EditorGUILayout.Vector3Field("Start Position", startPosition);

                float totalLength = numberOfTiles * tileLength;
                EditorGUILayout.LabelField("Total Path Length:", $"{totalLength:F1} meters", EditorStyles.boldLabel);

                EditorGUILayout.Space(4);
                spawnTrees = EditorGUILayout.Toggle("Spawn Edge Trees", spawnTrees);
                if (spawnTrees)
                {
                    treeSpawnChance = EditorGUILayout.Slider("Tree Density", treeSpawnChance, 0.1f, 1f);
                    treeSideOffset = EditorGUILayout.FloatField("Tree Offset (X)", treeSideOffset);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawGameplaySettingsSection()
        {
            _showGameplaySettings = EditorGUILayout.BeginFoldoutHeaderGroup(_showGameplaySettings, "3. Gameplay Spawner Parameters");
            if (_showGameplaySettings)
            {
                EditorGUI.indentLevel++;
                coinCount = EditorGUILayout.IntSlider("Coins to Scatter", coinCount, 0, 100);
                bananaCount = EditorGUILayout.IntSlider("Bananas to Scatter", bananaCount, 0, 50);
                obstacleCount = EditorGUILayout.IntSlider("Obstacles to Place", obstacleCount, 0, 30);
                enemyCount = EditorGUILayout.IntSlider("Enemies to Spawn", enemyCount, 0, 20);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawActionButtons()
        {
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
            if (GUILayout.Button("🚀 Generate Full Level Segment (1-Click)", GUILayout.Height(38)))
            {
                GenerateCompleteLevel();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🧱 Generate Path Only", GUILayout.Height(28)))
            {
                GeneratePathOnly();
            }

            if (GUILayout.Button("✨ Scatter Coins & Enemies", GUILayout.Height(28)))
            {
                ScatterGameplayOnExisting();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            if (_lastGeneratedLevelRoot != null)
            {
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("🗑️ Delete Last Generated Level", GUILayout.Height(24)))
                {
                    Undo.DestroyObjectImmediate(_lastGeneratedLevelRoot);
                    _lastGeneratedLevelRoot = null;
                }
                GUI.backgroundColor = Color.white;
            }
        }

        #region Procedural Generation Core Logic
        private void GenerateCompleteLevel()
        {
            if (groundTilePrefab == null)
            {
                EditorUtility.DisplayDialog("Missing Prefab", "Please assign a Ground Tile Prefab before generating!", "OK");
                return;
            }

            // 1. Create Root Level Object
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            GameObject levelRoot = new GameObject($"GeneratedLevel_{timestamp}");
            levelRoot.transform.position = startPosition;
            Undo.RegisterCreatedObjectUndo(levelRoot, "Generate Complete Level");

            _lastGeneratedLevelRoot = levelRoot;

            // Sub-folders for clean hierarchy
            GameObject pathFolder = new GameObject("01_Ground_Path");
            pathFolder.transform.SetParent(levelRoot.transform, false);

            GameObject decorFolder = new GameObject("02_Foliage_Trees");
            decorFolder.transform.SetParent(levelRoot.transform, false);

            GameObject gameplayFolder = new GameObject("03_Collectibles_Gameplay");
            gameplayFolder.transform.SetParent(levelRoot.transform, false);

            // 2. Generate Ground Tiles & Trees
            List<Vector3> tilePositions = new List<Vector3>();

            for (int i = 0; i < numberOfTiles; i++)
            {
                Vector3 tilePos = startPosition + new Vector3(0, 0, i * tileLength);
                tilePositions.Add(tilePos);

                // Instantiate Tile
                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(groundTilePrefab, pathFolder.transform);
                tile.transform.position = tilePos;
                tile.transform.rotation = Quaternion.identity;

                // Spawn Left & Right Trees
                if (spawnTrees && treePrefab != null)
                {
                    // Left tree
                    if (UnityEngine.Random.value <= treeSpawnChance)
                    {
                        Vector3 leftPos = tilePos + new Vector3(-treeSideOffset + UnityEngine.Random.Range(-0.5f, 0.5f), 0, UnityEngine.Random.Range(-tileLength * 0.4f, tileLength * 0.4f));
                        SpawnTree(leftPos, decorFolder.transform);
                    }

                    // Right tree
                    if (UnityEngine.Random.value <= treeSpawnChance)
                    {
                        Vector3 rightPos = tilePos + new Vector3(treeSideOffset + UnityEngine.Random.Range(-0.5f, 0.5f), 0, UnityEngine.Random.Range(-tileLength * 0.4f, tileLength * 0.4f));
                        SpawnTree(rightPos, decorFolder.transform);
                    }
                }
            }

            // 3. Scatter Gameplay (Coins, Bananas, Obstacles, Enemies)
            float pathMinZ = startPosition.z + tileLength * 0.5f;
            float pathMaxZ = startPosition.z + (numberOfTiles - 1) * tileLength;
            float halfWidth = (pathWidth * 0.5f) - 0.5f;

            // Coins (in lines / arcs)
            if (coinPrefab != null)
            {
                for (int c = 0; c < coinCount; c++)
                {
                    Vector3 pos = new Vector3(
                        startPosition.x + UnityEngine.Random.Range(-halfWidth, halfWidth),
                        startPosition.y + 0.6f,
                        UnityEngine.Random.Range(pathMinZ, pathMaxZ)
                    );
                    GameObject coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, gameplayFolder.transform);
                    coin.transform.position = pos;
                }
            }

            // Bananas
            if (bananaPrefab != null)
            {
                for (int b = 0; b < bananaCount; b++)
                {
                    Vector3 pos = new Vector3(
                        startPosition.x + UnityEngine.Random.Range(-halfWidth, halfWidth),
                        startPosition.y + 0.6f,
                        UnityEngine.Random.Range(pathMinZ, pathMaxZ)
                    );
                    GameObject banana = (GameObject)PrefabUtility.InstantiatePrefab(bananaPrefab, gameplayFolder.transform);
                    banana.transform.position = pos;
                }
            }

            // Obstacles
            if (obstaclePrefab != null)
            {
                for (int o = 0; o < obstacleCount; o++)
                {
                    Vector3 pos = new Vector3(
                        startPosition.x + UnityEngine.Random.Range(-halfWidth * 0.8f, halfWidth * 0.8f),
                        startPosition.y,
                        UnityEngine.Random.Range(pathMinZ, pathMaxZ)
                    );
                    GameObject obs = (GameObject)PrefabUtility.InstantiatePrefab(obstaclePrefab, gameplayFolder.transform);
                    obs.transform.position = pos;
                    obs.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 4) * 90f, 0);
                }
            }

            // Enemies
            if (enemyPrefab != null)
            {
                for (int e = 0; e < enemyCount; e++)
                {
                    Vector3 pos = new Vector3(
                        startPosition.x + UnityEngine.Random.Range(-halfWidth * 0.6f, halfWidth * 0.6f),
                        startPosition.y,
                        UnityEngine.Random.Range(pathMinZ + 5f, pathMaxZ)
                    );
                    GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, gameplayFolder.transform);
                    enemy.transform.position = pos;
                    enemy.transform.rotation = Quaternion.Euler(0, 180f, 0); // Face towards starting player
                }
            }

            Selection.activeGameObject = levelRoot;
            Debug.Log($"[LevelBuilderTool] Successfully generated '{levelRoot.name}' with {numberOfTiles} tiles ({numberOfTiles * tileLength}m)!");
        }

        private void SpawnTree(Vector3 position, Transform parent)
        {
            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, parent);
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
            float scaleMultiplier = UnityEngine.Random.Range(0.85f, 1.25f);
            tree.transform.localScale = Vector3.one * scaleMultiplier;
        }

        private void GeneratePathOnly()
        {
            if (groundTilePrefab == null)
            {
                EditorUtility.DisplayDialog("Missing Prefab", "Please assign a Ground Tile Prefab first!", "OK");
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            GameObject levelRoot = new GameObject($"GeneratedPath_{timestamp}");
            levelRoot.transform.position = startPosition;
            Undo.RegisterCreatedObjectUndo(levelRoot, "Generate Path Only");

            _lastGeneratedLevelRoot = levelRoot;

            for (int i = 0; i < numberOfTiles; i++)
            {
                Vector3 tilePos = startPosition + new Vector3(0, 0, i * tileLength);
                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(groundTilePrefab, levelRoot.transform);
                tile.transform.position = tilePos;
                tile.transform.rotation = Quaternion.identity;
            }

            Selection.activeGameObject = levelRoot;
        }

        private void ScatterGameplayOnExisting()
        {
            if (_lastGeneratedLevelRoot == null)
            {
                _lastGeneratedLevelRoot = Selection.activeGameObject;
                if (_lastGeneratedLevelRoot == null)
                {
                    EditorUtility.DisplayDialog("Select Level", "Please select a Generated Level object in the Hierarchy or generate one first!", "OK");
                    return;
                }
            }

            float pathMinZ = startPosition.z + tileLength * 0.5f;
            float pathMaxZ = startPosition.z + (numberOfTiles - 1) * tileLength;
            float halfWidth = (pathWidth * 0.5f) - 0.5f;

            Transform targetParent = _lastGeneratedLevelRoot.transform;

            // Scatter Coins
            if (coinPrefab != null)
            {
                for (int c = 0; c < coinCount; c++)
                {
                    Vector3 pos = new Vector3(
                        startPosition.x + UnityEngine.Random.Range(-halfWidth, halfWidth),
                        startPosition.y + 0.6f,
                        UnityEngine.Random.Range(pathMinZ, pathMaxZ)
                    );
                    GameObject coin = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab, targetParent);
                    coin.transform.position = pos;
                    Undo.RegisterCreatedObjectUndo(coin, "Scatter Coin");
                }
            }

            // Scatter Enemies
            if (enemyPrefab != null)
            {
                for (int e = 0; e < enemyCount; e++)
                {
                    Vector3 pos = new Vector3(
                        startPosition.x + UnityEngine.Random.Range(-halfWidth * 0.6f, halfWidth * 0.6f),
                        startPosition.y,
                        UnityEngine.Random.Range(pathMinZ + 5f, pathMaxZ)
                    );
                    GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, targetParent);
                    enemy.transform.position = pos;
                    enemy.transform.rotation = Quaternion.Euler(0, 180f, 0);
                    Undo.RegisterCreatedObjectUndo(enemy, "Scatter Enemy");
                }
            }

            Debug.Log("[LevelBuilderTool] Successfully scattered coins and enemies!");
        }
        #endregion
    }
}
