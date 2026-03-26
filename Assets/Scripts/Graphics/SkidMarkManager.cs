using UnityEngine;
using SendIt.Physics;

namespace SendIt.Graphics
{
    /// <summary>
    /// Integrates skid marks, surface deformation, and dirt accumulation systems.
    /// Subscribes to wheel contact events and coordinates visual feedback.
    /// </summary>
    public class SkidMarkManager : MonoBehaviour
    {
        private SkidMarkSystem skidMarkSystem;
        private SurfaceDeformation surfaceDeformation;
        private DirtAccumulation dirtAccumulation;
        private VehicleController vehicleController;
        private WheelContact[] wheelContacts;
        private TerrainMaterialManager terrainMaterialManager;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the skid mark manager and its subsystems.
        /// </summary>
        public void Initialize()
        {
            // Get or create skid mark systems
            skidMarkSystem = GetComponent<SkidMarkSystem>();
            if (skidMarkSystem == null)
            {
                skidMarkSystem = gameObject.AddComponent<SkidMarkSystem>();
            }

            surfaceDeformation = GetComponent<SurfaceDeformation>();
            if (surfaceDeformation == null)
            {
                surfaceDeformation = gameObject.AddComponent<SurfaceDeformation>();
            }

            dirtAccumulation = GetComponent<DirtAccumulation>();
            if (dirtAccumulation == null)
            {
                dirtAccumulation = gameObject.AddComponent<DirtAccumulation>();
            }

            // Get vehicle components
            vehicleController = GetComponent<VehicleController>();
            if (vehicleController == null)
            {
                Debug.LogError("VehicleController not found on this gameobject!");
                return;
            }

            // Get terrain material manager
            terrainMaterialManager = TerrainMaterialManager.Instance;
            if (terrainMaterialManager == null)
            {
                // Create one if it doesn't exist
                GameObject managerObject = new GameObject("TerrainMaterialManager");
                terrainMaterialManager = managerObject.AddComponent<TerrainMaterialManager>();
            }

            // Get wheel contacts
            wheelContacts = GetComponentsInChildren<WheelContact>();
            if (wheelContacts.Length == 0)
            {
                Debug.LogWarning("No WheelContact components found for skid mark system");
                return;
            }

            // Subscribe to wheel contact events
            SubscribeToWheelContacts();
        }

        /// <summary>
        /// Subscribe to wheel contact events.
        /// </summary>
        private void SubscribeToWheelContacts()
        {
            foreach (var wheelContact in wheelContacts)
            {
                if (wheelContact != null)
                {
                    // Note: This assumes WheelContact has these callback methods
                    // They would need to be implemented in WheelContact class
                }
            }
        }

        /// <summary>
        /// Process wheel contact and create visual effects.
        /// Called from VehicleController or WheelContact systems.
        /// </summary>
        public void OnWheelContact(Vector3 contactPoint, Vector3 contactNormal, TerrainMaterialManager.TerrainType terrainType,
                                   float slipRatio, float slipAngle, float wheelLoad)
        {
            if (skidMarkSystem == null || surfaceDeformation == null || dirtAccumulation == null)
                return;

            // Get tire temperature from telemetry
            float tireTemperature = GetTireTemperature();

            // Create skid mark if sufficient slip
            if (slipRatio > 0.1f) // minSlipForMark threshold
            {
                skidMarkSystem.CreateSkidMark(contactPoint, contactNormal, tireTemperature, slipRatio, slipAngle);
            }

            // Create surface deformation (works on deformable surfaces)
            surfaceDeformation.CreateSurfaceTrack(contactPoint, contactNormal, terrainType, slipRatio, wheelLoad);

            // Add dirt accumulation
            float speed = vehicleController != null ? vehicleController.GetSpeed() : 0f;
            dirtAccumulation.AddDirtFromTerrain(contactPoint, terrainType, speed, wheelLoad);
        }

        /// <summary>
        /// Get current tire temperature from vehicle telemetry.
        /// </summary>
        private float GetTireTemperature()
        {
            // Note: Tire temperature is passed directly from VehicleController.UpdateWheelVisualEffects()
            // This method is kept for reference, but temperature is obtained from wheel contact
            return 80f; // Default to optimal temperature
        }

        /// <summary>
        /// Clear all accumulated marks and deformation.
        /// </summary>
        public void ClearAllEffects()
        {
            if (skidMarkSystem != null)
                skidMarkSystem.ClearAllMarks();

            if (surfaceDeformation != null)
                surfaceDeformation.ClearAllTracks();

            if (dirtAccumulation != null)
                dirtAccumulation.CleanVehicle(1f);
        }

        /// <summary>
        /// Clean vehicle from rain.
        /// </summary>
        public void OnRain(float rainIntensity)
        {
            if (dirtAccumulation != null)
                dirtAccumulation.RainClean(rainIntensity);

            // Rain can also fade surface tracks
            if (surfaceDeformation != null)
                surfaceDeformation.RecoverSurface(transform.position, 100f);
        }

        /// <summary>
        /// Get diagnostic information about active marks and tracks.
        /// </summary>
        public string GetDiagnosticInfo()
        {
            string info = "\n=== SURFACE EFFECTS ===\n";

            if (skidMarkSystem != null)
                info += $"Skid Marks: {skidMarkSystem.GetMarkCount()}\n";

            if (surfaceDeformation != null)
                info += $"Surface Tracks: {surfaceDeformation.GetTrackCount()}\n";

            if (dirtAccumulation != null)
                info += $"Dirt Level: {dirtAccumulation.GetDirtLevel() * 100f:F1}%\n";

            return info;
        }

        // Getters for subsystems
        public SkidMarkSystem GetSkidMarkSystem() => skidMarkSystem;
        public SurfaceDeformation GetSurfaceDeformation() => surfaceDeformation;
        public DirtAccumulation GetDirtAccumulation() => dirtAccumulation;
    }
}
