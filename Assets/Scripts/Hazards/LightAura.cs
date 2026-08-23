using System.Collections;
using UnityEngine;
using GuardianSystem.Combat;

namespace MonkeyAdventure.Hazards
{
    /// <summary>
    /// Act 4: Light Aura Mechanic.
    /// Attached to the Player.
    /// In darkness, the light aura drains over time, shrinking the player's Point Light.
    /// Entering Safe Zones recharges the aura. If light runs out completely, the player takes damage from darkness.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Hazards/Light Aura")]
    public class LightAura : MonoBehaviour
    {
        [Header("Light Source Component")]
        [Tooltip("Point Light attached to the player (or child object).")]
        [SerializeField] private Light auraPointLight;

        [Header("Aura Energy Settings")]
        [SerializeField] private float maxLightEnergy = 100f;
        [SerializeField] private float currentLightEnergy = 100f;

        [Tooltip("Energy consumed per second when in darkness.")]
        [SerializeField] private float darknessDrainRate = 5.0f;

        [Header("Point Light Scaling")]
        [SerializeField] private float maxLightRange = 12f;
        [SerializeField] private float minLightRange = 1.5f;
        [SerializeField] private float maxLightIntensity = 2.5f;
        [SerializeField] private float minLightIntensity = 0.3f;

        [Header("Darkness Damage Settings")]
        [Tooltip("Damage dealt per interval when completely in darkness (0 light).")]
        [SerializeField] private int darknessDamage = 15;
        [SerializeField] private float darknessDamageInterval = 1.0f;

        [Header("Audio & Warnings")]
        [SerializeField] private AudioClip lowLightWarningSound;
        [SerializeField] private AudioClip darknessHitSound;

        private bool _isInSafeZone = false;
        private float _nextDamageTime = 0f;
        private IDamageable _playerDamageable;

        public float LightPercentage => Mathf.Clamp01(currentLightEnergy / maxLightEnergy);
        public float CurrentLightEnergy => currentLightEnergy;
        public bool IsInSafeZone => _isInSafeZone;

        private void Awake()
        {
            if (auraPointLight == null)
            {
                auraPointLight = GetComponentInChildren<Light>();
                if (auraPointLight == null)
                {
                    GameObject lightObj = new GameObject("PlayerLightAura");
                    lightObj.transform.SetParent(transform);
                    lightObj.transform.localPosition = new Vector3(0, 1.5f, 0);
                    auraPointLight = lightObj.AddComponent<Light>();
                    auraPointLight.type = LightType.Point;
                    auraPointLight.color = new Color(0.9f, 0.95f, 1f);
                }
            }

            _playerDamageable = GetComponent<IDamageable>();
        }

        private void Start()
        {
            currentLightEnergy = maxLightEnergy;
            UpdateLightVisuals();
        }

        private void Update()
        {
            if (!_isInSafeZone)
            {
                // Drain light energy in the dark
                currentLightEnergy = Mathf.Max(0f, currentLightEnergy - darknessDrainRate * Time.deltaTime);

                // Check if completely dark -> take damage
                if (currentLightEnergy <= 0f && Time.time >= _nextDamageTime)
                {
                    ApplyDarknessDamage();
                }
            }

            UpdateLightVisuals();
        }

        private void UpdateLightVisuals()
        {
            if (auraPointLight == null) return;

            float t = LightPercentage;
            auraPointLight.range = Mathf.Lerp(minLightRange, maxLightRange, t);
            auraPointLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, t);
        }

        private void ApplyDarknessDamage()
        {
            _nextDamageTime = Time.time + darknessDamageInterval;

            if (_playerDamageable != null)
            {
                _playerDamageable.TakeDamage(darknessDamage);
            }
            else
            {
                SendMessage("TakeDamage", darknessDamage, SendMessageOptions.DontRequireReceiver);
            }

            if (darknessHitSound != null)
            {
                AudioSource.PlayClipAtPoint(darknessHitSound, transform.position);
            }

            Debug.Log("[LightAura] Player taking DARKNESS DAMAGE!");
        }

        #region Public SafeZone API
        public void RechargeToMax()
        {
            currentLightEnergy = maxLightEnergy;
            UpdateLightVisuals();
        }

        public void NotifyInSafeZone(float refillAmount)
        {
            _isInSafeZone = true;
            currentLightEnergy = Mathf.Min(maxLightEnergy, currentLightEnergy + refillAmount);
            UpdateLightVisuals();
        }

        public void NotifyExitedSafeZone()
        {
            _isInSafeZone = false;
        }
        #endregion
    }
}
