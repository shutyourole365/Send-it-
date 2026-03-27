using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Tire pressure system for Phase 2.
    /// Simulates pressure changes with temperature and their effects on grip and wear.
    /// </summary>
    public class TirePressureSystem
    {
        // Pressure state (PSI)
        private float currentPressure = 32f; // Typical car tire: 30-35 PSI
        private float coldPressure = 32f; // Baseline pressure at 20°C

        // Pressure characteristics
        private float pressureTemperatureCoefficient = 0.1f; // PSI per °C
        private float minimumPressure = 28f;
        private float maximumPressure = 45f;
        private float optimalPressure = 32f;
        private float underPressureWarning = 28f;
        private float overPressureWarning = 38f;

        // Performance effects
        private float gripPerformanceAtOptimal = 1.0f;
        private float wearRateAtOptimal = 1.0f;

        public struct PressureState
        {
            public float CurrentPressure;
            public float IdealPressure;
            public float GripFactor;
            public float WearFactor;
            public float TemperaturePressureEffect;
            public bool IsUnderPressure;
            public bool IsOverPressure;
            public bool IsBlowoutRisk;
        }

        public TirePressureSystem(float initialPressure = 32f)
        {
            coldPressure = initialPressure;
            currentPressure = initialPressure;
            optimalPressure = initialPressure;
        }

        /// <summary>
        /// Update tire pressure based on temperature.
        /// </summary>
        public void Update(float tireTemperature)
        {
            // Apply ideal gas law: P1/T1 = P2/T2
            const float referenceTemperature = 20f; // Celsius
            float temperatureDifference = tireTemperature - referenceTemperature;

            // Pressure increases with temperature
            float pressureFromTemperature = coldPressure + (temperatureDifference * pressureTemperatureCoefficient);
            currentPressure = Mathf.Clamp(pressureFromTemperature, minimumPressure, maximumPressure);

            // Slow leak simulation (optional)
            SimulatePressureLoss();
        }

        /// <summary>
        /// Simulate slow pressure loss over time (air leaks, permeability).
        /// </summary>
        private void SimulatePressureLoss()
        {
            // Very slow leak: 0.01 PSI per second (realistic for driving)
            float leakRate = 0.0001f * Time.deltaTime; // 0.01 PSI over ~100 seconds
            currentPressure -= leakRate;
            currentPressure = Mathf.Max(currentPressure, minimumPressure * 0.5f);
        }

        /// <summary>
        /// Get grip factor affected by tire pressure.
        /// Optimal pressure gives best grip, under/over reduces it.
        /// </summary>
        public float GetPressureGripFactor()
        {
            float pressureDifference = currentPressure - optimalPressure;

            if (Mathf.Abs(pressureDifference) < 1f)
            {
                // Within ±1 PSI: optimal grip
                return 1.0f;
            }
            else if (currentPressure < optimalPressure)
            {
                // Under-pressure: reduces grip and increases heating
                // More severe reduction for extreme under-pressure
                float underPressureFactor = (optimalPressure - currentPressure) / (optimalPressure - minimumPressure);
                underPressureFactor = Mathf.Clamp01(underPressureFactor);

                // Parabolic loss: starts small, increases dramatically
                return 1.0f - (underPressureFactor * underPressureFactor * 0.4f);
            }
            else
            {
                // Over-pressure: reduces grip and increases wear
                float overPressureFactor = (currentPressure - optimalPressure) / (maximumPressure - optimalPressure);
                overPressureFactor = Mathf.Clamp01(overPressureFactor);

                // More linear loss for over-pressure
                return 1.0f - (overPressureFactor * 0.3f);
            }
        }

        /// <summary>
        /// Get wear rate affected by tire pressure.
        /// Extreme pressures increase wear.
        /// </summary>
        public float GetPressureWearFactor()
        {
            float pressureDifference = Mathf.Abs(currentPressure - optimalPressure);

            if (pressureDifference < 1f)
            {
                return 1.0f; // Baseline wear at optimal
            }
            else if (currentPressure < optimalPressure)
            {
                // Under-pressure: causes edge wear and increased friction
                float underPressureFactor = (optimalPressure - currentPressure) / (optimalPressure - minimumPressure);
                underPressureFactor = Mathf.Clamp01(underPressureFactor);

                return 1.0f + (underPressureFactor * 2.0f); // Up to 3x wear at minimum pressure
            }
            else
            {
                // Over-pressure: causes center wear
                float overPressureFactor = (currentPressure - optimalPressure) / (maximumPressure - optimalPressure);
                overPressureFactor = Mathf.Clamp01(overPressureFactor);

                return 1.0f + (overPressureFactor * 1.5f); // Up to 2.5x wear at maximum pressure
            }
        }

        /// <summary>
        /// Get temperature increase effect from pressure issues.
        /// </summary>
        public float GetPressureTemperatureEffect()
        {
            float pressureDifference = Mathf.Abs(currentPressure - optimalPressure);

            if (currentPressure < minimumPressure * 1.1f)
            {
                // Severely under-pressure: dangerous heat buildup
                return 2.0f;
            }
            else if (currentPressure < optimalPressure - 2f)
            {
                // Under-pressure: increased friction
                return 1.5f;
            }
            else if (currentPressure > optimalPressure + 5f)
            {
                // Over-pressure: slight temperature increase from stiffness
                return 1.1f;
            }

            return 1.0f;
        }

        /// <summary>
        /// Check if tire is under-pressured.
        /// </summary>
        public bool IsUnderPressure()
        {
            return currentPressure < underPressureWarning;
        }

        /// <summary>
        /// Check if tire is over-pressured.
        /// </summary>
        public bool IsOverPressure()
        {
            return currentPressure > overPressureWarning;
        }

        /// <summary>
        /// Check if blowout risk exists (critical condition).
        /// </summary>
        public bool IsBlowoutRisk()
        {
            // Very low pressure or very high pressure is dangerous
            return currentPressure < minimumPressure * 0.8f || currentPressure > maximumPressure * 0.95f;
        }

        /// <summary>
        /// Manually set pressure (for pit stop adjustments).
        /// </summary>
        public void SetPressure(float newPressure)
        {
            coldPressure = Mathf.Clamp(newPressure, minimumPressure, maximumPressure);
            currentPressure = coldPressure;
        }

        /// <summary>
        /// Get pressure status (0 = under, 1 = optimal, 2 = over).
        /// </summary>
        public int GetPressureStatus()
        {
            if (currentPressure < optimalPressure - 0.5f)
                return 0; // Under
            else if (currentPressure > optimalPressure + 0.5f)
                return 2; // Over
            else
                return 1; // Optimal
        }

        public PressureState GetPressureState()
        {
            return new PressureState
            {
                CurrentPressure = currentPressure,
                IdealPressure = optimalPressure,
                GripFactor = GetPressureGripFactor(),
                WearFactor = GetPressureWearFactor(),
                TemperaturePressureEffect = GetPressureTemperatureEffect(),
                IsUnderPressure = IsUnderPressure(),
                IsOverPressure = IsOverPressure(),
                IsBlowoutRisk = IsBlowoutRisk()
            };
        }

        // Getters
        public float GetCurrentPressure() => currentPressure;
        public float GetOptimalPressure() => optimalPressure;
        public float GetPressureDelta() => currentPressure - optimalPressure;
    }
}
