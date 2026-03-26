using UnityEngine;
using UnityEngine.UI;
using SendIt.Tuning;

namespace SendIt.UI
{
    /// <summary>
    /// UI control for graphics parameters with color pickers and toggles.
    /// Handles paint colors, material properties, and effect toggles.
    /// </summary>
    public class GraphicsParameterUI : MonoBehaviour
    {
        [SerializeField] private Text parameterNameLabel;
        [SerializeField] private Slider parameterSlider;
        [SerializeField] private InputField parameterInput;
        [SerializeField] private Image colorDisplay;
        [SerializeField] private Button colorPickerButton;
        [SerializeField] private Toggle effectToggle;

        private TuneParameter tuneParameter;
        private TuningManager tuningManager;
        private string parameterName;
        private bool isColorParameter;

        public void InitializeColorParameter(string name, Color initialColor, TuningManager manager)
        {
            parameterName = name;
            tuningManager = manager;
            isColorParameter = true;

            // Setup UI for color parameter
            if (parameterNameLabel != null)
                parameterNameLabel.text = name;

            if (colorDisplay != null)
                colorDisplay.color = initialColor;

            if (colorPickerButton != null)
                colorPickerButton.onClick.AddListener(OpenColorPicker);
        }

        public void InitializeSliderParameter(TuneParameter parameter, TuningManager manager)
        {
            tuneParameter = parameter;
            tuningManager = manager;
            parameterName = parameter.ParameterName;
            isColorParameter = false;

            // Setup UI for slider parameter
            if (parameterNameLabel != null)
                parameterNameLabel.text = parameterName;

            if (parameterSlider != null)
            {
                parameterSlider.minValue = 0f;
                parameterSlider.maxValue = 1f;
                parameterSlider.value = tuneParameter.GetNormalizedValue();
                parameterSlider.onValueChanged.AddListener(OnSliderChanged);
            }

            if (parameterInput != null)
            {
                parameterInput.text = tuneParameter.CurrentValue.ToString("F2");
                parameterInput.onEndEdit.AddListener(OnInputChanged);
            }

            tuneParameter.OnValueChanged += OnParameterValueChanged;
        }

        public void InitializeToggleParameter(string name, bool initialValue, TuningManager manager)
        {
            parameterName = name;
            tuningManager = manager;

            // Setup UI for toggle parameter
            if (parameterNameLabel != null)
                parameterNameLabel.text = name;

            if (effectToggle != null)
            {
                effectToggle.isOn = initialValue;
                effectToggle.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        /// <summary>
        /// Open color picker dialog.
        /// </summary>
        private void OpenColorPicker()
        {
            // In a full implementation, show a color picker UI
            // For now, this is a placeholder
            Debug.Log($"Color picker for {parameterName}");
        }

        /// <summary>
        /// Handle slider value changes.
        /// </summary>
        private void OnSliderChanged(float normalizedValue)
        {
            if (tuneParameter == null || tuningManager == null)
                return;

            tuneParameter.SetNormalizedValue(normalizedValue);
            tuningManager.SetGraphicsParameter(parameterName, tuneParameter.CurrentValue);
            RefreshDisplay();
        }

        /// <summary>
        /// Handle input field changes.
        /// </summary>
        private void OnInputChanged(string valueString)
        {
            if (tuneParameter == null || tuningManager == null)
                return;

            if (float.TryParse(valueString, out float newValue))
            {
                tuneParameter.SetValue(newValue);
                tuningManager.SetGraphicsParameter(parameterName, newValue);
                RefreshDisplay();
            }
            else
            {
                parameterInput.text = tuneParameter.CurrentValue.ToString("F2");
            }
        }

        /// <summary>
        /// Handle toggle changes.
        /// </summary>
        private void OnToggleChanged(bool isOn)
        {
            if (tuningManager == null)
                return;

            // Convert bool to float for parameter system
            tuningManager.SetGraphicsParameter(parameterName, isOn ? 1f : 0f);
        }

        /// <summary>
        /// Handle parameter value changes from other sources.
        /// </summary>
        private void OnParameterValueChanged(float newValue)
        {
            RefreshDisplay();
        }

        /// <summary>
        /// Refresh the UI display.
        /// </summary>
        public void RefreshDisplay()
        {
            if (tuneParameter == null)
                return;

            if (parameterSlider != null)
                parameterSlider.value = tuneParameter.GetNormalizedValue();

            if (parameterInput != null)
                parameterInput.text = tuneParameter.CurrentValue.ToString("F2");
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
