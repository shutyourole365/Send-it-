# Enhancement 4: Advanced Vehicle Damage System

## Overview

The Advanced Vehicle Damage System simulates realistic damage from impacts and degradation. Components take damage from collisions, which affects vehicle performance and can lead to component failure. Players must manage repairs to maintain vehicle functionality.

## Core Features

### 1. Impact Detection
- Collision-based damage application
- Impact force and speed measurement
- Component identification based on hit location
- Secondary damage to nearby components

### 2. Component Damage Tracking
- 10 tracked components with independent damage states
- Damage accumulation with thresholds
- Performance multipliers based on damage severity
- Functional/non-functional component states

### 3. Performance Penalties
- **Engine Damage**: Up to 50% horsepower reduction
- **Suspension Damage**: Up to 30% drag coefficient increase
- **Aerodynamic Damage**: Up to 60% downforce reduction
- **Brake Damage**: Up to 70% brake force reduction

### 4. Component Failures
- Automatic failure at damage threshold
- Cascading failures affecting gameplay
- Engine destruction = vehicle disabled
- Brake failure = braking disabled
- Transmission failure = gear lock

### 5. Repair System
- Individual component repairs with cost calculation
- Full vehicle repair option
- Repair cost tracking ($1,000 per damage point)
- Gradual repair mechanics

## Component Architecture

### Components Tracked (10 Total)

**Engine System (1)**
- Engine: 100% threshold, critical for vehicle operation

**Suspension System (4)**
- Wheel_0 to Wheel_3: 100% damage threshold
- Suspension_0 to Suspension_3: 80% damage threshold

**Bodywork (2)**
- FrontBumper: 60% damage threshold
- RearBumper: 60% damage threshold

**Aerodynamics (2)**
- FrontWing: 70% damage threshold
- RearWing: 70% damage threshold

**Powertrains (1)**
- Transmission: 90% damage threshold

**Braking (1)**
- BrakeSystem: 80% damage threshold

## Data Structures

### ComponentDamage
```csharp
public struct ComponentDamage
{
    public string ComponentName;              // Unique identifier
    public float DamageAmount;                // 0-1 normalized
    public float MaxDamageThreshold;          // Failure point
    public bool IsFunctional;                 // Operational state
    public float PerformanceMultiplier;       // 0-1, effect on vehicle
    public System.DateTime LastImpactTime;    // Most recent impact
    public int ImpactCount;                   // Total impacts
}
```

### ImpactEvent
```csharp
public struct ImpactEvent
{
    public Vector3 Position;                  // World position
    public Vector3 Normal;                    // Surface normal
    public float ImpactForce;                 // Newtons
    public float ImpactSpeed;                 // m/s
    public string ComponentHit;               // Component name
    public System.DateTime TimeOfImpact;      // Timestamp
}
```

## Damage Calculation

### Impact Damage
```
damageFactor = impactSpeed / 10.0
primaryDamage = damageFactor
secondaryDamage = primaryDamage * 0.5
```

### Performance Penalties
```
engineReduction = engineDamage * 0.5 (max 50%)
dragIncrease = suspensionDamage * 0.3 (max 30%)
downforceReduction = aeroDamage * 0.6 (max 60%)
brakeReduction = brakeDamage * 0.7 (max 70%)
```

### Repair Cost
```
repairCost = damageAmount * 1000
```

## API Reference

### Damage Registration

#### RegisterCollisionImpact
```csharp
void RegisterCollisionImpact(Collision collision)
```
Register a collision and apply damage.

**Parameters:**
- `collision`: Unity Physics collision object

**Usage:**
```csharp
void OnCollisionEnter(Collision collision)
{
    damageSystem.RegisterCollisionImpact(collision);
}
```

### Damage Queries

#### GetComponentDamage
```csharp
float GetComponentDamage(string componentName)
```
Get damage level of specific component (0-1).

**Example:**
```csharp
float engineDamage = damageSystem.GetComponentDamage("Engine");
if (engineDamage > 0.5f)
{
    Debug.Log("Engine severely damaged!");
}
```

#### GetOverallDamage
```csharp
float GetOverallDamage()
```
Get average damage across all components (0-1).

#### GetAllComponentDamage
```csharp
Dictionary<string, ComponentDamage> GetAllComponentDamage()
```
Get damage state of all components.

#### IsComponentFunctional
```csharp
bool IsComponentFunctional(string componentName)
```
Check if component is operational.

#### GetRecentImpacts
```csharp
List<ImpactEvent> GetRecentImpacts()
```
Get last 50 impact events with full details.

### Repairs

#### RepairComponent
```csharp
float RepairComponent(string componentName, float repairAmount = 1.0f)
```
Repair a specific component.

**Parameters:**
- `componentName`: Component to repair
- `repairAmount`: Repair factor (1.0 = full repair)

**Returns:** Repair cost in currency

**Example:**
```csharp
float cost = damageSystem.RepairComponent("Engine", 0.5f);
Debug.Log($"Partial engine repair cost: ${cost}");
```

#### FullRepair
```csharp
float FullRepair()
```
Repair all vehicle damage completely.

**Returns:** Total repair cost

**Example:**
```csharp
float totalCost = damageSystem.FullRepair();
playerCurrency -= totalCost;
```

#### GetTotalRepairCost
```csharp
float GetTotalRepairCost()
```
Get cumulative repair cost from start.

## Damage Thresholds

| Component | Threshold | Impact |
|-----------|-----------|--------|
| Engine | 100% | Vehicle disabled |
| Wheels (4) | 100% | Handling degraded |
| Suspension (4) | 80% | Increased drag |
| Bumpers (2) | 60% | Visual only |
| Wings (2) | 70% | Downforce reduced |
| Transmission | 90% | Gears locked |
| Brakes | 80% | Braking disabled |

## Integration Example

### Complete Damage System Setup

```csharp
public class VehicleManager : MonoBehaviour
{
    private VehicleDamageSystem damageSystem;
    private DamageVisualizationUI damageUI;
    private VehicleController vehicleController;

    void Start()
    {
        damageSystem = GetComponent<VehicleDamageSystem>();
        damageUI = GetComponent<DamageVisualizationUI>();
        vehicleController = GetComponent<VehicleController>();

        damageSystem.Initialize();
        damageUI.Initialize();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Register impact with damage system
        damageSystem.RegisterCollisionImpact(collision);

        // Visual/audio feedback
        PlayCrashSound(collision.relativeVelocity.magnitude);

        // Update UI
        damageUI.RefreshDamageDisplay();

        // Check for critical failures
        if (!damageSystem.IsComponentFunctional("Engine"))
        {
            EndRace("Vehicle disabled!");
        }
    }

    void OpenRepairShop()
    {
        float totalCost = 0f;
        var damageStates = damageSystem.GetAllComponentDamage();

        foreach (var component in damageStates.Values)
        {
            if (component.DamageAmount > 0f)
            {
                totalCost += component.DamageAmount * 1000f;
            }
        }

        if (playerCurrency >= totalCost)
        {
            damageSystem.FullRepair();
            playerCurrency -= totalCost;
            damageUI.RefreshDamageDisplay();
        }
    }
}
```

## Gameplay Mechanics

### Damage Progression

1. **Minor Impact** (1-5 m/s)
   - Minimal damage (cosmetic)
   - No performance impact
   - Visible scratches/dents

2. **Moderate Impact** (5-15 m/s)
   - Component damage 10-30%
   - Performance penalties visible
   - Handling changes noticeable

3. **Severe Impact** (15-25 m/s)
   - Component damage 30-70%
   - Significant performance loss
   - Vehicle still drivable

4. **Critical Impact** (25+ m/s)
   - Component damage 70-100%
   - Potential component failure
   - Vehicle may be inoperable

### Repair Strategy

Players must decide:
- **Full Repair**: Expensive but complete restoration
- **Selective Repair**: Prioritize critical components
- **Accept Damage**: Continue with degraded performance

### Economic Tension

```
Repair Cost = Sum(Component Damage * 1000)
Example: 40% overall damage = $4,000 repair cost
```

## Visual Feedback

### Damage Indicators

1. **Damage Bar**: Shows overall damage (0-100%)
2. **Component List**: Per-component damage status
3. **Color Coding**:
   - Green: Healthy (< 33%)
   - Yellow: Damaged (33-66%)
   - Red: Critical (> 66%)

4. **Status Text**:
   - "Excellent Condition"
   - "Minor Damage"
   - "Moderate Damage"
   - "Severe Damage"
   - "Critical Condition!"

### Impact Feedback

- Audio cue on impact
- Screen shake based on severity
- Particle effects at impact point
- HUD notification of damaged component

## Advanced Features

### Component-Specific Failures

**Engine Failure**: Vehicle immediately disabled, race ends
**Brake Failure**: Braking disabled, must coast to stop
**Transmission Failure**: Current gear locked, no gear changes
**Suspension Failure**: Handling severely affected
**Aero Damage**: Top speed reduced, stability affected

### Cascading Damage

When a component takes damage:
1. Direct component damage applied
2. Nearby components take secondary damage (50% reduction)
3. Performance penalties calculated
4. Failure thresholds checked
5. UI updated with new states

### Historical Tracking

The system maintains:
- Impact count per component
- Last impact time
- Total damage sustained
- Repair history
- Performance degradation timeline

## Performance Optimization

- **Collision Caching**: Avoids repeated impact calculations
- **Lazy Evaluation**: Performance penalties only updated on damage change
- **Circular Buffer**: Recent impacts limited to 50 events
- **Memory**: ~5KB per vehicle damage state

## Testing Scenarios

### Test Case 1: Minor Collision
```
Input: 5 m/s impact on FrontBumper
Expected: 5-10% damage, no performance impact
```

### Test Case 2: Severe Crash
```
Input: 25 m/s impact on Engine
Expected: Engine damaged 80%+, 40% horsepower loss
```

### Test Case 3: Multiple Impacts
```
Input: 5 collisions over 60 seconds
Expected: Components accumulate damage, failures at thresholds
```

### Test Case 4: Repair Workflow
```
Input: Damage 50%, repair 30%, damage again 20%
Expected: Component damage correctly accumulates and repairs
```

## Future Enhancements

- [ ] Tire puncture simulation
- [ ] Fuel leak damage
- [ ] Cooling system damage
- [ ] Component wear-in repair parts
- [ ] Part replacement vs repair cost differences
- [ ] Cosmetic damage (paint, body panels)
- [ ] Differential damage effects by surface
- [ ] Dynamic damage visualization (cracks, deformation)
- [ ] Progressive component degradation
- [ ] Insurance/claims system

## Best Practices

1. **Register impacts in OnCollisionEnter**, not Update
2. **Cache GetAllComponentDamage()** if calling multiple times
3. **Check IsComponentFunctional()** before critical operations
4. **Monitor GetOverallDamage()** for gameplay difficulty scaling
5. **Refresh UI** only when damage changes, not every frame

---

**Status**: ✓ Complete and integrated
**Last Updated**: 2026-03-27
**Implementation**: 2 files, ~900 lines of code
