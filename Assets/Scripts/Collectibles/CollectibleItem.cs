using UnityEngine;
using MonkeyAdventure.Core;

namespace MonkeyAdventure.Collectibles
{
    /// <summary>
    /// Item type identifier for collectibles.
    /// Can automatically infer type from GameObject tag ("Food" or "Coin") or use explicit override.
    /// </summary>
    public enum CollectibleType
    {
        AutoDetectFromTag,
        Food,
        Coin
    }

    /// <summary>
    /// Attach to Bananas (Food) and Coins.
    /// Handles continuous rotation, floating bob animation, player trigger detection,
    /// score addition via GameManager, sparkle VFX spawning, audio, and self-destruction.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Collectibles/Collectible Item")]
    [RequireComponent(typeof(Collider))]
    public class CollectibleItem : MonoBehaviour
    {
        [Header("Item Type")]
        [Tooltip("Auto-detects whether this is Food or Coin based on GameObject Tag, or set explicitly.")]
        [SerializeField] private CollectibleType itemType = CollectibleType.AutoDetectFromTag;

        [Tooltip("Amount of Food or Coins granted upon collection.")]
        [SerializeField] private int value = 1;

        [Header("Animation & Movement")]
        [Tooltip("Continuous rotation axis and speed in degrees per second.")]
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 120f, 0f);

        [Tooltip("Enable subtle floating up and down bobbing animation.")]
        [SerializeField] private bool enableBobbing = true;

        [Tooltip("Speed of vertical floating bob.")]
        [SerializeField] private float bobFrequency = 3.0f;

        [Tooltip("Height amplitude of the floating bob.")]
        [SerializeField] private float bobAmplitude = 0.15f;

        [Header("Collection Visual & Sound FX")]
        [Tooltip("Sparkle / burst particle effect spawned upon collection.")]
        [SerializeField] private GameObject sparkleVFXPrefab;

        [Tooltip("Sound clip played when picked up by the player.")]
        [SerializeField] private AudioClip pickupSound;

        [Tooltip("Duration before the spawned sparkle VFX is destroyed.")]
        [SerializeField] private float vfxDuration = 2.0f;

        private Vector3 _startPosition;
        private bool _isCollected = false;
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }
        }

        private void Start()
        {
            _startPosition = transform.position;
        }

        private void Update()
        {
            if (_isCollected) return;

            // 1. Continuous Rotation
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);

            // 2. Floating Bob Animation
            if (enableBobbing)
            {
                float newY = _startPosition.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected) return;

            // Check if collider belongs to Player
            if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
            {
                Collect();
            }
        }

        /// <summary>
        /// Handles the collection sequence: score increment, FX, sound, and destruction.
        /// </summary>
        public void Collect()
        {
            if (_isCollected) return;
            _isCollected = true;

            // 1. Determine whether this is Food or Coin
            bool isFood = false;
            if (itemType == CollectibleType.Food)
            {
                isFood = true;
            }
            else if (itemType == CollectibleType.Coin)
            {
                isFood = false;
            }
            else // AutoDetectFromTag
            {
                isFood = gameObject.CompareTag("Food") || name.ToLower().Contains("banana") || name.ToLower().Contains("food");
            }

            // 2. Notify GameManager
            if (GameManager.Instance != null)
            {
                if (isFood)
                {
                    GameManager.Instance.AddFood(value);
                }
                else
                {
                    GameManager.Instance.AddCoins(value);
                }
            }
            else
            {
                Debug.LogWarning("[CollectibleItem] GameManager.Instance not found! Ensure a GameManager is in the scene.", this);
            }

            // 3. Spawn Sparkle VFX
            if (sparkleVFXPrefab != null)
            {
                GameObject vfx = Instantiate(sparkleVFXPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, vfxDuration);
            }

            // 4. Play Sound
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            // 5. Destroy Collectible Object
            Destroy(gameObject);
        }
    }
}
