using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Advanced tire wear patterns system for Phase 2.
    /// Tracks location-specific wear and tread depth degradation.
    /// </summary>
    public class TireWearPatterns
    {
        // Wear locations (center, edges, inner/outer walls)
        private float centerWear = 0f;
        private float edgeLeftWear = 0f;
        private float edgeRightWear = 0f;
        private float innerWallWear = 0f;
        private float outerWallWear = 0f;

        // Overall wear amount
        private float totalWearAmount = 0f;

        // Tread depth simulation
        private float treadDepth = 8f; // mm (typical new tire is 8mm)
        private const float MinimumTreadDepth = 1.6f; // mm (legal minimum in most countries)
        private float slipperThreshold = 4f; // mm (tire becomes slippery)

        // Wear characteristics
        private float baseWearRate = 0.001f;
        private float temperatureSensitivity = 0.1f;
        private float slipSensitivity = 0.05f;
        private float loadSensitivity = 0.002f;

        // Tire compound (affects wear rate)
        private enum TireCompound
        {
            Street,      // Low wear, medium grip
            Sport,       // Medium wear, high grip
            Slick,       // High wear, very high grip
            AllWeather   // Low wear, medium grip
        }
        private TireCompound tireCompound = TireCompound.Street;

        public struct WearState
        {
            public float TotalWear;
            public float CenterWear;
            public float EdgeLeftWear;
            public float EdgeRightWear;
            public float InnerWallWear;
            public float OuterWallWear;
            public float TreadDepth;
            public float WearPattern; // 0-1: how uneven the wear is
        }

        public TireWearPatterns(TireCompound compound = TireCompound.Street)
        {
            tireCompound = compound;
            UpdateWearRateForCompound();
        }

        /// <summary>
        /// Update wear rate based on tire compound selected.
        /// </summary>
        private void UpdateWearRateForCompound()
        {
            switch (tireCompound)
            {
                case TireCompound.Street:
                    baseWearRate = 0.0008f;
                    temperatureSensitivity = 0.08f;
                    slipSensitivity = 0.04f;
                    break;
                case TireCompound.Sport:
                    baseWearRate = 0.0015f;
                    temperatureSensitivity = 0.12f;
                    slipSensitivity = 0.08f;
                    break;
                case TireCompound.Slick:
                    baseWearRate = 0.002f;
                    temperatureSensitivity = 0.15f;
                    slipSensitivity = 0.10f;
                    break;
                case TireCompound.AllWeather:
                    baseWearRate = 0.0006f;
                    temperatureSensitivity = 0.06f;
                    slipSensitivity = 0.03f;
                    break;
            }
        }

        /// <summary>
        /// Update tire wear based on driving conditions.
        /// </summary>
        public void Update(float temperatureFactor, float slipAngle, float slipRatio, float normalLoad, float velocity, float lateralAccel)
        {
            // Only accumulate wear during driving
            if (velocity < 0.5f)
                return;

            // Calculate wear for each zone
            float centerWearRate = CalculateCenterWearRate(temperatureFactor, slipRatio, normalLoad, velocity);
            float edgeWearRate = CalculateEdgeWearRate(temperatureFactor, slipAngle, normalLoad, velocity, lateralAccel);
            float wallWearRate = CalculateWallWearRate(slipAngle, slipRatio);

            // Apply wear
            centerWear += centerWearRate * Time.deltaTime;
            edgeLeftWear += edgeWearRate * (lateralAccel > 0f ? 1.5f : 0.5f) * Time.deltaTime;
            edgeRightWear += edgeWearRate * (lateralAccel > 0f ? 0.5f : 1.5f) * Time.deltaTime;
            innerWallWear += wallWearRate * (slipRatio < -0.05f ? 1.2f : 0.5f) * Time.deltaTime;
            outerWallWear += wallWearRate * (slipRatio > 0.05f ? 1.0f : 0.6f) * Time.deltaTime;

            // Clamp wear
            ClampWear();

            // Update total wear and tread depth
            UpdateOverallWear();
        }

        /// <summary>
        /// Calculate wear rate for center of tire.
        /// </summary>
        private float CalculateCenterWearRate(float temperatureFactor, float slipRatio, float normalLoad, float velocity)
        {
            float wearRate = baseWearRate;

            // Temperature effect: accelerates wear at high temps
            wearRate *= 1f + temperatureFactor * temperatureSensitivity;

            // Load effect: higher load increases wear
            float loadFactor = 1f + (normalLoad / 3000f) * loadSensitivity;
            wearRate *= loadFactor;

            // Slip ratio effect on center (longitudinal slip)
            float slipEffect = Mathf.Abs(slipRatio) * slipSensitivity;
            wearRate *= 1f + slipEffect;

            return wearRate;
        }

        /// <summary>
        /// Calculate wear rate for edges of tire.
        /// </summary>
        private float CalculateEdgeWearRate(float temperatureFactor, float slipAngle, float normalLoad, float velocity, float lateralAccel)
        {
            float wearRate = baseWearRate * 0.5f; // Edges wear slower than center in normal conditions

            // Temperature effect
            wearRate *= 1f + temperatureFactor * temperatureSensitivity;

            // Load effect
            float loadFactor = 1f + (normalLoad / 3000f) * loadSensitivity * 1.5f;
            wearRate *= loadFactor;

            // Lateral slip is critical for edge wear
            float lateralSlipEffect = Mathf.Abs(slipAngle) * slipSensitivity * 3f; // 3x more sensitive than center
            wearRate *= 1f + lateralSlipEffect;

            // Aggressive cornering increases edge wear
            float corneringIntensity = Mathf.Abs(lateralAccel) / 10f;
            wearRate *= 1f + corneringIntensity;

            return wearRate;
        }

        /// <summary>
        /// Calculate wear rate for inner/outer walls.
        /// </summary>
        private float CalculateWallWearRate(float slipAngle, float slipRatio)
        {
            float wearRate = baseWearRate * 0.2f;

            // Walls primarily wear from extreme slip conditions
            float extremeSlip = Mathf.Max(Mathf.Abs(slipAngle), Mathf.Abs(slipRatio));
            if (extremeSlip > 0.3f)
            {
                wearRate *= 1f + extremeSlip * 2f;
            }

            return wearRate;
        }

        /// <summary>
        /// Clamp wear values to 0-1 range.
        /// </summary>
        private void ClampWear()
        {
            centerWear = Mathf.Clamp01(centerWear);
            edgeLeftWear = Mathf.Clamp01(edgeLeftWear);
            edgeRightWear = Mathf.Clamp01(edgeRightWear);
            innerWallWear = Mathf.Clamp01(innerWallWear);
            outerWallWear = Mathf.Clamp01(outerWallWear);
        }

        /// <summary>
        /// Update overall wear amount and tread depth.
        /// </summary>
        private void UpdateOverallWear()
        {
            // Calculate average wear across all zones
            totalWearAmount = (centerWear * 0.4f +
                              edgeLeftWear * 0.15f +
                              edgeRightWear * 0.15f +
                              innerWallWear * 0.15f +
                              outerWallWear * 0.15f);

            // Update tread depth (linear degradation)
            treadDepth = 8f * (1f - totalWearAmount);
            treadDepth = Mathf.Max(treadDepth, 0f);
        }

        /// <summary>
        /// Get grip loss factor due to wear (0 = fully worn, 1 = new tire).
        /// </summary>
        public float GetWearGripFactor()
        {
            // Tire loses grip as it wears
            // Additional penalty when tread becomes too thin
            float baseFactor = 1f - (totalWearAmount * 0.35f); // Up to 35% loss from wear

            // Slippery condition when below 4mm tread
            if (treadDepth < slipperThreshold)
            {
                float slipperyFactor = treadDepth / slipperThreshold;
                baseFactor *= 0.5f + (slipperyFactor * 0.5f); // Additional 50% loss
            }

            return Mathf.Max(baseFactor, 0.2f); // Never completely lose grip
        }

        /// <summary>
        /// Get wear pattern severity (how uneven the wear is).
        /// </summary>
        public float GetWearPattern()
        {
            float maxWear = Mathf.Max(centerWear, edgeLeftWear, edgeRightWear, innerWallWear, outerWallWear);
            float minWear = Mathf.Min(centerWear, edgeLeftWear, edgeRightWear, innerWallWear, outerWallWear);

            return maxWear - minWear; // 0 = even wear, 1 = very uneven
        }

        /// <summary>
        /// Check if tire should be replaced.
        /// </summary>
        public bool ShouldReplaceTire()
        {
            return treadDepth < MinimumTreadDepth;
        }

        /// <summary>
        /// Get warning level for tire condition (0 = good, 1 = needs replacement).
        /// </summary>
        public float GetTireWarningLevel()
        {
            if (treadDepth < MinimumTreadDepth)
                return 1f;

            if (treadDepth < slipperThreshold)
                return 0.5f + ((slipperThreshold - treadDepth) / slipperThreshold) * 0.5f;

            return 0f;
        }

        public WearState GetWearState()
        {
            return new WearState
            {
                TotalWear = totalWearAmount,
                CenterWear = centerWear,
                EdgeLeftWear = edgeLeftWear,
                EdgeRightWear = edgeRightWear,
                InnerWallWear = innerWallWear,
                OuterWallWear = outerWallWear,
                TreadDepth = treadDepth,
                WearPattern = GetWearPattern()
            };
        }

        // Getters
        public float GetTotalWear() => totalWearAmount;
        public float GetTreadDepth() => treadDepth;
        public void ResetWear()
        {
            centerWear = 0f;
            edgeLeftWear = 0f;
            edgeRightWear = 0f;
            innerWallWear = 0f;
            outerWallWear = 0f;
            totalWearAmount = 0f;
            treadDepth = 8f;
        }
    }
}
