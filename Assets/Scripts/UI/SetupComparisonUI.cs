using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using SendIt.Data;

namespace SendIt.UI
{
    /// <summary>
    /// UI system for setup comparison and management.
    /// Provides interface for saving, loading, comparing, and analyzing vehicle setups.
    /// </summary>
    public class SetupComparisonUI : MonoBehaviour
    {
        [SerializeField] private SetupComparisonSystem comparisonSystem;

        // Setup Management UI
        [SerializeField] private InputField saveSetupNameInput;
        [SerializeField] private InputField saveSetupDescriptionInput;
        [SerializeField] private Button saveSetupButton;
        [SerializeField] private Text setupCountLabel;

        // Setup Selection UI
        [SerializeField] private Dropdown loadSetupDropdown;
        [SerializeField] private Button loadSetupButton;
        [SerializeField] private Button deleteSetupButton;
        [SerializeField] private Button duplicateSetupButton;

        // Setup Comparison UI
        [SerializeField] private Dropdown compareSetup1Dropdown;
        [SerializeField] private Dropdown compareSetup2Dropdown;
        [SerializeField] private Button compareButton;
        [SerializeField] private Text comparisonResultsText;
        [SerializeField] private ScrollRect comparisonScrollRect;

        // Performance Sorting UI
        [SerializeField] private Dropdown filterByTrackDropdown;
        [SerializeField] private Button sortByPerformanceButton;
        [SerializeField] private Button sortByUsageButton;
        [SerializeField] private ListView setupListView;

        private string currentSelectedSetup;
        private SetupComparisonSystem.SetupComparison currentComparison;
        private bool isInitialized;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (comparisonSystem == null)
                comparisonSystem = FindObjectOfType<SetupComparisonSystem>();

            comparisonSystem.Initialize();

            SetupSaveUI();
            SetupLoadUI();
            SetupComparisonUI();
            SetupSortingUI();

            RefreshSetupDropdowns();
            isInitialized = true;
        }

        /// <summary>
        /// Setup save functionality UI.
        /// </summary>
        private void SetupSaveUI()
        {
            if (saveSetupButton != null)
            {
                saveSetupButton.onClick.AddListener(OnSaveSetupClicked);
            }

            if (saveSetupNameInput != null)
            {
                saveSetupNameInput.contentType = InputField.ContentType.Alphanumeric;
                saveSetupNameInput.characterLimit = 50;
            }

            if (saveSetupDescriptionInput != null)
            {
                saveSetupDescriptionInput.contentType = InputField.ContentType.Standard;
                saveSetupDescriptionInput.characterLimit = 200;
            }
        }

        /// <summary>
        /// Setup load/management functionality UI.
        /// </summary>
        private void SetupLoadUI()
        {
            if (loadSetupButton != null)
            {
                loadSetupButton.onClick.AddListener(OnLoadSetupClicked);
            }

            if (deleteSetupButton != null)
            {
                deleteSetupButton.onClick.AddListener(OnDeleteSetupClicked);
            }

            if (duplicateSetupButton != null)
            {
                duplicateSetupButton.onClick.AddListener(OnDuplicateSetupClicked);
            }

            if (loadSetupDropdown != null)
            {
                loadSetupDropdown.onValueChanged.AddListener(OnSetupSelectionChanged);
            }
        }

        /// <summary>
        /// Setup comparison functionality UI.
        /// </summary>
        private void SetupComparisonUI()
        {
            if (compareButton != null)
            {
                compareButton.onClick.AddListener(OnCompareSetups);
            }

            if (compareSetup1Dropdown != null)
            {
                compareSetup1Dropdown.onValueChanged.AddListener((int index) => { });
            }

            if (compareSetup2Dropdown != null)
            {
                compareSetup2Dropdown.onValueChanged.AddListener((int index) => { });
            }
        }

        /// <summary>
        /// Setup sorting/filtering functionality UI.
        /// </summary>
        private void SetupSortingUI()
        {
            if (sortByPerformanceButton != null)
            {
                sortByPerformanceButton.onClick.AddListener(OnSortByPerformance);
            }

            if (sortByUsageButton != null)
            {
                sortByUsageButton.onClick.AddListener(OnSortByUsage);
            }

            if (filterByTrackDropdown != null)
            {
                filterByTrackDropdown.onValueChanged.AddListener(OnTrackFilterChanged);
            }
        }

        // Save Setup Callbacks
        private void OnSaveSetupClicked()
        {
            if (saveSetupNameInput == null || string.IsNullOrEmpty(saveSetupNameInput.text))
            {
                Debug.LogWarning("Setup name is required.");
                return;
            }

            string setupName = saveSetupNameInput.text;
            string description = saveSetupDescriptionInput != null ? saveSetupDescriptionInput.text : "";

            comparisonSystem.SaveCurrentSetup(setupName, description);

            // Clear inputs
            if (saveSetupNameInput != null) saveSetupNameInput.text = "";
            if (saveSetupDescriptionInput != null) saveSetupDescriptionInput.text = "";

            RefreshSetupDropdowns();
            UpdateSetupCountLabel();
        }

        // Load Setup Callbacks
        private void OnSetupSelectionChanged(int index)
        {
            if (loadSetupDropdown != null && index >= 0 && index < loadSetupDropdown.options.Count)
            {
                currentSelectedSetup = loadSetupDropdown.options[index].text;
            }
        }

        private void OnLoadSetupClicked()
        {
            if (string.IsNullOrEmpty(currentSelectedSetup))
            {
                Debug.LogWarning("No setup selected.");
                return;
            }

            comparisonSystem.LoadSetup(currentSelectedSetup);
        }

        private void OnDeleteSetupClicked()
        {
            if (string.IsNullOrEmpty(currentSelectedSetup))
            {
                Debug.LogWarning("No setup selected.");
                return;
            }

            if (comparisonSystem.DeleteSetup(currentSelectedSetup))
            {
                currentSelectedSetup = null;
                RefreshSetupDropdowns();
                UpdateSetupCountLabel();
            }
        }

        private void OnDuplicateSetupClicked()
        {
            if (string.IsNullOrEmpty(currentSelectedSetup))
            {
                Debug.LogWarning("No setup selected.");
                return;
            }

            string newName = currentSelectedSetup + " (Copy)";
            if (comparisonSystem.DuplicateSetup(currentSelectedSetup, newName))
            {
                RefreshSetupDropdowns();
                UpdateSetupCountLabel();
            }
        }

        // Comparison Callbacks
        private void OnCompareSetups()
        {
            if (compareSetup1Dropdown == null || compareSetup2Dropdown == null)
                return;

            string setup1Name = GetDropdownSelectedText(compareSetup1Dropdown);
            string setup2Name = GetDropdownSelectedText(compareSetup2Dropdown);

            if (string.IsNullOrEmpty(setup1Name) || string.IsNullOrEmpty(setup2Name))
            {
                Debug.LogWarning("Both setups must be selected.");
                return;
            }

            if (setup1Name == setup2Name)
            {
                Debug.LogWarning("Cannot compare a setup with itself.");
                return;
            }

            currentComparison = comparisonSystem.CompareSetups(setup1Name, setup2Name);
            DisplayComparisonResults();
        }

        private void DisplayComparisonResults()
        {
            if (comparisonResultsText == null)
                return;

            string report = comparisonSystem.GenerateComparisonReport(currentComparison);
            comparisonResultsText.text = report;

            // Scroll to top
            if (comparisonScrollRect != null)
            {
                comparisonScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        // Sorting Callbacks
        private void OnSortByPerformance()
        {
            string trackName = GetDropdownSelectedText(filterByTrackDropdown);
            var sortedSetups = comparisonSystem.GetSetupsByPerformance(trackName);
            DisplaySetupList(sortedSetups);
        }

        private void OnSortByUsage()
        {
            var mostUsed = comparisonSystem.GetMostUsedSetups(10);
            DisplaySetupList(mostUsed);
        }

        private void OnTrackFilterChanged(int index)
        {
            // Trigger refresh of displayed setups if a list view is visible
            if (setupListView != null && setupListView.gameObject.activeInHierarchy)
            {
                OnSortByPerformance();
            }
        }

        // Helper Methods
        private void RefreshSetupDropdowns()
        {
            var allSetups = comparisonSystem.GetAllSetups();

            // Populate load dropdown
            if (loadSetupDropdown != null)
            {
                loadSetupDropdown.ClearOptions();
                var setupNames = allSetups.Select(s => s.SetupName).ToList();
                loadSetupDropdown.AddOptions(setupNames);
            }

            // Populate comparison dropdowns
            if (compareSetup1Dropdown != null)
            {
                compareSetup1Dropdown.ClearOptions();
                compareSetup1Dropdown.AddOptions(allSetups.Select(s => s.SetupName).ToList());
            }

            if (compareSetup2Dropdown != null)
            {
                compareSetup2Dropdown.ClearOptions();
                compareSetup2Dropdown.AddOptions(allSetups.Select(s => s.SetupName).ToList());
            }

            // Populate track filter dropdown
            if (filterByTrackDropdown != null)
            {
                filterByTrackDropdown.ClearOptions();
                var trackNames = new HashSet<string> { "All Tracks" };
                foreach (var setup in allSetups)
                {
                    if (!string.IsNullOrEmpty(setup.TrackName))
                        trackNames.Add(setup.TrackName);
                }
                filterByTrackDropdown.AddOptions(trackNames.ToList());
            }

            UpdateSetupCountLabel();
        }

        private void UpdateSetupCountLabel()
        {
            if (setupCountLabel != null)
            {
                int current = comparisonSystem.GetSetupCount();
                int max = comparisonSystem.GetMaxSetups();
                setupCountLabel.text = $"Setups: {current}/{max}";
            }
        }

        private void DisplaySetupList(List<SetupComparisonSystem.SavedSetup> setups)
        {
            if (setupListView == null)
                return;

            setupListView.ClearItems();

            foreach (var setup in setups)
            {
                string itemText = $"{setup.SetupName} - {setup.TrackName} ({setup.BestLapTime:F3}s) [Used: {setup.UseCount}x]";
                setupListView.AddItem(itemText, setup.SetupName);
            }
        }

        private string GetDropdownSelectedText(Dropdown dropdown)
        {
            if (dropdown == null || dropdown.value < 0 || dropdown.value >= dropdown.options.Count)
                return "";
            return dropdown.options[dropdown.value].text;
        }

        public bool IsInitialized => isInitialized;
    }

    /// <summary>
    /// Simple list view helper for displaying setup items.
    /// </summary>
    public class ListView : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private GameObject itemPrefab;

        private List<(string label, string data)> items = new List<(string, string)>();

        public void AddItem(string label, string data)
        {
            items.Add((label, data));

            if (itemPrefab != null && content != null)
            {
                var item = Instantiate(itemPrefab, content);
                var text = item.GetComponent<Text>();
                if (text != null)
                    text.text = label;
            }
        }

        public void ClearItems()
        {
            items.Clear();
            if (content != null)
            {
                foreach (Transform child in content)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
}
