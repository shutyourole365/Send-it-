using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Advanced tire temperature system for Phase 2.
    /// Tracks localized temperature zones and realistic heating/cooling dynamics.
    /// </summary>
    public class TireTemperatureSystem
    {
        // Temperature zones (center, edge left, edge right, inner wall, outer wall)
        private float centerTemperature = 20f;
        private float edgeLeftTemperature = 20f;
        private float edgeRightTemperature = 20f;
        private float innerWallTemperature = 20f;
        private float outerWallTemperature = 20f;

        // Average tire temperature
        private float averageTemperature = 20f;

        // Temperature thresholds
        private const float AmbientTemperature = 20f;
        private const float OptimalTemperature = 85f;
        private const float MaxTemperature = 130f;
        private const float ColdThreshold = 40f;

        // Thermal properties
        private float thermalMass = 15f; // kg (affects heating/cooling rate)
        private float convectionCoefficient = 0.5f; // Heat transfer to air
        private float brakeDissipation = 0.3f; // Brake heat transfer

        // Performance curves
        private AnimationCurve gripVsTemperature;
        private AnimationCurve wearRateVsTemperature;

        public struct TemperatureState
        {
            public float AverageTemp;
            public float CenterTemp;
            public float EdgeLeftTemp;
            public float EdgeRightTemp;
            public float InnerWallTemp;
            public float OuterWallTemp;
            public float TemperatureVariance; // How uneven the tire temp is
        }

        public TireTemperatureSystem()
        {
            InitializePerformanceCurves();
        }

        /// <summary>
        /// Initialize grip and wear rate curves based on temperature.
        /// </summary>
        private void InitializePerformanceCurves()
        {
            // Grip curve: low in cold, peaks at optimal, drops at high temp
            gripVsTemperature = new AnimationCurve(
                new Keyframe(0f, 0.5f, 0f, 0.02f),      // 0°C: 50% grip
                new Keyframe(40f, 0.9f, 0f, 0.01f),     // 40°C: 90% grip (warming up)
                new Keyframe(85f, 1.15f, 0f, 0f),       // 85°C: 115% grip (peak)
                new Keyframe(100f, 1.0f, 0f, -0.02f),   // 100°C: dropping off
                new Keyframe(130f, 0.7f, 0f, 0f)        // 130°C: 70% grip (overheated)
            );

            // Wear rate curve: low in cold, increases with temperature
            wearRateVsTemperature = new AnimationCurve(
                new Keyframe(0f, 0.1f, 0f, 0f),         // 0°C: minimal wear
                new Keyframe(85f, 1.0f, 0f, 0.05f),     // 85°C: baseline wear
                new Keyframe(130f, 5.0f, 0f, 0f)        // 130°C: heavy wear
            );
        }

        /// <summary>
        /// Update tire temperatures based on friction, slip, and ambient conditions.
        /// </summary>
        public void Update(float slipAngle, float slipRatio, float normalLoad, float velocity, float lateralAccel)
        {
            // Calculate heat generation
            float frictionHeat = CalculateFrictionHeat(slipAngle, slipRatio, normalLoad, velocity);
            float slipHeat = CalculateSlipHeat(slipAngle, slipRatio, velocity);

            // Distribute heat to different zones based on slip characteristics
            DistributeHeat(frictionHeat, slipHeat, slipAngle, slipRatio, lateralAccel);

            // Apply cooling
            ApplyCooling(velocity);

            // Update average temperature
            UpdateAverageTemperature();
        }

        /// <summary>
        /// Calculate heat generated from friction between tire and road.
        /// </summary>
        private float CalculateFrictionHeat(float slipAngle, float slipRatio, float normalLoad, float velocity)
        {
            // Heat = friction force * relative velocity
            float relativeVelocity = Mathf.Abs(velocity) * (Mathf.Abs(slipAngle) + Mathf.Abs(slipRatio));
            float frictionMagnitude = normalLoad * 0.8f; // Approximate friction coefficient

            float heatGeneration = frictionMagnitude * relativeVelocity * 0.001f;
            return Mathf.Clamp(heatGeneration, 0f, 100f);
        }

        /// <summary>
        /// Calculate additional heat from tire slip (hysteresis).
        /// </summary>
        private float CalculateSlipHeat(float slipAngle, float slipRatio, float velocity)
        {
            // Slip generates heat proportional to slip magnitude squared
            float slipMagnitude = Mathf.Sqrt(slipAngle * slipAngle + slipRatio * slipRatio);
            float slipHeat = slipMagnitude * slipMagnitude * velocity * 0.5f;

            return Mathf.Clamp(slipHeat, 0f, 50f);
        }

        /// <summary>
        /// Distribute heat across different tire zones based on slip pattern.
        /// </summary>
        private void DistributeHeat(float frictionHeat, float slipHeat, float slipAngle, float slipRatio, float lateralAccel)
        {
            // Center always gets baseline heat
            centerTemperature += frictionHeat * 0.4f * Time.deltaTime;

            // Lateral slip creates edge wear
            if (Mathf.Abs(slipAngle) > 0.1f)
            {
                float edgeIntensity = Mathf.Abs(slipAngle) * slipHeat;
                if (lateralAccel > 0f)
                {
                    edgeLeftTemperature += edgeIntensity * 0.6f * Time.deltaTime;
                }
                else
                {
                    edgeRightTemperature += edgeIntensity * 0.6f * Time.deltaTime;
                }
            }

            // Longitudinal slip affects different zones
            if (Mathf.Abs(slipRatio) > 0.05f)
            {
                float longitudinalHeat = Mathf.Abs(slipRatio) * slipHeat;
                if (slipRatio > 0f) // Wheel spin (acceleration)
                {
                    centerTemperature += longitudinalHeat * 0.5f * Time.deltaTime;
                    outerWallTemperature += longitudinalHeat * 0.3f * Time.deltaTime;
                }
                else // Wheel lock (braking)
                {
                    centerTemperature += longitudinalHeat * 0.7f * Time.deltaTime;
                    innerWallTemperature += longitudinalHeat * 0.4f * Time.deltaTime;
                }
            }

            // Brake heat transfer to inner wall
            // (Assumes disc brakes heating the inside of the tire)
            float brakeHeat = slipRatio < -0.1f ? 10f : 0f;
            innerWallTemperature += brakeHeat * Time.deltaTime;

            // Clamp all temperatures
            ClampTemperatures();
        }

        /// <summary>
        /// Apply cooling from airflow and convection.
        /// </summary>
        private void ApplyCooling(float velocity)
        {
            // Convection cooling proportional to velocity
            float convectionCooling = velocity * convectionCoefficient * Time.deltaTime;

            // Radiation cooling (always present)
            float radiationCooling = 0.1f * Time.deltaTime;

            // Heat transfer to inner walls from center (conduction)
            float conductionCooling = 0.2f * Time.deltaTime;

            // Apply cooling to all zones
            float totalCooling = convectionCooling + radiationCooling;

            centerTemperature = Mathf.Lerp(centerTemperature, AmbientTemperature, totalCooling);
            edgeLeftTemperature = Mathf.Lerp(edgeLeftTemperature, AmbientTemperature, totalCooling);
            edgeRightTemperature = Mathf.Lerp(edgeRightTemperature, AmbientTemperature, totalCooling);
            outerWallTemperature = Mathf.Lerp(outerWallTemperature, AmbientTemperature, totalCooling * 1.5f);

            // Inner wall cools slower (insulated by tire material)
            innerWallTemperature = Mathf.Lerp(innerWallTemperature, centerTemperature, conductionCooling);
            innerWallTemperature = Mathf.Lerp(innerWallTemperature, AmbientTemperature, totalCooling * 0.5f);
        }

        /// <summary>
        /// Clamp all temperatures to realistic ranges.
        /// </summary>
        private void ClampTemperatures()
        {
            centerTemperature = Mathf.Clamp(centerTemperature, AmbientTemperature, MaxTemperature);
            edgeLeftTemperature = Mathf.Clamp(edgeLeftTemperature, AmbientTemperature, MaxTemperature);
            edgeRightTemperature = Mathf.Clamp(edgeRightTemperature, AmbientTemperature, MaxTemperature);
            innerWallTemperature = Mathf.Clamp(innerWallTemperature, AmbientTemperature, MaxTemperature);
            outerWallTemperature = Mathf.Clamp(outerWallTemperature, AmbientTemperature, MaxTemperature);
        }

        /// <summary>
        /// Update average temperature from all zones.
        /// </summary>
        private void UpdateAverageTemperature()
        {
            averageTemperature = (centerTemperature * 0.4f +
                                 edgeLeftTemperature * 0.15f +
                                 edgeRightTemperature * 0.15f +
                                 innerWallTemperature * 0.15f +
                                 outerWallTemperature * 0.15f);
        }

        /// <summary>
        /// Get grip multiplier based on tire temperature.
        /// </summary>
        public float GetTemperatureGripFactor()
        {
            // Use average temperature for grip calculation
            return gripVsTemperature.Evaluate(averageTemperature);
        }

        /// <summary>
        /// Get wear rate multiplier based on tire temperature.
        /// </summary>
        public float GetTemperatureWearFactor()
        {
            return wearRateVsTemperature.Evaluate(averageTemperature);
        }

        /// <summary>
        /// Get temperature variance (0-1) indicating uneven heating.
        /// </summary>
        public float GetTemperatureVariance()
        {
            float minTemp = Mathf.Min(centerTemperature, edgeLeftTemperature, edgeRightTemperature, innerWallTemperature, outerWallTemperature);
            float maxTemp = Mathf.Max(centerTemperature, edgeLeftTemperature, edgeRightTemperature, innerWallTemperature, outerWallTemperature);

            float variance = (maxTemp - minTemp) / MaxTemperature;
            return Mathf.Clamp01(variance);
        }

        public TemperatureState GetTemperatureState()
        {
            return new TemperatureState
            {
                AverageTemp = averageTemperature,
                CenterTemp = centerTemperature,
                EdgeLeftTemp = edgeLeftTemperature,
                EdgeRightTemp = edgeRightTemperature,
                InnerWallTemp = innerWallTemperature,
                OuterWallTemp = outerWallTemperature,
                TemperatureVariance = GetTemperatureVariance()
            };
        }

        // Getters
        public float GetAverageTemperature() => averageTemperature;
        public float GetCenterTemperature() => centerTemperature;
        public float GetEdgeTemperatureVariation() => Mathf.Abs(edgeLeftTemperature - edgeRightTemperature);
    }
}
