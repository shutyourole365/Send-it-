using UnityEngine;
using UnityEngine.UI;
using SendIt.Data;
using System.Collections.Generic;

namespace SendIt.UI
{
    /// <summary>
    /// UI system for career progression display and management.
    /// Shows driver profile, upgrades, milestones, and race history.
    /// </summary>
    public class CareerProgressionUI : MonoBehaviour
    {
        [SerializeField] private CareerProgressionSystem careerSystem;

        // Driver Profile UI
        [SerializeField] private Text driverNameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Image levelBar;
        [SerializeField] private Text experienceText;
        [SerializeField] private Text balanceText;
        [SerializeField] private Text statsText;

        // Upgrades UI
        [SerializeField] private ScrollRect upgradesScrollRect;
        [SerializeField] private Transform upgradesContent;
        [SerializeField] private GameObject upgradeItemPrefab;

        // Milestones UI
        [SerializeField] private ScrollRect milestonesScrollRect;
        [SerializeField] private Transform milestonesContent;
        [SerializeField] private GameObject milestoneItemPrefab;

        // Race History UI
        [SerializeField] private ScrollRect historyScrollRect;
        [SerializeField] private Transform historyContent;
        [SerializeField] private GameObject raceResultItemPrefab;

        private bool isInitialized;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (careerSystem == null)
                careerSystem = FindObjectOfType<CareerProgressionSystem>();

            careerSystem.Initialize();
            RefreshAllUI();
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            // Update dynamic elements
            if (balanceText != null)
            {
                var (level, xp, balance, races, wins) = careerSystem.GetCareerStats();
                balanceText.text = $"${balance:F0}";
            }
        }

        /// <summary>
        /// Refresh all UI elements.
        /// </summary>
        public void RefreshAllUI()
        {
            RefreshProfileUI();
            RefreshUpgradesUI();
            RefreshMilestonesUI();
            RefreshRaceHistoryUI();
        }

        /// <summary>
        /// Refresh driver profile display.
        /// </summary>
        private void RefreshProfileUI()
        {
            var (level, xp, balance, races, wins) = careerSystem.GetCareerStats();

            if (levelText != null)
                levelText.text = $"Level {level}";

            if (experienceText != null)
                experienceText.text = $"{xp} XP";

            if (balanceText != null)
                balanceText.text = $"${balance:F0}";

            if (levelBar != null)
            {
                int xpInLevel = xp % 1000;
                levelBar.fillAmount = xpInLevel / 1000f;
            }

            if (statsText != null)
                statsText.text = $"Races: {races} | Wins: {wins} | Podiums: N/A";
        }

        /// <summary>
        /// Refresh vehicle upgrades display.
        /// </summary>
        private void RefreshUpgradesUI()
        {
            if (upgradesContent == null)
                return;

            // Clear existing
            foreach (Transform child in upgradesContent)
                Destroy(child.gameObject);

            var availableUpgrades = careerSystem.GetAvailableUpgrades();

            foreach (var upgrade in availableUpgrades)
            {
                if (upgradeItemPrefab == null)
                    continue;

                var item = Instantiate(upgradeItemPrefab, upgradesContent);
                var texts = item.GetComponentsInChildren<Text>();
                var button = item.GetComponentInChildren<Button>();

                if (texts.Length >= 2)
                {
                    texts[0].text = upgrade.UpgradeName;
                    texts[1].text = $"${upgrade.Cost:F0} | +{upgrade.PerformanceGain * 100:F0}%";
                }

                if (button != null)
                {
                    button.onClick.AddListener(() => OnPurchaseUpgradeClicked(upgrade.UpgradeName));
                }
            }
        }

        /// <summary>
        /// Refresh milestones display.
        /// </summary>
        private void RefreshMilestonesUI()
        {
            if (milestonesContent == null)
                return;

            // Clear existing
            foreach (Transform child in milestonesContent)
                Destroy(child.gameObject);

            var milestones = careerSystem.GetMilestones();

            foreach (var milestone in milestones)
            {
                if (milestoneItemPrefab == null)
                    continue;

                var item = Instantiate(milestoneItemPrefab, milestonesContent);
                var texts = item.GetComponentsInChildren<Text>();
                var image = item.GetComponentInChildren<Image>();

                if (texts.Length >= 2)
                {
                    texts[0].text = milestone.MilestoneName;
                    texts[1].text = milestone.Description;
                    texts[0].color = milestone.IsAchieved ? Color.yellow : Color.gray;
                }

                if (image != null)
                {
                    image.color = milestone.IsAchieved ? Color.green : Color.red;
                }
            }
        }

        /// <summary>
        /// Refresh race history display.
        /// </summary>
        private void RefreshRaceHistoryUI()
        {
            if (historyContent == null)
                return;

            // Clear existing
            foreach (Transform child in historyContent)
                Destroy(child.gameObject);

            var recentRaces = careerSystem.GetRecentRaces(10);

            foreach (var race in recentRaces)
            {
                if (raceResultItemPrefab == null)
                    continue;

                var item = Instantiate(raceResultItemPrefab, historyContent);
                var text = item.GetComponentInChildren<Text>();

                if (text != null)
                {
                    text.text = $"P{race.Position} - {race.EventName} ({race.TrackName})\n" +
                               $"${race.PrizeMoney:F0} | {race.ExperiencePoints} XP | {race.BestLapTime:F2}s";
                }
            }
        }

        // Callbacks
        private void OnPurchaseUpgradeClicked(string upgradeName)
        {
            if (careerSystem.PurchaseUpgrade(upgradeName))
            {
                RefreshAllUI();
                Debug.Log($"Upgrade purchased: {upgradeName}");
            }
            else
            {
                Debug.LogWarning($"Failed to purchase upgrade: {upgradeName}");
            }
        }

        public bool IsInitialized => isInitialized;
    }
}
