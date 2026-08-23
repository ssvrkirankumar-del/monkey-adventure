using System.Collections;
using UnityEngine;

namespace MonkeyAdventure.Tutorial
{
    /// <summary>
    /// Invisible trigger collider placed in Level 1.
    /// Triggers specific tutorial action prompts (Run, Jump, Hang/Climb, Fight, Escape)
    /// with slow-motion time dilation, camera angle adjustments, and enemy/predator spawns.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Tutorial/Tutorial Trigger")]
    [RequireComponent(typeof(Collider))]
    public class TutorialTrigger : MonoBehaviour
    {
        [Header("Tutorial Action Type")]
        [SerializeField] private TutorialActionType actionType = TutorialActionType.Run;

        [Header("Custom Prompt Text")]
        [SerializeField] private string promptTitle = "MOVE FORWARD";
        [TextArea(2, 4)]
        [SerializeField] private string promptInstruction = "Use the Left Joystick to move your monkey through the jungle.";

        [Header("Settings")]
        [Tooltip("Slow down game time when entering this trigger.")]
        [SerializeField] private bool slowDownTime = true;

        [Tooltip("Trigger only once and disable.")]
        [SerializeField] private bool triggerOnce = true;

        [Tooltip("Auto dismiss prompt after X seconds (0 = wait for player input).")]
        [SerializeField] private float autoDismissSeconds = 4.0f;

        [Header("Action: Escape (Front View Mode)")]
        [Tooltip("Forces camera to look from the front facing the monkey.")]
        [SerializeField] private bool enableFrontViewCamera = false;
        [SerializeField] private Transform frontViewCameraPosition;
        [SerializeField] private GameObject predatorToSpawnPrefab;
        [SerializeField] private Transform predatorSpawnPoint;

        [Header("Action: Fight (Enemy Spawn)")]
        [SerializeField] private GameObject trainingEnemyPrefab;
        [SerializeField] private Transform enemySpawnPoint;

        [Header("Visual Indicator (Editor Only)")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.9f, 0.1f, 0.4f);

        private bool _hasTriggered = false;
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered && triggerOnce) return;

            // Check if player entered trigger
            if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
            {
                _hasTriggered = true;
                ExecuteTriggerSequence(other.gameObject);
            }
        }

        private void ExecuteTriggerSequence(GameObject playerObj)
        {
            // 1. Show UI Prompt and Slow Time via TutorialManager
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.ShowTutorialPrompt(
                    promptTitle,
                    promptInstruction,
                    actionType,
                    slowDownTime,
                    autoDismissSeconds
                );
            }

            // 2. Action-Specific Handling
            switch (actionType)
            {
                case TutorialActionType.Escape:
                    HandleEscapeSequence(playerObj);
                    break;

                case TutorialActionType.Fight:
                    HandleFightSequence();
                    break;

                case TutorialActionType.Hang:
                    Debug.Log("[TutorialTrigger] Hang/Climb trigger activated - Look for glowing vines!");
                    break;
            }
        }

        private void HandleEscapeSequence(GameObject playerObj)
        {
            // A. Spawn Predator behind player
            if (predatorToSpawnPrefab != null)
            {
                Vector3 spawnPos = predatorSpawnPoint != null ? predatorSpawnPoint.position : playerObj.transform.position - playerObj.transform.forward * 5f;
                Quaternion spawnRot = predatorSpawnPoint != null ? predatorSpawnPoint.rotation : playerObj.transform.rotation;
                Instantiate(predatorToSpawnPrefab, spawnPos, spawnRot);
            }

            // B. Front View Camera switch
            if (enableFrontViewCamera && Camera.main != null)
            {
                StartCoroutine(FrontViewCameraRoutine(playerObj));
            }
        }

        private void HandleFightSequence()
        {
            if (trainingEnemyPrefab != null)
            {
                Vector3 spawnPos = enemySpawnPoint != null ? enemySpawnPoint.position : transform.position + transform.forward * 3f;
                Instantiate(trainingEnemyPrefab, spawnPos, transform.rotation);
            }
        }

        private IEnumerator FrontViewCameraRoutine(GameObject playerObj)
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;

            Transform originalParent = cam.transform.parent;
            Vector3 originalPos = cam.transform.localPosition;
            Quaternion originalRot = cam.transform.localRotation;

            // Position camera in front of player facing back
            cam.transform.position = playerObj.transform.position + playerObj.transform.forward * 6f + Vector3.up * 2f;
            cam.transform.LookAt(playerObj.transform.position + Vector3.up * 1f);

            yield return new WaitForSecondsRealtime(autoDismissSeconds > 0 ? autoDismissSeconds : 3.5f);

            // Restore camera position
            cam.transform.localPosition = originalPos;
            cam.transform.localRotation = originalRot;
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);

            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }
    }
}
