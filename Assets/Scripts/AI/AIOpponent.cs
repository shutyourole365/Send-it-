using UnityEngine;
using System.Collections.Generic;
using SendIt.Physics;
using SendIt.Tuning;

namespace SendIt.AI
{
    /// <summary>
    /// AI opponent driver for competitive racing.
    /// Manages vehicle control, behavior decisions, and difficulty scaling.
    /// </summary>
    public class AIOpponent : MonoBehaviour
    {
        /// <summary>
        /// AI difficulty level affecting behavior and performance.
        /// </summary>
        public enum DifficultyLevel
        {
            Rookie = 0,      // 70% of player performance
            Intermediate = 1, // 85% of player performance
            Professional = 2, // 95% of player performance
            Expert = 3        // 110% of player performance
        }

        /// <summary>
        /// Racing behavior state.
        /// </summary>
        public enum RacingBehavior
        {
            Following,        // Following ideal racing line
            Attacking,        // Attempting to overtake
            Defending,        // Protecting position
            Recovering,       // Recovering from mistake
            Damaged,          // Driving damaged vehicle
            Qualifying        // Hot lap mode
        }

        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private DifficultyLevel difficulty = DifficultyLevel.Intermediate;
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private float targetSpeed = 50f;

        private RacingBehavior currentBehavior = RacingBehavior.Following;
        private int currentWaypoint;
        private float brakingDistance;
        private float cornersCompleted;
        private float bestLapTime = float.MaxValue;
        private float currentLapTime;
        private float sessionStartTime;

        // Performance characteristics
        private float accelerationFactor;
        private float brakingFactor;
        private float corneringFactor;
        private float consistencyFactor;

        // Opponent tracking
        private Transform playerTransform;
        private float distanceToPlayer;
        private bool isPlayerAhead;
        private float gainsOnPlayer; // Per lap time advantage

        // Behavioral state
        private float confidence;
        private float aggression;
        private float riskTolerance;
        private RacingState currentState;

        private struct RacingState
        {
            public float ThrottleInput;
            public float BrakeInput;
            public float SteerInput;
            public float ClutchInput;
            public int GearInput;
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (vehicleController == null)
                vehicleController = GetComponent<VehicleController>();

            // Set up difficulty scaling
            SetupDifficultyParameters();

            sessionStartTime = Time.time;
            currentWaypoint = 0;
            confidence = 0.5f;
            aggression = 0.5f;

            // Find player vehicle
            var allVehicles = FindObjectsOfType<VehicleController>();
            foreach (var vehicle in allVehicles)
            {
                if (vehicle != vehicleController)
                {
                    playerTransform = vehicle.transform;
                    break;
                }
            }

            Debug.Log($"AI Opponent initialized - Difficulty: {difficulty}");
        }

        /// <summary>
        /// Setup performance multipliers based on difficulty level.
        /// </summary>
        private void SetupDifficultyParameters()
        {
            switch (difficulty)
            {
                case DifficultyLevel.Rookie:
                    accelerationFactor = 0.65f;
                    brakingFactor = 0.70f;
                    corneringFactor = 0.70f;
                    consistencyFactor = 0.60f;
                    break;

                case DifficultyLevel.Intermediate:
                    accelerationFactor = 0.80f;
                    brakingFactor = 0.85f;
                    corneringFactor = 0.85f;
                    consistencyFactor = 0.80f;
                    break;

                case DifficultyLevel.Professional:
                    accelerationFactor = 0.92f;
                    brakingFactor = 0.95f;
                    corneringFactor = 0.95f;
                    consistencyFactor = 0.95f;
                    break;

                case DifficultyLevel.Expert:
                    accelerationFactor = 1.10f;
                    brakingFactor = 1.05f;
                    corneringFactor = 1.10f;
                    consistencyFactor = 1.05f;
                    break;
            }
        }

        private void Update()
        {
            if (vehicleController == null || waypoints.Length == 0)
                return;

            // Update timing
            currentLapTime = Time.time - sessionStartTime;

            // Update opponent tracking
            if (playerTransform != null)
            {
                distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                isPlayerAhead = (playerTransform.position - transform.position).magnitude > 0;
            }

            // Update racing behavior
            UpdateBehavior();

            // Execute racing commands
            DecideNextAction();

            // Apply inputs to vehicle
            ApplyControlInputs();
        }

        /// <summary>
        /// Update racing behavior based on situation.
        /// </summary>
        private void UpdateBehavior()
        {
            if (vehicleController.GetVehicleDamage() > 0.7f)
            {
                currentBehavior = RacingBehavior.Damaged;
                confidence -= 0.01f;
            }
            else if (isPlayerAhead && distanceToPlayer < 30f)
            {
                currentBehavior = RacingBehavior.Attacking;
                aggression = Mathf.Min(1f, aggression + 0.02f);
            }
            else if (!isPlayerAhead && distanceToPlayer < 20f)
            {
                currentBehavior = RacingBehavior.Defending;
                aggression = Mathf.Max(0f, aggression - 0.01f);
            }
            else
            {
                currentBehavior = RacingBehavior.Following;
            }

            // Update confidence based on performance
            if (currentLapTime > 0 && currentLapTime < bestLapTime)
            {
                bestLapTime = currentLapTime;
                confidence = Mathf.Min(1f, confidence + 0.05f);
            }

            confidence = Mathf.Clamp01(confidence);
        }

        /// <summary>
        /// Decide next racing action.
        /// </summary>
        private void DecideNextAction()
        {
            currentState = new RacingState();

            switch (currentBehavior)
            {
                case RacingBehavior.Following:
                    FollowingLineDecision();
                    break;

                case RacingBehavior.Attacking:
                    AttackingDecision();
                    break;

                case RacingBehavior.Defending:
                    DefendingDecision();
                    break;

                case RacingBehavior.Damaged:
                    DamagedVehicleDecision();
                    break;

                case RacingBehavior.Recovering:
                    RecoveringDecision();
                    break;
            }
        }

        /// <summary>
        /// Decide actions while following the racing line.
        /// </summary>
        private void FollowingLineDecision()
        {
            if (waypoints.Length == 0)
                return;

            // Get current and next waypoints
            Transform currentWaypoint = waypoints[currentWaypoint % waypoints.Length];
            Transform nextWaypoint = waypoints[(currentWaypoint + 1) % waypoints.Length];

            // Calculate distance to waypoint
            float distToWaypoint = Vector3.Distance(transform.position, currentWaypoint.position);

            // Smooth steering toward waypoint
            Vector3 dirToWaypoint = (currentWaypoint.position - transform.position).normalized;
            Vector3 vehicleForward = transform.forward;

            float dotProduct = Vector3.Dot(vehicleForward, dirToWaypoint);
            Vector3 cross = Vector3.Cross(vehicleForward, dirToWaypoint);

            currentState.SteerInput = Mathf.Clamp(cross.y * 2f, -1f, 1f);

            // Determine throttle/brake based on speed and approaching corner
            float currentSpeed = vehicleController.GetSpeed();
            float distToNextWaypoint = Vector3.Distance(currentWaypoint.position, nextWaypoint.position);

            brakingDistance = (currentSpeed * currentSpeed) / (2f * 9.81f * 0.8f);

            if (brakingDistance > distToNextWaypoint * 0.8f)
            {
                currentState.BrakeInput = Mathf.Clamp01((brakingDistance - distToNextWaypoint) / 10f);
                currentState.BrakeInput *= brakingFactor;
            }
            else if (currentSpeed < targetSpeed * corneringFactor)
            {
                currentState.ThrottleInput = Mathf.Min(1f, accelerationFactor);
            }

            // Advance waypoint if close enough
            if (distToWaypoint < 5f)
            {
                currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
                cornersCompleted++;
            }
        }

        /// <summary>
        /// Decide actions while attempting to overtake.
        /// </summary>
        private void AttackingDecision()
        {
            FollowingLineDecision();

            // More aggressive acceleration
            currentState.ThrottleInput = Mathf.Min(1f, accelerationFactor * 1.2f);

            // Earlier braking point for overtake
            if (brakingDistance > 20f)
            {
                currentState.BrakeInput = Mathf.Max(currentState.BrakeInput, 0.3f);
            }

            // Adjust steering for overtake line
            if (distanceToPlayer < 25f && aggression > 0.6f)
            {
                Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
                float playerOffset = Mathf.Sign(dirToPlayer.x) * 0.5f;
                currentState.SteerInput += playerOffset;
                currentState.SteerInput = Mathf.Clamp(currentState.SteerInput, -1f, 1f);
            }
        }

        /// <summary>
        /// Decide actions while defending position.
        /// </summary>
        private void DefendingDecision()
        {
            FollowingLineDecision();

            // Conservative driving to maintain position
            currentState.ThrottleInput *= 0.9f;

            // Block racing line to opponent
            if (playerTransform != null && distanceToPlayer < 30f)
            {
                Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
                float blockSteering = -Mathf.Sign(dirToPlayer.x) * 0.3f;
                currentState.SteerInput += blockSteering;
                currentState.SteerInput = Mathf.Clamp(currentState.SteerInput, -1f, 1f);
            }
        }

        /// <summary>
        /// Decide actions with damaged vehicle.
        /// </summary>
        private void DamagedVehicleDecision()
        {
            FollowingLineDecision();

            // Reduce all inputs due to damage
            float damageMultiplier = 1f - vehicleController.GetVehicleDamage();
            currentState.ThrottleInput *= damageMultiplier * 0.7f;
            currentState.BrakeInput *= damageMultiplier * 0.8f;
            currentState.SteerInput *= damageMultiplier * 0.9f;
        }

        /// <summary>
        /// Decide actions while recovering from mistake.
        /// </summary>
        private void RecoveringDecision()
        {
            // Smooth, cautious driving
            currentState.ThrottleInput = 0.5f * accelerationFactor;
            currentState.BrakeInput = 0.0f;
            currentState.SteerInput = 0.0f; // Don't adjust steering while recovering

            // Check if recovered
            if (vehicleController.GetAngularVelocity().magnitude < 1f)
            {
                currentBehavior = RacingBehavior.Following;
            }
        }

        /// <summary>
        /// Apply control inputs to the vehicle.
        /// </summary>
        private void ApplyControlInputs()
        {
            vehicleController.SetThrottleInput(currentState.ThrottleInput);
            vehicleController.SetBrakeInput(currentState.BrakeInput);
            vehicleController.SetSteerInput(currentState.SteerInput);

            // Automatic gear management
            float currentRPM = vehicleController.GetCurrentRPM();
            float maxRPM = vehicleController.GetMaxRPM();

            if (currentRPM > maxRPM * 0.9f && currentState.ThrottleInput > 0.5f)
            {
                vehicleController.ShiftUp();
            }
            else if (currentRPM < maxRPM * 0.4f && vehicleController.GetCurrentGear() > 1)
            {
                vehicleController.ShiftDown();
            }
        }

        /// <summary>
        /// Get opponent statistics.
        /// </summary>
        public float GetBestLapTime() => bestLapTime;
        public float GetCurrentLapTime() => currentLapTime;
        public float GetDistanceToPlayer() => distanceToPlayer;
        public bool IsPlayerAhead() => isPlayerAhead;
        public RacingBehavior GetCurrentBehavior() => currentBehavior;
        public DifficultyLevel GetDifficulty() => difficulty;
        public float GetConfidence() => confidence;
        public float GetCornersCompleted() => cornersCompleted;

        /// <summary>
        /// Set AI difficulty.
        /// </summary>
        public void SetDifficulty(DifficultyLevel newDifficulty)
        {
            difficulty = newDifficulty;
            SetupDifficultyParameters();
        }

        /// <summary>
        /// Set target waypoints for racing.
        /// </summary>
        public void SetWaypoints(Transform[] newWaypoints)
        {
            waypoints = newWaypoints;
            currentWaypoint = 0;
        }

        /// <summary>
        /// Reset AI for new session.
        /// </summary>
        public void ResetSession()
        {
            sessionStartTime = Time.time;
            currentLapTime = 0f;
            bestLapTime = float.MaxValue;
            cornersCompleted = 0f;
            confidence = 0.5f;
            currentBehavior = RacingBehavior.Following;
        }

        /// <summary>
        /// Adapt AI difficulty based on player performance.
        /// </summary>
        public void AdaptDifficulty(float playerLapTime, float aiLapTime)
        {
            float performanceRatio = playerLapTime / aiLapTime;

            if (performanceRatio > 1.15f && difficulty > DifficultyLevel.Rookie)
            {
                SetDifficulty(difficulty - 1);
                Debug.Log($"AI difficulty reduced to {difficulty}");
            }
            else if (performanceRatio < 0.85f && difficulty < DifficultyLevel.Expert)
            {
                SetDifficulty(difficulty + 1);
                Debug.Log($"AI difficulty increased to {difficulty}");
            }
        }
    }
}
