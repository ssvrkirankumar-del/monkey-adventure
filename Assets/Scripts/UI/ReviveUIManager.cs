using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MonkeyAdventure.Player;
using MonkeyAdventure.Monetization;
using MonkeyAdventure.Core;

namespace MonkeyAdventure.UI
{
    /// <summary>
    /// Manages the Game Over / Revive Screen UI.
    /// Freezes time on player death, starts a 10-second countdown, and provides options
    /// to revive via Rewarded Video Ads or 10 Premium Gems.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/UI/Revive UI Manager")]
    public class ReviveUIManager : MonoBehaviour
    {
        [Header("UI Canvas & Elements")]
        [Tooltip("Root panel of the Revive / Game Over popup.")]
        [SerializeField] private GameObject revivePanel;

        [Tooltip("Countdown timer text (10 -> 0).")]
        [SerializeField] private TextMeshProUGUI countdownText;

        [Tooltip("Text displaying the player's current gem balance.")]
        [SerializeField] private TextMeshProUGUI currentGemsText;

        [Header("Buttons")]
        [SerializeField] private Button watchAdReviveButton;
        [SerializeField] private Button spendGemsReviveButton;
        [SerializeField] private Button giveUpButton;

        [Header("Revive Settings")]
        [Tooltip("Countdown time allowed before forcing give up.")]
        [SerializeField] private float countdownDuration = 10.0f;

        [Tooltip("Cost in gems to revive instantly.")]
        [SerializeField] private int gemReviveCost = 10;

        [Header("VFX & Audio")]
        [SerializeField] private GameObject magicalReviveVFXPrefab;
        [SerializeField] private AudioClip reviveFanfareSound;
        [SerializeField] private AudioClip countdownTickSound;

        [Header("Debug OnGUI Display")]
        [Tooltip("Shows fallback UI in Game View if Canvas references are unassigned.")]
        [SerializeField] private bool enableDebugOnGUI = true;

        private PlayerHealth _playerHealth;
        private Coroutine _countdownCoroutine;
        private float _remainingTime;
        private bool _isReviveActive = false;
        private AudioSource _audioSource;

        public bool IsReviveActive => _isReviveActive;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }

            if (revivePanel != null)
            {
                revivePanel.SetActive(false);
            }
        }

        private void Start()
        {
            FindPlayerHealth();

            // Link Button Listeners
            if (watchAdReviveButton != null) watchAdReviveButton.onClick.AddListener(OnWatchAdClicked);
            if (spendGemsReviveButton != null) spendGemsReviveButton.onClick.AddListener(OnUseGemsClicked);
            if (giveUpButton != null) giveUpButton.onClick.AddListener(OnGiveUpClicked);

            // Subscribe to Rewarded Ad Revive Event
            if (MonetizationManager.Instance != null)
            {
                MonetizationManager.Instance.OnAdReviveSuccess += HandleAdReviveSuccess;
            }
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnPlayerDied -= HandlePlayerDied;
            }

            if (MonetizationManager.Instance != null)
            {
                MonetizationManager.Instance.OnAdReviveSuccess -= HandleAdReviveSuccess;
            }
        }

        private void FindPlayerHealth()
        {
            if (_playerHealth == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _playerHealth = player.GetComponent<PlayerHealth>();
                    if (_playerHealth != null)
                    {
                        _playerHealth.OnPlayerDied += HandlePlayerDied;
                    }
                }
            }
        }

        #region Death Trigger & Countdown
        private void HandlePlayerDied()
        {
            if (_isReviveActive) return;

            ShowReviveScreen();
        }

        public void ShowReviveScreen()
        {
            _isReviveActive = true;

            // 1. Pause Game Time
            Time.timeScale = 0f;

            // 2. Open Panel
            if (revivePanel != null)
            {
                revivePanel.SetActive(true);
            }

            // 3. Update Gem UI
            UpdateGemsDisplay();

            // 4. Start Countdown
            _remainingTime = countdownDuration;
            if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = StartCoroutine(CountdownRoutine());

            Debug.Log("[ReviveUIManager] Player defeated. Pausing time and opening Revive UI...");
        }

        private IEnumerator CountdownRoutine()
        {
            while (_remainingTime > 0)
            {
                if (countdownText != null)
                {
                    countdownText.text = Mathf.CeilToInt(_remainingTime).ToString();
                }

                if (countdownTickSound != null && _audioSource != null)
                {
                    _audioSource.PlayOneShot(countdownTickSound);
                }

                yield return new WaitForSecondsRealtime(1.0f);
                _remainingTime -= 1.0f;
            }

            // If timer runs out without reviving -> Force Give Up
            OnGiveUpClicked();
        }

        private void UpdateGemsDisplay()
        {
            if (currentGemsText != null && CurrencyManager.Instance != null)
            {
                currentGemsText.text = $"Gems: {CurrencyManager.Instance.GetGems()}";
            }
        }
        #endregion

        #region Button Action Hooks
        /// <summary>
        /// Button 1: Watch Ad to Revive.
        /// </summary>
        public void OnWatchAdClicked()
        {
            Debug.Log("[ReviveUIManager] Watch Ad Revive Clicked.");
            if (MonetizationManager.Instance != null)
            {
                MonetizationManager.Instance.ShowRewardedAdForRevive();
            }
            else
            {
                // Fallback simulation
                HandleAdReviveSuccess();
            }
        }

        private void HandleAdReviveSuccess()
        {
            Debug.Log("[ReviveUIManager] Ad completed successfully! Executing Revive...");
            ExecuteRevive();
        }

        /// <summary>
        /// Button 2: Use 10 Gems to Revive.
        /// </summary>
        public void OnUseGemsClicked()
        {
            if (CurrencyManager.Instance != null)
            {
                if (CurrencyManager.Instance.SpendGems(gemReviveCost))
                {
                    Debug.Log($"[ReviveUIManager] Spent {gemReviveCost} Gems. Executing Revive...");
                    ExecuteRevive();
                }
                else
                {
                    Debug.LogWarning("[ReviveUIManager] Not enough gems to revive!");
                    // Open IAP Gem Store prompt here if desired
                }
            }
            else
            {
                // Fallback direct revive
                ExecuteRevive();
            }
        }

        /// <summary>
        /// Button 3: Give Up & Respawn at Checkpoint.
        /// </summary>
        public void OnGiveUpClicked()
        {
            if (!_isReviveActive) return;

            Debug.Log("[ReviveUIManager] Give Up Clicked. Respawning at Checkpoint...");
            CloseReviveScreen();

            // Resume Time
            Time.timeScale = 1.0f;

            // Trigger standard checkpoint respawn
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerManualRespawn();
            }
        }
        #endregion

        #region Revive Execution
        private void ExecuteRevive()
        {
            CloseReviveScreen();

            // 1. Resume Game Time
            Time.timeScale = 1.0f;

            // 2. Revive Player in place with full health
            if (_playerHealth == null) FindPlayerHealth();

            if (_playerHealth != null)
            {
                _playerHealth.ReviveInPlace(100);
            }

            // 3. Spawn Magical Revive VFX & Audio
            GameObject player = _playerHealth != null ? _playerHealth.gameObject : GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (magicalReviveVFXPrefab != null)
                {
                    GameObject vfx = Instantiate(magicalReviveVFXPrefab, player.transform.position, Quaternion.identity);
                    Destroy(vfx, 3.5f);
                }

                if (reviveFanfareSound != null)
                {
                    AudioSource.PlayClipAtPoint(reviveFanfareSound, player.transform.position);
                }
            }

            Debug.Log("✨ [ReviveUIManager] PLAYER REVIVED WITH FULL HEALTH! ✨");
        }

        private void CloseReviveScreen()
        {
            _isReviveActive = false;

            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            if (revivePanel != null)
            {
                revivePanel.SetActive(false);
            }
        }
        #endregion

        #region Debug OnGUI Fallback
        private void OnGUI()
        {
            if (!_isReviveActive || !enableDebugOnGUI) return;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };

            GUIStyle timerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.red }
            };

            float width = 360;
            float height = 240;
            float x = (Screen.width - width) / 2f;
            float y = (Screen.height - height) / 2f;

            GUILayout.BeginArea(new Rect(x, y, width, height), GUI.skin.box);
            GUILayout.Label("💀 YOU FELL! REVIVE?", titleStyle);
            GUILayout.Label($"⏳ {Mathf.CeilToInt(_remainingTime)}s", timerStyle);

            int gems = CurrencyManager.Instance != null ? CurrencyManager.Instance.GetGems() : 0;
            GUILayout.Label($"Your Gems: 💎 {gems}", GUI.skin.label);

            GUILayout.Space(6);
            if (GUILayout.Button("🎬 Watch Ad to Revive (FREE)", GUILayout.Height(34)))
            {
                OnWatchAdClicked();
            }

            if (GUILayout.Button($"💎 Use {gemReviveCost} Gems to Revive", GUILayout.Height(34)))
            {
                OnUseGemsClicked();
            }

            if (GUILayout.Button("🏳️ Give Up (Checkpoint Respawn)", GUILayout.Height(28)))
            {
                OnGiveUpClicked();
            }
            GUILayout.EndArea();
        }
        #endregion
    }
}
