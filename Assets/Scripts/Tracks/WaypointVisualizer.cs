using UnityEngine;
using SendIt.Tracks;

namespace SendIt.Tracks
{
    /// <summary>
    /// Visualizes track waypoints and racing lines in the editor and at runtime.
    /// Helps with debugging and understanding track layout.
    /// </summary>
    public class WaypointVisualizer : MonoBehaviour
    {
        [SerializeField] private bool showWaypoints = true;
        [SerializeField] private bool showRacingLine = true;
        [SerializeField] private bool showDirections = true;
        [SerializeField] private bool showDifficulty = true;

        [SerializeField] private float waypointSize = 0.5f;
        [SerializeField] private float directionLength = 2f;
        [SerializeField] private float lineWidth = 0.1f;

        private TrackManager trackManager;
        private VehicleController playerVehicle;
        private TrackManager.Waypoint nearestWaypoint;
        private TrackManager.Waypoint nextWaypoint;

        private void Start()
        {
            trackManager = TrackManager.Instance;
            playerVehicle = FindObjectOfType<VehicleController>();
        }

        private void Update()
        {
            if (!trackManager.IsTrackLoaded() || playerVehicle == null)
                return;

            // Update nearest waypoint for player guidance
            nearestWaypoint = trackManager.FindNearestWaypoint(playerVehicle.transform.position);
            if (nearestWaypoint != null)
            {
                int nextIndex = (nearestWaypoint.WaypointIndex + 1) % trackManager.GetTrackWaypoints().Count;
                nextWaypoint = trackManager.GetTrackWaypoints()[nextIndex];
            }
        }

        private void OnDrawGizmos()
        {
            if (!showWaypoints && !showRacingLine && !showDirections)
                return;

            TrackManager tm = FindObjectOfType<TrackManager>();
            if (tm == null || !tm.IsTrackLoaded())
                return;

            var waypoints = tm.GetTrackWaypoints();
            if (waypoints == null || waypoints.Count == 0)
                return;

            // Draw racing line
            if (showRacingLine)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < waypoints.Count; i++)
                {
                    int nextIndex = (i + 1) % waypoints.Count;
                    Gizmos.DrawLine(waypoints[i].Position, waypoints[nextIndex].Position);
                }
            }

            // Draw waypoints
            for (int i = 0; i < waypoints.Count; i++)
            {
                var waypoint = waypoints[i];

                if (showWaypoints)
                {
                    // Color based on difficulty
                    if (showDifficulty)
                    {
                        Gizmos.color = Color.Lerp(Color.green, Color.red, waypoint.Difficulty);
                    }
                    else
                    {
                        Gizmos.color = Color.blue;
                    }

                    // Draw waypoint sphere
                    Gizmos.DrawWireSphere(waypoint.Position, waypointSize);
                }

                // Draw direction arrows
                if (showDirections)
                {
                    Gizmos.color = Color.cyan;
                    Vector3 directionEnd = waypoint.Position + waypoint.NextDirection.normalized * directionLength;
                    Gizmos.DrawLine(waypoint.Position, directionEnd);
                }
            }
        }

        /// <summary>
        /// Get racing line guidance to next waypoint.
        /// </summary>
        public Vector3 GetRacingLineDirection()
        {
            if (nextWaypoint == null)
                return Vector3.forward;

            return (nextWaypoint.Position - (nearestWaypoint?.Position ?? Vector3.zero)).normalized;
        }

        /// <summary>
        /// Get recommended speed through current waypoint.
        /// </summary>
        public float GetRecommendedSpeed()
        {
            if (nearestWaypoint == null)
                return 100f;

            return nearestWaypoint.Speed;
        }

        /// <summary>
        /// Get distance to next waypoint.
        /// </summary>
        public float GetDistanceToNextWaypoint()
        {
            if (nearestWaypoint == null || nextWaypoint == null)
                return 0f;

            return Vector3.Distance(nearestWaypoint.Position, nextWaypoint.Position);
        }

        /// <summary>
        /// Get waypoint difficulty rating.
        /// </summary>
        public float GetCurrentWaypointDifficulty()
        {
            if (nearestWaypoint == null)
                return 0f;

            return nearestWaypoint.Difficulty;
        }

        /// <summary>
        /// Toggle waypoint visualization.
        /// </summary>
        public void ToggleWaypoints()
        {
            showWaypoints = !showWaypoints;
        }

        /// <summary>
        /// Toggle racing line visualization.
        /// </summary>
        public void ToggleRacingLine()
        {
            showRacingLine = !showRacingLine;
        }

        /// <summary>
        /// Get waypoint visualization info.
        /// </summary>
        public string GetVisualizationInfo()
        {
            string info = "Waypoint Visualization:\n";
            info += $"Waypoints: {(showWaypoints ? "ON" : "OFF")}\n";
            info += $"Racing Line: {(showRacingLine ? "ON" : "OFF")}\n";
            info += $"Directions: {(showDirections ? "ON" : "OFF")}\n";
            if (nearestWaypoint != null)
            {
                info += $"Next Speed: {nearestWaypoint.Speed:F0} km/h\n";
                info += $"Difficulty: {nearestWaypoint.Difficulty * 100:F0}%\n";
            }
            return info;
        }
    }
}
