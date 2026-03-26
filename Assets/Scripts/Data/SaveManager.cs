using UnityEngine;
using System.IO;
using SendIt.Data;

namespace SendIt.Data
{
    /// <summary>
    /// Handles saving and loading vehicle configurations to disk.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private static string savePath = "";

        private static void InitializeSavePath()
        {
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = Path.Combine(Application.persistentDataPath, "Vehicles");
                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }
            }
        }

        /// <summary>
        /// Save a vehicle configuration to disk.
        /// </summary>
        public static void SaveVehicle(VehicleData vehicleData, string fileName)
        {
            InitializeSavePath();

            string jsonData = JsonUtility.ToJson(vehicleData, true);
            string filePath = Path.Combine(savePath, fileName + ".json");

            try
            {
                File.WriteAllText(filePath, jsonData);
                Debug.Log($"Vehicle saved: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save vehicle: {e.Message}");
            }
        }

        /// <summary>
        /// Load a vehicle configuration from disk.
        /// </summary>
        public static VehicleData LoadVehicle(string fileName)
        {
            InitializeSavePath();

            string filePath = Path.Combine(savePath, fileName + ".json");

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"Vehicle file not found: {filePath}");
                return new VehicleData();
            }

            try
            {
                string jsonData = File.ReadAllText(filePath);
                VehicleData vehicleData = JsonUtility.FromJson<VehicleData>(jsonData);
                Debug.Log($"Vehicle loaded: {filePath}");
                return vehicleData;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load vehicle: {e.Message}");
                return new VehicleData();
            }
        }

        /// <summary>
        /// Get all saved vehicle file names.
        /// </summary>
        public static string[] GetSavedVehicles()
        {
            InitializeSavePath();

            DirectoryInfo dir = new DirectoryInfo(savePath);
            FileInfo[] files = dir.GetFiles("*.json");

            string[] vehicleNames = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                vehicleNames[i] = Path.GetFileNameWithoutExtension(files[i].Name);
            }

            return vehicleNames;
        }

        /// <summary>
        /// Delete a saved vehicle configuration.
        /// </summary>
        public static void DeleteVehicle(string fileName)
        {
            InitializeSavePath();

            string filePath = Path.Combine(savePath, fileName + ".json");

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"Vehicle deleted: {filePath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete vehicle: {e.Message}");
            }
        }
    }
}
