# Phase 2 Quick Start Guide

## What's New

Phase 2 adds 5 advanced tire physics systems that work together to provide realistic vehicle dynamics:

1. **TireSlipDynamics** - Advanced slip curves with load sensitivity
2. **TireTemperatureSystem** - Multi-zone tire heating and cooling
3. **TireWearPatterns** - Location-based wear and tread depth
4. **TirePressureSystem** - Pressure effects on grip and wear
5. **SurfaceConditionsSystem** - Road surface modifiers

## Quick Integration

All systems are automatically integrated into the `Tire` class:

```csharp
// In your physics update:
float gripForce = tire.CalculateGripForce(slipAngleDegrees, normalLoad, velocity);
```

The grip force now accounts for:
- Temperature effects (heating/cooling)
- Wear patterns (tread depth, location-specific wear)
- Tire pressure (temperature-dependent)
- Surface conditions (grip, wetness, aquaplaning)
- Slip dynamics (load transfer, grip envelope)

## Accessing Systems

Get references to individual subsystems:

```csharp
// Get systems from tire
var slipDynamics = tire.GetSlipDynamics();
var tempSystem = tire.GetTemperatureSystem();
var wearPatterns = tire.GetWearPatterns();
var pressureSystem = tire.GetPressureSystem();
var surfaceConditions = tire.GetSurfaceConditions();
```

## Real-Time Adjustments

### Change Surface
```csharp
tire.SetSurfaceType(SurfaceConditionsSystem.SurfaceType.WetAsphalt);
tire.SetSurfaceWetness(0.7f); // 70% wet
```

### Adjust Tire Pressure
```csharp
tire.SetTirePressure(34f); // 34 PSI (pit stop adjustment)
```

### Monitor Tire Condition
```csharp
float treadDepth = tire.GetWearPatterns().GetTreadDepth();
float temperature = tire.GetTemperatureSystem().GetAverageTemperature();
float pressure = tire.GetPressureSystem().GetCurrentPressure();

if (treadDepth < 4f)
    ShowWarning("Tires are low on tread");
```

## Testing

Run the test suite to verify all systems:

```csharp
// Attach Phase2SystemTests to any GameObject
// Set "Run Tests On Start" to true in inspector
// Check console output for test results
```

Or manually trigger:
```csharp
var tests = gameObject.GetComponent<Phase2SystemTests>();
tests.RunAllTests();
```

## Performance Tips

### For Maximum Grip
1. Heat tires gradually (warm up)
2. Maintain optimal pressure (32 PSI)
3. Avoid extreme slip angles
4. Use high-grip compound (Sport/Slick)
5. Drive on high-grip surface (asphalt)

### For Realistic Handling
1. Let tires warm for 30-60 seconds
2. Account for load transfer in corners
3. Monitor tread depth (replace at 1.6mm)
4. Adjust pressure for conditions

### For Long Tire Life
1. Maintain steady, smooth driving
2. Minimize aggressive cornering
3. Avoid wheel spin and lock
4. Check pressure regularly
5. Use durable compound (Street/AllWeather)

## Common Scenarios

### Cold Start
Tires start at ambient temperature with reduced grip:
```
20°C: 50% grip
↓ (2-3 minutes of driving)
85°C: 115% grip (optimal)
```

### Wet Braking
```csharp
// On wet asphalt
tire.SetSurfaceType(SurfaceConditionsSystem.SurfaceType.WetAsphalt);
tire.SetSurfaceWetness(1.0f);
// Grip reduced to ~65% of dry
// Brake distances increase
// Tire wear is lower
```

### Pressure Drop
```csharp
// Under-pressure reduces grip and increases wear
tire.SetTirePressure(28f); // Below optimal 32 PSI
// Grip: 80% of optimal
// Wear: 3x baseline
// Edge wear pattern develops
```

### High-Speed Cornering
```
Load transfer to outside wheel:
- Inside wheel: reduced load → reduced grip
- Outside wheel: increased load → potential overpressure
```

## Telemetry Fields

All systems expose structured telemetry:

```csharp
// Slip state
var slipState = tire.GetSlipDynamics().GetSlipState();
// → SlipAngle, SlipRatio, PeakSlipAngle, LoadTransfers

// Temperature state
var tempState = tire.GetTemperatureSystem().GetTemperatureState();
// → AverageTemp, ZoneTemps (center, edges, walls), Variance

// Wear state
var wearState = tire.GetWearPatterns().GetWearState();
// → TotalWear, LocationWear, TreadDepth, WearPattern

// Pressure state
var pressureState = tire.GetPressureSystem().GetPressureState();
// → CurrentPressure, GripFactor, WearFactor, Warnings

// Surface properties
var surfaceProps = tire.GetSurfaceConditions().GetSurfaceProperties();
// → GripCoefficient, WearMultiplier, Temperature effects
```

Use these for UI dashboards, telemetry logging, and AI training.

## Debugging

Enable detailed logging in Phase2SystemTests:

```csharp
[SerializeField] private bool runTestsOnStart = true; // Watch console output
```

Check individual system states during gameplay:

```csharp
Debug.Log($"Temp: {tire.GetTemperatureSystem().GetAverageTemperature()}°C");
Debug.Log($"Tread: {tire.GetWearPatterns().GetTreadDepth()}mm");
Debug.Log($"Pressure: {tire.GetPressureSystem().GetCurrentPressure()} PSI");
```

## API Reference

### Tire Class
```csharp
// Grip calculation (main entry point)
float gripForce = tire.CalculateGripForce(slipAngleDeg, normalLoad, velocity);

// Surface/condition setters
tire.SetSurfaceType(surfaceType);
tire.SetSurfaceWetness(wetness);
tire.SetTirePressure(pressurePSI);

// Wear management
tire.ResetWear();
tire.SetWear(amount);

// System getters
TireSlipDynamics slipDynamics = tire.GetSlipDynamics();
TireTemperatureSystem tempSystem = tire.GetTemperatureSystem();
TireWearPatterns wearPatterns = tire.GetWearPatterns();
TirePressureSystem pressureSystem = tire.GetPressureSystem();
SurfaceConditionsSystem surfaceConditions = tire.GetSurfaceConditions();
```

## Next Steps

- [ ] Integrate into UI telemetry display
- [ ] Add tire condition warnings to HUD
- [ ] Implement pit stop tire changes
- [ ] Create setup tuning UI for pressures/compounds
- [ ] Add telemetry recording
- [ ] Implement AI learning from tire states
- [ ] Create performance analysis tools

## Support

For detailed physics documentation, see `PHASE2_DOCUMENTATION.md`

For system behavior verification, run `Phase2SystemTests`

For integration questions, check existing `VehicleController` implementation

---

**Phase 2 Status**: ✓ Complete and integrated
**Revision**: 1.0
**Last Updated**: 2026-03-27
