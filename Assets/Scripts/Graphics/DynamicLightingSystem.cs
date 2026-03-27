using UnityEngine;

namespace SendIt.Graphics
{
    /// <summary>
    /// Dynamic lighting system for real-time light adjustments based on vehicle state.
    /// Manages headlights, brake lights, engine glow, and environmental lighting.
    /// </summary>
    public class DynamicLightingSystem : MonoBehaviour
    {
        [SerializeField] private Light[] headlights = new Light[2];
        [SerializeField] private Light[] brakelights = new Light[2];
        [SerializeField] private Light engineGlowLight;
        [SerializeField] private Light ambientLight;

        // Light states
        private float headlightIntensity = 1.5f;
        private float brakeLightIntensity = 2.0f;
        private float engineGlowIntensity = 0.5f;

        // Colors
        private Color headlightColor = new Color(1f, 0.95f, 0.85f); // Warm white
        private Color brakeLightColor = new Color(1f, 0.2f, 0.2f); // Red
        private Color engineGlowColor = new Color(1f, 0.6f, 0.3f); // Orange

        // Control flags
        private bool headlightsOn = false;
        private bool brakeLightsActive = false;
        private float timeOfDay = 12f; // 0-24 hour cycle
        private float ambientIntensity = 1f;

        // Light response
        private float brakeLightResponseSpeed = 5f;
        private float engineGlowResponseSpeed = 2f;
        private AnimationCurve timeOfDayIntensity; // How light changes throughout day

        private bool isInitialized;

        public void Initialize()
        {
            InitializeTimeOfDayIntensity();
            isInitialized = true;
        }

        /// <summary>
        /// Initialize time of day lighting curve.
        /// </summary>
        private void InitializeTimeOfDayIntensity()
        {
            timeOfDayIntensity = new AnimationCurve(
                new Keyframe(0f, 0.3f),      // Midnight: very dark
                new Keyframe(6f, 0.4f),      // Early morning: still dark
                new Keyframe(9f, 0.8f),      // Morning: brightening
                new Keyframe(12f, 1.5f),     // Noon: peak brightness
                new Keyframe(15f, 1.2f),     // Afternoon: still bright
                new Keyframe(18f, 0.7f),     // Evening: getting dark
                new Keyframe(21f, 0.35f),    // Night: dark
                new Keyframe(24f, 0.3f)      // Midnight: very dark
            );
        }

        /// <summary>
        /// Update lighting based on vehicle state.
        /// </summary>
        public void UpdateLighting(bool braking, float engineTemp, float speed, bool nightMode = false)
        {
            if (!isInitialized)
                return;

            UpdateHeadlights(nightMode || timeOfDay < 6f || timeOfDay > 20f);
            UpdateBrakeLights(braking);
            UpdateEngineGlow(engineTemp);
            UpdateAmbientLighting();
        }

        /// <summary>
        /// Update headlight state and intensity.
        /// </summary>
        private void UpdateHeadlights(bool shouldBeOn)
        {
            if (shouldBeOn != headlightsOn)
            {
                headlightsOn = shouldBeOn;

                for (int i = 0; i < headlights.Length; i++)
                {
                    if (headlights[i] != null)
                    {
                        headlights[i].enabled = headlightsOn;
                    }
                }
            }

            // Adjust intensity based on ambient light
            if (headlightsOn)
            {
                float targetIntensity = headlightIntensity;
                for (int i = 0; i < headlights.Length; i++)
                {
                    if (headlights[i] != null)
                    {
                        headlights[i].intensity = targetIntensity;
                    }
                }
            }
        }

        /// <summary>
        /// Update brake light intensity smoothly.
        /// </summary>
        private void UpdateBrakeLights(bool braking)
        {
            float targetIntensity = braking ? brakeLightIntensity : 0.2f; // Idle glow when not braking

            for (int i = 0; i < brakelights.Length; i++)
            {
                if (brakelights[i] != null)
                {
                    float currentIntensity = brakelights[i].intensity;
                    float newIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * brakeLightResponseSpeed);

                    brakelights[i].intensity = newIntensity;
                    brakelights[i].color = brakeLightColor;

                    // Disable if intensity is very low
                    if (newIntensity < 0.01f)
                    {
                        brakelights[i].enabled = false;
                    }
                    else
                    {
                        brakelights[i].enabled = true;
                    }
                }
            }

            brakeLightsActive = braking;
        }

        /// <summary>
        /// Update engine glow based on temperature.
        /// </summary>
        private void UpdateEngineGlow(float engineTemp)
        {
            if (engineGlowLight == null)
                return;

            // Engine glow increases with temperature
            float tempFactor = Mathf.Clamp01((engineTemp - 80f) / 50f);

            // Color shifts from cool to hot
            Color glowColor = Color.Lerp(Color.blue, Color.red, tempFactor);

            // Intensity based on temperature
            float targetIntensity = tempFactor * engineGlowIntensity;
            engineGlowLight.intensity = Mathf.Lerp(engineGlowLight.intensity, targetIntensity, Time.deltaTime * engineGlowResponseSpeed);
            engineGlowLight.color = glowColor;

            // Enable/disable based on intensity
            if (engineGlowLight.intensity > 0.01f)
            {
                engineGlowLight.enabled = true;
            }
            else
            {
                engineGlowLight.enabled = false;
            }
        }

        /// <summary>
        /// Update ambient lighting based on time of day.
        /// </summary>
        private void UpdateAmbientLighting()
        {
            if (ambientLight == null)
                return;

            // Get intensity from curve
            float curveIntensity = timeOfDayIntensity.Evaluate(timeOfDay);
            float targetIntensity = curveIntensity * ambientIntensity;

            ambientLight.intensity = Mathf.Lerp(ambientLight.intensity, targetIntensity, Time.deltaTime);

            // Shift color based on time of day
            Color ambientColor = GetAmbientColorForTime();
            ambientLight.color = ambientColor;
        }

        /// <summary>
        /// Get ambient light color based on time of day.
        /// </summary>
        private Color GetAmbientColorForTime()
        {
            // Sunrise/sunset: warm colors
            if (timeOfDay < 7f) // Early morning
                return Color.Lerp(new Color(0.2f, 0.2f, 0.3f), new Color(1f, 0.7f, 0.3f), (timeOfDay / 7f));

            if (timeOfDay < 12f) // Morning to noon
                return Color.Lerp(new Color(1f, 0.7f, 0.3f), new Color(1f, 1f, 1f), ((timeOfDay - 7f) / 5f));

            if (timeOfDay < 17f) // Noon to evening
                return Color.Lerp(new Color(1f, 1f, 1f), new Color(1f, 0.8f, 0.4f), ((timeOfDay - 12f) / 5f));

            if (timeOfDay < 21f) // Evening to night
                return Color.Lerp(new Color(1f, 0.8f, 0.4f), new Color(0.3f, 0.3f, 0.5f), ((timeOfDay - 17f) / 4f));

            // Night: cool colors
            return new Color(0.2f, 0.2f, 0.4f);
        }

        /// <summary>
        /// Set time of day (0-24 hour cycle).
        /// </summary>
        public void SetTimeOfDay(float hour)
        {
            timeOfDay = Mathf.Repeat(hour, 24f);
        }

        /// <summary>
        /// Get current time of day.
        /// </summary>
        public float GetTimeOfDay() => timeOfDay;

        /// <summary>
        /// Set headlight intensity (0-2).
        /// </summary>
        public void SetHeadlightIntensity(float intensity)
        {
            headlightIntensity = Mathf.Clamp(intensity, 0f, 2f);
        }

        /// <summary>
        /// Set brake light intensity (0-3).
        /// </summary>
        public void SetBrakeLightIntensity(float intensity)
        {
            brakeLightIntensity = Mathf.Clamp(intensity, 0f, 3f);
        }

        /// <summary>
        /// Set engine glow intensity (0-2).
        /// </summary>
        public void SetEngineGlowIntensity(float intensity)
        {
            engineGlowIntensity = Mathf.Clamp(intensity, 0f, 2f);
        }

        /// <summary>
        /// Set overall ambient intensity multiplier.
        /// </summary>
        public void SetAmbientIntensity(float intensity)
        {
            ambientIntensity = Mathf.Max(intensity, 0.1f);
        }

        /// <summary>
        /// Set headlight color.
        /// </summary>
        public void SetHeadlightColor(Color color)
        {
            headlightColor = color;
            for (int i = 0; i < headlights.Length; i++)
            {
                if (headlights[i] != null)
                    headlights[i].color = color;
            }
        }

        /// <summary>
        /// Set brake light color.
        /// </summary>
        public void SetBrakeLightColor(Color color)
        {
            brakeLightColor = color;
            for (int i = 0; i < brakelights.Length; i++)
            {
                if (brakelights[i] != null)
                    brakelights[i].color = color;
            }
        }

        /// <summary>
        /// Is it night time? (5 PM to 7 AM)
        /// </summary>
        public bool IsNightTime() => timeOfDay < 7f || timeOfDay >= 17f;

        /// <summary>
        /// Get headlights active state.
        /// </summary>
        public bool AreHeadlightsOn() => headlightsOn;

        /// <summary>
        /// Get brake lights active state.
        /// </summary>
        public bool AreBrakeLightsActive() => brakeLightsActive;

        public Color GetHeadlightColor() => headlightColor;
        public Color GetBrakeLightColor() => brakeLightColor;
    }
}
