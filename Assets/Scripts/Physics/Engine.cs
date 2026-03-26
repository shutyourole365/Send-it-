using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Simulates engine behavior with realistic torque curves, rev-limiting, and transient response.
    /// </summary>
    public class Engine
    {
        public const float IdleRPM = 800f;
        private const float REV_LIMITER_THRESHOLD = 0.98f;
        private const float REV_LIMITER_CUT_RPM = 50f;

        private float maxRPM;
        private float horsePower;
        private float torquePeakRPM;
        private float engineResponsiveness;
        private float previousRPM;

        // Engine characteristics
        private float inertiaFactor = 2000f; // How quickly RPM responds to throttle
        private float engineFriction = 50f; // Baseline friction loss

        public struct EngineState
        {
            public float RPM;
            public float Torque;
            public float Power; // Current horsepower output
        }

        public Engine(PhysicsData physicsData)
        {
            UpdateParameters(physicsData);
            previousRPM = IdleRPM;
        }

        public void UpdateParameters(PhysicsData physicsData)
        {
            maxRPM = physicsData.MaxRPM;
            horsePower = physicsData.HorsePower;
            torquePeakRPM = physicsData.TorquePeakRPM;
            engineResponsiveness = physicsData.EngineResponsiveness;
        }

        /// <summary>
        /// Calculate torque output using a realistic torque curve model.
        /// Models a typical naturally-aspirated engine with linear rise, peak, then decline.
        /// </summary>
        private float CalculateTorque(float rpm)
        {
            if (rpm <= 0)
                return 0f;

            // Calculate base torque from power equation: Torque = (Power * 5252) / RPM
            // (5252 is the constant for converting HP to Nm at a given RPM)
            float baseTorque = (horsePower * 5252f) / rpm;

            // Apply torque curve shape
            float torqueCurve;
            if (rpm < torquePeakRPM)
            {
                // Linear rise to peak torque
                torqueCurve = Mathf.Lerp(0.4f, 1.0f, rpm / torquePeakRPM);
            }
            else
            {
                // Natural drop-off after peak (power stays more constant)
                float rpmAbovePeak = rpm - torquePeakRPM;
                float rpmRange = maxRPM - torquePeakRPM;
                // Cubic falloff for realistic behavior
                float falloff = Mathf.Pow(1f - (rpmAbovePeak / rpmRange), 1.5f);
                torqueCurve = Mathf.Lerp(0.3f, 1.0f, falloff);
            }

            return baseTorque * torqueCurve;
        }

        /// <summary>
        /// Calculate current power output in horsepower.
        /// Power = (Torque * RPM) / 5252
        /// </summary>
        private float CalculatePower(float torque, float rpm)
        {
            if (rpm <= 0)
                return 0f;
            return (torque * rpm) / 5252f;
        }

        /// <summary>
        /// Apply rev-limiter effect near max RPM to prevent over-revving.
        /// </summary>
        private float ApplyRevLimiter(float rpm, float targetRPM, float deltaTime)
        {
            float revLimiterThreshold = maxRPM * REV_LIMITER_THRESHOLD;

            if (rpm > revLimiterThreshold)
            {
                // Hard rev limiter: cut power delivery near max
                float overRevAmount = rpm - revLimiterThreshold;
                float limitCut = (overRevAmount / (maxRPM - revLimiterThreshold)) * REV_LIMITER_CUT_RPM;
                return rpm - limitCut * deltaTime;
            }

            return rpm;
        }

        /// <summary>
        /// Update engine RPM based on throttle input with realistic transient response.
        /// </summary>
        public EngineState Update(float currentRPM, float throttleInput, int currentGear)
        {
            previousRPM = currentRPM;

            // Calculate target RPM based on throttle
            float targetRPM = IdleRPM + throttleInput * (maxRPM - IdleRPM);

            // Apply engine inertia (RPM doesn't change instantly)
            // Heavier throttle = faster response, but still has lag
            float responseSpeed = engineResponsiveness * inertiaFactor * Time.deltaTime;
            float rpmDifference = targetRPM - currentRPM;
            float newRPM = currentRPM + (rpmDifference * responseSpeed / 1000f);

            // Apply engine friction (RPM decays when throttle is off)
            if (throttleInput < 0.1f)
            {
                float frictionDecay = engineFriction * Time.deltaTime;
                newRPM = Mathf.Max(newRPM - frictionDecay, IdleRPM);
            }

            // Apply rev limiter
            newRPM = ApplyRevLimiter(newRPM, targetRPM, Time.deltaTime);

            // Clamp to valid range
            newRPM = Mathf.Clamp(newRPM, IdleRPM, maxRPM);

            // Calculate outputs
            float torque = CalculateTorque(newRPM);
            float power = CalculatePower(torque, newRPM);

            return new EngineState
            {
                RPM = newRPM,
                Torque = torque,
                Power = power
            };
        }

        /// <summary>
        /// Calculate engine braking force when throttle is off.
        /// </summary>
        public float CalculateEngineBrakingForce(float rpm, float transmission)
        {
            // Engine braking is proportional to RPM and gear ratio
            float brakingTorque = (rpm / maxRPM) * horsePower * transmission;
            return Mathf.Max(brakingTorque, 0f);
        }

        public float GetMaxRPM() => maxRPM;
        public float GetHorsePower() => horsePower;
        public float GetIdleRPM() => IdleRPM;
        public float GetTorquePeakRPM() => torquePeakRPM;
    }
}
