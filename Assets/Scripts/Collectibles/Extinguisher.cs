using System.Collections;
using UnityEngine;
using MonkeyAdventure.Hazards;

namespace MonkeyAdventure.Collectibles
{
    /// <summary>
    /// Extinguisher / Water Aura Buff Collectible.
    /// When picked up, grants the Player a temporary Water Splash Buff for 15 seconds.
    /// While active, attacking or approaching FireHazards triggers an AOE water blast that douses the flames.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Collectibles/Extinguisher Buff")]
    [RequireComponent(typeof(Collider))]
    public class Extinguisher : MonoBehaviour
    {
        [Header("Buff Settings")]
        [Tooltip("Duration of the water buff in seconds.")]
        [SerializeField] private float buffDuration = 15.0f;

        [Tooltip("Radius of the AOE water blast that extinguishes fires.")]
        [SerializeField] private float extinguishRadius = 5.0f;

        [Header("VFX & Audio")]
        [SerializeField] private GameObject waterSplashVFX;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioClip waterSplashSound;

        private Collider _collider;
        private bool _isCollected = false;

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
            if (_isCollected) return;

            if (other.CompareTag("Player") || (other.transform.root != null && other.transform.root.CompareTag("Player")))
            {
                _isCollected = true;
                ApplyWaterBuffToPlayer(other.gameObject);
            }
        }

        private void ApplyWaterBuffToPlayer(GameObject playerObj)
        {
            // 1. Play Pickup Sound & VFX
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            // 2. Attach or trigger Water Aura Helper on Player
            WaterAuraBuff aura = playerObj.GetComponent<WaterAuraBuff>();
            if (aura == null)
            {
                aura = playerObj.AddComponent<WaterAuraBuff>();
            }

            aura.ActivateWaterBuff(buffDuration, extinguishRadius, waterSplashVFX, waterSplashSound);

            Debug.Log($"[Extinguisher] Water Buff granted for {buffDuration} seconds! Extinguish fires by attacking!");

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Runtime component attached to the Player while the Water Buff is active.
    /// </summary>
    public class WaterAuraBuff : MonoBehaviour
    {
        private float _buffDuration;
        private float _radius;
        private GameObject _splashVFX;
        private AudioClip _splashSound;
        private Coroutine _buffCoroutine;

        public void ActivateWaterBuff(float duration, float radius, GameObject splashVFX, AudioClip splashSound)
        {
            _buffDuration = duration;
            _radius = radius;
            _splashVFX = splashVFX;
            _splashSound = splashSound;

            if (_buffCoroutine != null) StopCoroutine(_buffCoroutine);
            _buffCoroutine = StartCoroutine(WaterBuffRoutine());
        }

        private IEnumerator WaterBuffRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _buffDuration)
            {
                // Douse fires near player automatically
                DouseNearbyFires();

                elapsed += 0.4f;
                yield return new WaitForSeconds(0.4f);
            }

            Debug.Log("[WaterAuraBuff] Water Buff Expired.");
            Destroy(this);
        }

        public void DouseNearbyFires()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _radius);
            bool extinguishedAny = false;

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<FireHazard>(out var fire))
                {
                    if (!fire.IsExtinguished)
                    {
                        fire.ExtinguishFire();
                        extinguishedAny = true;
                    }
                }
            }

            if (extinguishedAny)
            {
                if (_splashVFX != null)
                {
                    GameObject vfx = Instantiate(_splashVFX, transform.position, Quaternion.identity);
                    Destroy(vfx, 2.5f);
                }

                if (_splashSound != null)
                {
                    AudioSource.PlayClipAtPoint(_splashSound, transform.position);
                }
            }
        }
    }
}
