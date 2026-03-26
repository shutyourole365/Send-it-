using UnityEngine;
using System.Collections.Generic;

namespace SendIt.AI
{
    /// <summary>
    /// Manages ambient traffic and NPC vehicles on roads.
    /// Creates dynamic, living world with traffic flow.
    /// </summary>
    public class TrafficManager : MonoBehaviour
    {
        [System.Serializable]
        public class TrafficSpawner
        {
            public Vector3 SpawnPosition;
            public Vector3 SpawnDirection;
            public float SpawnInterval = 2f;
            public int MaxVehiclesPerSpawner = 5;
        }

        [SerializeField] private int targetTrafficDensity = 15; // Total vehicles to maintain
        [SerializeField] private float spawnCheckInterval = 1f;
        [SerializeField] private float despawnDistance = 200f; // Remove vehicles this far away

        private List<TrafficSpawner> trafficSpawners = new List<TrafficSpawner>();
        private List<AIVehicleController> activeTrafficVehicles = new List<AIVehicleController>();
        private VehicleController playerVehicle;

        private float timeSinceLastSpawnCheck = 0f;
        private Dictionary<TrafficSpawner, float> lastSpawnTimes = new Dictionary<TrafficSpawner, float>();

        public static TrafficManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize traffic manager.
        /// </summary>
        public void Initialize()
        {
            playerVehicle = FindObjectOfType<VehicleController>();

            // Create default traffic spawners along roads
            CreateDefaultSpawners();

            // Initialize spawn timers
            foreach (TrafficSpawner spawner in trafficSpawners)
            {
                lastSpawnTimes[spawner] = 0f;
            }

            Debug.Log($"TrafficManager initialized with {trafficSpawners.Count} spawners");
        }

        /// <summary>
        /// Create default traffic spawners for the scene.
        /// </summary>
        private void CreateDefaultSpawners()
        {
            // These would be placed at road entry points in a full implementation
            trafficSpawners.Add(new TrafficSpawner
            {
                SpawnPosition = Vector3.zero + Vector3.left * 50f,
                SpawnDirection = Vector3.right,
                SpawnInterval = 3f,
                MaxVehiclesPerSpawner = 5
            });

            trafficSpawners.Add(new TrafficSpawner
            {
                SpawnPosition = Vector3.zero + Vector3.right * 50f,
                SpawnDirection = Vector3.left,
                SpawnInterval = 3f,
                MaxVehiclesPerSpawner = 5
            });

            trafficSpawners.Add(new TrafficSpawner
            {
                SpawnPosition = Vector3.zero + Vector3.forward * 50f,
                SpawnDirection = Vector3.back,
                SpawnInterval = 3f,
                MaxVehiclesPerSpawner = 5
            });
        }

        private void Update()
        {
            if (playerVehicle == null)
                return;

            // Check for spawning new traffic
            timeSinceLastSpawnCheck += Time.deltaTime;
            if (timeSinceLastSpawnCheck >= spawnCheckInterval)
            {
                ManageTrafficSpawning();
                timeSinceLastSpawnCheck = 0f;
            }

            // Despawn distant vehicles
            DespawnDistantVehicles();
        }

        /// <summary>
        /// Manage spawning of new traffic vehicles.
        /// </summary>
        private void ManageTrafficSpawning()
        {
            // Only spawn if below target density
            if (activeTrafficVehicles.Count >= targetTrafficDensity)
                return;

            foreach (TrafficSpawner spawner in trafficSpawners)
            {
                // Check spawn interval
                if (Time.time - lastSpawnTimes[spawner] < spawner.SpawnInterval)
                    continue;

                // Check max vehicles at this spawner
                int vehiclesAtSpawner = activeTrafficVehicles.FindAll(v =>
                    Vector3.Distance(v.transform.position, spawner.SpawnPosition) < 100f).Count;

                if (vehiclesAtSpawner >= spawner.MaxVehiclesPerSpawner)
                    continue;

                // Spawn vehicle
                SpawnTrafficVehicle(spawner);
                lastSpawnTimes[spawner] = Time.time;
            }
        }

        /// <summary>
        /// Spawn a new traffic vehicle.
        /// </summary>
        private void SpawnTrafficVehicle(TrafficSpawner spawner)
        {
            GameObject trafficObj = new GameObject($"TrafficVehicle_{activeTrafficVehicles.Count}");
            trafficObj.transform.position = spawner.SpawnPosition;
            trafficObj.transform.rotation = Quaternion.LookRotation(spawner.SpawnDirection);

            // Add AI controller
            AIVehicleController aiController = trafficObj.AddComponent<AIVehicleController>();
            aiController.SetBehavior(AIVehicleController.AIBehavior.Traffic);
            aiController.SetMaxSpeed(60f + Random.Range(-10f, 20f)); // Varied speeds
            aiController.Initialize();

            activeTrafficVehicles.Add(aiController);

            Debug.Log($"Spawned traffic vehicle. Total: {activeTrafficVehicles.Count}");
        }

        /// <summary>
        /// Remove vehicles that are too far from player.
        /// </summary>
        private void DespawnDistantVehicles()
        {
            for (int i = activeTrafficVehicles.Count - 1; i >= 0; i--)
            {
                AIVehicleController vehicle = activeTrafficVehicles[i];

                if (vehicle == null)
                {
                    activeTrafficVehicles.RemoveAt(i);
                    continue;
                }

                float distance = Vector3.Distance(vehicle.transform.position, playerVehicle.transform.position);
                if (distance > despawnDistance)
                {
                    Destroy(vehicle.gameObject);
                    activeTrafficVehicles.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Set traffic density target.
        /// </summary>
        public void SetTrafficDensity(int density)
        {
            targetTrafficDensity = Mathf.Clamp(density, 0, 50);
        }

        /// <summary>
        /// Clear all traffic vehicles.
        /// </summary>
        public void ClearAllTraffic()
        {
            foreach (AIVehicleController vehicle in activeTrafficVehicles)
            {
                if (vehicle != null)
                    Destroy(vehicle.gameObject);
            }
            activeTrafficVehicles.Clear();
        }

        /// <summary>
        /// Get traffic information.
        /// </summary>
        public string GetTrafficInfo()
        {
            return $"Active Traffic: {activeTrafficVehicles.Count}/{targetTrafficDensity}\n" +
                   $"Spawners: {trafficSpawners.Count}";
        }

        /// <summary>
        /// Get list of nearby traffic vehicles.
        /// </summary>
        public List<AIVehicleController> GetNearbyTraffic(float distance)
        {
            List<AIVehicleController> nearby = new List<AIVehicleController>();
            foreach (AIVehicleController vehicle in activeTrafficVehicles)
            {
                if (Vector3.Distance(vehicle.transform.position, playerVehicle.transform.position) <= distance)
                    nearby.Add(vehicle);
            }
            return nearby;
        }
    }
}
