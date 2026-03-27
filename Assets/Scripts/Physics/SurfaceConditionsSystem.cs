using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Surface conditions system for Phase 2.
    /// Handles different road surfaces and their effects on tire grip and wear.
    /// </summary>
    public class SurfaceConditionsSystem
    {
        public enum SurfaceType
        {
            DryAsphalt,
            WetAsphalt,
            DampAsphalt,
            Gravel,
            Dirt,
            Grass,
            Concrete,
            Ice,
            Snow,
            WetConcrete
        }

        public struct SurfaceProperties
        {
            public float GripCoefficient;
            public float WearMultiplier;
            public float TemperatureMultiplier;
            public float NoiseLevel;
            public float Bumpiness;
            public float AquaplaningThreshold; // Speed at which aquaplaning occurs
        }

        private SurfaceType currentSurfaceType = SurfaceType.DryAsphalt;
        private SurfaceProperties currentSurfaceProperties;
        private float wetness = 0f; // 0 = dry, 1 = soaking wet
        private float temperature = 20f; // Ambient temperature in Celsius

        public SurfaceConditionsSystem()
        {
            UpdateSurfaceProperties();
        }

        /// <summary>
        /// Set the current surface type and update properties.
        /// </summary>
        public void SetSurfaceType(SurfaceType surfaceType)
        {
            currentSurfaceType = surfaceType;
            UpdateSurfaceProperties();
        }

        /// <summary>
        /// Set wetness level (0 = dry, 1 = soaking wet).
        /// </summary>
        public void SetWetness(float wetnessLevel)
        {
            wetness = Mathf.Clamp01(wetnessLevel);
            UpdateSurfaceProperties();
        }

        /// <summary>
        /// Set ambient temperature affecting grip and conditions.
        /// </summary>
        public void SetAmbientTemperature(float temp)
        {
            temperature = temp;
        }

        /// <summary>
        /// Update surface properties based on current type and conditions.
        /// </summary>
        private void UpdateSurfaceProperties()
        {
            // Get base properties for surface type
            SurfaceProperties baseProperties = GetBaseProperties(currentSurfaceType);

            // Apply wetness modifier
            if (wetness > 0f)
            {
                ApplyWetnessModifier(ref baseProperties, wetness);
            }

            // Apply temperature effects
            ApplyTemperatureEffects(ref baseProperties);

            currentSurfaceProperties = baseProperties;
        }

        /// <summary>
        /// Get base grip and wear properties for each surface type.
        /// </summary>
        private SurfaceProperties GetBaseProperties(SurfaceType surface)
        {
            SurfaceProperties props = new SurfaceProperties();

            switch (surface)
            {
                case SurfaceType.DryAsphalt:
                    props.GripCoefficient = 1.0f;
                    props.WearMultiplier = 1.0f;
                    props.TemperatureMultiplier = 1.0f;
                    props.NoiseLevel = 0.5f;
                    props.Bumpiness = 0.2f;
                    props.AquaplaningThreshold = 100f; // Very high speed
                    break;

                case SurfaceType.WetAsphalt:
                    props.GripCoefficient = 0.65f;
                    props.WearMultiplier = 0.8f;
                    props.TemperatureMultiplier = 0.7f;
                    props.NoiseLevel = 0.4f;
                    props.Bumpiness = 0.25f;
                    props.AquaplaningThreshold = 60f;
                    break;

                case SurfaceType.DampAsphalt:
                    props.GripCoefficient = 0.8f;
                    props.WearMultiplier = 0.9f;
                    props.TemperatureMultiplier = 0.85f;
                    props.NoiseLevel = 0.45f;
                    props.Bumpiness = 0.22f;
                    props.AquaplaningThreshold = 80f;
                    break;

                case SurfaceType.Gravel:
                    props.GripCoefficient = 0.55f;
                    props.WearMultiplier = 2.0f;
                    props.TemperatureMultiplier = 1.2f;
                    props.NoiseLevel = 0.8f;
                    props.Bumpiness = 0.6f;
                    props.AquaplaningThreshold = 999f; // No aquaplaning
                    break;

                case SurfaceType.Dirt:
                    props.GripCoefficient = 0.45f;
                    props.WearMultiplier = 2.5f;
                    props.TemperatureMultiplier = 1.3f;
                    props.NoiseLevel = 0.9f;
                    props.Bumpiness = 0.7f;
                    props.AquaplaningThreshold = 999f;
                    break;

                case SurfaceType.Grass:
                    props.GripCoefficient = 0.35f;
                    props.WearMultiplier = 3.0f;
                    props.TemperatureMultiplier = 0.8f;
                    props.NoiseLevel = 0.3f;
                    props.Bumpiness = 0.5f;
                    props.AquaplaningThreshold = 40f;
                    break;

                case SurfaceType.Concrete:
                    props.GripCoefficient = 0.95f;
                    props.WearMultiplier = 1.1f;
                    props.TemperatureMultiplier = 1.05f;
                    props.NoiseLevel = 0.6f;
                    props.Bumpiness = 0.15f;
                    props.AquaplaningThreshold = 90f;
                    break;

                case SurfaceType.Ice:
                    props.GripCoefficient = 0.15f;
                    props.WearMultiplier = 0.5f;
                    props.TemperatureMultiplier = 0.3f;
                    props.NoiseLevel = 0.2f;
                    props.Bumpiness = 0.1f;
                    props.AquaplaningThreshold = 10f;
                    break;

                case SurfaceType.Snow:
                    props.GripCoefficient = 0.25f;
                    props.WearMultiplier = 0.6f;
                    props.TemperatureMultiplier = 0.4f;
                    props.NoiseLevel = 0.4f;
                    props.Bumpiness = 0.4f;
                    props.AquaplaningThreshold = 20f;
                    break;

                case SurfaceType.WetConcrete:
                    props.GripCoefficient = 0.6f;
                    props.WearMultiplier = 0.9f;
                    props.TemperatureMultiplier = 0.75f;
                    props.NoiseLevel = 0.5f;
                    props.Bumpiness = 0.18f;
                    props.AquaplaningThreshold = 70f;
                    break;

                default:
                    props.GripCoefficient = 1.0f;
                    props.WearMultiplier = 1.0f;
                    props.TemperatureMultiplier = 1.0f;
                    props.NoiseLevel = 0.5f;
                    props.Bumpiness = 0.2f;
                    props.AquaplaningThreshold = 100f;
                    break;
            }

            return props;
        }

        /// <summary>
        /// Apply wetness modifiers to surface properties.
        /// </summary>
        private void ApplyWetnessModifier(ref SurfaceProperties props, float wetnessLevel)
        {
            // Wet surfaces lose grip significantly
            props.GripCoefficient *= Mathf.Lerp(1.0f, 0.6f, wetnessLevel);

            // Wet surfaces wear less
            props.WearMultiplier *= Mathf.Lerp(1.0f, 0.8f, wetnessLevel);

            // Temperature generation is lower on wet surfaces
            props.TemperatureMultiplier *= Mathf.Lerp(1.0f, 0.7f, wetnessLevel);

            // Aquaplaning threshold is reduced with water
            props.AquaplaningThreshold *= Mathf.Lerp(1.0f, 0.7f, wetnessLevel);
        }

        /// <summary>
        /// Apply temperature effects on grip and wear.
        /// </summary>
        private void ApplyTemperatureEffects(ref SurfaceProperties props)
        {
            // Cold temperatures reduce grip (ice risk)
            if (temperature < 0f)
            {
                float coldFactor = Mathf.Clamp01(temperature / -20f); // At -20°C, strong effect
                props.GripCoefficient *= Mathf.Lerp(0.5f, 1.0f, coldFactor);
            }

            // Very hot temperatures can soften asphalt
            if (temperature > 40f)
            {
                float heatFactor = Mathf.Clamp01((temperature - 40f) / 20f);
                props.GripCoefficient *= Mathf.Lerp(1.0f, 0.95f, heatFactor);
                props.WearMultiplier *= Mathf.Lerp(1.0f, 1.2f, heatFactor);
            }
        }

        /// <summary>
        /// Check if aquaplaning conditions exist.
        /// </summary>
        public bool IsAquaplaning(float vehicleSpeed)
        {
            if (wetness < 0.3f)
                return false;

            return vehicleSpeed > currentSurfaceProperties.AquaplaningThreshold;
        }

        /// <summary>
        /// Get effective grip coefficient for current conditions.
        /// </summary>
        public float GetGripCoefficient()
        {
            return currentSurfaceProperties.GripCoefficient;
        }

        /// <summary>
        /// Get wear multiplier for current surface.
        /// </summary>
        public float GetWearMultiplier()
        {
            return currentSurfaceProperties.WearMultiplier;
        }

        /// <summary>
        /// Get temperature generation multiplier.
        /// </summary>
        public float GetTemperatureMultiplier()
        {
            return currentSurfaceProperties.TemperatureMultiplier;
        }

        /// <summary>
        /// Get surface bumpiness for vibration effects.
        /// </summary>
        public float GetBumpiness()
        {
            return currentSurfaceProperties.Bumpiness;
        }

        /// <summary>
        /// Get surface noise level (for audio feedback).
        /// </summary>
        public float GetNoiseLevel()
        {
            return currentSurfaceProperties.NoiseLevel;
        }

        public SurfaceProperties GetSurfaceProperties()
        {
            return currentSurfaceProperties;
        }

        public SurfaceType GetSurfaceType()
        {
            return currentSurfaceType;
        }

        public float GetWetness()
        {
            return wetness;
        }
    }
}
