using UnityEngine;
using UnityEngine.UI;
using SendIt.Physics;
using System.Collections.Generic;

namespace SendIt.UI
{
    /// <summary>
    /// UI system for displaying and managing vehicle damage.
    /// Shows component damage status, repair options, and impact history.
    /// </summary>
    public class DamageVisualizationUI : MonoBehaviour
    {
        [SerializeField] private VehicleDamageSystem damageSystem;

        // Overall Damage Display
        [SerializeField] private Text overallDamageLabel;
        [SerializeField] private Image overallDamageBar;
        [SerializeField] private Text damageStatusText;

        // Component Display
        [SerializeField] private ScrollRect componentScrollRect;
        [SerializeField] private Transform componentListContent;
        [SerializeField] private GameObject componentDamageItemPrefab;

        // Repair Interface
        [SerializeField] private Button fullRepairButton;
        [SerializeField] private Text fullRepairCostText;
        [SerializeField] private Button selectiveRepairButton;
        [SerializeField] private Text totalRepairCostLabel;

        // Impact History
        [SerializeField] private ScrollRect impactHistoryScrollRect;
        [SerializeField] private Transform impactHistoryContent;
        [SerializeField] private GameObject impactHistoryItemPrefab;

        // Visual Damage Indicator
        [SerializeField] private Image vehicleDamageIndicator;
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color damagedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;

        private Dictionary<string, DamageComponentUI> componentUIElements = new Dictionary<string, DamageComponentUI>();
        private bool isInitialized;

        private struct DamageComponentUI
        {
            public Text damageLabel;
            public Image damageBar;
            public Button repairButton;
            public string componentName;
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (damageSystem == null)
                damageSystem = FindObjectOfType<VehicleDamageSystem>();

            damageSystem.Initialize();

            SetupRepairButtons();
            RefreshDamageDisplay();

            isInitialized = true;
        }

        private void SetupRepairButtons()
        {
            if (fullRepairButton != null)
            {
                fullRepairButton.onClick.AddListener(OnFullRepairClicked);
            }

            if (selectiveRepairButton != null)
            {
                selectiveRepairButton.onClick.AddListener(OnSelectiveRepairClicked);
            }
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            RefreshDamageDisplay();
        }

        /// <summary>
        /// Refresh all damage display elements.
        /// </summary>
        public void RefreshDamageDisplay()
        {
            UpdateOverallDamageDisplay();
            UpdateComponentDamageDisplay();
            UpdateRepairCostDisplay();
            UpdateImpactHistory();
            UpdateVisualIndicator();
        }

        /// <summary>
        /// Update overall damage display.
        /// </summary>
        private void UpdateOverallDamageDisplay()
        {
            float overallDamage = damageSystem.GetOverallDamage();

            if (overallDamageLabel != null)
                overallDamageLabel.text = $"Overall Damage: {overallDamage * 100f:F1}%";

            if (overallDamageBar != null)
                overallDamageBar.fillAmount = overallDamage;

            if (damageStatusText != null)
            {
                string status = GetDamageStatus(overallDamage);
                damageStatusText.text = status;
            }
        }

        /// <summary>
        /// Update individual component damage display.
        /// </summary>
        private void UpdateComponentDamageDisplay()
        {
            var allDamage = damageSystem.GetAllComponentDamage();

            // Clear existing items if list has changed
            if (componentUIElements.Count != allDamage.Count)
            {
                ClearComponentList();
            }

            foreach (var component in allDamage.Values)
            {
                if (!componentUIElements.ContainsKey(component.ComponentName))
                {
                    CreateComponentDamageItem(component);
                }
                else
                {
                    UpdateComponentDamageItem(component);
                }
            }
        }

        /// <summary>
        /// Create UI element for a component damage display.
        /// </summary>
        private void CreateComponentDamageItem(VehicleDamageSystem.ComponentDamage component)
        {
            if (componentDamageItemPrefab == null || componentListContent == null)
                return;

            var item = Instantiate(componentDamageItemPrefab, componentListContent);
            var damageLabel = item.GetComponentInChildren<Text>();
            var damageBar = item.GetComponentInChildren<Image>();
            var repairButton = item.GetComponentInChildren<Button>();

            var uiElement = new DamageComponentUI
            {
                damageLabel = damageLabel,
                damageBar = damageBar,
                repairButton = repairButton,
                componentName = component.ComponentName
            };

            if (repairButton != null)
            {
                string componentName = component.ComponentName;
                repairButton.onClick.AddListener(() => OnRepairComponentClicked(componentName));
            }

            componentUIElements[component.ComponentName] = uiElement;
            UpdateComponentDamageItem(component);
        }

        /// <summary>
        /// Update display of a component damage item.
        /// </summary>
        private void UpdateComponentDamageItem(VehicleDamageSystem.ComponentDamage component)
        {
            if (!componentUIElements.ContainsKey(component.ComponentName))
                return;

            var uiElement = componentUIElements[component.ComponentName];

            if (uiElement.damageLabel != null)
            {
                string status = component.IsFunctional ? "Functional" : "BROKEN";
                uiElement.damageLabel.text = $"{component.ComponentName}: {component.DamageAmount * 100f:F1}% ({status})";
                uiElement.damageLabel.color = component.IsFunctional ? Color.white : Color.red;
            }

            if (uiElement.damageBar != null)
            {
                uiElement.damageBar.fillAmount = component.DamageAmount;

                // Color gradient based on damage
                if (component.DamageAmount < 0.33f)
                    uiElement.damageBar.color = healthyColor;
                else if (component.DamageAmount < 0.66f)
                    uiElement.damageBar.color = damagedColor;
                else
                    uiElement.damageBar.color = criticalColor;
            }

            if (uiElement.repairButton != null)
            {
                uiElement.repairButton.interactable = component.DamageAmount > 0f;
            }
        }

        /// <summary>
        /// Update repair cost display.
        /// </summary>
        private void UpdateRepairCostDisplay()
        {
            float totalRepairCost = 0f;
            var allDamage = damageSystem.GetAllComponentDamage();

            foreach (var component in allDamage.Values)
            {
                totalRepairCost += component.DamageAmount * 1000f;
            }

            if (fullRepairCostText != null)
                fullRepairCostText.text = $"Full Repair: ${totalRepairCost:F2}";

            if (totalRepairCostLabel != null)
                totalRepairCostLabel.text = $"Total Repair Cost: ${totalRepairCost:F2}";
        }

        /// <summary>
        /// Update impact history display.
        /// </summary>
        private void UpdateImpactHistory()
        {
            if (impactHistoryContent == null)
                return;

            // Clear existing items
            foreach (Transform child in impactHistoryContent)
            {
                Destroy(child.gameObject);
            }

            var recentImpacts = damageSystem.GetRecentImpacts();

            // Display most recent impacts (reverse order)
            for (int i = Mathf.Max(0, recentImpacts.Count - 10); i < recentImpacts.Count; i++)
            {
                var impact = recentImpacts[i];
                CreateImpactHistoryItem(impact);
            }
        }

        /// <summary>
        /// Create UI element for impact history.
        /// </summary>
        private void CreateImpactHistoryItem(VehicleDamageSystem.ImpactEvent impact)
        {
            if (impactHistoryItemPrefab == null || impactHistoryContent == null)
                return;

            var item = Instantiate(impactHistoryItemPrefab, impactHistoryContent);
            var text = item.GetComponentInChildren<Text>();

            if (text != null)
            {
                string timeStr = impact.TimeOfImpact.ToString("HH:mm:ss");
                text.text = $"[{timeStr}] {impact.ComponentHit} - {impact.ImpactSpeed:F1} m/s ({impact.ImpactForce:F0} N)";

                // Color code by severity
                if (impact.ImpactSpeed > 15f)
                    text.color = criticalColor;
                else if (impact.ImpactSpeed > 10f)
                    text.color = damagedColor;
                else
                    text.color = healthyColor;
            }
        }

        /// <summary>
        /// Update overall damage visual indicator.
        /// </summary>
        private void UpdateVisualIndicator()
        {
            if (vehicleDamageIndicator == null)
                return;

            float overallDamage = damageSystem.GetOverallDamage();

            if (overallDamage < 0.33f)
                vehicleDamageIndicator.color = healthyColor;
            else if (overallDamage < 0.66f)
                vehicleDamageIndicator.color = damagedColor;
            else
                vehicleDamageIndicator.color = criticalColor;

            vehicleDamageIndicator.fillAmount = overallDamage;
        }

        // Repair Callbacks
        private void OnFullRepairClicked()
        {
            float cost = damageSystem.FullRepair();
            Debug.Log($"Full repair completed. Cost: ${cost:F2}");
            RefreshDamageDisplay();
        }

        private void OnSelectiveRepairClicked()
        {
            Debug.Log("Selective repair - user can choose individual components");
            // This would open a component selection dialog
        }

        private void OnRepairComponentClicked(string componentName)
        {
            float cost = damageSystem.RepairComponent(componentName, 1.0f);
            Debug.Log($"Repaired {componentName}. Cost: ${cost:F2}");
            RefreshDamageDisplay();
        }

        /// <summary>
        /// Clear component damage list.
        /// </summary>
        private void ClearComponentList()
        {
            componentUIElements.Clear();

            if (componentListContent != null)
            {
                foreach (Transform child in componentListContent)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        /// <summary>
        /// Get damage status text.
        /// </summary>
        private string GetDamageStatus(float damageLevel)
        {
            if (damageLevel < 0.1f)
                return "Excellent Condition";
            else if (damageLevel < 0.25f)
                return "Minor Damage";
            else if (damageLevel < 0.5f)
                return "Moderate Damage";
            else if (damageLevel < 0.75f)
                return "Severe Damage";
            else
                return "Critical Condition!";
        }

        public bool IsInitialized => isInitialized;
    }
}
