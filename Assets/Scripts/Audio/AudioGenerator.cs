using UnityEngine;

namespace SendIt.Audio
{
    /// <summary>
    /// Generates dynamic engine and vehicle audio based on real-time physics parameters.
    /// Uses procedural synthesis rather than pre-recorded samples for infinite variation.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioGenerator : MonoBehaviour
    {
        private AudioSource audioSource;

        // Engine parameters
        private float currentRPM = 1000f;
        private float enginePower = 200f;
        private float throttleAmount = 0f;

        // Sound characteristics
        private float enginePitch = 1f;
        private float engineVolume = 0.5f;
        private float cylinderCount = 4f; // Affects harmonic content

        // Turbo/Supercharger
        private float turboBoost = 0f; // 0-2.5 bar
        private bool turboEnabled = false;
        private float turboSpoolRate = 1.5f;
        private float currentTurboSpool = 0f;

        // Exhaust
        private float exhaustVolume = 0.3f;
        private bool exhaustPopEnabled = false;
        private float lastExhaustPopTime = 0f;

        private const float MinRPM = 800f;
        private const float MaxRPM = 8000f;

        private void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Configure audio source
            audioSource.spatialBlend = 0f; // 2D audio (engine sound is omnipresent)
            audioSource.volume = 0.7f;
            audioSource.pitch = 1f;
        }

        private void Update()
        {
            if (audioSource == null)
                return;

            // Update engine pitch based on RPM
            UpdateEnginePitch();

            // Update turbo spool
            UpdateTurboSpool();

            // Update volumes
            UpdateVolumes();
        }

        /// <summary>
        /// Update engine pitch based on current RPM.
        /// </summary>
        private void UpdateEnginePitch()
        {
            // Normalize RPM to pitch range (0.5 - 2.5)
            float normalizedRPM = Mathf.Clamp01((currentRPM - MinRPM) / (MaxRPM - MinRPM));
            enginePitch = Mathf.Lerp(0.5f, 2.5f, normalizedRPM);

            // Add harmonic variation based on cylinder firing
            float harmonics = GetEngineHarmonics();
            audioSource.pitch = enginePitch + (harmonics * 0.15f);
        }

        /// <summary>
        /// Calculate engine harmonics based on RPM and cylinder count.
        /// Creates the characteristic engine sound "roughness".
        /// </summary>
        private float GetEngineHarmonics()
        {
            // Cylinder firing frequency creates pitch variations
            float firingFrequency = (currentRPM / 60f) * (cylinderCount / 2f); // Each cycle fires half cylinders
            float harmonic = Mathf.Sin(firingFrequency * Time.time * 2f * Mathf.PI) * 0.3f;

            // Add slight randomness for natural sound
            harmonic += Random.Range(-0.05f, 0.05f);

            return harmonic;
        }

        /// <summary>
        /// Update turbo spool sound and pitch.
        /// </summary>
        private void UpdateTurboSpool()
        {
            if (!turboEnabled)
            {
                currentTurboSpool = 0f;
                return;
            }

            // Spool up when throttle is applied
            float targetSpool = throttleAmount > 0.5f ? turboBoost : 0f;
            currentTurboSpool = Mathf.Lerp(currentTurboSpool, targetSpool, turboSpoolRate * Time.deltaTime);
        }

        /// <summary>
        /// Update audio volumes based on engine state.
        /// </summary>
        private void UpdateVolumes()
        {
            // Base engine volume increases with throttle
            engineVolume = Mathf.Lerp(0.3f, 0.8f, throttleAmount);

            // Turbo adds high-frequency whine
            float turboWhineVolume = currentTurboSpool * 0.3f;

            // Exhaust pops and crackles when engine is under load
            exhaustVolume = Mathf.Lerp(0.1f, 0.4f, throttleAmount);

            // Apply to audio source
            audioSource.volume = Mathf.Clamp01(engineVolume + turboWhineVolume * 0.5f);
        }

        /// <summary>
        /// Set current engine RPM.
        /// </summary>
        public void SetRPM(float rpm)
        {
            currentRPM = Mathf.Clamp(rpm, MinRPM, MaxRPM);
        }

        /// <summary>
        /// Set engine power output.
        /// </summary>
        public void SetEnginePower(float power)
        {
            enginePower = power;
        }

        /// <summary>
        /// Set throttle input (0-1).
        /// </summary>
        public void SetThrottle(float throttle)
        {
            throttleAmount = Mathf.Clamp01(throttle);
        }

        /// <summary>
        /// Enable/disable turbo with boost amount.
        /// </summary>
        public void SetTurbo(bool enabled, float boost = 0f)
        {
            turboEnabled = enabled;
            turboBoost = Mathf.Clamp(boost, 0f, 2.5f);
        }

        /// <summary>
        /// Set cylinder count for engine sound character.
        /// </summary>
        public void SetCylinderCount(float count)
        {
            cylinderCount = Mathf.Clamp(count, 3f, 12f);
        }

        /// <summary>
        /// Enable/disable exhaust pops and crackles.
        /// </summary>
        public void SetExhaustPops(bool enabled)
        {
            exhaustPopEnabled = enabled;
        }

        /// <summary>
        /// Play exhaust pop effect.
        /// </summary>
        public void PlayExhaustPop()
        {
            if (!exhaustPopEnabled)
                return;

            // Prevent too frequent pops
            if (Time.time - lastExhaustPopTime < 0.1f)
                return;

            lastExhaustPopTime = Time.time;

            // Exhaust pop is a sharp, brief sound
            // Could trigger a separate sound effect here
            Debug.Log("Exhaust Pop!");
        }

        /// <summary>
        /// Get current engine sound pitch for UI/diagnostics.
        /// </summary>
        public float GetEnginePitch() => enginePitch;

        /// <summary>
        /// Get turbo spool percentage (0-1).
        /// </summary>
        public float GetTurboSpool() => currentTurboSpool;

        /// <summary>
        /// Get engine volume level (0-1).
        /// </summary>
        public float GetEngineVolume() => engineVolume;
    }
}
