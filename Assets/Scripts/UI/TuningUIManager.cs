using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SendIt.Tuning;
using SendIt.Data;
using SendIt.Physics;

namespace SendIt.UI
{
    /// <summary>
    /// Main UI manager for the tuning garage.
    /// Orchestrates all UI panels and handles player interaction.
    /// </summary>
    public class TuningUIManager : MonoBehaviour
    {
        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private TuningManager tuningManager;

        // UI References
        [SerializeField] private Transform physicsTabsContainer;
        [SerializeField] private Transform telemetryPanel;
        [SerializeField] private Transform presetPanel;
        [SerializeField] private Transform saveLoadPanel;

        // Prefabs
        [SerializeField] private PhysicsParameterUI parameterUIPrefab;
        [SerializeField] private TelemetryDisplay telemetryDisplayPrefab;

        // Ui state
        private Dictionary<string, PhysicsParameterUI> activeParameterUIs = new Dictionary<string, PhysicsParameterUI>();
        private TelemetryDisplay telemetryDisplay;
        private string currentVehicleName = "My Vehicle";

        private static TuningUIManager instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the tuning UI.
        /// </summary>
        private void Initialize()
        {
            if (tuningManager == null)
            {
                tuningManager = TuningManager.Instance;
            }

            if (tuningManager == null)
            {
                Debug.LogError("TuningManager not found!");
                return;
            }

            // Create physics parameter UI controls
            CreatePhysicsParameterUIs();

            // Create telemetry display
            CreateTelemetryDisplay();

            // Create preset UI
            CreatePresetUI();

            // Subscribe to parameter changes
            tuningManager.OnPhysicsParameterChanged += HandleParameterChanged;
        }

        /// <summary>
        /// Create UI controls for all physics parameters.
        /// </summary>
        private void CreatePhysicsParameterUIs()
        {
            if (physicsTabsContainer == null)
            {
                Debug.LogWarning("Physics tabs container not assigned!");
                return;
            }

            var physicsParams = tuningManager.GetAllPhysicsParameters();

            // Group parameters by category
            Dictionary<string, List<TuneParameter>> categorizedParams = new Dictionary<string, List<TuneParameter>>();

            foreach (var param in physicsParams.Values)
            {
                if (!categorizedParams.ContainsKey(param.Category))
                {
                    categorizedParams[param.Category] = new List<TuneParameter>();
                }
                categorizedParams[param.Category].Add(param);
            }

            // Create a panel for each category
            foreach (var category in categorizedParams)
            {
                GameObject categoryPanel = new GameObject(category.Key + " Panel");
                categoryPanel.transform.SetParent(physicsTabsContainer, false);
                LayoutGroup layout = categoryPanel.AddComponent<VerticalLayoutGroup>();
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;

                // Create parameter controls for this category
                foreach (var param in category.Value)
                {
                    CreateParameterUI(param, categoryPanel.transform);
                }
            }
        }

        /// <summary>
        /// Create a UI control for a single parameter.
        /// </summary>
        private void CreateParameterUI(TuneParameter param, Transform parent)
        {
            if (parameterUIPrefab != null)
            {
                PhysicsParameterUI paramUI = Instantiate(parameterUIPrefab, parent);
                paramUI.Initialize(param, tuningManager);
                activeParameterUIs[param.ParameterName] = paramUI;
            }
        }

        /// <summary>
        /// Create telemetry display panel.
        /// </summary>
        private void CreateTelemetryDisplay()
        {
            if (telemetryPanel == null || telemetryDisplayPrefab == null)
                return;

            telemetryDisplay = Instantiate(telemetryDisplayPrefab, telemetryPanel);
            if (vehicleController != null && vehicleController.GetTelemetry() != null)
            {
                telemetryDisplay.Initialize(vehicleController.GetTelemetry());
            }
        }

        /// <summary>
        /// Create preset/save-load UI.
        /// </summary>
        private void CreatePresetUI()
        {
            if (presetPanel == null)
                return;

            // Create preset save button
            Button saveButton = new GameObject("SaveButton").AddComponent<Button>();
            saveButton.transform.SetParent(presetPanel);
            Text saveText = saveButton.gameObject.AddComponent<Text>();
            saveText.text = "Save Setup";
            saveButton.onClick.AddListener(() => SaveCurrentSetup());

            // Create preset load dropdown
            Dropdown loadDropdown = new GameObject("LoadDropdown").AddComponent<Dropdown>();
            loadDropdown.transform.SetParent(presetPanel);
            RefreshPresetList(loadDropdown);
        }

        /// <summary>
        /// Save current vehicle tuning setup.
        /// </summary>
        public void SaveCurrentSetup()
        {
            string vehicleName = currentVehicleName;
            if (tuningManager != null)
            {
                SaveManager.SaveVehicle(tuningManager.GetVehicleData(), vehicleName);
                Debug.Log($"Vehicle setup saved: {vehicleName}");
            }
        }

        /// <summary>
        /// Load a vehicle setup by name.
        /// </summary>
        public void LoadSetup(string vehicleName)
        {
            VehicleData vehicleData = SaveManager.LoadVehicle(vehicleName);
            if (tuningManager != null)
            {
                tuningManager.SetVehicleData(vehicleData);
                RefreshAllParameterUIs();
                Debug.Log($"Vehicle setup loaded: {vehicleName}");
            }
        }

        /// <summary>
        /// Refresh the preset list dropdown.
        /// </summary>
        private void RefreshPresetList(Dropdown dropdown)
        {
            string[] presets = SaveManager.GetSavedVehicles();
            dropdown.options.Clear();
            foreach (string preset in presets)
            {
                dropdown.options.Add(new Dropdown.OptionData(preset));
            }
        }

        /// <summary>
        /// Refresh all parameter UI controls to match current values.
        /// </summary>
        private void RefreshAllParameterUIs()
        {
            foreach (var paramUI in activeParameterUIs.Values)
            {
                paramUI.RefreshDisplay();
            }
        }

        /// <summary>
        /// Reset all parameters to default values.
        /// </summary>
        public void ResetAllParameters()
        {
            if (tuningManager != null)
            {
                tuningManager.ResetAllParameters();
                RefreshAllParameterUIs();
                Debug.Log("All parameters reset to default");
            }
        }

        /// <summary>
        /// Reset a specific category of parameters.
        /// </summary>
        public void ResetParameterCategory(string category)
        {
            if (tuningManager != null)
            {
                tuningManager.ResetParameterCategory(category);
                RefreshAllParameterUIs();
                Debug.Log($"Category '{category}' reset to default");
            }
        }

        /// <summary>
        /// Handle parameter value changes from UI.
        /// </summary>
        private void HandleParameterChanged(string paramName, float newValue)
        {
            // Telemetry will update automatically through vehicle controller
            Debug.Log($"Parameter changed: {paramName} = {newValue}");
        }

        /// <summary>
        /// Update telemetry display every frame.
        /// </summary>
        private void Update()
        {
            if (telemetryDisplay != null && vehicleController != null)
            {
                var telemetry = vehicleController.GetTelemetry();
                if (telemetry != null)
                {
                    telemetryDisplay.UpdateDisplay(telemetry.GetFrame());
                }
            }
        }

        public void SetCurrentVehicleName(string name) => currentVehicleName = name;
        public static TuningUIManager Instance => instance;
    }
}
