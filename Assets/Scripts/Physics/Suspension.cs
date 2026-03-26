using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Simulates suspension behavior for a single wheel with realistic spring/damper physics.
    /// Implements Hooke's law and damping forces for accurate suspension response.
    /// </summary>
    public class Suspension
    {
        private int wheelIndex;
        private float springStiffness;
        private float compressionDamping;
        private float extensionDamping;
        private float rideHeight;
        private float antiRollBarStiffness;

        // Physical state
        private float currentCompressionDistance; // meters
        private float compressionVelocity; // meters per second
        private float previousCompressionDistance;

        // Limits
        private float maxCompressionDistance = 0.2f; // 20cm max travel
        private float minCompressionDistance = 0f;

        public Suspension(PhysicsData physicsData, int index)
        {
            wheelIndex = index;
            UpdateParameters(physicsData);
        }

        public void UpdateParameters(PhysicsData physicsData)
        {
            springStiffness = physicsData.SpringStiffness;
            compressionDamping = physicsData.CompressionDamping;
            extensionDamping = physicsData.ExtensionDamping;
            rideHeight = physicsData.RideHeight;
            antiRollBarStiffness = physicsData.AntiRollBarStiffness;
        }

        public void UpdateStiffness(float newStiffness)
        {
            springStiffness = Mathf.Clamp(newStiffness, 5000f, 50000f);
        }

        /// <summary>
        /// Calculate spring force using Hooke's Law: F = -kx
        /// </summary>
        private float CalculateSpringForce(float compression)
        {
            return -springStiffness * compression;
        }

        /// <summary>
        /// Calculate damping force: F_damping = -c * v
        /// Different coefficients for compression and extension (asymmetric damping)
        /// </summary>
        private float CalculateDampingForce(float velocity)
        {
            float dampingCoefficient = velocity > 0f ? compressionDamping : extensionDamping;
            return -dampingCoefficient * 10000f * velocity;
        }

        /// <summary>
        /// Update suspension state based on wheel collision.
        /// </summary>
        public void Update(WheelCollider wheelCollider, PhysicsData physicsData, float normalForce)
        {
            if (wheelCollider == null)
                return;

            // Get ground contact info
            WheelHit wheelHit;
            bool isGrounded = wheelCollider.GetGroundHit(out wheelHit);

            previousCompressionDistance = currentCompressionDistance;

            if (isGrounded)
            {
                // Wheel is touching ground: compress suspension based on distance
                float suspensionDistance = wheelCollider.suspensionDistance;
                float compressionAmount = suspensionDistance - wheelHit.distance;

                // Clamp compression to valid range
                currentCompressionDistance = Mathf.Clamp(compressionAmount, minCompressionDistance, maxCompressionDistance);
            }
            else
            {
                // No ground contact: suspension extends
                currentCompressionDistance = Mathf.Lerp(currentCompressionDistance, 0f, Time.deltaTime * 3f);
            }

            // Calculate velocity (compression/extension speed)
            compressionVelocity = (currentCompressionDistance - previousCompressionDistance) / Time.deltaTime;

            // Calculate forces using spring-damper model
            float springForce = CalculateSpringForce(currentCompressionDistance);
            float dampingForce = CalculateDampingForce(compressionVelocity);
            float totalSuspensionForce = springForce + dampingForce;

            // Apply suspension settings to wheel collider for Unity physics
            ApplySuspensionSettings(wheelCollider);
        }

        /// <summary>
        /// Apply suspension configuration to wheel collider.
        /// </summary>
        private void ApplySuspensionSettings(WheelCollider wheelCollider)
        {
            // Configure spring settings
            JettisonSpringSettings spring = wheelCollider.suspensionSpring;
            spring.spring = springStiffness / 10000f; // Normalize for Unity scale
            spring.damper = (compressionDamping + extensionDamping) / 2000f;
            spring.targetPosition = 0.5f; // Target middle of suspension travel
            wheelCollider.suspensionSpring = spring;

            // Set suspension distance (affects ride height)
            wheelCollider.suspensionDistance = rideHeight;
        }

        /// <summary>
        /// Get the total suspension force (spring + damping).
        /// </summary>
        public float GetTotalSuspensionForce()
        {
            float springForce = CalculateSpringForce(currentCompressionDistance);
            float dampingForce = CalculateDampingForce(compressionVelocity);
            return springForce + dampingForce;
        }

        /// <summary>
        /// Get anti-roll bar contribution (weight transfer between left/right wheels).
        /// </summary>
        public float GetAntiRollBarForce(float oppositeSideCompression)
        {
            float compressionDifference = currentCompressionDistance - oppositeSideCompression;
            return antiRollBarStiffness * compressionDifference;
        }

        // Getters for telemetry
        public float GetCurrentCompression() => currentCompressionDistance;
        public float GetCompressionVelocity() => compressionVelocity;
        public float GetSpringStiffness() => springStiffness;
        public float GetRideHeight() => rideHeight;
        public float GetAntiRollBarStiffness() => antiRollBarStiffness;
    }

    /// <summary>
    /// Spring settings for suspension configuration.
    /// </summary>
    public struct JettisonSpringSettings
    {
        public float spring;
        public float damper;
        public float targetPosition;
    }
}
