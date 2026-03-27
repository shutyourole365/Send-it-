using UnityEngine;
using System.Collections.Generic;
using System.Text;
using SendIt.Data;

namespace SendIt.Network
{
    /// <summary>
    /// Mobile app connectivity system for remote career management and setup sharing.
    /// Provides REST API endpoints for mobile app integration.
    /// </summary>
    public class MobileAppConnector : MonoBehaviour
    {
        /// <summary>
        /// Mobile app session data.
        /// </summary>
        public struct MobileSession
        {
            public string SessionToken;
            public string DeviceId;
            public string AppVersion;
            public System.DateTime LoginTime;
            public System.DateTime LastActivityTime;
            public bool IsActive;
        }

        [SerializeField] private CareerProgressionSystem careerSystem;
        [SerializeField] private SetupComparisonSystem setupSystem;
        [SerializeField] private int apiPort = 8080;

        private Dictionary<string, MobileSession> activeSessions = new Dictionary<string, MobileSession>();
        private List<string> connectedDevices = new List<string>();
        private bool serverActive;

        private const string apiVersion = "1.0";
        private const int sessionTimeoutMinutes = 30;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (careerSystem == null)
                careerSystem = FindObjectOfType<CareerProgressionSystem>();

            if (setupSystem == null)
                setupSystem = FindObjectOfType<SetupComparisonSystem>();

            Debug.Log($"Mobile App Connector initialized on port {apiPort}");
        }

        /// <summary>
        /// Start mobile API server.
        /// </summary>
        public void StartServer()
        {
            if (serverActive)
            {
                Debug.LogWarning("Server already running.");
                return;
            }

            serverActive = true;
            Debug.Log($"Mobile API server started on port {apiPort}");
        }

        /// <summary>
        /// Stop mobile API server.
        /// </summary>
        public void StopServer()
        {
            serverActive = false;
            activeSessions.Clear();
            connectedDevices.Clear();
            Debug.Log("Mobile API server stopped.");
        }

        /// <summary>
        /// Mobile app login endpoint.
        /// </summary>
        public string HandleLogin(string deviceId, string appVersion)
        {
            if (!serverActive)
                return CreateErrorResponse("Server not active");

            string sessionToken = System.Guid.NewGuid().ToString();
            var session = new MobileSession
            {
                SessionToken = sessionToken,
                DeviceId = deviceId,
                AppVersion = appVersion,
                LoginTime = System.DateTime.Now,
                LastActivityTime = System.DateTime.Now,
                IsActive = true
            };

            activeSessions[sessionToken] = session;
            if (!connectedDevices.Contains(deviceId))
                connectedDevices.Add(deviceId);

            var response = new LoginResponse
            {
                success = true,
                sessionToken = sessionToken,
                message = "Login successful"
            };

            return JsonUtility.ToJson(response);
        }

        /// <summary>
        /// Get career data endpoint.
        /// </summary>
        public string GetCareerData(string sessionToken)
        {
            if (!ValidateSession(sessionToken))
                return CreateErrorResponse("Invalid session");

            var (level, xp, balance, races, wins) = careerSystem.GetCareerStats();

            var response = new CareerDataResponse
            {
                success = true,
                level = level,
                totalExperience = xp,
                balance = balance,
                racesCompleted = races,
                wins = wins,
                bestLapTime = careerSystem.GetRecentRaces(1).Count > 0 ?
                    careerSystem.GetRecentRaces(1)[0].BestLapTime : 0f
            };

            return JsonUtility.ToJson(response);
        }

        /// <summary>
        /// Get race history endpoint.
        /// </summary>
        public string GetRaceHistory(string sessionToken, int count = 10)
        {
            if (!ValidateSession(sessionToken))
                return CreateErrorResponse("Invalid session");

            var races = careerSystem.GetRecentRaces(count);
            var raceData = new List<RaceHistoryItem>();

            foreach (var race in races)
            {
                raceData.Add(new RaceHistoryItem
                {
                    eventName = race.EventName,
                    trackName = race.TrackName,
                    position = race.Position,
                    bestLapTime = race.BestLapTime,
                    prizeMoney = race.PrizeMoney,
                    experiencePoints = race.ExperiencePoints,
                    completeDate = race.CompleteDate.ToString("o")
                });
            }

            var response = new RaceHistoryResponse
            {
                success = true,
                races = raceData
            };

            return JsonUtility.ToJson(new RaceHistoryWrapper { response = response });
        }

        /// <summary>
        /// Get available vehicle upgrades endpoint.
        /// </summary>
        public string GetAvailableUpgrades(string sessionToken)
        {
            if (!ValidateSession(sessionToken))
                return CreateErrorResponse("Invalid session");

            var upgrades = careerSystem.GetAvailableUpgrades();
            var upgradeData = new List<UpgradeItem>();

            foreach (var upgrade in upgrades)
            {
                upgradeData.Add(new UpgradeItem
                {
                    name = upgrade.UpgradeName,
                    type = upgrade.UpgradeType,
                    cost = upgrade.Cost,
                    performanceGain = upgrade.PerformanceGain,
                    requiredLevel = upgrade.RequiredLevel
                });
            }

            var response = new UpgradesResponse
            {
                success = true,
                upgrades = upgradeData
            };

            return JsonUtility.ToJson(new UpgradesWrapper { response = response });
        }

        /// <summary>
        /// Purchase upgrade endpoint.
        /// </summary>
        public string PurchaseUpgrade(string sessionToken, string upgradeName)
        {
            if (!ValidateSession(sessionToken))
                return CreateErrorResponse("Invalid session");

            bool success = careerSystem.PurchaseUpgrade(upgradeName);

            var response = new UpgradePurchaseResponse
            {
                success = success,
                message = success ? $"Upgrade {upgradeName} purchased" : "Purchase failed"
            };

            return JsonUtility.ToJson(response);
        }

        /// <summary>
        /// Get saved vehicle setups endpoint.
        /// </summary>
        public string GetSavedSetups(string sessionToken)
        {
            if (!ValidateSession(sessionToken))
                return CreateErrorResponse("Invalid session");

            var setups = setupSystem.GetAllSetups();
            var setupData = new List<SetupItem>();

            foreach (var setup in setups)
            {
                setupData.Add(new SetupItem
                {
                    name = setup.SetupName,
                    track = setup.TrackName,
                    bestLapTime = setup.BestLapTime,
                    useCount = setup.UseCount,
                    created = setup.CreatedDate.ToString("o")
                });
            }

            var response = new SetupsResponse
            {
                success = true,
                setups = setupData
            };

            return JsonUtility.ToJson(new SetupsWrapper { response = response });
        }

        /// <summary>
        /// Load setup endpoint.
        /// </summary>
        public string LoadSetup(string sessionToken, string setupName)
        {
            if (!ValidateSession(sessionToken))
                return CreateErrorResponse("Invalid session");

            bool success = setupSystem.LoadSetup(setupName);

            var response = new LoadSetupResponse
            {
                success = success,
                message = success ? $"Setup {setupName} loaded" : "Setup not found"
            };

            return JsonUtility.ToJson(response);
        }

        /// <summary>
        /// Validate mobile session token.
        /// </summary>
        private bool ValidateSession(string sessionToken)
        {
            if (!activeSessions.ContainsKey(sessionToken))
                return false;

            var session = activeSessions[sessionToken];

            // Check timeout
            double minutesElapsed = (System.DateTime.Now - session.LastActivityTime).TotalMinutes;
            if (minutesElapsed > sessionTimeoutMinutes)
            {
                activeSessions.Remove(sessionToken);
                return false;
            }

            // Update last activity
            session.LastActivityTime = System.DateTime.Now;
            activeSessions[sessionToken] = session;

            return session.IsActive;
        }

        /// <summary>
        /// Create error response.
        /// </summary>
        private string CreateErrorResponse(string message)
        {
            var response = new ErrorResponse
            {
                success = false,
                error = message
            };

            return JsonUtility.ToJson(response);
        }

        /// <summary>
        /// Get active session count.
        /// </summary>
        public int GetActiveSessionCount() => activeSessions.Count;

        /// <summary>
        /// Get connected device count.
        /// </summary>
        public int GetConnectedDeviceCount() => connectedDevices.Count;

        /// <summary>
        /// Get API status.
        /// </summary>
        public string GetApiStatus()
        {
            return $@"
API Status:
- Server Active: {serverActive}
- Port: {apiPort}
- API Version: {apiVersion}
- Active Sessions: {activeSessions.Count}
- Connected Devices: {connectedDevices.Count}
- Session Timeout: {sessionTimeoutMinutes} minutes
";
        }

        // Response data structures
        [System.Serializable]
        private class LoginResponse
        {
            public bool success;
            public string sessionToken;
            public string message;
        }

        [System.Serializable]
        private class ErrorResponse
        {
            public bool success;
            public string error;
        }

        [System.Serializable]
        private class CareerDataResponse
        {
            public bool success;
            public int level;
            public int totalExperience;
            public float balance;
            public int racesCompleted;
            public int wins;
            public float bestLapTime;
        }

        [System.Serializable]
        private class RaceHistoryItem
        {
            public string eventName;
            public string trackName;
            public int position;
            public float bestLapTime;
            public float prizeMoney;
            public int experiencePoints;
            public string completeDate;
        }

        [System.Serializable]
        private class RaceHistoryResponse
        {
            public bool success;
            public List<RaceHistoryItem> races;
        }

        [System.Serializable]
        private class RaceHistoryWrapper
        {
            public RaceHistoryResponse response;
        }

        [System.Serializable]
        private class UpgradeItem
        {
            public string name;
            public string type;
            public float cost;
            public float performanceGain;
            public int requiredLevel;
        }

        [System.Serializable]
        private class UpgradesResponse
        {
            public bool success;
            public List<UpgradeItem> upgrades;
        }

        [System.Serializable]
        private class UpgradesWrapper
        {
            public UpgradesResponse response;
        }

        [System.Serializable]
        private class UpgradePurchaseResponse
        {
            public bool success;
            public string message;
        }

        [System.Serializable]
        private class SetupItem
        {
            public string name;
            public string track;
            public float bestLapTime;
            public int useCount;
            public string created;
        }

        [System.Serializable]
        private class SetupsResponse
        {
            public bool success;
            public List<SetupItem> setups;
        }

        [System.Serializable]
        private class SetupsWrapper
        {
            public SetupsResponse response;
        }

        [System.Serializable]
        private class LoadSetupResponse
        {
            public bool success;
            public string message;
        }

        public bool IsServerActive => serverActive;
    }
}
