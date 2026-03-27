# Enhancement 2: Setup Comparison System

## Overview

The Setup Comparison System enables advanced vehicle setup management with performance analysis. Players can save, load, compare, and track the impact of configuration changes across different setups and track conditions.

## Core Features

### 1. Setup Management
- **Save Setup**: Store current vehicle configuration with metadata (name, description, track, lap time)
- **Load Setup**: Quickly apply saved configurations
- **Delete Setup**: Remove setups from history
- **Duplicate Setup**: Clone existing setup for experimentation

### 2. Setup Comparison
- **Side-by-Side Analysis**: Compare any two saved setups
- **Parameter Deltas**: View exact changes between configurations
- **Impact Analysis**: Quantify performance impact of each parameter change
- **Performance Metrics**: Track lap times and session statistics

### 3. Performance Tracking
- **Usage Counting**: Track how many times each setup was used
- **Lap Time Recording**: Store best lap times per setup per track
- **Performance History**: Analyze improvements over time
- **Setup Rankings**: Sort by performance or usage frequency

### 4. Intelligent Analysis
- **Impact Weighting**: Different parameters have different importance
  - High impact: HorsePower (12%), DownforceCoefficient (11%), TireGripCoefficient (13%)
  - Medium impact: DragCoefficient (10%), SpringStiffness (9%)
  - Lower impact: RideHeight (3%), WeightDistribution (4%)
- **Recommendation Engine**: Suggests whether changes are beneficial or detrimental
- **Direction Analysis**: Determines if parameter changes help or hurt performance

## Data Structures

### SavedSetup
```csharp
public struct SavedSetup
{
    public string SetupName;                          // Display name
    public string Description;                        // User notes
    public System.DateTime CreatedDate;               // When created
    public System.DateTime LastModified;              // Last load time
    public string TrackName;                          // Associated track
    public float BestLapTime;                         // Best recorded lap
    public Dictionary<string, float> Parameters;      // All tuning params
    public Dictionary<string, float> PerformanceMetrics; // Speed, RPM, temps
    public int UseCount;                              // Usage frequency
}
```

### SetupComparison
```csharp
public struct SetupComparison
{
    public SavedSetup Setup1;                                    // First setup
    public SavedSetup Setup2;                                    // Second setup
    public Dictionary<string, float> ParameterDeltas;            // Changes
    public Dictionary<string, PerformanceImpact> ImpactAnalysis; // Per-param analysis
    public float PerformanceDelta;                               // Lap time difference
    public string OverallRecommendation;                         // Summary
}
```

### PerformanceImpact
```csharp
public struct PerformanceImpact
{
    public string ParameterName;        // Parameter being analyzed
    public float CurrentValue;          // Setup 1 value
    public float ComparisonValue;       // Setup 2 value
    public float Delta;                 // Difference
    public float ImpactScore;           // 0-1, relative importance
    public string ImpactDirection;      // "Better", "Worse", "Neutral"
    public string Recommendation;       // Contextual advice
}
```

## API Reference

### Setup Management

#### SaveCurrentSetup
```csharp
void SaveCurrentSetup(string setupName, string description,
                     string trackName = "", float lapTime = 0f)
```
Saves the current vehicle configuration with metadata.

**Parameters:**
- `setupName`: Unique identifier for this setup
- `description`: Notes about the setup
- `trackName`: Track this setup is optimized for (optional)
- `lapTime`: Best lap time achieved (optional)

**Example:**
```csharp
comparisonSystem.SaveCurrentSetup(
    "Race Setup - Monza",
    "Optimized for high-speed circuit",
    "Monza",
    95.234f
);
```

#### LoadSetup
```csharp
bool LoadSetup(string setupName)
```
Loads a saved setup into the vehicle and increments usage count.

**Returns:** true if successful, false if not found

**Example:**
```csharp
if (comparisonSystem.LoadSetup("Race Setup - Monza"))
{
    Debug.Log("Setup loaded successfully");
}
```

#### DeleteSetup
```csharp
bool DeleteSetup(string setupName)
```
Permanently removes a saved setup.

#### DuplicateSetup
```csharp
bool DuplicateSetup(string originalSetupName, string newSetupName)
```
Creates a copy of a setup for experimentation.

### Retrieval

#### GetAllSetups
```csharp
List<SavedSetup> GetAllSetups()
```
Returns all saved setups in order of creation.

#### GetSetupsByPerformance
```csharp
List<SavedSetup> GetSetupsByPerformance(string trackName = "")
```
Returns setups sorted by lap time (fastest first).

**Optional filter by track name**

#### GetMostUsedSetups
```csharp
List<SavedSetup> GetMostUsedSetups(int count = 5)
```
Returns the most frequently used setups.

### Comparison & Analysis

#### CompareSetups
```csharp
SetupComparison CompareSetups(string setupName1, string setupName2)
```
Analyzes differences between two setups and calculates impact scores.

**Returns:** SetupComparison struct with detailed analysis

**Example:**
```csharp
var comparison = comparisonSystem.CompareSetups(
    "Conservative Setup",
    "Aggressive Setup"
);

// Access results
foreach (var impact in comparison.ImpactAnalysis.Values)
{
    Debug.Log($"{impact.ParameterName}: {impact.Delta:F2} " +
              $"({impact.ImpactDirection}) - {impact.Recommendation}");
}
```

#### GenerateComparisonReport
```csharp
string GenerateComparisonReport(SetupComparison comparison)
```
Creates detailed text report of comparison with recommendations.

**Report Contents:**
- Setup names and metadata
- Performance delta (lap time difference)
- Parameter-by-parameter breakdown
- Impact scores and directions
- Overall recommendation

## Parameter Impact Weighting

The system uses importance weights to determine how much each parameter affects overall performance:

| Parameter | Weight | Category |
|-----------|--------|----------|
| TireGripCoefficient | 13% | Tires |
| HorsePower | 12% | Engine |
| DownforceCoefficient | 11% | Aero |
| DragCoefficient | 10% | Aero |
| SpringStiffness | 9% | Suspension |
| MaxRPM | 8% | Engine |
| SlipAngleSensitivity | 8% | Tires |
| ResponsivenessFactor | 6% | Engine |
| TemperatureSensitivity | 6% | Tires |
| AntiRollBar | 5% | Suspension |
| VehicleMass | 5% | Weight |
| DamperRatio | 7% | Suspension |
| WearRate | 4% | Tires |
| WeightDistribution | 4% | Weight |
| RideHeight | 3% | Suspension |
| Other | 2% | Default |

## Integration with TuningManager

```csharp
// In your TuningManager setup
public void ApplySetupFromComparison(string setupName)
{
    var setup = comparisonSystem.GetSetup(setupName);

    foreach (var param in setup.Parameters)
    {
        SetPhysicsParameter(param.Key, param.Value);
    }
}
```

## UI Integration

### Setup Saving UI
- Input field for setup name (max 50 chars)
- Text area for description (max 200 chars)
- Save button
- Setup count label (current/max)

### Setup Loading UI
- Dropdown with all saved setups
- Load button
- Delete button
- Duplicate button

### Setup Comparison UI
- Two dropdown selectors
- Compare button
- Results display area with scrolling
- Formatted comparison report

### Setup Browser UI
- Filter by track
- Sort by performance (lap time)
- Sort by usage (frequency)
- Setup list view with metadata

## Usage Patterns

### Comparing Before & After
```csharp
// Save before making changes
comparisonSystem.SaveCurrentSetup("Setup v1", "Original configuration");

// Make adjustments...

// Save after changes
comparisonSystem.SaveCurrentSetup("Setup v2", "With suspension tweaks");

// Compare impact
var comparison = comparisonSystem.CompareSetups("Setup v1", "Setup v2");
Debug.Log(comparisonSystem.GenerateComparisonReport(comparison));
```

### Track-Specific Setup Management
```csharp
// Save different setups for different tracks
comparisonSystem.SaveCurrentSetup("Monza Setup", "High speed", "Monza", 95.2f);
comparisonSystem.SaveCurrentSetup("Monaco Setup", "Technical", "Monaco", 78.4f);

// Load best setup for current track
var bestSetups = comparisonSystem.GetSetupsByPerformance("Monza");
if (bestSetups.Count > 0)
{
    comparisonSystem.LoadSetup(bestSetups[0].SetupName);
}
```

### Performance Analysis
```csharp
// Find most used setup
var mostUsed = comparisonSystem.GetMostUsedSetups(1);
Debug.Log($"Most used: {mostUsed[0].SetupName} ({mostUsed[0].UseCount} times)");

// Compare with latest setup
var comparison = comparisonSystem.CompareSetups(
    mostUsed[0].SetupName,
    recentSetups[0].SetupName
);

if (comparison.PerformanceDelta < 0)
    Debug.Log("New setup is faster!");
```

## Performance Considerations

- **Max Saved Setups**: 20 (configurable)
- **Persistent Storage**: Saves to Application.persistentDataPath/setups.json
- **Comparison Time**: O(n) where n = number of parameters (~30-40)
- **Memory Usage**: ~5KB per setup

## File Storage

Setups are automatically persisted to:
```
{Application.persistentDataPath}/setups.json
```

Backup and restore by managing this JSON file.

## Future Enhancements

- [ ] Setup sharing via QR code
- [ ] Cloud sync across devices
- [ ] Undo/revert to previous setup state
- [ ] Automatic setup recommendations based on track
- [ ] Setup evolution trees (track setup lineage)
- [ ] A/B testing framework with statistical analysis
- [ ] Setup performance trending over multiple sessions
- [ ] Parameter optimization suggestions based on telemetry

## Example Integration

```csharp
public class GarageManager : MonoBehaviour
{
    private SetupComparisonSystem setupComparison;
    private AITuningAdvisor tuningAdvisor;

    void Start()
    {
        setupComparison = GetComponent<SetupComparisonSystem>();
        tuningAdvisor = GetComponent<AITuningAdvisor>();
    }

    void SaveAndAnalyze()
    {
        // Get AI recommendation
        var recommendations = tuningAdvisor.AnalyzeSession();

        // Save current setup
        setupComparison.SaveCurrentSetup(
            "Pre-AI Setup",
            "Before AI tuning suggestions"
        );

        // Apply first recommendation
        tuningAdvisor.ApplyRecommendation(recommendations[0]);

        // Save new setup
        setupComparison.SaveCurrentSetup(
            "Post-AI Setup",
            "After AI tuning suggestion"
        );

        // Compare impact
        var comparison = setupComparison.CompareSetups(
            "Pre-AI Setup",
            "Post-AI Setup"
        );

        Debug.Log(setupComparison.GenerateComparisonReport(comparison));
    }
}
```

## Workflow

1. **Save Base Setup** - Create baseline configuration
2. **Make Adjustments** - Tune parameters for specific track
3. **Save Updated Setup** - Store modified configuration
4. **Compare Setups** - Analyze differences
5. **Review Report** - Study parameter impacts
6. **Make Decision** - Keep improved setup or revert
7. **Load Setup** - Apply configuration to vehicle
8. **Test On Track** - Verify performance improvement
9. **Track Usage** - System automatically counts usage

---

**Status**: ✓ Complete and integrated
**Last Updated**: 2026-03-27
