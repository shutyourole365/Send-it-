using UnityEngine;
using System.Collections.Generic;

namespace SendIt.Customization
{
    /// <summary>
    /// Manages advanced visual customization including lighting effects,
    /// window tinting, underglow, neon, and other visual modifications.
    /// </summary>
    public class VisualCustomizer : MonoBehaviour
    {
        [SerializeField] private Light[] headlights;
        [SerializeField] private Light[] taillights;
        [SerializeField] private Renderer[] windowRenderers;
        [SerializeField] private Transform underglowContainer;

        // Lighting modifications
        private int headlightType = 0; // 0=Stock, 1=LED, 2=HID, 3=Laser/RGB
        private Color headlightColor = Color.white;
        private float headlightIntensity = 1f;

        private int taillightType = 0; // 0=Stock, 1=LED, 2=Custom, 3=RGB
        private Color taillightColor = Color.red;
        private float taillightIntensity = 1f;

        // Window customization
        private float tintLevel = 0f; // 0=Clear, 1=Full tint
        private Color tintColor = new Color(0, 0, 0, 0.5f);
        private bool hasSmokeLighting = false;

        // Underglow and neon
        private bool hasUnderglow = false;
        private int underglowType = 0; // 0=Static, 1=Animated, 2=Reactive
        private Color underglowColor = Color.blue;
        private float underglowIntensity = 0.5f;
        private bool hasNeon = false;
        private Color neonColor = Color.cyan;

        // Additional visual effects
        private bool hasCustomBadges = false;
        private bool hasLoweredSuspensionVisuals = false;
        private bool hasWidebodyKit = false;
        private float customVinylOpacity = 0f; // 0-1

        [System.Serializable]
        public struct VisualSettings
        {
            public int HeadlightType;
            public Color HeadlightColor;
            public int TaillightType;
            public Color TaillightColor;
            public float TintLevel;
            public bool HasUnderglow;
            public Color UnderglowColor;
            public bool HasNeon;
            public Color NeonColor;
            public bool HasCustomBadges;
            public bool HasWidebodyKit;
            public float CustomVinylOpacity;
        }

        /// <summary>
        /// Initialize visual customizer.
        /// </summary>
        public void Initialize()
        {
            if (headlights == null || headlights.Length == 0)
            {
                headlights = GetComponentsInChildren<Light>();
            }

            ApplyVisualSettings();
        }

        /// <summary>
        /// Set headlight type.
        /// </summary>
        public void SetHeadlightType(int type)
        {
            headlightType = Mathf.Clamp(type, 0, 3);
            ApplyHeadlights();
        }

        /// <summary>
        /// Set headlight color.
        /// </summary>
        public void SetHeadlightColor(Color color)
        {
            headlightColor = color;
            ApplyHeadlights();
        }

        /// <summary>
        /// Set headlight intensity.
        /// </summary>
        public void SetHeadlightIntensity(float intensity)
        {
            headlightIntensity = Mathf.Clamp01(intensity);
            ApplyHeadlights();
        }

        /// <summary>
        /// Apply headlight customization.
        /// </summary>
        private void ApplyHeadlights()
        {
            if (headlights == null || headlights.Length == 0)
                return;

            foreach (var light in headlights)
            {
                if (light != null)
                {
                    light.color = headlightColor;
                    light.intensity = headlightIntensity * (headlightType + 1) * 1.5f; // Brighter lights are more intense

                    switch (headlightType)
                    {
                        case 0: // Stock
                            light.range = 20f;
                            break;
                        case 1: // LED
                            light.range = 30f;
                            break;
                        case 2: // HID
                            light.range = 35f;
                            break;
                        case 3: // Laser/RGB
                            light.range = 40f;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Set taillight type.
        /// </summary>
        public void SetTaillightType(int type)
        {
            taillightType = Mathf.Clamp(type, 0, 3);
            ApplyTaillights();
        }

        /// <summary>
        /// Set taillight color.
        /// </summary>
        public void SetTaillightColor(Color color)
        {
            taillightColor = color;
            ApplyTaillights();
        }

        /// <summary>
        /// Apply taillight customization.
        /// </summary>
        private void ApplyTaillights()
        {
            if (taillights == null || taillights.Length == 0)
                return;

            foreach (var light in taillights)
            {
                if (light != null)
                {
                    light.color = taillightColor;
                    light.intensity = taillightIntensity * (taillightType + 1);
                }
            }
        }

        /// <summary>
        /// Set window tint level (0-1).
        /// </summary>
        public void SetWindowTint(float level)
        {
            tintLevel = Mathf.Clamp01(level);
            ApplyWindowTint();
        }

        /// <summary>
        /// Apply window tinting.
        /// </summary>
        private void ApplyWindowTint()
        {
            if (windowRenderers == null || windowRenderers.Length == 0)
                return;

            foreach (var renderer in windowRenderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    Color tintedColor = Color.Lerp(Color.white, tintColor, tintLevel);
                    renderer.material.color = tintedColor;
                    renderer.material.SetFloat("_Mode", tintLevel); // For transparency
                }
            }
        }

        /// <summary>
        /// Enable/disable underglow lighting.
        /// </summary>
        public void SetUnderglow(bool enabled)
        {
            hasUnderglow = enabled;
            ApplyUnderglow();
        }

        /// <summary>
        /// Set underglow type.
        /// </summary>
        public void SetUnderglowType(int type)
        {
            underglowType = Mathf.Clamp(type, 0, 2);
            ApplyUnderglow();
        }

        /// <summary>
        /// Set underglow color.
        /// </summary>
        public void SetUnderglowColor(Color color)
        {
            underglowColor = color;
            ApplyUnderglow();
        }

        /// <summary>
        /// Apply underglow lighting.
        /// </summary>
        private void ApplyUnderglow()
        {
            if (!hasUnderglow || underglowContainer == null)
                return;

            // Create or update underglow lights
            var underglowLight = underglowContainer.GetComponentInChildren<Light>();
            if (underglowLight == null)
            {
                underglowLight = underglowContainer.gameObject.AddComponent<Light>();
            }

            underglowLight.color = underglowColor;
            underglowLight.intensity = underglowIntensity;
            underglowLight.range = 10f;
            underglowLight.enabled = hasUnderglow;

            // Add animation if reactive type
            if (underglowType == 2)
            {
                // Reactive underglow pulses with music/engine
                float pulse = Mathf.Sin(Time.time * 5f) * 0.5f + 0.5f;
                underglowLight.intensity = underglowIntensity * pulse;
            }
        }

        /// <summary>
        /// Enable/disable neon accent lighting.
        /// </summary>
        public void SetNeon(bool enabled)
        {
            hasNeon = enabled;
            ApplyNeon();
        }

        /// <summary>
        /// Set neon color.
        /// </summary>
        public void SetNeonColor(Color color)
        {
            neonColor = color;
            ApplyNeon();
        }

        /// <summary>
        /// Apply neon lighting effects.
        /// </summary>
        private void ApplyNeon()
        {
            // Neon accents on the vehicle body
            // Would be implemented as emission on specific materials
        }

        /// <summary>
        /// Enable/disable custom badges and emblems.
        /// </summary>
        public void SetCustomBadges(bool enabled)
        {
            hasCustomBadges = enabled;
        }

        /// <summary>
        /// Enable/disable visual lowering effect (suspension visual).
        /// </summary>
        public void SetLoweredSuspensionVisuals(bool enabled)
        {
            hasLoweredSuspensionVisuals = enabled;
            if (enabled)
            {
                transform.localPosition -= Vector3.up * 0.05f; // Lower vehicle visually
            }
        }

        /// <summary>
        /// Enable/disable widebody kit visual.
        /// </summary>
        public void SetWidebodyKit(bool enabled)
        {
            hasWidebodyKit = enabled;
            // Would scale/modify vehicle width visually
        }

        /// <summary>
        /// Set custom vinyl wrap opacity.
        /// </summary>
        public void SetCustomVinylOpacity(float opacity)
        {
            customVinylOpacity = Mathf.Clamp01(opacity);
        }

        /// <summary>
        /// Apply all visual settings.
        /// </summary>
        private void ApplyVisualSettings()
        {
            ApplyHeadlights();
            ApplyTaillights();
            ApplyWindowTint();
            ApplyUnderglow();
            ApplyNeon();
        }

        /// <summary>
        /// Get current visual settings.
        /// </summary>
        public VisualSettings GetVisualSettings()
        {
            return new VisualSettings
            {
                HeadlightType = headlightType,
                HeadlightColor = headlightColor,
                TaillightType = taillightType,
                TaillightColor = taillightColor,
                TintLevel = tintLevel,
                HasUnderglow = hasUnderglow,
                UnderglowColor = underglowColor,
                HasNeon = hasNeon,
                NeonColor = neonColor,
                HasCustomBadges = hasCustomBadges,
                HasWidebodyKit = hasWidebodyKit,
                CustomVinylOpacity = customVinylOpacity
            };
        }

        // Getters
        public int GetHeadlightType() => headlightType;
        public Color GetHeadlightColor() => headlightColor;
        public int GetTaillightType() => taillightType;
        public float GetWindowTintLevel() => tintLevel;
        public bool HasUnderglow() => hasUnderglow;
        public bool HasNeon() => hasNeon;
        public bool HasWidebodyKit() => hasWidebodyKit;
    }
}
