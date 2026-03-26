using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Calculates vehicle-level dynamics including weight transfer,
    /// mass distribution, center of gravity effects, and balance.
    /// </summary>
    public class VehicleDynamics
    {
        private Rigidbody vehicleRigidbody;
        private float totalMass;
        private float frontWeightDistribution; // 0-1, where 0.5 = 50/50

        // Calculated properties
        private float frontAxleWeight;
        private float rearAxleWeight;

        // Dynamics state
        private float longitudinalWeightTransfer; // Load transfer during accel/brake
        private float lateralWeightTransfer; // Load transfer during cornering
        private float rollAngle; // Current vehicle roll

        // Geometry
        private float wheelbaseLength = 2.7f; // Distance between front and rear axles
        private float trackWidth = 1.5f; // Distance between left and right wheels
        private float centerOfGravityHeight = 0.5f; // Height of CoG above ground

        public struct DynamicsState
        {
            public float FrontAxleLoad;
            public float RearAxleLoad;
            public float LeftFrontLoad;
            public float RightFrontLoad;
            public float LeftRearLoad;
            public float RearRearLoad;
            public float LongitudinalTransfer;
            public float LateralTransfer;
            public float RollAngle;
        }

        public VehicleDynamics(Rigidbody rb, PhysicsData physicsData)
        {
            vehicleRigidbody = rb;
            totalMass = physicsData.TotalMass;
            frontWeightDistribution = physicsData.FrontWeightDistribution;
            UpdateWeightDistribution();
        }

        /// <summary>
        /// Calculate axle weight distribution based on center of gravity position.
        /// </summary>
        private void UpdateWeightDistribution()
        {
            float gravity = 9.81f;

            // Static axle loads based on CoG position
            // Front load = (rear distance / wheelbase) × total mass × g
            // Rear load = (front distance / wheelbase) × total mass × g
            frontAxleWeight = totalMass * gravity * frontWeightDistribution;
            rearAxleWeight = totalMass * gravity * (1f - frontWeightDistribution);
        }

        /// <summary>
        /// Update dynamics based on current vehicle motion.
        /// </summary>
        public void Update(Rigidbody vehicleBody, float[] wheelLoads)
        {
            if (vehicleBody == null)
                return;

            // Calculate weight transfer during acceleration/braking
            CalculateLongitudinalWeightTransfer(vehicleBody);

            // Calculate weight transfer during cornering
            CalculateLateralWeightTransfer(vehicleBody);

            // Update roll angle
            UpdateRollAngle(vehicleBody);
        }

        /// <summary>
        /// Calculate longitudinal weight transfer (front/rear).
        /// During acceleration: weight transfers to rear wheels (nose up).
        /// During braking: weight transfers to front wheels (nose down).
        /// </summary>
        private void CalculateLongitudinalWeightTransfer(Rigidbody vehicleBody)
        {
            if (vehicleBody.velocity.magnitude < 0.1f)
            {
                longitudinalWeightTransfer = 0f;
                return;
            }

            // Acceleration in vehicle's forward direction
            Vector3 localAcceleration = vehicleBody.transform.InverseTransformDirection(vehicleBody.velocity);
            float forwardAccel = vehicleBody.acceleration.magnitude;

            // Weight transfer = (acceleration × CoG height / wheelbase) × mass × g
            float maxTransfer = totalMass * 9.81f * 0.5f; // Max 50% transfer
            longitudinalWeightTransfer = (forwardAccel * centerOfGravityHeight / wheelbaseLength) * maxTransfer;
            longitudinalWeightTransfer = Mathf.Clamp(longitudinalWeightTransfer, -maxTransfer, maxTransfer);
        }

        /// <summary>
        /// Calculate lateral weight transfer (left/right) during cornering.
        /// </summary>
        private void CalculateLateralWeightTransfer(Rigidbody vehicleBody)
        {
            if (vehicleBody.velocity.magnitude < 0.1f)
            {
                lateralWeightTransfer = 0f;
                rollAngle = 0f;
                return;
            }

            // Calculate lateral acceleration (centripetal force)
            Vector3 localVelocity = vehicleBody.transform.InverseTransformDirection(vehicleBody.velocity);
            float lateralAccel = vehicleBody.angularVelocity.magnitude * localVelocity.z;

            // Weight transfer = (lateral accel × CoG height / track width) × mass × g
            float maxTransfer = totalMass * 9.81f * 0.35f; // Max 35% transfer (less extreme than longitudinal)
            lateralWeightTransfer = (lateralAccel * centerOfGravityHeight / trackWidth) * maxTransfer;
            lateralWeightTransfer = Mathf.Clamp(lateralWeightTransfer, -maxTransfer, maxTransfer);

            // Calculate roll angle from lateral acceleration
            float rollRate = (lateralAccel * centerOfGravityHeight) / 9.81f; // Radians
            rollAngle = Mathf.Clamp(rollRate, -0.3f, 0.3f); // ±17 degrees
        }

        /// <summary>
        /// Update vehicle roll angle based on centripetal forces.
        /// </summary>
        private void UpdateRollAngle(Rigidbody vehicleBody)
        {
            // Smooth damping of roll angle
            rollAngle = Mathf.Lerp(rollAngle, 0f, Time.deltaTime * 2f);
        }

        /// <summary>
        /// Calculate individual wheel loads based on weight distribution and transfers.
        /// </summary>
        public float[] CalculateWheelLoads()
        {
            float[] wheelLoads = new float[4];

            // Base axle loads
            float baseFrontLoad = frontAxleWeight / 2f; // Split between left and right
            float baseRearLoad = rearAxleWeight / 2f;

            // Apply longitudinal transfer
            float frontTransfer = longitudinalWeightTransfer / 2f;
            float rearTransfer = -longitudinalWeightTransfer / 2f;

            // Apply lateral transfer (left/right)
            float lateralTransfer = lateralWeightTransfer / 2f;

            // Wheel loads: 0=FL, 1=FR, 2=RL, 3=RR
            wheelLoads[0] = baseFrontLoad + frontTransfer - lateralTransfer; // FL
            wheelLoads[1] = baseFrontLoad + frontTransfer + lateralTransfer; // FR
            wheelLoads[2] = baseRearLoad + rearTransfer - lateralTransfer;   // RL
            wheelLoads[3] = baseRearLoad + rearTransfer + lateralTransfer;   // RR

            // Clamp to prevent negative loads
            for (int i = 0; i < 4; i++)
            {
                wheelLoads[i] = Mathf.Max(wheelLoads[i], 0f);
            }

            return wheelLoads;
        }

        /// <summary>
        /// Get current dynamics state for telemetry.
        /// </summary>
        public DynamicsState GetDynamicsState()
        {
            float[] loads = CalculateWheelLoads();

            return new DynamicsState
            {
                FrontAxleLoad = loads[0] + loads[1],
                RearAxleLoad = loads[2] + loads[3],
                LeftFrontLoad = loads[0],
                RightFrontLoad = loads[1],
                LeftRearLoad = loads[2],
                RearRearLoad = loads[3],
                LongitudinalTransfer = longitudinalWeightTransfer,
                LateralTransfer = lateralWeightTransfer,
                RollAngle = rollAngle
            };
        }

        public void UpdateMass(float newMass) => totalMass = newMass;
        public void UpdateWeightDistribution(float frontDist) => frontWeightDistribution = frontDist;
        public float GetFrontAxleWeight() => frontAxleWeight;
        public float GetRearAxleWeight() => rearAxleWeight;
    }
}
