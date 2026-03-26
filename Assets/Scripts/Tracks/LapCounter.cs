using UnityEngine;
using System.Collections.Generic;

namespace SendIt.Tracks
{
    /// <summary>
    /// Tracks lap progress, best lap times, and race completion.
    /// Manages crossing waypoints and detecting lap completions.
    /// </summary>
    public class LapCounter : MonoBehaviour
    {
        private VehicleController playerVehicle;
        private TrackManager trackManager;

        // Lap tracking
        private int currentLap = 0;
        private int totalLaps = 3;
        private bool currentLapValid = true;

        // Waypoint tracking
        private int lastCrossedWaypointIndex = -1;
        private int waypointsPassedThisLap = 0;

        // Timing
        private float lapStartTime = 0f;
        private float currentLapTime = 0f;
        private float bestLapTime = float.MaxValue;

        // Lap history
        private List<float> lapTimes = new List<float>();
        private float totalRaceTime = 0f;

        private bool raceInProgress = false;
        private bool finishLineDetected = false;

        public static LapCounter Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize lap counter.
        /// </summary>
        public void Initialize()
        {
            playerVehicle = FindObjectOfType<VehicleController>();
            trackManager = TrackManager.Instance;
            Debug.Log("LapCounter initialized");
        }

        private void Update()
        {
            if (!raceInProgress || playerVehicle == null || trackManager == null)
                return;

            UpdateLapTiming();
            CheckWaypointCrossing();
            CheckLapCompletion();
        }

        /// <summary>
        /// Start a new race/lap session.
        /// </summary>
        public void StartRace(int numberOfLaps = 3)
        {
            currentLap = 0;
            totalLaps = numberOfLaps;
            lapStartTime = Time.time;
            totalRaceTime = 0f;
            raceInProgress = true;
            currentLapValid = true;
            lapTimes.Clear();
            lastCrossedWaypointIndex = -1;
            waypointsPassedThisLap = 0;
            bestLapTime = float.MaxValue;
            finishLineDetected = false;

            Debug.Log($"Race started: {numberOfLaps} laps");
        }

        /// <summary>
        /// Update lap timing.
        /// </summary>
        private void UpdateLapTiming()
        {
            currentLapTime = Time.time - lapStartTime;
            totalRaceTime += Time.deltaTime;
        }

        /// <summary>
        /// Check if player crossed a waypoint.
        /// </summary>
        private void CheckWaypointCrossing()
        {
            var waypoints = trackManager.GetTrackWaypoints();
            if (waypoints == null || waypoints.Count == 0)
                return;

            TrackManager.Waypoint nearestWaypoint = trackManager.FindNearestWaypoint(
                playerVehicle.transform.position
            );

            if (nearestWaypoint != null && nearestWaypoint.WaypointIndex != lastCrossedWaypointIndex)
            {
                // Check if we crossed in order (or if we're on a new lap)
                if (nearestWaypoint.WaypointIndex == (lastCrossedWaypointIndex + 1) % waypoints.Count)
                {
                    lastCrossedWaypointIndex = nearestWaypoint.WaypointIndex;
                    waypointsPassedThisLap++;

                    // Check for invalid lap (missed waypoints)
                    if (nearestWaypoint.WaypointIndex == 0 && lastCrossedWaypointIndex == 0)
                    {
                        // Completed a lap
                        if (waypointsPassedThisLap >= waypoints.Count)
                        {
                            CompleteLap();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if race/lap is complete.
        /// </summary>
        private void CheckLapCompletion()
        {
            if (currentLap >= totalLaps)
            {
                EndRace();
            }
        }

        /// <summary>
        /// Record lap completion.
        /// </summary>
        private void CompleteLap()
        {
            currentLap++;

            if (currentLapTime < bestLapTime)
            {
                bestLapTime = currentLapTime;
            }

            lapTimes.Add(currentLapTime);

            Debug.Log($"Lap {currentLap} complete: {currentLapTime:F2}s");

            if (currentLap < totalLaps)
            {
                // Start next lap
                lapStartTime = Time.time;
                currentLapTime = 0f;
                waypointsPassedThisLap = 0;
            }
        }

        /// <summary>
        /// End the race.
        /// </summary>
        private void EndRace()
        {
            raceInProgress = false;
            Debug.Log($"Race complete in {totalRaceTime:F2}s");
        }

        /// <summary>
        /// Get current lap number.
        /// </summary>
        public int GetCurrentLap() => currentLap;

        /// <summary>
        /// Get total number of laps for race.
        /// </summary>
        public int GetTotalLaps() => totalLaps;

        /// <summary>
        /// Get current lap time.
        /// </summary>
        public float GetCurrentLapTime() => currentLapTime;

        /// <summary>
        /// Get best lap time so far.
        /// </summary>
        public float GetBestLapTime() => bestLapTime;

        /// <summary>
        /// Get average lap time.
        /// </summary>
        public float GetAverageLapTime()
        {
            if (lapTimes.Count == 0)
                return 0f;

            float total = 0f;
            foreach (float time in lapTimes)
                total += time;

            return total / lapTimes.Count;
        }

        /// <summary>
        /// Get total race time.
        /// </summary>
        public float GetTotalRaceTime() => totalRaceTime;

        /// <summary>
        /// Check if race is currently active.
        /// </summary>
        public bool IsRaceActive() => raceInProgress;

        /// <summary>
        /// Get lap progress percentage.
        /// </summary>
        public float GetLapProgress()
        {
            int totalWaypoints = trackManager.GetTrackWaypoints().Count;
            if (totalWaypoints == 0)
                return 0f;

            return waypointsPassedThisLap / (float)totalWaypoints;
        }

        /// <summary>
        /// Get lap counter info for HUD.
        /// </summary>
        public string GetLapInfo()
        {
            if (!raceInProgress)
                return "Not racing";

            string info = $"Lap: {currentLap}/{totalLaps}\n";
            info += $"Time: {FormatTime(currentLapTime)}\n";
            info += $"Best: {FormatTime(bestLapTime)}\n";
            info += $"Progress: {GetLapProgress() * 100:F0}%\n";
            return info;
        }

        /// <summary>
        /// Format time as MM:SS.MS
        /// </summary>
        private string FormatTime(float time)
        {
            if (time == float.MaxValue)
                return "--:--";

            int minutes = (int)(time / 60f);
            int seconds = (int)(time % 60f);
            int milliseconds = (int)((time % 1f) * 100f);

            return $"{minutes:D2}:{seconds:D2}.{milliseconds:D2}";
        }

        /// <summary>
        /// Get detailed race statistics.
        /// </summary>
        public string GetRaceStats()
        {
            string stats = "=== RACE STATISTICS ===\n";
            stats += $"Total Time: {FormatTime(totalRaceTime)}\n";
            stats += $"Best Lap: {FormatTime(bestLapTime)}\n";
            stats += $"Average Lap: {FormatTime(GetAverageLapTime())}\n";
            stats += $"Laps Completed: {lapTimes.Count}\n";
            return stats;
        }
    }
}
