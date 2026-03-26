using UnityEngine;
using SendIt.Data;

namespace SendIt.Graphics
{
    /// <summary>
    /// Manages vehicle paint customization including color, metallic, gloss, and pearlescent effects.
    /// Handles real-time shader updates for paint finish.
    /// </summary>
    public class PaintSystem : MonoBehaviour
    {
        [SerializeField] private Renderer[] bodyRenderers;
        [SerializeField] private Material paintMaterialTemplate;

        private Color baseColor = Color.red;
        private float metallicIntensity = 0f;
        private float glossiness = 0.5f;
        private float pearlcentIntensity = 0f;

        private Material[] activePaintMaterials;
        private const string PAINT_SHADER = "Standard";

        public struct PaintSettings
        {
            public Color BaseColor;
            public float MetallicIntensity;
            public float Glossiness;
            public float PearlcentIntensity;
        }

        public void Initialize(GraphicsData graphicsData, Renderer[] renderers)
        {
            bodyRenderers = renderers;
            if (bodyRenderers == null || bodyRenderers.Length == 0)
            {
                Debug.LogWarning("No body renderers assigned to PaintSystem");
                return;
            }

            // Create paint materials for each renderer
            activePaintMaterials = new Material[bodyRenderers.Length];
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                if (bodyRenderers[i] != null)
                {
                    // Create unique material for this renderer
                    activePaintMaterials[i] = new Material(bodyRenderers[i].material);
                    bodyRenderers[i].material = activePaintMaterials[i];
                }
            }

            // Load settings from graphics data
            baseColor = graphicsData.BaseColor;
            metallicIntensity = graphicsData.MetallicIntensity;
            glossiness = graphicsData.Glossiness;
            pearlcentIntensity = graphicsData.PearlcentIntensity;

            ApplyPaintSettings();
        }

        /// <summary>
        /// Set the base paint color.
        /// </summary>
        public void SetBaseColor(Color color)
        {
            baseColor = color;
            ApplyColorToMaterials();
        }

        /// <summary>
        /// Set metallic intensity (0-1).
        /// </summary>
        public void SetMetallicIntensity(float intensity)
        {
            metallicIntensity = Mathf.Clamp01(intensity);
            ApplyMetallicToMaterials();
        }

        /// <summary>
        /// Set glossiness/smoothness (0-1).
        /// </summary>
        public void SetGlossiness(float value)
        {
            glossiness = Mathf.Clamp01(value);
            ApplyGlossinessToMaterials();
        }

        /// <summary>
        /// Set pearlescent effect intensity (0-1).
        /// </summary>
        public void SetPearlcentIntensity(float intensity)
        {
            pearlcentIntensity = Mathf.Clamp01(intensity);
            ApplyPearlcentToMaterials();
        }

        /// <summary>
        /// Apply all paint settings to materials.
        /// </summary>
        private void ApplyPaintSettings()
        {
            ApplyColorToMaterials();
            ApplyMetallicToMaterials();
            ApplyGlossinessToMaterials();
            ApplyPearlcentToMaterials();
        }

        /// <summary>
        /// Apply base color to all paint materials.
        /// </summary>
        private void ApplyColorToMaterials()
        {
            if (activePaintMaterials == null)
                return;

            foreach (var material in activePaintMaterials)
            {
                if (material != null)
                {
                    material.color = baseColor;
                }
            }
        }

        /// <summary>
        /// Apply metallic effect to materials.
        /// </summary>
        private void ApplyMetallicToMaterials()
        {
            if (activePaintMaterials == null)
                return;

            foreach (var material in activePaintMaterials)
            {
                if (material != null && material.HasProperty("_Metallic"))
                {
                    material.SetFloat("_Metallic", metallicIntensity);
                }
            }
        }

        /// <summary>
        /// Apply glossiness to materials.
        /// </summary>
        private void ApplyGlossinessToMaterials()
        {
            if (activePaintMaterials == null)
                return;

            foreach (var material in activePaintMaterials)
            {
                if (material != null && material.HasProperty("_Glossiness"))
                {
                    material.SetFloat("_Glossiness", glossiness);
                }
            }
        }

        /// <summary>
        /// Apply pearlescent effect (using color shift).
        /// </summary>
        private void ApplyPearlcentToMaterials()
        {
            if (activePaintMaterials == null)
                return;

            foreach (var material in activePaintMaterials)
            {
                if (material != null)
                {
                    // Shift color slightly based on pearlescent intensity
                    Color pearlColor = Color.Lerp(baseColor, new Color(1f, 1f, 1f), pearlcentIntensity * 0.3f);

                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.SetColor("_EmissionColor", pearlColor * pearlcentIntensity * 0.2f);
                    }
                }
            }
        }

        /// <summary>
        /// Get current paint settings.
        /// </summary>
        public PaintSettings GetPaintSettings()
        {
            return new PaintSettings
            {
                BaseColor = baseColor,
                MetallicIntensity = metallicIntensity,
                Glossiness = glossiness,
                PearlcentIntensity = pearlcentIntensity
            };
        }

        /// <summary>
        /// Apply a preset paint configuration.
        /// </summary>
        public void ApplyPaintPreset(string presetName)
        {
            switch (presetName.ToLower())
            {
                case "racing_red":
                    baseColor = new Color(1f, 0f, 0f);
                    metallicIntensity = 0.3f;
                    glossiness = 0.8f;
                    break;

                case "carbon_black":
                    baseColor = new Color(0.1f, 0.1f, 0.1f);
                    metallicIntensity = 0f;
                    glossiness = 0.6f;
                    break;

                case "pearl_white":
                    baseColor = Color.white;
                    metallicIntensity = 0.2f;
                    glossiness = 0.9f;
                    pearlcentIntensity = 0.5f;
                    break;

                case "metallic_blue":
                    baseColor = new Color(0f, 0.3f, 1f);
                    metallicIntensity = 0.8f;
                    glossiness = 0.7f;
                    break;

                case "matte_grey":
                    baseColor = new Color(0.5f, 0.5f, 0.5f);
                    metallicIntensity = 0f;
                    glossiness = 0.2f;
                    break;
            }

            ApplyPaintSettings();
        }

        // Getters
        public Color GetBaseColor() => baseColor;
        public float GetMetallicIntensity() => metallicIntensity;
        public float GetGlossiness() => glossiness;
        public float GetPearlcentIntensity() => pearlcentIntensity;
    }
}
