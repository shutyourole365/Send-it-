using UnityEngine;

namespace SendIt.Audio
{
    /// <summary>
    /// Manages exhaust sound effects including pops, backfires, and crackling.
    /// Creates characteristic exhaust sounds based on engine state changes.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ExhaustSystem : MonoBehaviour
    {
        private AudioSource audioSource;

        // Exhaust characteristics
        private float currentRPM = 1000f;
        private float throttle = 0f;
        private float previousThrottle = 0f;
        private int currentGear = 1;
        private int previousGear = 1;

        // Pop and crackle parameters
        private float lastPopTime = 0f;
        private float minPopInterval = 0.05f; // Minimum time between pops
        private float popIntensity = 0f;

        // Backfire tuning
        private float backfireThreshold = 0.3f; // Throttle drop amount to trigger backfire
        private bool backfireEnabled = true;

        private bool isInitialized;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize exhaust audio system.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.spatialBlend = 0.8f; // Mostly 3D audio
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 100f;
            audioSource.volume = 0.5f;

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            UpdateExhaustSounds();

            previousThrottle = throttle;
            previousGear = currentGear;
        }

        /// <summary>
        /// Update exhaust sounds based on engine state.
        /// </summary>
        private void UpdateExhaustSounds()
        {
            // Check for gear change backfire/pop
            if (previousGear != currentGear && currentGear < previousGear)
            {
                // Downshift detected - create backfire
                TriggerBackfire();
            }

            // Check for throttle lift backfire (fuel rich condition)
            if (backfireEnabled && throttle < previousThrottle - backfireThreshold && currentRPM > 3000f)
            {
                TriggerBackfire();
            }

            // Natural exhaust crackling during high RPM deceleration
            if (currentRPM > 5000f && throttle < 0.3f)
            {
                UpdateCrackling();
            }
            else
            {
                popIntensity = 0f;
            }
        }

        /// <summary>
        /// Trigger exhaust pop/backfire sound.
        /// </summary>
        private void TriggerBackfire()
        {
            if (Time.time - lastPopTime < minPopInterval)
                return;

            lastPopTime = Time.time;

            // Play exhaust pop sound
            popIntensity = 1f;

            // Could trigger AudioClip playback here for actual backfire sound
            Debug.Log($"Backfire! RPM: {currentRPM:F0}, Throttle: {throttle:F2}");
        }

        /// <summary>
        /// Update crackling sound during high-RPM deceleration.
        /// </summary>
        private void UpdateCrackling()
        {
            // Crackling intensity increases with engine RPM
            popIntensity = Mathf.Lerp(0f, 0.6f, (currentRPM - 5000f) / 3000f);

            // Add random pops occasionally
            if (Random.value < (popIntensity * Time.deltaTime))
            {
                TriggerBackfire();
            }
        }

        /// <summary>
        /// Set current engine RPM.
        /// </summary>
        public void SetRPM(float rpm)
        {
            currentRPM = rpm;
        }

        /// <summary>
        /// Set throttle input (0-1).
        /// </summary>
        public void SetThrottle(float throttleInput)
        {
            throttle = Mathf.Clamp01(throttleInput);
        }

        /// <summary>
        /// Set current gear.
        /// </summary>
        public void SetGear(int gear)
        {
            currentGear = gear;
        }

        /// <summary>
        /// Enable/disable backfire effects.
        /// </summary>
        public void SetBackfireEnabled(bool enabled)
        {
            backfireEnabled = enabled;
        }

        /// <summary>
        /// Set backfire threshold for throttle lift backfire.
        /// </summary>
        public void SetBackfireThreshold(float threshold)
        {
            backfireThreshold = Mathf.Clamp01(threshold);
        }

        /// <summary>
        /// Get current pop/crackle intensity (0-1).
        /// </summary>
        public float GetPopIntensity() => popIntensity;

        /// <summary>
        /// Get exhaust audio volume.
        /// </summary>
        public float GetExhaustVolume() => audioSource.volume;
    }
}
