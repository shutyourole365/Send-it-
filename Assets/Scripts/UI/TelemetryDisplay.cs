using UnityEngine;
using UnityEngine.UI;
using SendIt.Physics;

namespace SendIt.UI
{
    /// <summary>
    /// Real-time telemetry display showing vehicle physics data.
    /// Updates every frame with current engine, speed, tire, and dynamics information.
    /// </summary>
    public class TelemetryDisplay : MonoBehaviour
    {
        [SerializeField] private Text engineRPMLabel;
        [SerializeField] private Text enginePowerLabel;
        [SerializeField] private Text engineTorqueLabel;
        [SerializeField] private Text gearLabel;
        [SerializeField] private Text speedLabel;

        [SerializeField] private Text[] tireTemperatureLabels = new Text[4];
        [SerializeField] private Text[] tireWearLabels = new Text[4];
        [SerializeField] private Text[] tireGripLabels = new Text[4];

        [SerializeField] private Text lateralAccelLabel;
        [SerializeField] private Text rollAngleLabel;
        [SerializeField] private Text tractionLabel;

        [SerializeField] private Text[] wheelLoadLabels = new Text[4];

        private Telemetry telemetry;
        private bool isInitialized;

        public void Initialize(Telemetry tel)
        {
            telemetry = tel;
            isInitialized = true;
        }

        /// <summary>
        /// Update all telemetry displays with latest data.
        /// </summary>
        public void UpdateDisplay(Telemetry.TelemetryFrame frame)
        {
            if (!isInitialized)
                return;

            // Update engine data
            if (engineRPMLabel != null)
                engineRPMLabel.text = $"RPM: {frame.EngineRPM:F0}";

            if (enginePowerLabel != null)
                enginePowerLabel.text = $"Power: {frame.EnginePower:F1} HP";

            if (engineTorqueLabel != null)
                engineTorqueLabel.text = $"Torque: {frame.EngineTorque:F1} Nm";

            if (gearLabel != null)
                gearLabel.text = $"Gear: {frame.CurrentGear}";

            // Update speed
            if (speedLabel != null)
                speedLabel.text = $"Speed: {frame.SpeedKmh:F1} km/h";

            // Update tire data
            for (int i = 0; i < 4; i++)
            {
                if (tireTemperatureLabels[i] != null && frame.TireTemperatures != null)
                {
                    string wheelName = GetWheelName(i);
                    tireTemperatureLabels[i].text = $"{wheelName} Temp: {frame.TireTemperatures[i]:F1}°C";
                }

                if (tireWearLabels[i] != null && frame.TireWear != null)
                {
                    tireWearLabels[i].text = $"{GetWheelName(i)} Wear: {frame.TireWear[i] * 100:F1}%";
                }

                if (tireGripLabels[i] != null && frame.TireGrip != null)
                {
                    tireGripLabels[i].text = $"{GetWheelName(i)} Grip: {frame.TireGrip[i]:F2}";
                }

                if (wheelLoadLabels[i] != null && frame.WheelLoads != null)
                {
                    wheelLoadLabels[i].text = $"{GetWheelName(i)} Load: {frame.WheelLoads[i]:F0}N";
                }
            }

            // Update dynamics
            if (lateralAccelLabel != null)
                lateralAccelLabel.text = $"Lateral Accel: {frame.LateralAccel:F2} m/s²";

            if (rollAngleLabel != null)
                rollAngleLabel.text = $"Roll: {frame.RollAngle:F1}°";

            if (tractionLabel != null)
                tractionLabel.text = $"Traction: {frame.TractionLevel * 100:F1}%";
        }

        /// <summary>
        /// Get wheel name from index.
        /// </summary>
        private string GetWheelName(int index)
        {
            return index switch
            {
                0 => "FL",
                1 => "FR",
                2 => "RL",
                3 => "RR",
                _ => "?"
            };
        }

        /// <summary>
        /// Create UI labels dynamically if not assigned in editor.
        /// </summary>
        public void CreateDefaultLabels()
        {
            // Create a simple vertical layout with text labels
            VerticalLayoutGroup layout = GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childForceExpandHeight = false;
            }

            // Create engine section
            CreateLabel("=== ENGINE ===");
            engineRPMLabel = CreateLabel("RPM: 0").GetComponent<Text>();
            enginePowerLabel = CreateLabel("Power: 0 HP").GetComponent<Text>();
            engineTorqueLabel = CreateLabel("Torque: 0 Nm").GetComponent<Text>();
            gearLabel = CreateLabel("Gear: 1").GetComponent<Text>();

            // Create speed section
            CreateLabel("=== SPEED ===");
            speedLabel = CreateLabel("Speed: 0 km/h").GetComponent<Text>();

            // Create tires section
            CreateLabel("=== TIRES ===");
            for (int i = 0; i < 4; i++)
            {
                tireTemperatureLabels[i] = CreateLabel($"{GetWheelName(i)} Temp: 0°C").GetComponent<Text>();
            }

            // Create dynamics section
            CreateLabel("=== DYNAMICS ===");
            lateralAccelLabel = CreateLabel("Lateral Accel: 0 m/s²").GetComponent<Text>();
            rollAngleLabel = CreateLabel("Roll: 0°").GetComponent<Text>();
            tractionLabel = CreateLabel("Traction: 0%").GetComponent<Text>();
        }

        /// <summary>
        /// Helper to create a label dynamically.
        /// </summary>
        private GameObject CreateLabel(string text)
        {
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(transform);
            Text labelText = labelGO.AddComponent<Text>();
            labelText.text = text;
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.alignment = TextAnchor.MiddleLeft;
            return labelGO;
        }
    }
}
