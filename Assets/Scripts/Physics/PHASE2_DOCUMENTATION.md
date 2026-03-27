# Phase 2: Advanced Physics Implementation

## Overview

Phase 2 introduces sophisticated tire physics systems that simulate realistic vehicle dynamics with advanced slip, temperature, wear, pressure, and surface condition effects. All systems are integrated into the main `Tire` class and work together to provide an authentic driving experience.

## New Systems

### 1. TireSlipDynamics

**Purpose**: Calculate realistic slip behavior with load sensitivity and grip envelope effects.

**Key Features**:
- Load-dependent peak slip angles
- Slip angle and ratio calculations with smooth damping
- Load transfer effects (lateral and longitudinal)
- Grip factor based on combined lateral/longitudinal slip
- Parabolic grip curve with peak at optimal slip

**Usage**:
```csharp
TireSlipDynamics slipDynamics = tire.GetSlipDynamics();
float gripFactor = slipDynamics.GetGripFactor(); // 0-1, peaks at 1.0
float adjustedLoad = slipDynamics.GetAdjustedNormalLoad(baseLoad, wheelIndex);
```

**Physics Model**:
- Peak slip angle: ~8° (decreases with higher load)
- Peak slip ratio: ~15% (increases slightly with load)
- Load sensitivity: √(Load_reference / Load_current) relationship
- Grip envelope: min(lateral_factor, longitudinal_factor)

---

### 2. TireTemperatureSystem

**Purpose**: Simulate localized tire temperature zones and their effects on grip and wear.

**Key Features**:
- 5 temperature zones: center, edges (left/right), walls (inner/outer)
- Heat generation from friction and slip
- Realistic cooling through convection and radiation
- Temperature-dependent grip curve (cold → warm → overheat)
- Wear rate multiplication based on temperature

**Temperature Zones**:
- **Center**: Primary contact area, heats quickly from longitudinal slip
- **Edges**: Side contact, heated by lateral slip and cornering
- **Walls**: Inner/outer walls, affected by brake heat and extreme slip

**Usage**:
```csharp
TireTemperatureSystem tempSystem = tire.GetTemperatureSystem();
float avgTemp = tempSystem.GetAverageTemperature();
float gripFactor = tempSystem.GetTemperatureGripFactor(); // 1.15 peak at 85°C
float wearFactor = tempSystem.GetTemperatureWearFactor();
```

**Temperature Curve**:
- 20°C (cold): 50% grip
- 40°C (warming): 90% grip
- 85°C (optimal): 115% grip (peak)
- 100°C: 100% grip
- 130°C (overheated): 70% grip

---

### 3. TireWearPatterns

**Purpose**: Track location-specific tire wear and simulate tread depth degradation.

**Key Features**:
- Location-based wear tracking (center, edges, walls)
- Tread depth simulation (8mm new to 1.6mm minimum)
- Compound selection (Street, Sport, Slick, AllWeather)
- Wear pattern detection (even vs. uneven wear)
- Slippery condition warning at 4mm tread depth

**Tire Compounds**:
| Compound | Wear Rate | Grip | Temp Sensitivity | Use Case |
|----------|-----------|------|-----------------|----------|
| Street | 0.0008 | Medium | Low | Daily driving |
| Sport | 0.0015 | High | Medium | Performance |
| Slick | 0.002 | Very High | High | Racing |
| AllWeather | 0.0006 | Medium | Very Low | All conditions |

**Usage**:
```csharp
TireWearPatterns wearPatterns = tire.GetWearPatterns();
float wearGripFactor = wearPatterns.GetWearGripFactor(); // Grip loss from wear
float treadDepth = wearPatterns.GetTreadDepth(); // mm
bool shouldReplace = wearPatterns.ShouldReplaceTire(); // At 1.6mm

if (wearPatterns.GetTireWarningLevel() > 0.5f)
    DisplayWarning("Tires are low on tread");
```

**Wear Effects**:
- Up to 35% grip loss from total wear
- Additional 50% loss when below 4mm tread depth
- Location-specific wear patterns affect handling

---

### 4. TirePressureSystem

**Purpose**: Simulate pressure effects on grip, wear, and safety.

**Key Features**:
- Temperature-pressure relationship (ideal gas law)
- Grip penalty for under/over-pressure
- Wear multiplier changes with pressure
- Pressure-dependent temperature effects
- Blowout risk detection

**Pressure Effects**:
- **Optimal (32 PSI)**: 100% grip, 1x wear
- **Under-pressure (28 PSI)**: 80% grip, 3x wear (edge wear)
- **Over-pressure (38 PSI)**: 70% grip, 2.5x wear (center wear)

**Usage**:
```csharp
TirePressureSystem pressureSystem = tire.GetPressureSystem();
float currentPressure = pressureSystem.GetCurrentPressure();
float gripFactor = pressureSystem.GetPressureGripFactor();

if (pressureSystem.IsBlowoutRisk())
    HandleEmergency();

// Pit stop adjustment
tire.SetTirePressure(34f); // Increase pressure
```

**Physics Model**:
- P₂ = P₁ × (T₂/T₁) ideal gas law
- Parabolic grip loss for under-pressure
- Linear grip loss for over-pressure
- Pressure changes ~0.1 PSI per °C

---

### 5. SurfaceConditionsSystem

**Purpose**: Simulate different road surfaces and environmental effects.

**Key Features**:
- 10 surface types with unique properties
- Wetness levels and dynamic grip changes
- Temperature effects on grip
- Aquaplaning simulation
- Surface bumpiness for vibration

**Surface Types**:
| Surface | Grip | Wear | Temperature | Aquaplaning Speed |
|---------|------|------|-------------|------------------|
| Dry Asphalt | 1.0 | 1.0 | 1.0 | 100 km/h |
| Wet Asphalt | 0.65 | 0.8 | 0.7 | 60 km/h |
| Gravel | 0.55 | 2.0 | 1.2 | N/A |
| Ice | 0.15 | 0.5 | 0.3 | 10 km/h |
| Concrete | 0.95 | 1.1 | 1.05 | 90 km/h |

**Usage**:
```csharp
SurfaceConditionsSystem surface = tire.GetSurfaceConditions();
tire.SetSurfaceType(SurfaceConditionsSystem.SurfaceType.WetAsphalt);
tire.SetSurfaceWetness(0.8f); // 80% wet

if (surface.IsAquaplaning(vehicleSpeed))
    WarningSystem.ShowAquaplaningWarning();
```

**Wetness Effects**:
- 0 = Dry (full grip)
- 0.3-0.7 = Damp/Wet (reduced grip)
- 1.0 = Soaking wet (minimum grip)

---

## System Integration

All systems work together in the `Tire.CalculateGripForce()` method:

```csharp
// Base Pacejka grip
baseGripForce = PacejkaLateralForce(slipAngleDegrees, normalLoad)

// Apply multipliers
finalGrip = baseGripForce
    × tempFactor          // Temperature (0.5-1.15)
    × wearFactor          // Wear (0.2-1.0)
    × pressureFactor      // Pressure (0.6-1.0)
    × surfaceGrip         // Surface (0.15-1.0)
    × slipGripFactor      // Slip envelope (0-1.0)
```

## Telemetry Integration

All systems expose their state through structured telemetry:

```csharp
var tempState = tempSystem.GetTemperatureState();
var wearState = wearPatterns.GetWearState();
var pressureState = pressureSystem.GetPressureState();
var slipState = slipDynamics.GetSlipState();
var surfaceProps = surface.GetSurfaceProperties();
```

Use these for:
- Dashboard displays
- Telemetry logging
- AI learning
- Balance and tuning

## Practical Scenarios

### Scenario 1: Cold Start
- Tires at 20°C
- Grip penalty: ~50%
- Gradually warm to optimal over 2-3 minutes
- Handle becomes responsive as tires heat

### Scenario 2: High-Speed Cornering
- Load transfer to outside wheels
- Inside wheel: reduced grip
- Outside wheel: increased grip up to limit
- Edge wear on loaded tire

### Scenario 3: Aggressive Braking
- Center and inner wall heating
- Slip ratio increases (wheel lock risk)
- Temperature spikes cause wear acceleration
- Pressure changes from heat

### Scenario 4: Wet Track
- Grip reduced to ~65% baseline
- Aquaplaning risk at high speeds
- Lower tire wear due to water cushion
- Extended braking distances

### Scenario 5: Low Tire Pressure
- Handling becomes vague
- Increased heat generation
- Edge wear pattern develops
- Warning system alerts driver

## Performance Tuning Tips

### For Better Grip:
1. Warm tires gradually (building temp)
2. Maintain optimal pressure (32 PSI)
3. Avoid extreme slip (use load transfer)
4. Choose grip-focused compound (Slick)

### For Longer Tire Life:
1. Minimize temperature spikes
2. Avoid aggressive cornering (lateral slip)
3. Smooth acceleration/braking
4. Maintain pressure
5. Use durable compound (Street/AllWeather)

### For Realistic Handling:
1. Adjust surface conditions
2. Set appropriate tire pressure
3. Let tires warm gradually
4. Account for load transfer
5. Monitor tread depth

## Technical Details

### Load Transfer Formula
```
Lateral Transfer = (Lateral Accel × CoG Height) / Vehicle Width
Longitudinal Transfer = (Longitudinal Accel × CoG Height) / Wheelbase
```

### Temperature Heating
```
Heat = Friction Force × Relative Velocity + Slip Magnitude²
Heating Rate ∝ Speed + Slip + Load
Cooling Rate ∝ Vehicle Speed
```

### Grip Envelope
```
Final Grip = MIN(
    Lateral Grip (from slip angle),
    Longitudinal Grip (from slip ratio)
)
```

This ensures realistic behavior where using all lateral grip reduces longitudinal grip.

## Future Enhancements

- [ ] Tire durability/damage model
- [ ] Wet weather spray effects
- [ ] Snow/ice special handling
- [ ] Tire temperature distribution visualization
- [ ] Adaptive suspension tuning based on tire state
- [ ] Tire compound cycling strategy
- [ ] Surface friction database expansion

---

## Credits

**Phase 2 Implementation**: Advanced physics subsystems
**Based on**: Pacejka Magic Formula, realistic tire physics models
**References**: Tire friction models, load transfer calculations, thermal dynamics

---

## Changelog

### Version 2.0.0
- Initial Phase 2 release
- 5 new physics systems
- Integrated into Tire class
- Full telemetry support
