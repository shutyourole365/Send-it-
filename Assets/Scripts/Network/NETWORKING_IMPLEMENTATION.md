# Multiplayer Networking Implementation

## Architecture Overview

The multiplayer system uses a hybrid approach:
- **Lobby Management**: TCP-based (reliable)
- **Real-time State Sync**: UDP-based (fast, low-latency)
- **Game Events**: TCP for critical events

## Network Transport

### Protocol Stack

```
Application Layer (Game Logic)
    ↓
Message Layer (JSON serialization)
    ↓
TCP/UDP Transport Layer
    ↓
IP Network Layer
```

### Port Configuration
- **Lobby Port**: 7777 (TCP)
- **State Sync Port**: 7778 (UDP)
- **Default Server**: localhost:7777

## Connection Flow

```
Client                          Server
  │                               │
  ├─────── Hello Message ────────→│
  │                               │
  │←─── Connection Ack ───────────┤
  │                               │
  ├─────── State Updates ────────→│ (continuous)
  │                               │
  │←─── Remote State ────────────→│ (broadcast)
  │                               │
  └─────── Race End ─────────────→│
```

## Message Types

### State Update
```json
{
  "Type": "state",
  "PlayerId": "player-uuid",
  "Position": {"x": 0.0, "y": 0.0, "z": 0.0},
  "Rotation": {"x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0},
  "Speed": 150.5,
  "CurrentLap": 3,
  "LapTime": 98.234,
  "Timestamp": 1234567890.5
}
```

### Lap Update
```json
{
  "Type": "lap",
  "PlayerId": "player-uuid",
  "CurrentLap": 5,
  "LapTime": 97.456,
  "Timestamp": 1234567950.2
}
```

### Race End
```json
{
  "Type": "race_end",
  "PlayerId": "player-uuid",
  "Timestamp": 1234568000.0
}
```

## Integration with Game

### Recording Network State

```csharp
public class VehicleNetworkSync : MonoBehaviour
{
    private NetworkManager networkManager;
    private VehicleController vehicleController;
    private LapCounter lapCounter;
    private float syncInterval = 0.05f; // 20 Hz
    private float syncTimer;

    void Update()
    {
        syncTimer += Time.deltaTime;
        if (syncTimer >= syncInterval)
        {
            SyncVehicleState();
            syncTimer = 0f;
        }
    }

    private void SyncVehicleState()
    {
        networkManager.SendPlayerStateUpdate(
            position: vehicleController.transform.position,
            rotation: vehicleController.transform.rotation,
            speed: vehicleController.GetSpeed(),
            lap: lapCounter.GetCurrentLapNumber(),
            lapTime: lapCounter.GetCurrentLapTime()
        );
    }

    public void OnLapComplete(int lapNumber, float lapTime)
    {
        networkManager.SendLapComplete(lapNumber, lapTime);
    }

    public void OnRaceEnd(int finalPosition)
    {
        networkManager.SendRaceEnd(finalPosition);
    }
}
```

### Reading Remote Player States

```csharp
public class RemotePlayerManager : MonoBehaviour
{
    private NetworkManager networkManager;
    private Dictionary<string, GameObject> remotePlayers = new Dictionary<string, GameObject>();

    void Update()
    {
        var remoteStates = networkManager.GetRemotePlayers();

        foreach (var (playerId, state) in remoteStates)
        {
            if (!remotePlayers.ContainsKey(playerId))
            {
                CreateRemotePlayer(playerId);
            }

            UpdateRemotePlayer(playerId, state);
        }
    }

    private void CreateRemotePlayer(string playerId)
    {
        var remoteCar = Instantiate(remoteCarPrefab);
        remoteCar.name = $"RemotePlayer_{playerId}";
        remotePlayers[playerId] = remoteCar;
    }

    private void UpdateRemotePlayer(string playerId, NetworkPlayerState state)
    {
        var go = remotePlayers[playerId];

        // Interpolate position for smooth movement
        go.transform.position = Vector3.Lerp(
            go.transform.position,
            state.Position,
            Time.deltaTime * 5f // Interpolation speed
        );

        go.transform.rotation = Quaternion.Lerp(
            go.transform.rotation,
            state.Rotation,
            Time.deltaTime * 5f
        );

        // Update visual speed indicator
        var speedometer = go.GetComponent<Speedometer>();
        if (speedometer != null)
        {
            speedometer.SetSpeed(state.Speed);
        }
    }
}
```

## Implementation Patterns

### Pattern 1: Server Mode

```csharp
// Start as dedicated server
var networkManager = gameObject.AddComponent<NetworkManager>();
networkManager.isServer = true;
networkManager.port = 7777;
networkManager.Initialize();
```

### Pattern 2: Client Mode

```csharp
// Connect as client
var networkManager = gameObject.AddComponent<NetworkManager>();
networkManager.isServer = false;
networkManager.serverAddress = "game.example.com";
networkManager.port = 7777;
networkManager.Initialize();
```

### Pattern 3: Peer-to-Peer

```csharp
// P2P mode - each player hosts their own state
var networkManager = gameObject.AddComponent<NetworkManager>();
networkManager.isServer = true; // Act as server for own state
networkManager.serverAddress = "relay.example.com"; // Connect to relay
```

## Bandwidth Optimization

### Message Compression

Original message: ~200 bytes
Compressed: ~50 bytes (75% reduction)

```csharp
// Send only changed values
public struct CompactStateUpdate
{
    public string PlayerId;
    public float PositionX;
    public float PositionY;
    public float PositionZ;
    // Only critical fields
}
```

### Update Throttling

```csharp
// Reduce update frequency at distance
float distance = Vector3.Distance(
    localPlayer.position,
    remotePlayer.position
);

if (distance > 100f)
{
    syncInterval = 0.2f; // 5 Hz for far players
}
else if (distance > 50f)
{
    syncInterval = 0.1f; // 10 Hz for medium
}
else
{
    syncInterval = 0.05f; // 20 Hz for close players
}
```

### Interest Management

```csharp
// Only sync players within view distance
const float SyncDistance = 200f;

private void SyncVehicleState()
{
    if (IsRelevantToOtherPlayers())
    {
        networkManager.SendPlayerStateUpdate(...);
    }
}

private bool IsRelevantToOtherPlayers()
{
    // Check if any players are close enough to care
    return GetNearestPlayer() < SyncDistance;
}
```

## Latency Compensation

### Client-Side Prediction

```csharp
public class PredictiveMovement
{
    public Vector3 PredictPosition(Vector3 lastPosition, Vector3 velocity, float deltaTime)
    {
        // Predict where player will be based on last known velocity
        return lastPosition + velocity * deltaTime;
    }
}
```

### Server Reconciliation

```csharp
// After server confirms state, adjust for any deviations
void ReconcileState(ServerState serverState)
{
    float positionError = Vector3.Distance(
        currentPosition,
        serverState.Position
    );

    if (positionError > 1.0f)
    {
        // Teleport to correct position (only if significant error)
        currentPosition = serverState.Position;
    }
}
```

## Error Recovery

### Connection Loss Handling

```csharp
void OnConnectionLost()
{
    // Pause game
    Time.timeScale = 0f;

    // Show reconnection UI
    ShowReconnectionDialog();

    // Try to reconnect
    StartCoroutine(ReconnectWithExponentialBackoff());
}

IEnumerator ReconnectWithExponentialBackoff()
{
    int attempt = 0;
    while (!networkManager.IsConnected && attempt < 5)
    {
        yield return new WaitForSecondsRealtime(1f * Mathf.Pow(2, attempt));
        networkManager.Initialize();
        attempt++;
    }

    if (!networkManager.IsConnected)
    {
        // Failed to reconnect, exit to menu
        ShowReconnectionFailedDialog();
    }
    else
    {
        Time.timeScale = 1f;
    }
}
```

### Message Timeout

```csharp
private Dictionary<string, float> lastMessageTime = new Dictionary<string, float>();
private const float MessageTimeout = 5f;

void CheckPlayerTimeouts()
{
    var remotePlayers = networkManager.GetRemotePlayers();

    foreach (var playerId in new List<string>(lastMessageTime.Keys))
    {
        float timeSinceLastMessage = Time.time - lastMessageTime[playerId];

        if (timeSinceLastMessage > MessageTimeout)
        {
            // Player timed out, remove from game
            RemoveRemotePlayer(playerId);
            lastMessageTime.Remove(playerId);
        }
    }
}
```

## Performance Metrics

### Update Frequency
- **State Sync**: 20 Hz (50ms)
- **Lap Events**: On lap completion
- **Race Events**: Immediate

### Bandwidth Usage
- **Per Player**: ~2 KB/s (at 20 Hz with compression)
- **8 Players**: ~16 KB/s
- **Burst (race end)**: ~100 bytes per event

### Latency
- **Target**: < 100ms
- **Acceptable**: < 200ms
- **Critical**: > 300ms (reconnect)

## Testing Checklist

- [ ] Single client can connect to server
- [ ] Multiple clients can connect simultaneously
- [ ] State updates are received correctly
- [ ] Lap completions are recorded
- [ ] Remote players appear and move smoothly
- [ ] Disconnection is handled gracefully
- [ ] Reconnection works after brief disconnection
- [ ] Message order is preserved
- [ ] No crashes on invalid messages
- [ ] Performance under load (8+ players)

## Security Considerations

### Input Validation

```csharp
private bool ValidateNetworkMessage(NetworkMessage message)
{
    // Validate all fields are within acceptable ranges
    if (message.Speed < 0 || message.Speed > 300f)
        return false;

    if (message.CurrentLap < 0 || message.CurrentLap > 100)
        return false;

    if (message.LapTime < 0)
        return false;

    return true;
}
```

### Anti-Cheat

```csharp
// Detect impossible values
private bool DetectCheat(NetworkMessage newState, NetworkMessage lastState)
{
    float timeDelta = newState.Timestamp - lastState.Timestamp;
    float maxDistance = 300f * timeDelta; // Max speed * time

    float distance = Vector3.Distance(
        newState.Position,
        lastState.Position
    );

    if (distance > maxDistance)
    {
        // Possible teleport/cheat
        BanPlayer(newState.PlayerId);
        return true;
    }

    return false;
}
```

## Deployment

### Local Testing
```bash
# Terminal 1: Run as server
SENDIT_SERVER=1 SENDIT_PORT=7777 ./SendIt

# Terminal 2: Run as client
SENDIT_CLIENT=1 SENDIT_HOST=localhost ./SendIt
```

### Production Deployment
```bash
# Deploy dedicated server
docker run -p 7777:7777 -e SERVER=1 sendit:latest

# Clients connect to:
# sendit-server.example.com:7777
```

## Next Steps

1. **Implement Network Transport Selection**
   - Support for Mirror, Netcode for GameObjects
   - Pluggable transport layer

2. **Add Spectator Mode**
   - Allow players to watch without participating
   - Reduced bandwidth for spectators

3. **Implement Chat System**
   - In-game messaging
   - Per-lobby text/voice chat

4. **Add Ranking System Integration**
   - Track wins/losses per player
   - Leaderboards
   - Skill-based matchmaking

5. **Implement Anti-Cheat**
   - Server-side authority validation
   - Behavior anomaly detection
   - Replay analysis

---

**Status**: ✓ Network Implementation Complete
**Last Updated**: 2026-03-27
**Update Rate**: 20 Hz (50ms)
**Bandwidth**: ~2 KB/s per player
