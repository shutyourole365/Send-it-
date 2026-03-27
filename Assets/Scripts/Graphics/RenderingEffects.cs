using UnityEngine;
using SendIt.Data;
using SendIt.Physics;

namespace SendIt.Graphics
{
    /// <summary>
    /// Central manager for all rendering effects (Phase 5 enhanced).
    /// Integrates motion blur, depth of field, particle effects, dynamic lighting, and advanced shadows.
    /// </summary>
    public class RenderingEffects : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Light mainLight;
        [SerializeField] private Rigidbody vehicleRigidbody;

        // Effect systems
        private MotionBlurEffect motionBlurEffect;
        private DepthOfFieldEffect depthOfFieldEffect;
        private ParticleEffectSystem particleEffectSystem;
        private DynamicLightingSystem dynamicLightingSystem;
        private AdvancedShadowSystem advancedShadowSystem;

        // Enable/disable flags
        private bool enableMotionBlur = true;
        private bool enableDepthOfField = false;
        private bool enableParticleEffects = true;
        private bool enableDynamicLighting = true;
        private bool enableAdvancedShadows = true;

        // Settings
        private float motionBlurIntensity = 0.5f;
        private bool autoFocusEnabled = true;
        private float particleDensity = 1f;

        private bool isInitialized;

        /// <summary>
        /// Initialize all rendering effect systems.
        /// </summary>
        public void Initialize(GraphicsData graphicsData, Camera camera, Rigidbody vehicleBody = null)
        {
            mainCamera = camera;
            vehicleRigidbody = vehicleBody;
            if (mainCamera == null)
                mainCamera = Camera.main;

            if (mainLight == null)
                mainLight = FindObjectOfType<Light>();

            motionBlurIntensity = graphicsData.MotionBlurIntensity;

            // Initialize all subsystems
            InitializeMotionBlur();
            InitializeDepthOfField();
            InitializeParticleEffects();
            InitializeDynamicLighting();
            InitializeAdvancedShadows();

            isInitialized = true;
            Debug.Log("RenderingEffects Phase 5 initialized successfully");
        }

        /// <summary>
        /// Initialize motion blur effect.
        /// </summary>
        private void InitializeMotionBlur()
        {
            if (!enableMotionBlur)
                return;

            motionBlurEffect = gameObject.AddComponent<MotionBlurEffect>();
            motionBlurEffect.Initialize(mainCamera, vehicleRigidbody, motionBlurIntensity);
        }

        /// <summary>
        /// Initialize depth of field effect.
        /// </summary>
        private void InitializeDepthOfField()
        {
            if (!enableDepthOfField)
                return;

            depthOfFieldEffect = gameObject.AddComponent<DepthOfFieldEffect>();
            depthOfFieldEffect.Initialize(mainCamera, vehicleRigidbody?.transform, 10f);
            depthOfFieldEffect.SetAutoFocus(autoFocusEnabled);
        }

        /// <summary>
        /// Initialize particle effect system.
        /// </summary>
        private void InitializeParticleEffects()
        {
            if (!enableParticleEffects)
                return;

            particleEffectSystem = gameObject.AddComponent<ParticleEffectSystem>();
            particleEffectSystem.Initialize(vehicleRigidbody);
        }

        /// <summary>
        /// Initialize dynamic lighting system.
        /// </summary>
        private void InitializeDynamicLighting()
        {
            if (!enableDynamicLighting)
                return;

            dynamicLightingSystem = gameObject.AddComponent<DynamicLightingSystem>();
            dynamicLightingSystem.Initialize();
        }

        /// <summary>
        /// Initialize advanced shadow system.
        /// </summary>
        private void InitializeAdvancedShadows()
        {
            if (!enableAdvancedShadows)
                return;

            advancedShadowSystem = gameObject.AddComponent<AdvancedShadowSystem>();
            advancedShadowSystem.Initialize(mainLight, mainCamera);
        }

        /// <summary>
        /// Update all rendering effects each frame.
        /// </summary>
        public void UpdateEffects(float vehicleSpeed, bool braking, float engineTemp, float engineTireTemp)
        {
            if (!isInitialized)
                return;

            if (enableMotionBlur && motionBlurEffect != null)
                motionBlurEffect.UpdateMotionBlur();

            if (enableDepthOfField && depthOfFieldEffect != null)
                depthOfFieldEffect.UpdateDepthOfField();

            if (enableDynamicLighting && dynamicLightingSystem != null)
                dynamicLightingSystem.UpdateLighting(braking, engineTemp, vehicleSpeed);

            if (enableAdvancedShadows && advancedShadowSystem != null)
                advancedShadowSystem.UpdateDynamicShadows(mainCamera.transform.position, vehicleSpeed);
        }

        /// <summary>
        /// Update particle effects based on wheel and vehicle state.
        /// </summary>
        public void UpdateParticleEffects(int wheelIndex, float slipRatio, float slipAngle,
                                         float tireTemp, float speed, bool onWetSurface)
        {
            if (!enableParticleEffects || particleEffectSystem == null)
                return;

            // Update tire smoke
            particleEffectSystem.UpdateTireSmoke(wheelIndex, slipRatio, slipAngle, tireTemp);

            // Update dust on loose surfaces
            bool onLooseSurface = false; // Would come from surface condition check
            particleEffectSystem.UpdateDustEffect(wheelIndex, speed, onLooseSurface, Vector3.zero);

            // Update water spray
            particleEffectSystem.UpdateWaterSpray(speed, onWetSurface, Vector3.zero);
        }

        /// <summary>
        /// Generate spark effects from impact.
        /// </summary>
        public void GenerateImpactSparks(Vector3 impactPoint, Vector3 impactNormal, float impactForce)
        {
            if (!enableParticleEffects || particleEffectSystem == null)
                return;

            particleEffectSystem.GenerateSparks(impactPoint, impactNormal, impactForce);
        }

        // Effect control methods

        public void SetMotionBlurIntensity(float intensity)
        {
            motionBlurIntensity = Mathf.Clamp01(intensity);
            if (motionBlurEffect != null)
                motionBlurEffect.SetBlurIntensity(intensity);
        }

        public void SetDepthOfFieldIntensity(float intensity)
        {
            if (depthOfFieldEffect != null)
                depthOfFieldEffect.SetDOFIntensity(intensity);
        }

        public void SetAutoFocus(bool enabled)
        {
            autoFocusEnabled = enabled;
            if (depthOfFieldEffect != null)
                depthOfFieldEffect.SetAutoFocus(enabled);
        }

        public void SetFocusDistance(float distance)
        {
            if (depthOfFieldEffect != null)
                depthOfFieldEffect.SetFocusDistance(distance);
        }

        public void SetParticleDensity(float density)
        {
            particleDensity = Mathf.Clamp01(density);
            if (particleEffectSystem != null)
            {
                particleEffectSystem.SetSmokeDensity(density);
                particleEffectSystem.SetDustDensity(density);
                particleEffectSystem.SetWaterDensity(density);
            }
        }

        public void SetShadowQuality(AdvancedShadowSystem.ShadowQuality quality)
        {
            if (advancedShadowSystem != null)
                advancedShadowSystem.SetQuality(quality);
        }

        public void SetTimeOfDay(float hour)
        {
            if (dynamicLightingSystem != null)
                dynamicLightingSystem.SetTimeOfDay(hour);
        }

        public void SetHeadlightIntensity(float intensity)
        {
            if (dynamicLightingSystem != null)
                dynamicLightingSystem.SetHeadlightIntensity(intensity);
        }

        // Getters for effect systems

        public MotionBlurEffect GetMotionBlurEffect() => motionBlurEffect;
        public DepthOfFieldEffect GetDepthOfFieldEffect() => depthOfFieldEffect;
        public ParticleEffectSystem GetParticleEffectSystem() => particleEffectSystem;
        public DynamicLightingSystem GetDynamicLightingSystem() => dynamicLightingSystem;
        public AdvancedShadowSystem GetAdvancedShadowSystem() => advancedShadowSystem;

        public bool GetRealTimeShadows() => enableAdvancedShadows;
        public bool GetReflections() => enableAdvancedShadows;
        public bool GetAmbientOcclusion() => enableAdvancedShadows;
        public float GetMotionBlurIntensity() => motionBlurIntensity;

        // Enable/disable individual effects

        public void EnableMotionBlur(bool enabled)
        {
            enableMotionBlur = enabled;
            if (motionBlurEffect != null)
                motionBlurEffect.enabled = enabled;
        }

        public void EnableDepthOfField(bool enabled)
        {
            enableDepthOfField = enabled;
            if (depthOfFieldEffect != null)
                depthOfFieldEffect.enabled = enabled;
        }

        public void EnableParticleEffects(bool enabled)
        {
            enableParticleEffects = enabled;
            if (particleEffectSystem != null)
                particleEffectSystem.enabled = enabled;
        }

        public void EnableDynamicLighting(bool enabled)
        {
            enableDynamicLighting = enabled;
            if (dynamicLightingSystem != null)
                dynamicLightingSystem.enabled = enabled;
        }

        public void EnableAdvancedShadows(bool enabled)
        {
            enableAdvancedShadows = enabled;
            if (advancedShadowSystem != null)
                advancedShadowSystem.enabled = enabled;
        }
    }
}
