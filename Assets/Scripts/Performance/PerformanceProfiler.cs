using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

namespace SendIt.Performance
{
    /// <summary>
    /// Performance profiler for Phase 6 optimization and analysis.
    /// Tracks frame time, memory usage, and system-specific bottlenecks.
    /// </summary>
    public class PerformanceProfiler : MonoBehaviour
    {
        // Timing data
        private Dictionary<string, PerformanceData> systemMetrics = new Dictionary<string, PerformanceData>();
        private Queue<float> frameTimeHistory = new Queue<float>();
        private Queue<float> memoryHistory = new Queue<float>();

        private const int maxHistoryPoints = 300; // 5 seconds at 60 FPS
        private float totalFrameTime;
        private int frameCount;

        // Performance thresholds
        private float targetFrameTime = 16.67f; // 60 FPS
        private float warningThreshold = 25f;   // 40 FPS warning
        private float criticalThreshold = 50f;  // 20 FPS critical

        private bool isEnabled = true;
        private bool showDebugUI = false;

        public struct PerformanceData
        {
            public float LastCallTime;
            public float AverageCallTime;
            public float PeakCallTime;
            public float MinCallTime;
            public int CallCount;
            public Queue<float> FrameTimes;

            public PerformanceData(int historySize = 300)
            {
                LastCallTime = 0f;
                AverageCallTime = 0f;
                PeakCallTime = 0f;
                MinCallTime = float.MaxValue;
                CallCount = 0;
                FrameTimes = new Queue<float>(historySize);
            }
        }

        public static PerformanceProfiler Instance { get; private set; }

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

        private void Update()
        {
            if (!isEnabled)
                return;

            // Track frame time
            float frameTime = Time.deltaTime * 1000f; // Convert to milliseconds
            totalFrameTime += frameTime;
            frameCount++;

            AddToHistory(frameTimeHistory, frameTime);
            AddToHistory(memoryHistory, GetMemoryUsage());

            // Debug display
            if (showDebugUI)
            {
                UpdateDebugDisplay();
            }
        }

        /// <summary>
        /// Begin timing a system operation.
        /// </summary>
        public void BeginProfiling(string systemName)
        {
            if (!isEnabled || !systemMetrics.ContainsKey(systemName))
            {
                if (!systemMetrics.ContainsKey(systemName))
                {
                    systemMetrics[systemName] = new PerformanceData();
                }
            }
        }

        /// <summary>
        /// End timing and record system operation duration.
        /// </summary>
        public void EndProfiling(string systemName, float duration)
        {
            if (!isEnabled || !systemMetrics.ContainsKey(systemName))
                return;

            var data = systemMetrics[systemName];
            data.LastCallTime = duration;
            data.CallCount++;
            data.PeakCallTime = Mathf.Max(data.PeakCallTime, duration);
            data.MinCallTime = Mathf.Min(data.MinCallTime, duration);

            // Update average
            float totalTime = 0f;
            foreach (float time in data.FrameTimes)
            {
                totalTime += time;
            }
            data.AverageCallTime = totalTime / Mathf.Max(data.FrameTimes.Count, 1);

            // Add to history
            AddToHistory(data.FrameTimes, duration);

            systemMetrics[systemName] = data;
        }

        /// <summary>
        /// Add value to history with size limit.
        /// </summary>
        private void AddToHistory(Queue<float> history, float value)
        {
            history.Enqueue(value);
            if (history.Count > maxHistoryPoints)
            {
                history.Dequeue();
            }
        }

        /// <summary>
        /// Get average frame time (milliseconds).
        /// </summary>
        public float GetAverageFrameTime()
        {
            if (frameCount == 0)
                return 0f;

            return totalFrameTime / frameCount;
        }

        /// <summary>
        /// Get average FPS from frame time.
        /// </summary>
        public float GetAverageFPS()
        {
            float avgFrameTime = GetAverageFrameTime();
            if (avgFrameTime <= 0f)
                return 0f;

            return 1000f / avgFrameTime;
        }

        /// <summary>
        /// Get current FPS.
        /// </summary>
        public float GetCurrentFPS()
        {
            if (Time.deltaTime <= 0f)
                return 0f;

            return 1f / Time.deltaTime;
        }

        /// <summary>
        /// Get memory usage in MB.
        /// </summary>
        public float GetMemoryUsage()
        {
            return System.GC.GetTotalMemory(false) / (1024f * 1024f);
        }

        /// <summary>
        /// Get performance rating (0-100, 100 is perfect).
        /// </summary>
        public float GetPerformanceRating()
        {
            float avgFrameTime = GetAverageFrameTime();

            if (avgFrameTime <= targetFrameTime)
                return 100f;
            if (avgFrameTime <= warningThreshold)
                return 75f + (25f * (1f - (avgFrameTime - targetFrameTime) / (warningThreshold - targetFrameTime)));
            if (avgFrameTime <= criticalThreshold)
                return 25f + (50f * (1f - (avgFrameTime - warningThreshold) / (criticalThreshold - warningThreshold)));

            return Mathf.Max(0f, 25f - ((avgFrameTime - criticalThreshold) / criticalThreshold) * 25f);
        }

        /// <summary>
        /// Get performance status text.
        /// </summary>
        public string GetPerformanceStatus()
        {
            float rating = GetPerformanceRating();

            if (rating >= 90f)
                return "EXCELLENT";
            if (rating >= 75f)
                return "GOOD";
            if (rating >= 50f)
                return "OK";
            if (rating >= 25f)
                return "WARNING";

            return "CRITICAL";
        }

        /// <summary>
        /// Get system-specific performance data.
        /// </summary>
        public PerformanceData GetSystemMetrics(string systemName)
        {
            if (systemMetrics.ContainsKey(systemName))
                return systemMetrics[systemName];

            return new PerformanceData();
        }

        /// <summary>
        /// Get all system metrics.
        /// </summary>
        public Dictionary<string, PerformanceData> GetAllMetrics()
        {
            return systemMetrics;
        }

        /// <summary>
        /// Find bottleneck (slowest system).
        /// </summary>
        public (string systemName, float avgTime) FindBottleneck()
        {
            string slowestSystem = "";
            float slowestTime = 0f;

            foreach (var kvp in systemMetrics)
            {
                if (kvp.Value.AverageCallTime > slowestTime)
                {
                    slowestTime = kvp.Value.AverageCallTime;
                    slowestSystem = kvp.Key;
                }
            }

            return (slowestSystem, slowestTime);
        }

        /// <summary>
        /// Generate performance report.
        /// </summary>
        public string GenerateReport()
        {
            string report = $@"
=== PERFORMANCE REPORT ===
Average FPS: {GetAverageFPS():F1}
Current FPS: {GetCurrentFPS():F1}
Average Frame Time: {GetAverageFrameTime():F2}ms
Performance Rating: {GetPerformanceRating():F0}/100 ({GetPerformanceStatus()})

Memory Usage: {GetMemoryUsage():F1} MB

=== SYSTEM METRICS ===
";

            var bottleneck = FindBottleneck();
            report += $"Bottleneck: {bottleneck.systemName} ({bottleneck.avgTime:F2}ms)\n\n";

            foreach (var kvp in systemMetrics)
            {
                var data = kvp.Value;
                report += $"{kvp.Key}:\n";
                report += $"  Average: {data.AverageCallTime:F2}ms\n";
                report += $"  Peak: {data.PeakCallTime:F2}ms\n";
                report += $"  Min: {data.MinCallTime:F2}ms\n";
                report += $"  Calls: {data.CallCount}\n\n";
            }

            return report;
        }

        /// <summary>
        /// Reset all metrics.
        /// </summary>
        public void ResetMetrics()
        {
            systemMetrics.Clear();
            frameTimeHistory.Clear();
            memoryHistory.Clear();
            totalFrameTime = 0f;
            frameCount = 0;
        }

        /// <summary>
        /// Toggle debug UI display.
        /// </summary>
        public void ToggleDebugUI()
        {
            showDebugUI = !showDebugUI;
        }

        /// <summary>
        /// Update debug display (called when UI is enabled).
        /// </summary>
        private void UpdateDebugDisplay()
        {
            // This would update debug text overlay
            // Implementation depends on available Text component
        }

        /// <summary>
        /// Enable/disable profiling.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        public bool IsEnabled => isEnabled;
        public float CurrentFrameTime => Time.deltaTime * 1000f;
    }
}
