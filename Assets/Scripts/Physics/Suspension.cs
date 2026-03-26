using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Simulates suspension behavior for a single wheel.
    /// Includes spring stiffness, damping, and ride height.
    /// </summary>
    public class Suspension
    {
        private int wheelIndex;
        private float springStiffness;
        private float compressionDamping;
        private float extensionDamping;
        private float rideHeight;
        private float antiRollBarStiffness;

        private float currentCompression;
        private float previousCompression;

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
            springStiffness = newStiffness;
        }

        /// <summary>
        /// Update suspension force on the wheel collider.
        /// </summary>
        public void Update(WheelCollider wheelCollider, PhysicsData physicsData)
        {
            if (wheelCollider == null)
                return;

            // Get suspension data from wheel collider
            WheelHit hit;
            bool isGrounded = wheelCollider.GetGroundHit(out hit);

            if (isGrounded)
            {
                // Calculate compression distance (0 = fully extended, max = fully compressed)
                float suspensionTravel = wheelCollider.suspensionDistance;
                currentCompression = 1f - (hit.distance / suspensionTravel);
            }
            else
            {
                // No contact, suspension returns to rest
                currentCompression = Mathf.Lerp(currentCompression, 0f, Time.deltaTime * 2f);
            }

            // Apply spring force (Hooke's Law: F = -kx)
            float springForce = springStiffness * currentCompression;

            // Apply damping (opposing velocity)
            float compressionVelocity = currentCompression - previousCompression;
            float dampingCoefficient = compressionVelocity > 0f ? compressionDamping : extensionDamping;
            float dampingForce = -compressionVelocity * dampingCoefficient * 1000f;

            // Store for next frame
            previousCompression = currentCompression;

            // Modify suspension stiffness through wheel collider
            JettisonSuspensionSettings(wheelCollider);
        }

        /// <summary>
        /// Apply suspension settings to the wheel collider.
        /// </summary>
        private void JettisonSuspensionSettings(WheelCollider wheelCollider)
        {
            WheelCollider.WheelColliderHitEvent hitEvent = new WheelCollider.WheelColliderHitEvent();

            // Update suspension spring
            JettisonSpringSettings spring = wheelCollider.suspensionSpring;
            spring.spring = springStiffness / 1000f; // Normalize for Unity physics
            spring.damper = (compressionDamping + extensionDamping) / 2f;
            wheelCollider.suspensionSpring = spring;

            // Update suspension distance (affects ride height)
            wheelCollider.suspensionDistance = rideHeight;
        }

        public float GetCurrentCompression() => currentCompression;
        public float GetSpringStiffness() => springStiffness;
        public float GetRideHeight() => rideHeight;
    }

    /// <summary>
    /// Simple wrapper for suspension spring settings in Unity.
    /// </summary>
    public struct JettisonSpringSettings
    {
        public float spring;
        public float damper;
        public float targetPosition;
    }
}
