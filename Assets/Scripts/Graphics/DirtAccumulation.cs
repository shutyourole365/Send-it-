using UnityEngine;
using System.Collections.Generic;

namespace SendIt.Graphics
{
    /// <summary>
    /// Manages dirt and mud accumulation on the vehicle.
    /// Tracks dirt from different terrain types with varying intensity and persistence.
    /// </summary>
    public class DirtAccumulation : MonoBehaviour
    {
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private float dirtBuildupRate = 0.05f; // How fast dirt accumulates
        [SerializeField] private float maxDirtAmount = 1f; // Maximum dirt coverage 0-1

        private float currentDirtLevel = 0f; // Total dirt on vehicle
        private Dictionary<string, float> dirtBySource = new Dictionary<string, float>();

        // Dirt properties per surface type
        private enum DirtType
        {
            Road,      // Light grey dust
            Grass,     // Green/brown dirt
            Sand,      // Light tan dirt
            Mud,       // Dark brown mud
            Gravel     // Grey dust
        }

        private struct DirtParticle
        {
            public Vector3 Position;
            public DirtType Type;
            public float Amount; // 0-1
            public float CreationTime;
        }

        private List<DirtParticle> dirtParticles = new List<DirtParticle>();

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize dirt accumulation system.
        /// </summary>
        private void Initialize()
        {
            if (bodyRenderers == null || bodyRenderers.Length == 0)
            {
                bodyRenderers = GetComponentsInChildren<Renderer>();
            }
        }

        /// <summary>
        /// Add dirt from terrain contact.
        /// </summary>
        public void AddDirtFromTerrain(Vector3 contactPoint, string terrainTag, float speed, float wheelLoad)
        {
            // Determine dirt type
            DirtType dirtType = GetDirtType(terrainTag);

            // Calculate dirt accumulation
            // Higher speed = more splashing/accumulation
            // Higher load = more contact = more dirt
            float speedFactor = Mathf.Clamp01(speed / 20f); // Normalized to 20 m/s
            float loadFactor = Mathf.Clamp01(wheelLoad / 5000f); // Normalized to 5000N
            float dirtAmount = speedFactor * loadFactor * dirtBuildupRate;

            // Add dirt
            currentDirtLevel = Mathf.Clamp01(currentDirtLevel + dirtAmount);

            // Track dirt by source
            if (!dirtBySource.ContainsKey(terrainTag))
                dirtBySource[terrainTag] = 0f;
            dirtBySource[terrainTag] = Mathf.Clamp01(dirtBySource[terrainTag] + dirtAmount);

            // Create dirt particle for visual effect
            DirtParticle particle = new DirtParticle
            {
                Position = contactPoint,
                Type = dirtType,
                Amount = dirtAmount,
                CreationTime = Time.time
            };

            dirtParticles.Add(particle);

            // Apply visual changes
            UpdateDirtAppearance();
        }

        /// <summary>
        /// Get dirt type from terrain tag.
        /// </summary>
        private DirtType GetDirtType(string terrainTag)
        {
            return terrainTag.ToLower() switch
            {
                "grass" => DirtType.Grass,
                "sand" => DirtType.Sand,
                "mud" => DirtType.Mud,
                "gravel" => DirtType.Gravel,
                _ => DirtType.Road
            };
        }

        /// <summary>
        /// Get color for dirt type.
        /// </summary>
        private Color GetDirtColor(DirtType dirtType)
        {
            return dirtType switch
            {
                DirtType.Road => new Color(0.6f, 0.6f, 0.6f), // Grey dust
                DirtType.Grass => new Color(0.4f, 0.35f, 0.2f), // Brown/green
                DirtType.Sand => new Color(0.75f, 0.7f, 0.5f), // Tan
                DirtType.Mud => new Color(0.3f, 0.25f, 0.15f), // Dark brown
                DirtType.Gravel => new Color(0.65f, 0.65f, 0.65f), // Grey
                _ => Color.black
            };
        }

        /// <summary>
        /// Update vehicle appearance based on dirt level.
        /// </summary>
        private void UpdateDirtAppearance()
        {
            if (bodyRenderers == null || bodyRenderers.Length == 0)
                return;

            foreach (var renderer in bodyRenderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    // Calculate average dirt color from sources
                    Color dirtColor = CalculateAverageDirtColor();

                    // Blend original paint color with dirt color
                    Color originalColor = renderer.material.color;
                    Color dirtedColor = Color.Lerp(originalColor, dirtColor, currentDirtLevel * 0.7f);

                    renderer.material.color = dirtedColor;

                    // Reduce glossiness as dirt builds up
                    if (renderer.material.HasProperty("_Glossiness"))
                    {
                        float baseSmoothness = 0.5f;
                        float dirtySmoothness = Mathf.Lerp(baseSmoothness, 0.2f, currentDirtLevel);
                        renderer.material.SetFloat("_Glossiness", dirtySmoothness);
                    }

                    // Increase roughness
                    if (renderer.material.HasProperty("_Smoothness"))
                    {
                        float baseSmooth = 0.5f;
                        float roughSmooth = Mathf.Lerp(baseSmooth, 0.1f, currentDirtLevel);
                        renderer.material.SetFloat("_Smoothness", roughSmooth);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate average dirt color from all accumulated dirt types.
        /// </summary>
        private Color CalculateAverageDirtColor()
        {
            Color averageColor = Color.black;
            float totalAmount = 0f;

            foreach (var dirtSource in dirtBySource)
            {
                DirtType dirtType = GetDirtType(dirtSource.Key);
                Color dirtColor = GetDirtColor(dirtType);
                float amount = dirtSource.Value;

                averageColor += dirtColor * amount;
                totalAmount += amount;
            }

            if (totalAmount > 0)
                averageColor /= totalAmount;

            return averageColor;
        }

        /// <summary>
        /// Clean the vehicle (wash it).
        /// </summary>
        public void CleanVehicle(float cleanAmount = 1f)
        {
            currentDirtLevel = Mathf.Max(0f, currentDirtLevel - cleanAmount);

            // Clear dirt sources
            if (cleanAmount >= 1f)
            {
                dirtBySource.Clear();
                dirtParticles.Clear();
            }
            else
            {
                // Partial clean - reduce all sources proportionally
                foreach (var key in new List<string>(dirtBySource.Keys))
                {
                    dirtBySource[key] *= (1f - cleanAmount);
                }
            }

            UpdateDirtAppearance();
        }

        /// <summary>
        /// Rain cleans some dirt from the vehicle.
        /// </summary>
        public void RainClean(float rainIntensity = 1f)
        {
            float cleanAmount = rainIntensity * 0.5f;
            CleanVehicle(cleanAmount);
        }

        /// <summary>
        /// Get current dirt level (0-1).
        /// </summary>
        public float GetDirtLevel() => currentDirtLevel;

        /// <summary>
        /// Get dirt composition information.
        /// </summary>
        public string GetDirtInfo()
        {
            string info = $"Dirt Level: {currentDirtLevel * 100:F1}%\n";
            foreach (var source in dirtBySource)
            {
                info += $"{source.Key}: {source.Value * 100:F1}%\n";
            }
            return info;
        }

        /// <summary>
        /// Update dirt accumulation over time.
        /// </summary>
        private void Update()
        {
            // Remove old dirt particles
            for (int i = dirtParticles.Count - 1; i >= 0; i--)
            {
                DirtParticle particle = dirtParticles[i];
                float age = Time.time - particle.CreationTime;

                // Particles fade over time (30 seconds for rain/wind to clean them)
                if (age > 30f)
                {
                    dirtParticles.RemoveAt(i);
                }
            }

            // Gradual settling of dirt (simulation)
            // Some dirt naturally falls off the car over time
            currentDirtLevel *= 0.995f; // Very gradual decay

            // Update appearance
            UpdateDirtAppearance();
        }

        /// <summary>
        /// Get dirt particle count for diagnostics.
        /// </summary>
        public int GetDirtParticleCount() => dirtParticles.Count;
    }
}
