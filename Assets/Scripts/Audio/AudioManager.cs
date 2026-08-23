using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MonkeyAdventure.Audio
{
    /// <summary>
    /// Master Audio Manager for Background Music (BGM) and Sound Effects (SFX).
    /// Persists across all 50 levels using DontDestroyOnLoad.
    /// Supports dynamic 2-second BGM crossfading and volume scaling.
    /// </summary>
    [AddComponentMenu("Monkey Adventure/Audio/Audio Manager")]
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Background Music (BGM)")]
        [Tooltip("List of looping music tracks for Acts, levels, and boss fights.")]
        [SerializeField] private Sound[] bgmSounds = new Sound[0];

        [Header("Sound Effects (SFX)")]
        [Tooltip("List of one-shot sound effects for actions, combat, collectibles, etc.")]
        [SerializeField] private Sound[] sfxSounds = new Sound[0];

        [Header("Global Volume Settings")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1.0f;

        [Header("Crossfade Settings")]
        [Tooltip("Duration in seconds for smooth BGM transitions between levels/acts.")]
        [SerializeField] private float defaultCrossfadeDuration = 2.0f;

        [Header("Auto-Play BGM on Start")]
        [SerializeField] private string initialBGMName = "BGM_Act1";

        // Runtime lookups
        private readonly Dictionary<string, Sound> _bgmDictionary = new Dictionary<string, Sound>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Sound> _sfxDictionary = new Dictionary<string, Sound>(StringComparer.OrdinalIgnoreCase);
        private Sound _currentBGM;
        private Coroutine _crossfadeCoroutine;

        public float MasterVolume => masterVolume;
        public float BGMVolume => bgmVolume;
        public float SFXVolume => sfxVolume;
        public Sound CurrentBGM => _currentBGM;

        private void Awake()
        {
            // Singleton & Persistence
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
        }

        private void Start()
        {
            // Auto play starting music if configured
            if (!string.IsNullOrEmpty(initialBGMName))
            {
                PlayBGM(initialBGMName);
            }
        }

        /// <summary>
        /// Instantiates and configures dedicated AudioSource components for each Sound entry.
        /// </summary>
        private void InitializeAudioSources()
        {
            // 1. Setup BGM AudioSources
            foreach (Sound s in bgmSounds)
            {
                if (s == null || string.IsNullOrEmpty(s.name)) continue;

                s.source = gameObject.AddComponent<AudioSource>();
                s.source.clip = s.clip;
                s.source.volume = s.volume * bgmVolume * masterVolume;
                s.source.pitch = s.pitch;
                s.source.loop = true;
                s.source.playOnAwake = false;

                if (!_bgmDictionary.ContainsKey(s.name))
                {
                    _bgmDictionary.Add(s.name, s);
                }
            }

            // 2. Setup SFX AudioSources
            foreach (Sound s in sfxSounds)
            {
                if (s == null || string.IsNullOrEmpty(s.name)) continue;

                s.source = gameObject.AddComponent<AudioSource>();
                s.source.clip = s.clip;
                s.source.volume = s.volume * sfxVolume * masterVolume;
                s.source.pitch = s.pitch;
                s.source.loop = s.loop;
                s.source.playOnAwake = false;

                if (!_sfxDictionary.ContainsKey(s.name))
                {
                    _sfxDictionary.Add(s.name, s);
                }
            }
        }

        #region SFX Playback
        /// <summary>
        /// Plays a one-shot sound effect by name (e.g. 'Jump', 'Attack', 'CollectCoin').
        /// </summary>
        /// <param name="soundName">Configured sound name</param>
        public void PlaySFX(string soundName)
        {
            if (string.IsNullOrEmpty(soundName)) return;

            if (_sfxDictionary.TryGetValue(soundName, out Sound sound))
            {
                if (sound.clip == null)
                {
                    Debug.LogWarning($"[AudioManager] Sound clip for '{soundName}' is null!", this);
                    return;
                }

                sound.source.volume = sound.volume * sfxVolume * masterVolume;
                sound.Play();
            }
            else
            {
                Debug.LogWarning($"[AudioManager] SFX '{soundName}' not found in AudioManager!", this);
            }
        }

        /// <summary>
        /// Plays a one-shot sound effect at a specific 3D world position.
        /// </summary>
        public void PlaySFXAtPosition(string soundName, Vector3 worldPosition)
        {
            if (string.IsNullOrEmpty(soundName)) return;

            if (_sfxDictionary.TryGetValue(soundName, out Sound sound))
            {
                if (sound.clip != null)
                {
                    float finalVol = sound.volume * sfxVolume * masterVolume;
                    AudioSource.PlayClipAtPoint(sound.clip, worldPosition, finalVol);
                }
            }
        }
        #endregion

        #region BGM Playback & Crossfade
        /// <summary>
        /// Plays a background music track. If a track is already playing, smoothly crossfades over 2 seconds.
        /// </summary>
        /// <param name="bgmName">Name of the target BGM track</param>
        /// <param name="crossfadeDuration">Optional override for crossfade length in seconds</param>
        public void PlayBGM(string bgmName, float crossfadeDuration = -1f)
        {
            if (string.IsNullOrEmpty(bgmName)) return;

            if (!_bgmDictionary.TryGetValue(bgmName, out Sound newBGM))
            {
                Debug.LogWarning($"[AudioManager] BGM track '{bgmName}' not found!", this);
                return;
            }

            // Already playing this exact track
            if (_currentBGM == newBGM && newBGM.source.isPlaying)
            {
                return;
            }

            float duration = crossfadeDuration >= 0f ? crossfadeDuration : defaultCrossfadeDuration;

            if (_crossfadeCoroutine != null)
            {
                StopCoroutine(_crossfadeCoroutine);
            }

            _crossfadeCoroutine = StartCoroutine(CrossfadeBGMRoutine(_currentBGM, newBGM, duration));
            _currentBGM = newBGM;
        }

        /// <summary>
        /// Coroutine to fade out the previous track while fading in the new track.
        /// </summary>
        private IEnumerator CrossfadeBGMRoutine(Sound oldBGM, Sound newBGM, float duration)
        {
            float elapsed = 0f;

            // Target volume for the new track
            float newTargetVolume = newBGM.volume * bgmVolume * masterVolume;

            // Start new track at 0 volume
            newBGM.source.volume = 0f;
            newBGM.source.Play();

            float oldStartVolume = oldBGM != null && oldBGM.source != null ? oldBGM.source.volume : 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Fade out old track
                if (oldBGM != null && oldBGM.source != null)
                {
                    oldBGM.source.volume = Mathf.Lerp(oldStartVolume, 0f, t);
                }

                // Fade in new track
                newBGM.source.volume = Mathf.Lerp(0f, newTargetVolume, t);

                yield return null;
            }

            // Stop old track once fully faded
            if (oldBGM != null && oldBGM.source != null)
            {
                oldBGM.source.Stop();
                oldBGM.source.volume = oldBGM.volume * bgmVolume * masterVolume;
            }

            newBGM.source.volume = newTargetVolume;
            _crossfadeCoroutine = null;
        }

        /// <summary>
        /// Stops the currently playing background music.
        /// </summary>
        public void StopBGM(float fadeOutDuration = 1.0f)
        {
            if (_currentBGM != null && _currentBGM.source.isPlaying)
            {
                StartCoroutine(FadeOutCurrentBGMRoutine(fadeOutDuration));
            }
        }

        private IEnumerator FadeOutCurrentBGMRoutine(float duration)
        {
            if (_currentBGM == null) yield break;

            float startVol = _currentBGM.source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _currentBGM.source.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }

            _currentBGM.source.Stop();
            _currentBGM = null;
        }
        #endregion

        #region Volume Controls
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }

        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            if (_currentBGM != null && _currentBGM.source != null)
            {
                _currentBGM.source.volume = _currentBGM.volume * bgmVolume * masterVolume;
            }
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        private void UpdateAllVolumes()
        {
            if (_currentBGM != null && _currentBGM.source != null)
            {
                _currentBGM.source.volume = _currentBGM.volume * bgmVolume * masterVolume;
            }

            foreach (var s in _sfxDictionary.Values)
            {
                if (s.source != null)
                {
                    s.source.volume = s.volume * sfxVolume * masterVolume;
                }
            }
        }
        #endregion
    }
}
