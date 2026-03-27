using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SendIt.Physics;

namespace SendIt.UI
{
    /// <summary>
    /// Real-time performance graph display system for Phase 3.
    /// Visualizes vehicle telemetry data with interactive graphs and charts.
    /// </summary>
    public class PerformanceGraphDisplay : MonoBehaviour
    {
        [SerializeField] private RawImage graphDisplay;
        [SerializeField] private Dropdown graphTypeDropdown;
        [SerializeField] private Text graphTitleLabel;
        [SerializeField] private Slider timeScaleSlider;
        [SerializeField] private Text[] dataPointLabels = new Text[4];

        // Graph configuration
        private int graphWidth = 512;
        private int graphHeight = 256;
        private Texture2D graphTexture;

        // Data storage
        private Queue<float> engineRPMHistory = new Queue<float>();
        private Queue<float> speedHistory = new Queue<float>();
        private Queue<float> tireTemperatureHistory = new Queue<float>();
        private Queue<float> powerHistory = new Queue<float>();
        private Queue<float> slipHistory = new Queue<float>();
        private Queue<float> suspensionLoadHistory = new Queue<float>();

        private const int maxHistoryPoints = 512;
        private float timescale = 1f;

        // Graph types
        public enum GraphType
        {
            EngineRPM,
            Speed,
            TireTemperature,
            Power,
            Slip,
            SuspensionLoad,
            Combined
        }
        private GraphType currentGraphType = GraphType.EngineRPM;

        // Colors for different data series
        private Color[] graphColors = new Color[]
        {
            new Color(1f, 0.5f, 0f),      // Orange
            new Color(0.5f, 1f, 0f),      // Green
            new Color(1f, 0f, 0f),        // Red
            new Color(0f, 0.5f, 1f)       // Blue
        };

        private bool isInitialized;

        public void Initialize()
        {
            // Create graph texture
            graphTexture = new Texture2D(graphWidth, graphHeight, TextureFormat.RGB24, false);
            graphTexture.filterMode = FilterMode.Point;

            if (graphDisplay != null)
            {
                graphDisplay.texture = graphTexture;
            }

            // Setup dropdown
            if (graphTypeDropdown != null)
            {
                graphTypeDropdown.onValueChanged.AddListener(OnGraphTypeChanged);
            }

            // Setup time scale slider
            if (timeScaleSlider != null)
            {
                timeScaleSlider.minValue = 0.1f;
                timeScaleSlider.maxValue = 5f;
                timeScaleSlider.value = 1f;
                timeScaleSlider.onValueChanged.AddListener(OnTimeScaleChanged);
            }

            ClearHistories();
            isInitialized = true;
        }

        /// <summary>
        /// Add data point to appropriate history queue.
        /// </summary>
        public void AddDataPoint(float rpm, float speed, float tireTemp, float power, float slip, float suspensionLoad)
        {
            if (!isInitialized)
                return;

            // Add to histories
            AddToHistory(engineRPMHistory, rpm);
            AddToHistory(speedHistory, speed);
            AddToHistory(tireTemperatureHistory, tireTemp);
            AddToHistory(powerHistory, power);
            AddToHistory(slipHistory, slip);
            AddToHistory(suspensionLoadHistory, suspensionLoad);

            // Update display
            UpdateGraphDisplay();
        }

        /// <summary>
        /// Add value to history queue with size limit.
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
        /// Update graph display with current data.
        /// </summary>
        private void UpdateGraphDisplay()
        {
            // Clear texture
            Color[] pixels = new Color[graphWidth * graphHeight];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.black;
            }

            // Draw grid
            DrawGrid(pixels);

            // Draw appropriate graph
            switch (currentGraphType)
            {
                case GraphType.EngineRPM:
                    DrawSingleGraph(pixels, engineRPMHistory, Color.yellow, "Engine RPM");
                    break;
                case GraphType.Speed:
                    DrawSingleGraph(pixels, speedHistory, new Color(0f, 1f, 0f), "Speed (m/s)");
                    break;
                case GraphType.TireTemperature:
                    DrawSingleGraph(pixels, tireTemperatureHistory, Color.red, "Tire Temperature (°C)");
                    break;
                case GraphType.Power:
                    DrawSingleGraph(pixels, powerHistory, new Color(1f, 0.5f, 0f), "Power (kW)");
                    break;
                case GraphType.Slip:
                    DrawSingleGraph(pixels, slipHistory, new Color(0.5f, 1f, 0f), "Slip Ratio");
                    break;
                case GraphType.SuspensionLoad:
                    DrawSingleGraph(pixels, suspensionLoadHistory, Color.cyan, "Suspension Load (N)");
                    break;
                case GraphType.Combined:
                    DrawCombinedGraph(pixels);
                    break;
            }

            // Apply to texture
            graphTexture.SetPixels(pixels);
            graphTexture.Apply();
        }

        /// <summary>
        /// Draw single data series graph.
        /// </summary>
        private void DrawSingleGraph(Color[] pixels, Queue<float> history, Color graphColor, string title)
        {
            if (graphTitleLabel != null)
                graphTitleLabel.text = title;

            if (history.Count < 2)
                return;

            float[] dataArray = history.ToArray();
            float maxValue = Mathf.Max(dataArray);
            float minValue = Mathf.Min(dataArray);
            float range = maxValue - minValue;

            if (range < 0.1f)
                range = 0.1f; // Prevent division by zero

            // Draw line graph
            for (int i = 1; i < dataArray.Length; i++)
            {
                float x1 = Mathf.Lerp(0, graphWidth - 1, (float)(i - 1) / (dataArray.Length - 1));
                float x2 = Mathf.Lerp(0, graphWidth - 1, (float)i / (dataArray.Length - 1));

                float y1 = Mathf.Lerp(0, graphHeight - 1, (dataArray[i - 1] - minValue) / range);
                float y2 = Mathf.Lerp(0, graphHeight - 1, (dataArray[i] - minValue) / range);

                DrawLine(pixels, (int)x1, (int)y1, (int)x2, (int)y2, graphColor);
            }

            // Draw data point labels
            for (int i = 0; i < Mathf.Min(4, dataPointLabels.Length); i++)
            {
                if (dataPointLabels[i] != null && i < dataArray.Length)
                {
                    float percent = Mathf.Clamp01((float)i / 3f);
                    int index = (int)Mathf.Lerp(0, dataArray.Length - 1, percent);
                    dataPointLabels[i].text = $"[{i}]: {dataArray[index]:F1}";
                }
            }
        }

        /// <summary>
        /// Draw multiple data series combined.
        /// </summary>
        private void DrawCombinedGraph(Color[] pixels)
        {
            if (graphTitleLabel != null)
                graphTitleLabel.text = "Combined Performance";

            // Normalize all data to 0-1 range for combined display
            var speeds = NormalizeHistory(speedHistory, 0, 100);
            var temps = NormalizeHistory(tireTemperatureHistory, 20, 130);
            var slips = slipHistory.ToArray();

            // Draw speed (green)
            DrawNormalizedGraph(pixels, speeds, Color.green);

            // Draw temperature (red)
            DrawNormalizedGraph(pixels, temps, Color.red);

            // Draw slip (blue)
            DrawNormalizedGraph(pixels, slips, Color.blue);
        }

        /// <summary>
        /// Normalize history data to 0-1 range.
        /// </summary>
        private float[] NormalizeHistory(Queue<float> history, float min, float max)
        {
            float[] data = history.ToArray();
            float[] normalized = new float[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                normalized[i] = Mathf.Clamp01((data[i] - min) / (max - min));
            }

            return normalized;
        }

        /// <summary>
        /// Draw normalized (0-1) graph.
        /// </summary>
        private void DrawNormalizedGraph(Color[] pixels, float[] data, Color color)
        {
            if (data.Length < 2)
                return;

            for (int i = 1; i < data.Length; i++)
            {
                float x1 = Mathf.Lerp(0, graphWidth - 1, (float)(i - 1) / (data.Length - 1));
                float x2 = Mathf.Lerp(0, graphWidth - 1, (float)i / (data.Length - 1));

                float y1 = Mathf.Lerp(0, graphHeight - 1, data[i - 1]);
                float y2 = Mathf.Lerp(0, graphHeight - 1, data[i]);

                DrawLine(pixels, (int)x1, (int)y1, (int)x2, (int)y2, color);
            }
        }

        /// <summary>
        /// Draw grid on graph.
        /// </summary>
        private void DrawGrid(Color[] pixels)
        {
            Color gridColor = new Color(0.2f, 0.2f, 0.2f);

            // Vertical lines (every 64 pixels)
            for (int x = 0; x < graphWidth; x += 64)
            {
                for (int y = 0; y < graphHeight; y++)
                {
                    int idx = y * graphWidth + x;
                    if (idx >= 0 && idx < pixels.Length)
                        pixels[idx] = gridColor;
                }
            }

            // Horizontal lines (every 32 pixels)
            for (int y = 0; y < graphHeight; y += 32)
            {
                for (int x = 0; x < graphWidth; x++)
                {
                    int idx = y * graphWidth + x;
                    if (idx >= 0 && idx < pixels.Length)
                        pixels[idx] = gridColor;
                }
            }
        }

        /// <summary>
        /// Draw line between two points (Bresenham's algorithm).
        /// </summary>
        private void DrawLine(Color[] pixels, int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                if (x0 >= 0 && x0 < graphWidth && y0 >= 0 && y0 < graphHeight)
                {
                    int idx = y0 * graphWidth + x0;
                    if (idx >= 0 && idx < pixels.Length)
                        pixels[idx] = color;
                }

                if (x0 == x1 && y0 == y1)
                    break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        /// <summary>
        /// Handle graph type change.
        /// </summary>
        private void OnGraphTypeChanged(int index)
        {
            currentGraphType = (GraphType)index;
        }

        /// <summary>
        /// Handle time scale change.
        /// </summary>
        private void OnTimeScaleChanged(float value)
        {
            timescale = value;
        }

        /// <summary>
        /// Clear all history data.
        /// </summary>
        public void ClearHistories()
        {
            engineRPMHistory.Clear();
            speedHistory.Clear();
            tireTemperatureHistory.Clear();
            powerHistory.Clear();
            slipHistory.Clear();
            suspensionLoadHistory.Clear();
        }

        /// <summary>
        /// Export graph data as CSV.
        /// </summary>
        public string ExportAsCSV()
        {
            string csv = "Frame,RPM,Speed,TireTemp,Power,Slip,SuspensionLoad\n";

            var rpmArray = engineRPMHistory.ToArray();
            var speedArray = speedHistory.ToArray();
            var tempArray = tireTemperatureHistory.ToArray();
            var powerArray = powerHistory.ToArray();
            var slipArray = slipHistory.ToArray();
            var loadArray = suspensionLoadHistory.ToArray();

            int maxLength = Mathf.Max(rpmArray.Length, speedArray.Length, tempArray.Length);

            for (int i = 0; i < maxLength; i++)
            {
                csv += $"{i},";
                csv += $"{(i < rpmArray.Length ? rpmArray[i] : 0)},";
                csv += $"{(i < speedArray.Length ? speedArray[i] : 0)},";
                csv += $"{(i < tempArray.Length ? tempArray[i] : 0)},";
                csv += $"{(i < powerArray.Length ? powerArray[i] : 0)},";
                csv += $"{(i < slipArray.Length ? slipArray[i] : 0)},";
                csv += $"{(i < loadArray.Length ? loadArray[i] : 0)}\n";
            }

            return csv;
        }

        public GraphType GetCurrentGraphType() => currentGraphType;
        public int GetHistoryCount() => engineRPMHistory.Count;
    }
}
