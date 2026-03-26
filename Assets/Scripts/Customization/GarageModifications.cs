using UnityEngine;
using SendIt.Data;
using SendIt.Physics;

namespace SendIt.Customization
{
    /// <summary>
    /// Manages garage modifications including suspension kits, brake upgrades,
    /// transmission changes, and differential tuning.
    /// These modifications directly affect vehicle physics performance.
    /// </summary>
    public class GarageModifications : MonoBehaviour
    {
        private PhysicsData physicsData;

        // Suspension modifications
        private int suspensionKit = 0; // 0=Stock, 1=Lowering, 2=Sport, 3=Racing, 4=Air Suspension
        private float suspensionHeight = 0.3f; // 0.1-0.5m
        private bool hasAntiRollBars = false;
        private float antiRollBarStiffness = 15000f;

        // Brake modifications
        private int brakeSystem = 0; // 0=Stock, 1=Performance, 2=Racing, 3=Carbon Ceramic
        private float brakeBiasPercentage = 0.5f; // 0-1, front to rear balance
        private bool hasRegenerativeBraking = false;

        // Transmission modifications
        private int transmissionType = 0; // 0=Stock, 1=LSD, 2=Mechanical LSD, 3=Full Race
        private float gearRatioMultiplier = 1f; // 0.8-1.2 (affects acceleration/top speed)
        private float shiftSpeedMultiplier = 1f; // Gear change speed

        // Differential settings
        private float differentialLockPercentage = 0f; // 0-1 (0=open, 1=locked)
        private float differentialBias = 1f; // 1.0=50/50, <1.0=favor rear, >1.0=favor front

        // Aerodynamic modifications
        private float downforceLevel = 0f; // 0-100
        private float dragMultiplier = 1f; // Affects top speed vs downforce

        [System.Serializable]
        public struct GarageModSettings
        {
            public int SuspensionKit;
            public float SuspensionHeight;
            public bool HasAntiRollBars;
            public int BrakeSystem;
            public float BrakeBias;
            public int TransmissionType;
            public float GearRatioMultiplier;
            public float DifferentialLock;
            public float DownforceLevel;
            public float DragMultiplier;
        }

        public void Initialize(PhysicsData data)
        {
            physicsData = data;
            suspensionHeight = data.RideHeight;
            antiRollBarStiffness = data.AntiRollBarStiffness;
            ApplyModifications();
        }

        /// <summary>
        /// Set suspension kit type.
        /// </summary>
        public void SetSuspensionKit(int kit)
        {
            suspensionKit = Mathf.Clamp(kit, 0, 4);
            ApplySuspensionModifications();
        }

        /// <summary>
        /// Set suspension ride height.
        /// </summary>
        public void SetSuspensionHeight(float height)
        {
            suspensionHeight = Mathf.Clamp(height, 0.1f, 0.5f);
            ApplySuspensionModifications();
        }

        /// <summary>
        /// Enable/disable anti-roll bars.
        /// </summary>
        public void SetAntiRollBars(bool enabled)
        {
            hasAntiRollBars = enabled;
            if (!enabled)
                antiRollBarStiffness = 0f;
            else
                antiRollBarStiffness = 15000f + (suspensionKit * 5000f);
        }

        /// <summary>
        /// Apply suspension modifications to physics.
        /// </summary>
        private void ApplySuspensionModifications()
        {
            // Suspension kit affects stiffness and travel
            float stiffnessMultiplier = suspensionKit switch
            {
                0 => 1.0f,  // Stock
                1 => 0.8f,  // Lowering (softer springs for ride height)
                2 => 1.2f,  // Sport
                3 => 1.8f,  // Racing (very stiff)
                4 => 0.6f,  // Air (adjustable, initially soft)
                _ => 1.0f
            };

            // Apply to physics
            if (physicsData != null)
            {
                physicsData.SetSpringStiffness(physicsData.SpringStiffness * stiffnessMultiplier);
            }
        }

        /// <summary>
        /// Set brake system type.
        /// </summary>
        public void SetBrakeSystem(int system)
        {
            brakeSystem = Mathf.Clamp(system, 0, 3);
            ApplyBrakeModifications();
        }

        /// <summary>
        /// Set brake bias (front to rear balance).
        /// </summary>
        public void SetBrakeBias(float bias)
        {
            brakeBiasPercentage = Mathf.Clamp01(bias);
        }

        /// <summary>
        /// Enable/disable regenerative braking.
        /// </summary>
        public void SetRegenerativeBraking(bool enabled)
        {
            hasRegenerativeBraking = enabled;
        }

        /// <summary>
        /// Apply brake modifications.
        /// </summary>
        private void ApplyBrakeModifications()
        {
            // Brake system affects braking power
            float brakePowerMultiplier = brakeSystem switch
            {
                0 => 1.0f,     // Stock
                1 => 1.3f,     // Performance
                2 => 1.6f,     // Racing
                3 => 1.8f,     // Carbon Ceramic (maximum)
                _ => 1.0f
            };

            // Apply brake force adjustment
            // This would modify vehicle physics braking distance
        }

        /// <summary>
        /// Set transmission/differential type.
        /// </summary>
        public void SetTransmissionType(int type)
        {
            transmissionType = Mathf.Clamp(type, 0, 3);
            ApplyTransmissionModifications();
        }

        /// <summary>
        /// Set gear ratio multiplier (affects acceleration/top speed trade-off).
        /// </summary>
        public void SetGearRatioMultiplier(float multiplier)
        {
            gearRatioMultiplier = Mathf.Clamp(multiplier, 0.8f, 1.2f);
            ApplyTransmissionModifications();
        }

        /// <summary>
        /// Apply transmission modifications.
        /// </summary>
        private void ApplyTransmissionModifications()
        {
            // LSD/differential type affects traction
            differentialLockPercentage = transmissionType switch
            {
                0 => 0f,    // Stock (open differential)
                1 => 0.5f,  // LSD (partial lock)
                2 => 0.8f,  // Mechanical LSD
                3 => 1.0f,  // Full race lock
                _ => 0f
            };
        }

        /// <summary>
        /// Set differential lock percentage.
        /// </summary>
        public void SetDifferentialLock(float lockPercentage)
        {
            differentialLockPercentage = Mathf.Clamp01(lockPercentage);
        }

        /// <summary>
        /// Set differential bias (front to rear grip distribution).
        /// </summary>
        public void SetDifferentialBias(float bias)
        {
            differentialBias = Mathf.Clamp(bias, 0.5f, 2f);
        }

        /// <summary>
        /// Set aerodynamic downforce level.
        /// </summary>
        public void SetDownforceLevel(float level)
        {
            downforceLevel = Mathf.Clamp(level, 0f, 100f);
            ApplyAerodynamicModifications();
        }

        /// <summary>
        /// Set drag multiplier (trade-off with downforce).
        /// </summary>
        public void SetDragMultiplier(float multiplier)
        {
            dragMultiplier = Mathf.Clamp(multiplier, 0.8f, 1.5f);
            ApplyAerodynamicModifications();
        }

        /// <summary>
        /// Apply aerodynamic modifications.
        /// </summary>
        private void ApplyAerodynamicModifications()
        {
            // More downforce = better grip but higher drag
            // This affects tire grip and top speed in physics
        }

        /// <summary>
        /// Apply all modifications at once.
        /// </summary>
        private void ApplyModifications()
        {
            ApplySuspensionModifications();
            ApplyBrakeModifications();
            ApplyTransmissionModifications();
            ApplyAerodynamicModifications();
        }

        /// <summary>
        /// Get the total performance rating (0-100).
        /// </summary>
        public float GetPerformanceRating()
        {
            float rating = 0f;

            // Suspension rating
            rating += (float)suspensionKit * 10f;

            // Brake rating
            rating += (float)brakeSystem * 15f;

            // Transmission rating
            rating += (float)transmissionType * 15f;

            // Aerodynamics rating
            rating += (downforceLevel / 100f) * 20f;

            return Mathf.Clamp(rating, 0f, 100f);
        }

        /// <summary>
        /// Get cornering grip multiplier (affected by suspension and downforce).
        /// </summary>
        public float GetCorneringGripMultiplier()
        {
            float gripMultiplier = 1f;

            // Suspension affects grip
            gripMultiplier += (suspensionKit * 0.15f);

            // Downforce improves grip
            gripMultiplier += (downforceLevel / 100f) * 0.3f;

            // Differential lock improves traction
            gripMultiplier += (differentialLockPercentage * 0.2f);

            return Mathf.Clamp(gripMultiplier, 0.8f, 2f);
        }

        /// <summary>
        /// Get acceleration multiplier (affected by gearing and transmission).
        /// </summary>
        public float GetAccelerationMultiplier()
        {
            // Shorter gears = faster acceleration but lower top speed
            float accelMultiplier = 1f / gearRatioMultiplier; // Inverse relationship
            accelMultiplier += (differentialLockPercentage * 0.15f); // Better traction
            return Mathf.Clamp(accelMultiplier, 0.7f, 1.3f);
        }

        /// <summary>
        /// Get top speed multiplier (affected by gearing and drag).
        /// </summary>
        public float GetTopSpeedMultiplier()
        {
            float speedMultiplier = gearRatioMultiplier;
            speedMultiplier -= (downforceLevel / 100f) * 0.15f; // Downforce reduces top speed
            speedMultiplier -= (dragMultiplier - 1f) * 0.2f;
            return Mathf.Clamp(speedMultiplier, 0.7f, 1.3f);
        }

        /// <summary>
        /// Get current garage modification settings.
        /// </summary>
        public GarageModSettings GetGarageModSettings()
        {
            return new GarageModSettings
            {
                SuspensionKit = suspensionKit,
                SuspensionHeight = suspensionHeight,
                HasAntiRollBars = hasAntiRollBars,
                BrakeSystem = brakeSystem,
                BrakeBias = brakeBiasPercentage,
                TransmissionType = transmissionType,
                GearRatioMultiplier = gearRatioMultiplier,
                DifferentialLock = differentialLockPercentage,
                DownforceLevel = downforceLevel,
                DragMultiplier = dragMultiplier
            };
        }

        // Getters
        public int GetSuspensionKit() => suspensionKit;
        public int GetBrakeSystem() => brakeSystem;
        public int GetTransmissionType() => transmissionType;
        public float GetPerformanceBoost() => GetPerformanceRating() / 50f; // 0-2x multiplier
    }
}
