using UnityEngine;
using SendIt.Data;
using SendIt.Tuning;

namespace SendIt.Graphics
{
    /// <summary>
    /// Central manager for all vehicle graphics customization.
    /// Orchestrates paint system, body modifications, materials, and rendering effects.
    /// </summary>
    public class VehicleVisuals : MonoBehaviour
    {
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private Camera previewCamera;

        private TuningManager tuningManager;
        private GraphicsData graphicsData;

        // Graphics subsystems
        private PaintSystem paintSystem;
        private BodyModifier bodyModifier;
        private MaterialCustomizer materialCustomizer;
        private RenderingEffects renderingEffects;

        private bool isInitialized;

        private void OnEnable()
        {
            if (tuningManager != null)
            {
                tuningManager.OnGraphicsParameterChanged += HandleGraphicsParameterChanged;
            }
        }

        private void OnDisable()
        {
            if (tuningManager != null)
            {
                tuningManager.OnGraphicsParameterChanged -= HandleGraphicsParameterChanged;
            }
        }

        /// <summary>
        /// Initialize all graphics systems.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            // Get tuning manager
            tuningManager = TuningManager.Instance;
            if (tuningManager == null)
            {
                Debug.LogError("TuningManager not found!");
                return;
            }

            graphicsData = tuningManager.GetVehicleData().Graphics;

            // Get body renderers if not assigned
            if (bodyRenderers == null || bodyRenderers.Length == 0)
            {
                bodyRenderers = GetComponentsInChildren<Renderer>();
            }

            // Initialize paint system
            paintSystem = gameObject.AddComponent<PaintSystem>();
            paintSystem.Initialize(graphicsData, bodyRenderers);

            // Initialize body modifier
            bodyModifier = gameObject.AddComponent<BodyModifier>();
            bodyModifier.Initialize(graphicsData);

            // Initialize material customizer
            materialCustomizer = gameObject.AddComponent<MaterialCustomizer>();
            materialCustomizer.Initialize(graphicsData, bodyRenderers[0].materials);

            // Initialize rendering effects
            if (previewCamera == null)
            {
                previewCamera = Camera.main;
            }
            renderingEffects = gameObject.AddComponent<RenderingEffects>();
            renderingEffects.Initialize(graphicsData, previewCamera);

            isInitialized = true;
            Debug.Log("VehicleVisuals initialized successfully");
        }

        /// <summary>
        /// Handle graphics parameter changes from tuning.
        /// </summary>
        private void HandleGraphicsParameterChanged(string paramName, float newValue)
        {
            if (!isInitialized)
                return;

            switch (paramName)
            {
                // Paint parameters
                case "MetallicIntensity":
                    if (paintSystem != null)
                        paintSystem.SetMetallicIntensity(newValue);
                    break;

                case "Glossiness":
                    if (paintSystem != null)
                        paintSystem.SetGlossiness(newValue);
                    break;

                case "PearlcentIntensity":
                    if (paintSystem != null)
                        paintSystem.SetPearlcentIntensity(newValue);
                    break;

                // Body modifications
                case "WheelSize":
                    if (bodyModifier != null)
                        bodyModifier.SetWheelSize((int)newValue);
                    break;

                case "WheelOffset":
                    if (bodyModifier != null)
                        bodyModifier.SetWheelOffset(newValue);
                    break;

                case "BumperStyle":
                    if (bodyModifier != null)
                        bodyModifier.SetBumperStyle((int)newValue);
                    break;

                case "BodyKitStyle":
                    if (bodyModifier != null)
                        bodyModifier.SetBodyKitStyle((int)newValue);
                    break;

                case "SpoilerHeight":
                    if (bodyModifier != null)
                        bodyModifier.SetSpoilerHeight(newValue);
                    break;

                case "SpoilerAngle":
                    if (bodyModifier != null)
                        bodyModifier.SetSpoilerAngle(newValue);
                    break;

                // Materials and wear
                case "WearAmount":
                    if (materialCustomizer != null)
                        materialCustomizer.SetWearAmount(newValue);
                    break;

                case "DirtAccumulation":
                    if (materialCustomizer != null)
                        materialCustomizer.SetDirtAccumulation(newValue);
                    break;

                case "RustAmount":
                    if (materialCustomizer != null)
                        materialCustomizer.SetRustAmount(newValue);
                    break;

                // Rendering effects
                case "MotionBlurIntensity":
                    if (renderingEffects != null)
                        renderingEffects.SetMotionBlurIntensity(newValue);
                    break;
            }
        }

        /// <summary>
        /// Set base paint color (called from UI).
        /// </summary>
        public void SetPaintColor(Color color)
        {
            if (paintSystem != null)
            {
                paintSystem.SetBaseColor(color);
            }
        }

        /// <summary>
        /// Apply a paint color preset.
        /// </summary>
        public void ApplyPaintPreset(string presetName)
        {
            if (paintSystem != null)
            {
                paintSystem.ApplyPaintPreset(presetName);
            }
        }

        /// <summary>
        /// Clean the vehicle (reduce dirt).
        /// </summary>
        public void CleanVehicle()
        {
            if (materialCustomizer != null)
            {
                materialCustomizer.Clean();
            }
        }

        /// <summary>
        /// Simulate wear from driving (called periodically during gameplay).
        /// </summary>
        public void SimulateWear(float deltaTime, float speed)
        {
            if (materialCustomizer != null)
            {
                materialCustomizer.AccumulateWear(deltaTime, 0.01f);
                materialCustomizer.AccumulateDirt(deltaTime, speed, 0.05f);
            }
        }

        // Getters for all subsystems
        public PaintSystem GetPaintSystem() => paintSystem;
        public BodyModifier GetBodyModifier() => bodyModifier;
        public MaterialCustomizer GetMaterialCustomizer() => materialCustomizer;
        public RenderingEffects GetRenderingEffects() => renderingEffects;

        private void Start()
        {
            Initialize();
        }
    }
}
