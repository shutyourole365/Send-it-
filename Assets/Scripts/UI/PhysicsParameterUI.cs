using UnityEngine;
using UnityEngine.UI;
using SendIt.Tuning;

namespace SendIt.UI
{
    /// <summary>
    /// UI control for adjusting a single physics parameter.
    /// Includes slider, input field, and real-time value display.
    /// </summary>
    public class PhysicsParameterUI : MonoBehaviour
    {
        [SerializeField] private Text parameterNameLabel;
        [SerializeField] private Slider parameterSlider;
        [SerializeField] private InputField parameterInput;
        [SerializeField] private Text valueDisplayLabel;
        [SerializeField] private Text unitLabel;
        [SerializeField] private Button resetButton;

        private TuneParameter tuneParameter;
        private TuningManager tuningManager;
        private string parameterName;

        public void Initialize(TuneParameter parameter, TuningManager manager)
        {
            tuneParameter = parameter;
            tuningManager = manager;
            parameterName = parameter.ParameterName;

            // Setup UI elements
            if (parameterNameLabel != null)
            {
                parameterNameLabel.text = parameterName;
            }

            // Setup slider
            if (parameterSlider != null)
            {
                parameterSlider.minValue = 0f;
                parameterSlider.maxValue = 1f;
                parameterSlider.value = tuneParameter.GetNormalizedValue();
                parameterSlider.onValueChanged.AddListener(OnSliderChanged);
            }

            // Setup input field
            if (parameterInput != null)
            {
                parameterInput.text = tuneParameter.CurrentValue.ToString("F2");
                parameterInput.onEndEdit.AddListener(OnInputChanged);
            }

            // Setup reset button
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetToDefault);
            }

            // Subscribe to parameter changes
            tuneParameter.OnValueChanged += OnParameterValueChanged;

            RefreshDisplay();
        }

        /// <summary>
        /// Handle slider value changes.
        /// </summary>
        private void OnSliderChanged(float normalizedValue)
        {
            if (tuneParameter == null || tuningManager == null)
                return;

            tuneParameter.SetNormalizedValue(normalizedValue);
            tuningManager.SetPhysicsParameter(parameterName, tuneParameter.CurrentValue);
            RefreshDisplay();
        }

        /// <summary>
        /// Handle direct input field changes.
        /// </summary>
        private void OnInputChanged(string valueString)
        {
            if (tuneParameter == null || tuningManager == null)
                return;

            if (float.TryParse(valueString, out float newValue))
            {
                tuneParameter.SetValue(newValue);
                tuningManager.SetPhysicsParameter(parameterName, newValue);
                RefreshDisplay();
            }
            else
            {
                // Revert to current value if parse fails
                parameterInput.text = tuneParameter.CurrentValue.ToString("F2");
            }
        }

        /// <summary>
        /// Handle parameter value changes from other sources (e.g., preset loading).
        /// </summary>
        private void OnParameterValueChanged(float newValue)
        {
            RefreshDisplay();
        }

        /// <summary>
        /// Refresh the UI display to match current parameter value.
        /// </summary>
        public void RefreshDisplay()
        {
            if (tuneParameter == null)
                return;

            // Update slider
            if (parameterSlider != null)
            {
                parameterSlider.value = tuneParameter.GetNormalizedValue();
            }

            // Update input field
            if (parameterInput != null)
            {
                parameterInput.text = tuneParameter.CurrentValue.ToString("F2");
            }

            // Update value display with proper formatting
            if (valueDisplayLabel != null)
            {
                valueDisplayLabel.text = FormatParameterValue(tuneParameter.CurrentValue, parameterName);
            }

            // Update unit label
            if (unitLabel != null)
            {
                unitLabel.text = GetParameterUnit(parameterName);
            }
        }

        /// <summary>
        /// Reset parameter to default value.
        /// </summary>
        public void ResetToDefault()
        {
            if (tuneParameter == null || tuningManager == null)
                return;

            tuneParameter.ResetToDefault();
            tuningManager.SetPhysicsParameter(parameterName, tuneParameter.DefaultValue);
            RefreshDisplay();
        }

        /// <summary>
        /// Format parameter value with appropriate precision.
        /// </summary>
        private string FormatParameterValue(float value, string paramName)
        {
            // Determine decimal places based on parameter
            int decimalPlaces = paramName switch
            {
                "MaxRPM" => 0,
                "HorsePower" => 0,
                "GearCount" => 0,
                "WheelSize" => 0,
                "BumperStyle" => 0,
                "BodyKitStyle" => 0,
                "SpringStiffness" => 0,
                "AntiRollBarStiffness" => 0,
                _ => 2
            };

            return value.ToString($"F{decimalPlaces}");
        }

        /// <summary>
        /// Get the unit label for a parameter.
        /// </summary>
        private string GetParameterUnit(string paramName)
        {
            return paramName switch
            {
                "MaxRPM" => "RPM",
                "HorsePower" => "HP",
                "TorquePeakRPM" => "RPM",
                "ShiftSpeed" => "sec",
                "SpringStiffness" => "N/m",
                "CompressionDamping" => "Ns/m",
                "ExtensionDamping" => "Ns/m",
                "RideHeight" => "m",
                "AntiRollBarStiffness" => "Nm/deg",
                "TireGripCoefficient" => "μ",
                "TirePeakSlip" => "rad",
                "DragCoefficient" => "Cd",
                "DownforceCoefficient" => "kg·f",
                "SpoilerAngle" => "°",
                "TotalMass" => "kg",
                "FrontWeightDistribution" => "%",
                "MetallicIntensity" => "0-1",
                "Glossiness" => "0-1",
                "WheelSize" => "in",
                "WheelOffset" => "mm",
                "SpoilerHeight" => "mm",
                "WearAmount" => "%",
                "MotionBlurIntensity" => "0-1",
                _ => ""
            };
        }

        private void OnDestroy()
        {
            if (tuneParameter != null)
            {
                tuneParameter.OnValueChanged -= OnParameterValueChanged;
            }
        }
    }
}
