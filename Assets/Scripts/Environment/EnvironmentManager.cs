using UnityEngine;

namespace SendIt.Environment
{
    /// <summary>
    /// Central manager for all environmental systems.
    /// Coordinates weather, time of day, and environmental effects.
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        private WeatherSystem weatherSystem;
        private TimeOfDaySystem timeOfDaySystem;

        // Presets for common scenarios
        [System.Serializable]
        public class EnvironmentPreset
        {
            public string Name;
            public float Time; // 0-24 hour format
            public WeatherSystem.WeatherCondition Weather;
            public float RainDuration;
        }

        private EnvironmentPreset[] presets = new EnvironmentPreset[]
        {
            new EnvironmentPreset { Name = "Clear Day", Time = 12f, Weather = WeatherSystem.WeatherCondition.Clear, RainDuration = 0f },
            new EnvironmentPreset { Name = "Rainy Day", Time = 14f, Weather = WeatherSystem.WeatherCondition.Moderate, RainDuration = 60f },
            new EnvironmentPreset { Name = "Storm", Time = 15f, Weather = WeatherSystem.WeatherCondition.Storm, RainDuration = 120f },
            new EnvironmentPreset { Name = "Sunrise", Time = 6.5f, Weather = WeatherSystem.WeatherCondition.Light, RainDuration = 30f },
            new EnvironmentPreset { Name = "Night Clear", Time = 22f, Weather = WeatherSystem.WeatherCondition.Clear, RainDuration = 0f },
            new EnvironmentPreset { Name = "Night Rain", Time = 23f, Weather = WeatherSystem.WeatherCondition.Heavy, RainDuration = 90f },
        };

        public static EnvironmentManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize environment manager and all subsystems.
        /// </summary>
        public void Initialize()
        {
            // Find or create weather system
            weatherSystem = WeatherSystem.Instance;
            if (weatherSystem == null)
            {
                GameObject weatherObj = new GameObject("WeatherSystem");
                weatherSystem = weatherObj.AddComponent<WeatherSystem>();
                weatherSystem.Initialize();
            }

            // Find or create time of day system
            timeOfDaySystem = TimeOfDaySystem.Instance;
            if (timeOfDaySystem == null)
            {
                GameObject timeObj = new GameObject("TimeOfDaySystem");
                timeOfDaySystem = timeObj.AddComponent<TimeOfDaySystem>();
                timeOfDaySystem.Initialize();
            }

            Debug.Log("EnvironmentManager initialized");
        }

        /// <summary>
        /// Apply an environment preset.
        /// </summary>
        public void ApplyPreset(int presetIndex)
        {
            if (presetIndex < 0 || presetIndex >= presets.Length)
                return;

            EnvironmentPreset preset = presets[presetIndex];
            ApplyPreset(preset);
        }

        /// <summary>
        /// Apply a custom environment preset.
        /// </summary>
        public void ApplyPreset(EnvironmentPreset preset)
        {
            if (timeOfDaySystem != null)
            {
                timeOfDaySystem.SetTime(preset.Time);
            }

            if (weatherSystem != null)
            {
                weatherSystem.SetWeather(preset.Weather);
                weatherSystem.SetRainDuration(preset.RainDuration);
            }

            Debug.Log($"Applied environment preset: {preset.Name}");
        }

        /// <summary>
        /// Set weather condition.
        /// </summary>
        public void SetWeather(WeatherSystem.WeatherCondition condition)
        {
            if (weatherSystem != null)
            {
                weatherSystem.SetWeather(condition);
            }
        }

        /// <summary>
        /// Set time of day.
        /// </summary>
        public void SetTime(float hour)
        {
            if (timeOfDaySystem != null)
            {
                timeOfDaySystem.SetTime(hour);
            }
        }

        /// <summary>
        /// Get current environment info as string.
        /// </summary>
        public string GetEnvironmentInfo()
        {
            string info = "=== ENVIRONMENT ===\n";

            if (timeOfDaySystem != null)
            {
                info += timeOfDaySystem.GetTimeInfo() + "\n";
            }

            if (weatherSystem != null)
            {
                info += weatherSystem.GetWeatherInfo() + "\n";
            }

            return info;
        }

        /// <summary>
        /// Get weather system for direct access.
        /// </summary>
        public WeatherSystem GetWeatherSystem() => weatherSystem;

        /// <summary>
        /// Get time of day system for direct access.
        /// </summary>
        public TimeOfDaySystem GetTimeOfDaySystem() => timeOfDaySystem;

        /// <summary>
        /// Get available environment presets.
        /// </summary>
        public EnvironmentPreset[] GetPresets() => presets;

        /// <summary>
        /// Get preset by name.
        /// </summary>
        public EnvironmentPreset GetPreset(string name)
        {
            foreach (EnvironmentPreset preset in presets)
            {
                if (preset.Name == name)
                    return preset;
            }
            return null;
        }
    }
}
