using UnityEngine;
using System.Collections.Generic;

namespace SendIt.Performance
{
    /// <summary>
    /// Object pooling system for effects and temporary objects.
    /// Reduces garbage collection by reusing objects instead of creating/destroying.
    /// </summary>
    public class EffectsObjectPool : MonoBehaviour
    {
        [System.Serializable]
        public class PooledObject
        {
            public string Name;
            public GameObject Prefab;
            public int InitialPoolSize = 50;
            public Queue<GameObject> AvailableObjects;
            public List<GameObject> ActiveObjects;
        }

        [SerializeField] private PooledObject[] poolConfigs;

        private Dictionary<string, PooledObject> pools = new Dictionary<string, PooledObject>();
        private Transform poolContainer;

        public static EffectsObjectPool Instance { get; private set; }

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
        /// Initialize all object pools.
        /// </summary>
        public void Initialize()
        {
            // Create container for pooled objects
            GameObject container = new GameObject("PooledObjectsContainer");
            container.transform.SetParent(transform);
            poolContainer = container.transform;

            // Initialize configured pools
            if (poolConfigs != null)
            {
                foreach (PooledObject config in poolConfigs)
                {
                    CreatePool(config.Name, config.Prefab, config.InitialPoolSize);
                }
            }

            Debug.Log($"ObjectPool initialized with {pools.Count} pools");
        }

        /// <summary>
        /// Create a new object pool.
        /// </summary>
        public void CreatePool(string poolName, GameObject prefab, int initialSize)
        {
            if (pools.ContainsKey(poolName))
            {
                Debug.LogWarning($"Pool '{poolName}' already exists");
                return;
            }

            PooledObject pool = new PooledObject
            {
                Name = poolName,
                Prefab = prefab,
                AvailableObjects = new Queue<GameObject>(initialSize),
                ActiveObjects = new List<GameObject>(initialSize)
            };

            // Pre-allocate pool
            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = Instantiate(prefab, poolContainer);
                obj.SetActive(false);
                pool.AvailableObjects.Enqueue(obj);
            }

            pools[poolName] = pool;
            Debug.Log($"Created pool '{poolName}' with {initialSize} objects");
        }

        /// <summary>
        /// Get an object from the pool.
        /// </summary>
        public GameObject GetObject(string poolName)
        {
            if (!pools.ContainsKey(poolName))
            {
                Debug.LogError($"Pool '{poolName}' not found");
                return null;
            }

            PooledObject pool = pools[poolName];
            GameObject obj;

            if (pool.AvailableObjects.Count > 0)
            {
                obj = pool.AvailableObjects.Dequeue();
            }
            else
            {
                // Expand pool if needed
                obj = Instantiate(pool.Prefab, poolContainer);
            }

            obj.SetActive(true);
            pool.ActiveObjects.Add(obj);

            return obj;
        }

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        public void ReturnObject(string poolName, GameObject obj)
        {
            if (!pools.ContainsKey(poolName))
            {
                Debug.LogError($"Pool '{poolName}' not found");
                Destroy(obj);
                return;
            }

            PooledObject pool = pools[poolName];
            obj.SetActive(false);
            pool.ActiveObjects.Remove(obj);
            pool.AvailableObjects.Enqueue(obj);
        }

        /// <summary>
        /// Clear a pool, destroying all objects.
        /// </summary>
        public void ClearPool(string poolName)
        {
            if (!pools.ContainsKey(poolName))
                return;

            PooledObject pool = pools[poolName];

            foreach (GameObject obj in pool.AvailableObjects)
                Destroy(obj);

            foreach (GameObject obj in pool.ActiveObjects)
                Destroy(obj);

            pool.AvailableObjects.Clear();
            pool.ActiveObjects.Clear();

            Debug.Log($"Cleared pool '{poolName}'");
        }

        /// <summary>
        /// Get pool statistics.
        /// </summary>
        public string GetPoolStats(string poolName)
        {
            if (!pools.ContainsKey(poolName))
                return "";

            PooledObject pool = pools[poolName];
            return $"{poolName}: {pool.ActiveObjects.Count} active, {pool.AvailableObjects.Count} available";
        }

        /// <summary>
        /// Get all pool statistics.
        /// </summary>
        public string GetAllPoolStats()
        {
            string stats = "\n=== OBJECT POOLS ===\n";
            foreach (var pool in pools.Values)
            {
                stats += $"{pool.Name}: {pool.ActiveObjects.Count} active, {pool.AvailableObjects.Count} available\n";
            }
            return stats;
        }

        /// <summary>
        /// Trim pools to reduce memory usage.
        /// </summary>
        public void TrimPools(int maxAvailable = 50)
        {
            foreach (PooledObject pool in pools.Values)
            {
                while (pool.AvailableObjects.Count > maxAvailable)
                {
                    GameObject obj = pool.AvailableObjects.Dequeue();
                    Destroy(obj);
                }
            }

            Debug.Log("Trimmed all pools");
        }

        /// <summary>
        /// Get available object count in a pool.
        /// </summary>
        public int GetAvailableCount(string poolName)
        {
            if (!pools.ContainsKey(poolName))
                return 0;
            return pools[poolName].AvailableObjects.Count;
        }

        /// <summary>
        /// Get active object count in a pool.
        /// </summary>
        public int GetActiveCount(string poolName)
        {
            if (!pools.ContainsKey(poolName))
                return 0;
            return pools[poolName].ActiveObjects.Count;
        }
    }
}
