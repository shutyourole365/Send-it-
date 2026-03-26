using UnityEngine;
using System;

namespace SendIt.Tuning
{
    /// <summary>
    /// Base class for all tunable vehicle parameters.
    /// Supports constraints, change callbacks, and real-time updates.
    /// </summary>
    [System.Serializable]
    public class TuneParameter
    {
        [SerializeField] private string parameterName;
        [SerializeField] private float currentValue;
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue;
        [SerializeField] private float defaultValue;
        [SerializeField] private string category;
        [SerializeField] private string description;

        // Event for when value changes
        public event Action<float> OnValueChanged;
        public event Action OnParameterUpdated;

        public TuneParameter(string name, float defaultVal, float min, float max, string cat = "General", string desc = "")
        {
            parameterName = name;
            defaultValue = defaultVal;
            currentValue = defaultVal;
            minValue = min;
            maxValue = max;
            category = cat;
            description = desc;
        }

        /// <summary>
        /// Set the parameter value with bounds checking and callbacks.
        /// </summary>
        public void SetValue(float value)
        {
            float clampedValue = Mathf.Clamp(value, minValue, maxValue);

            if (!Mathf.Approximately(clampedValue, currentValue))
            {
                currentValue = clampedValue;
                OnValueChanged?.Invoke(currentValue);
                OnParameterUpdated?.Invoke();
            }
        }

        /// <summary>
        /// Get the current value as a normalized 0-1 range.
        /// </summary>
        public float GetNormalizedValue()
        {
            if (minValue == maxValue) return 0f;
            return (currentValue - minValue) / (maxValue - minValue);
        }

        /// <summary>
        /// Set value from normalized 0-1 range.
        /// </summary>
        public void SetNormalizedValue(float normalized)
        {
            float denormalized = Mathf.Lerp(minValue, maxValue, Mathf.Clamp01(normalized));
            SetValue(denormalized);
        }

        /// <summary>
        /// Reset parameter to default value.
        /// </summary>
        public void ResetToDefault()
        {
            SetValue(defaultValue);
        }

        // Property accessors
        public string ParameterName => parameterName;
        public float CurrentValue => currentValue;
        public float MinValue => minValue;
        public float MaxValue => maxValue;
        public float DefaultValue => defaultValue;
        public string Category => category;
        public string Description => description;

        /// <summary>
        /// Get the value as a percentage of the min-max range.
        /// </summary>
        public float GetPercentage()
        {
            return GetNormalizedValue() * 100f;
        }

        public override string ToString()
        {
            return $"{parameterName}: {currentValue:F2} [{minValue:F2}-{maxValue:F2}]";
        }
    }
}
