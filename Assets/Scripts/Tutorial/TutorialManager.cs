using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MonkeyAdventure.Tutorial
{
    public enum TutorialActionType
    {
        Run,
        Jump,
        Hang,
        Fight,
        Escape
    }

    /// <summary>
    /// Central manager for Level 1 Tutorial & Training prompts.
    /// Handles UI overlays, slow-motion bullet time (Time.timeScale = 0.2f), and action completion.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Tutorial/Tutorial Manager")]
    [DisallowMultipleComponent]
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Header("UI Display Elements")]
        [Tooltip("Parent panel or CanvasGroup containing the tutorial prompt.")]
        [SerializeField] private GameObject tutorialUIPanel;

        [Tooltip("Main title prompt text (e.g. 'JUMP TRAINING').")]
        [SerializeField] private TextMeshProUGUI promptTitleText;

        [Tooltip("Detailed instruction text (e.g. 'Tap the Jump button to clear the pit.').")]
        [SerializeField] private TextMeshProUGUI promptInstructionText;

        [Tooltip("CanvasGroup for smooth alpha fade-in/fade-out.")]
        [SerializeField] private CanvasGroup uiCanvasGroup;

        [Header("Time Slow-Motion Settings")]
        [Tooltip("Slow-motion time scale when a tutorial trigger is entered.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float slowMotionScale = 0.2f;

        [Tooltip("Speed of transition into and out of slow-motion.")]
        [SerializeField] private float timeTransitionSpeed = 6.0f;

        [Header("Audio & SFX")]
        [SerializeField] private AudioClip promptAppearSound;
        [SerializeField] private AudioClip actionCompletedSound;

        [Header("Debug OnGUI Display")]
        [Tooltip("Fallback on-screen UI if TextMeshPro UI is unassigned in the scene.")]
        [SerializeField] private bool enableDebugOnGUI = true;

        // Runtime State
        private bool _isTutorialActive = false;
        private string _currentTitle = "";
        private string _currentInstruction = "";
        private TutorialActionType _currentActionType;
        private float _targetTimeScale = 1.0f;
        private Coroutine _timeScaleCoroutine;
        private Coroutine _autoDismissCoroutine;
        private AudioSource _audioSource;

        public bool IsTutorialActive => _isTutorialActive;
        public TutorialActionType CurrentActionType => _currentActionType;

        public event Action<TutorialActionType> OnTutorialStarted;
        public event Action<TutorialActionType> OnTutorialCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }

            HideTutorialImmediate();
        }

        private void Update()
        {
            // Smoothly interpolate timeScale towards target
            if (Mathf.Abs(Time.timeScale - _targetTimeScale) > 0.005f)
            {
                Time.timeScale = Mathf.MoveTowards(Time.timeScale, _targetTimeScale, timeTransitionSpeed * Time.unscaledDeltaTime);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            }

            // Keyboard testing shortcuts to complete active tutorial
            if (_isTutorialActive)
            {
                CheckTutorialCompletionInputs();
            }
        }

        private void CheckTutorialCompletionInputs()
        {
            switch (_currentActionType)
            {
                case TutorialActionType.Run:
                    if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.3f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.3f)
                    {
                        CompleteTutorial(TutorialActionType.Run);
                    }
                    break;

                case TutorialActionType.Jump:
                    if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
                    {
                        CompleteTutorial(TutorialActionType.Jump);
                    }
                    break;

                case TutorialActionType.Fight:
                    if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
                    {
                        CompleteTutorial(TutorialActionType.Fight);
                    }
                    break;
            }
        }

        #region Public Tutorial Control API
        /// <summary>
        /// Displays a tutorial prompt, slows time, and notifies listeners.
        /// </summary>
        public void ShowTutorialPrompt(string title, string instruction, TutorialActionType actionType, bool slowTime = true, float autoDismissSeconds = 0f)
        {
            _isTutorialActive = true;
            _currentTitle = title;
            _currentInstruction = instruction;
            _currentActionType = actionType;

            // 1. Slow down time
            if (slowTime)
            {
                _targetTimeScale = slowMotionScale;
            }

            // 2. Play Audio
            if (promptAppearSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(promptAppearSound);
            }

            // 3. Update TextMeshPro UI
            if (promptTitleText != null) promptTitleText.text = title;
            if (promptInstructionText != null) promptInstructionText.text = instruction;

            if (tutorialUIPanel != null)
            {
                tutorialUIPanel.SetActive(true);
            }

            if (uiCanvasGroup != null)
            {
                StopAllCoroutines();
                StartCoroutine(FadeCanvasGroup(uiCanvasGroup, 1f, 0.25f));
            }

            // 4. Auto dismiss timer if specified
            if (autoDismissSeconds > 0)
            {
                if (_autoDismissCoroutine != null) StopCoroutine(_autoDismissCoroutine);
                _autoDismissCoroutine = StartCoroutine(AutoDismissRoutine(autoDismissSeconds, actionType));
            }

            OnTutorialStarted?.Invoke(actionType);
            Debug.Log($"[TutorialManager] Showing Tutorial Prompt: '{title}' ({actionType})");
        }

        /// <summary>
        /// Restores normal game time and dismisses the tutorial prompt.
        /// </summary>
        public void CompleteTutorial(TutorialActionType actionType)
        {
            if (!_isTutorialActive) return;

            _isTutorialActive = false;

            // 1. Restore normal time scale
            _targetTimeScale = 1.0f;
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;

            // 2. Play Completion Audio
            if (actionCompletedSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(actionCompletedSound);
            }

            // 3. Fade out UI
            if (uiCanvasGroup != null)
            {
                StartCoroutine(FadeCanvasGroup(uiCanvasGroup, 0f, 0.2f, () =>
                {
                    if (tutorialUIPanel != null) tutorialUIPanel.SetActive(false);
                }));
            }
            else if (tutorialUIPanel != null)
            {
                tutorialUIPanel.SetActive(false);
            }

            OnTutorialCompleted?.Invoke(actionType);
            Debug.Log($"[TutorialManager] Completed Tutorial: {actionType}");
        }

        public void HideTutorialImmediate()
        {
            _isTutorialActive = false;
            _targetTimeScale = 1.0f;
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;

            if (tutorialUIPanel != null) tutorialUIPanel.SetActive(false);
            if (uiCanvasGroup != null) uiCanvasGroup.alpha = 0f;
        }
        #endregion

        private IEnumerator AutoDismissRoutine(float duration, TutorialActionType type)
        {
            yield return new WaitForSecondsRealtime(duration);
            if (_isTutorialActive && _currentActionType == type)
            {
                CompleteTutorial(type);
            }
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, float duration, Action onComplete = null)
        {
            float startAlpha = cg.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            cg.alpha = targetAlpha;
            onComplete?.Invoke();
        }

        #region Debug OnGUI Fallback Display
        private void OnGUI()
        {
            if (!_isTutorialActive || !enableDebugOnGUI) return;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };

            GUIStyle bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            float width = 450;
            float height = 130;
            float x = (Screen.width - width) / 2f;
            float y = Screen.height * 0.15f;

            GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);
            GUILayout.Label($"🐒 {_currentTitle}", titleStyle);
            GUILayout.Space(8);
            GUILayout.Label(_currentInstruction, bodyStyle);
            GUILayout.Space(6);
            if (GUILayout.Button("Got It (Continue)", GUILayout.Height(26)))
            {
                CompleteTutorial(_currentActionType);
            }
            GUILayout.EndArea();
        }
        #endregion
    }
}
