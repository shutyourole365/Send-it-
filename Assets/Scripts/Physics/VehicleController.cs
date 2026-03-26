using UnityEngine;
using SendIt.Tuning;
using SendIt.Data;
using SendIt.Graphics;

namespace SendIt.Physics
{
    /// <summary>
    /// Main vehicle controller integrating advanced physics simulation.
    /// Manages engine, transmission, suspension, tires, aerodynamics, and vehicle dynamics.
    /// </summary>
    public class VehicleController : MonoBehaviour
    {
        [SerializeField] private Rigidbody vehicleRigidbody;
        [SerializeField] private Transform[] wheelTransforms = new Transform[4];
        [SerializeField] private WheelCollider[] wheelColliders = new WheelCollider[4];
        [SerializeField] private bool enableTelemetry = true;

        private TuningManager tuningManager;
        private VehicleData vehicleData;

        // Physics systems
        private Engine engine;
        private Transmission transmission;
        private Suspension[] suspensions = new Suspension[4];
        private Tire[] tires = new Tire[4];
        private WheelContact[] wheelContacts = new WheelContact[4];
        private Aerodynamics aerodynamics;
        private VehicleDynamics vehicleDynamics;
        private Telemetry telemetry;

        // Graphics systems
        private SkidMarkManager skidMarkManager;

        // Input
        private float throttleInput;
        private float steerInput;
        private float brakeInput;

        // Engine state
        private float currentRPM;
        private int currentGear;
        private Engine.EngineState engineState;

        private bool isInitialized;

        private void OnEnable()
        {
            if (tuningManager != null)
            {
                tuningManager.OnPhysicsParameterChanged += HandlePhysicsParameterChanged;
            }
        }

        private void OnDisable()
        {
            if (tuningManager != null)
            {
                tuningManager.OnPhysicsParameterChanged -= HandlePhysicsParameterChanged;
            }
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize all physics systems.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            // Get references
            if (vehicleRigidbody == null)
                vehicleRigidbody = GetComponent<Rigidbody>();

            tuningManager = TuningManager.Instance;
            if (tuningManager == null)
            {
                Debug.LogError("TuningManager not found in scene!");
                return;
            }

            vehicleData = tuningManager.GetVehicleData();

            // Initialize physics subsystems
            engine = new Engine(vehicleData.Physics);
            transmission = new Transmission(vehicleData.Physics);
            aerodynamics = new Aerodynamics(vehicleData.Physics);
            vehicleDynamics = new VehicleDynamics(vehicleRigidbody, vehicleData.Physics);

            // Initialize per-wheel systems
            for (int i = 0; i < 4; i++)
            {
                suspensions[i] = new Suspension(vehicleData.Physics, i);
                tires[i] = new Tire(vehicleData.Physics, i);
                wheelContacts[i] = new WheelContact(i);
            }

            // Initialize telemetry
            if (enableTelemetry)
            {
                telemetry = new Telemetry();
            }

            // Initialize graphics systems
            skidMarkManager = GetComponent<SkidMarkManager>();
            if (skidMarkManager == null)
            {
                skidMarkManager = gameObject.AddComponent<SkidMarkManager>();
            }
            skidMarkManager.Initialize();

            // Apply initial configuration
            if (vehicleRigidbody != null)
            {
                vehicleRigidbody.mass = vehicleData.Physics.TotalMass;
                vehicleRigidbody.drag = 0.1f; // Minimal drag, physics handles aerodynamics
                vehicleRigidbody.angularDrag = 0.3f;
            }

            currentGear = 1;
            currentRPM = engine.IdleRPM;

            isInitialized = true;
        }

        private void FixedUpdate()
        {
            if (!isInitialized)
                return;

            // Get input
            GetInput();

            // Update engine and transmission
            UpdateEngine();
            UpdateTransmission();

            // Update vehicle dynamics
            if (vehicleDynamics != null)
            {
                vehicleDynamics.Update(vehicleRigidbody, new float[4]);
            }

            // Update wheel systems and apply forces
            UpdateWheels();

            // Apply motor and brake forces
            ApplyMotorForces();

            // Apply aerodynamic forces
            ApplyAerodynamicForces();

            // Update wheel visuals
            UpdateWheelVisuals();

            // Update telemetry
            if (enableTelemetry && telemetry != null)
            {
                telemetry.UpdateTelemetry(this, engine, transmission, vehicleDynamics, tires, suspensions, wheelContacts, vehicleRigidbody, throttleInput, brakeInput);
            }
        }

        /// <summary>
        /// Get input from player.
        /// </summary>
        private void GetInput()
        {
            throttleInput = Input.GetAxis("Vertical");
            steerInput = Input.GetAxis("Horizontal");
            brakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;
        }

        /// <summary>
        /// Update engine with realistic torque and rev-limiting.
        /// </summary>
        private void UpdateEngine()
        {
            engineState = engine.Update(currentRPM, throttleInput, currentGear);
            currentRPM = engineState.RPM;
        }

        /// <summary>
        /// Update transmission with auto-shifting logic.
        /// </summary>
        private void UpdateTransmission()
        {
            float maxRPM = tuningManager.GetPhysicsParameter("MaxRPM").CurrentValue;
            int gearCount = (int)tuningManager.GetPhysicsParameter("GearCount").CurrentValue;

            // Auto-shift up when near max RPM
            if (currentRPM > maxRPM * 0.92f && currentGear < gearCount && throttleInput > 0.5f)
            {
                currentGear = Mathf.Min(currentGear + 1, gearCount);
            }
            // Auto-shift down when RPM drops too low
            else if (currentRPM < engine.IdleRPM * 1.5f && currentGear > 1)
            {
                currentGear = Mathf.Max(currentGear - 1, 1);
            }
        }

        /// <summary>
        /// Update all wheel systems (suspension, tires, contacts).
        /// </summary>
        private void UpdateWheels()
        {
            for (int i = 0; i < 4; i++)
            {
                if (wheelColliders[i] != null)
                {
                    // Update suspension
                    suspensions[i].Update(wheelColliders[i], vehicleData.Physics, 0f);

                    // Update wheel contact (slip, normal force, friction)
                    wheelContacts[i].Update(wheelColliders[i], vehicleRigidbody, tires[i]);

                    // Update visual effects (skid marks, surface deformation, dirt)
                    if (skidMarkManager != null)
                    {
                        UpdateWheelVisualEffects(i);
                    }
                }
            }
        }

        /// <summary>
        /// Update visual effects for wheel contact (skid marks, surface deformation, dirt).
        /// </summary>
        private void UpdateWheelVisualEffects(int wheelIndex)
        {
            WheelCollider wheelCollider = wheelColliders[wheelIndex];
            if (wheelCollider == null || !wheelCollider.isGrounded)
                return;

            WheelContact wheelContact = wheelContacts[wheelIndex];
            if (wheelContact == null)
                return;

            // Get contact information
            RaycastHit hit;
            if (wheelCollider.GetGroundHit(out hit))
            {
                Vector3 contactPoint = hit.point;
                Vector3 contactNormal = hit.normal;
                string terrainTag = hit.collider.gameObject.tag;

                // Get slip and load information
                float slipRatio = wheelContact.GetSlipRatio();
                float slipAngle = wheelContact.GetSlipAngle();
                float normalForce = wheelContact.GetNormalForce();

                // Get tire temperature
                float tireTemperature = tires[wheelIndex].GetCurrentTemperature();

                // Call skid mark manager
                skidMarkManager.OnWheelContact(contactPoint, contactNormal, terrainTag,
                                              slipRatio, slipAngle, normalForce);
            }
        }

        /// <summary>
        /// Apply motor torque and braking to wheels.
        /// </summary>
        private void ApplyMotorForces()
        {
            float gearRatio = transmission.GetGearRatio(currentGear);
            float finalRatio = tuningManager.GetPhysicsParameter("FinalDriveRatio").CurrentValue;

            // Calculate available torque from engine
            float motorTorque = engineState.Torque * gearRatio * finalRatio;

            // Apply to rear wheels (RWD configuration)
            for (int i = 2; i < 4; i++)
            {
                if (wheelColliders[i] != null)
                {
                    float driveTorque = motorTorque * Mathf.Clamp01(throttleInput);
                    wheelColliders[i].motorTorque = driveTorque;
                    wheelColliders[i].brakeTorque = brakeInput * 3000f;
                }
            }

            // Front wheels: steering and braking only
            for (int i = 0; i < 2; i++)
            {
                if (wheelColliders[i] != null)
                {
                    wheelColliders[i].steerAngle = steerInput * 35f; // Max 35° steering angle
                    wheelColliders[i].brakeTorque = brakeInput * 2000f; // Less braking on front
                }
            }
        }

        /// <summary>
        /// Apply aerodynamic forces at high speeds.
        /// </summary>
        private void ApplyAerodynamicForces()
        {
            if (vehicleRigidbody == null)
                return;

            Vector3 velocity = vehicleRigidbody.velocity;

            // Apply drag
            Vector3 dragForce = aerodynamics.CalculateDragForce(velocity);
            vehicleRigidbody.AddForce(dragForce, ForceMode.Force);

            // Apply downforce (improves grip at high speeds)
            Vector3 downforceForce = aerodynamics.CalculateDownforce(velocity);
            vehicleRigidbody.AddForce(downforceForce, ForceMode.Force);
        }

        /// <summary>
        /// Update wheel visual positions and rotations.
        /// </summary>
        private void UpdateWheelVisuals()
        {
            for (int i = 0; i < 4; i++)
            {
                if (wheelColliders[i] != null && wheelTransforms[i] != null)
                {
                    wheelColliders[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
                    wheelTransforms[i].position = pos;
                    wheelTransforms[i].rotation = rot;
                }
            }
        }

        /// <summary>
        /// Handle real-time tuning parameter changes.
        /// </summary>
        private void HandlePhysicsParameterChanged(string paramName, float newValue)
        {
            switch (paramName)
            {
                case "TotalMass":
                    if (vehicleRigidbody != null)
                        vehicleRigidbody.mass = newValue;
                    if (vehicleDynamics != null)
                        vehicleDynamics.UpdateMass(newValue);
                    break;

                case "SpringStiffness":
                    for (int i = 0; i < 4; i++)
                        suspensions[i]?.UpdateStiffness(newValue);
                    break;

                case "MaxRPM":
                case "HorsePower":
                case "TorquePeakRPM":
                case "EngineResponsiveness":
                    engine?.UpdateParameters(vehicleData.Physics);
                    break;

                case "DragCoefficient":
                case "DownforceCoefficient":
                case "SpoilerAngle":
                    aerodynamics?.UpdateParameters(vehicleData.Physics);
                    break;
            }
        }

        // Getters for diagnostics and UI
        public float GetCurrentRPM() => currentRPM;
        public int GetCurrentGear() => currentGear;
        public float GetEnginePower() => engineState.Power;
        public float GetEngineTorque() => engineState.Torque;
        public float GetSpeed() => vehicleRigidbody.velocity.magnitude;
        public float GetSpeedKmh() => GetSpeed() * 3.6f;
        public Telemetry GetTelemetry() => telemetry;
        public VehicleDynamics GetDynamics() => vehicleDynamics;
        public SkidMarkManager GetSkidMarkManager() => skidMarkManager;
    }
}
