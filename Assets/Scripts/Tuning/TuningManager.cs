using UnityEngine;
using System;
using System.Collections.Generic;
using SendIt.Data;

namespace SendIt.Tuning
{
    /// <summary>
    /// Central manager for all vehicle tuning parameters.
    /// Handles creation, modification, and validation of all tunable physics and graphics parameters.
    /// </summary>
    public class TuningManager : MonoBehaviour
    {
        [SerializeField] private VehicleData vehicleData;

        // Physics Parameters Dictionary
        private Dictionary<string, TuneParameter> physicsParameters = new Dictionary<string, TuneParameter>();

        // Graphics Parameters Dictionary
        private Dictionary<string, TuneParameter> graphicsParameters = new Dictionary<string, TuneParameter>();

        // Events
        public event Action<string, float> OnPhysicsParameterChanged;
        public event Action<string, float> OnGraphicsParameterChanged;
        public event Action OnAllParametersUpdated;

        private static TuningManager instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            if (vehicleData == null)
            {
                vehicleData = new VehicleData();
            }

            InitializeParameters();
        }

        /// <summary>
        /// Initialize all tuning parameters from vehicle data.
        /// </summary>
        private void InitializeParameters()
        {
            InitializePhysicsParameters();
            InitializeGraphicsParameters();
        }

        /// <summary>
        /// Create and register physics tuning parameters.
        /// </summary>
        private void InitializePhysicsParameters()
        {
            physicsParameters.Clear();

            // Engine Parameters
            RegisterPhysicsParameter("MaxRPM", vehicleData.Physics.MaxRPM, 3000f, 12000f, "Engine");
            RegisterPhysicsParameter("HorsePower", vehicleData.Physics.HorsePower, 50f, 2000f, "Engine");
            RegisterPhysicsParameter("TorquePeakRPM", vehicleData.Physics.TorquePeakRPM, 2000f, 8000f, "Engine");
            RegisterPhysicsParameter("EngineResponsiveness", vehicleData.Physics.EngineResponsiveness, 0.5f, 2.0f, "Engine");

            // Transmission Parameters
            RegisterPhysicsParameter("GearCount", vehicleData.Physics.GearCount, 3f, 8f, "Transmission");
            RegisterPhysicsParameter("FinalDriveRatio", vehicleData.Physics.FinalDriveRatio, 2.0f, 5.0f, "Transmission");
            RegisterPhysicsParameter("ShiftSpeed", vehicleData.Physics.ShiftSpeed, 0.1f, 0.5f, "Transmission");

            // Suspension Parameters
            RegisterPhysicsParameter("SpringStiffness", vehicleData.Physics.SpringStiffness, 5000f, 50000f, "Suspension");
            RegisterPhysicsParameter("CompressionDamping", vehicleData.Physics.CompressionDamping, 0.1f, 2.0f, "Suspension");
            RegisterPhysicsParameter("ExtensionDamping", vehicleData.Physics.ExtensionDamping, 0.1f, 2.0f, "Suspension");
            RegisterPhysicsParameter("RideHeight", vehicleData.Physics.RideHeight, 0.1f, 0.5f, "Suspension");
            RegisterPhysicsParameter("AntiRollBarStiffness", vehicleData.Physics.AntiRollBarStiffness, 5000f, 30000f, "Suspension");

            // Tire Parameters
            RegisterPhysicsParameter("TireGripCoefficient", vehicleData.Physics.TireGripCoefficient, 0.5f, 1.5f, "Tires");
            RegisterPhysicsParameter("TirePeakSlip", vehicleData.Physics.TirePeakSlip, 0.1f, 0.25f, "Tires");
            RegisterPhysicsParameter("TireTemperatureSensitivity", vehicleData.Physics.TireTemperatureSensitivity, 0.5f, 1.5f, "Tires");
            RegisterPhysicsParameter("TireWearRate", vehicleData.Physics.TireWearRate, 0.0001f, 0.01f, "Tires");

            // Aerodynamics
            RegisterPhysicsParameter("DragCoefficient", vehicleData.Physics.DragCoefficient, 0.15f, 0.6f, "Aerodynamics");
            RegisterPhysicsParameter("DownforceCoefficient", vehicleData.Physics.DownforceCoefficient, 0f, 100f, "Aerodynamics");
            RegisterPhysicsParameter("SpoilerAngle", vehicleData.Physics.SpoilerAngle, 0f, 45f, "Aerodynamics");

            // Weight & Balance
            RegisterPhysicsParameter("TotalMass", vehicleData.Physics.TotalMass, 800f, 2000f, "Weight");
            RegisterPhysicsParameter("FrontWeightDistribution", vehicleData.Physics.FrontWeightDistribution, 0.3f, 0.7f, "Weight");
        }

        /// <summary>
        /// Create and register graphics tuning parameters.
        /// </summary>
        private void InitializeGraphicsParameters()
        {
            graphicsParameters.Clear();

            // Paint Parameters
            RegisterGraphicsParameter("MetallicIntensity", vehicleData.Graphics.MetallicIntensity, 0f, 1f, "Paint");
            RegisterGraphicsParameter("Glossiness", vehicleData.Graphics.Glossiness, 0f, 1f, "Paint");
            RegisterGraphicsParameter("PearlcentIntensity", vehicleData.Graphics.PearlcentIntensity, 0f, 1f, "Paint");

            // Body Modifications
            RegisterGraphicsParameter("WheelSize", vehicleData.Graphics.WheelSize, 15f, 22f, "Body");
            RegisterGraphicsParameter("WheelOffset", vehicleData.Graphics.WheelOffset, -50f, 50f, "Body");
            RegisterGraphicsParameter("SpoilerHeight", vehicleData.Graphics.SpoilerHeight, 0f, 200f, "Body");
            RegisterGraphicsParameter("SpoilerAngle", vehicleData.Graphics.SpoilerAngle, 0f, 45f, "Body");

            // Material & Wear
            RegisterGraphicsParameter("WearAmount", vehicleData.Graphics.WearAmount, 0f, 1f, "Materials");
            RegisterGraphicsParameter("DirtAccumulation", vehicleData.Graphics.DirtAccumulation, 0f, 1f, "Materials");
            RegisterGraphicsParameter("RustAmount", vehicleData.Graphics.RustAmount, 0f, 1f, "Materials");

            // Effects
            RegisterGraphicsParameter("MotionBlurIntensity", vehicleData.Graphics.MotionBlurIntensity, 0f, 1f, "Effects");
        }

        /// <summary>
        /// Register a physics parameter with change callback.
        /// </summary>
        private void RegisterPhysicsParameter(string name, float value, float min, float max, string category)
        {
            TuneParameter param = new TuneParameter(name, value, min, max, category);
            param.OnValueChanged += (newValue) => OnPhysicsParameterChanged?.Invoke(name, newValue);
            physicsParameters[name] = param;
        }

        /// <summary>
        /// Register a graphics parameter with change callback.
        /// </summary>
        private void RegisterGraphicsParameter(string name, float value, float min, float max, string category)
        {
            TuneParameter param = new TuneParameter(name, value, min, max, category);
            param.OnValueChanged += (newValue) => OnGraphicsParameterChanged?.Invoke(name, newValue);
            graphicsParameters[name] = param;
        }

        /// <summary>
        /// Get a physics parameter by name.
        /// </summary>
        public TuneParameter GetPhysicsParameter(string name)
        {
            if (physicsParameters.TryGetValue(name, out var param))
                return param;

            Debug.LogWarning($"Physics parameter '{name}' not found!");
            return null;
        }

        /// <summary>
        /// Get a graphics parameter by name.
        /// </summary>
        public TuneParameter GetGraphicsParameter(string name)
        {
            if (graphicsParameters.TryGetValue(name, out var param))
                return param;

            Debug.LogWarning($"Graphics parameter '{name}' not found!");
            return null;
        }

        /// <summary>
        /// Get all physics parameters as a copy of the dictionary.
        /// </summary>
        public Dictionary<string, TuneParameter> GetAllPhysicsParameters()
        {
            return new Dictionary<string, TuneParameter>(physicsParameters);
        }

        /// <summary>
        /// Get all graphics parameters as a copy of the dictionary.
        /// </summary>
        public Dictionary<string, TuneParameter> GetAllGraphicsParameters()
        {
            return new Dictionary<string, TuneParameter>(graphicsParameters);
        }

        /// <summary>
        /// Set a physics parameter value.
        /// </summary>
        public void SetPhysicsParameter(string name, float value)
        {
            var param = GetPhysicsParameter(name);
            if (param != null)
            {
                param.SetValue(value);
                vehicleData.MarkModified();
            }
        }

        /// <summary>
        /// Set a graphics parameter value.
        /// </summary>
        public void SetGraphicsParameter(string name, float value)
        {
            var param = GetGraphicsParameter(name);
            if (param != null)
            {
                param.SetValue(value);
                vehicleData.MarkModified();
            }
        }

        /// <summary>
        /// Reset all parameters to default values.
        /// </summary>
        public void ResetAllParameters()
        {
            foreach (var param in physicsParameters.Values)
                param.ResetToDefault();

            foreach (var param in graphicsParameters.Values)
                param.ResetToDefault();

            OnAllParametersUpdated?.Invoke();
            vehicleData.MarkModified();
        }

        /// <summary>
        /// Reset a specific category of parameters.
        /// </summary>
        public void ResetParameterCategory(string category)
        {
            foreach (var param in physicsParameters.Values)
                if (param.Category == category)
                    param.ResetToDefault();

            foreach (var param in graphicsParameters.Values)
                if (param.Category == category)
                    param.ResetToDefault();

            vehicleData.MarkModified();
        }

        public VehicleData GetVehicleData() => vehicleData;
        public void SetVehicleData(VehicleData data) => vehicleData = data;
        public static TuningManager Instance => instance;
    }
}
