using UnityEngine;
using SendIt.Physics;

namespace SendIt.Audio
{
    /// <summary>
    /// Manages tire sound effects including squealing and friction noise.
    /// Creates realistic tire audio based on slip ratio and slip angle.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class TireAudioSystem : MonoBehaviour
    {
        private AudioSource audioSource;
        private WheelContact[] wheelContacts;

        // Tire squeal parameters
        private float squealThreshold = 0.1f; // Minimum slip ratio to squeal
        private float maxSquealVolume = 0.6f;
        private float maxSquealPitch = 2f;

        // Tire temperatures affect sound
        private float[] tireTemperatures = new float[4];

        private bool isInitialized;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize tire audio system.
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

            audioSource.spatialBlend = 0.5f; // 3D audio - comes from wheel positions
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 50f;

            // Get wheel contacts from vehicle
            VehicleController vehicleController = GetComponentInParent<VehicleController>();
            if (vehicleController != null)
            {
                // We'll need to access wheel contacts through the vehicle
                // For now, find them in children
                wheelContacts = GetComponentsInChildren<WheelContact>();
            }

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || wheelContacts == null || wheelContacts.Length == 0)
                return;

            UpdateTireAudio();
        }

        /// <summary>
        /// Update tire audio based on wheel slip conditions.
        /// </summary>
        private void UpdateTireAudio()
        {
            float totalSquealVolume = 0f;
            float averageSquealPitch = 1f;
            int squealing = 0;

            // Process each wheel
            for (int i = 0; i < Mathf.Min(wheelContacts.Length, 4); i++)
            {
                if (wheelContacts[i] == null)
                    continue;

                float slipRatio = Mathf.Abs(wheelContacts[i].GetSlipRatio());
                float slipAngle = Mathf.Abs(wheelContacts[i].GetSlipAngle()) * Mathf.Rad2Deg;

                // Calculate squeal amount from slip ratio (longitudinal)
                float longitudinalSqueal = Mathf.Max(0f, (slipRatio - squealThreshold) / (1f - squealThreshold));

                // Calculate squeal amount from slip angle (lateral)
                float lateralSqueal = Mathf.Max(0f, (slipAngle - 5f) / 20f); // Start squealing above 5°

                // Combine both sources
                float wheelSqueal = Mathf.Max(longitudinalSqueal, lateralSqueal);

                if (wheelSqueal > 0.05f)
                {
                    totalSquealVolume += wheelSqueal;
                    squealing++;
                }
            }

            // Average the squeal from all wheels
            if (squealing > 0)
            {
                float squealVolume = (totalSquealVolume / squealing) * maxSquealVolume;
                float squealPitch = Mathf.Lerp(1f, maxSquealPitch, totalSquealVolume / squealing);

                audioSource.volume = squealVolume;
                audioSource.pitch = squealPitch;

                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
            else
            {
                audioSource.Stop();
            }
        }

        /// <summary>
        /// Set wheel contact references.
        /// </summary>
        public void SetWheelContacts(WheelContact[] contacts)
        {
            wheelContacts = contacts;
        }

        /// <summary>
        /// Set tire temperature for a wheel (affects audio characteristics).
        /// </summary>
        public void SetTireTemperature(int wheelIndex, float temperature)
        {
            if (wheelIndex >= 0 && wheelIndex < tireTemperatures.Length)
            {
                tireTemperatures[wheelIndex] = temperature;
            }
        }

        /// <summary>
        /// Get average tire squeal volume (0-1).
        /// </summary>
        public float GetSquealVolume() => audioSource.volume;

        /// <summary>
        /// Get average tire squeal pitch.
        /// </summary>
        public float GetSquealPitch() => audioSource.pitch;
    }
}
