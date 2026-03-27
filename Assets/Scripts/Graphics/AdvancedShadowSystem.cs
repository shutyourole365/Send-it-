using UnityEngine;

namespace SendIt.Graphics
{
    /// <summary>
    /// Advanced shadow system with cascaded shadows, contact shadows, and dynamic shadow optimization.
    /// Provides high-quality shadows with performance optimization.
    /// </summary>
    public class AdvancedShadowSystem : MonoBehaviour
    {
        [SerializeField] private Light mainLight;
        [SerializeField] private Camera mainCamera;

        // Shadow settings
        private int shadowResolution = 2048;
        private float shadowDistance = 100f;
        private int shadowCascades = 4;
        private float[] cascadeSplits = new float[3];

        // Contact shadows
        private bool enableContactShadows = true;
        private float contactShadowDistance = 2f;
        private float contactShadowThickness = 0.1f;
        private int contactShadowSamples = 4;

        // Quality levels
        public enum ShadowQuality
        {
            Low,      // 512x512, 1 cascade
            Medium,   // 1024x1024, 2 cascades
            High,     // 2048x2048, 4 cascades
            Ultra     // 4096x4096, 4 cascades with contact shadows
        }
        private ShadowQuality currentQuality = ShadowQuality.High;

        // Shadow bias
        private float shadowBias = 0.05f;
        private float shadowNormalBias = 0.4f;
        private float shadowSlopeBias = 0.1f;

        // Performance optimization
        private bool dynamicShadowDistance = true;
        private float minShadowDistance = 20f;
        private float maxShadowDistance = 200f;

        // Reflection probes
        private bool enableReflectionProbes = true;
        private ReflectionProbe[] reflectionProbes;

        private bool isInitialized;

        public void Initialize(Light light, Camera camera)
        {
            mainLight = light;
            mainCamera = camera;
            SetQuality(ShadowQuality.High);
            isInitialized = true;

            InitializeReflectionProbes();
        }

        /// <summary>
        /// Find or create reflection probes in scene.
        /// </summary>
        private void InitializeReflectionProbes()
        {
            reflectionProbes = FindObjectsOfType<ReflectionProbe>();
        }

        /// <summary>
        /// Set shadow quality level.
        /// </summary>
        public void SetQuality(ShadowQuality quality)
        {
            currentQuality = quality;

            switch (quality)
            {
                case ShadowQuality.Low:
                    shadowResolution = 512;
                    shadowDistance = 30f;
                    shadowCascades = 1;
                    enableContactShadows = false;
                    break;

                case ShadowQuality.Medium:
                    shadowResolution = 1024;
                    shadowDistance = 60f;
                    shadowCascades = 2;
                    enableContactShadows = false;
                    break;

                case ShadowQuality.High:
                    shadowResolution = 2048;
                    shadowDistance = 100f;
                    shadowCascades = 4;
                    enableContactShadows = true;
                    break;

                case ShadowQuality.Ultra:
                    shadowResolution = 4096;
                    shadowDistance = 200f;
                    shadowCascades = 4;
                    enableContactShadows = true;
                    contactShadowSamples = 8;
                    break;
            }

            ApplyShadowSettings();
        }

        /// <summary>
        /// Apply shadow settings to graphics.
        /// </summary>
        private void ApplyShadowSettings()
        {
            if (mainLight == null)
                return;

            // Set shadow resolution
            QualitySettings.shadowResolution = (ShadowResolution)shadowResolution;

            // Set shadow distance
            QualitySettings.shadowDistance = shadowDistance;

            // Configure cascaded shadows
            QualitySettings.shadowCascades = shadowCascades;
            if (shadowCascades >= 2)
                cascadeSplits[0] = 0.1f;
            if (shadowCascades >= 3)
                cascadeSplits[1] = 0.3f;
            if (shadowCascades >= 4)
                cascadeSplits[2] = 0.7f;

            // Apply cascade splits
            mainLight.shadowCascadeMultiplier = 1.5f;

            // Set shadow bias values
            mainLight.shadowBias = shadowBias;
            mainLight.shadowNormalBias = shadowNormalBias;

            // Enable soft shadows
            mainLight.shadows = LightShadows.Soft;
        }

        /// <summary>
        /// Update shadows based on camera position for dynamic optimization.
        /// </summary>
        public void UpdateDynamicShadows(Vector3 cameraPosition, float vehicleSpeed)
        {
            if (!isInitialized || !dynamicShadowDistance)
                return;

            // Adjust shadow distance based on speed
            // Higher speeds: further shadows for LOD awareness
            float speedFactor = Mathf.Clamp01(vehicleSpeed / 50f);
            float targetDistance = Mathf.Lerp(minShadowDistance, maxShadowDistance, speedFactor);

            shadowDistance = Mathf.Lerp(shadowDistance, targetDistance, Time.deltaTime * 0.5f);
            QualitySettings.shadowDistance = shadowDistance;
        }

        /// <summary>
        /// Update reflection probes for dynamic reflections.
        /// </summary>
        public void UpdateReflectionProbes()
        {
            if (!enableReflectionProbes || reflectionProbes == null)
                return;

            foreach (ReflectionProbe probe in reflectionProbes)
            {
                if (probe != null)
                {
                    // Update probes gradually for performance
                    probe.importance = 1;
                    probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
                    probe.RenderProbe();
                }
            }
        }

        /// <summary>
        /// Calculate contact shadow at a position.
        /// Contact shadows are dark regions where objects touch ground.
        /// </summary>
        public float GetContactShadowIntensity(Vector3 position, Vector3 normal)
        {
            if (!enableContactShadows)
                return 0f;

            // Cast ray downward to detect ground proximity
            RaycastHit hit;
            if (Physics.Raycast(position, Vector3.down, out hit, contactShadowDistance))
            {
                float distanceToGround = hit.distance;
                float shadowFade = 1f - (distanceToGround / contactShadowDistance);

                // Contact shadow is stronger for flat surfaces facing down
                float normalFactor = Vector3.Dot(normal, Vector3.up);
                normalFactor = Mathf.Max(normalFactor, 0f);

                return shadowFade * normalFactor * contactShadowThickness;
            }

            return 0f;
        }

        /// <summary>
        /// Get shadow fade factor at distance (for LOD).
        /// </summary>
        public float GetShadowFadeFactor(float distance)
        {
            float fadeStart = shadowDistance * 0.8f;
            float fadeEnd = shadowDistance;

            if (distance < fadeStart)
                return 1f;

            if (distance > fadeEnd)
                return 0f;

            return 1f - ((distance - fadeStart) / (fadeEnd - fadeStart));
        }

        /// <summary>
        /// Set shadow bias (affects shadow acne vs peter panning).
        /// </summary>
        public void SetShadowBias(float bias)
        {
            shadowBias = Mathf.Clamp(bias, 0f, 0.2f);
            if (mainLight != null)
                mainLight.shadowBias = shadowBias;
        }

        /// <summary>
        /// Set shadow normal bias.
        /// </summary>
        public void SetShadowNormalBias(float bias)
        {
            shadowNormalBias = Mathf.Clamp(bias, 0f, 1f);
            if (mainLight != null)
                mainLight.shadowNormalBias = shadowNormalBias;
        }

        /// <summary>
        /// Enable/disable dynamic shadow distance optimization.
        /// </summary>
        public void SetDynamicShadowDistance(bool enabled)
        {
            dynamicShadowDistance = enabled;
        }

        /// <summary>
        /// Set contact shadow parameters.
        /// </summary>
        public void SetContactShadows(bool enabled, float distance = 2f)
        {
            enableContactShadows = enabled;
            contactShadowDistance = Mathf.Max(distance, 0.1f);
        }

        /// <summary>
        /// Enable/disable reflection probes.
        /// </summary>
        public void SetReflectionProbes(bool enabled)
        {
            enableReflectionProbes = enabled;

            if (reflectionProbes != null)
            {
                foreach (ReflectionProbe probe in reflectionProbes)
                {
                    if (probe != null)
                    {
                        probe.enabled = enabled;
                    }
                }
            }
        }

        /// <summary>
        /// Get shadow quality level.
        /// </summary>
        public ShadowQuality GetQuality() => currentQuality;

        /// <summary>
        /// Get shadow resolution.
        /// </summary>
        public int GetShadowResolution() => shadowResolution;

        /// <summary>
        /// Get shadow distance.
        /// </summary>
        public float GetShadowDistance() => shadowDistance;

        /// <summary>
        /// Get cascade count.
        /// </summary>
        public int GetCascadeCount() => shadowCascades;

        /// <summary>
        /// Get contact shadows enabled state.
        /// </summary>
        public bool GetContactShadowsEnabled() => enableContactShadows;

        public ShadowQuality GetCurrentQuality() => currentQuality;
    }
}
