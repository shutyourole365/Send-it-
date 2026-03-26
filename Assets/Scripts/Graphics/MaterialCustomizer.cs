using UnityEngine;
using SendIt.Data;

namespace SendIt.Graphics
{
    /// <summary>
    /// Manages material customization including shader parameters, weathering, and wear effects.
    /// Applies real-time weathering, rust, and dirt accumulation to vehicle materials.
    /// </summary>
    public class MaterialCustomizer : MonoBehaviour
    {
        [SerializeField] private Material[] targetMaterials;

        // Material properties
        private float wearAmount = 0f; // 0-1
        private float dirtAccumulation = 0f; // 0-1
        private float rustAmount = 0f; // 0-1

        // Shader property names
        private const string WEAR_PROPERTY = "_WearAmount";
        private const string DIRT_PROPERTY = "_DirtAmount";
        private const string RUST_PROPERTY = "_RustAmount";
        private const string ROUGHNESS_PROPERTY = "_Smoothness";

        public struct MaterialSettings
        {
            public float WearAmount;
            public float DirtAmount;
            public float RustAmount;
        }

        public void Initialize(GraphicsData graphicsData, Material[] materials)
        {
            targetMaterials = materials;
            if (targetMaterials == null || targetMaterials.Length == 0)
            {
                Debug.LogWarning("No materials assigned to MaterialCustomizer");
                return;
            }

            wearAmount = graphicsData.WearAmount;
            dirtAccumulation = graphicsData.DirtAccumulation;
            rustAmount = graphicsData.RustAmount;

            ApplyMaterialSettings();
        }

        /// <summary>
        /// Set the wear/damage amount on the paint.
        /// </summary>
        public void SetWearAmount(float amount)
        {
            wearAmount = Mathf.Clamp01(amount);
            ApplyWearToMaterials();
        }

        /// <summary>
        /// Set dirt and dust accumulation.
        /// </summary>
        public void SetDirtAccumulation(float amount)
        {
            dirtAccumulation = Mathf.Clamp01(amount);
            ApplyDirtToMaterials();
        }

        /// <summary>
        /// Set rust/oxidation amount.
        /// </summary>
        public void SetRustAmount(float amount)
        {
            rustAmount = Mathf.Clamp01(amount);
            ApplyRustToMaterials();
        }

        /// <summary>
        /// Gradually apply wear effect over time (for simulation).
        /// </summary>
        public void AccumulateWear(float deltaTime, float intensity = 0.01f)
        {
            wearAmount += deltaTime * intensity;
            wearAmount = Mathf.Clamp01(wearAmount);
            ApplyWearToMaterials();
        }

        /// <summary>
        /// Gradually accumulate dirt from driving.
        /// </summary>
        public void AccumulateDirt(float deltaTime, float speed = 0f, float intensity = 0.05f)
        {
            // Dirt accumulates faster at higher speeds
            float speedFactor = Mathf.Clamp01(speed / 20f);
            dirtAccumulation += deltaTime * intensity * speedFactor;
            dirtAccumulation = Mathf.Clamp01(dirtAccumulation);
            ApplyDirtToMaterials();
        }

        /// <summary>
        /// Clean the vehicle (reduce dirt).
        /// </summary>
        public void Clean(float amount = 1f)
        {
            dirtAccumulation -= amount;
            dirtAccumulation = Mathf.Max(0f, dirtAccumulation);
            ApplyDirtToMaterials();
        }

        /// <summary>
        /// Apply wear effect to materials.
        /// </summary>
        private void ApplyWearToMaterials()
        {
            if (targetMaterials == null)
                return;

            foreach (var material in targetMaterials)
            {
                if (material != null)
                {
                    // Wear reduces gloss and adds slight color variation
                    if (material.HasProperty(ROUGHNESS_PROPERTY))
                    {
                        float baseRoughness = 0.5f;
                        float wearRoughness = Mathf.Lerp(baseRoughness, 0.1f, wearAmount);
                        material.SetFloat(ROUGHNESS_PROPERTY, wearRoughness);
                    }

                    // Add wear color (darker)
                    if (material.HasProperty("_WearColor"))
                    {
                        Color wearColor = Color.Lerp(Color.white, new Color(0.3f, 0.3f, 0.3f), wearAmount * 0.5f);
                        material.SetColor("_WearColor", wearColor);
                    }
                }
            }
        }

        /// <summary>
        /// Apply dirt/dust accumulation to materials.
        /// </summary>
        private void ApplyDirtToMaterials()
        {
            if (targetMaterials == null)
                return;

            foreach (var material in targetMaterials)
            {
                if (material != null)
                {
                    // Dirt makes surface slightly less reflective
                    if (material.HasProperty(ROUGHNESS_PROPERTY))
                    {
                        float baseRoughness = 0.5f;
                        float dirtRoughness = Mathf.Lerp(baseRoughness, 0.7f, dirtAccumulation);
                        material.SetFloat(ROUGHNESS_PROPERTY, dirtRoughness);
                    }

                    // Apply dirt color (brown/grey tint)
                    if (material.HasProperty("_DirtColor"))
                    {
                        Color dirtColor = Color.Lerp(Color.white, new Color(0.6f, 0.55f, 0.5f), dirtAccumulation);
                        material.SetColor("_DirtColor", dirtColor);
                    }
                }
            }
        }

        /// <summary>
        /// Apply rust/oxidation effect to materials.
        /// </summary>
        private void ApplyRustToMaterials()
        {
            if (targetMaterials == null)
                return;

            foreach (var material in targetMaterials)
            {
                if (material != null)
                {
                    // Rust reduces metallic sheen
                    if (material.HasProperty("_Metallic"))
                    {
                        float baseMetallic = 0.5f;
                        float rustMetallic = Mathf.Lerp(baseMetallic, 0.1f, rustAmount);
                        material.SetFloat("_Metallic", rustMetallic);
                    }

                    // Apply rust color (orange/red tint)
                    if (material.HasProperty("_RustColor"))
                    {
                        Color rustColor = Color.Lerp(Color.white, new Color(0.8f, 0.4f, 0.1f), rustAmount);
                        material.SetColor("_RustColor", rustColor);
                    }
                }
            }
        }

        /// <summary>
        /// Apply all material settings at once.
        /// </summary>
        private void ApplyMaterialSettings()
        {
            ApplyWearToMaterials();
            ApplyDirtToMaterials();
            ApplyRustToMaterials();
        }

        /// <summary>
        /// Get current material settings.
        /// </summary>
        public MaterialSettings GetMaterialSettings()
        {
            return new MaterialSettings
            {
                WearAmount = wearAmount,
                DirtAmount = dirtAccumulation,
                RustAmount = rustAmount
            };
        }

        /// <summary>
        /// Set a custom shader property value.
        /// </summary>
        public void SetShaderProperty(string propertyName, float value)
        {
            if (targetMaterials == null)
                return;

            foreach (var material in targetMaterials)
            {
                if (material != null && material.HasProperty(propertyName))
                {
                    material.SetFloat(propertyName, value);
                }
            }
        }

        /// <summary>
        /// Set a custom shader color property.
        /// </summary>
        public void SetShaderColor(string propertyName, Color color)
        {
            if (targetMaterials == null)
                return;

            foreach (var material in targetMaterials)
            {
                if (material != null && material.HasProperty(propertyName))
                {
                    material.SetColor(propertyName, color);
                }
            }
        }

        // Getters
        public float GetWearAmount() => wearAmount;
        public float GetDirtAccumulation() => dirtAccumulation;
        public float GetRustAmount() => rustAmount;
    }
}
