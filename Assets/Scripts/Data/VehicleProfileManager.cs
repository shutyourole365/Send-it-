using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace SendIt.Data
{
    /// <summary>
    /// Manages vehicle profiles, templates, and player save data.
    /// Handles saving, loading, and sharing vehicle configurations.
    /// </summary>
    public class VehicleProfileManager : MonoBehaviour
    {
        [System.Serializable]
        public class VehicleProfile
        {
            public string Name;
            public string Description;
            public VehicleData Data;
            public System.DateTime LastModified;
            public float PerformanceRating; // 0-100
        }

        [System.Serializable]
        public class VehicleTemplate
        {
            public string Name;
            public string Description;
            public VehicleData Data;
            public string Category; // Racing, Drift, Cruising, etc
        }

        [System.Serializable]
        public class PlayerProfile
        {
            public string PlayerName;
            public int TotalPlaytime; // seconds
            public int SessionsCompleted;
            public List<string> SavedVehicleNames;
            public float BestBurnoutDistance;
            public float BestDriftScore;
            public float BestLapTime;
            public System.DateTime CreatedDate;
        }

        private string savePath;
        private string profilesPath;
        private string templatesPath;
        private string playerProfilePath;

        private List<VehicleProfile> loadedProfiles = new List<VehicleProfile>();
        private List<VehicleTemplate> vehicleTemplates = new List<VehicleTemplate>();
        private PlayerProfile currentPlayerProfile;

        public static VehicleProfileManager Instance { get; private set; }

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
        /// Initialize profile manager and load all data.
        /// </summary>
        public void Initialize()
        {
            // Setup file paths
            savePath = Path.Combine(Application.persistentDataPath, "Vehicles");
            profilesPath = Path.Combine(savePath, "Profiles");
            templatesPath = Path.Combine(savePath, "Templates");
            playerProfilePath = Path.Combine(savePath, "player.json");

            // Create directories if they don't exist
            if (!Directory.Exists(profilesPath))
                Directory.CreateDirectory(profilesPath);
            if (!Directory.Exists(templatesPath))
                Directory.CreateDirectory(templatesPath);

            // Load all data
            LoadPlayerProfile();
            LoadVehicleProfiles();
            CreateDefaultTemplates();

            Debug.Log($"VehicleProfileManager initialized. Save path: {savePath}");
        }

        /// <summary>
        /// Load player profile from disk.
        /// </summary>
        private void LoadPlayerProfile()
        {
            if (File.Exists(playerProfilePath))
            {
                string json = File.ReadAllText(playerProfilePath);
                currentPlayerProfile = JsonUtility.FromJson<PlayerProfile>(json);
            }
            else
            {
                // Create new player profile
                currentPlayerProfile = new PlayerProfile
                {
                    PlayerName = "Player",
                    TotalPlaytime = 0,
                    SessionsCompleted = 0,
                    SavedVehicleNames = new List<string>(),
                    BestBurnoutDistance = 0f,
                    BestDriftScore = 0f,
                    BestLapTime = float.MaxValue,
                    CreatedDate = System.DateTime.Now
                };
                SavePlayerProfile();
            }
        }

        /// <summary>
        /// Save player profile to disk.
        /// </summary>
        private void SavePlayerProfile()
        {
            string json = JsonUtility.ToJson(currentPlayerProfile, true);
            File.WriteAllText(playerProfilePath, json);
            Debug.Log($"Player profile saved: {currentPlayerProfile.PlayerName}");
        }

        /// <summary>
        /// Load all vehicle profiles from disk.
        /// </summary>
        private void LoadVehicleProfiles()
        {
            loadedProfiles.Clear();

            if (!Directory.Exists(profilesPath))
                return;

            string[] profileFiles = Directory.GetFiles(profilesPath, "*.json");
            foreach (string filePath in profileFiles)
            {
                string json = File.ReadAllText(filePath);
                try
                {
                    VehicleProfile profile = JsonUtility.FromJson<VehicleProfile>(json);
                    loadedProfiles.Add(profile);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to load profile {filePath}: {e.Message}");
                }
            }

            Debug.Log($"Loaded {loadedProfiles.Count} vehicle profiles");
        }

        /// <summary>
        /// Save a vehicle profile to disk.
        /// </summary>
        public void SaveVehicleProfile(string profileName, VehicleData vehicleData, string description = "")
        {
            VehicleProfile profile = new VehicleProfile
            {
                Name = profileName,
                Description = description,
                Data = vehicleData,
                LastModified = System.DateTime.Now,
                PerformanceRating = CalculatePerformanceRating(vehicleData)
            };

            string filePath = Path.Combine(profilesPath, $"{profileName}.json");
            string json = JsonUtility.ToJson(profile, true);
            File.WriteAllText(filePath, json);

            // Update loaded profiles list
            VehicleProfile existing = loadedProfiles.Find(p => p.Name == profileName);
            if (existing != null)
                loadedProfiles.Remove(existing);
            loadedProfiles.Add(profile);

            // Update player profile
            if (!currentPlayerProfile.SavedVehicleNames.Contains(profileName))
                currentPlayerProfile.SavedVehicleNames.Add(profileName);
            SavePlayerProfile();

            Debug.Log($"Vehicle profile saved: {profileName}");
        }

        /// <summary>
        /// Load a vehicle profile by name.
        /// </summary>
        public VehicleProfile LoadVehicleProfile(string profileName)
        {
            return loadedProfiles.Find(p => p.Name == profileName);
        }

        /// <summary>
        /// Delete a vehicle profile.
        /// </summary>
        public void DeleteVehicleProfile(string profileName)
        {
            string filePath = Path.Combine(profilesPath, $"{profileName}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                loadedProfiles.RemoveAll(p => p.Name == profileName);
                currentPlayerProfile.SavedVehicleNames.Remove(profileName);
                SavePlayerProfile();
                Debug.Log($"Vehicle profile deleted: {profileName}");
            }
        }

        /// <summary>
        /// Create default vehicle templates.
        /// </summary>
        private void CreateDefaultTemplates()
        {
            vehicleTemplates.Clear();

            // Racing template
            vehicleTemplates.Add(new VehicleTemplate
            {
                Name = "Racing Setup",
                Description = "Optimized for speed and acceleration",
                Category = "Racing",
                Data = CreateRacingTemplate()
            });

            // Drift template
            vehicleTemplates.Add(new VehicleTemplate
            {
                Name = "Drift Setup",
                Description = "Tuned for drifting and handling",
                Category = "Drift",
                Data = CreateDriftTemplate()
            });

            // Cruising template
            vehicleTemplates.Add(new VehicleTemplate
            {
                Name = "Cruising Setup",
                Description = "Comfortable daily driver",
                Category = "Cruising",
                Data = CreateCruisingTemplate()
            });

            Debug.Log($"Created {vehicleTemplates.Count} vehicle templates");
        }

        /// <summary>
        /// Create racing template data.
        /// </summary>
        private VehicleData CreateRacingTemplate()
        {
            // This would create a racing-optimized VehicleData
            // For now, return a default that can be customized
            return new VehicleData();
        }

        /// <summary>
        /// Create drift template data.
        /// </summary>
        private VehicleData CreateDriftTemplate()
        {
            return new VehicleData();
        }

        /// <summary>
        /// Create cruising template data.
        /// </summary>
        private VehicleData CreateCruisingTemplate()
        {
            return new VehicleData();
        }

        /// <summary>
        /// Get all vehicle profiles.
        /// </summary>
        public List<VehicleProfile> GetAllProfiles() => loadedProfiles;

        /// <summary>
        /// Get all vehicle templates.
        /// </summary>
        public List<VehicleTemplate> GetAllTemplates() => vehicleTemplates;

        /// <summary>
        /// Get all profile names.
        /// </summary>
        public List<string> GetProfileNames()
        {
            List<string> names = new List<string>();
            foreach (VehicleProfile profile in loadedProfiles)
                names.Add(profile.Name);
            return names;
        }

        /// <summary>
        /// Calculate performance rating for a vehicle configuration.
        /// </summary>
        private float CalculatePerformanceRating(VehicleData vehicleData)
        {
            if (vehicleData == null || vehicleData.Physics == null)
                return 0f;

            // Simple rating based on engine power and handling
            float engineRating = vehicleData.Physics.HorsePower / 10f;
            float suspensionRating = vehicleData.Physics.SpringStiffness / 10f;
            float tireRating = vehicleData.Physics.TireGripCoefficient * 20f;

            return Mathf.Clamp01((engineRating + suspensionRating + tireRating) / 3f) * 100f;
        }

        /// <summary>
        /// Export profile as JSON string (for sharing).
        /// </summary>
        public string ExportProfileAsString(string profileName)
        {
            VehicleProfile profile = LoadVehicleProfile(profileName);
            if (profile == null)
                return "";

            return JsonUtility.ToJson(profile, true);
        }

        /// <summary>
        /// Import profile from JSON string.
        /// </summary>
        public bool ImportProfileFromString(string json, string importName = "")
        {
            try
            {
                VehicleProfile profile = JsonUtility.FromJson<VehicleProfile>(json);
                if (importName != "")
                    profile.Name = importName;

                SaveVehicleProfile(profile.Name, profile.Data, profile.Description);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import profile: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get player profile.
        /// </summary>
        public PlayerProfile GetPlayerProfile() => currentPlayerProfile;

        /// <summary>
        /// Update player statistics.
        /// </summary>
        public void UpdatePlayerStats(string statType, float value)
        {
            switch (statType)
            {
                case "burnout":
                    if (value > currentPlayerProfile.BestBurnoutDistance)
                        currentPlayerProfile.BestBurnoutDistance = value;
                    break;
                case "drift":
                    if (value > currentPlayerProfile.BestDriftScore)
                        currentPlayerProfile.BestDriftScore = value;
                    break;
                case "laptime":
                    if (value < currentPlayerProfile.BestLapTime)
                        currentPlayerProfile.BestLapTime = value;
                    break;
            }
            SavePlayerProfile();
        }

        /// <summary>
        /// Get profile statistics string.
        /// </summary>
        public string GetProfileStats()
        {
            string stats = "=== PLAYER PROFILE ===\n";
            stats += $"Player: {currentPlayerProfile.PlayerName}\n";
            stats += $"Saved Vehicles: {currentPlayerProfile.SavedVehicleNames.Count}\n";
            stats += $"Sessions Completed: {currentPlayerProfile.SessionsCompleted}\n";
            stats += $"Best Burnout: {currentPlayerProfile.BestBurnoutDistance:F1}m\n";
            stats += $"Best Drift Score: {currentPlayerProfile.BestDriftScore:F0}\n";
            if (currentPlayerProfile.BestLapTime < float.MaxValue)
                stats += $"Best Lap Time: {currentPlayerProfile.BestLapTime:F2}s\n";
            return stats;
        }
    }
}
