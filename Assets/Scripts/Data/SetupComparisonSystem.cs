using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SendIt.Tuning;
using SendIt.Physics;

namespace SendIt.Data
{
    /// <summary>
    /// Setup comparison system for analyzing vehicle configurations.
    /// Allows saving, comparing, and tracking performance differences between setups.
    /// </summary>
    public class SetupComparisonSystem : MonoBehaviour
    {
        public struct SavedSetup
        {
            public string SetupName;
            public string Description;
            public System.DateTime CreatedDate;
            public System.DateTime LastModified;
            public string TrackName;
            public float BestLapTime;
            public Dictionary<string, float> Parameters;
            public Dictionary<string, float> PerformanceMetrics;
            public int UseCount; // Times this setup was used
        }

        public struct SetupComparison
        {
            public SavedSetup Setup1;
            public SavedSetup Setup2;
            public Dictionary<string, float> ParameterDeltas;
            public Dictionary<string, PerformanceImpact> ImpactAnalysis;
            public float PerformanceDelta;
            public string OverallRecommendation;
        }

        public struct PerformanceImpact
        {
            public string ParameterName;
            public float CurrentValue;
            public float ComparisonValue;
            public float Delta;
            public float ImpactScore; // 0-1, how much this affects overall performance
            public string ImpactDirection; // "Better", "Worse", "Neutral"
            public string Recommendation;
        }

        [SerializeField] private TuningManager tuningManager;
        [SerializeField] private VehicleController vehicleController;

        private List<SavedSetup> savedSetups = new List<SavedSetup>();
        private const int maxSavedSetups = 20;

        // Impact weighting for different parameters
        private Dictionary<string, float> parameterImpactWeights = new Dictionary<string, float>
        {
            // Engine
            { "HorsePower", 0.12f },
            { "MaxRPM", 0.08f },
            { "ResponsivenessFactor", 0.06f },

            // Suspension
            { "SpringStiffness", 0.09f },
            { "DamperRatio", 0.07f },
            { "RideHeight", 0.03f },
            { "AntiRollBar", 0.05f },

            // Tires
            { "TireGripCoefficient", 0.13f },
            { "SlipAngleSensitivity", 0.08f },
            { "TemperatureSensitivity", 0.06f },
            { "WearRate", 0.04f },

            // Aerodynamics
            { "DragCoefficient", 0.10f },
            { "DownforceCoefficient", 0.11f },

            // Weight
            { "VehicleMass", 0.05f },
            { "WeightDistribution", 0.04f }
        };

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (tuningManager == null)
                tuningManager = TuningManager.Instance;

            LoadSetups();
        }

        /// <summary>
        /// Save current vehicle setup with metadata.
        /// </summary>
        public void SaveCurrentSetup(string setupName, string description, string trackName = "", float lapTime = 0f)
        {
            if (savedSetups.Count >= maxSavedSetups)
            {
                Debug.LogWarning("Maximum saved setups reached. Cannot save more.");
                return;
            }

            var setup = new SavedSetup
            {
                SetupName = setupName,
                Description = description,
                CreatedDate = System.DateTime.Now,
                LastModified = System.DateTime.Now,
                TrackName = trackName,
                BestLapTime = lapTime,
                Parameters = new Dictionary<string, float>(tuningManager.GetAllPhysicsParameters()),
                PerformanceMetrics = CollectPerformanceMetrics(),
                UseCount = 0
            };

            savedSetups.Add(setup);
            SaveSetupsToJSON();
            Debug.Log($"Setup '{setupName}' saved successfully.");
        }

        /// <summary>
        /// Load a saved setup into the vehicle.
        /// </summary>
        public bool LoadSetup(string setupName)
        {
            var setup = savedSetups.FirstOrDefault(s => s.SetupName == setupName);
            if (setup.SetupName == null)
            {
                Debug.LogWarning($"Setup '{setupName}' not found.");
                return false;
            }

            // Apply all parameters
            foreach (var param in setup.Parameters)
            {
                tuningManager.SetPhysicsParameter(param.Key, param.Value);
            }

            // Increment use count
            int index = savedSetups.FindIndex(s => s.SetupName == setupName);
            setup.UseCount++;
            setup.LastModified = System.DateTime.Now;
            savedSetups[index] = setup;
            SaveSetupsToJSON();

            Debug.Log($"Setup '{setupName}' loaded successfully.");
            return true;
        }

        /// <summary>
        /// Delete a saved setup.
        /// </summary>
        public bool DeleteSetup(string setupName)
        {
            int index = savedSetups.FindIndex(s => s.SetupName == setupName);
            if (index == -1)
            {
                Debug.LogWarning($"Setup '{setupName}' not found.");
                return false;
            }

            savedSetups.RemoveAt(index);
            SaveSetupsToJSON();
            Debug.Log($"Setup '{setupName}' deleted.");
            return true;
        }

        /// <summary>
        /// Duplicate a setup with a new name.
        /// </summary>
        public bool DuplicateSetup(string originalSetupName, string newSetupName)
        {
            var original = savedSetups.FirstOrDefault(s => s.SetupName == originalSetupName);
            if (original.SetupName == null)
            {
                Debug.LogWarning($"Setup '{originalSetupName}' not found.");
                return false;
            }

            if (savedSetups.Any(s => s.SetupName == newSetupName))
            {
                Debug.LogWarning($"Setup '{newSetupName}' already exists.");
                return false;
            }

            var duplicate = original;
            duplicate.SetupName = newSetupName;
            duplicate.CreatedDate = System.DateTime.Now;
            duplicate.LastModified = System.DateTime.Now;
            duplicate.UseCount = 0;
            duplicate.Parameters = new Dictionary<string, float>(original.Parameters);
            duplicate.PerformanceMetrics = new Dictionary<string, float>(original.PerformanceMetrics);

            savedSetups.Add(duplicate);
            SaveSetupsToJSON();
            Debug.Log($"Setup duplicated: '{originalSetupName}' → '{newSetupName}'");
            return true;
        }

        /// <summary>
        /// Compare two setups and analyze differences.
        /// </summary>
        public SetupComparison CompareSetups(string setupName1, string setupName2)
        {
            var setup1 = savedSetups.FirstOrDefault(s => s.SetupName == setupName1);
            var setup2 = savedSetups.FirstOrDefault(s => s.SetupName == setupName2);

            if (setup1.SetupName == null || setup2.SetupName == null)
            {
                Debug.LogError("One or both setups not found.");
                return new SetupComparison();
            }

            var comparison = new SetupComparison
            {
                Setup1 = setup1,
                Setup2 = setup2,
                ParameterDeltas = new Dictionary<string, float>(),
                ImpactAnalysis = new Dictionary<string, PerformanceImpact>()
            };

            // Calculate parameter deltas
            var allParams = new HashSet<string>();
            foreach (var key in setup1.Parameters.Keys) allParams.Add(key);
            foreach (var key in setup2.Parameters.Keys) allParams.Add(key);

            float totalImpact = 0f;
            foreach (var paramName in allParams)
            {
                float val1 = setup1.Parameters.ContainsKey(paramName) ? setup1.Parameters[paramName] : 0f;
                float val2 = setup2.Parameters.ContainsKey(paramName) ? setup2.Parameters[paramName] : 0f;
                float delta = val2 - val1;

                comparison.ParameterDeltas[paramName] = delta;

                // Analyze impact
                float impactWeight = parameterImpactWeights.ContainsKey(paramName) ? parameterImpactWeights[paramName] : 0.02f;
                float percentDelta = val1 != 0f ? Mathf.Abs(delta / val1) : Mathf.Abs(delta);
                float impactScore = Mathf.Min(1f, percentDelta * impactWeight * 10f);
                totalImpact += impactScore;

                string impactDirection = delta > 0.001f ? "Better" : (delta < -0.001f ? "Worse" : "Neutral");
                if (paramName.Contains("Weight") || paramName.Contains("Drag"))
                {
                    impactDirection = delta > 0.001f ? "Worse" : (delta < -0.001f ? "Better" : "Neutral");
                }

                comparison.ImpactAnalysis[paramName] = new PerformanceImpact
                {
                    ParameterName = paramName,
                    CurrentValue = val1,
                    ComparisonValue = val2,
                    Delta = delta,
                    ImpactScore = impactScore,
                    ImpactDirection = impactDirection,
                    Recommendation = GenerateParameterRecommendation(paramName, val1, val2, delta)
                };
            }

            // Calculate overall performance delta
            if (setup1.PerformanceMetrics.ContainsKey("BestLapTime") && setup2.PerformanceMetrics.ContainsKey("BestLapTime"))
            {
                float time1 = setup1.PerformanceMetrics["BestLapTime"];
                float time2 = setup2.PerformanceMetrics["BestLapTime"];
                if (time1 > 0) comparison.PerformanceDelta = time2 - time1;
            }

            comparison.OverallRecommendation = GenerateOverallRecommendation(comparison);

            return comparison;
        }

        /// <summary>
        /// Get all saved setups.
        /// </summary>
        public List<SavedSetup> GetAllSetups()
        {
            return new List<SavedSetup>(savedSetups);
        }

        /// <summary>
        /// Get a specific setup by name.
        /// </summary>
        public SavedSetup GetSetup(string setupName)
        {
            return savedSetups.FirstOrDefault(s => s.SetupName == setupName);
        }

        /// <summary>
        /// Get setups sorted by performance (best first).
        /// </summary>
        public List<SavedSetup> GetSetupsByPerformance(string trackName = "")
        {
            var filtered = string.IsNullOrEmpty(trackName)
                ? new List<SavedSetup>(savedSetups)
                : savedSetups.Where(s => s.TrackName == trackName).ToList();

            return filtered.OrderBy(s => s.BestLapTime > 0 ? s.BestLapTime : float.MaxValue).ToList();
        }

        /// <summary>
        /// Get most used setups.
        /// </summary>
        public List<SavedSetup> GetMostUsedSetups(int count = 5)
        {
            return savedSetups.OrderByDescending(s => s.UseCount).Take(count).ToList();
        }

        /// <summary>
        /// Generate detailed comparison report.
        /// </summary>
        public string GenerateComparisonReport(SetupComparison comparison)
        {
            string report = $@"
=== SETUP COMPARISON REPORT ===
Setup 1: {comparison.Setup1.SetupName}
Setup 2: {comparison.Setup2.SetupName}
Created: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}

--- PERFORMANCE ---
Setup 1 Best Lap: {comparison.Setup1.BestLapTime:F3}s ({comparison.Setup1.TrackName})
Setup 2 Best Lap: {comparison.Setup2.BestLapTime:F3}s ({comparison.Setup2.TrackName})
Performance Delta: {comparison.PerformanceDelta:F3}s {(comparison.PerformanceDelta < 0 ? "(Setup 2 faster)" : "(Setup 1 faster)")}

--- PARAMETER CHANGES ---
";

            var sortedImpacts = comparison.ImpactAnalysis.Values.OrderByDescending(x => x.ImpactScore);
            foreach (var impact in sortedImpacts.Where(i => i.ImpactScore > 0.01f))
            {
                report += $@"
{impact.ParameterName}:
  {impact.CurrentValue:F4} → {impact.ComparisonValue:F4} (Δ {impact.Delta:+0.0000;-0.0000})
  Impact: {impact.ImpactScore * 100:F1}% | {impact.ImpactDirection}
  Recommendation: {impact.Recommendation}
";
            }

            report += $@"
--- OVERALL RECOMMENDATION ---
{comparison.OverallRecommendation}
";
            return report;
        }

        /// <summary>
        /// Collect current vehicle performance metrics.
        /// </summary>
        private Dictionary<string, float> CollectPerformanceMetrics()
        {
            var metrics = new Dictionary<string, float>();

            if (vehicleController != null)
            {
                var telemetry = vehicleController.GetTelemetry();
                if (telemetry != null)
                {
                    var frame = telemetry.GetLatestFrame();
                    metrics["Speed"] = frame.Speed;
                    metrics["RPM"] = frame.EngineRPM;
                    metrics["AverageTireTemp"] = (frame.TireTemperatures[0] + frame.TireTemperatures[1] +
                                                   frame.TireTemperatures[2] + frame.TireTemperatures[3]) / 4f;
                }
            }

            return metrics;
        }

        /// <summary>
        /// Generate recommendation for a specific parameter change.
        /// </summary>
        private string GenerateParameterRecommendation(string paramName, float oldValue, float newValue, float delta)
        {
            float percentChange = oldValue != 0f ? (delta / oldValue) * 100f : 0f;

            // Engine parameters
            if (paramName == "HorsePower")
                return percentChange > 5 ? "Significant power increase - expect better acceleration" : "Minor power adjustment";

            if (paramName == "MaxRPM")
                return percentChange > 5 ? "Higher rev limit - gains top speed potential" : "Rev limit adjustment";

            // Suspension parameters
            if (paramName == "SpringStiffness")
                return percentChange > 10 ? (percentChange > 0 ? "Stiffer springs improve responsiveness but reduce comfort" : "Softer springs increase comfort but reduce stability") : "Suspension fine-tuning";

            // Tire parameters
            if (paramName == "TireGripCoefficient")
                return percentChange > 5 ? (percentChange > 0 ? "Better grip for acceleration and braking" : "Lower grip reduces acceleration") : "Tire grip adjustment";

            // Aero parameters
            if (paramName == "DragCoefficient")
                return percentChange > 5 ? (percentChange > 0 ? "Increased drag reduces top speed" : "Reduced drag improves top speed") : "Drag coefficient adjustment";

            if (paramName == "DownforceCoefficient")
                return percentChange > 5 ? (percentChange > 0 ? "More downforce for high-speed stability" : "Less downforce reduces drag") : "Downforce adjustment";

            return "Parameter modified";
        }

        /// <summary>
        /// Generate overall recommendation based on comparison analysis.
        /// </summary>
        private string GenerateOverallRecommendation(SetupComparison comparison)
        {
            if (comparison.PerformanceDelta < 0)
            {
                return $"Setup 2 is {Mathf.Abs(comparison.PerformanceDelta):F3}s faster. The parameter changes have improved performance significantly.";
            }
            else if (comparison.PerformanceDelta > 0)
            {
                return $"Setup 1 is {Mathf.Abs(comparison.PerformanceDelta):F3}s faster. Consider reverting some changes or adjusting further.";
            }
            else
            {
                var highImpactChanges = comparison.ImpactAnalysis.Values.Where(x => x.ImpactScore > 0.1f).ToList();
                if (highImpactChanges.Count == 0)
                    return "Setups are very similar. No significant differences detected.";

                var betterCount = highImpactChanges.Count(x => x.ImpactDirection == "Better");
                var worseCount = highImpactChanges.Count(x => x.ImpactDirection == "Worse");

                if (betterCount > worseCount)
                    return "Setup 2 has more beneficial parameter changes, but performance impact data is unavailable.";
                else if (worseCount > betterCount)
                    return "Setup 1 appears more balanced. Setup 2 has made several detrimental changes.";
                else
                    return "Setups have mixed parameter changes. Further testing required to determine which is better.";
            }
        }

        /// <summary>
        /// Save setups to JSON file.
        /// </summary>
        private void SaveSetupsToJSON()
        {
            string jsonPath = System.IO.Path.Combine(Application.persistentDataPath, "setups.json");

            var setupData = new SetupListWrapper
            {
                setups = savedSetups
            };

            string json = JsonUtility.ToJson(setupData, true);
            System.IO.File.WriteAllText(jsonPath, json);
        }

        /// <summary>
        /// Load setups from JSON file.
        /// </summary>
        private void LoadSetupsFromJSON()
        {
            string jsonPath = System.IO.Path.Combine(Application.persistentDataPath, "setups.json");

            if (!System.IO.File.Exists(jsonPath))
                return;

            string json = System.IO.File.ReadAllText(jsonPath);
            var setupData = JsonUtility.FromJson<SetupListWrapper>(json);

            if (setupData != null && setupData.setups != null)
                savedSetups = new List<SavedSetup>(setupData.setups);
        }

        /// <summary>
        /// Helper class for JSON serialization.
        /// </summary>
        [System.Serializable]
        private class SetupListWrapper
        {
            public List<SavedSetup> setups = new List<SavedSetup>();
        }

        private void LoadSetups()
        {
            LoadSetupsFromJSON();
        }

        public int GetSetupCount() => savedSetups.Count;
        public int GetMaxSetups() => maxSavedSetups;
    }
}
