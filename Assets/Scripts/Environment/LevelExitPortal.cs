using UnityEngine;
using MonkeyAdventure.Progression;
using MonkeyAdventure.Core;

namespace MonkeyAdventure.Environment
{
    /// <summary>
    /// Level Exit Gateway Portal.
    /// Triggers level completion and advances the player to the next campaign level
    /// via LevelProgressionManager when entered by the Player.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Environment/Level Exit Portal")]
    [RequireComponent(typeof(Collider))]
    public class LevelExitPortal : MonoBehaviour
    {
        [Header("Portal Settings")]
        [Tooltip("Score/stars awarded upon entering this completion gateway.")]
        [SerializeField] private int completionScore = 100;

        [Tooltip("Optional particle effect or audio played on level finish.")]
        [SerializeField] private AudioClip levelCompleteSound;

        [Header("Visual Animation")]
        [SerializeField] private bool rotatePortal = true;
        [SerializeField] private float rotationSpeed = 45f;

        private bool _hasTriggered = false;

        private void Start()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            if (rotatePortal)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasTriggered) return;

            if (other.CompareTag("Player") || other.GetComponent<MonkeyAdventure.Player.MonkeyPlayerController>() != null)
            {
                _hasTriggered = true;
                Debug.Log($"[LevelExitPortal] Player touched {gameObject.name}! Triggering Level Completion...");

                if (levelCompleteSound != null)
                {
                    AudioSource.PlayClipAtPoint(levelCompleteSound, transform.position);
                }

                if (LevelProgressionManager.Instance != null)
                {
                    LevelProgressionManager.Instance.CompleteCurrentLevel(completionScore);
                }
                else
                {
                    Debug.Log("[LevelExitPortal] LevelProgressionManager not found in scene. Level Complete!");
                }
            }
        }
    }
}
