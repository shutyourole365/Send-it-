using UnityEngine;
using SendIt.Physics;
using SendIt.Graphics;

namespace SendIt.Environment
{
    /// <summary>
    /// Manages dynamic weather effects including rain, wet surfaces, and environmental interaction.
    /// Affects vehicle grip, surface cleanliness, and visual conditions.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        public enum WeatherCondition
        {
            Clear,      // No rain, optimal conditions
            Light,      // Light rain, slight grip reduction
            Moderate,   // Moderate rain, noticeable grip loss
            Heavy,      // Heavy rain, significantly reduced grip
            Storm       // Severe storm, very slippery
        }

        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private Light directionalLight;
        [SerializeField] private Camera mainCamera;

        // Weather state
        private WeatherCondition currentWeather = WeatherCondition.Clear;
        private float rainIntensity = 0f; // 0-1
        private float weatherTransitionSpeed = 0.5f; // Seconds to change weather
        private float rainTimer = 0f;
        private float rainDuration = 30f; // How long rain lasts

        // Physics effects
        private float baseGrip = 1f; // Normal grip on dry surfaces
        private float wetSurfaceGripReduction = 0.15f; // Wet surface reduces grip by 15%
        private float visibilityReduction = 1f; // 0-1, affects camera view distance

        // Visual effects
        private Color clearSkyColor = new Color(0.87f, 0.92f, 0.98f); // Light blue
        private Color rainySkyColor = new Color(0.5f, 0.55f, 0.6f); // Grey
        private float clearDirectionalIntensity = 1f;
        private float rainyDirectionalIntensity = 0.6f;

        // Surface interaction
        private VehicleController vehicleController;
        private SkidMarkManager skidMarkManager;
        private DirtAccumulation dirtAccumulation;
        private SurfaceDeformation surfaceDeformation;

        // Configuration
        private TerrainMaterialManager terrainManager;

        private bool isInitialized;

        public static WeatherSystem Instance { get; private set; }

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
        /// Initialize weather system and find required components.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            // Find or create rain particle system
            if (rainParticles == null)
            {
                rainParticles = GetComponentInChildren<ParticleSystem>();
                if (rainParticles == null)
                {
                    // Create a basic rain particle system
                    GameObject rainObject = new GameObject("RainParticles");
                    rainObject.transform.SetParent(transform);
                    rainParticles = rainObject.AddComponent<ParticleSystem>();
                    ConfigureRainParticles();
                }
            }

            // Find main camera
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            // Find directional light (sun)
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

            // Find vehicle and graphics systems
            vehicleController = FindObjectOfType<VehicleController>();
            if (vehicleController != null)
            {
                skidMarkManager = vehicleController.GetSkidMarkManager();
            }

            dirtAccumulation = FindObjectOfType<DirtAccumulation>();
            surfaceDeformation = FindObjectOfType<SurfaceDeformation>();
            terrainManager = TerrainMaterialManager.Instance;

            isInitialized = true;
            Debug.Log("WeatherSystem initialized");
        }

        /// <summary>
        /// Configure the rain particle system.
        /// </summary>
        private void ConfigureRainParticles()
        {
            ParticleSystem.MainModule main = rainParticles.main;
            main.maxParticles = 5000;
            main.duration = 10f;
            main.loop = true;
            main.startLifetime = 3f;
            main.startSpeed = 25f;
            main.startSize = 0.1f;
            main.gravityModifier = 1f;

            ParticleSystem.EmissionModule emission = rainParticles.emission;
            emission.rateOverTime = 1500f;

            ParticleSystem.ShapeModule shape = rainParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(100f, 50f, 100f);
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            // Update rain duration
            if (currentWeather != WeatherCondition.Clear)
            {
                rainTimer += Time.deltaTime;
                if (rainTimer > rainDuration)
                {
                    // Transition back to clear weather
                    SetWeather(WeatherCondition.Clear);
                }
            }

            // Update visual effects
            UpdateVisualEffects();

            // Update particle system
            UpdateRainParticles();
        }

        /// <summary>
        /// Set weather condition with smooth transition.
        /// </summary>
        public void SetWeather(WeatherCondition condition)
        {
            currentWeather = condition;
            rainTimer = 0f;

            // Set target intensity based on condition
            float targetIntensity = condition switch
            {
                WeatherCondition.Clear => 0f,
                WeatherCondition.Light => 0.3f,
                WeatherCondition.Moderate => 0.6f,
                WeatherCondition.Heavy => 0.85f,
                WeatherCondition.Storm => 1f,
                _ => 0f
            };

            // Smoothly transition to target intensity
            StartCoroutine(TransitionRain(targetIntensity));
        }

        /// <summary>
        /// Smoothly transition rain intensity.
        /// </summary>
        private System.Collections.IEnumerator TransitionRain(float targetIntensity)
        {
            float startIntensity = rainIntensity;
            float elapsedTime = 0f;

            while (elapsedTime < weatherTransitionSpeed)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / weatherTransitionSpeed;
                rainIntensity = Mathf.Lerp(startIntensity, targetIntensity, t);

                // Update surface grip as rain intensity changes
                UpdateSurfaceGrip();

                // Clean surfaces as rain falls
                CleanSurfaces();

                yield return null;
            }

            rainIntensity = targetIntensity;
        }

        /// <summary>
        /// Update grip coefficient based on rain intensity.
        /// </summary>
        private void UpdateSurfaceGrip()
        {
            if (terrainManager == null)
                return;

            // Reduce grip for wet surfaces
            float gripReduction = rainIntensity * wetSurfaceGripReduction;

            // Update all terrain types to reduce grip in rain
            foreach (TerrainMaterialManager.TerrainType terrainType in System.Enum.GetValues(typeof(TerrainMaterialManager.TerrainType)))
            {
                var props = terrainManager.GetTerrainProperties(terrainType);
                float originalFriction = props.FrictionCoefficient;

                // Reduce friction based on rain intensity
                // Some surfaces get more slippery (mud, grass) than others (road has good drainage)
                float slipperiness = terrainType switch
                {
                    TerrainMaterialManager.TerrainType.Road => 0.08f, // Roads have good drainage
                    TerrainMaterialManager.TerrainType.Grass => 0.2f, // Grass gets very slippery
                    TerrainMaterialManager.TerrainType.Sand => 0.15f, // Sand somewhat slippery
                    TerrainMaterialManager.TerrainType.Mud => 0.25f, // Mud becomes very slippery
                    TerrainMaterialManager.TerrainType.Gravel => 0.18f, // Gravel gets loose
                    TerrainMaterialManager.TerrainType.Ice => 0.1f, // Ice barely affected
                    TerrainMaterialManager.TerrainType.Concrete => 0.1f, // Concrete has decent grip
                    _ => 0.15f
                };

                props.FrictionCoefficient = originalFriction * (1f - (rainIntensity * slipperiness));
                terrainManager.SetTerrainProperties(terrainType, props);
            }
        }

        /// <summary>
        /// Clean vehicle and surface marks during rain.
        /// </summary>
        private void CleanSurfaces()
        {
            if (rainIntensity <= 0f)
                return;

            // Clean vehicle dirt
            if (dirtAccumulation != null)
            {
                dirtAccumulation.RainClean(rainIntensity);
            }

            // Fade surface deformation
            if (surfaceDeformation != null)
            {
                // Rain cleans up surface marks in a large radius around the vehicle
                if (vehicleController != null)
                {
                    Vector3 vehiclePos = vehicleController.transform.position;
                    surfaceDeformation.RecoverSurface(vehiclePos, 50f);
                }
            }
        }

        /// <summary>
        /// Update visual effects based on weather.
        /// </summary>
        private void UpdateVisualEffects()
        {
            // Update sky color
            Color targetSkyColor = Color.Lerp(clearSkyColor, rainySkyColor, rainIntensity);
            RenderSettings.ambientLight = targetSkyColor;

            // Update directional light intensity
            if (directionalLight != null)
            {
                directionalLight.intensity = Mathf.Lerp(clearDirectionalIntensity, rainyDirectionalIntensity, rainIntensity);
            }

            // Update visibility
            visibilityReduction = 1f - (rainIntensity * 0.4f); // Up to 40% visibility reduction in storm
        }

        /// <summary>
        /// Update rain particle system based on weather intensity.
        /// </summary>
        private void UpdateRainParticles()
        {
            if (rainParticles == null)
                return;

            if (rainIntensity <= 0f)
            {
                rainParticles.Stop();
                return;
            }

            if (!rainParticles.isPlaying)
                rainParticles.Play();

            // Adjust particle emission based on rain intensity
            ParticleSystem.EmissionModule emission = rainParticles.emission;
            emission.rateOverTime = 500f + (rainIntensity * 3500f); // 500-4000 particles/sec

            // Follow camera position
            if (mainCamera != null)
            {
                Vector3 cameraPos = mainCamera.transform.position;
                rainParticles.transform.position = cameraPos + Vector3.up * 30f;
            }

            // Adjust particle speed based on intensity
            ParticleSystem.MainModule main = rainParticles.main;
            main.startSpeed = 20f + (rainIntensity * 15f); // 20-35 m/s
        }

        /// <summary>
        /// Get current weather condition.
        /// </summary>
        public WeatherCondition GetCurrentWeather() => currentWeather;

        /// <summary>
        /// Get current rain intensity (0-1).
        /// </summary>
        public float GetRainIntensity() => rainIntensity;

        /// <summary>
        /// Get visibility modifier (0-1, where 1 is full visibility).
        /// </summary>
        public float GetVisibility() => visibilityReduction;

        /// <summary>
        /// Get grip modifier for current weather (0-1).
        /// </summary>
        public float GetGripModifier() => 1f - (rainIntensity * wetSurfaceGripReduction);

        /// <summary>
        /// Get weather as string for UI.
        /// </summary>
        public string GetWeatherInfo()
        {
            return $"Weather: {currentWeather} ({rainIntensity * 100:F0}% rain)\n" +
                   $"Visibility: {visibilityReduction * 100:F0}%\n" +
                   $"Grip: {GetGripModifier() * 100:F0}%";
        }

        /// <summary>
        /// Set custom rain duration.
        /// </summary>
        public void SetRainDuration(float duration)
        {
            rainDuration = duration;
            rainTimer = 0f;
        }

        /// <summary>
        /// Set custom weather transition speed.
        /// </summary>
        public void SetTransitionSpeed(float speed)
        {
            weatherTransitionSpeed = Mathf.Max(0.1f, speed);
        }
    }
}
