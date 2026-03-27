using UnityEngine;
using UnityEngine.UI;
using SendIt.Graphics;
using SendIt.Data;

namespace SendIt.UI
{
    /// <summary>
    /// Comprehensive graphics customization UI for Phase 4.
    /// Manages paint, body modifications, materials, and visual effects.
    /// </summary>
    public class GraphicsCustomizationUI : MonoBehaviour
    {
        [SerializeField] private VehicleVisuals vehicleVisuals;

        // Paint UI
        [SerializeField] private Button paintColorButton;
        [SerializeField] private Slider metallicIntensitySlider;
        [SerializeField] private Slider glossinessSlider;
        [SerializeField] private Slider pearlcentIntensitySlider;
        [SerializeField] private Dropdown paintPresetsDropdown;
        [SerializeField] private Text currentColorLabel;

        // Body Modification UI
        [SerializeField] private Slider wheelSizeSlider;
        [SerializeField] private Slider wheelOffsetSlider;
        [SerializeField] private Dropdown bumperStyleDropdown;
        [SerializeField] private Dropdown bodyKitDropdown;
        [SerializeField] private Slider spoilerHeightSlider;
        [SerializeField] private Slider spoilerAngleSlider;

        // Material UI
        [SerializeField] private Slider wearAmountSlider;
        [SerializeField] private Slider dirtAccumulationSlider;
        [SerializeField] private Slider rustAmountSlider;
        [SerializeField] private Button cleanVehicleButton;

        // Effects UI
        [SerializeField] private Toggle enableMotionBlurToggle;
        [SerializeField] private Slider motionBlurIntensitySlider;
        [SerializeField] private Toggle enableDepthOfFieldToggle;
        [SerializeField] private Slider depthOfFieldIntensitySlider;
        [SerializeField] private Slider particleDensitySlider;

        // Lighting UI
        [SerializeField] private Slider timeOfDaySlider;
        [SerializeField] private Text timeOfDayLabel;
        [SerializeField] private Slider headlightIntensitySlider;
        [SerializeField] private Toggle enableDynamicLightingToggle;

        // Shadow UI
        [SerializeField] private Dropdown shadowQualityDropdown;

        // Current color
        private Color currentPaintColor = Color.red;
        private bool isInitialized;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (vehicleVisuals == null)
                vehicleVisuals = FindObjectOfType<VehicleVisuals>();

            SetupPaintUI();
            SetupBodyModUI();
            SetupMaterialUI();
            SetupEffectsUI();
            SetupLightingUI();
            SetupShadowUI();

            isInitialized = true;
        }

        /// <summary>
        /// Setup paint customization UI.
        /// </summary>
        private void SetupPaintUI()
        {
            if (paintColorButton != null)
            {
                paintColorButton.GetComponent<Image>().color = currentPaintColor;
                paintColorButton.onClick.AddListener(() => OpenColorPicker());
            }

            if (metallicIntensitySlider != null)
            {
                metallicIntensitySlider.minValue = 0f;
                metallicIntensitySlider.maxValue = 1f;
                metallicIntensitySlider.onValueChanged.AddListener(OnMetallicIntensityChanged);
            }

            if (glossinessSlider != null)
            {
                glossinessSlider.minValue = 0f;
                glossinessSlider.maxValue = 1f;
                glossinessSlider.onValueChanged.AddListener(OnGlossinessChanged);
            }

            if (pearlcentIntensitySlider != null)
            {
                pearlcentIntensitySlider.minValue = 0f;
                pearlcentIntensitySlider.maxValue = 1f;
                pearlcentIntensitySlider.onValueChanged.AddListener(OnPearlcentIntensityChanged);
            }

            if (paintPresetsDropdown != null)
            {
                paintPresetsDropdown.onValueChanged.AddListener(OnPaintPresetSelected);
                PopulatePaintPresets();
            }
        }

        /// <summary>
        /// Setup body modification UI.
        /// </summary>
        private void SetupBodyModUI()
        {
            if (wheelSizeSlider != null)
            {
                wheelSizeSlider.minValue = 15f;
                wheelSizeSlider.maxValue = 21f;
                wheelSizeSlider.wholeNumbers = true;
                wheelSizeSlider.onValueChanged.AddListener(OnWheelSizeChanged);
            }

            if (wheelOffsetSlider != null)
            {
                wheelOffsetSlider.minValue = -2f;
                wheelOffsetSlider.maxValue = 2f;
                wheelOffsetSlider.onValueChanged.AddListener(OnWheelOffsetChanged);
            }

            if (bumperStyleDropdown != null)
            {
                bumperStyleDropdown.onValueChanged.AddListener(OnBumperStyleChanged);
                PopulateBumperStyles();
            }

            if (bodyKitDropdown != null)
            {
                bodyKitDropdown.onValueChanged.AddListener(OnBodyKitChanged);
                PopulateBodyKits();
            }

            if (spoilerHeightSlider != null)
            {
                spoilerHeightSlider.minValue = 0f;
                spoilerHeightSlider.maxValue = 1f;
                spoilerHeightSlider.onValueChanged.AddListener(OnSpoilerHeightChanged);
            }

            if (spoilerAngleSlider != null)
            {
                spoilerAngleSlider.minValue = -30f;
                spoilerAngleSlider.maxValue = 30f;
                spoilerAngleSlider.onValueChanged.AddListener(OnSpoilerAngleChanged);
            }
        }

        /// <summary>
        /// Setup material customization UI.
        /// </summary>
        private void SetupMaterialUI()
        {
            if (wearAmountSlider != null)
            {
                wearAmountSlider.minValue = 0f;
                wearAmountSlider.maxValue = 1f;
                wearAmountSlider.onValueChanged.AddListener(OnWearAmountChanged);
            }

            if (dirtAccumulationSlider != null)
            {
                dirtAccumulationSlider.minValue = 0f;
                dirtAccumulationSlider.maxValue = 1f;
                dirtAccumulationSlider.onValueChanged.AddListener(OnDirtAccumulationChanged);
            }

            if (rustAmountSlider != null)
            {
                rustAmountSlider.minValue = 0f;
                rustAmountSlider.maxValue = 1f;
                rustAmountSlider.onValueChanged.AddListener(OnRustAmountChanged);
            }

            if (cleanVehicleButton != null)
            {
                cleanVehicleButton.onClick.AddListener(OnCleanVehicle);
            }
        }

        /// <summary>
        /// Setup effects UI.
        /// </summary>
        private void SetupEffectsUI()
        {
            if (enableMotionBlurToggle != null)
            {
                enableMotionBlurToggle.onValueChanged.AddListener(OnMotionBlurToggled);
            }

            if (motionBlurIntensitySlider != null)
            {
                motionBlurIntensitySlider.minValue = 0f;
                motionBlurIntensitySlider.maxValue = 1f;
                motionBlurIntensitySlider.onValueChanged.AddListener(OnMotionBlurIntensityChanged);
            }

            if (enableDepthOfFieldToggle != null)
            {
                enableDepthOfFieldToggle.onValueChanged.AddListener(OnDepthOfFieldToggled);
            }

            if (depthOfFieldIntensitySlider != null)
            {
                depthOfFieldIntensitySlider.minValue = 0f;
                depthOfFieldIntensitySlider.maxValue = 1f;
                depthOfFieldIntensitySlider.onValueChanged.AddListener(OnDepthOfFieldIntensityChanged);
            }

            if (particleDensitySlider != null)
            {
                particleDensitySlider.minValue = 0f;
                particleDensitySlider.maxValue = 1f;
                particleDensitySlider.value = 0.7f;
                particleDensitySlider.onValueChanged.AddListener(OnParticleDensityChanged);
            }
        }

        /// <summary>
        /// Setup lighting UI.
        /// </summary>
        private void SetupLightingUI()
        {
            if (timeOfDaySlider != null)
            {
                timeOfDaySlider.minValue = 0f;
                timeOfDaySlider.maxValue = 24f;
                timeOfDaySlider.value = 12f;
                timeOfDaySlider.onValueChanged.AddListener(OnTimeOfDayChanged);
            }

            if (headlightIntensitySlider != null)
            {
                headlightIntensitySlider.minValue = 0f;
                headlightIntensitySlider.maxValue = 2f;
                headlightIntensitySlider.value = 1.5f;
                headlightIntensitySlider.onValueChanged.AddListener(OnHeadlightIntensityChanged);
            }

            if (enableDynamicLightingToggle != null)
            {
                enableDynamicLightingToggle.onValueChanged.AddListener(OnDynamicLightingToggled);
            }
        }

        /// <summary>
        /// Setup shadow UI.
        /// </summary>
        private void SetupShadowUI()
        {
            if (shadowQualityDropdown != null)
            {
                shadowQualityDropdown.onValueChanged.AddListener(OnShadowQualityChanged);
                PopulateShadowQualities();
            }
        }

        // Paint callbacks
        private void OpenColorPicker()
        {
            // In a full implementation, open color picker dialog
            Debug.Log("Color picker opened");
        }

        private void OnMetallicIntensityChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetPaintSystem()?.SetMetallicIntensity(value);
        }

        private void OnGlossinessChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetPaintSystem()?.SetGlossiness(value);
        }

        private void OnPearlcentIntensityChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetPaintSystem()?.SetPearlcentIntensity(value);
        }

        private void OnPaintPresetSelected(int index)
        {
            string[] presets = { "Racing Red", "Pearl White", "Matte Black", "Electric Blue" };
            if (index < presets.Length && vehicleVisuals != null)
            {
                vehicleVisuals.ApplyPaintPreset(presets[index]);
            }
        }

        // Body modification callbacks
        private void OnWheelSizeChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetBodyModifier()?.SetWheelSize((int)value);
        }

        private void OnWheelOffsetChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetBodyModifier()?.SetWheelOffset(value);
        }

        private void OnBumperStyleChanged(int index)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetBodyModifier()?.SetBumperStyle(index);
        }

        private void OnBodyKitChanged(int index)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetBodyModifier()?.SetBodyKitStyle(index);
        }

        private void OnSpoilerHeightChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetBodyModifier()?.SetSpoilerHeight(value);
        }

        private void OnSpoilerAngleChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetBodyModifier()?.SetSpoilerAngle(value);
        }

        // Material callbacks
        private void OnWearAmountChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetMaterialCustomizer()?.SetWearAmount(value);
        }

        private void OnDirtAccumulationChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetMaterialCustomizer()?.SetDirtAccumulation(value);
        }

        private void OnRustAmountChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetMaterialCustomizer()?.SetRustAmount(value);
        }

        private void OnCleanVehicle()
        {
            if (vehicleVisuals != null)
                vehicleVisuals.CleanVehicle();

            if (dirtAccumulationSlider != null)
                dirtAccumulationSlider.value = 0f;
        }

        // Effects callbacks
        private void OnMotionBlurToggled(bool enabled)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetRenderingEffects()?.EnableMotionBlur(enabled);
        }

        private void OnMotionBlurIntensityChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetRenderingEffects()?.SetMotionBlurIntensity(value);
        }

        private void OnDepthOfFieldToggled(bool enabled)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetRenderingEffects()?.EnableDepthOfField(enabled);
        }

        private void OnDepthOfFieldIntensityChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetRenderingEffects()?.SetDepthOfFieldIntensity(value);
        }

        private void OnParticleDensityChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetRenderingEffects()?.SetParticleDensity(value);
        }

        // Lighting callbacks
        private void OnTimeOfDayChanged(float hour)
        {
            if (timeOfDayLabel != null)
            {
                int hours = (int)hour;
                int minutes = (int)((hour - hours) * 60f);
                timeOfDayLabel.text = $"{hours:D2}:{minutes:D2}";
            }

            if (vehicleVisuals != null)
                vehicleVisuals.GetRenderingEffects()?.SetTimeOfDay(hour);
        }

        private void OnHeadlightIntensityChanged(float value)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetRenderingEffects()?.SetHeadlightIntensity(value);
        }

        private void OnDynamicLightingToggled(bool enabled)
        {
            if (vehicleVisuals != null)
                vehicleVisuals.GetRenderingEffects()?.EnableDynamicLighting(enabled);
        }

        // Shadow callbacks
        private void OnShadowQualityChanged(int index)
        {
            var quality = (AdvancedShadowSystem.ShadowQuality)index;
            if (vehicleVisuals != null)
                vehicleVisuals.GetRenderingEffects()?.SetShadowQuality(quality);
        }

        // Populate dropdowns
        private void PopulatePaintPresets()
        {
            if (paintPresetsDropdown == null) return;
            paintPresetsDropdown.ClearOptions();
            paintPresetsDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Racing Red", "Pearl White", "Matte Black", "Electric Blue", "Sunset Orange"
            });
        }

        private void PopulateBumperStyles()
        {
            if (bumperStyleDropdown == null) return;
            bumperStyleDropdown.ClearOptions();
            bumperStyleDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Stock", "Aggressive", "Sport", "Tuned"
            });
        }

        private void PopulateBodyKits()
        {
            if (bodyKitDropdown == null) return;
            bodyKitDropdown.ClearOptions();
            bodyKitDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "None", "Street", "Sport", "Racing", "Custom"
            });
        }

        private void PopulateShadowQualities()
        {
            if (shadowQualityDropdown == null) return;
            shadowQualityDropdown.ClearOptions();
            shadowQualityDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Low", "Medium", "High", "Ultra"
            });
        }

        public bool IsInitialized => isInitialized;
    }
}
