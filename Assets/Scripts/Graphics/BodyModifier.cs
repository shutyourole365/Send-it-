using UnityEngine;
using System.Collections.Generic;
using SendIt.Data;

namespace SendIt.Graphics
{
    /// <summary>
    /// Manages vehicle body modifications including wheels, spoilers, bumpers, and body kits.
    /// Handles swapping visual components and adjusting their parameters.
    /// </summary>
    public class BodyModifier : MonoBehaviour
    {
        [SerializeField] private Transform[] wheelSlots = new Transform[4]; // FL, FR, RL, RR
        [SerializeField] private Transform spoilerSlot;
        [SerializeField] private Transform bumperSlot;

        // Body modification state
        private int wheelSize = 18;
        private float wheelOffset = 0f;
        private int bumperStyle = 0;
        private int bodyKitStyle = 0;
        private float spoilerHeight = 0f;
        private float spoilerAngle = 0f;

        // Visual component prefabs/models
        private Dictionary<int, GameObject> wheelModels = new Dictionary<int, GameObject>();
        private Dictionary<int, GameObject> bumperModels = new Dictionary<int, GameObject>();
        private Dictionary<int, GameObject> bodyKitModels = new Dictionary<int, GameObject>();

        private GameObject currentWheelInstance;
        private GameObject currentBumperInstance;
        private GameObject currentBodyKitInstance;

        public struct BodyModSettings
        {
            public int WheelSize;
            public float WheelOffset;
            public int BumperStyle;
            public int BodyKitStyle;
            public float SpoilerHeight;
            public float SpoilerAngle;
        }

        public void Initialize(GraphicsData graphicsData)
        {
            wheelSize = graphicsData.WheelSize;
            wheelOffset = graphicsData.WheelOffset;
            bumperStyle = graphicsData.BumperStyle;
            bodyKitStyle = graphicsData.BodyKitStyle;
            spoilerHeight = graphicsData.SpoilerHeight;
            spoilerAngle = graphicsData.SpoilerAngle;

            ApplyBodyModifications();
        }

        /// <summary>
        /// Set wheel size in inches.
        /// </summary>
        public void SetWheelSize(int sizeInches)
        {
            wheelSize = Mathf.Clamp(sizeInches, 15, 22);
            UpdateWheelSize();
        }

        /// <summary>
        /// Set wheel offset in millimeters.
        /// </summary>
        public void SetWheelOffset(float offsetMM)
        {
            wheelOffset = Mathf.Clamp(offsetMM, -50f, 50f);
            UpdateWheelPosition();
        }

        /// <summary>
        /// Set bumper style/variant.
        /// </summary>
        public void SetBumperStyle(int style)
        {
            bumperStyle = Mathf.Max(0, style);
            UpdateBumper();
        }

        /// <summary>
        /// Set body kit style/variant.
        /// </summary>
        public void SetBodyKitStyle(int style)
        {
            bodyKitStyle = Mathf.Max(0, style);
            UpdateBodyKit();
        }

        /// <summary>
        /// Set spoiler height in millimeters.
        /// </summary>
        public void SetSpoilerHeight(float heightMM)
        {
            spoilerHeight = Mathf.Clamp(heightMM, 0f, 200f);
            UpdateSpoiler();
        }

        /// <summary>
        /// Set spoiler angle in degrees.
        /// </summary>
        public void SetSpoilerAngle(float angleDegrees)
        {
            spoilerAngle = Mathf.Clamp(angleDegrees, 0f, 45f);
            UpdateSpoiler();
        }

        /// <summary>
        /// Apply all body modifications.
        /// </summary>
        private void ApplyBodyModifications()
        {
            UpdateWheels();
            UpdateBumper();
            UpdateBodyKit();
            UpdateSpoiler();
        }

        /// <summary>
        /// Update wheel visuals based on size and offset.
        /// </summary>
        private void UpdateWheels()
        {
            UpdateWheelSize();
            UpdateWheelPosition();
        }

        /// <summary>
        /// Update wheel size scale.
        /// </summary>
        private void UpdateWheelSize()
        {
            // Calculate scale from wheel size (18" is baseline 1.0)
            float sizeScale = wheelSize / 18f;

            for (int i = 0; i < wheelSlots.Length; i++)
            {
                if (wheelSlots[i] != null)
                {
                    wheelSlots[i].localScale = Vector3.one * sizeScale;
                }
            }
        }

        /// <summary>
        /// Update wheel position based on offset.
        /// </summary>
        private void UpdateWheelPosition()
        {
            // Convert offset from mm to world units (assuming 1 unit = 1 meter)
            float offsetUnits = wheelOffset / 1000f;

            for (int i = 0; i < wheelSlots.Length; i++)
            {
                if (wheelSlots[i] != null)
                {
                    // Left wheels get negative offset, right wheels get positive
                    float direction = (i == 1 || i == 3) ? 1f : -1f;
                    Vector3 pos = wheelSlots[i].localPosition;
                    pos.x = offsetUnits * direction;
                    wheelSlots[i].localPosition = pos;
                }
            }
        }

        /// <summary>
        /// Update bumper visual component.
        /// </summary>
        private void UpdateBumper()
        {
            if (bumperSlot == null)
                return;

            // Destroy current bumper
            if (currentBumperInstance != null)
            {
                Destroy(currentBumperInstance);
            }

            // Instantiate new bumper based on style
            // In a full implementation, load from resources or asset database
            currentBumperInstance = new GameObject($"Bumper_Style{bumperStyle}");
            currentBumperInstance.transform.SetParent(bumperSlot);
            currentBumperInstance.transform.localPosition = Vector3.zero;
            currentBumperInstance.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Update body kit visual component.
        /// </summary>
        private void UpdateBodyKit()
        {
            if (transform == null)
                return;

            // Destroy current body kit
            if (currentBodyKitInstance != null)
            {
                Destroy(currentBodyKitInstance);
            }

            // Instantiate new body kit based on style
            currentBodyKitInstance = new GameObject($"BodyKit_Style{bodyKitStyle}");
            currentBodyKitInstance.transform.SetParent(transform);
            currentBodyKitInstance.transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// Update spoiler position and angle.
        /// </summary>
        private void UpdateSpoiler()
        {
            if (spoilerSlot == null)
                return;

            // Adjust spoiler height (Z position)
            Vector3 pos = spoilerSlot.localPosition;
            pos.y = spoilerHeight / 1000f; // Convert mm to world units
            spoilerSlot.localPosition = pos;

            // Adjust spoiler angle
            Vector3 rot = spoilerSlot.localEulerAngles;
            rot.x = spoilerAngle;
            spoilerSlot.localEulerAngles = rot;
        }

        /// <summary>
        /// Get current body modification settings.
        /// </summary>
        public BodyModSettings GetBodyModSettings()
        {
            return new BodyModSettings
            {
                WheelSize = wheelSize,
                WheelOffset = wheelOffset,
                BumperStyle = bumperStyle,
                BodyKitStyle = bodyKitStyle,
                SpoilerHeight = spoilerHeight,
                SpoilerAngle = spoilerAngle
            };
        }

        // Getters
        public int GetWheelSize() => wheelSize;
        public float GetWheelOffset() => wheelOffset;
        public int GetBumperStyle() => bumperStyle;
        public int GetBodyKitStyle() => bodyKitStyle;
        public float GetSpoilerHeight() => spoilerHeight;
        public float GetSpoilerAngle() => spoilerAngle;
    }
}
