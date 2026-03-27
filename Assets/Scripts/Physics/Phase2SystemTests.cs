using UnityEngine;
using SendIt.Physics;
using SendIt.Data;

namespace SendIt.Tests
{
    /// <summary>
    /// Unit tests for Phase 2 physics systems.
    /// Run these tests to validate system behavior.
    /// </summary>
    public class Phase2SystemTests : MonoBehaviour
    {
        [SerializeField] private bool runTestsOnStart = true;
        private int totalTests = 0;
        private int passedTests = 0;

        private void Start()
        {
            if (runTestsOnStart)
            {
                RunAllTests();
            }
        }

        public void RunAllTests()
        {
            Debug.Log("=== Running Phase 2 System Tests ===");
            totalTests = 0;
            passedTests = 0;

            TestTireSlipDynamics();
            TestTireTemperatureSystem();
            TestTireWearPatterns();
            TestTirePressureSystem();
            TestSurfaceConditionsSystem();

            Debug.Log($"\n=== Test Results: {passedTests}/{totalTests} Passed ===");
        }

        #region Slip Dynamics Tests

        private void TestTireSlipDynamics()
        {
            Debug.Log("\n--- Testing TireSlipDynamics ---");

            var slipDynamics = new TireSlipDynamics();

            // Test 1: Grip factor at zero slip
            slipDynamics.Update(0f, 0f, 3000f, 0f, 0f);
            float gripFactor = slipDynamics.GetGripFactor();
            AssertGreater(gripFactor, 0.9f, "Grip factor at zero slip should be near 1.0");
            AssertLess(gripFactor, 1.05f, "Grip factor should not exceed 1.05");

            // Test 2: Grip decreases with extreme slip
            slipDynamics.Update(0.2f, 0f, 3000f, 0f, 0f);
            float gripWithSlip = slipDynamics.GetGripFactor();
            AssertLess(gripWithSlip, gripFactor, "Grip should decrease with slip");

            // Test 3: Load transfer calculation
            float baseLoad = 3000f;
            float adjustedLoad = slipDynamics.GetAdjustedNormalLoad(baseLoad, 0); // Left wheel
            AssertNotEqual(adjustedLoad, baseLoad, "Load transfer should adjust normal load");

            // Test 4: Slip state retrieval
            var slipState = slipDynamics.GetSlipState();
            AssertGreaterOrEqual(slipState.SlipAngle, -Mathf.PI / 2f, "Slip angle should be valid");
            AssertLessOrEqual(slipState.SlipAngle, Mathf.PI / 2f, "Slip angle should be valid");

            Debug.Log("TireSlipDynamics: All tests passed!");
        }

        #endregion

        #region Temperature System Tests

        private void TestTireTemperatureSystem()
        {
            Debug.Log("\n--- Testing TireTemperatureSystem ---");

            var tempSystem = new TireTemperatureSystem();

            // Test 1: Initial temperature is ambient
            float initialTemp = tempSystem.GetAverageTemperature();
            AssertLess(initialTemp, 30f, "Initial temperature should be cold (ambient)");

            // Test 2: Temperature increases with activity
            tempSystem.Update(0.1f, 0.1f, 3000f, 50f, 5f); // Driving at 50 m/s
            float heatedTemp = tempSystem.GetAverageTemperature();
            AssertGreater(heatedTemp, initialTemp, "Temperature should increase with driving");

            // Test 3: Grip factor peaks at optimal temperature
            tempSystem.Update(0f, 0f, 3000f, 30f, 0f); // Low slip, steady speed
            for (int i = 0; i < 100; i++)
            {
                tempSystem.Update(0f, 0f, 3000f, 30f, 0f);
            }
            float gripAtOptimal = tempSystem.GetTemperatureGripFactor();
            AssertGreater(gripAtOptimal, 1.1f, "Grip should peak at optimal temperature");

            // Test 4: Wear factor increases with temperature
            float wearFactor = tempSystem.GetTemperatureWearFactor();
            AssertGreater(wearFactor, 0.5f, "Wear factor should be positive");
            AssertLess(wearFactor, 10f, "Wear factor should be reasonable");

            Debug.Log("TireTemperatureSystem: All tests passed!");
        }

        #endregion

        #region Wear Patterns Tests

        private void TestTireWearPatterns()
        {
            Debug.Log("\n--- Testing TireWearPatterns ---");

            var wearPatterns = new TireWearPatterns(TireWearPatterns.TireCompound.Sport);

            // Test 1: Initial tread depth
            float initialTread = wearPatterns.GetTreadDepth();
            AssertEqual(initialTread, 8f, "New tire should have 8mm tread");

            // Test 2: Wear accumulates
            for (int i = 0; i < 100; i++)
            {
                wearPatterns.Update(1.5f, 0.1f, 0.1f, 3000f, 30f, 5f);
            }
            float wornTread = wearPatterns.GetTreadDepth();
            AssertLess(wornTread, initialTread, "Tread should decrease with wear");

            // Test 3: Grip loss from wear
            float wearGripFactor = wearPatterns.GetWearGripFactor();
            AssertLess(wearGripFactor, 1.0f, "Worn tire should have grip penalty");
            AssertGreater(wearGripFactor, 0.2f, "Worn tire should still have some grip");

            // Test 4: Wear state structure
            var wearState = wearPatterns.GetWearState();
            AssertGreaterOrEqual(wearState.TotalWear, 0f, "Wear should be non-negative");
            AssertLessOrEqual(wearState.TotalWear, 1f, "Wear should not exceed 100%");

            // Test 5: Reset wear
            wearPatterns.ResetWear();
            float resetTread = wearPatterns.GetTreadDepth();
            AssertEqual(resetTread, 8f, "Reset tire should have 8mm tread");

            Debug.Log("TireWearPatterns: All tests passed!");
        }

        #endregion

        #region Pressure System Tests

        private void TestTirePressureSystem()
        {
            Debug.Log("\n--- Testing TirePressureSystem ---");

            var pressureSystem = new TirePressureSystem(32f);

            // Test 1: Initial pressure is optimal
            float initialPressure = pressureSystem.GetCurrentPressure();
            AssertEqual(initialPressure, 32f, "Initial pressure should be 32 PSI");

            // Test 2: Pressure changes with temperature
            pressureSystem.Update(20f); // Cold
            float coldPressure = pressureSystem.GetCurrentPressure();
            pressureSystem.Update(80f); // Hot
            float hotPressure = pressureSystem.GetCurrentPressure();
            AssertGreater(hotPressure, coldPressure, "Pressure should increase with temperature");

            // Test 3: Optimal pressure gives best grip
            pressureSystem.SetPressure(32f);
            pressureSystem.Update(85f);
            float optimalGrip = pressureSystem.GetPressureGripFactor();
            AssertGreater(optimalGrip, 0.95f, "Optimal pressure should give near-perfect grip");

            // Test 4: Under-pressure reduces grip
            pressureSystem.SetPressure(28f);
            float underGrip = pressureSystem.GetPressureGripFactor();
            AssertLess(underGrip, optimalGrip, "Under-pressure should reduce grip");

            // Test 5: Over-pressure also reduces grip
            pressureSystem.SetPressure(38f);
            float overGrip = pressureSystem.GetPressureGripFactor();
            AssertLess(overGrip, optimalGrip, "Over-pressure should reduce grip");

            // Test 6: Blowout risk detection
            pressureSystem.SetPressure(25f);
            bool underRisk = pressureSystem.IsUnderPressure();
            AssertTrue(underRisk, "Should detect under-pressure");

            pressureSystem.SetPressure(40f);
            bool overRisk = pressureSystem.IsOverPressure();
            AssertTrue(overRisk, "Should detect over-pressure");

            Debug.Log("TirePressureSystem: All tests passed!");
        }

        #endregion

        #region Surface Conditions Tests

        private void TestSurfaceConditionsSystem()
        {
            Debug.Log("\n--- Testing SurfaceConditionsSystem ---");

            var surface = new SurfaceConditionsSystem();

            // Test 1: Dry asphalt has baseline grip
            surface.SetSurfaceType(SurfaceConditionsSystem.SurfaceType.DryAsphalt);
            surface.SetWetness(0f);
            float dryGrip = surface.GetGripCoefficient();
            AssertEqual(dryGrip, 1.0f, "Dry asphalt should have 100% grip");

            // Test 2: Wet surface reduces grip
            surface.SetWetness(1f);
            float wetGrip = surface.GetGripCoefficient();
            AssertLess(wetGrip, dryGrip, "Wet surface should reduce grip");

            // Test 3: Ice has very low grip
            surface.SetSurfaceType(SurfaceConditionsSystem.SurfaceType.Ice);
            float iceGrip = surface.GetGripCoefficient();
            AssertLess(iceGrip, 0.2f, "Ice should have very low grip");

            // Test 4: Different surfaces have different wear multipliers
            surface.SetSurfaceType(SurfaceConditionsSystem.SurfaceType.DryAsphalt);
            float asphaltWear = surface.GetWearMultiplier();
            surface.SetSurfaceType(SurfaceConditionsSystem.SurfaceType.Gravel);
            float gravelWear = surface.GetWearMultiplier();
            AssertGreater(gravelWear, asphaltWear, "Gravel should wear tires faster");

            // Test 5: Aquaplaning detection
            surface.SetSurfaceType(SurfaceConditionsSystem.SurfaceType.WetAsphalt);
            surface.SetWetness(0.8f);
            bool aquaplaning = surface.IsAquaplaning(100f); // 100 m/s (~360 km/h)
            AssertTrue(aquaplaning, "High speed on wet surface should cause aquaplaning");

            aquaplaning = surface.IsAquaplaning(10f); // 10 m/s (~36 km/h)
            AssertFalse(aquaplaning, "Low speed should not cause aquaplaning");

            Debug.Log("SurfaceConditionsSystem: All tests passed!");
        }

        #endregion

        #region Assertion Helpers

        private void AssertEqual(float actual, float expected, string message)
        {
            totalTests++;
            if (Mathf.Abs(actual - expected) < 0.01f)
            {
                passedTests++;
                Debug.Log($"✓ PASS: {message}");
            }
            else
            {
                Debug.LogError($"✗ FAIL: {message} (Expected: {expected}, Got: {actual})");
            }
        }

        private void AssertGreater(float actual, float threshold, string message)
        {
            totalTests++;
            if (actual > threshold)
            {
                passedTests++;
                Debug.Log($"✓ PASS: {message}");
            }
            else
            {
                Debug.LogError($"✗ FAIL: {message} (Value: {actual}, Threshold: {threshold})");
            }
        }

        private void AssertGreaterOrEqual(float actual, float threshold, string message)
        {
            totalTests++;
            if (actual >= threshold)
            {
                passedTests++;
                Debug.Log($"✓ PASS: {message}");
            }
            else
            {
                Debug.LogError($"✗ FAIL: {message} (Value: {actual}, Threshold: {threshold})");
            }
        }

        private void AssertLess(float actual, float threshold, string message)
        {
            totalTests++;
            if (actual < threshold)
            {
                passedTests++;
                Debug.Log($"✓ PASS: {message}");
            }
            else
            {
                Debug.LogError($"✗ FAIL: {message} (Value: {actual}, Threshold: {threshold})");
            }
        }

        private void AssertLessOrEqual(float actual, float threshold, string message)
        {
            totalTests++;
            if (actual <= threshold)
            {
                passedTests++;
                Debug.Log($"✓ PASS: {message}");
            }
            else
            {
                Debug.LogError($"✗ FAIL: {message} (Value: {actual}, Threshold: {threshold})");
            }
        }

        private void AssertNotEqual(float actual, float notExpected, string message)
        {
            totalTests++;
            if (Mathf.Abs(actual - notExpected) >= 0.01f)
            {
                passedTests++;
                Debug.Log($"✓ PASS: {message}");
            }
            else
            {
                Debug.LogError($"✗ FAIL: {message} (Should not be: {notExpected})");
            }
        }

        private void AssertTrue(bool condition, string message)
        {
            totalTests++;
            if (condition)
            {
                passedTests++;
                Debug.Log($"✓ PASS: {message}");
            }
            else
            {
                Debug.LogError($"✗ FAIL: {message}");
            }
        }

        private void AssertFalse(bool condition, string message)
        {
            totalTests++;
            if (!condition)
            {
                passedTests++;
                Debug.Log($"✓ PASS: {message}");
            }
            else
            {
                Debug.LogError($"✗ FAIL: {message}");
            }
        }

        #endregion
    }
}
