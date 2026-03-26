using UnityEngine;
using System.Collections.Generic;

namespace SendIt.Customization
{
    /// <summary>
    /// Manages vehicle audio system customization including speaker upgrades,
    /// EQ settings, and audio profiles.
    /// </summary>
    public class AudioSystem : MonoBehaviour
    {
        [SerializeField] private AudioSource engineAudioSource;
        [SerializeField] private AudioSource musicAudioSource;
        [SerializeField] private AudioListener audioListener;

        // Audio system state
        private int speakerSystem = 0; // 0=Stock, 1=Premium, 2=High-End, 3=Custom
        private float bassBump = 0.5f; // 0-1
        private float treble = 0.5f; // 0-1
        private float midrange = 0.5f; // 0-1
        private float volume = 0.8f; // 0-1
        private bool enableSubwoofer = false;
        private float subwooferPower = 0.5f; // 0-1

        // Speaker configurations
        private Dictionary<int, SpeakerConfig> speakerConfigs = new Dictionary<int, SpeakerConfig>();

        [System.Serializable]
        public struct SpeakerConfig
        {
            public string Name;
            public int Channels; // 2, 4, 6, 8
            public float Power; // Watts
            public float Frequency; // Hz response
            public float Quality; // 0-1 sound quality rating
        }

        public struct AudioSettings
        {
            public int SpeakerSystem;
            public float BassBump;
            public float Treble;
            public float Midrange;
            public float Volume;
            public bool EnableSubwoofer;
            public float SubwooferPower;
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize audio system with preset speaker configurations.
        /// </summary>
        private void Initialize()
        {
            // Get or create audio sources
            if (engineAudioSource == null)
                engineAudioSource = gameObject.AddComponent<AudioSource>();
            if (musicAudioSource == null)
                musicAudioSource = gameObject.AddComponent<AudioSource>();

            // Setup speaker configurations
            speakerConfigs[0] = new SpeakerConfig
            {
                Name = "Stock",
                Channels = 2,
                Power = 10f,
                Frequency = 20000f,
                Quality = 0.5f
            };

            speakerConfigs[1] = new SpeakerConfig
            {
                Name = "Premium",
                Channels = 4,
                Power = 50f,
                Frequency = 20000f,
                Quality = 0.8f
            };

            speakerConfigs[2] = new SpeakerConfig
            {
                Name = "High-End",
                Channels = 6,
                Power = 150f,
                Frequency = 20000f,
                Quality = 0.95f
            };

            speakerConfigs[3] = new SpeakerConfig
            {
                Name = "Custom",
                Channels = 8,
                Power = 300f,
                Frequency = 20000f,
                Quality = 1.0f
            };

            ApplyAudioSettings();
        }

        /// <summary>
        /// Set speaker system tier.
        /// </summary>
        public void SetSpeakerSystem(int system)
        {
            speakerSystem = Mathf.Clamp(system, 0, speakerConfigs.Count - 1);
            ApplyAudioSettings();
        }

        /// <summary>
        /// Adjust bass frequencies.
        /// </summary>
        public void SetBassBump(float amount)
        {
            bassBump = Mathf.Clamp01(amount);
            ApplyAudioEQ();
        }

        /// <summary>
        /// Adjust treble frequencies.
        /// </summary>
        public void SetTreble(float amount)
        {
            treble = Mathf.Clamp01(amount);
            ApplyAudioEQ();
        }

        /// <summary>
        /// Adjust midrange frequencies.
        /// </summary>
        public void SetMidrange(float amount)
        {
            midrange = Mathf.Clamp01(amount);
            ApplyAudioEQ();
        }

        /// <summary>
        /// Set overall volume level.
        /// </summary>
        public void SetVolume(float level)
        {
            volume = Mathf.Clamp01(level);
            if (engineAudioSource != null)
                engineAudioSource.volume = volume * 0.7f;
            if (musicAudioSource != null)
                musicAudioSource.volume = volume * 0.8f;
        }

        /// <summary>
        /// Enable or disable subwoofer.
        /// </summary>
        public void SetSubwoofer(bool enabled)
        {
            enableSubwoofer = enabled;
            ApplyAudioEQ();
        }

        /// <summary>
        /// Set subwoofer power level.
        /// </summary>
        public void SetSubwooferPower(float power)
        {
            subwooferPower = Mathf.Clamp01(power);
            ApplyAudioEQ();
        }

        /// <summary>
        /// Apply audio EQ based on current settings.
        /// </summary>
        private void ApplyAudioEQ()
        {
            // In a full implementation, apply audio filter effects
            // Using Unity's AudioLowPassFilter, AudioHighPassFilter, AudioEchoFilter, etc.

            if (engineAudioSource != null)
            {
                // Adjust pitch based on bass/treble (simplified)
                float eqFactor = bassBump - treble;
                engineAudioSource.pitch = 1f + (eqFactor * 0.2f);
            }
        }

        /// <summary>
        /// Apply speaker system configuration.
        /// </summary>
        private void ApplyAudioSettings()
        {
            if (!speakerConfigs.ContainsKey(speakerSystem))
                return;

            SpeakerConfig config = speakerConfigs[speakerSystem];

            // Set audio source quality based on speaker system
            if (musicAudioSource != null)
            {
                // Higher quality systems have less compression
                musicAudioSource.pitch = 0.95f + (config.Quality * 0.1f);
            }

            ApplyAudioEQ();
        }

        /// <summary>
        /// Play engine sound with current audio configuration.
        /// </summary>
        public void PlayEngineSound(AudioClip engineSound, float pitch = 1f)
        {
            if (engineAudioSource == null)
                return;

            engineAudioSource.clip = engineSound;
            engineAudioSource.pitch = pitch;
            engineAudioSource.volume = volume * 0.7f;

            if (!engineAudioSource.isPlaying)
                engineAudioSource.Play();
        }

        /// <summary>
        /// Play music with current audio configuration.
        /// </summary>
        public void PlayMusic(AudioClip musicClip)
        {
            if (musicAudioSource == null)
                return;

            musicAudioSource.clip = musicClip;
            musicAudioSource.volume = volume * 0.8f;

            if (!musicAudioSource.isPlaying)
                musicAudioSource.Play();
        }

        /// <summary>
        /// Stop audio playback.
        /// </summary>
        public void StopAudio()
        {
            if (engineAudioSource != null)
                engineAudioSource.Stop();
            if (musicAudioSource != null)
                musicAudioSource.Stop();
        }

        /// <summary>
        /// Get current audio settings.
        /// </summary>
        public AudioSettings GetAudioSettings()
        {
            return new AudioSettings
            {
                SpeakerSystem = speakerSystem,
                BassBump = bassBump,
                Treble = treble,
                Midrange = midrange,
                Volume = volume,
                EnableSubwoofer = enableSubwoofer,
                SubwooferPower = subwooferPower
            };
        }

        /// <summary>
        /// Get speaker system info.
        /// </summary>
        public string GetSpeakerSystemInfo()
        {
            if (!speakerConfigs.ContainsKey(speakerSystem))
                return "Unknown";

            SpeakerConfig config = speakerConfigs[speakerSystem];
            return $"{config.Name} - {config.Channels}ch, {config.Power}W, Quality: {config.Quality * 100:F0}%";
        }

        // Getters
        public int GetSpeakerSystem() => speakerSystem;
        public float GetBassBump() => bassBump;
        public float GetTreble() => treble;
        public float GetMidrange() => midrange;
        public float GetVolume() => volume;
        public bool IsSubwooferEnabled() => enableSubwoofer;
        public float GetSubwooferPower() => subwooferPower;
    }
}
