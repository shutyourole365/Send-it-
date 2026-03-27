# Phase 5: Advanced Rendering & Effects

## Overview

Phase 5 introduces sophisticated visual effects systems that bring the driving experience to life. Five integrated rendering systems provide realistic motion blur, depth of field, particle effects, dynamic lighting, and advanced shadows for a cinematic presentation.

## New Systems

### 1. MotionBlurEffect

**Purpose**: Realistic motion blur based on vehicle velocity and camera movement.

**Key Features**:
- Velocity buffer management
- Frame accumulation and temporal filtering
- Adjustable sample count (4-16 samples)
- Shutter angle control (0-360°)
- Smooth blur transitions based on speed

**Physics Model**:
- Blur amount = (vehicle_speed / max_blur_speed) × intensity²
- Non-linear response at higher speeds
- Relative motion between camera and vehicle
- Frame blending for smooth accumulation

**Usage**:
```csharp
MotionBlurEffect blur = renderingEffects.GetMotionBlurEffect();
blur.SetBlurIntensity(0.7f);        // 0-1 intensity
blur.SetSampleCount(8);              // More samples = better quality
blur.SetShutterAngle(180f);          // 0-360° shutter angle
```

**Performance**: Medium impact, highly configurable via sample count

---

### 2. DepthOfFieldEffect

**Purpose**: Simulate camera focus with realistic bokeh blur and auto-focus tracking.

**Key Features**:
- Auto-focus tracking with smooth transitions
- Circle of Confusion (CoC) calculation
- Multiple bokeh shapes (Circle, Hexagon, Octagon, Diamond)
- f-stop aperture simulation (1.4-32.0)
- Focus range and falloff distance

**Focus Distance Calculation**:
- CoC = (aperture / focal_length) × distance_blur_factor
- Blur peaks at focus_distance ± focus_range

**Bokeh Shapes**:
- **Circle**: Natural bokeh, smooth appearance
- **Hexagon**: Camera lens aperture shape
- **Octagon**: Cinematic multi-blade effect
- **Diamond**: Stylized diamond aperture

**Usage**:
```csharp
DepthOfFieldEffect dof = renderingEffects.GetDepthOfFieldEffect();
dof.SetAutoFocus(true);                      // Enable auto-focus on vehicle
dof.SetApertureSize(5.6f);                   // f/5.6 aperture
dof.SetDOFIntensity(0.8f);                   // 0-1 effect strength
dof.SetBokehShape(DepthOfFieldEffect.BokehShape.Circle);
dof.SetFocusTrackingSpeed(3f);               // Fast focus transitions
```

**Performance**: Medium impact, depends on bokeh samples and blur radius

---

### 3. ParticleEffectSystem

**Purpose**: Dynamic visual feedback from tire slip, impacts, and environmental interaction.

**Particle Types**:
- **TireSmoke**: From tire slip and burnout
- **TireDust**: From gravel/dirt surfaces
- **WaterSpray**: From high-speed wet road driving
- **Sparks**: From collision impacts
- **EngineSmoke**: From engine overheating
- **BrakeGlow**: From brake heat radiation
- **RoadSpray**: General surface spray effects
- **Dirt**: Dirt accumulation on vehicle

**Trigger Conditions**:

| Effect | Trigger | Intensity Factor |
|--------|---------|-----------------|
| Tire Smoke | Slip > 30% | Temperature × Slip |
| Dust | Speed > 20 m/s | Surface loose condition |
| Water | Speed > 15 m/s | Wetness level > 30% |
| Sparks | Impact force > 30 N | Force / threshold |
| Engine | Temp > 100°C | (Temp - 100) / range |
| Brake | Braking pressure | Pressure × Temperature |

**Usage**:
```csharp
ParticleEffectSystem particles = renderingEffects.GetParticleEffectSystem();
particles.SetSmokeDensity(1.0f);              // 0-1 smoke amount
particles.SetDustDensity(0.8f);               // 0-1 dust amount
particles.SetWaterDensity(1.0f);              // 0-1 water spray
particles.SetSparkIntensity(0.6f);            // 0-1 spark emission

// Update tire effects
for (int i = 0; i < 4; i++)
{
    particles.UpdateTireSmoke(i, slipRatio, slipAngle, tireTemp);
}

// Impact sparks
particles.GenerateSparks(hitPoint, hitNormal, impactForce);
```

**Performance**: Low-Medium impact depending on particle count

---

### 4. DynamicLightingSystem

**Purpose**: Real-time lighting adjustments based on vehicle state and time of day.

**Light Components**:
- **Headlights**: Forward-facing lights for night driving
- **Brake Lights**: Red rear lights with intensity response
- **Engine Glow**: Orange/red glow from engine bay
- **Ambient Light**: Time-of-day environmental lighting

**Time-of-Day Cycle**:
```
00:00 - 06:00  Night (dark, cool blues)
06:00 - 09:00  Early morning (warming)
09:00 - 12:00  Morning (brightening)
12:00 - 15:00  Noon (peak brightness, warm white)
15:00 - 18:00  Afternoon (still bright)
18:00 - 21:00  Evening (warming, reddening)
21:00 - 24:00  Night (cooling, darkening)
```

**Light Response**:
- **Headlights**: Auto on/off at dawn/dusk
- **Brake Lights**: Smooth response to braking
- **Engine Glow**: Quadratic response to temperature
- **Ambient**: 24-hour atmospheric cycle

**Usage**:
```csharp
DynamicLightingSystem lighting = renderingEffects.GetDynamicLightingSystem();
lighting.SetTimeOfDay(14.5f);                 // 2:30 PM
lighting.SetHeadlightIntensity(1.5f);         // 0-2 intensity
lighting.SetBrakeLightIntensity(2.0f);        // 0-3 intensity
lighting.SetEngineGlowIntensity(1.0f);        // 0-2 intensity
lighting.SetAmbientIntensity(1.2f);           // Multiplier

// Update based on vehicle state
lighting.UpdateLighting(isBraking, engineTemp, vehicleSpeed);

// Check time of day
if (lighting.IsNightTime())
    EnableHeadlights();
```

**Performance**: Low impact, primarily state management

---

### 5. AdvancedShadowSystem

**Purpose**: High-quality shadows with dynamic optimization and multiple quality levels.

**Shadow Quality Levels**:

| Level | Resolution | Distance | Cascades | Contact Shadows |
|-------|-----------|----------|----------|-----------------|
| Low | 512 | 30m | 1 | No |
| Medium | 1024 | 60m | 2 | No |
| High | 2048 | 100m | 4 | Yes |
| Ultra | 4096 | 200m | 4 | Yes (8 samples) |

**Features**:
- **Cascaded Shadows**: Multiple shadow maps at different scales
- **Contact Shadows**: Dark areas where objects touch surfaces
- **Dynamic Distance**: Adjusts shadow distance based on vehicle speed
- **Shadow Bias**: Configurable to reduce acne/peter panning
- **Reflection Probes**: Real-time reflection updates

**Cascade System**:
- Cascade 0: Close range (detailed shadows)
- Cascade 1: Medium range
- Cascade 2: Far range
- Cascade 3: Horizon (optional)

**Contact Shadow Calculation**:
```
intensity = shadow_fade × normal_factor × thickness
shadow_fade = 1 - (distance_to_ground / max_distance)
```

**Usage**:
```csharp
AdvancedShadowSystem shadows = renderingEffects.GetAdvancedShadowSystem();

// Set quality level
shadows.SetQuality(AdvancedShadowSystem.ShadowQuality.High);

// Configure contact shadows
shadows.SetContactShadows(true, contactDistance: 2f);

// Dynamic optimization
shadows.SetDynamicShadowDistance(true);

// Reflection updates
shadows.SetReflectionProbes(true);
shadows.UpdateReflectionProbes();
```

**Performance Impact**:
- Low: ~2-3ms
- Medium: ~4-5ms
- High: ~6-8ms
- Ultra: ~10-15ms (on high-end hardware)

---

## Integration Pattern

All systems integrate through `RenderingEffects`:

```csharp
// In VehicleController or game manager
void Update()
{
    // Update all effects
    renderingEffects.UpdateEffects(vehicleSpeed, isBraking, engineTemp, tireTemp);

    // Update particle effects per wheel
    for (int i = 0; i < 4; i++)
    {
        renderingEffects.UpdateParticleEffects(i, slipRatio[i], slipAngle[i],
                                               tireTemp[i], vehicleSpeed, onWetSurface);
    }

    // Impact events
    if (collisionDetected)
    {
        renderingEffects.GenerateImpactSparks(impactPoint, impactNormal, impactForce);
    }
}
```

## Performance Guidelines

### Target Performance
- **30 FPS minimum** (console/lower-end PC)
- **60 FPS standard** (mid-range PC)
- **120+ FPS** (high-end PC with all effects)

### Quality Presets

**Performance Mode**:
- Motion Blur: Off or intensity 0.3
- Depth of Field: Off
- Particle Effects: Low density (0.3)
- Dynamic Lighting: Simple (time-based only)
- Shadows: Medium quality

**Balanced Mode** (Recommended):
- Motion Blur: 0.6 intensity, 8 samples
- Depth of Field: Low (3 samples)
- Particle Effects: Medium density (0.7)
- Dynamic Lighting: Full dynamic
- Shadows: High quality

**Quality Mode**:
- Motion Blur: 1.0 intensity, 16 samples
- Depth of Field: Full (8 samples)
- Particle Effects: High density (1.0)
- Dynamic Lighting: Full dynamic with HDR
- Shadows: Ultra quality

### Optimization Tips

1. **Motion Blur**:
   - Reduce sample count on lower-end hardware
   - Disable below 30 FPS

2. **Depth of Field**:
   - Use circle bokeh (fastest)
   - Disable when not in cinematic mode

3. **Particles**:
   - Limit max particles globally
   - Use object pooling
   - Disable distant particle systems

4. **Dynamic Lighting**:
   - Limit light updates to 30 FPS
   - Use light baking for static elements

5. **Shadows**:
   - Use quality presets
   - Enable dynamic distance on mobile
   - Disable contact shadows on lower tiers

## Real-World Examples

### Example 1: High-Speed Chase
```csharp
// Fast motion, cinematic feeling
renderingEffects.SetMotionBlurIntensity(0.8f);
renderingEffects.SetDepthOfFieldIntensity(0.6f);
renderingEffects.SetParticleDensity(1.0f);
```

### Example 2: Burnout Moment
```csharp
// Heavy smoke and sparks
particles.SetSmokeDensity(1.5f);      // Can exceed 1.0 for cinematic
particles.GenerateSparks(position, normal, 100f);
lighting.UpdateLighting(braking: true, engineTemp: 120f, speed: 50f);
```

### Example 3: Night Driving
```csharp
// Headlights on, dark ambient
lighting.SetTimeOfDay(22f);          // 10 PM
lighting.SetHeadlightIntensity(1.5f);
shadows.SetQuality(AdvancedShadowSystem.ShadowQuality.High);
```

### Example 4: Wet Surface Racing
```csharp
// Water spray, lower vision
particles.UpdateWaterSpray(speed: 30f, onWetSurface: true, position);
dof.SetFocusDistance(20f);           // Focus further ahead
shadows.UpdateReflectionProbes();    // Reflections of wet track
```

## Telemetry Fields

All systems expose state for monitoring and UI:

```csharp
// Motion blur
var blurFactor = motionBlur.GetBlurFactor();
var motionVector = motionBlur.GetMotionVector();

// Depth of field
var dofState = dof.GetDOFState(); // (focusDistance, blurAmount, shape)

// Particles
var smokeRate = particles.GetEmissionRate(ParticleType.TireSmoke);

// Lighting
var isNight = lighting.IsNightTime();
var headlightsOn = lighting.AreHeadlightsOn();

// Shadows
var shadowQuality = shadows.GetCurrentQuality();
var shadowDistance = shadows.GetShadowDistance();
```

## Performance Profiling

Use these methods to monitor performance:

```csharp
// In debug UI
string GetEffectStatus()
{
    return $@"
Motion Blur: {renderingEffects.GetMotionBlurEffect().GetBlurIntensity():F2}
DoF Distance: {renderingEffects.GetDepthOfFieldEffect().GetFocusDistance():F1}m
Particles: {renderingEffects.GetParticleEffectSystem().IsInitialized}
Time: {renderingEffects.GetDynamicLightingSystem().GetTimeOfDay():F1}
Shadows: {renderingEffects.GetAdvancedShadowSystem().GetCurrentQuality()}
    ";
}
```

## Future Enhancements

- [ ] Lens flare and anamorphic effects
- [ ] Chromatic aberration for high-speed
- [ ] Volumetric lighting (god rays)
- [ ] Dust particle physics
- [ ] Screen-space ambient occlusion (SSAO)
- [ ] Temporal anti-aliasing (TAA)
- [ ] Ray-traced reflections
- [ ] Real-time global illumination

---

**Phase 5 Status**: ✓ Complete and integrated
**Revision**: 1.0
**Last Updated**: 2026-03-27
