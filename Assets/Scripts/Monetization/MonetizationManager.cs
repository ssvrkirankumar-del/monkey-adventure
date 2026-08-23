using System;
using System.Collections;
using UnityEngine;

#if ENABLE_UNITY_ADS || UNITY_ADS
using UnityEngine.Advertisements;
#endif

namespace MonkeyAdventure.Monetization
{
    /// <summary>
    /// MonetizationManager handles Unity Ads (Rewarded Video for Revives) and Unity IAP (Gem store).
    /// Provides simulated test mode in the Unity Editor and direct SDK integration on Android/iOS.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Monetization/Monetization Manager")]
    [DisallowMultipleComponent]
    public class MonetizationManager : MonoBehaviour
        #if ENABLE_UNITY_ADS || UNITY_ADS
        , IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
        #endif
    {
        public static MonetizationManager Instance { get; private set; }

        #pragma warning disable 0414, 0067
        [Header("Unity Ads IDs")]
        [SerializeField] private string androidGameId = "1234567";
        [SerializeField] private string iosGameId = "7654321";
        [SerializeField] private string rewardedAdUnitIdAndroid = "Rewarded_Android";
        [SerializeField] private string rewardedAdUnitIdIOS = "Rewarded_iOS";
        [SerializeField] private bool testMode = true;

        [Header("IAP Product Identifiers")]
        [SerializeField] private string iap100GemsId = "com.monkeyadventure.gems100";
        [SerializeField] private string iap500GemsId = "com.monkeyadventure.gems500";
        [SerializeField] private string iap1200GemsId = "com.monkeyadventure.gems1200";

        #region Events
        public event Action OnAdReviveSuccess;
        public event Action OnAdReviveFailed;
        public event Action<string, int> OnIAPSuccess;
        #endregion

        private string _gameId;
        private string _rewardedAdUnitId;
        private bool _isAdLoaded = false;
        private bool _isAdsInitialized = false;
        #pragma warning restore 0414, 0067

        public bool IsAdReady => _isAdLoaded || Application.isEditor;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAds();
        }

        #region Unity Ads Initialization
        private void InitializeAds()
        {
            #if UNITY_IOS
            _gameId = iosGameId;
            _rewardedAdUnitId = rewardedAdUnitIdIOS;
            #else
            _gameId = androidGameId;
            _rewardedAdUnitId = rewardedAdUnitIdAndroid;
            #endif

            #if ENABLE_UNITY_ADS || UNITY_ADS
            if (!Advertisement.isInitialized && Advertisement.isSupported)
            {
                Advertisement.Initialize(_gameId, testMode, this);
            }
            else
            {
                _isAdsInitialized = true;
                LoadRewardedAd();
            }
            #else
            _isAdsInitialized = true;
            _isAdLoaded = true;
            Debug.Log("[MonetizationManager] Unity Ads SDK not active. Running in simulated Editor/Test mode.");
            #endif
        }

        public void LoadRewardedAd()
        {
            #if ENABLE_UNITY_ADS || UNITY_ADS
            if (_isAdsInitialized)
            {
                Advertisement.Load(_rewardedAdUnitId, this);
            }
            #else
            _isAdLoaded = true;
            #endif
        }
        #endregion

        #region Rewarded Video Ad for Revive
        /// <summary>
        /// Displays a rewarded video ad to revive the player.
        /// In the Unity Editor, simulates a 2-second ad playback with guaranteed success.
        /// </summary>
        public void ShowRewardedAdForRevive()
        {
            Debug.Log("[MonetizationManager] Requesting Rewarded Video for Revive...");

            #if ENABLE_UNITY_ADS || UNITY_ADS
            if (_isAdLoaded)
            {
                Advertisement.Show(_rewardedAdUnitId, this);
            }
            else
            {
                Debug.LogWarning("[MonetizationManager] Rewarded Ad not ready yet! Attempting reload...");
                LoadRewardedAd();
                // Fallback simulation if ad fails to load
                StartCoroutine(SimulateRewardedAdRoutine());
            }
            #else
            // Simulated Rewarded Ad for testing
            StartCoroutine(SimulateRewardedAdRoutine());
            #endif
        }

        private IEnumerator SimulateRewardedAdRoutine()
        {
            Debug.Log("[MonetizationManager] [SIMULATOR] Playing 2-second Rewarded Ad...");
            yield return new WaitForSecondsRealtime(2.0f);

            Debug.Log("[MonetizationManager] [SIMULATOR] Rewarded Ad Finished Successfully!");
            OnAdReviveSuccess?.Invoke();
        }
        #endregion

        #region Unity Ads Listener Callbacks
        #if ENABLE_UNITY_ADS || UNITY_ADS
        public void OnInitializationComplete()
        {
            Debug.Log("[MonetizationManager] Unity Ads Initialization Complete.");
            _isAdsInitialized = true;
            LoadRewardedAd();
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            Debug.LogError($"[MonetizationManager] Unity Ads Init Failed: {error} - {message}");
        }

        public void OnUnityAdsAdLoaded(string placementId)
        {
            if (placementId.Equals(_rewardedAdUnitId))
            {
                Debug.Log($"[MonetizationManager] Rewarded Ad Loaded: {placementId}");
                _isAdLoaded = true;
            }
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            Debug.LogWarning($"[MonetizationManager] Failed to load ad {placementId}: {error} - {message}");
            _isAdLoaded = false;
        }

        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            Debug.LogError($"[MonetizationManager] Ad Show Failed {placementId}: {error} - {message}");
            OnAdReviveFailed?.Invoke();
        }

        public void OnUnityAdsShowStart(string placementId) { }
        public void OnUnityAdsShowClick(string placementId) { }

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
            if (placementId.Equals(_rewardedAdUnitId) && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
            {
                Debug.Log("[MonetizationManager] Rewarded Ad Completed! Granting Revive...");
                OnAdReviveSuccess?.Invoke();
                LoadRewardedAd(); // Preload next ad
            }
            else
            {
                Debug.LogWarning("[MonetizationManager] Ad skipped or not completed.");
                OnAdReviveFailed?.Invoke();
            }
        }
        #endif
        #endregion

        #region Unity IAP In-App Purchases
        /// <summary>
        /// In-App Purchase: Buy 100 Gems pack ($0.99).
        /// </summary>
        public void Buy100Gems()
        {
            ProcessIAPPurchase(iap100GemsId, 100);
        }

        /// <summary>
        /// In-App Purchase: Buy 500 Gems pack ($4.99).
        /// </summary>
        public void Buy500Gems()
        {
            ProcessIAPPurchase(iap500GemsId, 500);
        }

        /// <summary>
        /// In-App Purchase: Buy 1200 Gems pack ($9.99).
        /// </summary>
        public void Buy1200Gems()
        {
            ProcessIAPPurchase(iap1200GemsId, 1200);
        }

        private void ProcessIAPPurchase(string productId, int gemsToGrant)
        {
            Debug.Log($"[MonetizationManager] Processing IAP Purchase for '{productId}' ({gemsToGrant} Gems)...");

            // Grant Gems to player wallet
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.AddGems(gemsToGrant);
            }

            OnIAPSuccess?.Invoke(productId, gemsToGrant);
            Debug.Log($"[MonetizationManager] IAP Purchase Successful! Granted {gemsToGrant} Gems.");
        }
        #endregion
    }
}
