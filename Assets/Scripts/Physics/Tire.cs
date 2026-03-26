using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Advanced tire simulation using a simplified Pacejka tire model.
    /// Simulates grip curves, slip angles, temperature effects, and wear.
    /// </summary>
    public class Tire
    {
        private int wheelIndex;
        private float gripCoefficient;
        private float peakSlipAngle;
        private float temperatureSensitivity;
        private float wearRate;

        // Physical state
        private float currentTemperature = 20f; // Celsius
        private float wearAmount = 0f; // 0-1
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
        /// Calculate grip force based on slip angle, load, and tire conditions.
        /// </summary>
        public float CalculateGripForce(float slipAngleDegrees, float normalLoad, float velocity)
        {
            // Update tire conditions
            UpdateTemperature(velocity);

            // Apply Pacejka curve
            float baseGripForce = PacejkaLateralForce(slipAngleDegrees, normalLoad);

            // Apply temperature factor (grip improves with warmth, degraded when cold or overheated)
            float tempFactor = GetTemperatureFactor();

            // Apply wear factor (gradual grip loss)
            float wearFactor = 1f - (wearAmount * 0.35f); // Up to 35% grip loss from full wear

            return baseGripForce * tempFactor * wearFactor;
        }

        /// <summary>
        /// Simplified longitudinal force (acceleration/braking) model.
        /// </summary>
        public float CalculateLongitudinalForce(float slipRatio, float normalLoad, float velocity)
        {
            UpdateTemperature(velocity);

            // Slip ratio: 0 = no slip, 1 = full slip (wheel lock or spin)
            slipRatio = Mathf.Clamp01(slipRatio);

            // Peak grip at ~15% slip ratio
            float optimalSlipRatio = 0.15f;
            float normalizedSlip = slipRatio / optimalSlipRatio;

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

            float tempFactor = GetTemperatureFactor();
            float wearFactor = 1f - (wearAmount * 0.35f);

            return gripCoefficient * normalLoad * gripFraction * tempFactor * wearFactor;
        }

        /// <summary>
        /// Update tire temperature based on vehicle speed and friction.
        /// </summary>
        private void UpdateTemperature(float velocity)
        {
            // Heat generation proportional to speed and grip
            float heatGeneration = Mathf.Abs(velocity) * gripCoefficient * 0.5f;
            float targetTemperature = MinTireTemperature + heatGeneration;

            // Clamp target temperature
            targetTemperature = Mathf.Clamp(targetTemperature, MinTireTemperature, MaxTireTemperature);

            // Exponential approach to target temperature
            float heatingRate = 2f * Time.deltaTime; // Tires heat up quickly
            currentTemperature = Mathf.Lerp(currentTemperature, targetTemperature, heatingRate);

            // Cooling when parked
            if (velocity < 1f)
            {
                float coolingRate = 0.5f * Time.deltaTime;
                currentTemperature = Mathf.Lerp(currentTemperature, MinTireTemperature, coolingRate);
            }

            // Tire degradation accelerates at high temperatures
            if (currentTemperature > OptimalTireTemperature)
            {
                float excessTemp = currentTemperature - OptimalTireTemperature;
                float degradationFactor = 1f + (excessTemp / (MaxTireTemperature - OptimalTireTemperature));
                wearAmount += wearRate * degradationFactor * Time.deltaTime;
                wearAmount = Mathf.Clamp01(wearAmount);
            }
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

        public void ResetWear() => wearAmount = 0f;
        public void SetWear(float amount) => wearAmount = Mathf.Clamp01(amount);
    }
}
