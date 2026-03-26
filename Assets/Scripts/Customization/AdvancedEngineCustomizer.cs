using UnityEngine;
using SendIt.Physics;
using SendIt.Data;

namespace SendIt.Customization
{
    /// <summary>
    /// Manages advanced engine modifications including turbocharging, supercharging,
    /// ECU tuning, exhaust systems, and intake upgrades.
    /// </summary>
    public class AdvancedEngineCustomizer : MonoBehaviour
    {
        private PhysicsData physicsData;
        private Engine engine;

        // Engine modifications
        private int inletType = 0; // 0=Stock, 1=Performance, 2=Racing, 3=Custom
        private int exhaustSystem = 0; // 0=Stock, 1=Performance, 2=Racing, 3=Full Race
        private int boostSystem = 0; // 0=None, 1=Turbo (single), 2=Turbo (twin), 3=Supercharger
        private float boostPressure = 0f; // 0-2.5 bar
        private int ecuTune = 0; // 0=Stock, 1=Mild, 2=Aggressive, 3=Race
        private float fuelOctane = 87f; // 87-105+
        private bool hasNitro = false;
        private float nitroPower = 0f; // 0-1

        // Performance gains
        private float basePowerGain = 1f; // 1.0 = 100% of original
        private float baseTorqueGain = 1f;
        private float responseMultiplier = 1f; // Engine responsiveness
        private float reliabilityFactor = 1f; // 1.0 = fully reliable, < 1.0 = prone to failure

        [System.Serializable]
        public struct EngineModSettings
        {
            public int InletType;
            public int ExhaustSystem;
            public int BoostSystem;
            public float BoostPressure;
            public int EcuTune;
            public float FuelOctane;
            public bool HasNitro;
            public float NitroPower;
            public float PowerGain;
            public float TorqueGain;
            public float ReliabilityFactor;
        }

        public void Initialize(PhysicsData data)
        {
            physicsData = data;
            CalculatePerformanceGains();
        }

        /// <summary>
        /// Set intake/inlet type.
        /// </summary>
        public void SetInletType(int type)
        {
            inletType = Mathf.Clamp(type, 0, 3);
            CalculatePerformanceGains();
        }

        /// <summary>
        /// Set exhaust system type.
        /// </summary>
        public void SetExhaustSystem(int system)
        {
            exhaustSystem = Mathf.Clamp(system, 0, 3);
            CalculatePerformanceGains();
        }

        /// <summary>
        /// Set boost system type.
        /// </summary>
        public void SetBoostSystem(int boost)
        {
            boostSystem = Mathf.Clamp(boost, 0, 3);
            if (boostSystem == 0)
                boostPressure = 0f;
            CalculatePerformanceGains();
        }

        /// <summary>
        /// Set boost pressure (for turbo/supercharger).
        /// </summary>
        public void SetBoostPressure(float pressure)
        {
            boostPressure = Mathf.Clamp(pressure, 0f, 2.5f);
            CalculatePerformanceGains();
        }

        /// <summary>
        /// Set ECU tune level.
        /// </summary>
        public void SetEcuTune(int tune)
        {
            ecuTune = Mathf.Clamp(tune, 0, 3);
            CalculatePerformanceGains();
        }

        /// <summary>
        /// Set fuel octane rating.
        /// </summary>
        public void SetFuelOctane(float octane)
        {
            fuelOctane = Mathf.Clamp(octane, 87f, 115f);
            CalculatePerformanceGains();
        }

        /// <summary>
        /// Enable/disable nitrous oxide system.
        /// </summary>
        public void SetNitro(bool enabled)
        {
            hasNitro = enabled;
            if (!enabled)
                nitroPower = 0f;
            CalculatePerformanceGains();
        }

        /// <summary>
        /// Set nitro power level (0-1, where 1 = maximum).
        /// </summary>
        public void SetNitroPower(float power)
        {
            nitroPower = Mathf.Clamp01(power);
            if (nitroPower > 0)
                hasNitro = true;
        }

        /// <summary>
        /// Calculate performance gains from modifications.
        /// </summary>
        private void CalculatePerformanceGains()
        {
            basePowerGain = 1f;
            baseTorqueGain = 1f;
            responseMultiplier = 1f;
            reliabilityFactor = 1f;

            // Inlet gains
            basePowerGain += inletType * 0.05f;
            responseMultiplier += inletType * 0.1f;

            // Exhaust gains
            basePowerGain += exhaustSystem * 0.08f;
            baseTorqueGain += exhaustSystem * 0.05f;

            // Boost system gains (most significant)
            switch (boostSystem)
            {
                case 1: // Turbo (single)
                    basePowerGain += 0.35f;
                    basePowerGain += boostPressure * 0.1f; // Additional for pressure
                    reliabilityFactor -= 0.15f;
                    break;
                case 2: // Twin turbo
                    basePowerGain += 0.5f;
                    basePowerGain += boostPressure * 0.15f;
                    reliabilityFactor -= 0.2f;
                    break;
                case 3: // Supercharger
                    basePowerGain += 0.4f;
                    basePowerGain += boostPressure * 0.12f;
                    baseTorqueGain += 0.3f;
                    reliabilityFactor -= 0.1f; // More reliable than turbos
                    break;
            }

            // ECU tune gains
            basePowerGain += ecuTune * 0.1f;
            baseTorqueGain += ecuTune * 0.08f;
            responseMultiplier += ecuTune * 0.2f;
            reliabilityFactor -= ecuTune * 0.08f; // Higher tunes reduce reliability

            // Fuel octane allows higher boost
            if (fuelOctane > 91f)
            {
                float octaneBonus = (fuelOctane - 91f) / 24f; // Normalize to 0-1
                basePowerGain += octaneBonus * 0.1f;
            }

            // Nitro system
            if (hasNitro)
            {
                basePowerGain += nitroPower * 0.2f; // Temporary boost
                reliabilityFactor -= 0.1f;
            }

            // Clamp values
            basePowerGain = Mathf.Clamp(basePowerGain, 0.5f, 3f); // 50% to 300% power
            baseTorqueGain = Mathf.Clamp(baseTorqueGain, 0.5f, 2.5f);
            responseMultiplier = Mathf.Clamp(responseMultiplier, 0.5f, 3f);
            reliabilityFactor = Mathf.Clamp(reliabilityFactor, 0.3f, 1f); // Minimum 30% reliability
        }

        /// <summary>
        /// Get the power multiplier for the modified engine.
        /// </summary>
        public float GetPowerMultiplier() => basePowerGain;

        /// <summary>
        /// Get the torque multiplier.
        /// </summary>
        public float GetTorqueMultiplier() => baseTorqueGain;

        /// <summary>
        /// Get engine responsiveness multiplier.
        /// </summary>
        public float GetResponseMultiplier() => responseMultiplier;

        /// <summary>
        /// Get engine reliability rating (affects failure chance).
        /// </summary>
        public float GetReliabilityFactor() => reliabilityFactor;

        /// <summary>
        /// Simulate engine failure from over-tuning.
        /// </summary>
        public bool SimulateEngineFailure()
        {
            // Higher modifications = higher failure chance
            float failureChance = (1f - reliabilityFactor) * 0.01f; // Max 0.7% per frame at 30% reliability
            return Random.value < failureChance;
        }

        /// <summary>
        /// Get engine modification summary.
        /// </summary>
        public string GetModificationSummary()
        {
            string summary = "";
            summary += $"Power: +{(basePowerGain - 1f) * 100:F0}% ";
            summary += $"| Torque: +{(baseTorqueGain - 1f) * 100:F0}% ";
            summary += $"| Reliability: {reliabilityFactor * 100:F0}%";
            return summary;
        }

        /// <summary>
        /// Apply engine modifications to physics engine.
        /// </summary>
        public void ApplyModificationsToEngine(Engine engine)
        {
            if (engine == null)
                return;

            // This would modify the engine torque curve by the gain factors
            // Implementation depends on how Engine class is structured
        }

        /// <summary>
        /// Get current engine modification settings.
        /// </summary>
        public EngineModSettings GetEngineModSettings()
        {
            return new EngineModSettings
            {
                InletType = inletType,
                ExhaustSystem = exhaustSystem,
                BoostSystem = boostSystem,
                BoostPressure = boostPressure,
                EcuTune = ecuTune,
                FuelOctane = fuelOctane,
                HasNitro = hasNitro,
                NitroPower = nitroPower,
                PowerGain = basePowerGain,
                TorqueGain = baseTorqueGain,
                ReliabilityFactor = reliabilityFactor
            };
        }

        // Getters
        public int GetInletType() => inletType;
        public int GetExhaustSystem() => exhaustSystem;
        public int GetBoostSystem() => boostSystem;
        public float GetBoostPressure() => boostPressure;
        public int GetEcuTune() => ecuTune;
        public float GetFuelOctane() => fuelOctane;
        public bool HasNitro() => hasNitro;
    }
}
