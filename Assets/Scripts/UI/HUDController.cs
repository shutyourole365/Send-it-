using UnityEngine;
using UnityEngine.UI;
using SendIt.Physics;
using SendIt.Gameplay;

namespace SendIt.UI
{
    /// <summary>
    /// Manages real-time HUD updates with vehicle telemetry.
    /// Displays speed, RPM, gear, and mode-specific stats.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private Text speedText;
        private Text rpmText;
        private Text timerText;
        private Text statsText;

        private VehicleController vehicleController;
        private GameplayManager gameplayManager;

        private float updateInterval = 0.1f; // Update every 0.1 seconds
        private float timeSinceLastUpdate = 0f;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize HUD controller.
        /// </summary>
        public void Initialize()
        {
            // Find vehicle controller
            vehicleController = FindObjectOfType<VehicleController>();

            // Find gameplay manager
            gameplayManager = GameplayManager.Instance;

            // Find UI text elements
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Text[] textElements = canvas.GetComponentsInChildren<Text>();
                foreach (Text text in textElements)
                {
                    if (text.name.Contains("Speed"))
                        speedText = text;
                    else if (text.name.Contains("RPM"))
                        rpmText = text;
                    else if (text.name.Contains("Timer"))
                        timerText = text;
                    else if (text.name.Contains("Stats"))
                        statsText = text;
                }
            }

            Debug.Log("HUDController initialized");
        }

        private void Update()
        {
            if (vehicleController == null || gameplayManager == null)
                return;

            // Update HUD at intervals
            timeSinceLastUpdate += Time.deltaTime;
            if (timeSinceLastUpdate >= updateInterval)
            {
                UpdateHUDDisplay();
                timeSinceLastUpdate = 0f;
            }

            // Toggle HUD with TAB
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                UIManager.Instance.ToggleHUD();
            }

            // Pause with ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (gameplayManager.GetGameState() == GameplayManager.GameState.Driving)
                {
                    gameplayManager.TogglePause();
                    UIManager.Instance.ShowPauseMenu();
                }
            }
        }

        /// <summary>
        /// Update all HUD display elements.
        /// </summary>
        private void UpdateHUDDisplay()
        {
            // Update speed
            if (speedText != null)
            {
                float speedKmh = vehicleController.GetSpeedKmh();
                speedText.text = $"{speedKmh:F0} km/h";
            }

            // Update RPM
            if (rpmText != null)
            {
                float rpm = vehicleController.GetCurrentRPM();
                int gear = vehicleController.GetCurrentGear();
                rpmText.text = $"RPM: {rpm:F0} | Gear: {gear}";
            }

            // Update timer
            if (timerText != null)
            {
                timerText.text = gameplayManager.GetSessionTimeFormatted();
            }

            // Update mode stats
            if (statsText != null)
            {
                statsText.text = gameplayManager.GetModeStats();
            }
        }

        /// <summary>
        /// Display temporary notification.
        /// </summary>
        public void ShowNotification(string message, float duration = 3f)
        {
            Debug.Log(message);
            // Could add floating text UI here
        }
    }
}
