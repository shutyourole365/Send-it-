using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Simulates engine behavior with realistic torque curves.
    /// </summary>
    public class Engine
    {
        public const float IdleRPM = 800f;
        private const float RPM_DAMPING = 0.95f;

        private float maxRPM;
        private float horsePower;
        private float torquePeakRPM;
        private float engineResponsiveness;

        public struct EngineState
        {
            public float RPM;
            public float Torque;
        }

        public Engine(PhysicsData physicsData)
        {
            UpdateParameters(physicsData);
        }

        public void UpdateParameters(PhysicsData physicsData)
        {
            maxRPM = physicsData.MaxRPM;
            horsePower = physicsData.HorsePower;
            torquePeakRPM = physicsData.TorquePeakRPM;
            engineResponsiveness = physicsData.EngineResponsiveness;
        }

        /// <summary>
        /// Calculate torque output based on RPM using a realistic torque curve.
        /// </summary>
        private float CalculateTorque(float rpm)
        {
            // Normalize RPM to 0-1 range
            float normalizedRPM = Mathf.Clamp01(rpm / maxRPM);

            // Peak torque at torquePeakRPM, then drops off
            float torqueFraction;
            if (rpm < torquePeakRPM)
            {
                // Linear rise to peak
                torqueFraction = rpm / torquePeakRPM;
            }
            else
            {
                // Power curve drop-off after peak
                float rpmAbovePeak = rpm - torquePeakRPM;
                float rpmRangeBelowMax = maxRPM - torquePeakRPM;
                torqueFraction = 1f - (rpmAbovePeak / rpmRangeBelowMax) * 0.6f;
            }

            // Convert HP to torque: Torque (Nm) = HP * 7.46 / RPM
            float baseTorque = (horsePower * 7.46f) / (rpm > 0 ? rpm : 1f);
            return baseTorque * Mathf.Clamp01(torqueFraction);
        }

        /// <summary>
        /// Update engine RPM based on throttle input.
        /// </summary>
        public EngineState Update(float currentRPM, float throttleInput, int currentGear)
        {
            float targetRPM = IdleRPM + throttleInput * (maxRPM - IdleRPM);
            float rpmChangeRate = engineResponsiveness * 2000f * Time.deltaTime; // RPM/second change

            // Smoothly transition to target RPM
            float newRPM = Mathf.Lerp(currentRPM, targetRPM, Time.deltaTime * engineResponsiveness);

            // Add some damping
            newRPM = Mathf.Lerp(currentRPM, newRPM, RPM_DAMPING);

            // Clamp to valid range
            newRPM = Mathf.Clamp(newRPM, IdleRPM, maxRPM);

            float torque = CalculateTorque(newRPM);

            return new EngineState { RPM = newRPM, Torque = torque };
        }

        public float GetMaxRPM() => maxRPM;
        public float GetHorsePower() => horsePower;
        public float GetIdleRPM() => IdleRPM;
    }
}
