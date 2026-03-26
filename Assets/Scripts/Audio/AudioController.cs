using UnityEngine;
using SendIt.Physics;

namespace SendIt.Audio
{
    /// <summary>
    /// Central controller for all vehicle audio systems.
    /// Coordinates engine audio, tire audio, and exhaust effects.
    /// Integrates with vehicle physics for realistic audio feedback.
    /// </summary>
    public class AudioController : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicleController;

        private AudioGenerator audioGenerator;
        private TireAudioSystem tireAudioSystem;
        private ExhaustSystem exhaustSystem;

        // Audio customization
        private float engineSoundIntensity = 1f; // Multiplier for all engine sounds
        private int cylinderCount = 4; // Stock: 4, 6, 8 cylinders
        private bool turboEnabled = false;
        private float turboBoost = 0f; // 0-2.5 bar

        private bool isInitialized;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize all audio systems.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            // Get vehicle controller reference
            if (vehicleController == null)
            {
                vehicleController = GetComponent<VehicleController>();
            }

            // Create or find audio generator
            audioGenerator = GetComponent<AudioGenerator>();
            if (audioGenerator == null)
            {
                audioGenerator = gameObject.AddComponent<AudioGenerator>();
            }

            // Create tire audio system
            GameObject tireAudioObj = new GameObject("TireAudio");
            tireAudioObj.transform.SetParent(transform);
            tireAudioSystem = tireAudioObj.AddComponent<TireAudioSystem>();
            tireAudioSystem.Initialize();

            // Create exhaust system
            GameObject exhaustObj = new GameObject("ExhaustAudio");
            exhaustObj.transform.SetParent(transform);
            exhaustSystem = exhaustObj.AddComponent<ExhaustSystem>();
            exhaustSystem.Initialize();

            isInitialized = true;
            Debug.Log("AudioController initialized");
        }

        private void FixedUpdate()
        {
            if (!isInitialized || vehicleController == null)
                return;

            UpdateAudioFromVehicle();
        }

        /// <summary>
        /// Update all audio systems based on vehicle state.
        /// </summary>
        private void UpdateAudioFromVehicle()
        {
            // Get vehicle data
            float currentRPM = vehicleController.GetCurrentRPM();
            float throttle = Input.GetAxis("Vertical");
            int gear = vehicleController.GetCurrentGear();
            float speed = vehicleController.GetSpeed();
            float enginePower = vehicleController.GetEnginePower();
            Telemetry telemetry = vehicleController.GetTelemetry();

            // Update engine audio
            audioGenerator.SetRPM(currentRPM);
            audioGenerator.SetEnginePower(enginePower);
            audioGenerator.SetThrottle(throttle);
            audioGenerator.SetCylinderCount(cylinderCount);
            audioGenerator.SetTurbo(turboEnabled, turboBoost);

            // Update exhaust audio
            exhaustSystem.SetRPM(currentRPM);
            exhaustSystem.SetThrottle(throttle);
            exhaustSystem.SetGear(gear);

            // Update tire audio (will pull from wheel contacts)
            UpdateTireAudio();
        }

        /// <summary>
        /// Update tire audio based on wheel slip conditions.
        /// </summary>
        private void UpdateTireAudio()
        {
            // Tire audio system updates from wheel contacts
            // This is handled internally in TireAudioSystem.Update()
        }

        /// <summary>
        /// Set engine sound characteristics.
        /// </summary>
        public void SetEngineSoundProfile(int cylinders, bool hasTurbo, float boost)
        {
            cylinderCount = Mathf.Clamp(cylinders, 3, 12);
            turboEnabled = hasTurbo;
            turboBoost = Mathf.Clamp(boost, 0f, 2.5f);
        }

        /// <summary>
        /// Set exhaust sound profile.
        /// </summary>
        public void SetExhaustProfile(bool backfiresEnabled, float backfireThreshold)
        {
            exhaustSystem.SetBackfireEnabled(backfiresEnabled);
            exhaustSystem.SetBackfireThreshold(backfireThreshold);
        }

        /// <summary>
        /// Set overall engine sound intensity multiplier.
        /// </summary>
        public void SetEngineSoundIntensity(float intensity)
        {
            engineSoundIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// Get audio diagnostic information.
        /// </summary>
        public string GetAudioDiagnostics()
        {
            string info = "\n=== AUDIO DIAGNOSTICS ===\n";
            info += $"Engine Pitch: {audioGenerator.GetEnginePitch():F2}\n";
            info += $"Engine Volume: {audioGenerator.GetEngineVolume():F2}\n";
            info += $"Turbo Spool: {audioGenerator.GetTurboSpool() * 100:F1}%\n";
            info += $"Tire Squeal: {tireAudioSystem.GetSquealVolume():F2}\n";
            info += $"Exhaust Pop: {exhaustSystem.GetPopIntensity():F2}\n";
            info += $"Cylinders: {cylinderCount:F0}\n";
            info += $"Turbo: {(turboEnabled ? "Enabled" : "Disabled")} ({turboBoost:F1} bar)\n";
            return info;
        }

        // Getters
        public AudioGenerator GetAudioGenerator() => audioGenerator;
        public TireAudioSystem GetTireAudioSystem() => tireAudioSystem;
        public ExhaustSystem GetExhaustSystem() => exhaustSystem;
        public float GetEngineSoundIntensity() => engineSoundIntensity;
    }
}
