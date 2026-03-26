using UnityEngine;
using System.Collections.Generic;

namespace SendIt.Customization
{
    /// <summary>
    /// Manages boot/trunk customization including cargo space,
    /// sound deadening, carpet options, and storage solutions.
    /// </summary>
    public class BootModifier : MonoBehaviour
    {
        [SerializeField] private Transform bootTransform;
        [SerializeField] private Renderer[] bootRenderers;

        // Boot customization state
        private int cargoSetup = 0; // 0=Stock, 1=Minimal, 2=Enhanced, 3=Full
        private int soundDeadeningLevel = 0; // 0=None, 1=Standard, 2=Premium, 3=Full
        private int carpetType = 0; // 0=Rubber, 1=Carpet, 2=Leather, 3=Carbon
        private float spareWheelIncluded = 1f; // 0=None, 1=Compact, 2=Full-size
        private bool hasToolKit = false;
        private bool hasEmergencyKit = false;
        private bool hasAmpMounting = false;
        private int amplifierSize = 0; // 0=None, 1=Small, 2=Medium, 3=Large

        // Cargo capacity tracking
        private float maxCargoCapacity = 300f; // kg
        private float currentCargoWeight = 0f;

        // Audio dampening effect
        private float soundDampeningFactor = 0f; // 0-1 (affects ambient noise reduction)

        [System.Serializable]
        public struct BootSettings
        {
            public int CargoSetup;
            public int SoundDeadeningLevel;
            public int CarpetType;
            public float SpareWheelIncluded;
            public bool HasToolKit;
            public bool HasEmergencyKit;
            public bool HasAmpMounting;
            public int AmplifierSize;
            public float CurrentCargoWeight;
        }

        /// <summary>
        /// Initialize boot customizer.
        /// </summary>
        public void Initialize()
        {
            if (bootRenderers == null || bootRenderers.Length == 0)
            {
                bootRenderers = GetComponentsInChildren<Renderer>();
            }

            ApplyBootSettings();
        }

        /// <summary>
        /// Set cargo setup configuration.
        /// </summary>
        public void SetCargoSetup(int setup)
        {
            cargoSetup = Mathf.Clamp(setup, 0, 3);
            UpdateCargoCapacity();
            ApplyBootSettings();
        }

        /// <summary>
        /// Set sound deadening level (improves audio quality).
        /// </summary>
        public void SetSoundDeadening(int level)
        {
            soundDeadeningLevel = Mathf.Clamp(level, 0, 3);
            UpdateSoundDampening();
        }

        /// <summary>
        /// Set boot carpet type.
        /// </summary>
        public void SetCarpetType(int type)
        {
            carpetType = Mathf.Clamp(type, 0, 3);
            ApplyBootSettings();
        }

        /// <summary>
        /// Include/exclude spare wheel.
        /// </summary>
        public void SetSpareWheel(float wheelType)
        {
            spareWheelIncluded = Mathf.Clamp(wheelType, 0f, 2f);
            UpdateCargoCapacity();
        }

        /// <summary>
        /// Add/remove tool kit (affects weight).
        /// </summary>
        public void SetToolKit(bool included)
        {
            hasToolKit = included;
            UpdateCargoWeight();
        }

        /// <summary>
        /// Add/remove emergency kit.
        /// </summary>
        public void SetEmergencyKit(bool included)
        {
            hasEmergencyKit = included;
            UpdateCargoWeight();
        }

        /// <summary>
        /// Enable/disable amplifier mounting.
        /// </summary>
        public void SetAmpMounting(bool enabled)
        {
            hasAmpMounting = enabled;
            if (!enabled)
                amplifierSize = 0;
            UpdateCargoCapacity();
        }

        /// <summary>
        /// Set amplifier size.
        /// </summary>
        public void SetAmplifierSize(int size)
        {
            amplifierSize = Mathf.Clamp(size, 0, 3);
            if (amplifierSize > 0)
                hasAmpMounting = true;
            UpdateCargoCapacity();
        }

        /// <summary>
        /// Update cargo capacity based on configuration.
        /// </summary>
        private void UpdateCargoCapacity()
        {
            // Base capacity varies by setup
            float baseCapacity = cargoSetup switch
            {
                0 => 300f, // Stock
                1 => 250f, // Minimal (removed seat, less space)
                2 => 350f, // Enhanced (optimized layout)
                3 => 400f, // Full (maximum cargo area)
                _ => 300f
            };

            // Reduce capacity for spare wheel
            if (spareWheelIncluded == 1f)
                baseCapacity -= 40f; // Compact spare
            else if (spareWheelIncluded == 2f)
                baseCapacity -= 80f; // Full-size spare

            // Reduce capacity for amplifier
            if (amplifierSize > 0)
            {
                baseCapacity -= (amplifierSize * 30f);
            }

            maxCargoCapacity = Mathf.Max(50f, baseCapacity); // Minimum 50kg
        }

        /// <summary>
        /// Update current cargo weight based on items.
        /// </summary>
        private void UpdateCargoWeight()
        {
            currentCargoWeight = 0f;

            // Add weights for items
            if (spareWheelIncluded == 1f)
                currentCargoWeight += 40f;
            else if (spareWheelIncluded == 2f)
                currentCargoWeight += 80f;

            if (hasToolKit)
                currentCargoWeight += 15f;

            if (hasEmergencyKit)
                currentCargoWeight += 10f;

            if (amplifierSize > 0)
            {
                currentCargoWeight += (amplifierSize * 15f);
            }
        }

        /// <summary>
        /// Update sound dampening effect on audio.
        /// </summary>
        private void UpdateSoundDampening()
        {
            // Higher sound deadening = more ambient noise reduction
            soundDampeningFactor = soundDeadeningLevel * 0.25f; // 0, 0.25, 0.5, 0.75

            // This would affect:
            // - Engine noise reduction
            // - Wind/road noise reduction
            // - Interior acoustics
            // - Audio system clarity
        }

        /// <summary>
        /// Apply all boot settings visually.
        /// </summary>
        private void ApplyBootSettings()
        {
            if (bootRenderers == null || bootRenderers.Length == 0)
                return;

            foreach (var renderer in bootRenderers)
            {
                if (renderer != null && renderer.material != null)
                {
                    // Apply carpet type appearance
                    switch (carpetType)
                    {
                        case 0: // Rubber
                            renderer.material.color = new Color(0.2f, 0.2f, 0.2f);
                            renderer.material.SetFloat("_Smoothness", 0.3f);
                            break;
                        case 1: // Carpet
                            renderer.material.color = new Color(0.3f, 0.3f, 0.3f);
                            renderer.material.SetFloat("_Smoothness", 0.2f);
                            break;
                        case 2: // Leather
                            renderer.material.color = new Color(0.4f, 0.4f, 0.4f);
                            renderer.material.SetFloat("_Smoothness", 0.5f);
                            break;
                        case 3: // Carbon
                            renderer.material.color = new Color(0.1f, 0.1f, 0.1f);
                            renderer.material.SetFloat("_Smoothness", 0.7f);
                            renderer.material.SetFloat("_Metallic", 0.3f);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Get current boot settings.
        /// </summary>
        public BootSettings GetBootSettings()
        {
            return new BootSettings
            {
                CargoSetup = cargoSetup,
                SoundDeadeningLevel = soundDeadeningLevel,
                CarpetType = carpetType,
                SpareWheelIncluded = spareWheelIncluded,
                HasToolKit = hasToolKit,
                HasEmergencyKit = hasEmergencyKit,
                HasAmpMounting = hasAmpMounting,
                AmplifierSize = amplifierSize,
                CurrentCargoWeight = currentCargoWeight
            };
        }

        /// <summary>
        /// Get cargo capacity information.
        /// </summary>
        public string GetCargoInfo()
        {
            float availableSpace = maxCargoCapacity - currentCargoWeight;
            return $"Cargo: {currentCargoWeight:F0}kg / {maxCargoCapacity:F0}kg (Available: {Mathf.Max(0, availableSpace):F0}kg)";
        }

        /// <summary>
        /// Get sound deadening quality rating.
        /// </summary>
        public float GetSoundDampeningFactor() => soundDampeningFactor;

        // Getters
        public int GetCargoSetup() => cargoSetup;
        public int GetSoundDeadeningLevel() => soundDeadeningLevel;
        public int GetCarpetType() => carpetType;
        public float GetMaxCargoCapacity() => maxCargoCapacity;
        public float GetCurrentCargoWeight() => currentCargoWeight;
    }
}
