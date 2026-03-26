using UnityEngine;

namespace SendIt.Performance
{
    /// <summary>
    /// Manages performance optimization, graphics settings, and frame rate targeting.
    /// Balances visual quality with performance across different hardware.
    /// </summary>
    public class PerformanceManager : MonoBehaviour
    {
        public enum QualityLevel
        {
            Low,      // 30 FPS, minimal effects
            Medium,   // 60 FPS, balanced
            High,     // 60+ FPS, full effects
            Ultra     // 120+ FPS, all features enabled
        }

        [SerializeField] private QualityLevel targetQualityLevel = QualityLevel.High;
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool useVSync = false;

        // Graphics settings
        private int maxSkidMarks = 500;
        private int maxSurfaceDeformationTracks = 200;
        private int maxRainParticles = 5000;
        private float shadowDistance = 100f;
        private int shadowQuality = 2; // 0=Off, 1=Low, 2=Medium, 3=High

        // Performance monitoring
        private float currentFPS = 0f;
        private float frameTime = 0f;
        private int frameCount = 0;
        private float fpsUpdateInterval = 0.5f;
        private float timeSinceLastFpsUpdate = 0f;

        // Culling
        private Camera mainCamera;
        private float cullingDistance = 200f;

        public static PerformanceManager Instance { get; private set; }

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
        /// Initialize performance manager and apply settings.
        /// </summary>
        public void Initialize()
        {
            mainCamera = Camera.main;
            ApplyQualityLevel(targetQualityLevel);
            ApplyFrameRateSettings();
            Debug.Log($"PerformanceManager initialized - Quality: {targetQualityLevel}, Target FPS: {targetFrameRate}");
        }

        private void Update()
        {
            // Monitor FPS
            UpdateFPSCounter();
        }

        /// <summary>
        /// Apply graphics quality settings based on quality level.
        /// </summary>
        public void ApplyQualityLevel(QualityLevel level)
        {
            targetQualityLevel = level;

            switch (level)
            {
                case QualityLevel.Low:
                    ApplyLowQualitySettings();
                    break;
                case QualityLevel.Medium:
                    ApplyMediumQualitySettings();
                    break;
                case QualityLevel.High:
                    ApplyHighQualitySettings();
                    break;
                case QualityLevel.Ultra:
                    ApplyUltraQualitySettings();
                    break;
            }

            Debug.Log($"Applied {level} quality settings");
        }

        /// <summary>
        /// Low quality settings (30 FPS target, minimal effects).
        /// </summary>
        private void ApplyLowQualitySettings()
        {
            targetFrameRate = 30;
            maxSkidMarks = 200;
            maxSurfaceDeformationTracks = 50;
            maxRainParticles = 1000;
            shadowDistance = 30f;
            shadowQuality = 0;

            // Disable expensive features
            QualitySettings.shadowCascades = 0;
            QualitySettings.antiAliasing = 0;
            RenderSettings.fog = false;
        }

        /// <summary>
        /// Medium quality settings (60 FPS target, balanced).
        /// </summary>
        private void ApplyMediumQualitySettings()
        {
            targetFrameRate = 60;
            maxSkidMarks = 300;
            maxSurfaceDeformationTracks = 100;
            maxRainParticles = 2500;
            shadowDistance = 60f;
            shadowQuality = 1;

            QualitySettings.shadowCascades = 1;
            QualitySettings.antiAliasing = 2;
            RenderSettings.fog = true;
            RenderSettings.fogDensity = 0.01f;
        }

        /// <summary>
        /// High quality settings (60+ FPS target, full effects).
        /// </summary>
        private void ApplyHighQualitySettings()
        {
            targetFrameRate = 60;
            maxSkidMarks = 500;
            maxSurfaceDeformationTracks = 200;
            maxRainParticles = 4000;
            shadowDistance = 150f;
            shadowQuality = 2;

            QualitySettings.shadowCascades = 2;
            QualitySettings.antiAliasing = 4;
            RenderSettings.fog = true;
            RenderSettings.fogDensity = 0.015f;
        }

        /// <summary>
        /// Ultra quality settings (120+ FPS, all features).
        /// </summary>
        private void ApplyUltraQualitySettings()
        {
            targetFrameRate = 120;
            maxSkidMarks = 1000;
            maxSurfaceDeformationTracks = 500;
            maxRainParticles = 5000;
            shadowDistance = 200f;
            shadowQuality = 3;

            QualitySettings.shadowCascades = 4;
            QualitySettings.antiAliasing = 8;
            RenderSettings.fog = true;
            RenderSettings.fogDensity = 0.02f;
        }

        /// <summary>
        /// Apply frame rate and V-Sync settings.
        /// </summary>
        private void ApplyFrameRateSettings()
        {
            if (useVSync)
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1; // Let V-Sync control
            }
            else
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = targetFrameRate;
            }

            Debug.Log($"Frame rate settings - V-Sync: {useVSync}, Target: {targetFrameRate}");
        }

        /// <summary>
        /// Update FPS counter.
        /// </summary>
        private void UpdateFPSCounter()
        {
            frameCount++;
            timeSinceLastFpsUpdate += Time.deltaTime;

            if (timeSinceLastFpsUpdate >= fpsUpdateInterval)
            {
                currentFPS = frameCount / timeSinceLastFpsUpdate;
                frameTime = 1f / currentFPS;
                timeSinceLastFpsUpdate = 0f;
                frameCount = 0;

                // Auto-adjust quality if FPS drops significantly
                if (currentFPS < targetFrameRate * 0.8f)
                {
                    CheckAndAdjustQuality();
                }
            }
        }

        /// <summary>
        /// Check if quality needs adjustment based on performance.
        /// </summary>
        private void CheckAndAdjustQuality()
        {
            // If FPS is 20% below target for 5+ seconds, lower quality
            // This prevents oscillation by only adjusting gradually
            if (currentFPS < targetFrameRate * 0.7f && targetQualityLevel > QualityLevel.Low)
            {
                Debug.LogWarning($"FPS dropped to {currentFPS:F1}, lowering quality");
                ApplyQualityLevel(targetQualityLevel - 1);
            }
        }

        /// <summary>
        /// Set custom frame rate.
        /// </summary>
        public void SetTargetFrameRate(int frameRate)
        {
            targetFrameRate = Mathf.Clamp(frameRate, 30, 240);
            ApplyFrameRateSettings();
        }

        /// <summary>
        /// Toggle V-Sync.
        /// </summary>
        public void SetVSyncEnabled(bool enabled)
        {
            useVSync = enabled;
            ApplyFrameRateSettings();
        }

        /// <summary>
        /// Get current FPS.
        /// </summary>
        public float GetCurrentFPS() => currentFPS;

        /// <summary>
        /// Get current frame time (ms).
        /// </summary>
        public float GetFrameTimeMS() => frameTime * 1000f;

        /// <summary>
        /// Get current quality level.
        /// </summary>
        public QualityLevel GetQualityLevel() => targetQualityLevel;

        /// <summary>
        /// Get max skid marks for current quality.
        /// </summary>
        public int GetMaxSkidMarks() => maxSkidMarks;

        /// <summary>
        /// Get max surface deformation tracks for current quality.
        /// </summary>
        public int GetMaxSurfaceDeformationTracks() => maxSurfaceDeformationTracks;

        /// <summary>
        /// Get max rain particles for current quality.
        /// </summary>
        public int GetMaxRainParticles() => maxRainParticles;

        /// <summary>
        /// Get shadow distance for culling.
        /// </summary>
        public float GetShadowDistance() => shadowDistance;

        /// <summary>
        /// Get culling distance for LOD.
        /// </summary>
        public float GetCullingDistance() => cullingDistance;

        /// <summary>
        /// Get performance diagnostics.
        /// </summary>
        public string GetPerformanceDiagnostics()
        {
            string diag = "\n=== PERFORMANCE ===\n";
            diag += $"FPS: {currentFPS:F1} (Target: {targetFrameRate})\n";
            diag += $"Frame Time: {frameTime * 1000f:F2}ms\n";
            diag += $"Quality: {targetQualityLevel}\n";
            diag += $"V-Sync: {(useVSync ? "On" : "Off")}\n";
            diag += $"Max Skid Marks: {maxSkidMarks}\n";
            diag += $"Max Deformation: {maxSurfaceDeformationTracks}\n";
            diag += $"Max Rain: {maxRainParticles}\n";
            diag += $"Shadow Distance: {shadowDistance}m\n";
            return diag;
        }
    }
}
