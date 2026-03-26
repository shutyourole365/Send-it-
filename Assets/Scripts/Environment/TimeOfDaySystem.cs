using UnityEngine;

namespace SendIt.Environment
{
    /// <summary>
    /// Manages time of day and lighting cycles.
    /// Creates dynamic lighting changes and affects vehicle visibility.
    /// </summary>
    public class TimeOfDaySystem : MonoBehaviour
    {
        [SerializeField] private Light directionalLight;
        [SerializeField] private float dayDurationMinutes = 10f; // Full cycle in minutes
        [SerializeField] private float sunRotationSpeed = 1.5f;

        // Time tracking
        private float currentTime = 6f; // 6:00 AM start time (0-24 hour format)
        private float timeScale = 1f; // How fast time passes (1 = real-time seconds per minute)

        // Lighting
        private Color sunriseColor = new Color(1f, 0.7f, 0.3f);
        private Color noonColor = new Color(1f, 0.95f, 0.8f);
        private Color sunsetColor = new Color(1f, 0.5f, 0.2f);
        private Color nightColor = new Color(0.2f, 0.3f, 0.5f);

        private float sunriseIntensity = 0.4f;
        private float noonIntensity = 1.2f;
        private float sunsetIntensity = 0.6f;
        private float nightIntensity = 0.1f;

        // Fog effect
        private float baseFogDensity = 0f;
        private float nightFogDensity = 0.02f;

        private bool isInitialized;

        public static TimeOfDaySystem Instance { get; private set; }

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
        /// Initialize time of day system.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            // Find directional light
            if (directionalLight == null)
            {
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        directionalLight = light;
                        break;
                    }
                }
            }

            isInitialized = true;
            Debug.Log("TimeOfDaySystem initialized");
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            // Update time
            currentTime += Time.deltaTime * timeScale / 60f; // Convert to minutes then to hours
            if (currentTime >= 24f)
                currentTime -= 24f;

            // Update lighting based on time
            UpdateLighting();
        }

        /// <summary>
        /// Update sun position and color based on time of day.
        /// </summary>
        private void UpdateLighting()
        {
            if (directionalLight == null)
                return;

            // Calculate sun rotation (0-360 degrees through the day)
            // 6 AM = dawn, 12 PM = noon, 6 PM = sunset, 12 AM = night
            float sunAngle = (currentTime - 6f) * 15f; // 15 degrees per hour
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 0f, 0f);

            // Update sun color and intensity based on time
            Color sunColor = GetSunColor();
            float sunIntensity = GetSunIntensity();
            float ambientIntensity = GetAmbientIntensity();

            directionalLight.color = sunColor;
            directionalLight.intensity = sunIntensity;

            // Update ambient light
            RenderSettings.ambientLight = Color.Lerp(Color.white, nightColor, 1f - ambientIntensity);
            RenderSettings.ambientIntensity = ambientIntensity;

            // Update fog based on weather and time
            UpdateFog();
        }

        /// <summary>
        /// Get sun color based on current time.
        /// </summary>
        private Color GetSunColor()
        {
            if (currentTime < 6f || currentTime >= 22f)
                return nightColor; // Night
            else if (currentTime < 7f)
                return Color.Lerp(nightColor, sunriseColor, (currentTime - 6f) / 1f); // Sunrise
            else if (currentTime < 12f)
                return Color.Lerp(sunriseColor, noonColor, (currentTime - 7f) / 5f); // Morning
            else if (currentTime < 17f)
                return Color.Lerp(noonColor, sunsetColor, (currentTime - 12f) / 5f); // Afternoon
            else if (currentTime < 18f)
                return Color.Lerp(sunsetColor, nightColor, (currentTime - 17f) / 1f); // Sunset
            else if (currentTime < 22f)
                return Color.Lerp(nightColor, nightColor, 1f); // Dusk to night
            else
                return nightColor; // Night
        }

        /// <summary>
        /// Get sun intensity based on current time.
        /// </summary>
        private float GetSunIntensity()
        {
            if (currentTime < 6f || currentTime >= 22f)
                return nightIntensity; // Night
            else if (currentTime < 7f)
                return Mathf.Lerp(nightIntensity, sunriseIntensity, (currentTime - 6f) / 1f); // Sunrise
            else if (currentTime < 12f)
                return Mathf.Lerp(sunriseIntensity, noonIntensity, (currentTime - 7f) / 5f); // Morning
            else if (currentTime < 17f)
                return Mathf.Lerp(noonIntensity, sunsetIntensity, (currentTime - 12f) / 5f); // Afternoon
            else if (currentTime < 18f)
                return Mathf.Lerp(sunsetIntensity, nightIntensity, (currentTime - 17f) / 1f); // Sunset
            else if (currentTime < 22f)
                return Mathf.Lerp(nightIntensity, nightIntensity, 1f); // Dusk to night
            else
                return nightIntensity; // Night
        }

        /// <summary>
        /// Get ambient light intensity based on time.
        /// </summary>
        private float GetAmbientIntensity()
        {
            if (currentTime < 6f || currentTime >= 22f)
                return 0.2f; // Night
            else if (currentTime < 7f)
                return Mathf.Lerp(0.2f, 0.5f, (currentTime - 6f) / 1f); // Sunrise
            else if (currentTime < 12f)
                return Mathf.Lerp(0.5f, 1.2f, (currentTime - 7f) / 5f); // Morning
            else if (currentTime < 17f)
                return Mathf.Lerp(1.2f, 0.6f, (currentTime - 12f) / 5f); // Afternoon
            else if (currentTime < 18f)
                return Mathf.Lerp(0.6f, 0.2f, (currentTime - 17f) / 1f); // Sunset
            else
                return 0.2f; // Night
        }

        /// <summary>
        /// Update fog based on time of day.
        /// </summary>
        private void UpdateFog()
        {
            WeatherSystem weather = WeatherSystem.Instance;
            if (weather == null)
                return;

            // Increase fog at night and in rain
            float timeOfDayFog = IsNight() ? nightFogDensity : baseFogDensity;
            float weatherFog = weather.GetRainIntensity() * 0.03f;

            RenderSettings.fogDensity = timeOfDayFog + weatherFog;
        }

        /// <summary>
        /// Check if it's currently night time.
        /// </summary>
        public bool IsNight() => currentTime < 6f || currentTime >= 20f;

        /// <summary>
        /// Check if it's currently day time.
        /// </summary>
        public bool IsDay() => currentTime >= 7f && currentTime < 19f;

        /// <summary>
        /// Get current time in 24-hour format.
        /// </summary>
        public float GetCurrentTime() => currentTime;

        /// <summary>
        /// Get time as formatted string (HH:MM).
        /// </summary>
        public string GetTimeString()
        {
            int hours = (int)currentTime;
            int minutes = (int)((currentTime - hours) * 60f);
            return $"{hours:D2}:{minutes:D2}";
        }

        /// <summary>
        /// Set current time.
        /// </summary>
        public void SetTime(float hour)
        {
            currentTime = Mathf.Clamp(hour, 0f, 24f);
        }

        /// <summary>
        /// Set time scale (how fast time passes).
        /// </summary>
        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Max(0f, scale);
        }

        /// <summary>
        /// Get time of day information.
        /// </summary>
        public string GetTimeInfo()
        {
            string period = "Day";
            if (IsNight())
                period = "Night";
            else if (currentTime < 12f)
                period = "Morning";
            else if (currentTime < 17f)
                period = "Afternoon";
            else
                period = "Evening";

            return $"Time: {GetTimeString()} ({period})";
        }

        /// <summary>
        /// Advance time by specified hours.
        /// </summary>
        public void AdvanceTime(float hours)
        {
            currentTime += hours;
            if (currentTime >= 24f)
                currentTime -= 24f;
        }
    }
}
