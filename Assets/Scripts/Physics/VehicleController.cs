using UnityEngine;
using SendIt.Tuning;
using SendIt.Data;

namespace SendIt.Physics
{
    /// <summary>
    /// Main vehicle controller that integrates physics simulation with tuning parameters.
    /// Manages the rigidbody, wheels, and applies forces based on physics parameters.
    /// </summary>
    public class VehicleController : MonoBehaviour
    {
        [SerializeField] private Rigidbody vehicleRigidbody;
        [SerializeField] private Transform[] wheelTransforms = new Transform[4];
        [SerializeField] private WheelCollider[] wheelColliders = new WheelCollider[4];

        private TuningManager tuningManager;
        private VehicleData vehicleData;

        // Cached physics components
        private Engine engine;
        private Transmission transmission;
        private Suspension[] suspensions = new Suspension[4];
        private Tire[] tires = new Tire[4];
        private Aerodynamics aerodynamics;

        // Input
        private float throttleInput;
        private float steerInput;
        private float brakeInput;

        // Current engine state
        private float currentRPM;
        private int currentGear;
        private float currentEngineForce;

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
        /// Initialize the vehicle controller with physics components.
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

            // Initialize physics systems
            engine = new Engine(vehicleData.Physics);
            transmission = new Transmission(vehicleData.Physics);
            aerodynamics = new Aerodynamics(vehicleData.Physics);

            // Initialize suspension and tires for each wheel
            for (int i = 0; i < 4; i++)
            {
                suspensions[i] = new Suspension(vehicleData.Physics, i);
                tires[i] = new Tire(vehicleData.Physics, i);
            }

            // Apply initial mass
            if (vehicleRigidbody != null)
            {
                vehicleRigidbody.mass = vehicleData.Physics.TotalMass;
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

            // Update engine state
            UpdateEngine();

            // Update transmission
            UpdateTransmission();

            // Apply forces
            ApplyMotorForces();
            ApplySuspensionForces();
            ApplyAerodynamicForces();

            // Update wheel visuals
            UpdateWheelVisuals();
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
        /// Update engine simulation based on throttle and transmission.
        /// </summary>
        private void UpdateEngine()
        {
            Engine.EngineState state = engine.Update(currentRPM, throttleInput, currentGear);
            currentRPM = state.RPM;
            currentEngineForce = state.Torque;
        }

        /// <summary>
        /// Update transmission and handle gear changes.
        /// </summary>
        private void UpdateTransmission()
        {
            // Simple auto-shifting logic
            if (currentRPM > tuningManager.GetPhysicsParameter("MaxRPM").CurrentValue * 0.95f && currentGear < tuningManager.GetPhysicsParameter("GearCount").CurrentValue)
            {
                currentGear++;
            }
            else if (currentRPM < tuningManager.GetPhysicsParameter("MaxRPM").CurrentValue * 0.3f && currentGear > 1)
            {
                currentGear--;
            }
        }

        /// <summary>
        /// Apply motor forces to the wheels.
        /// </summary>
        private void ApplyMotorForces()
        {
            if (wheelColliders.Length < 4)
                return;

            float gearRatio = transmission.GetGearRatio(currentGear);
            float finalRatio = tuningManager.GetPhysicsParameter("FinalDriveRatio").CurrentValue;
            float driveForce = currentEngineForce * gearRatio * finalRatio * (1f - brakeInput);

            // Apply force to rear wheels (RWD configuration)
            for (int i = 2; i < 4; i++)
            {
                wheelColliders[i].motorTorque = driveForce * throttleInput;
                wheelColliders[i].brakeTorque = brakeInput * 3000f;
            }

            // Front wheels for steering
            for (int i = 0; i < 2; i++)
            {
                wheelColliders[i].steerAngle = steerInput * 30f;
                wheelColliders[i].brakeTorque = brakeInput * 3000f;
            }
        }

        /// <summary>
        /// Apply suspension forces.
        /// </summary>
        private void ApplySuspensionForces()
        {
            for (int i = 0; i < 4; i++)
            {
                if (wheelColliders[i] != null)
                {
                    suspensions[i].Update(wheelColliders[i], tuningManager.GetVehicleData().Physics);
                }
            }
        }

        /// <summary>
        /// Apply aerodynamic forces.
        /// </summary>
        private void ApplyAerodynamicForces()
        {
            Vector3 velocity = vehicleRigidbody.velocity;
            Vector3 aeroDrag = aerodynamics.CalculateDragForce(velocity);
            Vector3 aeroDownforce = aerodynamics.CalculateDownforce(velocity);

            vehicleRigidbody.AddForce(aeroDrag, ForceMode.Force);
            vehicleRigidbody.AddForce(aeroDownforce, ForceMode.Force);
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
        /// Handle physics parameter changes from tuning.
        /// </summary>
        private void HandlePhysicsParameterChanged(string paramName, float newValue)
        {
            switch (paramName)
            {
                case "TotalMass":
                    if (vehicleRigidbody != null)
                        vehicleRigidbody.mass = newValue;
                    break;

                case "SpringStiffness":
                    for (int i = 0; i < 4; i++)
                        suspensions[i]?.UpdateStiffness(newValue);
                    break;

                case "MaxRPM":
                case "HorsePower":
                case "TorquePeakRPM":
                    if (engine != null)
                        engine.UpdateParameters(vehicleData.Physics);
                    break;

                case "DragCoefficient":
                case "DownforceCoefficient":
                    if (aerodynamics != null)
                        aerodynamics.UpdateParameters(vehicleData.Physics);
                    break;
            }
        }

        // Getters for diagnostics
        public float GetCurrentRPM() => currentRPM;
        public int GetCurrentGear() => currentGear;
        public float GetEngineForce() => currentEngineForce;
        public float GetSpeed() => vehicleRigidbody.velocity.magnitude;
    }
}
