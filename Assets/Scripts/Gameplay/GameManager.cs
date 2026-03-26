using UnityEngine;
using SendIt.Physics;
using SendIt.Tuning;
using SendIt.Data;

namespace SendIt.Gameplay
{
    /// <summary>
    /// Main game manager that orchestrates all systems.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameObject vehiclePrefab;
        [SerializeField] private bool autoLoadDefaultVehicle = true;

        private VehicleController vehicleController;
        private TuningManager tuningManager;
        private GameObject currentVehicle;

        private static GameManager instance;

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
        /// Initialize the game with a vehicle and all systems.
        /// </summary>
        public void Initialize()
        {
            // Create tuning manager if it doesn't exist
            if (tuningManager == null)
            {
                GameObject tuningManagerGO = new GameObject("TuningManager");
                tuningManager = tuningManagerGO.AddComponent<TuningManager>();
            }

            // Load or create vehicle
            if (autoLoadDefaultVehicle)
            {
                LoadDefaultVehicle();
            }
        }

        /// <summary>
        /// Load the default vehicle or create a new one.
        /// </summary>
        private void LoadDefaultVehicle()
        {
            // Check if a saved vehicle exists
            string[] savedVehicles = SaveManager.GetSavedVehicles();

            if (savedVehicles.Length > 0)
            {
                LoadVehicle(savedVehicles[0]);
            }
            else
            {
                CreateNewVehicle();
            }
        }

        /// <summary>
        /// Create a new vehicle with default settings.
        /// </summary>
        public void CreateNewVehicle()
        {
            if (vehiclePrefab == null)
            {
                Debug.LogError("Vehicle prefab not assigned to GameManager!");
                return;
            }

            // Destroy old vehicle if exists
            if (currentVehicle != null)
            {
                Destroy(currentVehicle);
            }

            // Instantiate new vehicle
            currentVehicle = Instantiate(vehiclePrefab, Vector3.zero, Quaternion.identity);
            currentVehicle.name = "Vehicle";

            // Get vehicle controller
            vehicleController = currentVehicle.GetComponent<VehicleController>();
            if (vehicleController == null)
            {
                vehicleController = currentVehicle.AddComponent<VehicleController>();
            }

            // Initialize vehicle with tuning manager
            vehicleController.Initialize();
        }

        /// <summary>
        /// Load a vehicle from saved configuration.
        /// </summary>
        public void LoadVehicle(string vehicleName)
        {
            VehicleData vehicleData = SaveManager.LoadVehicle(vehicleName);
            tuningManager.SetVehicleData(vehicleData);
            CreateNewVehicle();
        }

        /// <summary>
        /// Save the current vehicle configuration.
        /// </summary>
        public void SaveCurrentVehicle(string vehicleName)
        {
            if (tuningManager == null)
            {
                Debug.LogError("TuningManager not initialized!");
                return;
            }

            VehicleData data = tuningManager.GetVehicleData();
            data.SetVehicleName(vehicleName);
            SaveManager.SaveVehicle(data, vehicleName);
        }

        // Getters
        public VehicleController GetVehicleController() => vehicleController;
        public TuningManager GetTuningManager() => tuningManager;
        public GameObject GetCurrentVehicle() => currentVehicle;
        public static GameManager Instance => instance;
    }
}
