using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MonkeyAdventure.AILevelBuilder
{
    /// <summary>
    /// Severity level of an individual validation finding.
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Overall health status of the level validation scan.
    /// </summary>
    public enum ValidationOverallStatus
    {
        NotRun,
        Pass,
        PassWithWarnings,
        Failed
    }

    /// <summary>
    /// Represents an individual issue, warning, or informational note produced by the validator.
    /// </summary>
    [Serializable]
    public class ValidationIssue
    {
        public ValidationSeverity severity;
        public string category;
        public string objectName;
        public string message;
        public Vector3 worldPosition;
        public GameObject targetGameObject;

        public ValidationIssue(ValidationSeverity severity, string category, string objectName, string message, Vector3 worldPosition, GameObject targetGameObject = null)
        {
            this.severity = severity;
            this.category = category;
            this.objectName = objectName;
            this.message = message;
            this.worldPosition = worldPosition;
            this.targetGameObject = targetGameObject;
        }
    }

    /// <summary>
    /// Detailed validation report containing metrics, counts, and findings list.
    /// </summary>
    [Serializable]
    public class ValidationReport
    {
        public ValidationOverallStatus overallStatus = ValidationOverallStatus.NotRun;
        public DateTime timestamp = DateTime.Now;

        public int errorCount = 0;
        public int warningCount = 0;
        public int infoCount = 0;
        public int totalObjectsScanned = 0;

        public float levelLength = 0f;
        public float playableWidth = 0f;
        public Vector3 startPosition = Vector3.zero;
        public Vector3 checkpointPosition = Vector3.zero;
        public Vector3 finishPosition = Vector3.zero;
        public Bounds levelBounds = new Bounds();

        public List<ValidationIssue> issues = new List<ValidationIssue>();

        public void AddIssue(ValidationSeverity severity, string category, string objectName, string message, Vector3 worldPosition, GameObject targetGameObject = null)
        {
            issues.Add(new ValidationIssue(severity, category, objectName, message, worldPosition, targetGameObject));
            if (severity == ValidationSeverity.Error) errorCount++;
            else if (severity == ValidationSeverity.Warning) warningCount++;
            else if (severity == ValidationSeverity.Info) infoCount++;
        }

        public void FinalizeStatus()
        {
            if (errorCount > 0)
            {
                overallStatus = ValidationOverallStatus.Failed;
            }
            else if (warningCount > 0)
            {
                overallStatus = ValidationOverallStatus.PassWithWarnings;
            }
            else
            {
                overallStatus = ValidationOverallStatus.Pass;
            }
        }
    }

    /// <summary>
    /// Automated Level 01 ('The Awakening') blockout validator.
    /// Non-destructively inspects the AI_GENERATED_LEVEL hierarchy in the active scene
    /// to ensure structural integrity, gameplay readiness, and component validity.
    /// </summary>
    public static class Level01AutoValidator
    {
        public const string REPORT_DEFAULT_PATH = "Assets/AILevelBuilder/Reports/Level01_ValidationReport.txt";

        /// <summary>
        /// Validates the active AI_GENERATED_LEVEL in the scene.
        /// </summary>
        public static ValidationReport ValidateActiveLevel()
        {
            ValidationReport report = new ValidationReport
            {
                timestamp = DateTime.Now
            };

            // A. LEVEL ROOT
            GameObject root = GameObject.Find(LevelGenerator.ROOT_NAME);
            if (root == null)
            {
                report.AddIssue(ValidationSeverity.Error, "Root", LevelGenerator.ROOT_NAME,
                    $"Root object '{LevelGenerator.ROOT_NAME}' not found in active scene. Generate Level 1 first.", Vector3.zero);
                report.FinalizeStatus();
                return report;
            }

            report.AddIssue(ValidationSeverity.Info, "Root", root.name, $"Found active level root '{root.name}'.", root.transform.position, root);

            // Subfolders validation
            Transform envFolder = root.transform.Find(LevelGenerator.ENV_FOLDER);
            Transform startFolder = root.transform.Find(LevelGenerator.START_FOLDER);
            Transform finishFolder = root.transform.Find(LevelGenerator.FINISH_FOLDER);
            Transform collectiblesFolder = root.transform.Find(LevelGenerator.COLLECTIBLES_FOLDER);
            Transform obstaclesFolder = root.transform.Find(LevelGenerator.OBSTACLES_FOLDER);
            Transform enemiesFolder = root.transform.Find(LevelGenerator.ENEMIES_FOLDER);
            Transform checkpointsFolder = root.transform.Find(LevelGenerator.CHECKPOINTS_FOLDER);

            CheckFolder(envFolder, LevelGenerator.ENV_FOLDER, report, root);
            CheckFolder(startFolder, LevelGenerator.START_FOLDER, report, root);
            CheckFolder(finishFolder, LevelGenerator.FINISH_FOLDER, report, root);
            CheckFolder(collectiblesFolder, LevelGenerator.COLLECTIBLES_FOLDER, report, root);
            CheckFolder(obstaclesFolder, LevelGenerator.OBSTACLES_FOLDER, report, root);
            CheckFolder(enemiesFolder, LevelGenerator.ENEMIES_FOLDER, report, root);
            CheckFolder(checkpointsFolder, LevelGenerator.CHECKPOINTS_FOLDER, report, root);

            // Calculate overall Bounds & Scan all objects for NaN/Transform errors
            CalculateBoundsAndCheckTransforms(root, report);

            // B. PLAYER START
            ValidatePlayerStart(startFolder, report);

            // C. FINISH
            ValidateFinish(finishFolder, report);

            // D. CHECKPOINTS
            ValidateCheckpoints(checkpointsFolder, report);

            // E. GROUND CONTINUITY
            ValidateGroundContinuity(envFolder, report);

            // F. COLLECTIBLES
            ValidateCollectibles(collectiblesFolder, report);

            // G. OBSTACLES
            ValidateObstacles(obstaclesFolder, report);

            // H. ENEMIES
            ValidateEnemies(enemiesFolder, report);

            // I. RIVER & CROSSING
            ValidateRiverCrossing(envFolder, report);

            // Finalize
            report.FinalizeStatus();
            Debug.Log($"<color=#00FF88><b>[Level01AutoValidator] Validation Complete: {report.overallStatus} (Errors: {report.errorCount}, Warnings: {report.warningCount}, Info: {report.infoCount})</b></color>");

            return report;
        }

        #region Validation Modules

        private static void CheckFolder(Transform folder, string expectedName, ValidationReport report, GameObject root)
        {
            if (folder == null)
            {
                report.AddIssue(ValidationSeverity.Warning, "Hierarchy", expectedName, $"Category folder '{expectedName}' is missing under '{LevelGenerator.ROOT_NAME}'.", root.transform.position, root);
            }
            else
            {
                report.AddIssue(ValidationSeverity.Info, "Hierarchy", folder.name, $"Category folder '{folder.name}' is present with {folder.childCount} child object(s).", folder.position, folder.gameObject);
            }
        }

        private static void CalculateBoundsAndCheckTransforms(GameObject root, ValidationReport report)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);
            report.totalObjectsScanned = allTransforms.Length;

            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    b.Encapsulate(renderers[i].bounds);
                }
                report.levelBounds = b;
                report.levelLength = b.size.z;
                report.playableWidth = b.size.x;

                report.AddIssue(ValidationSeverity.Info, "Bounds", "LevelBounds",
                    $"Level dimensions: Length = {b.size.z:F1}m, Width = {b.size.x:F1}m, Height = {b.size.y:F1}m (Min Z: {b.min.z:F1}m, Max Z: {b.max.z:F1}m).",
                    b.center, root);
            }

            // Check every object for Transform issues or missing components
            foreach (Transform t in allTransforms)
            {
                Vector3 pos = t.position;
                Vector3 rot = t.eulerAngles;
                Vector3 scale = t.localScale;

                if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z) ||
                    float.IsInfinity(pos.x) || float.IsInfinity(pos.y) || float.IsInfinity(pos.z))
                {
                    report.AddIssue(ValidationSeverity.Error, "Transform", t.name, $"Invalid NaN/Infinity position detected on object '{t.name}'.", Vector3.zero, t.gameObject);
                }

                if (scale.x <= 0.001f || scale.y <= 0.001f || scale.z <= 0.001f)
                {
                    report.AddIssue(ValidationSeverity.Warning, "Transform", t.name, $"Extremely small or zero scale detected ({scale}) on object '{t.name}'.", t.position, t.gameObject);
                }

                // Check for missing MonoBehaviours
                Component[] comps = t.GetComponents<Component>();
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i] == null)
                    {
                        report.AddIssue(ValidationSeverity.Warning, "Components", t.name, $"Missing script component detected on GameObject '{t.name}'.", t.position, t.gameObject);
                    }
                }
            }
        }

        private static void ValidatePlayerStart(Transform startFolder, ValidationReport report)
        {
            GameObject startMarker = null;
            if (startFolder != null)
            {
                Transform t = startFolder.Find("PlayerStart");
                if (t != null) startMarker = t.gameObject;
            }

            if (startMarker == null)
            {
                LevelMarker[] markers = UnityEngine.Object.FindObjectsByType<LevelMarker>(FindObjectsInactive.Include);
                foreach (var m in markers)
                {
                    if (m.MarkerType == LevelMarkerType.Start)
                    {
                        startMarker = m.gameObject;
                        break;
                    }
                }
            }

            if (startMarker == null)
            {
                report.AddIssue(ValidationSeverity.Error, "Start", "PlayerStart", "PlayerStart marker not found in generated hierarchy.", Vector3.zero);
                return;
            }

            report.startPosition = startMarker.transform.position;
            report.AddIssue(ValidationSeverity.Info, "Start", startMarker.name, $"PlayerStart marker confirmed at position {startMarker.transform.position}.", startMarker.transform.position, startMarker);

            // Active player check
            GameObject player = GameObject.Find("Monkey_B3 (1)") ?? GameObject.FindWithTag("Player");
            if (player != null)
            {
                float distToStart = Vector3.Distance(player.transform.position, startMarker.transform.position);
                if (distToStart > 20f)
                {
                    report.AddIssue(ValidationSeverity.Warning, "Player", player.name,
                        $"Active player '{player.name}' is located {distToStart:F1}m away from Start point at {player.transform.position}.", player.transform.position, player);
                }
                else
                {
                    report.AddIssue(ValidationSeverity.Info, "Player", player.name,
                        $"Active player '{player.name}' correctly positioned at Start ({player.transform.position}).", player.transform.position, player);
                }

                CharacterController cc = player.GetComponent<CharacterController>();
                if (cc != null && !cc.enabled)
                {
                    report.AddIssue(ValidationSeverity.Warning, "Player", player.name, "CharacterController on active player is currently disabled.", player.transform.position, player);
                }
            }
            else
            {
                report.AddIssue(ValidationSeverity.Info, "Player", "PlayerObject", "No active Player GameObject found in scene root; start marker ready for runtime spawn.", startMarker.transform.position);
            }
        }

        private static void ValidateFinish(Transform finishFolder, ValidationReport report)
        {
            GameObject finishMarker = null;
            if (finishFolder != null)
            {
                Transform t = finishFolder.Find("Finish_Gateway") ?? finishFolder.Find("Finish_Portal");
                if (t != null) finishMarker = t.gameObject;
            }

            if (finishMarker == null)
            {
                LevelMarker[] markers = UnityEngine.Object.FindObjectsByType<LevelMarker>(FindObjectsInactive.Include);
                foreach (var m in markers)
                {
                    if (m.MarkerType == LevelMarkerType.Finish)
                    {
                        finishMarker = m.gameObject;
                        break;
                    }
                }
            }

            if (finishMarker == null)
            {
                report.AddIssue(ValidationSeverity.Error, "Finish", "Finish_Gateway", "Finish marker not found in generated hierarchy.", Vector3.zero);
                return;
            }

            report.finishPosition = finishMarker.transform.position;
            if (finishMarker.transform.position.z <= report.startPosition.z)
            {
                report.AddIssue(ValidationSeverity.Error, "Finish", finishMarker.name,
                    $"Finish marker Z position ({finishMarker.transform.position.z:F1}) is not ahead of Start Z position ({report.startPosition.z:F1}).", finishMarker.transform.position, finishMarker);
            }
            else
            {
                report.AddIssue(ValidationSeverity.Info, "Finish", finishMarker.name,
                    $"Finish portal confirmed at position {finishMarker.transform.position} ({finishMarker.transform.position.z - report.startPosition.z:F1}m from Start).", finishMarker.transform.position, finishMarker);
            }
        }

        private static void ValidateCheckpoints(Transform checkpointsFolder, ValidationReport report)
        {
            List<LevelMarker> cpMarkers = new List<LevelMarker>();
            if (checkpointsFolder != null)
            {
                LevelMarker[] markers = checkpointsFolder.GetComponentsInChildren<LevelMarker>(true);
                foreach (var m in markers)
                {
                    if (m.MarkerType == LevelMarkerType.Checkpoint) cpMarkers.Add(m);
                }
            }

            if (cpMarkers.Count == 0)
            {
                report.AddIssue(ValidationSeverity.Warning, "Checkpoint", "Checkpoints", "No checkpoint markers detected in Level 01.", report.startPosition);
                return;
            }

            report.checkpointPosition = cpMarkers[0].transform.position;
            foreach (var cp in cpMarkers)
            {
                Vector3 pos = cp.transform.position;
                if (pos.z <= report.startPosition.z || pos.z >= report.finishPosition.z)
                {
                    report.AddIssue(ValidationSeverity.Warning, "Checkpoint", cp.name,
                        $"Checkpoint '{cp.name}' Z position ({pos.z:F1}) is outside the start-finish range ({report.startPosition.z:F1} to {report.finishPosition.z:F1}).", pos, cp.gameObject);
                }
                else
                {
                    report.AddIssue(ValidationSeverity.Info, "Checkpoint", cp.name,
                        $"Checkpoint '{cp.name}' verified at {pos} (Z = {pos.z:F1}m).", pos, cp.gameObject);
                }
            }
        }

        private static void ValidateGroundContinuity(Transform envFolder, ValidationReport report)
        {
            if (envFolder == null) return;

            Transform groundFolder = envFolder.Find("Ground");
            if (groundFolder == null)
            {
                report.AddIssue(ValidationSeverity.Warning, "Ground", "GroundFolder", "Ground folder missing under Environment.", envFolder.position, envFolder.gameObject);
                return;
            }

            List<Collider> groundColliders = new List<Collider>();
            foreach (Transform child in groundFolder)
            {
                Collider col = child.GetComponent<Collider>();
                if (col != null) groundColliders.Add(col);
            }

            if (groundColliders.Count == 0)
            {
                report.AddIssue(ValidationSeverity.Error, "Ground", "GroundSections", "No ground colliders found in Environment/Ground folder.", groundFolder.position, groundFolder.gameObject);
                return;
            }

            // Sort by Z position to verify walkable flow
            groundColliders.Sort((a, b) => a.bounds.min.z.CompareTo(b.bounds.min.z));

            for (int i = 0; i < groundColliders.Count; i++)
            {
                Collider current = groundColliders[i];
                Vector3 cPos = current.transform.position;

                if (cPos.y < -5f)
                {
                    report.AddIssue(ValidationSeverity.Warning, "Ground", current.name,
                        $"Ground section '{current.name}' placed unusually low at Y = {cPos.y:F1}m.", cPos, current.gameObject);
                }

                if (i < groundColliders.Count - 1)
                {
                    Collider next = groundColliders[i + 1];
                    float gapZ = next.bounds.min.z - current.bounds.max.z;

                    // Large gaps (> 16m) outside crossing area flag as warning
                    if (gapZ > 16.0f)
                    {
                        report.AddIssue(ValidationSeverity.Warning, "Ground", current.name,
                            $"Large gap of {gapZ:F1}m detected between ground section '{current.name}' and '{next.name}'.", current.bounds.center, current.gameObject);
                    }

                    float stepY = Mathf.Abs(next.bounds.center.y - current.bounds.center.y);
                    if (stepY > 2.5f)
                    {
                        report.AddIssue(ValidationSeverity.Warning, "Ground", current.name,
                            $"Vertical elevation step of {stepY:F1}m between '{current.name}' and '{next.name}' exceeds jump clearance.", current.bounds.center, current.gameObject);
                    }
                }
            }

            report.AddIssue(ValidationSeverity.Info, "Ground", "GroundContinuity",
                $"Validated {groundColliders.Count} walkable ground sections along the level course.", groundFolder.position, groundFolder.gameObject);
        }

        private static void ValidateCollectibles(Transform collectiblesFolder, ValidationReport report)
        {
            if (collectiblesFolder == null) return;

            Collider[] colliders = collectiblesFolder.GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
            {
                report.AddIssue(ValidationSeverity.Warning, "Collectibles", "FruitItems", "No collectible items found in Collectibles folder.", collectiblesFolder.position, collectiblesFolder.gameObject);
                return;
            }

            int validCount = 0;
            foreach (var col in colliders)
            {
                Vector3 pos = col.transform.position;
                if (!col.isTrigger)
                {
                    report.AddIssue(ValidationSeverity.Warning, "Collectibles", col.name,
                        $"Collectible item '{col.name}' collider is not marked as Trigger.", pos, col.gameObject);
                }

                if (pos.y < -0.2f)
                {
                    report.AddIssue(ValidationSeverity.Warning, "Collectibles", col.name,
                        $"Collectible '{col.name}' is placed below floor level (Y = {pos.y:F2}m).", pos, col.gameObject);
                }
                else
                {
                    validCount++;
                }
            }

            report.AddIssue(ValidationSeverity.Info, "Collectibles", "FruitCount",
                $"Verified {validCount} collectible item(s) placed along the path.", collectiblesFolder.position, collectiblesFolder.gameObject);
        }

        private static void ValidateObstacles(Transform obstaclesFolder, ValidationReport report)
        {
            if (obstaclesFolder == null) return;

            Collider[] colliders = obstaclesFolder.GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
            {
                report.AddIssue(ValidationSeverity.Info, "Obstacles", "ObstacleCount", "No obstacle objects present in level.", obstaclesFolder.position, obstaclesFolder.gameObject);
                return;
            }

            foreach (var col in colliders)
            {
                float height = col.bounds.size.y;
                if (height > 1.8f)
                {
                    report.AddIssue(ValidationSeverity.Warning, "Obstacles", col.name,
                        $"Obstacle '{col.name}' height ({height:F2}m) is high and may challenge standard jump height (~2.2m).", col.bounds.center, col.gameObject);
                }
                else
                {
                    report.AddIssue(ValidationSeverity.Info, "Obstacles", col.name,
                        $"Obstacle '{col.name}' height ({height:F2}m) is within standard jumpable clearance.", col.bounds.center, col.gameObject);
                }
            }
        }

        private static void ValidateEnemies(Transform enemiesFolder, ValidationReport report)
        {
            if (enemiesFolder == null) return;

            int enemyCount = enemiesFolder.childCount;
            if (enemyCount == 0)
            {
                report.AddIssue(ValidationSeverity.Info, "Enemies", "EnemyCount", "No enemy encounter placeholders in level.", enemiesFolder.position, enemiesFolder.gameObject);
                return;
            }

            foreach (Transform enemy in enemiesFolder)
            {
                Vector3 pos = enemy.position;
                float distToStart = Vector3.Distance(pos, report.startPosition);
                if (distToStart < 15f)
                {
                    report.AddIssue(ValidationSeverity.Warning, "Enemies", enemy.name,
                        $"Enemy '{enemy.name}' is placed too close to PlayerStart ({distToStart:F1}m).", pos, enemy.gameObject);
                }

                if (pos.y < -0.2f)
                {
                    report.AddIssue(ValidationSeverity.Warning, "Enemies", enemy.name,
                        $"Enemy '{enemy.name}' is sunken below ground (Y = {pos.y:F2}m).", pos, enemy.gameObject);
                }
                else
                {
                    report.AddIssue(ValidationSeverity.Info, "Enemies", enemy.name,
                        $"Enemy '{enemy.name}' verified at position {pos} (Z = {pos.z:F1}m).", pos, enemy.gameObject);
                }
            }
        }

        private static void ValidateRiverCrossing(Transform envFolder, ValidationReport report)
        {
            if (envFolder == null) return;

            Transform waterFolder = envFolder.Find("Water");
            Transform crossingFolder = envFolder.Find("Crossing");

            if (waterFolder == null && crossingFolder == null)
            {
                report.AddIssue(ValidationSeverity.Info, "Crossing", "RiverSection", "No river or water crossing present in this blockout.", envFolder.position, envFolder.gameObject);
                return;
            }

            if (waterFolder != null)
            {
                report.AddIssue(ValidationSeverity.Info, "Crossing", "WaterChannel", $"River water channel verified with {waterFolder.childCount} element(s).", waterFolder.position, waterFolder.gameObject);
            }

            if (crossingFolder != null)
            {
                int stoneCount = crossingFolder.childCount;
                if (stoneCount == 0)
                {
                    report.AddIssue(ValidationSeverity.Warning, "Crossing", "SteppingStones", "River crossing folder exists but contains no stepping stones.", crossingFolder.position, crossingFolder.gameObject);
                }
                else
                {
                    report.AddIssue(ValidationSeverity.Info, "Crossing", "SteppingStones", $"River crossing verified with {stoneCount} stepping stone(s) bridging the river.", crossingFolder.position, crossingFolder.gameObject);
                }
            }
        }

        #endregion

        #region Report File Export

        /// <summary>
        /// Saves a human-readable text report of the validation results to disk.
        /// </summary>
        public static string SaveValidationReport(ValidationReport report, string filePath = REPORT_DEFAULT_PATH)
        {
            if (report == null) return "";

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (StreamWriter sw = new StreamWriter(filePath, false))
                {
                    sw.WriteLine("================================================================================");
                    sw.WriteLine("MONKEY ADVENTURE — AI LEVEL BUILDER");
                    sw.WriteLine("LEVEL 01: THE AWAKENING — AUTOMATED VALIDATION REPORT");
                    sw.WriteLine("================================================================================");
                    sw.WriteLine($"Generated: {report.timestamp:yyyy-MM-dd HH:mm:ss}");
                    sw.WriteLine($"Overall Status: {report.overallStatus.ToString().ToUpper()} (Errors: {report.errorCount}, Warnings: {report.warningCount}, Info: {report.infoCount})");
                    sw.WriteLine();
                    sw.WriteLine("--------------------------------------------------------------------------------");
                    sw.WriteLine("LEVEL METRICS & STATISTICS");
                    sw.WriteLine("--------------------------------------------------------------------------------");
                    sw.WriteLine($"- Level Length: {report.levelLength:F1} m");
                    sw.WriteLine($"- Playable Width: {report.playableWidth:F1} m");
                    sw.WriteLine($"- Start Position: ({report.startPosition.x:F2}, {report.startPosition.y:F2}, {report.startPosition.z:F2})");
                    sw.WriteLine($"- Checkpoint Position: ({report.checkpointPosition.x:F2}, {report.checkpointPosition.y:F2}, {report.checkpointPosition.z:F2})");
                    sw.WriteLine($"- Finish Position: ({report.finishPosition.x:F2}, {report.finishPosition.y:F2}, {report.finishPosition.z:F2})");
                    sw.WriteLine($"- Total Objects Scanned: {report.totalObjectsScanned}");
                    sw.WriteLine($"- Errors: {report.errorCount}");
                    sw.WriteLine($"- Warnings: {report.warningCount}");
                    sw.WriteLine($"- Info Notes: {report.infoCount}");
                    sw.WriteLine();
                    sw.WriteLine("--------------------------------------------------------------------------------");
                    sw.WriteLine("VALIDATION FINDINGS");
                    sw.WriteLine("--------------------------------------------------------------------------------");

                    foreach (var issue in report.issues)
                    {
                        string sevTag = issue.severity.ToString().ToUpper();
                        sw.WriteLine($"[{sevTag}] [{issue.category}] {issue.objectName}: {issue.message} @ ({issue.worldPosition.x:F1}, {issue.worldPosition.y:F1}, {issue.worldPosition.z:F1})");
                    }

                    sw.WriteLine();
                    sw.WriteLine("--------------------------------------------------------------------------------");
                    sw.WriteLine("FINAL RECOMMENDATION");
                    sw.WriteLine("--------------------------------------------------------------------------------");
                    if (report.errorCount == 0)
                    {
                        sw.WriteLine("READY FOR HD ASSET PASS");
                    }
                    else
                    {
                        sw.WriteLine("FIX BLOCKOUT ISSUES FIRST");
                    }
                    sw.WriteLine("================================================================================");
                }

                Debug.Log($"<color=#00FF88><b>[Level01AutoValidator] Successfully saved report to '{filePath}'.</b></color>");
                return filePath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Level01AutoValidator] Failed to save report: {ex.Message}");
                return "";
            }
        }

        #endregion
    }
}
