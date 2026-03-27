using UnityEngine;
using System.Collections.Generic;
using SendIt.Physics;
using SendIt.Tuning;
using SendIt.Data;

namespace SendIt.AI
{
    /// <summary>
    /// AI-powered tuning advisor that analyzes driving data and recommends adjustments.
    /// Uses performance metrics to identify bottlenecks and suggest improvements.
    /// </summary>
    public class AITuningAdvisor : MonoBehaviour
    {
        [SerializeField] private TuningManager tuningManager;
        [SerializeField] private VehicleController vehicleController;

        // Performance thresholds for analysis
        private float optimalSlipRatio = 0.15f;
        private float optimalTireTemp = 85f;
        private float targetTraction = 0.95f;
        private float targetStability = 0.8f;

        // Historical data for analysis
        private PerformanceSession currentSession;
        private List<PerformanceSession> sessionHistory = new List<PerformanceSession>();

        // Recommendation engine
        private Dictionary<string, RecommendationEngine> recommendations = new Dictionary<string, RecommendationEngine>();

        private bool isAnalyzing = false;

        public struct PerformanceSession
        {
            public float SessionTime;
            public float AverageSpeed;
            public float MaxSpeed;
            public float AverageTireTemp;
            public float MaxTireTemp;
            public float AverageSlip;
            public float MaxSlip;
            public float TractionLossCount;
            public float SpinOutCount;
            public float CorneringGForce;
            public string TrackName;
            public float LapTime;
            public Dictionary<string, float> TuningParameters;
        }

        public struct TuningRecommendation
        {
            public string ParameterName;
            public float CurrentValue;
            public float RecommendedValue;
            public float ConfidenceLevel;  // 0-1
            public string Reason;
            public float ExpectedImprovement; // Percentage improvement
        }

        private struct RecommendationEngine
        {
            public float Confidence;
            public string Suggestion;
            public float[] OptimalRange;
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (tuningManager == null)
                tuningManager = TuningManager.Instance;

            currentSession = new PerformanceSession
            {
                TuningParameters = new Dictionary<string, float>()
            };

            InitializeRecommendationEngines();
        }

        /// <summary>
        /// Initialize AI recommendation engines for each system.
        /// </summary>
        private void InitializeRecommendationEngines()
        {
            // Engine tuning recommendations
            recommendations["MaxRPM"] = new RecommendationEngine
            {
                Confidence = 0.8f,
                Suggestion = "Increase for higher top speed",
                OptimalRange = new float[] { 6000f, 8000f }
            };

            recommendations["Horsepower"] = new RecommendationEngine
            {
                Confidence = 0.85f,
                Suggestion = "Increase for better acceleration",
                OptimalRange = new float[] { 300f, 500f }
            };

            // Suspension tuning
            recommendations["SpringStiffness"] = new RecommendationEngine
            {
                Confidence = 0.75f,
                Suggestion = "Stiffer for better cornering, softer for comfort",
                OptimalRange = new float[] { 1.0f, 3.0f }
            };

            // Tire tuning
            recommendations["TireGripCoefficient"] = new RecommendationEngine
            {
                Confidence = 0.8f,
                Suggestion = "Higher grip for better acceleration and braking",
                OptimalRange = new float[] { 0.8f, 1.2f }
            };

            // Aerodynamics
            recommendations["DragCoefficient"] = new RecommendationEngine
            {
                Confidence = 0.7f,
                Suggestion = "Lower for higher top speed",
                OptimalRange = new float[] { 0.2f, 0.4f }
            };

            recommendations["DownforceCoefficient"] = new RecommendationEngine
            {
                Confidence = 0.75f,
                Suggestion = "Higher for better stability at high speeds",
                OptimalRange = new float[] { 0.1f, 0.4f }
            };
        }

        /// <summary>
        /// Analyze current driving session and generate recommendations.
        /// </summary>
        public List<TuningRecommendation> AnalyzeSession()
        {
            List<TuningRecommendation> recommendations = new List<TuningRecommendation>();

            // Collect current session data
            CollectSessionData();

            // Analyze engine performance
            if (currentSession.AverageSlip > optimalSlipRatio * 1.2f)
            {
                recommendations.Add(new TuningRecommendation
                {
                    ParameterName = "Horsepower",
                    CurrentValue = tuningManager.GetPhysicsParameter("HorsePower").CurrentValue,
                    RecommendedValue = tuningManager.GetPhysicsParameter("HorsePower").CurrentValue * 1.1f,
                    ConfidenceLevel = 0.8f,
                    Reason = "Excessive wheel spin detected. Increase power delivery smoothness.",
                    ExpectedImprovement = 5f
                });
            }

            // Analyze tire temperature
            if (currentSession.MaxTireTemp > optimalTireTemp + 20f)
            {
                recommendations.Add(new TuningRecommendation
                {
                    ParameterName = "DragCoefficient",
                    CurrentValue = tuningManager.GetPhysicsParameter("DragCoefficient").CurrentValue,
                    RecommendedValue = tuningManager.GetPhysicsParameter("DragCoefficient").CurrentValue * 1.05f,
                    ConfidenceLevel = 0.7f,
                    Reason = "Tire overheating. Increase cooling through aerodynamics.",
                    ExpectedImprovement = 3f
                });
            }

            // Analyze stability
            float slipVariance = currentSession.MaxSlip - currentSession.AverageSlip;
            if (slipVariance > 0.3f)
            {
                recommendations.Add(new TuningRecommendation
                {
                    ParameterName = "SpringStiffness",
                    CurrentValue = tuningManager.GetPhysicsParameter("SpringStiffness").CurrentValue,
                    RecommendedValue = tuningManager.GetPhysicsParameter("SpringStiffness").CurrentValue * 1.2f,
                    ConfidenceLevel = 0.75f,
                    Reason = "Unstable suspension behavior. Increase spring stiffness for consistency.",
                    ExpectedImprovement = 8f
                });
            }

            // Analyze cornering performance
            if (currentSession.CorneringGForce < 0.8f)
            {
                recommendations.Add(new TuningRecommendation
                {
                    ParameterName = "DownforceCoefficient",
                    CurrentValue = tuningManager.GetPhysicsParameter("DownforceCoefficient").CurrentValue,
                    RecommendedValue = tuningManager.GetPhysicsParameter("DownforceCoefficient").CurrentValue * 1.3f,
                    ConfidenceLevel = 0.8f,
                    Reason = "Poor cornering performance. Increase downforce for grip.",
                    ExpectedImprovement = 12f
                });
            }

            return recommendations;
        }

        /// <summary>
        /// Collect performance data from current session.
        /// </summary>
        private void CollectSessionData()
        {
            var telemetry = vehicleController.GetTelemetry();
            if (telemetry == null)
                return;

            var frame = telemetry.GetLatestFrame();

            // Update session metrics
            currentSession.SessionTime += Time.deltaTime;
            currentSession.AverageSpeed = Mathf.Lerp(currentSession.AverageSpeed, frame.Speed, 0.01f);
            currentSession.MaxSpeed = Mathf.Max(currentSession.MaxSpeed, frame.Speed);

            // Tire metrics
            float avgTemp = 0;
            float avgSlip = 0;
            for (int i = 0; i < 4; i++)
            {
                avgTemp += frame.TireTemperatures[i];
                avgSlip += Mathf.Abs(frame.SlipRatios[i]);
            }
            avgTemp /= 4f;
            avgSlip /= 4f;

            currentSession.AverageTireTemp = Mathf.Lerp(currentSession.AverageTireTemp, avgTemp, 0.01f);
            currentSession.MaxTireTemp = Mathf.Max(currentSession.MaxTireTemp, avgTemp);
            currentSession.AverageSlip = Mathf.Lerp(currentSession.AverageSlip, avgSlip, 0.01f);
            currentSession.MaxSlip = Mathf.Max(currentSession.MaxSlip, avgSlip);

            // Dynamics metrics
            currentSession.CorneringGForce = Mathf.Max(currentSession.CorneringGForce,
                Mathf.Abs(frame.LateralAcceleration) / 9.81f);
        }

        /// <summary>
        /// Apply a tuning recommendation.
        /// </summary>
        public void ApplyRecommendation(TuningRecommendation recommendation)
        {
            if (tuningManager != null)
            {
                var parameter = tuningManager.GetPhysicsParameter(recommendation.ParameterName);
                if (parameter != null)
                {
                    tuningManager.SetPhysicsParameter(recommendation.ParameterName, recommendation.RecommendedValue);
                    Debug.Log($"Applied recommendation: {recommendation.ParameterName} = {recommendation.RecommendedValue:F2}");
                }
            }
        }

        /// <summary>
        /// Get AI confidence score for current setup (0-100).
        /// </summary>
        public float GetSetupOptimalityScore()
        {
            float score = 50f; // Base score

            // Check tire temperature
            if (currentSession.AverageTireTemp > optimalTireTemp - 10f &&
                currentSession.AverageTireTemp < optimalTireTemp + 10f)
            {
                score += 15f;
            }

            // Check slip ratio
            if (currentSession.AverageSlip > optimalSlipRatio * 0.8f &&
                currentSession.AverageSlip < optimalSlipRatio * 1.2f)
            {
                score += 15f;
            }

            // Check stability
            float slipVariance = currentSession.MaxSlip - currentSession.AverageSlip;
            if (slipVariance < 0.3f)
            {
                score += 20f;
            }

            // Check speed achieved
            score += Mathf.Clamp(currentSession.MaxSpeed / 50f * 15f, 0f, 15f);

            return Mathf.Clamp01(score / 100f) * 100f;
        }

        /// <summary>
        /// Get AI rating for a specific parameter (0-100).
        /// </summary>
        public float RateParameter(string parameterName)
        {
            var param = tuningManager.GetPhysicsParameter(parameterName);
            if (param == null)
                return 50f;

            if (recommendations.ContainsKey(parameterName))
            {
                var engine = recommendations[parameterName];
                float optMin = engine.OptimalRange[0];
                float optMax = engine.OptimalRange[1];
                float current = param.CurrentValue;

                if (current >= optMin && current <= optMax)
                    return 100f;

                float distance = Mathf.Min(Mathf.Abs(current - optMin), Mathf.Abs(current - optMax));
                float rangeWidth = optMax - optMin;
                return Mathf.Max(0f, 100f - (distance / rangeWidth) * 100f);
            }

            return 50f;
        }

        /// <summary>
        /// Save current session to history.
        /// </summary>
        public void SaveSession(string trackName, float lapTime)
        {
            currentSession.TrackName = trackName;
            currentSession.LapTime = lapTime;
            currentSession.TuningParameters = new Dictionary<string, float>(
                tuningManager.GetAllPhysicsParameters()
            );

            sessionHistory.Add(currentSession);

            // Reset for next session
            currentSession = new PerformanceSession
            {
                TuningParameters = new Dictionary<string, float>()
            };
        }

        /// <summary>
        /// Get setup recommendations based on track type.
        /// </summary>
        public Dictionary<string, float> GetTrackSpecificSetup(string trackType)
        {
            Dictionary<string, float> setup = new Dictionary<string, float>();

            switch (trackType.ToLower())
            {
                case "high_speed_circuit":
                    setup["DownforceCoefficient"] = 0.35f;
                    setup["DragCoefficient"] = 0.25f;
                    setup["SpringStiffness"] = 2.5f;
                    break;

                case "tight_technical":
                    setup["DownforceCoefficient"] = 0.25f;
                    setup["DragCoefficient"] = 0.35f;
                    setup["SpringStiffness"] = 1.8f;
                    break;

                case "balanced":
                    setup["DownforceCoefficient"] = 0.3f;
                    setup["DragCoefficient"] = 0.3f;
                    setup["SpringStiffness"] = 2.0f;
                    break;

                case "wet_weather":
                    setup["TireGripCoefficient"] = 1.0f;
                    setup["DragCoefficient"] = 0.32f;
                    setup["SpringStiffness"] = 2.2f;
                    break;
            }

            return setup;
        }

        public float GetAverageSetupScore()
        {
            float totalScore = 0f;
            foreach (var param in tuningManager.GetAllPhysicsParameters())
            {
                totalScore += RateParameter(param.Key);
            }

            int paramCount = tuningManager.GetAllPhysicsParameters().Count;
            return paramCount > 0 ? totalScore / paramCount : 50f;
        }

        public List<PerformanceSession> GetSessionHistory() => sessionHistory;
    }
}
