using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SendIt.Physics;
using SendIt.Data;

namespace SendIt.UI
{
    /// <summary>
    /// Advanced telemetry panel for Phase 3 with detailed system monitoring.
    /// Displays real-time data from all vehicle systems with live comparisons.
    /// </summary>
    public class AdvancedTelemetryPanel : MonoBehaviour
    {
        [SerializeField] private Transform telemetryContent;
        [SerializeField] private Toggle showPhysicsToggle;
        [SerializeField] private Toggle showGraphicsToggle;
        [SerializeField] private Toggle showComparisonToggle;
        [SerializeField] private Text summaryLabel;

        // Telemetry item prefab
        [SerializeField] private Text telemetryItemPrefab;

        // Data storage
        private Dictionary<string, Text> telemetryDisplays = new Dictionary<string, Text>();
        private Dictionary<string, float> previousValues = new Dictionary<string, float>();
        private Dictionary<string, float> deltaValues = new Dictionary<string, float>();

        // Comparison data
        private Dictionary<string, (float min, float max, float avg)> sessionStats = new Dictionary<string, (float, float, float)>();
        private Dictionary<string, Queue<float>> dataHistory = new Dictionary<string, Queue<float>>();
        private const int maxDataPoints = 300; // 5 seconds at 60 FPS

        private VehicleController vehicleController;
        private bool isInitialized;

        public void Initialize(VehicleController controller)
        {
            vehicleController = controller;
            CreateTelemetryDisplays();
            isInitialized = true;
        }

        /// <summary>
        /// Create telemetry display items dynamically.
        /// </summary>
        private void CreateTelemetryDisplays()
        {
            // Physics section
            CreateDisplayItem("Engine RPM", "RPM");
            CreateDisplayItem("Engine Power", "kW");
            CreateDisplayItem("Engine Torque", "Nm");
            CreateDisplayItem("Current Gear", "");
            CreateDisplayItem("Speed", "m/s");

            // Tire section
            CreateDisplayItem("Tire Temp (Avg)", "°C");
            CreateDisplayItem("Tire Wear (Avg)", "%");
            CreateDisplayItem("Tire Pressure (Avg)", "PSI");
            CreateDisplayItem("Slip Angle (Avg)", "°");
            CreateDisplayItem("Slip Ratio (Avg)", "");

            // Dynamics section
            CreateDisplayItem("Lateral Accel", "m/s²");
            CreateDisplayItem("Longitudinal Accel", "m/s²");
            CreateDisplayItem("Roll Angle", "°");
            CreateDisplayItem("Traction", "%");

            // Suspension section
            CreateDisplayItem("Suspension Load (Avg)", "N");
            CreateDisplayItem("Spring Force (Avg)", "N");

            // Graphics section
            CreateDisplayItem("Frame Time", "ms");
            CreateDisplayItem("FPS", "");
            CreateDisplayItem("Draw Calls", "");
        }

        /// <summary>
        /// Create a telemetry display item.
        /// </summary>
        private void CreateDisplayItem(string label, string unit)
        {
            if (telemetryItemPrefab == null || telemetryContent == null)
                return;

            Text item = Instantiate(telemetryItemPrefab, telemetryContent);
            item.text = $"{label}: -- {unit}";

            string key = label.ToLower().Replace(" ", "_");
            telemetryDisplays[key] = item;
            dataHistory[key] = new Queue<float>();

            // Initialize stats
            sessionStats[key] = (float.MaxValue, float.MinValue, 0f);
        }

        /// <summary>
        /// Update all telemetry displays.
        /// </summary>
        public void UpdateTelemetry(Telemetry telemetry, VehicleController controller)
        {
            if (!isInitialized)
                return;

            // Get latest telemetry frame
            var frame = telemetry.GetLatestFrame();

            // Update engine data
            UpdateDisplay("engine_rpm", frame.EngineRPM, "RPM");
            UpdateDisplay("engine_power", frame.EnginePower / 1000f, "kW");
            UpdateDisplay("engine_torque", frame.EngineTorque, "Nm");
            UpdateDisplay("current_gear", frame.CurrentGear, "");
            UpdateDisplay("speed", frame.Speed, "m/s");

            // Calculate averages for tire data
            float avgTireTemp = 0, avgTireWear = 0, avgPressure = 0, avgSlipAngle = 0, avgSlipRatio = 0;
            for (int i = 0; i < 4; i++)
            {
                avgTireTemp += frame.TireTemperatures[i];
                avgTireWear += frame.TireWear[i];
                avgSlipAngle += Mathf.Abs(frame.SlipAngles[i]);
                avgSlipRatio += Mathf.Abs(frame.SlipRatios[i]);
            }
            avgTireTemp /= 4f;
            avgTireWear /= 4f;
            avgSlipAngle /= 4f;
            avgSlipRatio /= 4f;

            UpdateDisplay("tire_temp_avg", avgTireTemp, "°C");
            UpdateDisplay("tire_wear_avg", avgTireWear * 100f, "%");
            UpdateDisplay("tire_pressure_avg", avgPressure, "PSI");
            UpdateDisplay("slip_angle_avg", avgSlipAngle * Mathf.Rad2Deg, "°");
            UpdateDisplay("slip_ratio_avg", avgSlipRatio, "");

            // Update dynamics
            UpdateDisplay("lateral_accel", frame.LateralAcceleration, "m/s²");
            UpdateDisplay("longitudinal_accel", frame.LongitudinalAcceleration, "m/s²");
            UpdateDisplay("roll_angle", frame.RollAngle, "°");
            UpdateDisplay("traction", Mathf.Clamp01(frame.Traction) * 100f, "%");

            // Update suspension
            float avgLoad = 0;
            for (int i = 0; i < 4; i++)
            {
                avgLoad += frame.WheelLoads[i];
            }
            avgLoad /= 4f;
            UpdateDisplay("suspension_load_avg", avgLoad, "N");

            // Update graphics
            float frameTime = Time.deltaTime * 1000f;
            float fps = 1f / Time.deltaTime;
            UpdateDisplay("frame_time", frameTime, "ms");
            UpdateDisplay("fps", fps, "");

            // Update summary
            UpdateSummaryDisplay(frame);
        }

        /// <summary>
        /// Update a single telemetry display value.
        /// </summary>
        private void UpdateDisplay(string key, float value, string unit)
        {
            if (!telemetryDisplays.ContainsKey(key))
                return;

            Text display = telemetryDisplays[key];
            string label = key.Replace("_", " ").ToUpper();

            // Calculate delta
            if (previousValues.ContainsKey(key))
            {
                deltaValues[key] = value - previousValues[key];
            }
            previousValues[key] = value;

            // Store in history
            if (dataHistory[key].Count >= maxDataPoints)
            {
                dataHistory[key].Dequeue();
            }
            dataHistory[key].Enqueue(value);

            // Update stats
            UpdateStats(key, value);

            // Format display
            string deltaStr = deltaValues.ContainsKey(key) ?
                $" [{deltaValues[key]:+0.0;-0.0}]" : "";

            display.text = $"{label}: {value:F1} {unit}{deltaStr}";

            // Color code based on change
            if (deltaValues.ContainsKey(key))
            {
                float delta = deltaValues[key];
                if (Mathf.Abs(delta) < 0.01f)
                    display.color = Color.white;
                else if (delta > 0)
                    display.color = Color.green;
                else
                    display.color = Color.red;
            }
        }

        /// <summary>
        /// Update session statistics for a value.
        /// </summary>
        private void UpdateStats(string key, float value)
        {
            if (!sessionStats.ContainsKey(key))
                return;

            var stats = sessionStats[key];
            stats.min = Mathf.Min(stats.min, value);
            stats.max = Mathf.Max(stats.max, value);

            // Calculate running average
            float total = 0;
            foreach (float v in dataHistory[key])
            {
                total += v;
            }
            stats.avg = total / Mathf.Max(dataHistory[key].Count, 1);

            sessionStats[key] = stats;
        }

        /// <summary>
        /// Update summary display with key metrics.
        /// </summary>
        private void UpdateSummaryDisplay(Telemetry.TelemetryFrame frame)
        {
            if (summaryLabel == null)
                return;

            string summary = $@"
PERFORMANCE SUMMARY
Speed: {frame.Speed:F1} m/s ({frame.Speed * 3.6f:F1} km/h)
Power: {frame.EnginePower / 1000f:F1} kW | Torque: {frame.EngineTorque:F0} Nm
Grip: {sessionStats.ContainsKey("traction") ? sessionStats["traction"].avg * 100f : 0:F0}%
Temperature: {(sessionStats.ContainsKey("tire_temp_avg") ? sessionStats["tire_temp_avg"].avg : 0):F0}°C
Wear: {(sessionStats.ContainsKey("tire_wear_avg") ? sessionStats["tire_wear_avg"].avg : 0):F1}%
";

            summaryLabel.text = summary;
        }

        /// <summary>
        /// Get session statistics for analysis.
        /// </summary>
        public Dictionary<string, (float min, float max, float avg)> GetSessionStats()
        {
            return sessionStats;
        }

        /// <summary>
        /// Export telemetry session as CSV.
        /// </summary>
        public string ExportSessionAsCSV()
        {
            List<string> keys = new List<string>(dataHistory.Keys);
            if (keys.Count == 0)
                return "";

            // Header
            string csv = string.Join(",", keys) + "\n";

            // Data rows
            int maxRows = 0;
            foreach (var history in dataHistory.Values)
            {
                maxRows = Mathf.Max(maxRows, history.Count);
            }

            List<float[]> dataArrays = new List<float[]>();
            foreach (var key in keys)
            {
                dataArrays.Add(dataHistory[key].ToArray());
            }

            for (int row = 0; row < maxRows; row++)
            {
                List<string> rowValues = new List<string>();
                for (int col = 0; col < dataArrays.Count; col++)
                {
                    float value = row < dataArrays[col].Length ? dataArrays[col][row] : 0f;
                    rowValues.Add(value.ToString("F2"));
                }
                csv += string.Join(",", rowValues) + "\n";
            }

            return csv;
        }

        /// <summary>
        /// Clear all session data.
        /// </summary>
        public void ClearSession()
        {
            foreach (var history in dataHistory.Values)
            {
                history.Clear();
            }

            foreach (var key in sessionStats.Keys)
            {
                sessionStats[key] = (float.MaxValue, float.MinValue, 0f);
            }
        }

        public bool IsInitialized => isInitialized;
    }
}
