using UnityEngine;
using System.Collections.Generic;
using SendIt.Environment;

namespace SendIt.Tracks
{
    /// <summary>
    /// Manages race tracks, environments, and waypoint systems.
    /// Handles track selection, layout variants, and environmental conditions.
    /// </summary>
    public class TrackManager : MonoBehaviour
    {
        [System.Serializable]
        public class Track
        {
            public string Name;
            public string Description;
            public EnvironmentType EnvironmentType;
            public float TrackLength = 5000f; // meters
            public int RecommendedLaps = 3;
            public float DifficultyRating = 0.5f; // 0-1
            public Vector3 StartPosition = Vector3.zero;
            public Quaternion StartRotation = Quaternion.identity;
            public List<Waypoint> Waypoints = new List<Waypoint>();
            public WeatherSystem.WeatherCondition DefaultWeather = WeatherSystem.WeatherCondition.Clear;
            public float DefaultTimeOfDay = 12f;
            public int MaxPlayers = 4;
        }

        [System.Serializable]
        public class TrackLayout
        {
            public string LayoutName;
            public List<Waypoint> LayoutWaypoints = new List<Waypoint>();
            public float LayoutDistance;
            public int LayoutTurns;
        }

        [System.Serializable]
        public class Waypoint
        {
            public Vector3 Position;
            public Vector3 NextDirection;
            public float Speed; // Recommended speed through waypoint
            public string WaypointType = "corner"; // corner, straightaway, chicane, etc
            public float Difficulty; // 0-1, how hard is this turn
            public int WaypointIndex;
        }

        public enum EnvironmentType
        {
            UrbanCity,      // Streets and buildings
            RaceCircuit,    // Dedicated race track
            Desert,         // Open desert with sand
            Mountain,       // Winding mountain roads
            Industrial,     // Warehouse and dock area
            Airport,        // Runway and tarmac
            BeachRoad,      // Coastal highway
            Night           // Neon-lit night racing
        }

        [SerializeField] private Track currentTrack;
        [SerializeField] private int currentLayoutIndex = 0;

        private List<Track> availableTracks = new List<Track>();
        private List<TrackLayout> trackLayouts = new List<TrackLayout>();

        private VehicleController playerVehicle;
        private bool trackLoaded = false;

        public static TrackManager Instance { get; private set; }

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
        /// Initialize track manager and create default tracks.
        /// </summary>
        public void Initialize()
        {
            playerVehicle = FindObjectOfType<VehicleController>();
            CreateDefaultTracks();
            Debug.Log($"TrackManager initialized with {availableTracks.Count} tracks");
        }

        /// <summary>
        /// Create default race tracks.
        /// </summary>
        private void CreateDefaultTracks()
        {
            // Urban City Circuit
            availableTracks.Add(new Track
            {
                Name = "Downtown Circuit",
                Description = "Fast-paced street racing through city blocks",
                EnvironmentType = EnvironmentType.UrbanCity,
                TrackLength = 3500f,
                RecommendedLaps = 5,
                DifficultyRating = 0.6f,
                StartPosition = Vector3.zero,
                StartRotation = Quaternion.identity,
                DefaultWeather = WeatherSystem.WeatherCondition.Clear,
                DefaultTimeOfDay = 14f,
                MaxPlayers = 4,
                Waypoints = CreateUrbanWaypoints()
            });

            // Race Circuit
            availableTracks.Add(new Track
            {
                Name = "Grand Prix Circuit",
                Description = "Professional racing circuit with high-speed sections",
                EnvironmentType = EnvironmentType.RaceCircuit,
                TrackLength = 5200f,
                RecommendedLaps = 3,
                DifficultyRating = 0.7f,
                StartPosition = Vector3.zero + Vector3.right * 10f,
                StartRotation = Quaternion.identity,
                DefaultWeather = WeatherSystem.WeatherCondition.Clear,
                DefaultTimeOfDay = 12f,
                MaxPlayers = 4,
                Waypoints = CreateCircuitWaypoints()
            });

            // Desert Track
            availableTracks.Add(new Track
            {
                Name = "Desert Run",
                Description = "Long straight with tight desert corners",
                EnvironmentType = EnvironmentType.Desert,
                TrackLength = 6000f,
                RecommendedLaps = 2,
                DifficultyRating = 0.5f,
                StartPosition = Vector3.zero,
                StartRotation = Quaternion.identity,
                DefaultWeather = WeatherSystem.WeatherCondition.Clear,
                DefaultTimeOfDay = 16f,
                MaxPlayers = 4,
                Waypoints = CreateDesertWaypoints()
            });

            // Mountain Pass
            availableTracks.Add(new Track
            {
                Name = "Mountain Ascent",
                Description = "Technical mountain road with elevation changes",
                EnvironmentType = EnvironmentType.Mountain,
                TrackLength = 4200f,
                RecommendedLaps = 3,
                DifficultyRating = 0.8f,
                StartPosition = Vector3.zero,
                StartRotation = Quaternion.identity,
                DefaultWeather = WeatherSystem.WeatherCondition.Light,
                DefaultTimeOfDay = 10f,
                MaxPlayers = 4,
                Waypoints = CreateMountainWaypoints()
            });

            // Night City
            availableTracks.Add(new Track
            {
                Name = "Neon Nights",
                Description = "Lit-up city streets at night with neon signs",
                EnvironmentType = EnvironmentType.Night,
                TrackLength = 4000f,
                RecommendedLaps = 4,
                DifficultyRating = 0.65f,
                StartPosition = Vector3.zero,
                StartRotation = Quaternion.identity,
                DefaultWeather = WeatherSystem.WeatherCondition.Clear,
                DefaultTimeOfDay = 22f,
                MaxPlayers = 4,
                Waypoints = CreateNightWaypoints()
            });
        }

        /// <summary>
        /// Load a track by name.
        /// </summary>
        public bool LoadTrack(string trackName)
        {
            Track track = availableTracks.Find(t => t.Name == trackName);
            if (track == null)
            {
                Debug.LogError($"Track '{trackName}' not found");
                return false;
            }

            currentTrack = track;
            currentLayoutIndex = 0;
            trackLoaded = true;

            // Apply track settings
            ApplyTrackEnvironment();

            if (playerVehicle != null)
            {
                playerVehicle.transform.position = track.StartPosition;
                playerVehicle.transform.rotation = track.StartRotation;
            }

            Debug.Log($"Track loaded: {trackName}");
            return true;
        }

        /// <summary>
        /// Apply track environment settings (weather, time, etc).
        /// </summary>
        private void ApplyTrackEnvironment()
        {
            if (currentTrack == null)
                return;

            // Set weather
            WeatherSystem weather = WeatherSystem.Instance;
            if (weather != null)
            {
                weather.SetWeather(currentTrack.DefaultWeather);
            }

            // Set time of day
            TimeOfDaySystem timeSystem = TimeOfDaySystem.Instance;
            if (timeSystem != null)
            {
                timeSystem.SetTime(currentTrack.DefaultTimeOfDay);
            }

            Debug.Log($"Applied environment for {currentTrack.Name}");
        }

        /// <summary>
        /// Create waypoints for urban circuit.
        /// </summary>
        private List<Waypoint> CreateUrbanWaypoints()
        {
            List<Waypoint> waypoints = new List<Waypoint>();
            // Simplified waypoint creation - would have many more in real implementation
            waypoints.Add(new Waypoint
            {
                Position = Vector3.zero,
                NextDirection = Vector3.right,
                Speed = 80f,
                WaypointType = "straightaway",
                Difficulty = 0.2f,
                WaypointIndex = 0
            });
            return waypoints;
        }

        /// <summary>
        /// Create waypoints for race circuit.
        /// </summary>
        private List<Waypoint> CreateCircuitWaypoints()
        {
            List<Waypoint> waypoints = new List<Waypoint>();
            waypoints.Add(new Waypoint
            {
                Position = Vector3.zero,
                NextDirection = Vector3.forward,
                Speed = 200f,
                WaypointType = "straightaway",
                Difficulty = 0.3f,
                WaypointIndex = 0
            });
            return waypoints;
        }

        /// <summary>
        /// Create waypoints for desert track.
        /// </summary>
        private List<Waypoint> CreateDesertWaypoints()
        {
            List<Waypoint> waypoints = new List<Waypoint>();
            waypoints.Add(new Waypoint
            {
                Position = Vector3.zero,
                NextDirection = Vector3.right,
                Speed = 150f,
                WaypointType = "straightaway",
                Difficulty = 0.1f,
                WaypointIndex = 0
            });
            return waypoints;
        }

        /// <summary>
        /// Create waypoints for mountain track.
        /// </summary>
        private List<Waypoint> CreateMountainWaypoints()
        {
            List<Waypoint> waypoints = new List<Waypoint>();
            waypoints.Add(new Waypoint
            {
                Position = Vector3.zero,
                NextDirection = Vector3.forward + Vector3.up,
                Speed = 60f,
                WaypointType = "corner",
                Difficulty = 0.8f,
                WaypointIndex = 0
            });
            return waypoints;
        }

        /// <summary>
        /// Create waypoints for night track.
        /// </summary>
        private List<Waypoint> CreateNightWaypoints()
        {
            List<Waypoint> waypoints = new List<Waypoint>();
            waypoints.Add(new Waypoint
            {
                Position = Vector3.zero,
                NextDirection = Vector3.forward,
                Speed = 90f,
                WaypointType = "straightaway",
                Difficulty = 0.4f,
                WaypointIndex = 0
            });
            return waypoints;
        }

        /// <summary>
        /// Get all available tracks.
        /// </summary>
        public List<Track> GetAvailableTracks() => availableTracks;

        /// <summary>
        /// Get current track.
        /// </summary>
        public Track GetCurrentTrack() => currentTrack;

        /// <summary>
        /// Get track waypoints.
        /// </summary>
        public List<Waypoint> GetTrackWaypoints() => currentTrack?.Waypoints ?? new List<Waypoint>();

        /// <summary>
        /// Get track info.
        /// </summary>
        public string GetTrackInfo()
        {
            if (currentTrack == null)
                return "No track loaded";

            string info = $"Track: {currentTrack.Name}\n";
            info += $"Environment: {currentTrack.EnvironmentType}\n";
            info += $"Length: {currentTrack.TrackLength:F0}m\n";
            info += $"Recommended Laps: {currentTrack.RecommendedLaps}\n";
            info += $"Difficulty: {currentTrack.DifficultyRating * 100:F0}%\n";
            info += $"Waypoints: {currentTrack.Waypoints.Count}\n";
            return info;
        }

        /// <summary>
        /// Get next waypoint for racing line guidance.
        /// </summary>
        public Waypoint GetNextWaypoint(int currentWaypointIndex)
        {
            if (currentTrack == null || currentTrack.Waypoints.Count == 0)
                return null;

            int nextIndex = (currentWaypointIndex + 1) % currentTrack.Waypoints.Count;
            return currentTrack.Waypoints[nextIndex];
        }

        /// <summary>
        /// Find nearest waypoint to position.
        /// </summary>
        public Waypoint FindNearestWaypoint(Vector3 position)
        {
            if (currentTrack == null || currentTrack.Waypoints.Count == 0)
                return null;

            Waypoint nearest = currentTrack.Waypoints[0];
            float nearestDistance = Vector3.Distance(position, nearest.Position);

            foreach (Waypoint waypoint in currentTrack.Waypoints)
            {
                float distance = Vector3.Distance(position, waypoint.Position);
                if (distance < nearestDistance)
                {
                    nearest = waypoint;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Is track loaded and ready.
        /// </summary>
        public bool IsTrackLoaded() => trackLoaded && currentTrack != null;
    }
}
