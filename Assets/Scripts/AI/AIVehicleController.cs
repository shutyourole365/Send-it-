using UnityEngine;
using SendIt.Physics;

namespace SendIt.AI
{
    /// <summary>
    /// Controls NPC/AI vehicle behavior and physics.
    /// Handles autonomous driving, racing, and traffic behavior.
    /// </summary>
    public class AIVehicleController : MonoBehaviour
    {
        public enum AIBehavior
        {
            Traffic,      // Follow traffic rules, avoid collisions
            Racing,       // Compete aggressively
            Patrolling,   // Wander and explore
            Following     // Follow player vehicle
        }

        [SerializeField] private AIBehavior currentBehavior = AIBehavior.Traffic;
        [SerializeField] private float maxSpeed = 60f; // km/h
        [SerializeField] private float aggressiveness = 0.5f; // 0-1, how aggressive the AI drives
        [SerializeField] private float awareness = 50f; // Distance AI can "see"

        private Rigidbody vehicleRigidbody;
        private WheelCollider[] wheelColliders = new WheelCollider[4];
        private Transform[] wheelTransforms = new Transform[4];

        // AI decision making
        private Vector3 targetDirection = Vector3.forward;
        private float targetSpeed = 0f;
        private float currentThrottle = 0f;
        private float currentBrake = 0f;
        private float currentSteer = 0f;

        // Obstacle avoidance
        private RaycastHit[] sensorHits = new RaycastHit[5];
        private bool[] sensorDetections = new bool[5];

        // Performance tracking
        private float currentSpeed = 0f;
        private int currentGear = 1;

        private bool isInitialized;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize AI vehicle controller.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
                return;

            // Get vehicle components
            vehicleRigidbody = GetComponent<Rigidbody>();
            WheelCollider[] colliders = GetComponentsInChildren<WheelCollider>();
            for (int i = 0; i < Mathf.Min(colliders.Length, 4); i++)
            {
                wheelColliders[i] = colliders[i];
            }

            // Get wheel transforms
            Transform[] transforms = GetComponentsInChildren<Transform>();
            int wheelIndex = 0;
            foreach (Transform t in transforms)
            {
                if (t.name.Contains("Wheel") && wheelIndex < 4)
                {
                    wheelTransforms[wheelIndex] = t;
                    wheelIndex++;
                }
            }

            isInitialized = true;
            Debug.Log($"AI Vehicle initialized - Behavior: {currentBehavior}");
        }

        private void FixedUpdate()
        {
            if (!isInitialized || vehicleRigidbody == null)
                return;

            // Update sensors
            UpdateSensors();

            // Make driving decisions based on behavior
            MakeDrivingDecision();

            // Apply physics
            ApplyMotorForces();
            UpdateWheelVisuals();

            // Track current speed
            currentSpeed = vehicleRigidbody.velocity.magnitude * 3.6f; // Convert to km/h
        }

        /// <summary>
        /// Update AI sensors (raycasts for obstacle detection).
        /// </summary>
        private void UpdateSensors()
        {
            Vector3 forward = transform.forward;
            Vector3 position = transform.position;

            // Front center
            Physics.Raycast(position, forward, out sensorHits[0], awareness);
            sensorDetections[0] = sensorHits[0].collider != null;

            // Front left
            Vector3 leftForward = Quaternion.Euler(0, 30, 0) * forward;
            Physics.Raycast(position, leftForward, out sensorHits[1], awareness);
            sensorDetections[1] = sensorHits[1].collider != null;

            // Front right
            Vector3 rightForward = Quaternion.Euler(0, -30, 0) * forward;
            Physics.Raycast(position, rightForward, out sensorHits[2], awareness);
            sensorDetections[2] = sensorHits[2].collider != null;

            // Left side
            Physics.Raycast(position, transform.right, out sensorHits[3], awareness);
            sensorDetections[3] = sensorHits[3].collider != null;

            // Right side
            Physics.Raycast(position, -transform.right, out sensorHits[4], awareness);
            sensorDetections[4] = sensorHits[4].collider != null;
        }

        /// <summary>
        /// Make driving decisions based on sensors and behavior.
        /// </summary>
        private void MakeDrivingDecision()
        {
            switch (currentBehavior)
            {
                case AIBehavior.Traffic:
                    MakeTrafficDecision();
                    break;
                case AIBehavior.Racing:
                    MakeRacingDecision();
                    break;
                case AIBehavior.Patrolling:
                    MakePatrollingDecision();
                    break;
                case AIBehavior.Following:
                    MakeFollowingDecision();
                    break;
            }
        }

        /// <summary>
        /// Traffic behavior - avoid obstacles, maintain speed limit.
        /// </summary>
        private void MakeTrafficDecision()
        {
            targetSpeed = maxSpeed;
            currentThrottle = 0.7f;

            // Obstacle avoidance
            if (sensorDetections[0]) // Front obstacle
            {
                currentBrake = 0.5f;
                currentThrottle = 0f;
            }

            // Lane changing behavior
            if (sensorDetections[1] && !sensorDetections[3])
            {
                currentSteer = -0.5f; // Steer right
            }
            else if (sensorDetections[2] && !sensorDetections[4])
            {
                currentSteer = 0.5f; // Steer left
            }
            else
            {
                currentSteer = 0f;
            }

            // Speed adjustment
            if (currentSpeed > targetSpeed)
            {
                currentBrake = 0.3f;
                currentThrottle = 0f;
            }
            else if (currentSpeed < targetSpeed * 0.8f)
            {
                currentThrottle = 0.8f;
                currentBrake = 0f;
            }
        }

        /// <summary>
        /// Racing behavior - aggressive driving, push limits.
        /// </summary>
        private void MakeRacingDecision()
        {
            targetSpeed = maxSpeed * (1f + aggressiveness * 0.5f);
            currentThrottle = 0.9f + (aggressiveness * 0.1f);

            // Aggressive steering
            if (sensorDetections[0])
            {
                // Late braking for competitive advantage
                currentBrake = 0.8f - (aggressiveness * 0.3f); // Less braking if aggressive
                currentThrottle = 0f;
            }

            // Racing line - cutoff inside of turns
            float turnIntensity = 0.3f + (aggressiveness * 0.7f);

            if (sensorDetections[1])
                currentSteer = Mathf.Lerp(0f, 1f, turnIntensity);
            else if (sensorDetections[2])
                currentSteer = Mathf.Lerp(0f, -1f, turnIntensity);
            else
                currentSteer = 0f;
        }

        /// <summary>
        /// Patrolling behavior - wander around course.
        /// </summary>
        private void MakePatrollingDecision()
        {
            targetSpeed = maxSpeed * 0.6f;
            currentThrottle = 0.5f;

            // Random wandering with obstacle avoidance
            if (sensorDetections[0])
            {
                currentBrake = 0.4f;
                currentThrottle = 0f;
                // Random steer to avoid
                currentSteer = (Random.value > 0.5f) ? 0.5f : -0.5f;
            }
            else
            {
                currentSteer = Mathf.Sin(Time.time * 0.5f) * 0.3f; // Gentle wandering
            }
        }

        /// <summary>
        /// Following behavior - chase player vehicle.
        /// </summary>
        private void MakeFollowingDecision()
        {
            VehicleController player = FindObjectOfType<VehicleController>();
            if (player != null)
            {
                Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
                float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

                // Steer toward player
                if (angleToPlayer > 5f)
                {
                    currentSteer = Mathf.Sign(Vector3.Cross(transform.forward, directionToPlayer).y);
                }

                // Match player speed
                float playerSpeed = player.GetSpeed() * 3.6f;
                if (playerSpeed > currentSpeed)
                {
                    currentThrottle = 0.8f;
                    currentBrake = 0f;
                }
                else if (playerSpeed < currentSpeed - 10f)
                {
                    currentThrottle = 0f;
                    currentBrake = 0.3f;
                }
            }
        }

        /// <summary>
        /// Apply motor and steering forces to wheels.
        /// </summary>
        private void ApplyMotorForces()
        {
            // Apply throttle/brake to rear wheels
            for (int i = 2; i < 4; i++)
            {
                if (wheelColliders[i] != null)
                {
                    float motorForce = currentThrottle * 3000f;
                    float brakeTorque = currentBrake * 3000f;

                    wheelColliders[i].motorTorque = motorForce;
                    wheelColliders[i].brakeTorque = brakeTorque;
                }
            }

            // Apply steering to front wheels
            for (int i = 0; i < 2; i++)
            {
                if (wheelColliders[i] != null)
                {
                    wheelColliders[i].steerAngle = currentSteer * 35f;
                    wheelColliders[i].brakeTorque = currentBrake * 2000f;
                }
            }
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
        /// Set AI behavior mode.
        /// </summary>
        public void SetBehavior(AIBehavior behavior)
        {
            currentBehavior = behavior;
        }

        /// <summary>
        /// Set AI aggressiveness (0-1).
        /// </summary>
        public void SetAggressiveness(float aggression)
        {
            aggressiveness = Mathf.Clamp01(aggression);
        }

        /// <summary>
        /// Set maximum speed for AI.
        /// </summary>
        public void SetMaxSpeed(float speed)
        {
            maxSpeed = speed;
        }

        /// <summary>
        /// Get current speed in km/h.
        /// </summary>
        public float GetSpeed() => currentSpeed;

        /// <summary>
        /// Get current behavior.
        /// </summary>
        public AIBehavior GetBehavior() => currentBehavior;

        /// <summary>
        /// Get AI info for debugging.
        /// </summary>
        public string GetAIInfo()
        {
            string info = $"AI: {currentBehavior}\n";
            info += $"Speed: {currentSpeed:F1} km/h\n";
            info += $"Throttle: {currentThrottle:F2}\n";
            info += $"Brake: {currentBrake:F2}\n";
            return info;
        }
    }
}
