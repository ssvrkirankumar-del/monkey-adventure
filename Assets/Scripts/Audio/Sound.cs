using System;
using UnityEngine;

namespace MonkeyAdventure.Audio
{
    /// <summary>
    /// Serializable class defining an audio track or sound effect profile.
    /// Holds clip references, volume/pitch settings, loop toggles, and runtime AudioSource instances.
    /// </summary>
    [Serializable]
    public class Sound
    {
        [Tooltip("Unique identifier name for this sound (e.g. 'Jump', 'BGM_Act1', 'CoinCollect').")]
        public string name;

        [Tooltip("The audio asset clip.")]
        public AudioClip clip;

        [Range(0f, 1f)]
        [Tooltip("Default volume level for this sound.")]
        public float volume = 1f;

        [Range(0.1f, 3f)]
        [Tooltip("Default pitch playback multiplier.")]
        public float pitch = 1f;

        [Tooltip("Whether this sound should loop continuously (recommended true for BGM).")]
        public bool loop = false;

        [Tooltip("Add subtle random pitch variation when playing SFX for organic sound feel.")]
        [Range(0f, 0.3f)]
        public float pitchVariation = 0f;

        [HideInInspector]
        public AudioSource source;

        public void Play()
        {
            if (source == null) return;

            if (pitchVariation > 0f)
            {
                source.pitch = pitch + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
            }
            else
            {
                source.pitch = pitch;
            }

            source.Play();
        }

        public void Stop()
        {
            if (source != null && source.isPlaying)
            {
                source.Stop();
            }
        }

        public void Pause()
        {
            if (source != null && source.isPlaying)
            {
                source.Pause();
            }
        }

        public void UnPause()
        {
            if (source != null)
            {
                source.UnPause();
            }
        }
    }
}
