using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Advanced tire slip dynamics system for Phase 2.
    /// Handles sophisticated slip angle and slip ratio calculations with load sensitivity.
    /// </summary>
    public class TireSlipDynamics
    {
        // Slip state
        private float currentSlipAngle = 0f; // Radians
        private float currentSlipRatio = 0f; // -1 to 1
        private float peakSlipAngle = 0f; // Angle at peak grip
        private float peakSlipRatio = 0.15f; // Typically 15% for longitudinal

        // Load sensitivity curves
        private float normalLoadAtReference = 3000f; // Reference load in N
        private float currentNormalLoad = 3000f;

        // Slip characteristics
        private float slipAngleSensitivity = 1.0f; // How much load affects slip angle peak
        private float slipRatioSensitivity = 1.0f; // How much load affects slip ratio peak

        // Dynamic slip effects
        private float slipAcceleration = 0f; // How quickly slip changes
        private float slipVelocity = 0f; // Rate of slip change

        // Load transfer effects
        private float lateralLoadTransfer = 0f; // Load transfer during cornering
        private float longitudinalLoadTransfer = 0f; // Load transfer during accel/braking

        public struct SlipState
        {
            public float SlipAngle;
            public float SlipRatio;
            public float PeakSlipAngle;
            public float PeakSlipRatio;
            public float LateralLoadTransfer;
            public float LongitudinalLoadTransfer;
        }

        public TireSlipDynamics()
        {
            // Default values
            peakSlipAngle = 8f * Mathf.Deg2Rad; // ~8 degrees peak
        }

        /// <summary>
        /// Update slip dynamics based on vehicle state and load.
        /// </summary>
        public void Update(float slipAngle, float slipRatio, float normalLoad, float lateralAccel, float longitudinalAccel)
        {
            currentNormalLoad = Mathf.Max(normalLoad, 100f); // Minimum load to avoid division by zero

            // Update slip values with damping for smooth transitions
            UpdateSlipAngle(slipAngle);
            UpdateSlipRatio(slipRatio);

            // Calculate load transfer effects
            CalculateLoadTransfer(normalLoad, lateralAccel, longitudinalAccel);

            // Update peak slip angles based on load
            UpdatePeakSlipCharacteristics();
        }

        /// <summary>
        /// Update slip angle with smooth damping and load sensitivity.
        /// </summary>
        private void UpdateSlipAngle(float inputSlipAngle)
        {
            // Clamp input
            inputSlipAngle = Mathf.Clamp(inputSlipAngle, -Mathf.PI / 2f, Mathf.PI / 2f);

            // Apply exponential averaging for smooth transitions
            float dampingFactor = 0.1f * Time.deltaTime;
            currentSlipAngle = Mathf.Lerp(currentSlipAngle, inputSlipAngle, dampingFactor);

            // Load sensitivity: higher loads reduce peak slip angle (tire becomes stiffer)
            float loadFactor = Mathf.Sqrt(normalLoadAtReference / currentNormalLoad);
            currentSlipAngle *= loadFactor;
        }

        /// <summary>
        /// Update slip ratio with load-dependent behavior.
        /// </summary>
        private void UpdateSlipRatio(float inputSlipRatio)
        {
            // Clamp input
            inputSlipRatio = Mathf.Clamp(inputSlipRatio, -1f, 1f);

            // Apply exponential averaging
            float dampingFactor = 0.15f * Time.deltaTime;
            currentSlipRatio = Mathf.Lerp(currentSlipRatio, inputSlipRatio, dampingFactor);
        }

        /// <summary>
        /// Calculate load transfer during dynamic movements.
        /// </summary>
        private void CalculateLoadTransfer(float normalLoad, float lateralAccel, float longitudinalAccel)
        {
            // Lateral load transfer during cornering
            // Higher lateral acceleration transfers load to outside wheels
            float vehicleWidth = 1.5f; // meters (typical car width)
            float centerOfGravityHeight = 0.5f; // meters (typical height)

            lateralLoadTransfer = (lateralAccel * centerOfGravityHeight) / vehicleWidth;
            lateralLoadTransfer = Mathf.Clamp(lateralLoadTransfer, -normalLoad * 0.3f, normalLoad * 0.3f);

            // Longitudinal load transfer during acceleration/braking
            // Acceleration transfers load to rear, braking to front
            float wheelBase = 2.7f; // meters
            longitudinalLoadTransfer = (longitudinalAccel * centerOfGravityHeight) / wheelBase;
            longitudinalLoadTransfer = Mathf.Clamp(longitudinalLoadTransfer, -normalLoad * 0.2f, normalLoad * 0.2f);
        }

        /// <summary>
        /// Update peak slip characteristics based on load (load sensitivity curve).
        /// </summary>
        private void UpdatePeakSlipCharacteristics()
        {
            // Load sensitivity: peak slip angle decreases with higher load (tire stiffens)
            // Using square root relationship (typical for tires)
            float loadRatio = Mathf.Sqrt(currentNormalLoad / normalLoadAtReference);

            // Peak slip angle varies with load (typically decreases with load)
            peakSlipAngle = (8f * Mathf.Deg2Rad) / loadRatio * slipAngleSensitivity;
            peakSlipAngle = Mathf.Clamp(peakSlipAngle, 4f * Mathf.Deg2Rad, 12f * Mathf.Deg2Rad);

            // Peak slip ratio also changes with load
            peakSlipRatio = 0.15f / Mathf.Sqrt(loadRatio) * slipRatioSensitivity;
            peakSlipRatio = Mathf.Clamp(peakSlipRatio, 0.1f, 0.25f);
        }

        /// <summary>
        /// Get grip factor based on current slip state (normalized 0-1, peak at 1).
        /// </summary>
        public float GetGripFactor()
        {
            // Lateral grip factor
            float lateralGripFactor = CalculateSlipGripFactor(Mathf.Abs(currentSlipAngle), peakSlipAngle);

            // Longitudinal grip factor
            float longitudinalGripFactor = CalculateSlipGripFactor(Mathf.Abs(currentSlipRatio), peakSlipRatio);

            // Combined grip (envelope): lower of the two (you lose lateral grip if using all longitudinal)
            return Mathf.Min(lateralGripFactor, longitudinalGripFactor);
        }

        /// <summary>
        /// Calculate grip factor for a given slip value (peak at ideal slip).
        /// </summary>
        private float CalculateSlipGripFactor(float slipValue, float peakSlip)
        {
            if (peakSlip <= 0f)
                return 1f;

            float normalizedSlip = slipValue / peakSlip;

            // Parabolic curve: peak at 1.0 slip, drops off on both sides
            if (normalizedSlip < 1f)
            {
                // Rise to peak
                return 1f - (normalizedSlip * normalizedSlip * 0.3f);
            }
            else
            {
                // Drop after peak (more aggressive drop)
                return 1f - (normalizedSlip - 1f) * 0.5f;
            }
        }

        /// <summary>
        /// Get load-adjusted normal load for a specific wheel.
        /// </summary>
        public float GetAdjustedNormalLoad(float baseLoad, int wheelIndex)
        {
            float adjustedLoad = baseLoad;

            // Apply load transfers based on wheel position
            // 0,1 = front left/right, 2,3 = rear left/right
            if (wheelIndex == 0 || wheelIndex == 2) // Left wheels
            {
                adjustedLoad += lateralLoadTransfer;
            }
            else // Right wheels
            {
                adjustedLoad -= lateralLoadTransfer;
            }

            if (wheelIndex < 2) // Front wheels
            {
                adjustedLoad += longitudinalLoadTransfer;
            }
            else // Rear wheels
            {
                adjustedLoad -= longitudinalLoadTransfer;
            }

            return Mathf.Max(adjustedLoad, 100f); // Minimum load
        }

        // Getters
        public SlipState GetSlipState()
        {
            return new SlipState
            {
                SlipAngle = currentSlipAngle,
                SlipRatio = currentSlipRatio,
                PeakSlipAngle = peakSlipAngle,
                PeakSlipRatio = peakSlipRatio,
                LateralLoadTransfer = lateralLoadTransfer,
                LongitudinalLoadTransfer = longitudinalLoadTransfer
            };
        }

        public float GetCurrentSlipAngle() => currentSlipAngle;
        public float GetCurrentSlipRatio() => currentSlipRatio;
        public float GetPeakSlipAngle() => peakSlipAngle;
        public float GetPeakSlipRatio() => peakSlipRatio;
        public float GetNormalLoad() => currentNormalLoad;
    }
}
