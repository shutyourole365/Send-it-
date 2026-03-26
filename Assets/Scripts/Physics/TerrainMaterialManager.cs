using UnityEngine;
using System.Collections.Generic;

namespace SendIt.Physics
{
    /// <summary>
    /// Manages terrain material detection and properties.
    /// Maps physics materials to terrain types for surface deformation and effects.
    /// </summary>
    public class TerrainMaterialManager : MonoBehaviour
    {
        public enum TerrainType
        {
            Road,      // Asphalt, concrete - hard, no deformation
            Grass,     // Grass, dirt - light deformation, quick recovery
            Sand,      // Sand, beach - medium deformation, medium recovery
            Mud,       // Mud, wet ground - deep deformation, slow recovery
            Gravel,    // Gravel, loose stones - medium deformation, medium recovery
            Ice,       // Ice, snow - very low friction, no visible marks
            Concrete   // Concrete, pavement - hard surface, minimal marks
        }

        /// <summary>
        /// Data structure for terrain material properties.
        /// </summary>
        [System.Serializable]
        public class TerrainMaterial
        {
            public string MaterialName;
            public TerrainType TerrainType;
            public float FrictionCoefficient = 1f; // How slippery (0-2)
            public float DeformationDepth = 0.02f; // How deep marks go
            public float DeformationRecoveryTime = 10f; // Seconds to fade
            public float DirtAccumulationRate = 1f; // How much dirt collects
            public Color DirtColor = Color.gray;
        }

        // Singleton instance
        public static TerrainMaterialManager Instance { get; private set; }

        // Material mapping
        private Dictionary<PhysicMaterial, TerrainType> materialMap = new Dictionary<PhysicMaterial, TerrainType>();
        private Dictionary<TerrainType, TerrainMaterial> terrainProperties = new Dictionary<TerrainType, TerrainMaterial>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDefaultMaterials();
        }

        /// <summary>
        /// Initialize default terrain material properties.
        /// </summary>
        private void InitializeDefaultMaterials()
        {
            // Road properties
            terrainProperties[TerrainType.Road] = new TerrainMaterial
            {
                MaterialName = "Road",
                TerrainType = TerrainType.Road,
                FrictionCoefficient = 0.8f,
                DeformationDepth = 0f,
                DeformationRecoveryTime = 0f,
                DirtAccumulationRate = 0.5f,
                DirtColor = new Color(0.6f, 0.6f, 0.6f) // Grey dust
            };

            // Grass properties
            terrainProperties[TerrainType.Grass] = new TerrainMaterial
            {
                MaterialName = "Grass",
                TerrainType = TerrainType.Grass,
                FrictionCoefficient = 0.7f,
                DeformationDepth = 0.01f,
                DeformationRecoveryTime = 7.5f,
                DirtAccumulationRate = 0.8f,
                DirtColor = new Color(0.4f, 0.35f, 0.2f) // Brown/green
            };

            // Sand properties
            terrainProperties[TerrainType.Sand] = new TerrainMaterial
            {
                MaterialName = "Sand",
                TerrainType = TerrainType.Sand,
                FrictionCoefficient = 0.5f,
                DeformationDepth = 0.03f,
                DeformationRecoveryTime = 25f,
                DirtAccumulationRate = 1.2f,
                DirtColor = new Color(0.75f, 0.7f, 0.5f) // Tan
            };

            // Mud properties
            terrainProperties[TerrainType.Mud] = new TerrainMaterial
            {
                MaterialName = "Mud",
                TerrainType = TerrainType.Mud,
                FrictionCoefficient = 0.4f,
                DeformationDepth = 0.05f,
                DeformationRecoveryTime = 45f,
                DirtAccumulationRate = 1.5f,
                DirtColor = new Color(0.3f, 0.25f, 0.15f) // Dark brown
            };

            // Gravel properties
            terrainProperties[TerrainType.Gravel] = new TerrainMaterial
            {
                MaterialName = "Gravel",
                TerrainType = TerrainType.Gravel,
                FrictionCoefficient = 0.65f,
                DeformationDepth = 0.015f,
                DeformationRecoveryTime = 18f,
                DirtAccumulationRate = 1.1f,
                DirtColor = new Color(0.65f, 0.65f, 0.65f) // Grey
            };

            // Ice properties
            terrainProperties[TerrainType.Ice] = new TerrainMaterial
            {
                MaterialName = "Ice",
                TerrainType = TerrainType.Ice,
                FrictionCoefficient = 0.1f,
                DeformationDepth = 0f,
                DeformationRecoveryTime = 0f,
                DirtAccumulationRate = 0.1f,
                DirtColor = new Color(0.9f, 0.95f, 1f) // Light blue
            };

            // Concrete properties
            terrainProperties[TerrainType.Concrete] = new TerrainMaterial
            {
                MaterialName = "Concrete",
                TerrainType = TerrainType.Concrete,
                FrictionCoefficient = 0.85f,
                DeformationDepth = 0f,
                DeformationRecoveryTime = 0f,
                DirtAccumulationRate = 0.6f,
                DirtColor = new Color(0.5f, 0.5f, 0.5f) // Dark grey
            };
        }

        /// <summary>
        /// Register a physics material as a terrain type.
        /// </summary>
        public void RegisterMaterial(PhysicMaterial physicMaterial, TerrainType terrainType)
        {
            if (physicMaterial != null)
            {
                materialMap[physicMaterial] = terrainType;
            }
        }

        /// <summary>
        /// Get terrain type from physics material.
        /// </summary>
        public TerrainType GetTerrainType(PhysicMaterial physicMaterial)
        {
            if (physicMaterial != null && materialMap.ContainsKey(physicMaterial))
            {
                return materialMap[physicMaterial];
            }

            // Default to road for unmapped materials
            return TerrainType.Road;
        }

        /// <summary>
        /// Get terrain type from collider's physics material.
        /// </summary>
        public TerrainType GetTerrainTypeFromCollider(Collider collider)
        {
            if (collider == null)
                return TerrainType.Road;

            PhysicMaterial material = collider.GetComponent<Collider>().material;
            if (material != null && materialMap.ContainsKey(material))
            {
                return materialMap[material];
            }

            // Check if collider has a tag-based fallback (legacy support)
            string tag = collider.gameObject.tag;
            if (!string.IsNullOrEmpty(tag) && tag != "Untagged")
            {
                return GetTerrainTypeFromTag(tag);
            }

            return TerrainType.Road;
        }

        /// <summary>
        /// Legacy: Get terrain type from tag for backwards compatibility.
        /// </summary>
        public TerrainType GetTerrainTypeFromTag(string tag)
        {
            return tag.ToLower() switch
            {
                "grass" => TerrainType.Grass,
                "sand" => TerrainType.Sand,
                "mud" => TerrainType.Mud,
                "gravel" => TerrainType.Gravel,
                "ice" => TerrainType.Ice,
                "concrete" => TerrainType.Concrete,
                _ => TerrainType.Road
            };
        }

        /// <summary>
        /// Get terrain material properties.
        /// </summary>
        public TerrainMaterial GetTerrainProperties(TerrainType terrainType)
        {
            if (terrainProperties.ContainsKey(terrainType))
            {
                return terrainProperties[terrainType];
            }

            return terrainProperties[TerrainType.Road];
        }

        /// <summary>
        /// Update terrain material properties at runtime.
        /// </summary>
        public void SetTerrainProperties(TerrainType terrainType, TerrainMaterial properties)
        {
            terrainProperties[terrainType] = properties;
        }

        /// <summary>
        /// Get friction coefficient for terrain type.
        /// </summary>
        public float GetFrictionCoefficient(TerrainType terrainType)
        {
            return GetTerrainProperties(terrainType).FrictionCoefficient;
        }

        /// <summary>
        /// Get deformation depth for terrain type.
        /// </summary>
        public float GetDeformationDepth(TerrainType terrainType)
        {
            return GetTerrainProperties(terrainType).DeformationDepth;
        }

        /// <summary>
        /// Get deformation recovery time for terrain type.
        /// </summary>
        public float GetRecoveryTime(TerrainType terrainType)
        {
            return GetTerrainProperties(terrainType).DeformationRecoveryTime;
        }

        /// <summary>
        /// Get dirt color for terrain type.
        /// </summary>
        public Color GetDirtColor(TerrainType terrainType)
        {
            return GetTerrainProperties(terrainType).DirtColor;
        }

        /// <summary>
        /// Get dirt accumulation rate for terrain type.
        /// </summary>
        public float GetDirtAccumulationRate(TerrainType terrainType)
        {
            return GetTerrainProperties(terrainType).DirtAccumulationRate;
        }
    }
}
