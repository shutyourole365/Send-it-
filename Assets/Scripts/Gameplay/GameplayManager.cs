using UnityEngine;
using SendIt.Physics;
using SendIt.Data;

namespace SendIt.Gameplay
{
    /// <summary>
    /// Central manager for gameplay flow and modes.
    /// Controls game state, transitions, and gameplay mechanics.
    /// </summary>
    public class GameplayManager : MonoBehaviour
    {
        public enum GameState
        {
            MainMenu,
            Garage,
            Loading,
            Driving,
            Paused,
            GameOver
        }

        public enum GameMode
        {
            FreeRoam,      // Unlimited driving
            Burnout,       // Create longest skid marks
            Drift,         // Score points for drifting
            TimeTrial,     // Complete lap in shortest time
            Showdown       // Race against AI
        }

        [SerializeField] private VehicleController vehicleController;

        private GameState currentState = GameState.MainMenu;
        private GameMode currentMode = GameMode.FreeRoam;

        // Game data
        private VehicleData currentVehicle;
        private float sessionTimer = 0f;
        private bool isSessionRunning = false;

        // Mode-specific tracking
        private float longestSkidMark = 0f;
        private float driftScore = 0f;
        private float lapTime = 0f;

        public static GameplayManager Instance { get; private set; }

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
        /// Initialize gameplay manager.
        /// </summary>
        public void Initialize()
        {
            // Find vehicle controller
            if (vehicleController == null)
            {
                vehicleController = FindObjectOfType<VehicleController>();
            }

            // Load default vehicle or last saved
            LoadDefaultVehicle();

            SetGameState(GameState.MainMenu);
            Debug.Log("GameplayManager initialized");
        }

        private void Update()
        {
            if (!isSessionRunning)
                return;

            // Update session timer
            sessionTimer += Time.deltaTime;

            // ESC to pause
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        /// <summary>
        /// Set the current game state.
        /// </summary>
        public void SetGameState(GameState newState)
        {
            if (currentState == newState)
                return;

            OnStateExit(currentState);
            currentState = newState;
            OnStateEnter(newState);

            Debug.Log($"Game state changed to: {newState}");
        }

        /// <summary>
        /// Handle state exit logic.
        /// </summary>
        private void OnStateExit(GameState state)
        {
            switch (state)
            {
                case GameState.Driving:
                    isSessionRunning = false;
                    Time.timeScale = 1f; // Ensure time is running
                    break;
            }
        }

        /// <summary>
        /// Handle state enter logic.
        /// </summary>
        private void OnStateEnter(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    // Load main menu scene or show menu UI
                    break;

                case GameState.Garage:
                    Time.timeScale = 1f;
                    // Show customization UI
                    break;

                case GameState.Driving:
                    Time.timeScale = 1f;
                    isSessionRunning = true;
                    sessionTimer = 0f;
                    ResetModeTracking();
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    // Show pause menu
                    break;
            }
        }

        /// <summary>
        /// Start a new game session with specified mode.
        /// </summary>
        public void StartGameSession(GameMode mode)
        {
            currentMode = mode;
            SetGameState(GameState.Driving);
            Debug.Log($"Started game session: {mode}");
        }

        /// <summary>
        /// Toggle pause state.
        /// </summary>
        public void TogglePause()
        {
            if (currentState == GameState.Driving)
            {
                SetGameState(GameState.Paused);
            }
            else if (currentState == GameState.Paused)
            {
                SetGameState(GameState.Driving);
            }
        }

        /// <summary>
        /// Return to main menu.
        /// </summary>
        public void ReturnToMainMenu()
        {
            SetGameState(GameState.MainMenu);
            sessionTimer = 0f;
        }

        /// <summary>
        /// Return to garage from driving.
        /// </summary>
        public void ReturnToGarage()
        {
            SetGameState(GameState.Garage);
        }

        /// <summary>
        /// Reset mode-specific tracking variables.
        /// </summary>
        private void ResetModeTracking()
        {
            longestSkidMark = 0f;
            driftScore = 0f;
            lapTime = 0f;
        }

        /// <summary>
        /// Load default vehicle or last saved configuration.
        /// </summary>
        private void LoadDefaultVehicle()
        {
            TuningManager tuningManager = TuningManager.Instance;
            if (tuningManager != null)
            {
                currentVehicle = tuningManager.GetVehicleData();
            }
        }

        /// <summary>
        /// Get current game state.
        /// </summary>
        public GameState GetGameState() => currentState;

        /// <summary>
        /// Get current game mode.
        /// </summary>
        public GameMode GetGameMode() => currentMode;

        /// <summary>
        /// Get session time in seconds.
        /// </summary>
        public float GetSessionTime() => sessionTimer;

        /// <summary>
        /// Get session time formatted as MM:SS.
        /// </summary>
        public string GetSessionTimeFormatted()
        {
            int minutes = (int)(sessionTimer / 60f);
            int seconds = (int)(sessionTimer % 60f);
            return $"{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// Update mode-specific score/stat.
        /// </summary>
        public void UpdateModeTracking(string trackingType, float value)
        {
            switch (currentMode)
            {
                case GameMode.Burnout:
                    if (trackingType == "skidMark")
                        longestSkidMark = Mathf.Max(longestSkidMark, value);
                    break;

                case GameMode.Drift:
                    if (trackingType == "driftScore")
                        driftScore += value;
                    break;

                case GameMode.TimeTrial:
                    if (trackingType == "lapTime")
                        lapTime = value;
                    break;
            }
        }

        /// <summary>
        /// Get current mode score/stats.
        /// </summary>
        public string GetModeStats()
        {
            return currentMode switch
            {
                GameMode.FreeRoam => "Free Roam - No objectives",
                GameMode.Burnout => $"Longest Skid: {longestSkidMark:F1}m",
                GameMode.Drift => $"Drift Score: {driftScore:F0}",
                GameMode.TimeTrial => $"Lap Time: {lapTime:F2}s",
                GameMode.Showdown => "Racing against AI",
                _ => "Unknown Mode"
            };
        }

        /// <summary>
        /// Get game info for UI display.
        /// </summary>
        public string GetGameInfo()
        {
            string info = "=== GAMEPLAY ===\n";
            info += $"State: {currentState}\n";
            info += $"Mode: {currentMode}\n";
            info += $"Time: {GetSessionTimeFormatted()}\n";
            info += $"{GetModeStats()}\n";
            return info;
        }

        // Getters
        public VehicleController GetVehicleController() => vehicleController;
        public VehicleData GetCurrentVehicle() => currentVehicle;
        public bool IsSessionRunning() => isSessionRunning;
    }
}
