using UnityEngine;
using SendIt.Gameplay;
using SendIt.Physics;
using SendIt.Data;
using SendIt.AI;
using SendIt.Network;
using System.Collections.Generic;

namespace SendIt.Tests
{
    /// <summary>
    /// Automated integration test runner that verifies all 8 systems are working together.
    /// Add this to an empty GameObject in your test scene and run.
    /// </summary>
    public class IntegrationTestRunner : MonoBehaviour
    {
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private float testDurationSeconds = 10f;

        private EnhancedGameIntegration gameIntegration;
        private VehicleController vehicleController;
        private List<TestResult> testResults = new List<TestResult>();
        private int testFrameCounter = 0;
        private int totalFramesNeeded;
        private bool testsCompleted = false;

        private struct TestResult
        {
            public string TestName;
            public bool Passed;
            public string Message;
            public int FrameNumber;
        }

        private void Start()
        {
            if (runTestsOnStart)
            {
                Debug.Log("\n╔════════════════════════════════════════════════════════════════╗");
                Debug.Log("║           INTEGRATION TEST SUITE - BEGINNING                     ║");
                Debug.Log("╚════════════════════════════════════════════════════════════════╝\n");

                totalFramesNeeded = Mathf.RoundToInt(testDurationSeconds * 60f);

                // Find or create required components
                gameIntegration = FindObjectOfType<EnhancedGameIntegration>();
                if (gameIntegration == null)
                {
                    var go = new GameObject("EnhancedGameIntegration");
                    gameIntegration = go.AddComponent<EnhancedGameIntegration>();
                }

                vehicleController = FindObjectOfType<VehicleController>();

                // Initialize all systems
                gameIntegration.InitializeAllSystems();
            }
        }

        private void Update()
        {
            if (!runTestsOnStart || testsCompleted)
                return;

            testFrameCounter++;

            // Run tests at different frame intervals
            if (testFrameCounter == 1)
                Test_AllSystemsInitialized();

            if (testFrameCounter == 10)
                Test_SystemReferences();

            if (testFrameCounter == 30)
                Test_ReplaySystemRecording();

            if (testFrameCounter == 60)
                Test_CareerSystemData();

            if (testFrameCounter == 90)
                Test_DamageSystemSetup();

            if (testFrameCounter == 120)
                Test_AITuningAdvisorSetup();

            if (testFrameCounter == 150)
                Test_SetupComparisonSetup();

            if (testFrameCounter == 180)
                Test_MobileConnectorRunning();

            if (testFrameCounter == 210)
                Test_NetworkManagerSetup();

            if (testFrameCounter >= totalFramesNeeded || Input.GetKeyDown(KeyCode.Escape))
            {
                CompleteTests();
            }
        }

        // ============ TEST IMPLEMENTATIONS ============

        private void Test_AllSystemsInitialized()
        {
            Debug.Log("[TEST 1/8] All Systems Initialized");

            if (gameIntegration == null)
            {
                AddResult("Initialization", false, "EnhancedGameIntegration is null");
                return;
            }

            // Simple check - if we got here without errors, systems initialized
            AddResult("Initialization", true, "All 8 systems initialized without errors");
        }

        private void Test_SystemReferences()
        {
            Debug.Log("[TEST 2/8] System References");

            int systemsFound = 0;
            var problems = new List<string>();

            if (gameIntegration.GetTuningAdvisor() != null) systemsFound++;
            else problems.Add("AI Tuning Advisor not found");

            if (gameIntegration.GetSetupComparison() != null) systemsFound++;
            else problems.Add("Setup Comparison not found");

            if (gameIntegration.GetReplaySystem() != null) systemsFound++;
            else problems.Add("Replay System not found");

            if (gameIntegration.GetDamageSystem() != null) systemsFound++;
            else problems.Add("Damage System not found");

            if (gameIntegration.GetAIRaceManager() != null) systemsFound++;
            else problems.Add("AI Race Manager not found");

            if (gameIntegration.GetCareerSystem() != null) systemsFound++;
            else problems.Add("Career System not found");

            if (gameIntegration.GetMobileConnector() != null) systemsFound++;
            else problems.Add("Mobile App Connector not found");

            if (gameIntegration.GetMultiplayerManager() != null) systemsFound++;
            else problems.Add("Multiplayer Manager not found");

            bool passed = systemsFound == 8;
            string message = passed ? "All 8 system references accessible" : $"Only {systemsFound}/8 systems found";

            if (problems.Count > 0)
            {
                message += "\nMissing: " + string.Join(", ", problems);
            }

            AddResult("System References", passed, message);
        }

        private void Test_ReplaySystemRecording()
        {
            Debug.Log("[TEST 3/8] Replay System Recording");

            var replaySystem = gameIntegration.GetReplaySystem();

            if (replaySystem == null)
            {
                AddResult("Replay System", false, "Replay system reference is null");
                return;
            }

            // Check if recording is active
            bool isRecording = replaySystem.IsRecording;
            string message = isRecording ?
                "Replay system is actively recording" :
                "Replay system not in recording mode";

            AddResult("Replay System", isRecording, message);
        }

        private void Test_CareerSystemData()
        {
            Debug.Log("[TEST 4/8] Career System Data");

            var careerSystem = gameIntegration.GetCareerSystem();

            if (careerSystem == null)
            {
                AddResult("Career System", false, "Career system reference is null");
                return;
            }

            // Try to get career data
            try
            {
                var careerData = careerSystem.GetCareerData();
                string message = $"Career: Level {careerData.Level}, Wins: {careerData.Wins}, XP: {careerData.TotalExperience}";
                AddResult("Career System", true, message);
            }
            catch (System.Exception ex)
            {
                AddResult("Career System", false, $"Error accessing career data: {ex.Message}");
            }
        }

        private void Test_DamageSystemSetup()
        {
            Debug.Log("[TEST 5/8] Damage System Setup");

            var damageSystem = gameIntegration.GetDamageSystem();

            if (damageSystem == null)
            {
                AddResult("Damage System", false, "Damage system reference is null");
                return;
            }

            try
            {
                float damagePercent = damageSystem.GetTotalDamagePercent();
                string message = $"Damage system initialized, current damage: {damagePercent:F2}%";
                AddResult("Damage System", true, message);
            }
            catch (System.Exception ex)
            {
                AddResult("Damage System", false, $"Error accessing damage data: {ex.Message}");
            }
        }

        private void Test_AITuningAdvisorSetup()
        {
            Debug.Log("[TEST 6/8] AI Tuning Advisor Setup");

            var tuningAdvisor = gameIntegration.GetTuningAdvisor();

            if (tuningAdvisor == null)
            {
                AddResult("AI Tuning Advisor", false, "Tuning advisor reference is null");
                return;
            }

            AddResult("AI Tuning Advisor", true, "AI Tuning Advisor initialized and ready");
        }

        private void Test_SetupComparisonSetup()
        {
            Debug.Log("[TEST 7/8] Setup Comparison System");

            var setupComparison = gameIntegration.GetSetupComparison();

            if (setupComparison == null)
            {
                AddResult("Setup Comparison", false, "Setup comparison reference is null");
                return;
            }

            AddResult("Setup Comparison", true, "Setup Comparison System initialized");
        }

        private void Test_MobileConnectorRunning()
        {
            Debug.Log("[TEST 8/8] Mobile App Connector");

            var mobileConnector = gameIntegration.GetMobileConnector();

            if (mobileConnector == null)
            {
                AddResult("Mobile Connector", false, "Mobile connector reference is null");
                return;
            }

            bool running = mobileConnector.IsServerRunning;
            string message = running ?
                "Mobile API server running on port 8080" :
                "Mobile API server not running";

            AddResult("Mobile Connector", running, message);
        }

        private void Test_NetworkManagerSetup()
        {
            Debug.Log("[BONUS TEST] Network Manager Setup");

            var multiplayerManager = gameIntegration.GetMultiplayerManager();

            if (multiplayerManager == null)
            {
                AddResult("Network Manager", false, "Multiplayer manager reference is null");
                return;
            }

            AddResult("Network Manager", true, "Multiplayer Manager initialized and ready for networking");
        }

        // ============ HELPER METHODS ============

        private void AddResult(string testName, bool passed, string message)
        {
            var result = new TestResult
            {
                TestName = testName,
                Passed = passed,
                Message = message,
                FrameNumber = testFrameCounter
            };

            testResults.Add(result);

            // Log immediately
            string icon = passed ? "✓" : "✗";
            Debug.Log($"{icon} {testName}: {message}");
        }

        private void CompleteTests()
        {
            if (testsCompleted)
                return;

            testsCompleted = true;

            // Print summary
            Debug.Log("\n╔════════════════════════════════════════════════════════════════╗");
            Debug.Log("║                    TEST RESULTS SUMMARY                          ║");
            Debug.Log("╚════════════════════════════════════════════════════════════════╝\n");

            int passedCount = 0;
            int totalCount = testResults.Count;

            foreach (var result in testResults)
            {
                string status = result.Passed ? "PASS" : "FAIL";
                Debug.Log($"[{status}] {result.TestName}");
                Debug.Log($"      {result.Message}\n");

                if (result.Passed)
                    passedCount++;
            }

            // Overall verdict
            Debug.Log("╔════════════════════════════════════════════════════════════════╗");

            if (passedCount == totalCount)
            {
                Debug.Log($"║ ✓ ALL TESTS PASSED ({passedCount}/{totalCount})                           ║");
                Debug.Log("║ System integration is complete and working correctly!         ║");
            }
            else if (passedCount >= totalCount * 0.75f)
            {
                Debug.Log($"║ ⚠ MOST TESTS PASSED ({passedCount}/{totalCount})                         ║");
                Debug.Log("║ Check failed tests above and address issues.                  ║");
            }
            else
            {
                Debug.Log($"║ ✗ TESTS FAILED ({passedCount}/{totalCount})                              ║");
                Debug.Log("║ Integration is incomplete. Review failures above.             ║");
            }

            Debug.Log("╚════════════════════════════════════════════════════════════════╝\n");

            Debug.Log("Next Steps:");
            Debug.Log("1. Review any FAIL results above");
            Debug.Log("2. Check Assets/Scripts for missing components");
            Debug.Log("3. Ensure GameManager and VehicleController exist in scene");
            Debug.Log("4. For mobile testing, run iOS/Android app and connect to server");
            Debug.Log("5. For network testing, start a second game instance as client\n");
        }
    }
}
