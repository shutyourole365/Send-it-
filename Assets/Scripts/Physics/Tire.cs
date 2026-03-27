using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Advanced tire simulation using Pacejka tire model with Phase 2 enhancements.
    /// Integrates slip dynamics, temperature zones, wear patterns, pressure effects, and surface conditions.
    /// </summary>
    public class Tire
    {
        private int wheelIndex;
        private float gripCoefficient;
        private float peakSlipAngle;
        private float temperatureSensitivity;
        private float wearRate;

        // Phase 2 systems
        private TireSlipDynamics slipDynamics;
        private TireTemperatureSystem temperatureSystem;
        private TireWearPatterns wearPatterns;
        private TirePressureSystem pressureSystem;
        private SurfaceConditionsSystem surfaceConditions;

        // Physical state
        private float currentTemperature = 20f; // Celsius (kept for compatibility)
        private float wearAmount = 0f; // 0-1 (kept for compatibility)
        private float slipAngle = 0f; // Radians
        private float slipRatio = 0f; // 0-1

        // Temperature characteristics
        private const float OptimalTireTemperature = 85f;
        private const float MaxTireTemperature = 130f;
        private const float MinTireTemperature = 20f;
        private const float ColdTireTemperature = 40f;

        // Pacejka coefficients (simplified Magic Formula)
        private float pacejkaB = 10f; // Stiffness factor
        private float pacejkaC = 1.3f; // Shape factor
        private float pacejkaE = 0.97f; // Curvature factor

        public Tire(PhysicsData physicsData, int index)
        {
            wheelIndex = index;
            UpdateParameters(physicsData);

            // Initialize Phase 2 systems
            slipDynamics = new TireSlipDynamics();
            temperatureSystem = new TireTemperatureSystem();
            wearPatterns = new TireWearPatterns(TireWearPatterns.TireCompound.Street);
            pressureSystem = new TirePressureSystem(32f);
            surfaceConditions = new SurfaceConditionsSystem();
        }

        public void UpdateParameters(PhysicsData physicsData)
        {
            gripCoefficient = physicsData.TireGripCoefficient;
            peakSlipAngle = physicsData.TirePeakSlip;
            temperatureSensitivity = physicsData.TireTemperatureSensitivity;
            wearRate = physicsData.TireWearRate;
        }

        /// <summary>
        /// Simplified Pacejka Magic Formula for lateral grip curve.
        /// Models the realistic non-linear grip response of tires.
        /// </summary>
        private float PacejkaLateralForce(float slipAngleDegrees, float normalLoad)
        {
            // Clamp slip angle to reasonable range
            slipAngleDegrees = Mathf.Clamp(slipAngleDegrees, -20f, 20f);

            // Pacejka formula: Y = D * sin(C * atan(B*x - E*(B*x - atan(B*x))))
            float x = slipAngleDegrees;
            float bx = pacejkaB * x;
            float y = pacejkaD * Mathf.Sin(pacejkaC * Mathf.Atan(bx - pacejkaE * (bx - Mathf.Atan(bx))));

            return y * normalLoad;
        }

        private float pacejkaD => gripCoefficient * 1000f; // D = peak friction coefficient

        /// <summary>
        /// Calculate grip force based on slip angle, load, and tire conditions (Phase 2 enhanced).
        /// Integrates all Phase 2 systems: slip dynamics, temperature, wear, pressure, and surface.
        /// </summary>
        public float CalculateGripForce(float slipAngleDegrees, float normalLoad, float velocity)
        {
            // Convert to radians for new systems
            float slipAngleRad = slipAngleDegrees * Mathf.Deg2Rad;

            // Update tire conditions
            UpdateTemperature(velocity);
            slipAngle = slipAngleRad;

            // Update Phase 2 systems
            float averageTemp = temperatureSystem.GetAverageTemperature();
            pressureSystem.Update(averageTemp);

            // Update slip dynamics with load and vehicle state
            slipDynamics.Update(slipAngleRad, slipRatio, normalLoad, 0f, 0f);

            // Adjust normal load for load transfer
            float adjustedLoad = slipDynamics.GetAdjustedNormalLoad(normalLoad, wheelIndex);

            // Apply Pacejka curve with adjusted load
            float baseGripForce = PacejkaLateralForce(slipAngleDegrees, adjustedLoad);

            // Apply temperature factor from new system
            float tempFactor = temperatureSystem.GetTemperatureGripFactor();

            // Apply wear factor from new system
            float wearFactor = wearPatterns.GetWearGripFactor();

            // Apply pressure factor
            float pressureFactor = pressureSystem.GetPressureGripFactor();

            // Apply surface conditions
            float surfaceGrip = surfaceConditions.GetGripCoefficient();

            // Apply slip dynamics grip factor (accounts for slip envelope)
            float slipGripFactor = slipDynamics.GetGripFactor();

            // Combined grip: multiply all factors
            return baseGripForce * tempFactor * wearFactor * pressureFactor * surfaceGrip * slipGripFactor;
        }

        /// <summary>
        /// Longitudinal force (acceleration/braking) model with Phase 2 enhancements.
        /// </summary>
        public float CalculateLongitudinalForce(float inputSlipRatio, float normalLoad, float velocity)
        {
            UpdateTemperature(velocity);

            // Slip ratio: 0 = no slip, 1 = full slip (wheel lock or spin)
            this.slipRatio = Mathf.Clamp(inputSlipRatio, -1f, 1f);

            // Update Phase 2 systems
            float averageTemp = temperatureSystem.GetAverageTemperature();
            pressureSystem.Update(averageTemp);

            // Update slip dynamics
            slipDynamics.Update(slipAngle, this.slipRatio, normalLoad, 0f, 0f);

            // Adjust normal load for load transfer
            float adjustedLoad = slipDynamics.GetAdjustedNormalLoad(normalLoad, wheelIndex);

            // Peak grip at load-dependent slip ratio
            float optimalSlipRatio = slipDynamics.GetPeakSlipRatio();
            float normalizedSlip = this.slipRatio / optimalSlipRatio;

            float gripFraction;
            if (normalizedSlip < 1f)
            {
                // Linear rise to peak
                gripFraction = normalizedSlip;
            }
            else
            {
                // Drop-off after peak (wheel lock territory)
                gripFraction = 1f - (normalizedSlip - 1f) * 0.4f;
            }

            // Apply all Phase 2 factors
            float tempFactor = temperatureSystem.GetTemperatureGripFactor();
            float wearFactor = wearPatterns.GetWearGripFactor();
            float pressureFactor = pressureSystem.GetPressureGripFactor();
            float surfaceGrip = surfaceConditions.GetGripCoefficient();
            float slipGripFactor = slipDynamics.GetGripFactor();

            return gripCoefficient * adjustedLoad * gripFraction * tempFactor * wearFactor * pressureFactor * surfaceGrip * slipGripFactor;
        }

        /// <summary>
        /// Update tire temperature based on vehicle speed, friction, and slip (Phase 2 enhanced).
        /// </summary>
        private void UpdateTemperature(float velocity)
        {
            // Use new temperature system
            float normalLoad = 3000f; // Default normal load
            float lateralAccel = 0f; // Would come from vehicle dynamics in full integration

            temperatureSystem.Update(slipAngle, slipRatio, normalLoad, velocity, lateralAccel);

            // Update compatibility field
            currentTemperature = temperatureSystem.GetAverageTemperature();

            // Update wear patterns based on temperature
            float tempWearFactor = temperatureSystem.GetTemperatureWearFactor();
            wearPatterns.Update(tempWearFactor, slipAngle, slipRatio, normalLoad, velocity, lateralAccel);

            // Update compatibility field
            wearAmount = wearPatterns.GetTotalWear();
        }

        /// <summary>
        /// Calculate temperature factor for grip.
        /// Tires have optimal grip at 80-90°C, worse when cold or overheated.
        /// </summary>
        private float GetTemperatureFactor()
        {
            if (currentTemperature < ColdTireTemperature)
            {
                // Cold tires: grip improves as temperature rises
                float coldFactor = currentTemperature / ColdTireTemperature;
                return Mathf.Lerp(0.6f, 1.0f, coldFactor);
            }
            else if (currentTemperature < OptimalTireTemperature)
            {
                // Warming up: approaching peak grip
                float warmupFactor = (currentTemperature - ColdTireTemperature) / (OptimalTireTemperature - ColdTireTemperature);
                return Mathf.Lerp(1.0f, 1.15f, warmupFactor);
            }
            else
            {
                // Overheating: grip degrades
                float overheatFactor = (currentTemperature - OptimalTireTemperature) / (MaxTireTemperature - OptimalTireTemperature);
                return Mathf.Lerp(1.15f, 0.8f, Mathf.Clamp01(overheatFactor));
            }
        }

        /// <summary>
        /// Get tire grip rating (0-1) for display purposes.
        /// </summary>
        public float GetGripRating()
        {
            return Mathf.Clamp01(gripCoefficient * GetTemperatureFactor() * (1f - wearAmount * 0.35f));
        }

        // State getters for telemetry
        public float GetCurrentTemperature() => currentTemperature;
        public float GetWearAmount() => wearAmount;
        public float GetSlipAngle() => slipAngle;
        public float GetSlipRatio() => slipRatio;

        public void ResetWear()
        {
            wearAmount = 0f;
            wearPatterns?.ResetWear();
        }

        public void SetWear(float amount)
        {
            wearAmount = Mathf.Clamp01(amount);
            wearPatterns?.SetWear(amount);
        }

        /// <summary>
        /// Set surface conditions (for multi-surface track simulation).
        /// </summary>
        public void SetSurfaceType(SurfaceConditionsSystem.SurfaceType surfaceType)
        {
            surfaceConditions?.SetSurfaceType(surfaceType);
        }

        public void SetSurfaceWetness(float wetness)
        {
            surfaceConditions?.SetWetness(wetness);
        }

        /// <summary>
        /// Adjust tire pressure (pit stop adjustments).
        /// </summary>
        public void SetTirePressure(float pressurePSI)
        {
            pressureSystem?.SetPressure(pressurePSI);
        }

        // Phase 2 system getters for telemetry and diagnostics
        public TireSlipDynamics GetSlipDynamics() => slipDynamics;
        public TireTemperatureSystem GetTemperatureSystem() => temperatureSystem;
        public TireWearPatterns GetWearPatterns() => wearPatterns;
        public TirePressureSystem GetPressureSystem() => pressureSystem;
        public SurfaceConditionsSystem GetSurfaceConditions() => surfaceConditions;
    }
}
