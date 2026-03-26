using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Simulates aerodynamic effects including drag and downforce.
    /// </summary>
    public class Aerodynamics
    {
        private float dragCoefficient;
        private float downforceCoefficient;
        private float spoilerAngle;

        // Air density at sea level (kg/m³)
        private const float AirDensity = 1.225f;
        // Reference frontal area (m²) - typical sports car
        private const float FrontalArea = 2.2f;

        public Aerodynamics(PhysicsData physicsData)
        {
            UpdateParameters(physicsData);
        }

        public void UpdateParameters(PhysicsData physicsData)
        {
            dragCoefficient = physicsData.DragCoefficient;
            downforceCoefficient = physicsData.DownforceCoefficient;
            spoilerAngle = physicsData.SpoilerAngle;
        }

        /// <summary>
        /// Calculate aerodynamic drag force opposing motion.
        /// F_drag = 0.5 × ρ × v² × Cd × A
        /// </summary>
        public Vector3 CalculateDragForce(Vector3 velocity)
        {
            if (velocity.magnitude < 0.1f)
                return Vector3.zero;

            float speed = velocity.magnitude;
            float dragMagnitude = 0.5f * AirDensity * speed * speed * dragCoefficient * FrontalArea;

            // Drag opposes velocity direction
            return -velocity.normalized * dragMagnitude;
        }

        /// <summary>
        /// Calculate downforce (negative lift) that increases tire grip at speed.
        /// F_downforce = 0.5 × ρ × v² × Cl × A
        /// </summary>
        public Vector3 CalculateDownforce(Vector3 velocity)
        {
            if (velocity.magnitude < 0.1f)
                return Vector3.zero;

            float speed = velocity.magnitude;

            // Effective downforce coefficient (increases with spoiler angle)
            float effectiveClift = downforceCoefficient + (spoilerAngle * 0.02f);

            float downforceMagnitude = 0.5f * AirDensity * speed * speed * effectiveClift * FrontalArea;

            // Downforce is always downward (negative Y in local space)
            return Vector3.down * downforceMagnitude;
        }

        /// <summary>
        /// Calculate combined aerodynamic forces (drag + downforce).
        /// </summary>
        public Vector3 CalculateTotalAerodynamicForce(Vector3 velocity)
        {
            return CalculateDragForce(velocity) + CalculateDownforce(velocity);
        }

        public float GetDragCoefficient() => dragCoefficient;
        public float GetDownforceCoefficient() => downforceCoefficient;
        public float GetSpoilerAngle() => spoilerAngle;
    }
}
