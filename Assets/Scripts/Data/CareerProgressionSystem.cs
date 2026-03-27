using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SendIt.Data
{
    /// <summary>
    /// Career progression system managing driver development, events, and achievements.
    /// Tracks career progress, finances, and unlockable content.
    /// </summary>
    public class CareerProgressionSystem : MonoBehaviour
    {
        /// <summary>
        /// Event result data.
        /// </summary>
        public struct EventResult
        {
            public string EventName;
            public string TrackName;
            public int Position;
            public int TotalParticipants;
            public float BestLapTime;
            public float PrizeMoney;
            public int ExperiencePoints;
            public System.DateTime CompleteDate;
            public float FastestLapBonus; // 10% bonus if fastest lap
        }

        /// <summary>
        /// Career milestone.
        /// </summary>
        public struct CareerMilestone
        {
            public string MilestoneName;
            public string Description;
            public int ExperienceRequired;
            public float MoneyReward;
            public bool IsAchieved;
            public System.DateTime AchievedDate;
        }

        /// <summary>
        /// Vehicle upgrade.
        /// </summary>
        public struct VehicleUpgrade
        {
            public string UpgradeName;
            public string UpgradeType; // "Engine", "Suspension", "Brakes", "Aero"
            public float Cost;
            public float PerformanceGain;
            public float DurabilityGain;
            public bool IsInstalled;
            public int RequiredLevel;
        }

        [SerializeField] private string driverName = "Player";
        [SerializeField] private int currentCareerLevel = 1;

        // Career progression
        private int totalExperiencePoints;
        private float totalPrizeMoney;
        private int racesCompleted;
        private float averageFinishPosition;
        private int firstPlaceFinishes;

        // Career events
        private List<EventResult> eventHistory = new List<EventResult>();
        private List<CareerMilestone> milestones = new List<CareerMilestone>();
        private List<VehicleUpgrade> availableUpgrades = new List<VehicleUpgrade>();
        private List<VehicleUpgrade> installedUpgrades = new List<VehicleUpgrade>();

        // Financial system
        private float sponsorshipIncome;
        private float totalSpent;
        private float currentBalance;
        private const float startingMoney = 50000f;

        // Driver stats
        private float averageLapTime = float.MaxValue;
        private float bestEverLapTime = float.MaxValue;
        private int totalLapsRaced;
        private float totalRaceTime;

        // Season data
        private int currentSeason = 1;
        private int eventsCompletedThisSeason;
        private float seasonPrizeMoney;

        private bool initialized;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (initialized)
                return;

            currentBalance = startingMoney;
            InitializeMilestones();
            InitializeUpgrades();
            LoadCareerData();

            initialized = true;
            Debug.Log($"Career system initialized for {driverName}");
        }

        /// <summary>
        /// Initialize milestone system.
        /// </summary>
        private void InitializeMilestones()
        {
            milestones = new List<CareerMilestone>
            {
                new CareerMilestone
                {
                    MilestoneName = "First Race",
                    Description = "Complete your first race",
                    ExperienceRequired = 0,
                    MoneyReward = 0f,
                    IsAchieved = false
                },
                new CareerMilestone
                {
                    MilestoneName = "Podium Finisher",
                    Description = "Achieve top 3 finish",
                    ExperienceRequired = 100,
                    MoneyReward = 5000f,
                    IsAchieved = false
                },
                new CareerMilestone
                {
                    MilestoneName = "Champion",
                    Description = "Win a race",
                    ExperienceRequired = 500,
                    MoneyReward = 15000f,
                    IsAchieved = false
                },
                new CareerMilestone
                {
                    MilestoneName = "Experienced Driver",
                    Description = "Complete 10 races",
                    ExperienceRequired = 1000,
                    MoneyReward = 25000f,
                    IsAchieved = false
                },
                new CareerMilestone
                {
                    MilestoneName = "Season Winner",
                    Description = "Win a season championship",
                    ExperienceRequired = 5000,
                    MoneyReward = 100000f,
                    IsAchieved = false
                }
            };
        }

        /// <summary>
        /// Initialize vehicle upgrade system.
        /// </summary>
        private void InitializeUpgrades()
        {
            availableUpgrades = new List<VehicleUpgrade>
            {
                new VehicleUpgrade
                {
                    UpgradeName = "Turbocharger",
                    UpgradeType = "Engine",
                    Cost = 15000f,
                    PerformanceGain = 0.15f,
                    DurabilityGain = -0.05f,
                    RequiredLevel = 5
                },
                new VehicleUpgrade
                {
                    UpgradeName = "High-Flow Intake",
                    UpgradeType = "Engine",
                    Cost = 8000f,
                    PerformanceGain = 0.08f,
                    DurabilityGain = 0f,
                    RequiredLevel = 3
                },
                new VehicleUpgrade
                {
                    UpgradeName = "Carbon Suspension",
                    UpgradeType = "Suspension",
                    Cost = 12000f,
                    PerformanceGain = 0.12f,
                    DurabilityGain = 0.10f,
                    RequiredLevel = 4
                },
                new VehicleUpgrade
                {
                    UpgradeName = "Race Brakes",
                    UpgradeType = "Brakes",
                    Cost = 10000f,
                    PerformanceGain = 0.10f,
                    DurabilityGain = 0.05f,
                    RequiredLevel = 3
                },
                new VehicleUpgrade
                {
                    UpgradeName = "Aerodynamic Kit",
                    UpgradeType = "Aero",
                    Cost = 18000f,
                    PerformanceGain = 0.20f,
                    DurabilityGain = 0f,
                    RequiredLevel = 6
                },
                new VehicleUpgrade
                {
                    UpgradeName = "Lightweight Chassis",
                    UpgradeType = "Weight",
                    Cost = 20000f,
                    PerformanceGain = 0.18f,
                    DurabilityGain = -0.10f,
                    RequiredLevel = 7
                }
            };
        }

        /// <summary>
        /// Record race result and update career progress.
        /// </summary>
        public void RecordRaceResult(string eventName, string trackName, int position,
                                   int totalParticipants, float bestLapTime, float raceTime)
        {
            float prizeMoney = CalculatePrizeMoney(position, totalParticipants);
            int experienceGain = CalculateExperience(position, totalParticipants, bestLapTime);
            float fastestLapBonus = 0f;

            // Check for fastest lap bonus (assume position 1 had fastest lap)
            if (position == 1)
            {
                fastestLapBonus = prizeMoney * 0.1f;
                prizeMoney += fastestLapBonus;
                experienceGain += 50;
            }

            var result = new EventResult
            {
                EventName = eventName,
                TrackName = trackName,
                Position = position,
                TotalParticipants = totalParticipants,
                BestLapTime = bestLapTime,
                PrizeMoney = prizeMoney,
                ExperiencePoints = experienceGain,
                CompleteDate = System.DateTime.Now,
                FastestLapBonus = fastestLapBonus
            };

            eventHistory.Add(result);

            // Update career stats
            totalPrizeMoney += prizeMoney;
            currentBalance += prizeMoney;
            totalExperiencePoints += experienceGain;
            racesCompleted++;
            eventsCompletedThisSeason++;
            seasonPrizeMoney += prizeMoney;

            // Update finish position tracking
            if (racesCompleted == 1)
                averageFinishPosition = position;
            else
                averageFinishPosition = (averageFinishPosition * (racesCompleted - 1) + position) / racesCompleted;

            if (position == 1)
                firstPlaceFinishes++;

            // Update lap time tracking
            if (bestLapTime < bestEverLapTime)
                bestEverLapTime = bestLapTime;

            if (averageLapTime == float.MaxValue)
                averageLapTime = bestLapTime;
            else
                averageLapTime = (averageLapTime + bestLapTime) / 2f;

            totalLapsRaced += (int)(raceTime / 80f); // Rough estimate
            totalRaceTime += raceTime;

            // Update level
            UpdateCareerLevel();

            // Check milestones
            CheckMilestones();

            Debug.Log($"Race result recorded: {eventName} - P{position} - ${prizeMoney:F0} - {experienceGain} XP");
        }

        /// <summary>
        /// Calculate prize money based on finishing position.
        /// </summary>
        private float CalculatePrizeMoney(int position, int totalParticipants)
        {
            if (position == 1) return 10000f;
            if (position == 2) return 6000f;
            if (position == 3) return 3500f;
            if (position <= 5) return 2000f;
            if (position <= 10) return 1000f;
            return 500f; // Participation bonus
        }

        /// <summary>
        /// Calculate experience points based on race performance.
        /// </summary>
        private int CalculateExperience(int position, int totalParticipants, float lapTime)
        {
            int baseXP = 0;

            if (position == 1) baseXP = 500;
            else if (position == 2) baseXP = 300;
            else if (position == 3) baseXP = 200;
            else if (position <= 5) baseXP = 150;
            else if (position <= 10) baseXP = 100;
            else baseXP = 50;

            // Bonus for good lap time
            if (lapTime < 80f) baseXP += 100; // Under 80 seconds
            if (lapTime < 70f) baseXP += 50;  // Under 70 seconds

            return baseXP;
        }

        /// <summary>
        /// Update career level based on experience.
        /// </summary>
        private void UpdateCareerLevel()
        {
            int newLevel = 1 + (totalExperiencePoints / 1000);
            if (newLevel > currentCareerLevel)
            {
                currentCareerLevel = newLevel;
                Debug.Log($"Career level advanced to {currentCareerLevel}!");
                // Award level-up bonus
                currentBalance += 5000f;
            }
        }

        /// <summary>
        /// Check and award milestone achievements.
        /// </summary>
        private void CheckMilestones()
        {
            foreach (var milestone in milestones)
            {
                if (!milestone.IsAchieved && totalExperiencePoints >= milestone.ExperienceRequired)
                {
                    // Find and update milestone
                    for (int i = 0; i < milestones.Count; i++)
                    {
                        if (milestones[i].MilestoneName == milestone.MilestoneName)
                        {
                            milestone.IsAchieved = true;
                            milestone.AchievedDate = System.DateTime.Now;
                            milestones[i] = milestone;

                            currentBalance += milestone.MoneyReward;
                            Debug.Log($"Milestone achieved: {milestone.MilestoneName}!");
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Purchase and install a vehicle upgrade.
        /// </summary>
        public bool PurchaseUpgrade(string upgradeName)
        {
            var upgrade = availableUpgrades.FirstOrDefault(u => u.UpgradeName == upgradeName);
            if (upgrade.UpgradeName == null)
            {
                Debug.LogWarning($"Upgrade not found: {upgradeName}");
                return false;
            }

            if (currentCareerLevel < upgrade.RequiredLevel)
            {
                Debug.LogWarning($"Career level {currentCareerLevel} required. Upgrade needs level {upgrade.RequiredLevel}");
                return false;
            }

            if (currentBalance < upgrade.Cost)
            {
                Debug.LogWarning($"Insufficient funds. Need ${upgrade.Cost:F0}, have ${currentBalance:F0}");
                return false;
            }

            // Purchase upgrade
            currentBalance -= upgrade.Cost;
            totalSpent += upgrade.Cost;
            upgrade.IsInstalled = true;
            installedUpgrades.Add(upgrade);

            // Remove from available upgrades
            availableUpgrades.Remove(upgrade);

            Debug.Log($"Upgrade purchased: {upgradeName} for ${upgrade.Cost:F0}");
            return true;
        }

        /// <summary>
        /// Get installed upgrades.
        /// </summary>
        public List<VehicleUpgrade> GetInstalledUpgrades() => new List<VehicleUpgrade>(installedUpgrades);

        /// <summary>
        /// Get available upgrades for purchase.
        /// </summary>
        public List<VehicleUpgrade> GetAvailableUpgrades()
        {
            return availableUpgrades.Where(u => currentCareerLevel >= u.RequiredLevel).ToList();
        }

        /// <summary>
        /// Calculate total performance multiplier from upgrades.
        /// </summary>
        public float GetPerformanceMultiplier()
        {
            float multiplier = 1f;
            foreach (var upgrade in installedUpgrades)
            {
                multiplier += upgrade.PerformanceGain;
            }
            return multiplier;
        }

        /// <summary>
        /// Get career statistics.
        /// </summary>
        public (int level, int totalXP, float balance, int racesCompleted, int wins) GetCareerStats()
        {
            return (currentCareerLevel, totalExperiencePoints, currentBalance, racesCompleted, firstPlaceFinishes);
        }

        /// <summary>
        /// Get recent race results.
        /// </summary>
        public List<EventResult> GetRecentRaces(int count = 10)
        {
            return eventHistory.OrderByDescending(r => r.CompleteDate).Take(count).ToList();
        }

        /// <summary>
        /// Get career milestones.
        /// </summary>
        public List<CareerMilestone> GetMilestones() => new List<CareerMilestone>(milestones);

        /// <summary>
        /// Start new season.
        /// </summary>
        public void StartNewSeason()
        {
            currentSeason++;
            eventsCompletedThisSeason = 0;
            seasonPrizeMoney = 0f;
            Debug.Log($"Season {currentSeason} started!");
        }

        /// <summary>
        /// Save career data.
        /// </summary>
        public void SaveCareerData()
        {
            string careerDataPath = System.IO.Path.Combine(
                Application.persistentDataPath,
                $"{driverName}_career.json"
            );

            var careerData = new CareerDataWrapper
            {
                driverName = driverName,
                currentCareerLevel = currentCareerLevel,
                totalExperiencePoints = totalExperiencePoints,
                totalPrizeMoney = totalPrizeMoney,
                currentBalance = currentBalance,
                racesCompleted = racesCompleted,
                firstPlaceFinishes = firstPlaceFinishes,
                bestEverLapTime = bestEverLapTime,
                currentSeason = currentSeason,
                lastSaveDate = System.DateTime.Now.ToString("o")
            };

            string json = JsonUtility.ToJson(careerData, true);
            System.IO.File.WriteAllText(careerDataPath, json);
            Debug.Log($"Career data saved: {careerDataPath}");
        }

        /// <summary>
        /// Load career data.
        /// </summary>
        private void LoadCareerData()
        {
            string careerDataPath = System.IO.Path.Combine(
                Application.persistentDataPath,
                $"{driverName}_career.json"
            );

            if (System.IO.File.Exists(careerDataPath))
            {
                string json = System.IO.File.ReadAllText(careerDataPath);
                var careerData = JsonUtility.FromJson<CareerDataWrapper>(json);

                currentCareerLevel = careerData.currentCareerLevel;
                totalExperiencePoints = careerData.totalExperiencePoints;
                totalPrizeMoney = careerData.totalPrizeMoney;
                currentBalance = careerData.currentBalance;
                racesCompleted = careerData.racesCompleted;
                firstPlaceFinishes = careerData.firstPlaceFinishes;
                bestEverLapTime = careerData.bestEverLapTime;
                currentSeason = careerData.currentSeason;

                Debug.Log($"Career data loaded for {driverName}");
            }
        }

        /// <summary>
        /// Get driver profile summary.
        /// </summary>
        public string GetCareerSummary()
        {
            return $@"
=== CAREER SUMMARY ===
Driver: {driverName}
Level: {currentCareerLevel}
Experience: {totalExperiencePoints} XP
Races Completed: {racesCompleted}
Wins: {firstPlaceFinishes}
Avg Position: {averageFinishPosition:F2}
Total Prize Money: ${totalPrizeMoney:F0}
Current Balance: ${currentBalance:F0}
Best Lap: {(bestEverLapTime == float.MaxValue ? "N/A" : bestEverLapTime.ToString("F2") + "s")}
Season: {currentSeason}
";
        }

        [System.Serializable]
        private class CareerDataWrapper
        {
            public string driverName;
            public int currentCareerLevel;
            public int totalExperiencePoints;
            public float totalPrizeMoney;
            public float currentBalance;
            public int racesCompleted;
            public int firstPlaceFinishes;
            public float bestEverLapTime;
            public int currentSeason;
            public string lastSaveDate;
        }
    }
}
