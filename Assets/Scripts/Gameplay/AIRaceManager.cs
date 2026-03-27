using UnityEngine;
using System.Collections.Generic;
using SendIt.AI;

namespace SendIt.Gameplay
{
    /// <summary>
    /// Manager for AI opponent races and competitive events.
    /// Handles multiple AI opponents, race conditions, and result tracking.
    /// </summary>
    public class AIRaceManager : MonoBehaviour
    {
        /// <summary>
        /// Race result data structure.
        /// </summary>
        public struct RaceResult
        {
            public string DriverName;
            public int Position;
            public float BestLapTime;
            public float FinalLapTime;
            public int LapsCompleted;
            public float TotalRaceTime;
            public bool FinishedRace;
            public int Penalties;
        }

        [SerializeField] private List<AIOpponent> aiOpponents = new List<AIOpponent>();
        [SerializeField] private VehicleController playerVehicle;
        [SerializeField] private Transform[] trackWaypoints;
        [SerializeField] private int raceLaps = 5;
        [SerializeField] private float raceStartDelay = 3f;

        private List<RaceResult> raceResults = new List<RaceResult>();
        private bool raceActive;
        private float raceStartTime;
        private float playerBestLapTime = float.MaxValue;
        private float playerCurrentLapTime;
        private int playerLapsCompleted;

        private const float lapCrossingDistance = 50f;

        public void Initialize()
        {
            if (playerVehicle == null)
                playerVehicle = FindObjectOfType<VehicleController>();

            // Find all AI opponents
            aiOpponents.Clear();
            var opponents = FindObjectsOfType<AIOpponent>();
            aiOpponents.AddRange(opponents);

            // Set waypoints for all opponents
            foreach (var opponent in aiOpponents)
            {
                opponent.SetWaypoints(trackWaypoints);
            }

            Debug.Log($"Race Manager initialized with {aiOpponents.Count} opponents");
        }

        private void Update()
        {
            if (!raceActive)
                return;

            UpdateRaceState();
            UpdatePlayerMetrics();
            UpdateOpponentMetrics();
        }

        /// <summary>
        /// Start a new race with AI opponents.
        /// </summary>
        public void StartRace(int numOpponents, AIOpponent.DifficultyLevel difficulty)
        {
            if (raceActive)
            {
                Debug.LogWarning("Race already in progress.");
                return;
            }

            raceResults.Clear();
            raceActive = true;
            raceStartTime = Time.time + raceStartDelay;
            playerBestLapTime = float.MaxValue;
            playerCurrentLapTime = 0f;
            playerLapsCompleted = 0;

            // Configure AI opponents
            for (int i = 0; i < Mathf.Min(numOpponents, aiOpponents.Count); i++)
            {
                aiOpponents[i].SetDifficulty(difficulty);
                aiOpponents[i].ResetSession();
                aiOpponents[i].gameObject.SetActive(true);
            }

            // Disable unused opponents
            for (int i = numOpponents; i < aiOpponents.Count; i++)
            {
                aiOpponents[i].gameObject.SetActive(false);
            }

            Debug.Log($"Race started with {numOpponents} opponents at {difficulty} difficulty");
        }

        /// <summary>
        /// Stop the current race and generate results.
        /// </summary>
        public void EndRace()
        {
            if (!raceActive)
                return;

            raceActive = false;
            GenerateRaceResults();
            DisplayRaceResults();

            Debug.Log("Race ended. Results generated.");
        }

        /// <summary>
        /// Update race state and check for completion.
        /// </summary>
        private void UpdateRaceState()
        {
            if (playerLapsCompleted >= raceLaps)
            {
                EndRace();
            }
        }

        /// <summary>
        /// Update player metrics.
        /// </summary>
        private void UpdatePlayerMetrics()
        {
            if (playerVehicle == null)
                return;

            playerCurrentLapTime += Time.deltaTime;

            // Check if player completed a lap (simplified detection)
            if (playerVehicle.GetCurrentLapTime() > 0)
            {
                float lapTime = playerVehicle.GetCurrentLapTime();
                if (lapTime < playerBestLapTime)
                {
                    playerBestLapTime = lapTime;
                }

                // Lap completion detection (when lap counter increases)
                int currentLap = playerVehicle.GetCurrentLapNumber();
                if (currentLap > playerLapsCompleted)
                {
                    playerLapsCompleted = currentLap;
                    playerCurrentLapTime = 0f;
                }
            }
        }

        /// <summary>
        /// Update opponent metrics.
        /// </summary>
        private void UpdateOpponentMetrics()
        {
            foreach (var opponent in aiOpponents)
            {
                if (!opponent.gameObject.activeInHierarchy)
                    continue;

                // Adapt difficulty based on performance
                opponent.AdaptDifficulty(playerBestLapTime, opponent.GetBestLapTime());
            }
        }

        /// <summary>
        /// Generate race results from current metrics.
        /// </summary>
        private void GenerateRaceResults()
        {
            raceResults.Clear();

            // Add player result
            raceResults.Add(new RaceResult
            {
                DriverName = "Player",
                Position = 1, // Will be sorted
                BestLapTime = playerBestLapTime,
                FinalLapTime = playerCurrentLapTime,
                LapsCompleted = playerLapsCompleted,
                TotalRaceTime = Time.time - raceStartTime,
                FinishedRace = playerLapsCompleted >= raceLaps,
                Penalties = 0
            });

            // Add opponent results
            foreach (var opponent in aiOpponents)
            {
                if (!opponent.gameObject.activeInHierarchy)
                    continue;

                raceResults.Add(new RaceResult
                {
                    DriverName = $"AI ({opponent.GetDifficulty()})",
                    Position = 1, // Will be sorted
                    BestLapTime = opponent.GetBestLapTime(),
                    FinalLapTime = opponent.GetCurrentLapTime(),
                    LapsCompleted = (int)opponent.GetCornersCompleted() / 4, // Rough estimate
                    TotalRaceTime = Time.time - raceStartTime,
                    FinishedRace = opponent.GetCornersCompleted() >= raceLaps * 4,
                    Penalties = 0
                });
            }

            // Sort by laps completed (descending), then by best lap time
            raceResults.Sort((a, b) =>
            {
                int lapComparison = b.LapsCompleted.CompareTo(a.LapsCompleted);
                if (lapComparison != 0)
                    return lapComparison;

                return a.BestLapTime.CompareTo(b.BestLapTime);
            });

            // Assign positions
            for (int i = 0; i < raceResults.Count; i++)
            {
                var result = raceResults[i];
                result.Position = i + 1;
                raceResults[i] = result;
            }
        }

        /// <summary>
        /// Display race results (console for now, would show UI in production).
        /// </summary>
        private void DisplayRaceResults()
        {
            string resultsText = "\n=== RACE RESULTS ===\n";

            for (int i = 0; i < raceResults.Count; i++)
            {
                var result = raceResults[i];
                resultsText += $"{result.Position}. {result.DriverName}\n";
                resultsText += $"   Laps: {result.LapsCompleted} | Best: {result.BestLapTime:F2}s | Final: {result.FinalLapTime:F2}s\n";
            }

            Debug.Log(resultsText);
        }

        /// <summary>
        /// Get race results.
        /// </summary>
        public List<RaceResult> GetRaceResults() => new List<RaceResult>(raceResults);

        /// <summary>
        /// Get current race state.
        /// </summary>
        public bool IsRaceActive => raceActive;
        public int RemainingLaps => Mathf.Max(0, raceLaps - playerLapsCompleted);
        public float TimeElapsed => Time.time - raceStartTime;

        /// <summary>
        /// Set race parameters.
        /// </summary>
        public void SetRaceParameters(int laps, float startDelay)
        {
            raceLaps = laps;
            raceStartDelay = startDelay;
        }

        /// <summary>
        /// Get player position in race.
        /// </summary>
        public int GetPlayerPosition()
        {
            int position = 1;

            foreach (var opponent in aiOpponents)
            {
                if (!opponent.gameObject.activeInHierarchy)
                    continue;

                int opponentLaps = (int)opponent.GetCornersCompleted() / 4;
                if (opponentLaps > playerLapsCompleted)
                {
                    position++;
                }
                else if (opponentLaps == playerLapsCompleted)
                {
                    if (opponent.GetBestLapTime() < playerBestLapTime)
                    {
                        position++;
                    }
                }
            }

            return position;
        }

        /// <summary>
        /// Get gap to leader.
        /// </summary>
        public float GetGapToLeader()
        {
            float leaderBestLap = float.MaxValue;

            foreach (var opponent in aiOpponents)
            {
                if (!opponent.gameObject.activeInHierarchy)
                    continue;

                if (opponent.GetBestLapTime() < leaderBestLap)
                {
                    leaderBestLap = opponent.GetBestLapTime();
                }
            }

            if (leaderBestLap < float.MaxValue && playerBestLapTime < float.MaxValue)
            {
                return playerBestLapTime - leaderBestLap;
            }

            return 0f;
        }

        /// <summary>
        /// Get gap to player behind.
        /// </summary>
        public float GetGapToBehind()
        {
            float behindBestLap = float.MinValue;
            int playerPos = GetPlayerPosition();

            foreach (var opponent in aiOpponents)
            {
                if (!opponent.gameObject.activeInHierarchy)
                    continue;

                if (opponent.GetBestLapTime() > behindBestLap)
                {
                    behindBestLap = opponent.GetBestLapTime();
                }
            }

            if (behindBestLap > float.MinValue && playerBestLapTime < float.MaxValue)
            {
                return behindBestLap - playerBestLapTime;
            }

            return 0f;
        }

        /// <summary>
        /// Get opponent count.
        /// </summary>
        public int GetActiveOpponentCount()
        {
            int count = 0;
            foreach (var opponent in aiOpponents)
            {
                if (opponent.gameObject.activeInHierarchy)
                    count++;
            }
            return count;
        }
    }
}
