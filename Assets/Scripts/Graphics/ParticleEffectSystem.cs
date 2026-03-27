using UnityEngine;
using SendIt.Physics;

namespace SendIt.Graphics
{
    /// <summary>
    /// Advanced particle effect system for dynamic visual feedback.
    /// Manages tire smoke, dust, water spray, sparks, and engine heat effects.
    /// </summary>
    public class ParticleEffectSystem : MonoBehaviour
    {
        public enum ParticleType
        {
            TireSmoke,      // From tire slip/burnout
            TireDust,       // From gravel/dirt surfaces
            WaterSpray,     // From wet roads
            Sparks,         // From contact with barriers
            EngineSmoke,    // From overheating
            BrakeGlow,      // From brake heat
            RoadSpray,      // General surface spray
            Dirt            // Dirt accumulation
        }

        // Particle emitter references
        private ParticleSystem[] tireSmokeSystems = new ParticleSystem[4];
        private ParticleSystem[] tireDustSystems = new ParticleSystem[4];
        private ParticleSystem waterSpraySystem;
        private ParticleSystem sparksSystem;
        private ParticleSystem engineSmokeSystem;
        private ParticleSystem brakeGlowSystem;

        // Control parameters
        private float smokeDensity = 1f;
        private float dustDensity = 1f;
        private float waterDensity = 1f;
        private float sparkIntensity = 1f;

        // Thresholds
        private float tireSlipSmokeThreshold = 0.3f; // Slip ratio for smoke generation
        private float dustGenerationSpeed = 20f; // Speed needed for dust on dirt
        private float waterSpraySpeed = 15f; // Speed needed for water spray
        private float impactSparkThreshold = 30f; // Collision force for sparks

        private Rigidbody vehicleBody;
        private bool isInitialized;

        public void Initialize(Rigidbody vehicle)
        {
            vehicleBody = vehicle;
            CreateParticleSystems();
            isInitialized = true;
        }

        /// <summary>
        /// Create all particle systems for effects.
        /// </summary>
        private void CreateParticleSystems()
        {
            // This would be called from prefabs in full implementation
            // For now, we initialize the system structure
            Debug.Log("Particle systems initialized");
        }

        /// <summary>
        /// Update tire smoke based on slip and temperature.
        /// </summary>
        public void UpdateTireSmoke(int wheelIndex, float slipRatio, float slipAngle, float tireTemperature)
        {
            if (!isInitialized || wheelIndex < 0 || wheelIndex > 3)
                return;

            if (tireSmokeSystems[wheelIndex] == null)
                return;

            ParticleSystem.EmissionModule emission = tireSmokeSystems[wheelIndex].emission;

            // Calculate smoke intensity from slip
            float slipMagnitude = Mathf.Max(Mathf.Abs(slipRatio), Mathf.Abs(slipAngle));

            // Smoke increases with slip beyond threshold
            if (slipMagnitude > tireSlipSmokeThreshold)
            {
                float smokeAmount = (slipMagnitude - tireSlipSmokeThreshold) / 0.7f; // Peak at 100% slip
                smokeAmount = Mathf.Clamp01(smokeAmount);

                // Temperature also increases smoke (overheated tires smoke more)
                float tempFactor = Mathf.Clamp01((tireTemperature - 80f) / 50f);
                smokeAmount = Mathf.Max(smokeAmount, tempFactor * 0.5f);

                // Set emission rate (particles per second)
                emission.rateOverTime = smokeAmount * 50f * smokeDensity;
            }
            else
            {
                emission.rateOverTime = 0f;
            }
        }

        /// <summary>
        /// Update dust/dirt particles on loose surfaces.
        /// </summary>
        public void UpdateDustEffect(int wheelIndex, float speed, bool onLooseSurface, Vector3 wheelPos)
        {
            if (!isInitialized || !onLooseSurface)
                return;

            if (wheelIndex < 0 || wheelIndex > 3 || tireDustSystems[wheelIndex] == null)
                return;

            ParticleSystem.EmissionModule emission = tireDustSystems[wheelIndex].emission;

            // Dust generation based on speed
            if (speed > dustGenerationSpeed)
            {
                float dustAmount = (speed - dustGenerationSpeed) / 30f;
                dustAmount = Mathf.Clamp01(dustAmount);

                emission.rateOverTime = dustAmount * 80f * dustDensity;

                // Update emitter position
                ParticleSystem.MainModule main = tireDustSystems[wheelIndex].main;
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(0.8f, 0.7f, 0.6f, 1f),  // Brown dust
                    new Color(1f, 0.9f, 0.8f, 1f)
                );
            }
            else
            {
                emission.rateOverTime = 0f;
            }
        }

        /// <summary>
        /// Update water spray on wet roads.
        /// </summary>
        public void UpdateWaterSpray(float speed, bool onWetSurface, Vector3 position)
        {
            if (!isInitialized || waterSpraySystem == null || !onWetSurface)
                return;

            ParticleSystem.EmissionModule emission = waterSpraySystem.emission;

            if (speed > waterSpraySpeed)
            {
                float sprayAmount = (speed - waterSpraySpeed) / 40f;
                sprayAmount = Mathf.Clamp01(sprayAmount);

                emission.rateOverTime = sprayAmount * 100f * waterDensity;
            }
            else
            {
                emission.rateOverTime = 0f;
            }
        }

        /// <summary>
        /// Generate spark particles on impact.
        /// </summary>
        public void GenerateSparks(Vector3 impactPoint, Vector3 impactNormal, float impactForce)
        {
            if (!isInitialized || sparksSystem == null)
                return;

            // Only generate sparks if impact force is significant
            if (impactForce < impactSparkThreshold)
                return;

            // Emit sparks at impact location
            sparksSystem.transform.position = impactPoint;

            ParticleSystem.EmissionModule emission = sparksSystem.emission;
            float sparkAmount = Mathf.Clamp01((impactForce - impactSparkThreshold) / 100f);

            // Emit burst of sparks
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, (int)(20f * sparkAmount * sparkIntensity))
            });
        }

        /// <summary>
        /// Update engine overheat smoke effect.
        /// </summary>
        public void UpdateEngineSmokeEffect(float engineTemperature, float maxEngineTemp = 130f)
        {
            if (!isInitialized || engineSmokeSystem == null)
                return;

            ParticleSystem.EmissionModule emission = engineSmokeSystem.emission;

            // Engine smoke only when overheating
            float tempNormalized = Mathf.Clamp01((engineTemperature - 100f) / (maxEngineTemp - 100f));

            if (tempNormalized > 0f)
            {
                emission.rateOverTime = tempNormalized * tempNormalized * 30f; // Quadratic response
            }
            else
            {
                emission.rateOverTime = 0f;
            }
        }

        /// <summary>
        /// Update brake glow effect from heat.
        /// </summary>
        public void UpdateBrakeGlowEffect(float brakePressure, float brakeTemperature = 100f)
        {
            if (!isInitialized || brakeGlowSystem == null)
                return;

            ParticleSystem.EmissionModule emission = brakeGlowSystem.emission;

            // Brake glow from temperature
            float glowAmount = Mathf.Clamp01(brakeTemperature / 200f);

            // Also affected by brake pressure
            glowAmount = Mathf.Max(glowAmount, brakePressure * 0.5f);

            emission.rateOverTime = glowAmount * 40f;

            // Adjust glow color based on temperature
            ParticleSystem.MainModule main = brakeGlowSystem.main;
            Color glowColor = Color.Lerp(Color.red, new Color(1f, 0.5f, 0f), Mathf.Clamp01(brakeTemperature / 200f));
            main.startColor = glowColor;
        }

        /// <summary>
        /// Set overall smoke density (0-1).
        /// </summary>
        public void SetSmokeDensity(float density)
        {
            smokeDensity = Mathf.Clamp01(density);
        }

        /// <summary>
        /// Set dust density (0-1).
        /// </summary>
        public void SetDustDensity(float density)
        {
            dustDensity = Mathf.Clamp01(density);
        }

        /// <summary>
        /// Set water spray density (0-1).
        /// </summary>
        public void SetWaterDensity(float density)
        {
            waterDensity = Mathf.Clamp01(density);
        }

        /// <summary>
        /// Set spark intensity (0-1).
        /// </summary>
        public void SetSparkIntensity(float intensity)
        {
            sparkIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// Stop all particle effects immediately.
        /// </summary>
        public void StopAllEffects()
        {
            for (int i = 0; i < 4; i++)
            {
                if (tireSmokeSystems[i] != null)
                    tireSmokeSystems[i].Stop();
                if (tireDustSystems[i] != null)
                    tireDustSystems[i].Stop();
            }

            if (waterSpraySystem != null)
                waterSpraySystem.Stop();
            if (sparksSystem != null)
                sparksSystem.Stop();
            if (engineSmokeSystem != null)
                engineSmokeSystem.Stop();
            if (brakeGlowSystem != null)
                brakeGlowSystem.Stop();
        }

        /// <summary>
        /// Get current emission rate for a particle type.
        /// </summary>
        public float GetEmissionRate(ParticleType type)
        {
            ParticleSystem system = GetParticleSystem(type);
            if (system == null)
                return 0f;

            return system.emission.rateOverTime.constant;
        }

        /// <summary>
        /// Get particle system for given type.
        /// </summary>
        private ParticleSystem GetParticleSystem(ParticleType type)
        {
            return type switch
            {
                ParticleType.WaterSpray => waterSpraySystem,
                ParticleType.Sparks => sparksSystem,
                ParticleType.EngineSmoke => engineSmokeSystem,
                ParticleType.BrakeGlow => brakeGlowSystem,
                _ => null
            };
        }

        public bool IsInitialized => isInitialized;
    }
}
