using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SendIt.Tuning;

namespace SendIt.UI
{
    /// <summary>
    /// Manages category tabs for organizing physics parameters.
    /// Groups parameters by category (Engine, Suspension, Tires, Aerodynamics, etc.)
    /// and provides tab-based UI navigation.
    /// </summary>
    public class CategoryTabManager : MonoBehaviour
    {
        [SerializeField] private Transform tabButtonContainer;
        [SerializeField] private Transform contentPanelContainer;

        [SerializeField] private Button tabButtonPrefab;
        [SerializeField] private PhysicsParameterUI parameterUIPrefab;

        private Dictionary<string, GameObject> categoryPanels = new Dictionary<string, GameObject>();
        private Dictionary<string, Button> categoryButtons = new Dictionary<string, Button>();
        private string currentActiveCategory;

        private TuningManager tuningManager;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the category tab system.
        /// </summary>
        private void Initialize()
        {
            tuningManager = TuningManager.Instance;
            if (tuningManager == null)
            {
                Debug.LogError("TuningManager not found!");
                return;
            }

            // Get all parameters and group by category
            var physicsParams = tuningManager.GetAllPhysicsParameters();
            Dictionary<string, List<TuneParameter>> categorizedParams = new Dictionary<string, List<TuneParameter>>();

            foreach (var param in physicsParams.Values)
            {
                if (!categorizedParams.ContainsKey(param.Category))
                {
                    categorizedParams[param.Category] = new List<TuneParameter>();
                }
                categorizedParams[param.Category].Add(param);
            }

            // Create tabs and panels for each category
            foreach (var category in categorizedParams)
            {
                CreateCategoryTab(category.Key, category.Value);
            }

            // Activate first category
            if (categoryPanels.Count > 0)
            {
                string firstCategory = new List<string>(categoryPanels.Keys)[0];
                SetActiveCategory(firstCategory);
            }
        }

        /// <summary>
        /// Create a tab and panel for a category.
        /// </summary>
        private void CreateCategoryTab(string categoryName, List<TuneParameter> parameters)
        {
            // Create tab button
            Button tabButton = Instantiate(tabButtonPrefab, tabButtonContainer);
            Text buttonText = tabButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = categoryName;
            }

            // Create content panel
            GameObject contentPanel = new GameObject(categoryName + " Panel");
            contentPanel.transform.SetParent(contentPanelContainer, false);

            // Add layout group
            VerticalLayoutGroup layout = contentPanel.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 5f;
            layout.padding = new RectOffset(10, 10, 10, 10);

            // Add parameters to panel
            foreach (var param in parameters)
            {
                CreateParameterUI(param, contentPanel.transform);
            }

            // Store references
            categoryPanels[categoryName] = contentPanel;
            categoryButtons[categoryName] = tabButton;

            // Setup button click
            string categoryKey = categoryName;
            tabButton.onClick.AddListener(() => SetActiveCategory(categoryKey));

            // Initially hide panel
            contentPanel.SetActive(false);
        }

        /// <summary>
        /// Create a parameter UI element.
        /// </summary>
        private void CreateParameterUI(TuneParameter param, Transform parent)
        {
            if (parameterUIPrefab != null)
            {
                PhysicsParameterUI paramUI = Instantiate(parameterUIPrefab, parent);
                paramUI.Initialize(param, tuningManager);
            }
        }

        /// <summary>
        /// Set the active category and show its panel.
        /// </summary>
        public void SetActiveCategory(string categoryName)
        {
            if (!categoryPanels.ContainsKey(categoryName))
                return;

            // Hide all panels
            foreach (var panel in categoryPanels.Values)
            {
                panel.SetActive(false);
            }

            // Deselect all buttons
            foreach (var button in categoryButtons.Values)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(0.8f, 0.8f, 0.8f);
                button.colors = colors;
            }

            // Activate selected panel
            categoryPanels[categoryName].SetActive(true);

            // Highlight selected button
            if (categoryButtons.ContainsKey(categoryName))
            {
                ColorBlock selectedColors = categoryButtons[categoryName].colors;
                selectedColors.normalColor = new Color(0.2f, 0.5f, 1f);
                categoryButtons[categoryName].colors = selectedColors;
            }

            currentActiveCategory = categoryName;
        }

        /// <summary>
        /// Get the currently active category.
        /// </summary>
        public string GetActiveCategory() => currentActiveCategory;

        /// <summary>
        /// Get all available categories.
        /// </summary>
        public List<string> GetAllCategories() => new List<string>(categoryPanels.Keys);
    }
}
