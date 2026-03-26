using UnityEngine;
using System.Collections.Generic;

namespace SendIt.Customization
{
    /// <summary>
    /// Manages interior customization including seats, steering wheel,
    /// dashboard, trim materials, and cabin colors.
    /// </summary>
    public class InteriorCustomizer : MonoBehaviour
    {
        [SerializeField] private Transform[] seatTransforms = new Transform[4]; // Driver, Passenger, Rear-Left, Rear-Right
        [SerializeField] private Transform steeringWheelTransform;
        [SerializeField] private Transform dashboardTransform;
        [SerializeField] private Renderer[] interiorRenderers;

        // Interior state
        private int seatStyle = 0; // 0=Stock, 1=Sport, 2=Luxury, 3=Racing
        private int seatMaterial = 0; // 0=Cloth, 1=Leather, 2=Suede, 3=Custom
        private Color seatColor = Color.black;
        private int steeringWheelStyle = 0; // 0=Stock, 1=Sport, 2=Racing, 3=Custom
        private int dashboardTrim = 0; // 0=Plastic, 1=Leather, 2=Alcantara, 3=Carbon Fiber
        private Color trimColor = new Color(0.3f, 0.3f, 0.3f);
        private float ambientLighting = 0.5f; // 0-1, interior cabin lighting
        private bool enableAmbientLights = true;

        // Customization options
        private Dictionary<int, string> seatStyles = new Dictionary<int, string>
        {
            { 0, "Stock Seats" },
            { 1, "Sport Bucket Seats" },
            { 2, "Luxury Lounge Seats" },
            { 3, "Racing Harness Seats" }
        };

        private Dictionary<int, string> seatMaterials = new Dictionary<int, string>
        {
            { 0, "Cloth Upholstery" },
            { 1, "Leather Interior" },
            { 2, "Suede Premium" },
            { 3, "Custom Fabric" }
        };

        [System.Serializable]
        public struct InteriorSettings
        {
            public int SeatStyle;
            public int SeatMaterial;
            public Color SeatColor;
            public int SteeringWheelStyle;
            public int DashboardTrim;
            public Color TrimColor;
            public float AmbientLighting;
            public bool EnableAmbientLights;
        }

        /// <summary>
        /// Initialize interior customizer.
        /// </summary>
        public void Initialize()
        {
            // Get renderers if not assigned
            if (interiorRenderers == null || interiorRenderers.Length == 0)
            {
                interiorRenderers = GetComponentsInChildren<Renderer>();
            }

            ApplyInteriorSettings();
        }

        /// <summary>
        /// Set seat style/type.
        /// </summary>
        public void SetSeatStyle(int style)
        {
            seatStyle = Mathf.Clamp(style, 0, 3);
            ApplySeats();
        }

        /// <summary>
        /// Set seat material/upholstery.
        /// </summary>
        public void SetSeatMaterial(int material)
        {
            seatMaterial = Mathf.Clamp(material, 0, 3);
            ApplySeats();
        }

        /// <summary>
        /// Set seat color.
        /// </summary>
        public void SetSeatColor(Color color)
        {
            seatColor = color;
            ApplySeats();
        }

        /// <summary>
        /// Set steering wheel type.
        /// </summary>
        public void SetSteeringWheelStyle(int style)
        {
            steeringWheelStyle = Mathf.Clamp(style, 0, 3);
            ApplySteeringWheel();
        }

        /// <summary>
        /// Set dashboard/trim material.
        /// </summary>
        public void SetDashboardTrim(int trim)
        {
            dashboardTrim = Mathf.Clamp(trim, 0, 3);
            ApplyDashboard();
        }

        /// <summary>
        /// Set interior trim color.
        /// </summary>
        public void SetTrimColor(Color color)
        {
            trimColor = color;
            ApplyDashboard();
        }

        /// <summary>
        /// Set ambient lighting level (0-1).
        /// </summary>
        public void SetAmbientLighting(float level)
        {
            ambientLighting = Mathf.Clamp01(level);
            ApplyAmbientLighting();
        }

        /// <summary>
        /// Enable/disable ambient cabin lighting.
        /// </summary>
        public void SetAmbientLights(bool enabled)
        {
            enableAmbientLights = enabled;
            ApplyAmbientLighting();
        }

        /// <summary>
        /// Apply seat customization.
        /// </summary>
        private void ApplySeats()
        {
            // In a full implementation:
            // 1. Instantiate correct seat model based on seatStyle
            // 2. Apply material/texture based on seatMaterial
            // 3. Apply color tint
            // 4. Update physics if seat affects weight distribution

            foreach (var seatTransform in seatTransforms)
            {
                if (seatTransform != null)
                {
                    // Update seat visual properties
                    var seatRenderer = seatTransform.GetComponent<Renderer>();
                    if (seatRenderer != null && seatRenderer.material != null)
                    {
                        seatRenderer.material.color = seatColor;

                        // Adjust material properties based on material type
                        switch (seatMaterial)
                        {
                            case 0: // Cloth
                                seatRenderer.material.SetFloat("_Smoothness", 0.3f);
                                break;
                            case 1: // Leather
                                seatRenderer.material.SetFloat("_Smoothness", 0.6f);
                                seatRenderer.material.SetFloat("_Metallic", 0.1f);
                                break;
                            case 2: // Suede
                                seatRenderer.material.SetFloat("_Smoothness", 0.2f);
                                break;
                            case 3: // Custom
                                seatRenderer.material.SetFloat("_Smoothness", 0.5f);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Apply steering wheel customization.
        /// </summary>
        private void ApplySteeringWheel()
        {
            if (steeringWheelTransform == null)
                return;

            // Update steering wheel visual based on style
            var wheelRenderer = steeringWheelTransform.GetComponent<Renderer>();
            if (wheelRenderer != null && wheelRenderer.material != null)
            {
                // Apply material properties based on steering wheel style
                switch (steeringWheelStyle)
                {
                    case 0: // Stock
                        wheelRenderer.material.color = new Color(0.5f, 0.5f, 0.5f);
                        wheelRenderer.material.SetFloat("_Smoothness", 0.4f);
                        break;
                    case 1: // Sport
                        wheelRenderer.material.color = new Color(0.3f, 0.3f, 0.3f);
                        wheelRenderer.material.SetFloat("_Smoothness", 0.5f);
                        break;
                    case 2: // Racing
                        wheelRenderer.material.color = Color.black;
                        wheelRenderer.material.SetFloat("_Smoothness", 0.3f);
                        wheelRenderer.material.SetFloat("_Metallic", 0.2f);
                        break;
                    case 3: // Custom
                        wheelRenderer.material.color = trimColor;
                        wheelRenderer.material.SetFloat("_Smoothness", 0.6f);
                        break;
                }
            }
        }

        /// <summary>
        /// Apply dashboard and trim customization.
        /// </summary>
        private void ApplyDashboard()
        {
            if (dashboardTransform == null)
                return;

            var dashRenderer = dashboardTransform.GetComponent<Renderer>();
            if (dashRenderer != null && dashRenderer.material != null)
            {
                dashRenderer.material.color = trimColor;

                // Apply material properties based on trim type
                switch (dashboardTrim)
                {
                    case 0: // Plastic
                        dashRenderer.material.SetFloat("_Smoothness", 0.3f);
                        dashRenderer.material.SetFloat("_Metallic", 0f);
                        break;
                    case 1: // Leather
                        dashRenderer.material.SetFloat("_Smoothness", 0.5f);
                        dashRenderer.material.SetFloat("_Metallic", 0.05f);
                        break;
                    case 2: // Alcantara
                        dashRenderer.material.SetFloat("_Smoothness", 0.2f);
                        dashRenderer.material.SetFloat("_Metallic", 0f);
                        break;
                    case 3: // Carbon Fiber
                        dashRenderer.material.SetFloat("_Smoothness", 0.7f);
                        dashRenderer.material.SetFloat("_Metallic", 0.3f);
                        break;
                }
            }
        }

        /// <summary>
        /// Apply ambient cabin lighting.
        /// </summary>
        private void ApplyAmbientLighting()
        {
            if (!enableAmbientLights)
            {
                // Disable ambient lights
                RenderSettings.ambientIntensity *= 0.8f;
                return;
            }

            // Adjust ambient light intensity based on setting
            float targetIntensity = 0.5f + (ambientLighting * 0.5f);
            RenderSettings.ambientIntensity = Mathf.Lerp(RenderSettings.ambientIntensity, targetIntensity, Time.deltaTime);
        }

        /// <summary>
        /// Apply all interior settings at once.
        /// </summary>
        private void ApplyInteriorSettings()
        {
            ApplySeats();
            ApplySteeringWheel();
            ApplyDashboard();
            ApplyAmbientLighting();
        }

        /// <summary>
        /// Get current interior settings.
        /// </summary>
        public InteriorSettings GetInteriorSettings()
        {
            return new InteriorSettings
            {
                SeatStyle = seatStyle,
                SeatMaterial = seatMaterial,
                SeatColor = seatColor,
                SteeringWheelStyle = steeringWheelStyle,
                DashboardTrim = dashboardTrim,
                TrimColor = trimColor,
                AmbientLighting = ambientLighting,
                EnableAmbientLights = enableAmbientLights
            };
        }

        /// <summary>
        /// Get description of current seat configuration.
        /// </summary>
        public string GetSeatDescription()
        {
            string style = seatStyles.ContainsKey(seatStyle) ? seatStyles[seatStyle] : "Unknown";
            string material = seatMaterials.ContainsKey(seatMaterial) ? seatMaterials[seatMaterial] : "Unknown";
            return $"{style} - {material}";
        }

        // Getters
        public int GetSeatStyle() => seatStyle;
        public int GetSeatMaterial() => seatMaterial;
        public Color GetSeatColor() => seatColor;
        public int GetSteeringWheelStyle() => steeringWheelStyle;
        public int GetDashboardTrim() => dashboardTrim;
        public Color GetTrimColor() => trimColor;
    }
}
