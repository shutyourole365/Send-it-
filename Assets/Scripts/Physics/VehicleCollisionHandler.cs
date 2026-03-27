using UnityEngine;
using SendIt.Gameplay;

namespace SendIt.Physics
{
    /// <summary>
    /// Handles vehicle collision events and integrates with damage system.
    /// Should be added to the same GameObject as VehicleController.
    /// </summary>
    public class VehicleCollisionHandler : MonoBehaviour
    {
        private VehicleController vehicleController;
        private VehicleDamageSystem damageSystem;
        private EnhancedGameIntegration gameIntegration;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            vehicleController = GetComponent<VehicleController>();
            damageSystem = GetComponent<VehicleDamageSystem>();

            if (vehicleController == null)
                Debug.LogWarning("VehicleController not found on this object");

            if (damageSystem == null)
                Debug.LogWarning("VehicleDamageSystem not found on this object - damage won't be applied");

            gameIntegration = FindObjectOfType<EnhancedGameIntegration>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (vehicleController == null)
                return;

            // Register impact with damage system if available
            if (damageSystem != null)
            {
                damageSystem.RegisterCollisionImpact(collision);
            }

            // Log collision for debugging
            float impactForce = collision.relativeVelocity.magnitude * vehicleController.GetMass();
            Debug.Log($"Vehicle collision: {collision.gameObject.name} - Force: {impactForce:F0}N, Speed: {collision.relativeVelocity.magnitude:F2} m/s");
        }

        private void OnCollisionStay(Collision collision)
        {
            // Handle continuous collision if needed
        }

        private void OnCollisionExit(Collision collision)
        {
            // Collision ended
        }

        /// <summary>
        /// Get current vehicle damage level (0-1).
        /// </summary>
        public float GetCurrentDamageLevel()
        {
            return damageSystem != null ? damageSystem.GetOverallDamage() : 0f;
        }

        /// <summary>
        /// Get damage system component.
        /// </summary>
        public VehicleDamageSystem GetDamageSystem() => damageSystem;
    }
}
