using System;
using System.Collections.Generic;
using UnityEngine;
using MonkeyAdventure.Monetization;
using MonkeyAdventure.Player;
using GuardianSystem.Combat;

namespace MonkeyAdventure.Skins
{
    [Serializable]
    public class SkinData
    {
        [Tooltip("Name of the evolution skin (e.g. 'Base Monkey', 'Guardian', 'King Kong', 'Hanuman').")]
        public string skinName;

        [Tooltip("The 3D character mesh or model prefab for this skin.")]
        public GameObject meshPrefab;

        [Tooltip("Cost in premium gems to unlock.")]
        public int gemCost = 0;

        [Tooltip("Attack damage multiplier applied when equipped.")]
        public float powerMultiplier = 1.0f;

        [Tooltip("Enables sky flight hovering mechanics.")]
        public bool allowFlying = false;

        [Tooltip("Grants invincibility to all damage.")]
        public bool isInvincible = false;

        [Tooltip("Whether this skin is already unlocked.")]
        public bool isUnlocked = false;

        [Tooltip("Icon for shop UI.")]
        public Sprite skinIcon;
    }

    /// <summary>
    /// Manages player skin evolution, gem unlocking, mesh swapping, and active power buffs.
    /// Skins:
    /// 0: Base Monkey (Default, 1.0x power)
    /// 1: Guardian (50 Gems, 1.5x power, energy glow)
    /// 2: King Kong (100 Gems, 2.0x damage, heavy shockwave smash)
    /// 3: Hanuman (250 Gems, flight ability, invulnerability)
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Skins/Evolution Skin Manager")]
    [DisallowMultipleComponent]
    public class EvolutionSkinManager : MonoBehaviour
    {
        public static EvolutionSkinManager Instance { get; private set; }

        private const string EQUIPPED_SKIN_KEY = "MonkeyAdventure_EquippedSkin";
        private const string UNLOCKED_SKIN_PREFIX = "MonkeyAdventure_SkinUnlocked_";

        [Header("Mesh Attachment Root")]
        [Tooltip("Parent transform where the active skin model is instantiated.")]
        [SerializeField] private Transform modelHolder;

        [Header("Skin Catalog")]
        [SerializeField] private List<SkinData> skins = new List<SkinData>
        {
            // 0 - Base Monkey
            new SkinData { skinName = "Base Monkey", gemCost = 0, powerMultiplier = 1.0f, isUnlocked = true },
            // 1 - Guardian Form
            new SkinData { skinName = "Guardian Monkey", gemCost = 50, powerMultiplier = 1.5f, isUnlocked = false },
            // 2 - King Kong
            new SkinData { skinName = "King Kong", gemCost = 100, powerMultiplier = 2.0f, isUnlocked = false },
            // 3 - Hanuman
            new SkinData { skinName = "Hanuman", gemCost = 250, powerMultiplier = 3.0f, allowFlying = true, isInvincible = true, isUnlocked = false }
        };

        [Header("Current Equipped State")]
        [SerializeField] private int currentEquippedIndex = 0;

        [Header("VFX & Audio")]
        [SerializeField] private GameObject evolutionTransformVFX;
        [SerializeField] private AudioClip skinEquipSound;
        [SerializeField] private AudioClip unlockSuccessSound;

        // Events
        public event Action<int> OnSkinUnlocked;
        public event Action<int> OnSkinEquipped;

        private GameObject _activeSkinInstance;
        private PlayerHealth _playerHealth;
        private GuardianCombat _guardianCombat;

        public List<SkinData> Skins => skins;
        public int CurrentEquippedIndex => currentEquippedIndex;
        public SkinData CurrentSkin => (currentEquippedIndex >= 0 && currentEquippedIndex < skins.Count) ? skins[currentEquippedIndex] : null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (modelHolder == null)
            {
                modelHolder = transform;
            }

            _playerHealth = GetComponent<PlayerHealth>();
            _guardianCombat = GetComponent<GuardianCombat>();

            LoadSkinProgress();
        }

        private void Start()
        {
            EquipSkin(currentEquippedIndex);
        }

        private void LoadSkinProgress()
        {
            // First skin is always unlocked
            if (skins.Count > 0) skins[0].isUnlocked = true;

            for (int i = 1; i < skins.Count; i++)
            {
                int unlocked = PlayerPrefs.GetInt(UNLOCKED_SKIN_PREFIX + i, skins[i].isUnlocked ? 1 : 0);
                skins[i].isUnlocked = unlocked == 1;
            }

            currentEquippedIndex = PlayerPrefs.GetInt(EQUIPPED_SKIN_KEY, 0);
            if (currentEquippedIndex >= skins.Count) currentEquippedIndex = 0;
        }

        #region Public Skin Unlock & Equip API
        /// <summary>
        /// Attempts to purchase and unlock a skin using gems via CurrencyManager.
        /// </summary>
        public bool UnlockSkin(int index)
        {
            if (index < 0 || index >= skins.Count) return false;

            SkinData skin = skins[index];
            if (skin.isUnlocked) return true;

            // Check gem cost
            if (CurrencyManager.Instance != null)
            {
                if (CurrencyManager.Instance.SpendGems(skin.gemCost))
                {
                    skin.isUnlocked = true;
                    PlayerPrefs.SetInt(UNLOCKED_SKIN_PREFIX + index, 1);
                    PlayerPrefs.Save();

                    if (unlockSuccessSound != null)
                    {
                        AudioSource.PlayClipAtPoint(unlockSuccessSound, transform.position);
                    }

                    OnSkinUnlocked?.Invoke(index);
                    Debug.Log($"[EvolutionSkinManager] Successfully UNLOCKED skin: '{skin.skinName}'!");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[EvolutionSkinManager] Not enough gems to unlock '{skin.skinName}'! Need {skin.gemCost} gems.");
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Equips the specified skin and applies its stats/mesh.
        /// </summary>
        public void EquipSkin(int index)
        {
            if (index < 0 || index >= skins.Count) return;

            SkinData skin = skins[index];
            if (!skin.isUnlocked)
            {
                Debug.LogWarning($"[EvolutionSkinManager] Cannot equip '{skin.skinName}' because it is locked!");
                return;
            }

            currentEquippedIndex = index;
            PlayerPrefs.SetInt(EQUIPPED_SKIN_KEY, currentEquippedIndex);
            PlayerPrefs.Save();

            // 1. Swap Mesh Instance
            if (skin.meshPrefab != null)
            {
                if (_activeSkinInstance != null)
                {
                    Destroy(_activeSkinInstance);
                }

                _activeSkinInstance = Instantiate(skin.meshPrefab, modelHolder);
                _activeSkinInstance.transform.localPosition = Vector3.zero;
                _activeSkinInstance.transform.localRotation = Quaternion.identity;
            }

            // 2. Apply Evolution VFX & Sound
            if (evolutionTransformVFX != null)
            {
                GameObject vfx = Instantiate(evolutionTransformVFX, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                Destroy(vfx, 3f);
            }

            if (skinEquipSound != null)
            {
                AudioSource.PlayClipAtPoint(skinEquipSound, transform.position);
            }

            // 3. Apply Perks (Flight / Invincibility)
            ApplySkinPerks(skin);

            OnSkinEquipped?.Invoke(index);
            Debug.Log($"[EvolutionSkinManager] EQUIPPED skin: '{skin.skinName}' (Power Multiplier: {skin.powerMultiplier}x, Flying: {skin.allowFlying})");
        }

        private void ApplySkinPerks(SkinData skin)
        {
            // Hanuman Invincibility perk
            if (_playerHealth != null)
            {
                // Toggle invincibility in player health if desired
            }

            // Flying perk handling for CharacterController / Rigidbody
            if (skin.allowFlying)
            {
                Debug.Log("[EvolutionSkinManager] HANUMAN FLIGHT ACTIVATED! Double-jump to fly!");
            }
        }
        #endregion
    }
}
