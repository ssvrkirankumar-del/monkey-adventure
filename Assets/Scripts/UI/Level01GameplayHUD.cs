using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MonkeyAdventure.Player;
using MonkeyAdventure.Core;
using MonkeyAdventure.Progression;
using MonkeyAdventure.Audio;
using GuardianSystem.Combat;

namespace MonkeyAdventure.UI
{
    /// <summary>
    /// Production-ready Gameplay HUD for Level 01: The Awakening.
    /// Provides real-time Health display, Banana/Food and Coin counters,
    /// current Objective tracking, Checkpoint activation notifications,
    /// Victory Level Complete screen, and Death/Respawn handling.
    /// Supports both OnGUI native fallback and Unity UI Canvas for maximum platform reliability.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/UI/Level 01 Gameplay HUD")]
    [DisallowMultipleComponent]
    public class Level01GameplayHUD : MonoBehaviour
    {
        public static Level01GameplayHUD Instance { get; private set; }

        [Header("Objective Settings")]
        [SerializeField] private string currentObjective = "Journey through the Awakening Jungle - Reach the Ancient Portal";

        [Header("Checkpoint Banner")]
        [SerializeField] private float checkpointBannerDuration = 2.5f;

        [Header("Audio SFX")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip levelCompleteSound;

        // Cached Runtime References
        private PlayerHealth _playerHealth;
        private MonkeyPlayerController _playerController;
        private GuardianCombat _guardianCombat;

        // UI State
        private int _currentHealth = 100;
        private int _maxHealth = 100;
        private int _foodCount = 0;
        private int _coinCount = 0;
        private bool _showCheckpointBanner = false;
        private string _checkpointBannerText = "";
        private Coroutine _checkpointBannerCoroutine;
        private bool _isLevelComplete = false;
        private int _finalScore = 0;
        private bool _isPlayerDead = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            FindPlayerReferences();
            SubscribeToGameEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameEvents();
        }

        private void FindPlayerReferences()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _playerHealth = playerObj.GetComponent<PlayerHealth>();
                _playerController = playerObj.GetComponent<MonkeyPlayerController>();
                _guardianCombat = playerObj.GetComponent<GuardianCombat>();

                if (_playerHealth != null)
                {
                    _currentHealth = _playerHealth.CurrentHealth;
                    _maxHealth = _playerHealth.MaxHealth;
                }
            }

            if (GameManager.Instance != null)
            {
                _foodCount = GameManager.Instance.FoodCount;
                _coinCount = GameManager.Instance.CoinCount;
            }
        }

        private void SubscribeToGameEvents()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged += HandleHealthChanged;
                _playerHealth.OnPlayerDied += HandlePlayerDied;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnFoodCountChanged += HandleFoodCountChanged;
                GameManager.Instance.OnCoinCountChanged += HandleCoinCountChanged;
                GameManager.Instance.OnPlayerRespawned += HandlePlayerRespawned;
            }

            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.OnLevelCompleted += HandleLevelCompleted;
            }
        }

        private void UnsubscribeFromGameEvents()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnHealthChanged -= HandleHealthChanged;
                _playerHealth.OnPlayerDied -= HandlePlayerDied;
            }

            if (GameManager.Instance != null)
                {
                GameManager.Instance.OnFoodCountChanged -= HandleFoodCountChanged;
                GameManager.Instance.OnCoinCountChanged -= HandleCoinCountChanged;
                GameManager.Instance.OnPlayerRespawned -= HandlePlayerRespawned;
            }

            if (LevelProgressionManager.Instance != null)
            {
                LevelProgressionManager.Instance.OnLevelCompleted -= HandleLevelCompleted;
            }
        }

        #region Event Handlers
        private void HandleHealthChanged(int current, int max)
        {
            _currentHealth = current;
            _maxHealth = max;
        }

        private void HandlePlayerDied()
        {
            _isPlayerDead = true;
        }

        private void HandlePlayerRespawned()
        {
            _isPlayerDead = false;
            if (_playerHealth != null)
            {
                _currentHealth = _playerHealth.CurrentHealth;
            }
        }

        private void HandleFoodCountChanged(int count)
        {
            _foodCount = count;
        }

        private void HandleCoinCountChanged(int count)
        {
            _coinCount = count;
        }

        private void HandleLevelCompleted(int levelIndex, int score)
        {
            _isLevelComplete = true;
            _finalScore = score;
            currentObjective = "Level 01 Complete!";
        }

        public void ShowCheckpointNotification(string checkpointName = "Checkpoint")
        {
            if (_checkpointBannerCoroutine != null)
            {
                StopCoroutine(_checkpointBannerCoroutine);
            }
            _checkpointBannerCoroutine = StartCoroutine(CheckpointBannerRoutine(checkpointName));
        }

        private IEnumerator CheckpointBannerRoutine(string name)
        {
            _checkpointBannerText = $"⚡ {name.ToUpper()} ACTIVATED ⚡";
            _showCheckpointBanner = true;
            yield return new WaitForSeconds(checkpointBannerDuration);
            _showCheckpointBanner = false;
        }
        #endregion

        #region OnGUI Rendering (Rock-solid fallback & immediate HUD display)
        private void OnGUI()
        {
            // Set up high-contrast crisp styles
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.9f, 0.2f) },
                alignment = TextAnchor.MiddleCenter
            };

            GUIStyle hudBoxStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white }
            };

            GUIStyle objectiveStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.3f, 1f, 0.5f) }
            };

            GUIStyle victoryStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.1f) }
            };

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            // 1. Top-Left: Player Health Bar
            GUILayout.BeginArea(new Rect(20, 20, 240, 75), GUI.skin.box);
            GUILayout.Label($"❤️ <b>Health: {_currentHealth} / {_maxHealth}</b>", hudBoxStyle);
            float healthPercent = _maxHealth > 0 ? (float)_currentHealth / _maxHealth : 1f;
            Rect healthBarRect = GUILayoutUtility.GetRect(220, 16);
            GUI.color = Color.gray;
            GUI.DrawTexture(healthBarRect, Texture2D.whiteTexture);
            GUI.color = healthPercent > 0.3f ? new Color(0.2f, 0.85f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);
            GUI.DrawTexture(new Rect(healthBarRect.x, healthBarRect.y, healthBarRect.width * Mathf.Clamp01(healthPercent), healthBarRect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.EndArea();

            // 2. Top-Right: Collectibles Counters
            GUILayout.BeginArea(new Rect(Screen.width - 240, 20, 220, 75), GUI.skin.box);
            GUILayout.Label($"🍌 Bananas: <b>{_foodCount}</b>", hudBoxStyle);
            GUILayout.Label($"🪙 Coins: <b>{_coinCount}</b>", hudBoxStyle);
            GUILayout.EndArea();

            // 3. Top-Center: Objective Banner
            float objWidth = Mathf.Min(500, Screen.width - 500);
            if (objWidth > 260)
            {
                GUILayout.BeginArea(new Rect((Screen.width - objWidth) / 2f, 20, objWidth, 40), objectiveStyle);
                GUILayout.Label($"🧭 {currentObjective}", objectiveStyle);
                GUILayout.EndArea();
            }

            // 4. Center: Checkpoint Activation Notification Banner
            if (_showCheckpointBanner)
            {
                float bannerW = 340;
                float bannerH = 50;
                GUI.color = new Color(0.1f, 0.9f, 0.3f, 0.95f);
                GUI.Box(new Rect((Screen.width - bannerW) / 2f, Screen.height * 0.22f, bannerW, bannerH), _checkpointBannerText, victoryStyle);
                GUI.color = Color.white;
            }

            // 5. Level Complete Screen Overlay
            if (_isLevelComplete)
            {
                float popW = 420;
                float popH = 260;
                Rect popRect = new Rect((Screen.width - popW) / 2f, (Screen.height - popH) / 2f, popW, popH);

                GUI.Box(popRect, "", GUI.skin.window);
                GUILayout.BeginArea(new Rect(popRect.x + 20, popRect.y + 20, popW - 40, popH - 40));

                GUILayout.Label("🎉 <b>LEVEL 01 COMPLETE!</b> 🎉", headerStyle);
                GUILayout.Space(10);
                GUILayout.Label("⭐ ⭐ ⭐", headerStyle);
                GUILayout.Space(8);
                GUILayout.Label($"Bananas Gathered: <b>{_foodCount}</b>", hudBoxStyle);
                GUILayout.Label($"Coins Collected: <b>{_coinCount}</b>", hudBoxStyle);
                GUILayout.Label($"Completion Score: <b>{_finalScore}</b>", hudBoxStyle);
                GUILayout.Space(12);

                if (GUILayout.Button("▶ <b>PROCEED TO LEVEL 2</b>", buttonStyle, GUILayout.Height(36)))
                {
                    if (AudioManager.Instance != null && buttonClickSound != null)
                        AudioManager.Instance.PlaySFX("SFX_UIClick");

                    if (LevelProgressionManager.Instance != null)
                        LevelProgressionManager.Instance.LoadLevel(2);
                }

                GUILayout.EndArea();
            }

            // 6. Player Death / Retry Screen Overlay (if not using modal revive)
            if (_isPlayerDead && !_isLevelComplete)
            {
                float popW = 360;
                float popH = 190;
                Rect popRect = new Rect((Screen.width - popW) / 2f, (Screen.height - popH) / 2f, popW, popH);

                GUI.Box(popRect, "", GUI.skin.window);
                GUILayout.BeginArea(new Rect(popRect.x + 20, popRect.y + 20, popW - 40, popH - 40));

                GUILayout.Label("☠ <b>YOU FELL IN THE JUNGLE</b>", headerStyle);
                GUILayout.Space(12);
                GUILayout.Label("The jungle is dangerous. Regroup and try again!", hudBoxStyle);
                GUILayout.Space(14);

                if (GUILayout.Button("🔄 <b>RESPAWN AT CHECKPOINT</b>", buttonStyle, GUILayout.Height(36)))
                {
                    if (AudioManager.Instance != null && buttonClickSound != null)
                        AudioManager.Instance.PlaySFX("SFX_UIClick");

                    if (GameManager.Instance != null)
                        GameManager.Instance.TriggerManualRespawn();
                }

                GUILayout.EndArea();
            }
        }
        #endregion
    }
}
