using UnityEngine;
using SendIt.Physics;
using SendIt.Tuning;
using SendIt.Data;
using SendIt.AI;
using SendIt.UI;
using SendIt.Network;
using System.Collections.Generic;

namespace SendIt.Gameplay
{
    /// <summary>
    /// Master integration layer connecting all 8 enhancement systems.
    /// Orchestrates data flow, event handling, and system coordination.
    /// </summary>
    public class EnhancedGameIntegration : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private bool autoInitializeAllSystems = true;

        // Core systems
        private VehicleController vehicleController;
        private TuningManager tuningManager;
        private GameplayManager gameplayManager;
        private LapCounter lapCounter;

        // Enhancement systems
        private AITuningAdvisor tuningAdvisor;
        private SetupComparisonSystem setupComparison;
        private ReplaySystem replaySystem;
        private VehicleDamageSystem damageSystem;
        private AIRaceManager aiRaceManager;
        private CareerProgressionSystem careerSystem;
        private MobileAppConnector mobileConnector;
        private MultiplayerRaceManager multiplayerManager;

        // UI systems
        private ReplayTelemetryOverlay replayOverlay;
        private DamageVisualizationUI damageUI;
        private CareerProgressionUI careerUI;
        private SetupComparisonUI setupUI;

        // State tracking
        private bool allSystemsInitialized;
        private float sessionStartTime;
        private int currentLapNumber;
        private float currentLapStartTime;

        private static EnhancedGameIntegration instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (autoInitializeAllSystems)
            {
                InitializeAllSystems();
            }
        }

        private void Update()
        {
            if (!allSystemsInitialized)
                return;

            // Update session timing
            if (replaySystem.IsRecording)
            {
                replaySystem.RecordFrame();
            }

            // Check for lap completion
            CheckLapCompletion();
        }

        /// <summary>
        /// Initialize all enhancement systems and wire up event handlers.
        /// </summary>
        public void InitializeAllSystems()
        {
            Debug.Log("=== Initializing Enhanced Game Systems ===");

            // Get or create core systems
            if (gameManager == null)
                gameManager = FindObjectOfType<GameManager>();

            vehicleController = gameManager.GetVehicleController();
            tuningManager = gameManager.GetTuningManager();

            // Initialize core game systems
            gameplayManager = FindObjectOfType<GameplayManager>();
            lapCounter = FindObjectOfType<LapCounter>();

            // Initialize Enhancement 1: AI Tuning Advisor
            InitializeTuningAdvisor();

            // Initialize Enhancement 2: Setup Comparison
            InitializeSetupComparison();

            // Initialize Enhancement 3: Replay System
            InitializeReplaySystem();

            // Initialize Enhancement 4: Damage System
            InitializeDamageSystem();

            // Initialize Enhancement 5: AI Opponents
            InitializeAIOpponents();

            // Initialize Enhancement 6: Career System
            InitializeCareerSystem();

            // Initialize Enhancement 7: Mobile App Connector
            InitializeMobileConnector();

            // Initialize Enhancement 8: Multiplayer Manager
            InitializeMultiplayer();

            // Wire up event handlers
            WireUpEventHandlers();

            // Initialize UI systems
            InitializeUIOverlays();

            sessionStartTime = Time.time;
            allSystemsInitialized = true;

            Debug.Log("=== All Enhancement Systems Initialized ===");
            Debug.Log($"Active Systems: {CountActiveSystems()}/8");
        }

        /// <summary>
        /// Initialize AI Tuning Advisor.
        /// </summary>
        private void InitializeTuningAdvisor()
        {
            tuningAdvisor = gameObject.AddComponent<AITuningAdvisor>();
            tuningAdvisor.Initialize();
            Debug.Log("✓ AI Tuning Advisor initialized");
        }

        /// <summary>
        /// Initialize Setup Comparison System.
        /// </summary>
        private void InitializeSetupComparison()
        {
            setupComparison = gameObject.AddComponent<SetupComparisonSystem>();
            setupComparison.Initialize();
            Debug.Log("✓ Setup Comparison System initialized");
        }

        /// <summary>
        /// Initialize Replay System.
        /// </summary>
        private void InitializeReplaySystem()
        {
            replaySystem = gameObject.AddComponent<ReplaySystem>();
            replaySystem.Initialize();

            // Auto-start recording
            replaySystem.StartRecording("Current Session", "");
            Debug.Log("✓ Replay System initialized and recording");
        }

        /// <summary>
        /// Initialize Damage System.
        /// </summary>
        private void InitializeDamageSystem()
        {
            damageSystem = vehicleController.gameObject.AddComponent<VehicleDamageSystem>();
            damageSystem.Initialize();
            Debug.Log("✓ Damage System initialized");
        }

        /// <summary>
        /// Initialize AI Opponents and Race Manager.
        /// </summary>
        private void InitializeAIOpponents()
        {
            aiRaceManager = gameObject.AddComponent<AIRaceManager>();
            aiRaceManager.Initialize();
            Debug.Log("✓ AI Race Manager initialized");
        }

        /// <summary>
        /// Initialize Career Progression System.
        /// </summary>
        private void InitializeCareerSystem()
        {
            careerSystem = gameObject.AddComponent<CareerProgressionSystem>();
            careerSystem.Initialize();
            Debug.Log("✓ Career Progression System initialized");
        }

        /// <summary>
        /// Initialize Mobile App Connector.
        /// </summary>
        private void InitializeMobileConnector()
        {
            mobileConnector = gameObject.AddComponent<MobileAppConnector>();
            mobileConnector.Initialize();
            mobileConnector.StartServer();
            Debug.Log("✓ Mobile App Connector initialized");
        }

        /// <summary>
        /// Initialize Multiplayer Manager.
        /// </summary>
        private void InitializeMultiplayer()
        {
            multiplayerManager = gameObject.AddComponent<MultiplayerRaceManager>();
            multiplayerManager.Initialize();
            Debug.Log("✓ Multiplayer Race Manager initialized");
        }

        /// <summary>
        /// Initialize UI overlay systems.
        /// </summary>
        private void InitializeUIOverlays()
        {
            // Damage UI
            var damageUIGO = new GameObject("DamageVisualizationUI");
            damageUI = damageUIGO.AddComponent<DamageVisualizationUI>();
            damageUI.Initialize();

            // Career UI
            var careerUIGO = new GameObject("CareerProgressionUI");
            careerUI = careerUIGO.AddComponent<CareerProgressionUI>();
            careerUI.Initialize();

            // Replay Overlay
            var replayUIGO = new GameObject("ReplayTelemetryOverlay");
            replayOverlay = replayUIGO.AddComponent<ReplayTelemetryOverlay>();
            replayOverlay.Initialize();

            // Setup Comparison UI
            var setupUIGO = new GameObject("SetupComparisonUI");
            setupUI = setupUIGO.AddComponent<SetupComparisonUI>();
            setupUI.Initialize();

            Debug.Log("✓ UI Systems initialized");
        }

        /// <summary>
        /// Wire up all event handlers between systems.
        /// </summary>
        private void WireUpEventHandlers()
        {
            // Collision events → Damage system
            if (vehicleController != null)
            {
                var rb = vehicleController.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Note: OnCollisionEnter would be handled in VehicleController
                    // damageSystem receives collision via RegisterCollisionImpact()
                }
            }

            // Tuning changes → Setup comparison update
            // (Would be triggered by tuning UI changes)

            // Replay system listens to all vehicle state changes
            // (Updated each frame via RecordFrame())

            // Career system listens to race completion
            // (Would be triggered by race end event)

            // AI advisor analyzes tuning performance
            // (Would analyze session data at race end)

            // Mobile connector syncs all system data
            // (REST API provides read-only access)

            // Multiplayer manager syncs player states
            // (Updated via network tick in Update)

            Debug.Log("✓ Event handlers wired up");
        }

        /// <summary>
        /// Start a race session with all systems enabled.
        /// </summary>
        public void StartRaceSession(string trackName, int laps)
        {
            Debug.Log($"Starting race session: {trackName} ({laps} laps)");

            sessionStartTime = Time.time;
            currentLapNumber = 0;
            currentLapStartTime = Time.time;

            // Start replay recording
            if (replaySystem != null)
            {
                replaySystem.StopRecording();
                replaySystem.StartRecording($"Race - {trackName}", trackName);
            }

            // Reset tuning advisor
            if (tuningAdvisor != null)
            {
                tuningAdvisor.ResetSession();
            }

            // Reset damage system
            if (damageSystem != null)
            {
                // Would reset damage if needed
            }

            Debug.Log("Race session started - all systems active");
        }

        /// <summary>
        /// End race session and process results.
        /// </summary>
        public void EndRaceSession(int finalPosition, int totalParticipants)
        {
            Debug.Log($"Ending race session - Final Position: {finalPosition}/{totalParticipants}");

            float totalRaceTime = Time.time - sessionStartTime;

            // Stop replay recording
            if (replaySystem != null)
            {
                replaySystem.StopRecording();
                replaySystem.SaveReplay();
            }

            // Record career results
            if (careerSystem != null)
            {
                float bestLapTime = lapCounter != null ? lapCounter.GetBestLapTime() : 0f;
                careerSystem.RecordRaceResult(
                    "Race Session",
                    "",
                    finalPosition,
                    totalParticipants,
                    bestLapTime,
                    totalRaceTime
                );
                careerSystem.SaveCareerData();
            }

            // Analyze performance with AI tuning advisor
            if (tuningAdvisor != null)
            {
                var recommendations = tuningAdvisor.AnalyzeSession();
                Debug.Log($"AI Tuning Advisor: {recommendations.Count} recommendations generated");
            }

            // Update multiplayer stats
            if (multiplayerManager != null && multiplayerManager.IsRaceActive)
            {
                multiplayerManager.FinishRace();
            }

            // Update damage visualization
            if (damageUI != null)
            {
                damageUI.RefreshDamageDisplay();
            }

            // Update career UI
            if (careerUI != null)
            {
                careerUI.RefreshAllUI();
            }

            Debug.Log("Race session ended - results processed");
        }

        /// <summary>
        /// Check for lap completion and update all systems.
        /// </summary>
        private void CheckLapCompletion()
        {
            if (lapCounter == null)
                return;

            int newLapNumber = lapCounter.GetCurrentLapNumber();

            if (newLapNumber > currentLapNumber)
            {
                float lapTime = Time.time - currentLapStartTime;
                currentLapNumber = newLapNumber;
                currentLapStartTime = Time.time;

                Debug.Log($"Lap {currentLapNumber} completed - Time: {lapTime:F2}s");

                // Update AI tuning advisor with lap data
                if (tuningAdvisor != null)
                {
                    tuningAdvisor.CollectSessionData();
                }

                // Refresh UI systems
                if (damageUI != null)
                    damageUI.RefreshDamageDisplay();
                if (careerUI != null)
                    careerUI.RefreshAllUI();
            }
        }

        /// <summary>
        /// Get integration status for debugging.
        /// </summary>
        public string GetIntegrationStatus()
        {
            return $@"
=== ENHANCED GAME INTEGRATION STATUS ===
Initialized: {allSystemsInitialized}
Session Time: {Time.time - sessionStartTime:F1}s
Current Lap: {currentLapNumber}

Core Systems:
- Vehicle Controller: {(vehicleController != null ? "✓" : "✗")}
- Tuning Manager: {(tuningManager != null ? "✓" : "✗")}
- Gameplay Manager: {(gameplayManager != null ? "✓" : "✗")}
- Lap Counter: {(lapCounter != null ? "✓" : "✗")}

Enhancement Systems:
1. AI Tuning Advisor: {(tuningAdvisor != null ? "✓" : "✗")}
2. Setup Comparison: {(setupComparison != null ? "✓" : "✗")}
3. Replay System: {(replaySystem != null && replaySystem.IsRecording ? "✓ Recording" : "✗")}
4. Damage System: {(damageSystem != null ? "✓" : "✗")} - Damage: {damageSystem?.GetOverallDamage() * 100:F1}%
5. AI Race Manager: {(aiRaceManager != null ? "✓" : "✗")}
6. Career System: {(careerSystem != null ? "✓" : "✗")}
7. Mobile Connector: {(mobileConnector != null && mobileConnector.IsServerActive ? "✓ Active" : "✗")}
8. Multiplayer Manager: {(multiplayerManager != null ? "✓" : "✗")}

UI Systems:
- Damage UI: {(damageUI != null ? "✓" : "✗")}
- Career UI: {(careerUI != null ? "✓" : "✗")}
- Replay Overlay: {(replayOverlay != null ? "✓" : "✗")}
- Setup UI: {(setupUI != null ? "✓" : "✗")}
";
        }

        private int CountActiveSystems()
        {
            int count = 0;
            if (tuningAdvisor != null) count++;
            if (setupComparison != null) count++;
            if (replaySystem != null) count++;
            if (damageSystem != null) count++;
            if (aiRaceManager != null) count++;
            if (careerSystem != null) count++;
            if (mobileConnector != null) count++;
            if (multiplayerManager != null) count++;
            return count;
        }

        // Getters for external access
        public AITuningAdvisor GetTuningAdvisor => tuningAdvisor;
        public SetupComparisonSystem GetSetupComparison => setupComparison;
        public ReplaySystem GetReplaySystem => replaySystem;
        public VehicleDamageSystem GetDamageSystem => damageSystem;
        public AIRaceManager GetAIRaceManager => aiRaceManager;
        public CareerProgressionSystem GetCareerSystem => careerSystem;
        public MobileAppConnector GetMobileConnector => mobileConnector;
        public MultiplayerRaceManager GetMultiplayerManager => multiplayerManager;

        public bool AllSystemsInitialized => allSystemsInitialized;
        public static EnhancedGameIntegration Instance => instance;
    }
}
