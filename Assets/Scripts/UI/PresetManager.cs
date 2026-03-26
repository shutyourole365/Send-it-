using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SendIt.Data;
using SendIt.Tuning;

namespace SendIt.UI
{
    /// <summary>
    /// Manages preset/setup saving and loading.
    /// Allows players to create, load, and manage vehicle configurations.
    /// </summary>
    public class PresetManager : MonoBehaviour
    {
        [SerializeField] private InputField presetNameInput;
        [SerializeField] private Button savePresetButton;
        [SerializeField] private Dropdown loadPresetDropdown;
        [SerializeField] private Button deletePresetButton;
        [SerializeField] private Button refreshButton;

        [SerializeField] private Text statusLabel;

        private TuningManager tuningManager;
        private List<string> currentPresets = new List<string>();

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the preset manager.
        /// </summary>
        private void Initialize()
        {
            tuningManager = TuningManager.Instance;
            if (tuningManager == null)
            {
                Debug.LogError("TuningManager not found!");
                return;
            }

            // Setup button listeners
            if (savePresetButton != null)
                savePresetButton.onClick.AddListener(SaveCurrentPreset);

            if (deletePresetButton != null)
                deletePresetButton.onClick.AddListener(DeleteCurrentPreset);

            if (refreshButton != null)
                refreshButton.onClick.AddListener(RefreshPresetList);

            if (loadPresetDropdown != null)
                loadPresetDropdown.onValueChanged.AddListener(OnPresetSelected);

            // Initial load of presets
            RefreshPresetList();
        }

        /// <summary>
        /// Save the current vehicle setup as a preset.
        /// </summary>
        private void SaveCurrentPreset()
        {
            if (tuningManager == null)
                return;

            string presetName = presetNameInput.text.Trim();
            if (string.IsNullOrEmpty(presetName))
            {
                ShowStatus("Please enter a preset name", Color.red);
                return;
            }

            VehicleData vehicleData = tuningManager.GetVehicleData();
            vehicleData.SetVehicleName(presetName);

            SaveManager.SaveVehicle(vehicleData, presetName);
            ShowStatus($"Preset '{presetName}' saved successfully!", Color.green);

            // Clear input and refresh list
            presetNameInput.text = "";
            RefreshPresetList();
        }

        /// <summary>
        /// Load a preset when selected from dropdown.
        /// </summary>
        private void OnPresetSelected(int index)
        {
            if (index < 0 || index >= currentPresets.Count)
                return;

            string presetName = currentPresets[index];
            LoadPreset(presetName);
        }

        /// <summary>
        /// Load a vehicle setup by name.
        /// </summary>
        public void LoadPreset(string presetName)
        {
            if (tuningManager == null)
                return;

            VehicleData vehicleData = SaveManager.LoadVehicle(presetName);
            tuningManager.SetVehicleData(vehicleData);

            ShowStatus($"Loaded preset '{presetName}'", Color.green);
            RefreshParameterUI();
        }

        /// <summary>
        /// Delete the currently selected preset.
        /// </summary>
        private void DeleteCurrentPreset()
        {
            int selectedIndex = loadPresetDropdown.value;
            if (selectedIndex < 0 || selectedIndex >= currentPresets.Count)
            {
                ShowStatus("No preset selected", Color.red);
                return;
            }

            string presetName = currentPresets[selectedIndex];
            if (ConfirmDelete(presetName))
            {
                SaveManager.DeleteVehicle(presetName);
                ShowStatus($"Preset '{presetName}' deleted", Color.yellow);
                RefreshPresetList();
            }
        }

        /// <summary>
        /// Refresh the preset list dropdown.
        /// </summary>
        private void RefreshPresetList()
        {
            currentPresets.Clear();
            string[] presets = SaveManager.GetSavedVehicles();
            currentPresets.AddRange(presets);

            if (loadPresetDropdown != null)
            {
                loadPresetDropdown.options.Clear();
                foreach (string preset in presets)
                {
                    loadPresetDropdown.options.Add(new Dropdown.OptionData(preset));
                }

                if (presets.Length > 0)
                {
                    loadPresetDropdown.value = 0;
                }
            }

            ShowStatus($"{presets.Length} presets available", Color.white);
        }

        /// <summary>
        /// Show status message to user.
        /// </summary>
        private void ShowStatus(string message, Color color)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
                statusLabel.color = color;
            }
            Debug.Log(message);
        }

        /// <summary>
        /// Confirm deletion dialog.
        /// </summary>
        private bool ConfirmDelete(string presetName)
        {
            // Simple confirmation - in a real game, show a dialog
            return true;
        }

        /// <summary>
        /// Refresh parameter UI after loading preset.
        /// </summary>
        private void RefreshParameterUI()
        {
            // Notify TuningUIManager to refresh all parameter displays
            TuningUIManager uiManager = TuningUIManager.Instance;
            if (uiManager != null)
            {
                uiManager.RefreshAllParameterUIs();
            }
        }
    }
}
