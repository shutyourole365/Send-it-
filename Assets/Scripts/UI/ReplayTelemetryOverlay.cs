using UnityEngine;
using UnityEngine.UI;
using SendIt.Gameplay;
using SendIt.Physics;

namespace SendIt.UI
{
    /// <summary>
    /// Overlay display for replay telemetry data.
    /// Shows real-time vehicle data during replay playback with customizable layout.
    /// </summary>
    public class ReplayTelemetryOverlay : MonoBehaviour
    {
        [SerializeField] private ReplaySystem replaySystem;
        [SerializeField] private VehicleController vehicleController;

        // Display UI Elements
        [SerializeField] private Text sessionNameText;
        [SerializeField] private Text playbackStatusText;
        [SerializeField] private Text currentTimeText;
        [SerializeField] private Text totalTimeText;
        [SerializeField] private Slider playbackProgressSlider;

        // Engine Telemetry
        [SerializeField] private Text rpmText;
        [SerializeField] private Text powerText;
        [SerializeField] private Text torqueText;
        [SerializeField] private Text speedText;
        [SerializeField] private Text gearText;

        // Tire Telemetry
        [SerializeField] private Text tireTempsText;
        [SerializeField] private Text tireWearText;
        [SerializeField] private Text tirePressuresText;
        [SerializeField] private Text slipRatiosText;
        [SerializeField] private Text slipAnglesText;

        // Dynamics Telemetry
        [SerializeField] private Text lateralAccelText;
        [SerializeField] private Text longitudinalAccelText;
        [SerializeField] private Text rollText;
        [SerializeField] private Text pitchText;
        [SerializeField] private Text yawText;

        // Lap Data
        [SerializeField] private Text lapTimeText;
        [SerializeField] private Text lapNumberText;
        [SerializeField] private Text distanceText;

        // Control UI
        [SerializeField] private Button playPauseButton;
        [SerializeField] private Button rewindButton;
        [SerializeField] private Button fastForwardButton;
        [SerializeField] private Button toggleOverlayButton;
        [SerializeField] private Slider playbackSpeedSlider;

        // Visibility toggles
        [SerializeField] private Toggle showEngineToggle;
        [SerializeField] private Toggle showTiresToggle;
        [SerializeField] private Toggle showDynamicsToggle;
        [SerializeField] private Toggle showLapDataToggle;

        private bool overlayVisible = true;
        private bool showEngineData = true;
        private bool showTireData = true;
        private bool showDynamicsData = true;
        private bool showLapData = true;

        private ReplaySystem.ReplaySession currentSession;
        private int lastDisplayedFrameIndex = -1;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (replaySystem == null)
                replaySystem = FindObjectOfType<ReplaySystem>();

            if (vehicleController == null)
                vehicleController = FindObjectOfType<VehicleController>();

            SetupControlUI();
            SetupToggles();

            // Hide overlay initially
            HideOverlay();
        }

        private void SetupControlUI()
        {
            if (playPauseButton != null)
                playPauseButton.onClick.AddListener(OnPlayPauseClicked);

            if (rewindButton != null)
                rewindButton.onClick.AddListener(OnRewindClicked);

            if (fastForwardButton != null)
                fastForwardButton.onClick.AddListener(OnFastForwardClicked);

            if (toggleOverlayButton != null)
                toggleOverlayButton.onClick.AddListener(OnToggleOverlayClicked);

            if (playbackSpeedSlider != null)
            {
                playbackSpeedSlider.minValue = 0.1f;
                playbackSpeedSlider.maxValue = 4f;
                playbackSpeedSlider.value = 1f;
                playbackSpeedSlider.onValueChanged.AddListener(OnPlaybackSpeedChanged);
            }

            if (playbackProgressSlider != null)
            {
                playbackProgressSlider.onValueChanged.AddListener(OnProgressSliderChanged);
            }
        }

        private void SetupToggles()
        {
            if (showEngineToggle != null)
                showEngineToggle.onValueChanged.AddListener((bool value) => showEngineData = value);

            if (showTiresToggle != null)
                showTiresToggle.onValueChanged.AddListener((bool value) => showTireData = value);

            if (showDynamicsToggle != null)
                showDynamicsToggle.onValueChanged.AddListener((bool value) => showDynamicsData = value);

            if (showLapDataToggle != null)
                showLapDataToggle.onValueChanged.AddListener((bool value) => showLapData = value);
        }

        private void Update()
        {
            if (!overlayVisible || replaySystem == null)
                return;

            currentSession = replaySystem.GetCurrentSession();
            if (currentSession == null || !replaySystem.IsPlaying)
                return;

            // Update playback display
            var frame = replaySystem.UpdatePlayback();
            if (frame.SessionTime > 0)
            {
                DisplayFrameData(frame);
                UpdatePlaybackUI();
            }
        }

        /// <summary>
        /// Display data from a replay frame.
        /// </summary>
        private void DisplayFrameData(ReplaySystem.ReplayFrame frame)
        {
            if (sessionNameText != null)
                sessionNameText.text = currentSession.SessionName;

            if (currentTimeText != null)
                currentTimeText.text = FormatTime(frame.SessionTime);

            if (totalTimeText != null)
                totalTimeText.text = FormatTime(currentSession.TotalDuration);

            // Update playback progress
            if (playbackProgressSlider != null && currentSession.TotalFrames > 0)
            {
                playbackProgressSlider.maxValue = currentSession.TotalFrames - 1;
                playbackProgressSlider.value = currentSession.CurrentFrameIndex;
            }

            if (showEngineData)
                DisplayEngineData(frame);

            if (showTireData)
                DisplayTireData(frame);

            if (showDynamicsData)
                DisplayDynamicsData(frame);

            if (showLapData)
                DisplayLapData(frame);
        }

        private void DisplayEngineData(ReplaySystem.ReplayFrame frame)
        {
            if (rpmText != null)
                rpmText.text = $"RPM: {frame.EngineRPM:F0}";

            if (powerText != null)
                powerText.text = $"Power: {frame.EnginePower:F1} kW";

            if (torqueText != null)
                torqueText.text = $"Torque: {frame.EngineTorque:F1} Nm";

            if (speedText != null)
                speedText.text = $"Speed: {frame.Speed:F2} m/s ({frame.Speed * 3.6f:F1} km/h)";

            if (gearText != null)
                gearText.text = $"Gear: {(frame.GearInput == 0 ? "R" : frame.GearInput.ToString())}";
        }

        private void DisplayTireData(ReplaySystem.ReplayFrame frame)
        {
            if (tireTempsText != null)
            {
                string temps = "Tire Temps: ";
                for (int i = 0; i < 4; i++)
                {
                    temps += $"{frame.TireTemperatures[i]:F0}°";
                    if (i < 3) temps += " | ";
                }
                tireTempsText.text = temps;
            }

            if (tireWearText != null)
            {
                string wear = "Wear: ";
                for (int i = 0; i < 4; i++)
                {
                    wear += $"{frame.TireWear[i] * 100:F1}%";
                    if (i < 3) wear += " | ";
                }
                tireWearText.text = wear;
            }

            if (tirePressuresText != null)
            {
                string pressures = "Pressure: ";
                for (int i = 0; i < 4; i++)
                {
                    pressures += $"{frame.TirePressures[i]:F1}";
                    if (i < 3) pressures += " | ";
                }
                tirePressuresText.text = pressures;
            }

            if (slipRatiosText != null)
            {
                string slips = "Slip Ratio: ";
                for (int i = 0; i < 4; i++)
                {
                    slips += $"{frame.SlipRatios[i]:F3}";
                    if (i < 3) slips += " | ";
                }
                slipRatiosText.text = slips;
            }

            if (slipAnglesText != null)
            {
                string angles = "Slip Angle: ";
                for (int i = 0; i < 4; i++)
                {
                    angles += $"{frame.SlipAngles[i]:F1}°";
                    if (i < 3) angles += " | ";
                }
                slipAnglesText.text = angles;
            }
        }

        private void DisplayDynamicsData(ReplaySystem.ReplayFrame frame)
        {
            if (lateralAccelText != null)
                lateralAccelText.text = $"Lateral: {frame.LateralAcceleration:F2} m/s²";

            if (longitudinalAccelText != null)
                longitudinalAccelText.text = $"Longitudinal: {frame.LongitudinalAcceleration:F2} m/s²";

            if (rollText != null)
                rollText.text = $"Roll: {frame.RollAngle:F1}°";

            if (pitchText != null)
                pitchText.text = $"Pitch: {frame.PitchAngle:F1}°";

            if (yawText != null)
                yawText.text = $"Yaw: {frame.YawRate:F2}°/s";
        }

        private void DisplayLapData(ReplaySystem.ReplayFrame frame)
        {
            if (lapTimeText != null)
                lapTimeText.text = $"Lap Time: {FormatTime(frame.LapTime)}";

            if (lapNumberText != null)
                lapNumberText.text = $"Lap: {frame.LapNumber}";

            if (distanceText != null)
                distanceText.text = $"Distance: {frame.CumulativeDistance:F0} m";
        }

        private void UpdatePlaybackUI()
        {
            if (playbackStatusText != null)
            {
                string status = replaySystem.IsPlaying ? "Playing" : "Paused";
                playbackStatusText.text = $"{status} - {replaySystem.GetCurrentSession().PlaybackSpeed:F1}x";
            }
        }

        // Control Callbacks
        private void OnPlayPauseClicked()
        {
            if (replaySystem.IsPlaying)
                replaySystem.PausePlayback();
            else
                replaySystem.ResumePlayback();
        }

        private void OnRewindClicked()
        {
            replaySystem.SeekToFrame(Mathf.Max(0, replaySystem.GetCurrentSession().CurrentFrameIndex - 60));
        }

        private void OnFastForwardClicked()
        {
            var session = replaySystem.GetCurrentSession();
            replaySystem.SeekToFrame(Mathf.Min(session.TotalFrames - 1, session.CurrentFrameIndex + 60));
        }

        private void OnToggleOverlayClicked()
        {
            if (overlayVisible)
                HideOverlay();
            else
                ShowOverlay();
        }

        private void OnPlaybackSpeedChanged(float speed)
        {
            if (replaySystem != null)
                replaySystem.SetPlaybackSpeed(speed);
        }

        private void OnProgressSliderChanged(float value)
        {
            if (replaySystem != null && replaySystem.IsPlaying)
            {
                replaySystem.SeekToFrame((int)value);
            }
        }

        /// <summary>
        /// Show the overlay display.
        /// </summary>
        public void ShowOverlay()
        {
            overlayVisible = true;
            if (GetComponent<CanvasGroup>() != null)
                GetComponent<CanvasGroup>().alpha = 1f;
            else
                gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the overlay display.
        /// </summary>
        public void HideOverlay()
        {
            overlayVisible = false;
            if (GetComponent<CanvasGroup>() != null)
                GetComponent<CanvasGroup>().alpha = 0f;
            else
                gameObject.SetActive(false);
        }

        /// <summary>
        /// Toggle overlay visibility.
        /// </summary>
        public void ToggleOverlay()
        {
            if (overlayVisible)
                HideOverlay();
            else
                ShowOverlay();
        }

        /// <summary>
        /// Format time in MM:SS.MS format.
        /// </summary>
        private string FormatTime(float seconds)
        {
            int mins = (int)(seconds / 60f);
            float secs = seconds % 60f;
            return $"{mins:D2}:{secs:F2}";
        }

        public bool IsOverlayVisible => overlayVisible;
    }
}
