using UnityEngine;
using System.Collections.Generic;
using SendIt.Tuning;

namespace SendIt.Physics
{
    /// <summary>
    /// Advanced vehicle damage simulation system.
    /// Tracks impact damage, mechanical wear, and performance degradation with visual feedback.
    /// </summary>
    public class VehicleDamageSystem : MonoBehaviour
    {
        /// <summary>
        /// Damage state of a specific vehicle component.
        /// </summary>
        public struct ComponentDamage
        {
            public string ComponentName;
            public float DamageAmount;              // 0-1, where 1 is destroyed
            public float MaxDamageThreshold;        // Damage amount before failure
            public bool IsFunctional;               // Can component operate
            public float PerformanceMultiplier;     // Impact on vehicle performance
            public System.DateTime LastImpactTime;
            public int ImpactCount;
        }

        /// <summary>
        /// Impact event data.
        /// </summary>
        public struct ImpactEvent
        {
            public Vector3 Position;
            public Vector3 Normal;
            public float ImpactForce;               // Force magnitude (Newtons)
            public float ImpactSpeed;               // Speed at impact (m/s)
            public string ComponentHit;
            public System.DateTime TimeOfImpact;
        }

        [SerializeField] private VehicleController vehicleController;
        [SerializeField] private TuningManager tuningManager;
        [SerializeField] private Rigidbody vehicleRigidbody;

        // Damage components
        private Dictionary<string, ComponentDamage> componentDamage = new Dictionary<string, ComponentDamage>();
        private List<ImpactEvent> recentImpacts = new List<ImpactEvent>();

        // Damage thresholds
        private float criticalImpactForce = 10000f;  // Force threshold for significant damage
        private float damageAccumulationRate = 0.05f; // Damage per unit force

        // Visual damage representation
        private Dictionary<string, Material> damageableMaterials = new Dictionary<string, Material>();
        private float overallDamageLevel;

        private const int maxRecentImpacts = 50;

        // Damage progression
        private float totalRepairCost;
        private float totalDamagePoints;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (vehicleController == null)
                vehicleController = FindObjectOfType<VehicleController>();

            if (tuningManager == null)
                tuningManager = TuningManager.Instance;

            if (vehicleRigidbody == null)
                vehicleRigidbody = GetComponent<Rigidbody>();

            InitializeComponents();
            SetupDamageableMaterials();
        }

        /// <summary>
        /// Initialize all vehicle components with damage tracking.
        /// </summary>
        private void InitializeComponents()
        {
            // Engine components
            componentDamage["Engine"] = new ComponentDamage
            {
                ComponentName = "Engine",
                DamageAmount = 0f,
                MaxDamageThreshold = 1.0f,
                IsFunctional = true,
                PerformanceMultiplier = 1.0f,
                ImpactCount = 0
            };

            // Suspension components (4 wheels)
            for (int i = 0; i < 4; i++)
            {
                string wheelName = $"Wheel_{i}";
                componentDamage[wheelName] = new ComponentDamage
                {
                    ComponentName = wheelName,
                    DamageAmount = 0f,
                    MaxDamageThreshold = 1.0f,
                    IsFunctional = true,
                    PerformanceMultiplier = 1.0f,
                    ImpactCount = 0
                };

                string suspensionName = $"Suspension_{i}";
                componentDamage[suspensionName] = new ComponentDamage
                {
                    ComponentName = suspensionName,
                    DamageAmount = 0f,
                    MaxDamageThreshold = 0.8f,
                    IsFunctional = true,
                    PerformanceMultiplier = 1.0f,
                    ImpactCount = 0
                };
            }

            // Bodywork components
            componentDamage["FrontBumper"] = new ComponentDamage
            {
                ComponentName = "FrontBumper",
                DamageAmount = 0f,
                MaxDamageThreshold = 0.6f,
                IsFunctional = true,
                PerformanceMultiplier = 1.0f,
                ImpactCount = 0
            };

            componentDamage["RearBumper"] = new ComponentDamage
            {
                ComponentName = "RearBumper",
                DamageAmount = 0f,
                MaxDamageThreshold = 0.6f,
                IsFunctional = true,
                PerformanceMultiplier = 1.0f,
                ImpactCount = 0
            };

            // Aerodynamic components
            componentDamage["FrontWing"] = new ComponentDamage
            {
                ComponentName = "FrontWing",
                DamageAmount = 0f,
                MaxDamageThreshold = 0.7f,
                IsFunctional = true,
                PerformanceMultiplier = 1.0f,
                ImpactCount = 0
            };

            componentDamage["RearWing"] = new ComponentDamage
            {
                ComponentName = "RearWing",
                DamageAmount = 0f,
                MaxDamageThreshold = 0.7f,
                IsFunctional = true,
                PerformanceMultiplier = 1.0f,
                ImpactCount = 0
            };

            // Transmission
            componentDamage["Transmission"] = new ComponentDamage
            {
                ComponentName = "Transmission",
                DamageAmount = 0f,
                MaxDamageThreshold = 0.9f,
                IsFunctional = true,
                PerformanceMultiplier = 1.0f,
                ImpactCount = 0
            };

            // Brakes
            componentDamage["BrakeSystem"] = new ComponentDamage
            {
                ComponentName = "BrakeSystem",
                DamageAmount = 0f,
                MaxDamageThreshold = 0.8f,
                IsFunctional = true,
                PerformanceMultiplier = 1.0f,
                ImpactCount = 0
            };
        }

        /// <summary>
        /// Register collision impact and apply damage.
        /// </summary>
        public void RegisterCollisionImpact(Collision collision)
        {
            if (collision == null)
                return;

            float impactForce = collision.relativeVelocity.magnitude * vehicleRigidbody.mass;
            float impactSpeed = collision.relativeVelocity.magnitude;

            // Only register significant impacts
            if (impactSpeed < 1.0f)
                return;

            // Determine which component was hit based on collision point
            string componentHit = DetermineComponentHit(collision.contacts[0].point);

            var impactEvent = new ImpactEvent
            {
                Position = collision.contacts[0].point,
                Normal = collision.contacts[0].normal,
                ImpactForce = impactForce,
                ImpactSpeed = impactSpeed,
                ComponentHit = componentHit,
                TimeOfImpact = System.DateTime.Now
            };

            recentImpacts.Add(impactEvent);
            if (recentImpacts.Count > maxRecentImpacts)
                recentImpacts.RemoveAt(0);

            ApplyDamageFromImpact(impactEvent);
        }

        /// <summary>
        /// Apply damage to affected components based on impact.
        /// </summary>
        private void ApplyDamageFromImpact(ImpactEvent impact)
        {
            float damageFactor = impact.ImpactSpeed / 10.0f; // Normalize by 10 m/s

            // Primary component takes full damage
            if (componentDamage.ContainsKey(impact.ComponentHit))
            {
                ApplySingleComponentDamage(impact.ComponentHit, damageFactor);
            }

            // Secondary components take reduced damage based on proximity
            ApplySecondarydamage(impact.Position, damageFactor * 0.5f);

            // Update overall damage
            UpdateOverallDamage();

            Debug.Log($"Impact on {impact.ComponentHit} - Speed: {impact.ImpactSpeed:F2} m/s, Force: {impact.ImpactForce:F0} N");
        }

        /// <summary>
        /// Apply damage to a single component.
        /// </summary>
        private void ApplySingleComponentDamage(string componentName, float damageAmount)
        {
            if (!componentDamage.ContainsKey(componentName))
                return;

            var damage = componentDamage[componentName];
            damage.DamageAmount = Mathf.Min(damage.DamageAmount + damageAmount, damage.MaxDamageThreshold);
            damage.LastImpactTime = System.DateTime.Now;
            damage.ImpactCount++;

            // Check if component is destroyed
            if (damage.DamageAmount >= damage.MaxDamageThreshold)
            {
                damage.IsFunctional = false;
                damage.PerformanceMultiplier = 0.0f;
                ApplyComponentFailure(componentName);
            }
            else
            {
                // Calculate performance degradation based on damage
                damage.PerformanceMultiplier = Mathf.Max(0.3f, 1.0f - (damage.DamageAmount / damage.MaxDamageThreshold));
            }

            componentDamage[componentName] = damage;
        }

        /// <summary>
        /// Apply damage to nearby components.
        /// </summary>
        private void ApplySecondarydamage(Vector3 impactPosition, float damageAmount)
        {
            float damageRadius = 2.0f;

            foreach (var component in componentDamage)
            {
                // Estimate distance to component (would use actual transforms in production)
                float distance = Mathf.Abs(damageAmount) * 2.0f; // Simplified

                if (distance < damageRadius)
                {
                    float falloff = 1.0f - (distance / damageRadius);
                    ApplySingleComponentDamage(component.Key, damageAmount * falloff * 0.3f);
                }
            }
        }

        /// <summary>
        /// Handle component failure consequences.
        /// </summary>
        private void ApplyComponentFailure(string componentName)
        {
            switch (componentName)
            {
                case "Engine":
                    Debug.LogError("Engine destroyed! Vehicle no longer functional.");
                    vehicleController.DisableVehicle();
                    break;

                case "BrakeSystem":
                    Debug.LogWarning("Brake system failed! Braking disabled.");
                    if (tuningManager != null)
                    {
                        tuningManager.SetPhysicsParameter("BrakeForce", 0f);
                    }
                    break;

                case "Transmission":
                    Debug.LogWarning("Transmission failed! Gear changes disabled.");
                    vehicleController.LockGear();
                    break;

                case string s when s.StartsWith("Wheel_"):
                    Debug.LogWarning($"{componentName} destroyed! Handling severely affected.");
                    break;

                case string s when s.StartsWith("Suspension_"):
                    Debug.LogWarning($"{componentName} failed! Suspension compromised.");
                    break;
            }
        }

        /// <summary>
        /// Determine which component was hit based on collision position.
        /// </summary>
        private string DetermineComponentHit(Vector3 collisionPoint)
        {
            Vector3 relativePos = vehicleRigidbody.transform.InverseTransformPoint(collisionPoint);

            // Front impacts
            if (relativePos.z > 1.5f)
            {
                return relativePos.y > 0.3f ? "FrontWing" : "FrontBumper";
            }

            // Rear impacts
            if (relativePos.z < -1.5f)
            {
                return relativePos.y > 0.3f ? "RearWing" : "RearBumper";
            }

            // Side impacts - determine which wheel/suspension
            int wheelIndex = relativePos.x > 0 ? 0 : 1; // Left or right
            wheelIndex += relativePos.z > 0 ? 0 : 2;     // Front or rear
            return $"Wheel_{wheelIndex}";
        }

        /// <summary>
        /// Update overall vehicle damage level and apply performance penalties.
        /// </summary>
        private void UpdateOverallDamage()
        {
            float totalDamage = 0f;
            int componentCount = 0;

            foreach (var component in componentDamage.Values)
            {
                totalDamage += component.DamageAmount;
                componentCount++;
            }

            overallDamageLevel = componentCount > 0 ? totalDamage / componentCount : 0f;

            // Apply performance penalties
            ApplyDamagePerformancePenalties();
        }

        /// <summary>
        /// Apply performance penalties based on damage.
        /// </summary>
        private void ApplyDamagePerformancePenalties()
        {
            if (tuningManager == null)
                return;

            // Engine damage reduces horsepower
            if (componentDamage.ContainsKey("Engine"))
            {
                float engineDamage = componentDamage["Engine"].DamageAmount;
                float horsepowerReduction = engineDamage * 0.5f; // Max 50% reduction
                var hpParam = tuningManager.GetPhysicsParameter("HorsePower");
                if (hpParam != null)
                {
                    tuningManager.SetPhysicsParameter("HorsePower",
                        hpParam.CurrentValue * (1.0f - horsepowerReduction));
                }
            }

            // Suspension damage increases drag
            float suspensionDamage = 0f;
            int suspensionCount = 0;
            for (int i = 0; i < 4; i++)
            {
                if (componentDamage.ContainsKey($"Suspension_{i}"))
                {
                    suspensionDamage += componentDamage[$"Suspension_{i}"].DamageAmount;
                    suspensionCount++;
                }
            }
            suspensionDamage /= Mathf.Max(suspensionCount, 1);

            var dragParam = tuningManager.GetPhysicsParameter("DragCoefficient");
            if (dragParam != null)
            {
                float dragIncrease = suspensionDamage * 0.3f; // Max 30% increase
                tuningManager.SetPhysicsParameter("DragCoefficient",
                    dragParam.CurrentValue * (1.0f + dragIncrease));
            }

            // Aero damage reduces downforce
            float aeroDamage = (componentDamage.ContainsKey("FrontWing") ? componentDamage["FrontWing"].DamageAmount : 0f) +
                               (componentDamage.ContainsKey("RearWing") ? componentDamage["RearWing"].DamageAmount : 0f);
            aeroDamage /= 2f;

            var downforceParam = tuningManager.GetPhysicsParameter("DownforceCoefficient");
            if (downforceParam != null)
            {
                float downforceReduction = aeroDamage * 0.6f; // Max 60% reduction
                tuningManager.SetPhysicsParameter("DownforceCoefficient",
                    downforceParam.CurrentValue * (1.0f - downforceReduction));
            }

            // Brake damage reduces brake force
            if (componentDamage.ContainsKey("BrakeSystem"))
            {
                float brakeDamage = componentDamage["BrakeSystem"].DamageAmount;
                var brakeParam = tuningManager.GetPhysicsParameter("BrakeForce");
                if (brakeParam != null)
                {
                    float brakeReduction = brakeDamage * 0.7f; // Max 70% reduction
                    tuningManager.SetPhysicsParameter("BrakeForce",
                        brakeParam.CurrentValue * (1.0f - brakeReduction));
                }
            }
        }

        /// <summary>
        /// Repair a damaged component.
        /// </summary>
        public float RepairComponent(string componentName, float repairAmount = 1.0f)
        {
            if (!componentDamage.ContainsKey(componentName))
                return 0f;

            var damage = componentDamage[componentName];
            float originalDamage = damage.DamageAmount;
            damage.DamageAmount = Mathf.Max(0f, damage.DamageAmount - repairAmount);

            if (damage.DamageAmount == 0f)
            {
                damage.IsFunctional = true;
                damage.PerformanceMultiplier = 1.0f;
            }

            componentDamage[componentName] = damage;

            float repairCost = originalDamage * 1000f; // Cost per damage point
            totalRepairCost += repairCost;

            UpdateOverallDamage();

            Debug.Log($"Repaired {componentName}. Cost: ${repairCost:F2}");
            return repairCost;
        }

        /// <summary>
        /// Repair all damage.
        /// </summary>
        public float FullRepair()
        {
            float totalCost = 0f;

            foreach (var componentName in new List<string>(componentDamage.Keys))
            {
                totalCost += RepairComponent(componentName, 1.0f);
            }

            Debug.Log($"Full repair completed. Total cost: ${totalCost:F2}");
            return totalCost;
        }

        /// <summary>
        /// Get damage level of a component (0-1).
        /// </summary>
        public float GetComponentDamage(string componentName)
        {
            return componentDamage.ContainsKey(componentName) ?
                componentDamage[componentName].DamageAmount : 0f;
        }

        /// <summary>
        /// Get overall vehicle damage level (0-1).
        /// </summary>
        public float GetOverallDamage() => overallDamageLevel;

        /// <summary>
        /// Get all component damage states.
        /// </summary>
        public Dictionary<string, ComponentDamage> GetAllComponentDamage() =>
            new Dictionary<string, ComponentDamage>(componentDamage);

        /// <summary>
        /// Get recent impacts.
        /// </summary>
        public List<ImpactEvent> GetRecentImpacts() =>
            new List<ImpactEvent>(recentImpacts);

        /// <summary>
        /// Check if component is functional.
        /// </summary>
        public bool IsComponentFunctional(string componentName) =>
            componentDamage.ContainsKey(componentName) &&
            componentDamage[componentName].IsFunctional;

        /// <summary>
        /// Get damage repair cost.
        /// </summary>
        public float GetTotalRepairCost() => totalRepairCost;

        private void SetupDamageableMaterials()
        {
            // Cache materials for damage visualization
            // In production, would iterate through all renderers
        }
    }
}
