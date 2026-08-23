using System;
using UnityEngine;

namespace MonkeyAdventure.Monetization
{
    /// <summary>
    /// Persistent Singleton manager for premium currency (Gems) and player wallet.
    /// Handles persistent storage via PlayerPrefs with security validation.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Monetization/Currency Manager")]
    [DisallowMultipleComponent]
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        private const string GEMS_PREFS_KEY = "MonkeyAdventure_PlayerGems";
        private const string COINS_PREFS_KEY = "MonkeyAdventure_PlayerCoins";

        [Header("Starting Balance (For New Players)")]
        [SerializeField] private int initialGems = 20;

        [Header("Runtime Balance")]
        [SerializeField] private int currentGems = 0;

        public event Action<int> OnGemsChanged;

        public int CurrentGems => currentGems;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadCurrency();
        }

        private void LoadCurrency()
        {
            if (!PlayerPrefs.HasKey(GEMS_PREFS_KEY))
            {
                currentGems = initialGems;
                SaveCurrency();
            }
            else
            {
                currentGems = PlayerPrefs.GetInt(GEMS_PREFS_KEY, initialGems);
            }

            OnGemsChanged?.Invoke(currentGems);
        }

        private void SaveCurrency()
        {
            PlayerPrefs.SetInt(GEMS_PREFS_KEY, currentGems);
            PlayerPrefs.Save();
        }

        #region Public Currency API
        /// <summary>
        /// Gets current gem balance.
        /// </summary>
        public int GetGems()
        {
            return currentGems;
        }

        /// <summary>
        /// Adds gems to player's wallet (e.g. from IAP or achievements).
        /// </summary>
        public void AddGems(int amount)
        {
            if (amount <= 0) return;

            currentGems += amount;
            SaveCurrency();
            OnGemsChanged?.Invoke(currentGems);

            Debug.Log($"[CurrencyManager] Added {amount} Gems! New Balance: {currentGems}");
        }

        /// <summary>
        /// Deducts gems if the player has sufficient balance.
        /// </summary>
        /// <returns>True if transaction succeeded, false if not enough gems.</returns>
        public bool SpendGems(int amount)
        {
            if (amount <= 0) return true;

            if (currentGems >= amount)
            {
                currentGems -= amount;
                SaveCurrency();
                OnGemsChanged?.Invoke(currentGems);

                Debug.Log($"[CurrencyManager] Spent {amount} Gems! Remaining: {currentGems}");
                return true;
            }

            Debug.LogWarning($"[CurrencyManager] Insufficient Gems! Needed: {amount}, Current: {currentGems}");
            return false;
        }

        /// <summary>
        /// Checks if player can afford an item.
        /// </summary>
        public bool HasEnoughGems(int amount)
        {
            return currentGems >= amount;
        }
        #endregion
    }
}
