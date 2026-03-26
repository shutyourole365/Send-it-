using UnityEngine;
using SendIt.Data;
using SendIt.Tuning;
using SendIt.Physics;

namespace SendIt.Customization
{
    /// <summary>
    /// Central manager for all vehicle customization systems.
    /// Orchestrates audio, interior, engine, garage, and visual modifications.
    /// </summary>
    public class CustomizationManager : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicleController;

        private TuningManager tuningManager;
        private AudioSystem audioSystem;
        private InteriorCustomizer interiorCustomizer;
        private BootModifier bootModifier;
        private AdvancedEngineCustomizer engineCustomizer;
        private GarageModifications garageModifications;
        private VisualCustomizer visualCustomizer;

        private bool isInitialized;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize all customization systems.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            tuningManager = TuningManager.Instance;
            if (tuningManager == null)
            {
                Debug.LogError("TuningManager not found!");
                return;
            }

            // Initialize audio system
            audioSystem = gameObject.AddComponent<AudioSystem>();
            audioSystem.Initialize();

            // Initialize interior customizer
            interiorCustomizer = gameObject.AddComponent<InteriorCustomizer>();
            interiorCustomizer.Initialize();

            // Initialize boot modifier
            bootModifier = gameObject.AddComponent<BootModifier>();
            bootModifier.Initialize();

            // Initialize advanced engine customizer
            engineCustomizer = gameObject.AddComponent<AdvancedEngineCustomizer>();
            engineCustomizer.Initialize(tuningManager.GetVehicleData().Physics);

            // Initialize garage modifications
            garageModifications = gameObject.AddComponent<GarageModifications>();
            garageModifications.Initialize(tuningManager.GetVehicleData().Physics);

            // Initialize visual customizer
            visualCustomizer = gameObject.AddComponent<VisualCustomizer>();
            visualCustomizer.Initialize();

            isInitialized = true;
            Debug.Log("CustomizationManager initialized successfully");
        }

        /// <summary>
        /// Get the total performance rating from all modifications.
        /// </summary>
        public float GetTotalPerformanceRating()
        {
            float enginePerf = engineCustomizer.GetPowerMultiplier();
            float garagePerf = garageModifications.GetPerformanceRating();

            return (enginePerf * 25f) + garagePerf;
        }

        /// <summary>
        /// Get customization summary.
        /// </summary>
        public string GetCustomizationSummary()
        {
            string summary = "\n=== VEHICLE CUSTOMIZATION SUMMARY ===\n";

            // Audio
            summary += $"Audio: {audioSystem.GetSpeakerSystemInfo()}\n";

            // Interior
            summary += $"Seats: {interiorCustomizer.GetSeatDescription()}\n";

            // Boot
            summary += $"Cargo: {bootModifier.GetCargoInfo()}\n";

            // Engine
            summary += $"Engine: {engineCustomizer.GetModificationSummary()}\n";

            // Garage
            summary += $"Suspension: {garageModifications.GetSuspensionKit()}\n";
            summary += $"Brakes: {garageModifications.GetBrakeSystem()}\n";

            // Visual
            summary += $"Headlights: Type {visualCustomizer.GetHeadlightType()}\n";

            return summary;
        }

        /// <summary>
        /// Apply all customizations to the vehicle.
        /// </summary>
        public void ApplyAllCustomizations()
        {
            if (!isInitialized)
                return;

            // Apply engine modifications to physics
            engineCustomizer.ApplyModificationsToEngine(null); // Would pass engine reference

            // Engine customizations affect top speed and acceleration
            float powerMultiplier = engineCustomizer.GetPowerMultiplier();
            Debug.Log($"Engine power multiplied by {powerMultiplier}x");

            // Garage modifications affect handling and performance
            float gripMultiplier = garageModifications.GetCorneringGripMultiplier();
            float accelMultiplier = garageModifications.GetAccelerationMultiplier();
            float speedMultiplier = garageModifications.GetTopSpeedMultiplier();

            Debug.Log($"Performance - Grip: {gripMultiplier}x, Accel: {accelMultiplier}x, TopSpeed: {speedMultiplier}x");
        }

        /// <summary>
        /// Reset all customizations to stock.
        /// </summary>
        public void ResetAllCustomizations()
        {
            audioSystem.SetSpeakerSystem(0);
            interiorCustomizer.SetSeatStyle(0);
            bootModifier.SetCargoSetup(0);
            engineCustomizer.SetInletType(0);
            garageModifications.SetSuspensionKit(0);
            visualCustomizer.SetHeadlightType(0);

            Debug.Log("All customizations reset to stock");
        }

        // Getters for all customization systems
        public AudioSystem GetAudioSystem() => audioSystem;
        public InteriorCustomizer GetInteriorCustomizer() => interiorCustomizer;
        public BootModifier GetBootModifier() => bootModifier;
        public AdvancedEngineCustomizer GetEngineCustomizer() => engineCustomizer;
        public GarageModifications GetGarageModifications() => garageModifications;
        public VisualCustomizer GetVisualCustomizer() => visualCustomizer;

        public static CustomizationManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
    }
}
