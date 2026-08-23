using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MonkeyAdventure.EditorTools
{
    /// <summary>
    /// Procedurally synthesizes retro-stylized, uncompressed 16-bit PCM WAV audio clips for all game BGM and SFX.
    /// Saves audio assets to Assets/Art/Audio/.
    /// </summary>
    public static class ProceduralAudioSynthesizer
    {
        private const string AUDIO_DIR = "Assets/Art/Audio";
        private const int SAMPLE_RATE = 22050; // Optimized for mobile memory footprint

        public static void SynthesizeAllAudioClips()
        {
            EnsureDirectoryExists(AUDIO_DIR);

            // BGM Tracks (Looped melodies & ambiences)
            CreateAct1BGM();
            CreateAct2BGM();
            CreateAct3BGM();
            CreateAct4BGM();
            CreateAct5BGM();
            CreateBossBGM();

            // Action SFX
            CreateToneSFX("SFX_Jump", 0.2f, 350f, 600f, 0.4f);
            CreateToneSFX("SFX_Land", 0.15f, 200f, 80f, 0.5f);
            CreateToneSFX("SFX_Attack", 0.2f, 440f, 220f, 0.6f);
            CreateToneSFX("SFX_HeavyAttack", 0.35f, 150f, 60f, 0.8f);
            CreateToneSFX("SFX_EnergyBlast", 0.3f, 800f, 200f, 0.5f);
            CreateNoiseSFX("SFX_Footstep", 0.08f, 0.25f);
            CreateChimeSFX("SFX_Coin", 0.25f, 987.77f, 1318.51f, 0.4f);
            CreateChimeSFX("SFX_Banana", 0.3f, 523.25f, 659.25f, 0.5f);
            CreateToneSFX("SFX_Hurt", 0.25f, 280f, 120f, 0.7f);
            CreateToneSFX("SFX_Death", 0.6f, 220f, 55f, 0.8f);
            CreateChimeSFX("SFX_Checkpoint", 0.5f, 659.25f, 1046.50f, 0.6f);
            CreateChimeSFX("SFX_RuneActivate", 0.4f, 440f, 880f, 0.5f);
            CreateRumbleSFX("SFX_DoorOpen", 0.8f, 0.6f);
            CreateFanfareSFX("SFX_LevelComplete", 1.2f, 0.7f);
            CreateToneSFX("SFX_EnemyHit", 0.18f, 300f, 150f, 0.5f);
            CreateRumbleSFX("SFX_BossRoar", 0.9f, 0.8f);
            CreateToneSFX("SFX_UIClick", 0.06f, 1200f, 800f, 0.3f);
            CreateNoiseSFX("SFX_WaterSplash", 0.4f, 0.5f);
            CreateNoiseSFX("SFX_FireCrackle", 0.5f, 0.4f);
            CreateToneSFX("SFX_PoisonBubble", 0.25f, 400f, 700f, 0.4f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ProceduralAudioSynthesizer] All BGM and SFX WAV audio clips generated in Assets/Art/Audio/!");
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        #region BGM Synthesizers
        private static void CreateAct1BGM()
        {
            // Upbeat tropical pentatonic loop (4.0 seconds)
            float duration = 4.0f;
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];
            float[] notes = { 261.63f, 293.66f, 329.63f, 392.00f, 440.00f, 523.25f }; // C pentatonic

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                int noteIndex = ((int)(t * 4)) % notes.Length;
                float freq = notes[noteIndex];

                // Melodic marimba tone
                float melody = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.25f * Mathf.Exp(-(t % 0.25f) * 6f);
                // Bass drone
                float bass = Mathf.Sin(2f * Mathf.PI * (notes[0] * 0.5f) * t) * 0.15f;
                // Jungle ambient breeze
                float breeze = (Mathf.PerlinNoise(t * 2f, 0f) - 0.5f) * 0.05f;

                samples[i] = Mathf.Clamp(melody + bass + breeze, -1f, 1f);
            }

            WriteWavFile($"{AUDIO_DIR}/BGM_Act1.wav", samples, SAMPLE_RATE);
        }

        private static void CreateAct2BGM()
        {
            // Mysterious mist & deep bamboo wood block (4.0s)
            float duration = 4.0f;
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];
            float[] notes = { 220.00f, 261.63f, 293.66f, 349.23f, 392.00f }; // A minor

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                int noteIndex = ((int)(t * 2)) % notes.Length;
                float freq = notes[noteIndex];

                float melody = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.2f * Mathf.Exp(-(t % 0.5f) * 4f);
                float subBass = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.18f;
                float rainMist = (UnityEngine.Random.value - 0.5f) * 0.04f;

                samples[i] = Mathf.Clamp(melody + subBass + rainMist, -1f, 1f);
            }

            WriteWavFile($"{AUDIO_DIR}/BGM_Act2.wav", samples, SAMPLE_RATE);
        }

        private static void CreateAct3BGM()
        {
            // Flowing water river melody (4.0s)
            float duration = 4.0f;
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];
            float[] notes = { 329.63f, 392.00f, 493.88f, 587.33f, 659.25f }; // E minor

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                int noteIndex = ((int)(t * 3)) % notes.Length;
                float freq = notes[noteIndex];

                float melody = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.22f * Mathf.Exp(-(t % 0.333f) * 5f);
                float riverFlow = Mathf.Sin(2f * Mathf.PI * 164.81f * t) * 0.15f + (UnityEngine.Random.value - 0.5f) * 0.03f;

                samples[i] = Mathf.Clamp(melody + riverFlow, -1f, 1f);
            }

            WriteWavFile($"{AUDIO_DIR}/BGM_Act3.wav", samples, SAMPLE_RATE);
        }

        private static void CreateAct4BGM()
        {
            // Dark forest bioluminescent shimmer (4.0s)
            float duration = 4.0f;
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];
            float[] notes = { 196.00f, 233.08f, 293.66f, 311.13f, 392.00f }; // G minor

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                int noteIndex = ((int)(t * 1.5f)) % notes.Length;
                float freq = notes[noteIndex];

                float shimmer = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Sin(2f * Mathf.PI * 6f * t) * 0.2f;
                float drone = Mathf.Sin(2f * Mathf.PI * 98f * t) * 0.18f;

                samples[i] = Mathf.Clamp(shimmer + drone, -1f, 1f);
            }

            WriteWavFile($"{AUDIO_DIR}/BGM_Act4.wav", samples, SAMPLE_RATE);
        }

        private static void CreateAct5BGM()
        {
            // Celestial corrupted temple tension (4.0s)
            float duration = 4.0f;
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];
            float[] notes = { 174.61f, 220.00f, 261.63f, 329.63f, 349.23f }; // F major/minor tension

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                int noteIndex = ((int)(t * 2)) % notes.Length;
                float freq = notes[noteIndex];

                float brass = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.25f + Mathf.Sin(4f * Mathf.PI * freq * t) * 0.1f;
                float storm = (Mathf.PerlinNoise(t * 4f, 0f) - 0.5f) * 0.08f;

                samples[i] = Mathf.Clamp(brass + storm, -1f, 1f);
            }

            WriteWavFile($"{AUDIO_DIR}/BGM_Act5.wav", samples, SAMPLE_RATE);
        }

        private static void CreateBossBGM()
        {
            // Intense rhythmic boss battle beat (3.0s)
            float duration = 3.0f;
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / SAMPLE_RATE;
                float drum = Mathf.Sin(2f * Mathf.PI * 60f * t) * Mathf.Exp(-(t % 0.25f) * 15f) * 0.4f;
                float brass = Mathf.Sin(2f * Mathf.PI * 130.81f * t) * 0.2f;
                float hihat = ((t % 0.125f) < 0.03f ? (UnityEngine.Random.value - 0.5f) * 0.15f : 0f);

                samples[i] = Mathf.Clamp(drum + brass + hihat, -1f, 1f);
            }

            WriteWavFile($"{AUDIO_DIR}/BGM_Boss.wav", samples, SAMPLE_RATE);
        }
        #endregion

        #region SFX Synthesizers
        private static void CreateToneSFX(string name, float duration, float startFreq, float endFreq, float volume)
        {
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float currentFreq = Mathf.Lerp(startFreq, endFreq, t);
                float env = Mathf.Sin(t * Mathf.PI);
                samples[i] = Mathf.Sin(2f * Mathf.PI * currentFreq * ((float)i / SAMPLE_RATE)) * volume * env;
            }

            WriteWavFile($"{AUDIO_DIR}/{name}.wav", samples, SAMPLE_RATE);
        }

        private static void CreateChimeSFX(string name, float duration, float freq1, float freq2, float volume)
        {
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float timeSec = (float)i / SAMPLE_RATE;
                float decay = Mathf.Exp(-t * 4f);
                float wave = (Mathf.Sin(2f * Mathf.PI * freq1 * timeSec) + Mathf.Sin(2f * Mathf.PI * freq2 * timeSec)) * 0.5f;
                samples[i] = wave * volume * decay;
            }

            WriteWavFile($"{AUDIO_DIR}/{name}.wav", samples, SAMPLE_RATE);
        }

        private static void CreateNoiseSFX(string name, float duration, float volume)
        {
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float env = Mathf.Exp(-t * 5f);
                samples[i] = (UnityEngine.Random.value * 2f - 1f) * volume * env;
            }

            WriteWavFile($"{AUDIO_DIR}/{name}.wav", samples, SAMPLE_RATE);
        }

        private static void CreateRumbleSFX(string name, float duration, float volume)
        {
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float timeSec = (float)i / SAMPLE_RATE;
                float lowSine = Mathf.Sin(2f * Mathf.PI * 45f * timeSec) * 0.6f;
                float noise = (UnityEngine.Random.value * 2f - 1f) * 0.4f;
                float env = Mathf.Sin(t * Mathf.PI);
                samples[i] = (lowSine + noise) * volume * env;
            }

            WriteWavFile($"{AUDIO_DIR}/{name}.wav", samples, SAMPLE_RATE);
        }

        private static void CreateFanfareSFX(string name, float duration, float volume)
        {
            int totalSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[totalSamples];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C, E, G, High C

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float timeSec = (float)i / SAMPLE_RATE;
                int noteIndex = Mathf.Clamp((int)(t * 4), 0, 3);
                float freq = notes[noteIndex];
                float env = Mathf.Exp(-(t % 0.25f) * 5f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * timeSec) * volume * env;
            }

            WriteWavFile($"{AUDIO_DIR}/{name}.wav", samples, SAMPLE_RATE);
        }
        #endregion

        #region WAV Binary Writer
        private static void WriteWavFile(string filePath, float[] samples, int sampleRate)
        {
            int byteCount = samples.Length * 2; // 16-bit PCM (2 bytes per sample)
            using (var stream = new FileStream(filePath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                // RIFF header
                writer.Write(new char[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + byteCount);
                writer.Write(new char[] { 'W', 'A', 'V', 'E' });

                // fmt chunk
                writer.Write(new char[] { 'f', 'm', 't', ' ' });
                writer.Write(16); // SubChunk1Size (16 for PCM)
                writer.Write((short)1); // AudioFormat (1 = PCM)
                writer.Write((short)1); // NumChannels (1 = Mono)
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2); // ByteRate (SampleRate * NumChannels * BitsPerSample/8)
                writer.Write((short)2); // BlockAlign (NumChannels * BitsPerSample/8)
                writer.Write((short)16); // BitsPerSample

                // data chunk
                writer.Write(new char[] { 'd', 'a', 't', 'a' });
                writer.Write(byteCount);

                for (int i = 0; i < samples.Length; i++)
                {
                    short pcmSample = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767);
                    writer.Write(pcmSample);
                }
            }
        }
        #endregion
    }
}
