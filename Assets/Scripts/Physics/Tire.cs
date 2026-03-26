using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Simulates tire behavior including grip, slip angles, and temperature effects.
    /// </summary>
    public class Tire
    {
        private int wheelIndex;
        private float gripCoefficient;
        private float peakSlipAngle;
        private float temperatureSensitivity;
        private float wearRate;

        // Tire state
        private float currentTemperature = 20f; // Celsius
        private float wearAmount = 0f; // 0-1
        private float slipAngle = 0f;
        private float slipRatio = 0f;

        private const float MaxTireTemperature = 130f;
        private const float MinTireTemperature = 20f;

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
        /// Calculate grip coefficient based on slip angle and temperature.
        /// </summary>
        public float CalculateGrip(float currentSlipAngle, float velocity)
        {
            // Normalize slip angle to degrees
            float slipDegrees = currentSlipAngle * Mathf.Rad2Deg;

            // Tire grip curve: peak grip at optimal slip angle
            float normalizedSlip = Mathf.Abs(slipDegrees) / peakSlipAngle;
            float gripFraction;

            if (normalizedSlip < 1f)
            {
                // Linear rise to peak grip
                gripFraction = normalizedSlip;
            }
            else
            {
                // Drop-off after peak (oversteer territory)
                gripFraction = 1f - (normalizedSlip - 1f) * 0.5f;
            }

            // Apply temperature effects (tires warm up at speed)
            UpdateTemperature(velocity);
            float tempFactor = GetTemperatureFactor();

            // Apply wear effects
            float wearFactor = 1f - (wearAmount * 0.3f); // Max 30% grip loss from wear

            return gripCoefficient * gripFraction * tempFactor * wearFactor;
        }

        /// <summary>
        /// Update tire temperature based on velocity and friction.
        /// </summary>
        private void UpdateTemperature(float velocity)
        {
            float heatingRate = (velocity * gripCoefficient * 0.1f) * Time.deltaTime;
            float targetTemp = MinTireTemperature + heatingRate * 10f;

            // Tire temperature asymptotically approaches a target
            currentTemperature = Mathf.Lerp(currentTemperature, targetTemp, Time.deltaTime * 0.2f);
            currentTemperature = Mathf.Clamp(currentTemperature, MinTireTemperature, MaxTireTemperature);

            // Gradual wear increase at high temperatures
            if (currentTemperature > 100f)
            {
                wearAmount += wearRate * Time.deltaTime;
                wearAmount = Mathf.Clamp01(wearAmount);
            }
        }

        /// <summary>
        /// Calculate temperature factor for grip (tires grip better when warm, worse when cold or overheated).
        /// </summary>
        private float GetTemperatureFactor()
        {
            // Optimal tire temperature: 80-90°C
            float optimalTemp = 85f;
            float tempDeviation = Mathf.Abs(currentTemperature - optimalTemp);

            // Grip is best at optimal temperature, decreases as temp deviates
            return 1f - (tempDeviation / 100f) * temperatureSensitivity;
        }

        /// <summary>
        /// Calculate the lateral (slip angle) force.
        /// </summary>
        public float CalculateLateralForce(float normalLoad, float slipAngle, float velocity)
        {
            float grip = CalculateGrip(slipAngle, velocity);
            return grip * normalLoad;
        }

        public float GetCurrentTemperature() => currentTemperature;
        public float GetWearAmount() => wearAmount;
        public void ResetWear() => wearAmount = 0f;
        public void SetWear(float amount) => wearAmount = Mathf.Clamp01(amount);
    }
}
