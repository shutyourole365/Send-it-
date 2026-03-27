# Enhancement 5: AI Driver Opponents

## Overview

The AI Driver Opponent system provides competitive racing against dynamic AI-controlled vehicles. Opponents adapt their behavior based on track position, vehicle damage, and player performance with four difficulty levels ranging from Rookie to Expert.

## Core Features

### 1. Difficulty Levels

| Difficulty | Acceleration | Braking | Cornering | Consistency | Performance |
|------------|-------------|---------|-----------|-------------|-------------|
| Rookie | 65% | 70% | 70% | 60% | 70% |
| Intermediate | 80% | 85% | 85% | 80% | 85% |
| Professional | 92% | 95% | 95% | 95% | 95% |
| Expert | 110% | 105% | 110% | 105% | 110% |

### 2. Racing Behaviors

**Following** - Maintains ideal racing line, targets waypoints
- Standard lap pace
- Smooth inputs
- Consistent performance

**Attacking** - Aggressive driving to overtake
- Increased throttle (+20%)
- Earlier braking points
- Modified racing line for overtake
- Higher risk tolerance

**Defending** - Protects position against overtaking
- Conservative throttle (-10%)
- Blocks racing line
- Reduced aggression
- Risk averse

**Recovering** - Smooth driving after mistakes
- 50% throttle
- Minimal steering input
- Cautious recovery
- Returns to following when stable

**Damaged** - Reduced performance with vehicle damage
- Damage multiplier reduces all inputs
- 70% throttle cap
- 80% braking cap
- 90% steering cap

### 3. Dynamic Adaptation

- **Confidence System**: Increases on good laps, decreases on mistakes
- **Aggression**: Scales with race situation (attacking vs defending)
- **Performance Scaling**: Adjusts based on lap times and damage
- **Waypoint Following**: Uses racing line for smooth consistent laps
- **Difficulty Scaling**: Adapts difficulty based on player performance gap

## Architecture

### AIOpponent Class

Main AI driver controller managing behavior, decision-making, and vehicle control.

```csharp
public class AIOpponent : MonoBehaviour
{
    public enum DifficultyLevel { Rookie, Intermediate, Professional, Expert }
    public enum RacingBehavior { Following, Attacking, Defending, Recovering, Damaged }

    // Setup and configuration
    public void Initialize()
    public void SetDifficulty(DifficultyLevel newDifficulty)
    public void SetWaypoints(Transform[] newWaypoints)
    public void ResetSession()

    // Behavior and control
    private void UpdateBehavior()
    private void DecideNextAction()
    private void ApplyControlInputs()

    // Race adaptation
    public void AdaptDifficulty(float playerLapTime, float aiLapTime)

    // Queries
    public float GetBestLapTime()
    public float GetCurrentLapTime()
    public RacingBehavior GetCurrentBehavior()
}
```

### AIRaceManager Class

Manages multiple AI opponents and race state.

```csharp
public class AIRaceManager : MonoBehaviour
{
    public struct RaceResult
    {
        public string DriverName;
        public int Position;
        public float BestLapTime;
        public int LapsCompleted;
        public bool FinishedRace;
    }

    // Race control
    public void StartRace(int numOpponents, AIOpponent.DifficultyLevel difficulty)
    public void EndRace()

    // Race queries
    public int GetPlayerPosition()
    public float GetGapToLeader()
    public float GetGapToBehind()
    public List<RaceResult> GetRaceResults()
}
```

## Racing Behavior System

### Behavior Decision Tree

```
CURRENT STATE
    ├── HIGH DAMAGE (> 70%)
    │   └── DAMAGED behavior
    ├── PLAYER AHEAD & CLOSE (< 30m)
    │   └── ATTACKING behavior
    │       ├── Increase throttle (+20%)
    │       ├── Earlier braking
    │       └── Modified line for overtake
    ├── PLAYER BEHIND & CLOSE (< 20m)
    │   └── DEFENDING behavior
    │       ├── Conservative throttle (-10%)
    │       ├── Block racing line
    │       └── Reduced aggression
    └── DEFAULT
        └── FOLLOWING behavior
            ├── Maintain racing line
            ├── Target throttle
            └── Smooth inputs
```

### Input Calculation

```csharp
// Steering toward waypoint
steer = Cross(vehicleForward, dirToWaypoint).y * 2.0
steer = Clamp(steer, -1.0, 1.0)

// Braking distance
brakingDist = (speed^2) / (2 * gravity * friction)
brake = Clamp01((brakingDist - nextWaypointDist) / 10.0)

// Throttle control
if (speed < targetSpeed * corneringFactor)
    throttle = accelerationFactor
else
    throttle = 0
```

## Waypoint Following

AI opponents follow a predetermined racing line defined by waypoints.

```csharp
// Setup waypoints
Transform[] waypoints = GetRacingLineWaypoints();
aiOpponent.SetWaypoints(waypoints);

// Automatic waypoint advancement
if (Vector3.Distance(position, waypoint) < 5f)
{
    currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
}
```

### Optimal Waypoint Spacing
- Straight sections: 20-30m
- Corner exits: 10-15m
- Braking zones: 5-10m
- Apex lines: Tight spacing for precision

## API Reference

### AIOpponent Setup

#### Initialize
```csharp
aiOpponent.Initialize();
```
Initializes AI system and finds player vehicle.

#### SetDifficulty
```csharp
aiOpponent.SetDifficulty(AIOpponent.DifficultyLevel.Professional);
```
Changes AI difficulty and recalculates performance factors.

#### SetWaypoints
```csharp
Transform[] racingLine = GetTrackWaypoints();
aiOpponent.SetWaypoints(racingLine);
```
Sets the racing line waypoints for the opponent to follow.

#### ResetSession
```csharp
aiOpponent.ResetSession();
```
Resets lap time, best lap, and behavioral state for new session.

### Performance Queries

#### GetBestLapTime
```csharp
float bestLap = aiOpponent.GetBestLapTime();
```
Returns fastest lap time. Returns float.MaxValue if no lap completed.

#### GetCurrentLapTime
```csharp
float currentLap = aiOpponent.GetCurrentLapTime();
```
Returns time elapsed in current lap.

#### GetCurrentBehavior
```csharp
var behavior = aiOpponent.GetCurrentBehavior();
switch (behavior)
{
    case AIOpponent.RacingBehavior.Attacking:
        ShowAttackingIndicator();
        break;
}
```

### Difficulty Adaptation

#### AdaptDifficulty
```csharp
aiOpponent.AdaptDifficulty(playerBestLap, aiBestLap);
```
Automatically adjusts AI difficulty based on performance ratio.

**Adaptation Rules:**
- Player 15%+ faster → Reduce difficulty
- Player 15%+ slower → Increase difficulty
- Within 15% → No change (competitive balance)

## Race Manager API

### Race Control

#### StartRace
```csharp
raceManager.StartRace(numOpponents: 3, difficulty: Intermediate);
```
Initializes race with specified number of opponents.

**Parameters:**
- `numOpponents`: 1-8 opponent count
- `difficulty`: Rookie, Intermediate, Professional, Expert

#### EndRace
```csharp
raceManager.EndRace();
```
Finishes race and generates results.

### Race Queries

#### GetPlayerPosition
```csharp
int position = raceManager.GetPlayerPosition();
// Returns 1 for 1st place, 2 for 2nd, etc.
```

#### GetGapToLeader
```csharp
float gap = raceManager.GetGapToLeader();
// Returns gap in seconds to race leader
// Positive = player is slower
// Negative = player is faster
```

#### GetGapToBehind
```csharp
float gap = raceManager.GetGapToBehind();
// Returns gap in seconds to car behind
// Positive = player has lead
// Negative = car behind is faster
```

#### GetRaceResults
```csharp
var results = raceManager.GetRaceResults();
foreach (var result in results)
{
    Debug.Log($"{result.Position}. {result.DriverName} - {result.BestLapTime:F2}s");
}
```

## Usage Example

### Basic Race Setup

```csharp
public class RaceController : MonoBehaviour
{
    private AIRaceManager raceManager;
    private AIOpponent[] aiOpponents;

    void Start()
    {
        raceManager = GetComponent<AIRaceManager>();
        raceManager.Initialize();
    }

    public void StartMultiplayerRace()
    {
        // 3 opponents at intermediate difficulty
        raceManager.StartRace(
            numOpponents: 3,
            difficulty: AIOpponent.DifficultyLevel.Intermediate
        );
    }

    void Update()
    {
        if (!raceManager.IsRaceActive)
            return;

        // Update HUD
        int position = raceManager.GetPlayerPosition();
        float gapToLeader = raceManager.GetGapToLeader();
        int remaining = raceManager.RemainingLaps;

        Debug.Log($"Position: {position} | Gap: {gapToLeader:F2}s | Laps: {remaining}");

        // Check race end
        if (raceManager.RemainingLaps <= 0)
        {
            raceManager.EndRace();
            DisplayResults(raceManager.GetRaceResults());
        }
    }

    void DisplayResults(List<AIRaceManager.RaceResult> results)
    {
        foreach (var result in results)
        {
            Debug.Log($"{result.Position}. {result.DriverName}: " +
                     $"{result.BestLapTime:F2}s ({result.LapsCompleted} laps)");
        }
    }
}
```

### Advanced: Competitive Difficulty

```csharp
public void SetupCompetitiveRace(int playerSkillLevel)
{
    // Adapt AI difficulty to player skill
    var difficulty = playerSkillLevel switch
    {
        1 => AIOpponent.DifficultyLevel.Rookie,
        2 => AIOpponent.DifficultyLevel.Intermediate,
        3 => AIOpponent.DifficultyLevel.Professional,
        _ => AIOpponent.DifficultyLevel.Expert
    };

    // Add variety with mixed difficulties
    for (int i = 0; i < 3; i++)
    {
        aiOpponents[i].SetDifficulty(
            i % 2 == 0 ? difficulty : difficulty - 1
        );
    }

    raceManager.StartRace(3, difficulty);
}
```

## Behavioral State Transitions

```
         ┌─────────────────────────────────────────┐
         │      FOLLOWING (Default)                │
         │  - Waypoint following                   │
         │  - Consistent pace                      │
         │  - Smooth inputs                        │
         └──────────────┬──────────────────────────┘
                        │
        ┌───────────────┼───────────────┐
        │               │               │
        ▼               ▼               ▼
    ATTACKING      DEFENDING       DAMAGED
    (Overtaking)   (Defense)       (Reduced Perf)
      +20% throttle -10% throttle  Damage multiplier
      Early brake   Block line     Limited inputs
      Modified line Risk averse    Damaged behavior
        │               │               │
        └───────────────┼───────────────┘
                        │
         ┌──────────────▼───────────────────────────┐
         │     FOLLOWING (Resume)                  │
         │  After overtake/defense/recovery        │
         └─────────────────────────────────────────┘
```

## Performance Tuning

### Waypoint Distance Optimization
- Too close: CPU overhead, jerky movement
- Too far: Poor cornering, missing apexes
- Optimal: 5-30m depending on corner type

### Update Frequency
- Per-frame behavior updates (most responsive)
- Waypoint checks every 0.1s (efficient)
- Difficulty adaptation every 10 laps

### CPU Impact
- Single opponent: ~1-2ms per frame
- 8 opponents: ~8-16ms per frame
- Scales linearly with opponent count

## Advanced Features

### Confidence System
```csharp
// Increases on good laps
if (currentLapTime < bestLapTime)
    confidence += 0.05f;

// Decreases on poor performance
if (vehicleDamage > 0.7f)
    confidence -= 0.01f;

confidence = Clamp01(confidence);
```

### Aggression Scaling
- Increases when attacking (up to 1.0)
- Decreases when defending (down to 0.0)
- Affects overtaking decisions
- Determines behavior aggressiveness

### Damage Impact
- All inputs multiplied by (1 - damageLevel)
- Behavior changes to DAMAGED at > 70% damage
- Confidence decreases with damage
- Can recover if damage is repaired

## Testing Scenarios

### Scenario 1: Chase
```
Expected: AI maintains lead, drives defensive
Result: Throttle at 90%, blocks racing line
```

### Scenario 2: Trailing
```
Expected: AI attempts to overtake
Result: Increased throttle, modified line, aggressive steering
```

### Scenario 3: Damaged Vehicle
```
Expected: AI reduces pace significantly
Result: All inputs scaled by damage factor
```

### Scenario 4: Difficulty Scaling
```
Expected: If player is 20% faster, AI difficulty reduces
Result: Opponent difficulty changes, pace matches player skill
```

## Future Enhancements

- [ ] Machine learning for behavior improvement
- [ ] Traffic awareness for multi-car races
- [ ] Tire degradation consideration
- [ ] Fuel management strategy
- [ ] Pit stop planning
- [ ] Weather-dependent behavior
- [ ] Customizable opponent personalities
- [ ] Formation racing (team AI)
- [ ] Telemetry learning from player
- [ ] Dynamic difficulty rubberband effect

---

**Status**: ✓ Complete and integrated
**Last Updated**: 2026-03-27
**Implementation**: 2 files, ~1,100 lines of code
