using UnityEngine;
using System;
using System.Collections.Generic;

namespace SendIt.Data
{
    /// <summary>
    /// Serializable vehicle configuration containing all tuning and customization settings.
    /// This can be saved/loaded from disk or stored in player profiles.
    /// </summary>
    [System.Serializable]
    public class VehicleData
    {
        [SerializeField] private string vehicleName = "Default Vehicle";
        [SerializeField] private string vehicleType = "Sports Car";
        [SerializeField] private long createdTimestamp;
        [SerializeField] private long lastModifiedTimestamp;

        // Physics Parameters
        [SerializeField] private PhysicsData physicsData;

        // Graphics Parameters
        [SerializeField] private GraphicsData graphicsData;

        public VehicleData()
        {
            physicsData = new PhysicsData();
            graphicsData = new GraphicsData();
            createdTimestamp = System.DateTime.UtcNow.Ticks;
            lastModifiedTimestamp = createdTimestamp;
        }

        public void MarkModified()
        {
            lastModifiedTimestamp = System.DateTime.UtcNow.Ticks;
        }

        public string VehicleName => vehicleName;
        public string VehicleType => vehicleType;
        public PhysicsData Physics => physicsData;
        public GraphicsData Graphics => graphicsData;
        public long CreatedTimestamp => createdTimestamp;
        public long LastModifiedTimestamp => lastModifiedTimestamp;

        public void SetVehicleName(string name) => vehicleName = name;
        public void SetVehicleType(string type) => vehicleType = type;
    }

    /// <summary>
    /// Serializable container for all physics tuning data.
    /// </summary>
    [System.Serializable]
    public class PhysicsData
    {
        // Engine Parameters
        [SerializeField] private float maxRPM = 7000f;
        [SerializeField] private float horsePower = 300f;
        [SerializeField] private float torquePeakRPM = 4000f;
        [SerializeField] private float engineResponsiveness = 1.0f;

        // Transmission Parameters
        [SerializeField] private int gearCount = 5;
        [SerializeField] private float finalDriveRatio = 3.5f;
        [SerializeField] private float shiftSpeed = 0.15f;

        // Suspension Parameters (per corner)
        [SerializeField] private float springStiffness = 20000f;
        [SerializeField] private float compressionDamping = 1.0f;
        [SerializeField] private float extensionDamping = 1.0f;
        [SerializeField] private float rideHeight = 0.3f;
        [SerializeField] private float antiRollBarStiffness = 15000f;

        // Tire Parameters
        [SerializeField] private float tireGripCoefficient = 1.0f;
        [SerializeField] private float tirePeakSlip = 0.15f;
        [SerializeField] private float tireTemperatureSensitivity = 1.0f;
        [SerializeField] private float tireWearRate = 0.001f;

        // Aerodynamics
        [SerializeField] private float dragCoefficient = 0.32f;
        [SerializeField] private float downforceCoefficient = 0.0f;
        [SerializeField] private float spoilerAngle = 0f;

        // Weight Distribution
        [SerializeField] private float totalMass = 1300f;
        [SerializeField] private float frontWeightDistribution = 0.5f; // 0-1, where 0.5 = 50/50

        public float MaxRPM => maxRPM;
        public float HorsePower => horsePower;
        public float TorquePeakRPM => torquePeakRPM;
        public float EngineResponsiveness => engineResponsiveness;
        public int GearCount => gearCount;
        public float FinalDriveRatio => finalDriveRatio;
        public float ShiftSpeed => shiftSpeed;
        public float SpringStiffness => springStiffness;
        public float CompressionDamping => compressionDamping;
        public float ExtensionDamping => extensionDamping;
        public float RideHeight => rideHeight;
        public float AntiRollBarStiffness => antiRollBarStiffness;
        public float TireGripCoefficient => tireGripCoefficient;
        public float TirePeakSlip => tirePeakSlip;
        public float TireTemperatureSensitivity => tireTemperatureSensitivity;
        public float TireWearRate => tireWearRate;
        public float DragCoefficient => dragCoefficient;
        public float DownforceCoefficient => downforceCoefficient;
        public float SpoilerAngle => spoilerAngle;
        public float TotalMass => totalMass;
        public float FrontWeightDistribution => frontWeightDistribution;

        public void SetMaxRPM(float value) => maxRPM = Mathf.Clamp(value, 3000f, 12000f);
        public void SetHorsePower(float value) => horsePower = Mathf.Clamp(value, 50f, 2000f);
        public void SetSpringStiffness(float value) => springStiffness = Mathf.Clamp(value, 5000f, 50000f);
        public void SetTireGripCoefficient(float value) => tireGripCoefficient = Mathf.Clamp(value, 0.5f, 1.5f);
        public void SetDragCoefficient(float value) => dragCoefficient = Mathf.Clamp(value, 0.15f, 0.6f);
    }

    /// <summary>
    /// Serializable container for all graphics customization data.
    /// </summary>
    [System.Serializable]
    public class GraphicsData
    {
        // Paint System
        [SerializeField] private Color baseColor = Color.red;
        [SerializeField] private float metallicIntensity = 0.0f;
        [SerializeField] private float glossiness = 0.5f;
        [SerializeField] private float pearlcentIntensity = 0.0f;

        // Body Modifications
        [SerializeField] private int wheelSize = 18; // inches
        [SerializeField] private float wheelOffset = 0f; // mm
        [SerializeField] private int bumperStyle = 0;
        [SerializeField] private int bodyKitStyle = 0;
        [SerializeField] private float spoilerHeight = 0f;
        [SerializeField] private float spoilerAngle = 0f;

        // Materials & Weathering
        [SerializeField] private float wearAmount = 0f; // 0-1
        [SerializeField] private float dirtAccumulation = 0f; // 0-1
        [SerializeField] private float rustAmount = 0f; // 0-1

        // Effects Settings
        [SerializeField] private bool enableRealTimeShadows = true;
        [SerializeField] private bool enableReflections = true;
        [SerializeField] private bool enableAmbientOcclusion = true;
        [SerializeField] private float motionBlurIntensity = 0.5f;

        public Color BaseColor => baseColor;
        public float MetallicIntensity => metallicIntensity;
        public float Glossiness => glossiness;
        public float PearlcentIntensity => pearlcentIntensity;
        public int WheelSize => wheelSize;
        public float WheelOffset => wheelOffset;
        public int BumperStyle => bumperStyle;
        public int BodyKitStyle => bodyKitStyle;
        public float SpoilerHeight => spoilerHeight;
        public float SpoilerAngle => spoilerAngle;
        public float WearAmount => wearAmount;
        public float DirtAccumulation => dirtAccumulation;
        public float RustAmount => rustAmount;
        public bool EnableRealTimeShadows => enableRealTimeShadows;
        public bool EnableReflections => enableReflections;
        public bool EnableAmbientOcclusion => enableAmbientOcclusion;
        public float MotionBlurIntensity => motionBlurIntensity;

        public void SetBaseColor(Color color) => baseColor = color;
        public void SetMetallicIntensity(float value) => metallicIntensity = Mathf.Clamp01(value);
        public void SetGlossiness(float value) => glossiness = Mathf.Clamp01(value);
        public void SetWheelSize(int inches) => wheelSize = Mathf.Clamp(inches, 15, 22);
        public void SetWearAmount(float value) => wearAmount = Mathf.Clamp01(value);
    }
}
