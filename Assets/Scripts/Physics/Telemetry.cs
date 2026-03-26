using UnityEngine;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Real-time telemetry system for collecting and displaying vehicle physics data.
    /// Useful for diagnostics, tuning, and performance monitoring.
    /// </summary>
    public class Telemetry
    {
        // Engine data
        private float engineRPM;
        private float enginePower;
        private float engineTorque;
        private float throttleInput;

        // Speed data
        private float vehicleSpeedKmh;
        private float vehicleSpeedMph;

        // Gear and transmission
        private int currentGear;
        private float gearRatio;

        // Tire data
        private float[] tireTemperatures = new float[4];
        private float[] tireWear = new float[4];
        private float[] tireGrip = new float[4];
        private float[] tireSlipAngles = new float[4];

        // Suspension data
        private float[] suspensionCompression = new float[4];
        private float[] suspensionVelocity = new float[4];

        // Dynamics data
        private float[] wheelLoads = new float[4];
        private float longitudinalAccel;
        private float lateralAccel;
        private float rollAngle;

        // Brake and traction
        private float brakePressure;
        private float traction; // 0-1, grip level

        public struct TelemetryFrame
        {
            // Engine
            public float EngineRPM;
            public float EnginePower;
            public float EngineTorque;
            public int CurrentGear;

            // Speed
            public float SpeedKmh;
            public float SpeedMph;

            // Tires (arrays)
            public float[] TireTemperatures;
            public float[] TireWear;
            public float[] TireGrip;
            public float[] TireSlipAngles;

            // Suspension
            public float[] SuspensionCompression;

            // Dynamics
            public float[] WheelLoads;
            public float LongitudinalAccel;
            public float LateralAccel;
            public float RollAngle;
            public float TractionLevel;
        }

        public Telemetry()
        {
            tireTemperatures = new float[4];
            tireWear = new float[4];
            tireGrip = new float[4];
            tireSlipAngles = new float[4];
            suspensionCompression = new float[4];
            suspensionVelocity = new float[4];
            wheelLoads = new float[4];
        }

        /// <summary>
        /// Update telemetry with current vehicle state.
        /// </summary>
        public void UpdateTelemetry(
            VehicleController vehicleController,
            Engine engine,
            Transmission transmission,
            VehicleDynamics dynamics,
            Tire[] tires,
            Suspension[] suspensions,
            WheelContact[] wheelContacts,
            Rigidbody vehicleBody,
            float throttle,
            float brake)
        {
            if (vehicleController == null || engine == null)
                return;

            // Engine data
            engineRPM = vehicleController.GetCurrentRPM();
            engineTorque = engine.CalculateTorque(engineRPM);
            enginePower = (engineTorque * engineRPM) / 5252f;
            currentGear = vehicleController.GetCurrentGear();
            throttleInput = throttle;

            // Speed
            vehicleSpeedKmh = vehicleBody.velocity.magnitude * 3.6f;
            vehicleSpeedMph = vehicleSpeedKmh / 1.609f;

            // Transmission
            if (transmission != null)
            {
                gearRatio = transmission.GetGearRatio(currentGear);
            }

            // Tire data
            for (int i = 0; i < 4 && i < tires.Length; i++)
            {
                if (tires[i] != null)
                {
                    tireTemperatures[i] = tires[i].GetCurrentTemperature();
                    tireWear[i] = tires[i].GetWearAmount();
                    tireGrip[i] = tires[i].GetGripRating();
                    tireSlipAngles[i] = tires[i].GetSlipAngle() * Mathf.Rad2Deg;
                }

                if (wheelContacts[i] != null)
                {
                    var contactData = wheelContacts[i].GetContactData();
                    wheelLoads[i] = contactData.NormalForce;
                }
            }

            // Suspension data
            for (int i = 0; i < 4 && i < suspensions.Length; i++)
            {
                if (suspensions[i] != null)
                {
                    suspensionCompression[i] = suspensions[i].GetCurrentCompression();
                    suspensionVelocity[i] = suspensions[i].GetCompressionVelocity();
                }
            }

            // Dynamics data
            if (dynamics != null)
            {
                var dynamicsState = dynamics.GetDynamicsState();
                longitudinalAccel = vehicleBody.acceleration.z;
                lateralAccel = vehicleBody.acceleration.x;
                rollAngle = dynamicsState.RollAngle * Mathf.Rad2Deg;
            }

            // Brake pressure
            brakePressure = brake;

            // Traction calculation (average grip of all wheels)
            traction = 0f;
            for (int i = 0; i < 4; i++)
            {
                traction += tireGrip[i];
            }
            traction /= 4f;
        }

        /// <summary>
        /// Get a complete telemetry frame snapshot.
        /// </summary>
        public TelemetryFrame GetFrame()
        {
            return new TelemetryFrame
            {
                EngineRPM = engineRPM,
                EnginePower = enginePower,
                EngineTorque = engineTorque,
                CurrentGear = currentGear,
                SpeedKmh = vehicleSpeedKmh,
                SpeedMph = vehicleSpeedMph,
                TireTemperatures = tireTemperatures,
                TireWear = tireWear,
                TireGrip = tireGrip,
                TireSlipAngles = tireSlipAngles,
                SuspensionCompression = suspensionCompression,
                WheelLoads = wheelLoads,
                LongitudinalAccel = longitudinalAccel,
                LateralAccel = lateralAccel,
                RollAngle = rollAngle,
                TractionLevel = traction
            };
        }

        /// <summary>
        /// Get a formatted telemetry string for debugging.
        /// </summary>
        public string GetFormattedTelemetry()
        {
            string telemetry = "";
            telemetry += $"=== ENGINE ===\n";
            telemetry += $"RPM: {engineRPM:F0} | Power: {enginePower:F1} HP | Torque: {engineTorque:F1} Nm\n";
            telemetry += $"Gear: {currentGear} | Throttle: {throttleInput * 100:F1}%\n\n";

            telemetry += $"=== SPEED ===\n";
            telemetry += $"{vehicleSpeedKmh:F1} km/h | {vehicleSpeedMph:F1} mph\n\n";

            telemetry += $"=== TIRES ===\n";
            for (int i = 0; i < 4; i++)
            {
                string wheelName = GetWheelName(i);
                telemetry += $"{wheelName}: {tireTemperatures[i]:F1}°C | Wear: {tireWear[i] * 100:F1}% | Grip: {tireGrip[i]:F2}\n";
            }
            telemetry += "\n";

            telemetry += $"=== SUSPENSION ===\n";
            for (int i = 0; i < 4; i++)
            {
                string wheelName = GetWheelName(i);
                telemetry += $"{wheelName}: {suspensionCompression[i]:F3}m compression\n";
            }
            telemetry += "\n";

            telemetry += $"=== DYNAMICS ===\n";
            telemetry += $"Lateral Accel: {lateralAccel:F2} m/s² | Roll: {rollAngle:F1}°\n";
            telemetry += $"Traction: {traction * 100:F1}%\n";

            return telemetry;
        }

        /// <summary>
        /// Get wheel name from index.
        /// </summary>
        private string GetWheelName(int index)
        {
            return index switch
            {
                0 => "FL",
                1 => "FR",
                2 => "RL",
                3 => "RR",
                _ => "??",
            };
        }

        // Getters for individual telemetry values
        public float GetEngineRPM() => engineRPM;
        public float GetEnginePower() => enginePower;
        public float GetEngineTorque() => engineTorque;
        public float GetSpeedKmh() => vehicleSpeedKmh;
        public float GetSpeedMph() => vehicleSpeedMph;
        public int GetCurrentGear() => currentGear;
        public float GetTraction() => traction;
        public float GetRollAngle() => rollAngle;

        public float[] GetTireTemperatures() => tireTemperatures;
        public float[] GetTireWear() => tireWear;
        public float[] GetWheelLoads() => wheelLoads;
    }
}
