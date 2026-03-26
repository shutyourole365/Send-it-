using UnityEngine;
using System.Collections.Generic;
using SendIt.Gameplay;

namespace SendIt.AI
{
    /// <summary>
    /// Manages racing events, competition, and AI opponents.
    /// Tracks lap times, positions, and race results.
    /// </summary>
    public class RaceManager : MonoBehaviour
    {
        public enum RaceType
        {
            Sprint,    // Point-to-point race
            Circuit,   // Lap-based race
            Endurance, // Long race with fuel/tire management
            Drift,     // Score-based drift competition
            Drag       // Straight-line acceleration
        }

        [System.Serializable]
        public class RaceEvent
        {
            public string Name;
            public RaceType Type;
            public int NumberOfOpponents = 3;
            public float OpponentAggressiveness = 0.5f;
            public int LapsOrDistance = 3;
            public float PrizeReward = 1000f;
        }

        [System.Serializable]
        public class RaceResult
        {
            public string RaceName;
            public int PlayerPosition = 0;
            public float PlayerTime = 0f;
            public List<string> FinishOrder = new List<string>();
            public List<float> FinishTimes = new List<float>();
            public bool PlayerWon = false;
            public float RewardEarned = 0f;
        }

        private RaceEvent currentRace;
        private RaceResult currentRaceResult;
        private List<AIVehicleController> raceOpponents = new List<AIVehicleController>();
        private VehicleController playerVehicle;

        private bool raceInProgress = false;
        private float raceTimer = 0f;
        private int playerLapsCompleted = 0;
        private int playerCurrentPosition = 1;

        // Race tracking
        private Dictionary<AIVehicleController, int> opponentLaps = new Dictionary<AIVehicleController, int>();
        private Dictionary<AIVehicleController, float> opponentBestLapTimes = new Dictionary<AIVehicleController, float>();

        public static RaceManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize race manager.
        /// </summary>
        public void Initialize()
        {
            playerVehicle = FindObjectOfType<VehicleController>();
            Debug.Log("RaceManager initialized");
        }

        private void Update()
        {
            if (!raceInProgress)
                return;

            raceTimer += Time.deltaTime;
            UpdateRaceStatus();
            CheckRaceCompletion();
        }

        /// <summary>
        /// Start a new race with specified parameters.
        /// </summary>
        public void StartRace(RaceEvent raceEvent)
        {
            currentRace = raceEvent;
            currentRaceResult = new RaceResult { RaceName = raceEvent.Name };

            // Create AI opponents
            SpawnOpponents(raceEvent.NumberOfOpponents);

            raceInProgress = true;
            raceTimer = 0f;
            playerLapsCompleted = 0;

            Debug.Log($"Race started: {raceEvent.Name} ({raceEvent.Type})");
        }

        /// <summary>
        /// Spawn AI opponent vehicles.
        /// </summary>
        private void SpawnOpponents(int count)
        {
            raceOpponents.Clear();
            opponentLaps.Clear();

            for (int i = 0; i < count; i++)
            {
                // Create opponent vehicle (would instantiate from prefab in real implementation)
                GameObject opponentObj = new GameObject($"Opponent_{i + 1}");
                opponentObj.transform.position = playerVehicle.transform.position + Vector3.right * (i + 1) * 5f;

                AIVehicleController aiController = opponentObj.AddComponent<AIVehicleController>();
                aiController.SetBehavior(AIVehicleController.AIBehavior.Racing);
                aiController.SetAggressiveness(currentRace.OpponentAggressiveness);
                aiController.Initialize();

                raceOpponents.Add(aiController);
                opponentLaps[aiController] = 0;
            }

            Debug.Log($"Spawned {count} opponents");
        }

        /// <summary>
        /// Update race status and positions.
        /// </summary>
        private void UpdateRaceStatus()
        {
            // Calculate current positions
            List<(AIVehicleController opponent, float distance)> positions = new List<(AIVehicleController, float)>();

            // Track player position
            foreach (AIVehicleController opponent in raceOpponents)
            {
                float distanceAhead = Vector3.Distance(opponent.transform.position, playerVehicle.transform.position);
                positions.Add((opponent, distanceAhead));
            }

            // Sort by distance (closest = ahead)
            positions.Sort((a, b) => a.distance.CompareTo(b.distance));

            // Update player position (count opponents ahead + 1)
            playerCurrentPosition = 1;
            foreach (var pos in positions)
            {
                if (pos.distance < 0) // Opponent is ahead
                    playerCurrentPosition++;
            }
        }

        /// <summary>
        /// Check if race has been completed.
        /// </summary>
        private void CheckRaceCompletion()
        {
            switch (currentRace.Type)
            {
                case RaceType.Circuit:
                    // Check lap count
                    if (playerLapsCompleted >= currentRace.LapsOrDistance)
                    {
                        EndRace();
                    }
                    break;

                case RaceType.Sprint:
                    // Check distance (would need waypoint system)
                    break;
            }
        }

        /// <summary>
        /// Complete the current race.
        /// </summary>
        public void EndRace()
        {
            if (!raceInProgress)
                return;

            raceInProgress = false;

            // Compile results
            currentRaceResult.PlayerPosition = playerCurrentPosition;
            currentRaceResult.PlayerTime = raceTimer;
            currentRaceResult.PlayerWon = (playerCurrentPosition == 1);

            // Calculate reward
            float baseReward = currentRace.PrizeReward;
            float positionMultiplier = 1f / (playerCurrentPosition * 0.5f); // Better position = higher reward
            currentRaceResult.RewardEarned = baseReward * positionMultiplier;

            // Update player stats
            if (GameplayManager.Instance != null)
            {
                GameplayManager.Instance.UpdateModeTracking("racetime", raceTimer);
            }

            // Clean up opponents
            foreach (AIVehicleController opponent in raceOpponents)
            {
                Destroy(opponent.gameObject);
            }
            raceOpponents.Clear();

            Debug.Log($"Race completed! Position: {playerCurrentPosition}, Time: {raceTimer:F2}s, Reward: {currentRaceResult.RewardEarned:F0}");
        }

        /// <summary>
        /// Record a lap completion for player.
        /// </summary>
        public void PlayerCompletedLap()
        {
            playerLapsCompleted++;
            Debug.Log($"Lap {playerLapsCompleted} completed");
        }

        /// <summary>
        /// Get race status string.
        /// </summary>
        public string GetRaceStatus()
        {
            if (!raceInProgress)
                return "No active race";

            string status = $"Position: {playerCurrentPosition}/{raceOpponents.Count + 1}\n";
            status += $"Time: {(int)raceTimer}:{(int)(raceTimer % 60):D2}\n";

            if (currentRace.Type == RaceType.Circuit)
            {
                status += $"Lap: {playerLapsCompleted}/{currentRace.LapsOrDistance}";
            }

            return status;
        }

        /// <summary>
        /// Get available race events.
        /// </summary>
        public List<RaceEvent> GetAvailableRaces()
        {
            return new List<RaceEvent>
            {
                new RaceEvent { Name = "City Sprint", Type = RaceType.Sprint, NumberOfOpponents = 2, LapsOrDistance = 1 },
                new RaceEvent { Name = "Circuit Qualifying", Type = RaceType.Circuit, NumberOfOpponents = 3, LapsOrDistance = 3 },
                new RaceEvent { Name = "Drag Challenge", Type = RaceType.Drag, NumberOfOpponents = 1, LapsOrDistance = 1 },
                new RaceEvent { Name = "Endurance Run", Type = RaceType.Endurance, NumberOfOpponents = 4, LapsOrDistance = 10 },
                new RaceEvent { Name = "Drift Battle", Type = RaceType.Drift, NumberOfOpponents = 1, LapsOrDistance = 1 },
            };
        }

        /// <summary>
        /// Is a race currently in progress.
        /// </summary>
        public bool IsRaceActive() => raceInProgress;

        /// <summary>
        /// Get last race result.
        /// </summary>
        public RaceResult GetLastRaceResult() => currentRaceResult;

        /// <summary>
        /// Get player current position during race.
        /// </summary>
        public int GetPlayerPosition() => playerCurrentPosition;

        /// <summary>
        /// Get player lap count.
        /// </summary>
        public int GetPlayerLaps() => playerLapsCompleted;
    }
}
