using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Simulates vehicle transmission (gearbox) with configurable gear ratios.
    /// </summary>
    public class Transmission
    {
        private float[] gearRatios;
        private float finalDriveRatio;
        private float shiftSpeed;
        private int gearCount;

        public Transmission(PhysicsData physicsData)
        {
            UpdateParameters(physicsData);
        }

        public void UpdateParameters(PhysicsData physicsData)
        {
            gearCount = physicsData.GearCount;
            finalDriveRatio = physicsData.FinalDriveRatio;
            shiftSpeed = physicsData.ShiftSpeed;

            // Generate gear ratios (realistic progression)
            GenerateGearRatios();
        }

        /// <summary>
        /// Generate realistic gear ratios for the transmission.
        /// Ratios decrease as gears increase (lower speed multiplication).
        /// </summary>
        private void GenerateGearRatios()
        {
            gearRatios = new float[gearCount];

            // Realistic gear ratio progression
            // 1st gear: high ratio, each subsequent gear is lower
            float firstGearRatio = 3.5f;
            float lastGearRatio = 0.7f;

            for (int i = 0; i < gearCount; i++)
            {
                float t = gearCount > 1 ? (float)i / (gearCount - 1) : 0f;
                gearRatios[i] = Mathf.Lerp(firstGearRatio, lastGearRatio, t);
            }
        }

        /// <summary>
        /// Get the gear ratio for a specific gear (1-indexed).
        /// </summary>
        public float GetGearRatio(int gear)
        {
            gear = Mathf.Clamp(gear, 1, gearCount);
            return gearRatios[gear - 1];
        }

        /// <summary>
        /// Get the overall drive ratio (gear ratio × final drive ratio).
        /// </summary>
        public float GetOverallDriveRatio(int gear)
        {
            return GetGearRatio(gear) * finalDriveRatio;
        }

        /// <summary>
        /// Calculate vehicle speed in km/h given engine RPM and wheel radius.
        /// </summary>
        public float CalculateSpeed(float rpm, float wheelRadius, int gear)
        {
            // Speed (km/h) = (RPM / overallRatio) × wheelRadius × 2π × 60 / 100000
            float overallRatio = GetOverallDriveRatio(gear);
            float wheelSpeedRPM = rpm / overallRatio;
            float speedMs = (wheelSpeedRPM * 2f * Mathf.PI * wheelRadius) / 60f;
            return speedMs * 3.6f; // Convert m/s to km/h
        }

        public int GetGearCount() => gearCount;
        public float GetShiftSpeed() => shiftSpeed;
        public float GetFinalDriveRatio() => finalDriveRatio;
    }
}
