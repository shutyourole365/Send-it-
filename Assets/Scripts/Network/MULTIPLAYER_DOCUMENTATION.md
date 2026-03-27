# Enhancement 8: Multiplayer Support

## Overview

The Multiplayer Racing System enables competitive online racing with lobby management, real-time vehicle synchronization, and skill-based matchmaking. Players can create lobbies, join friends, and race competitively with full network state synchronization.

## Core Features

### 1. Lobby System
- Create private/public race lobbies
- Join lobbies with friend requests
- Host controls (start race, kick players, settings)
- Lobby browser with filters
- Ready status tracking

### 2. Matchmaking
- Skill-based lobby recommendations
- ELO-style skill rating system (800-3000+)
- Skill range filtering for balanced matches
- New player placement system

### 3. Network Synchronization
- Real-time vehicle state updates (20 Hz)
- Position and rotation sync
- Lap time and position tracking
- Latency compensation
- Automatic player ranking

### 4. Social Features
- Friends list management
- Player blocking system
- Lobby search by player
- Quick-join for friends
- Statistics tracking

### 5. Race Management
- Multi-player competitive racing (2-8 players)
- Real-time position rankings
- Network state validation
- Graceful disconnection handling
- Race result persistence

## Data Structures

### LobbyPlayer
```csharp
public struct LobbyPlayer
{
    public string PlayerId;           // Unique player ID
    public string PlayerName;         // Display name
    public int SkillRating;          // ELO rating (800-3000+)
    public string CarColor;          // Vehicle color hex code
    public bool IsReady;             // Ready status
    public System.DateTime JoinTime; // Join timestamp
}
```

### RaceLobby
```csharp
public struct RaceLobby
{
    public string LobbyId;                   // Unique lobby ID
    public string LobbyName;                 // Display name
    public string HostId;                    // Host player ID
    public List<LobbyPlayer> Players;        // All players in lobby
    public int MaxPlayers;                   // Capacity (2-8)
    public string TrackName;                 // Track selection
    public int LapCount;                     // Race duration
    public bool IsRacing;                    // Race active flag
    public System.DateTime CreatedTime;      // Creation timestamp
}
```

### NetworkPlayerState
```csharp
public struct NetworkPlayerState
{
    public string PlayerId;              // Player identifier
    public Vector3 Position;             // World position
    public Quaternion Rotation;          // Vehicle rotation
    public float Speed;                  // Current speed
    public int CurrentLap;               // Lap number
    public float LapTime;                // Current lap time
    public int Position_Rank;            // Race position
    public System.DateTime LastUpdate;   // Last sync time
}
```

## API Reference

### Lobby Management

#### CreateLobby
```csharp
RaceLobby CreateLobby(string lobbyName, string trackName, int lapCount)
```
Create a new race lobby as host.

**Parameters:**
- `lobbyName`: Display name for lobby
- `trackName`: Race track selection
- `lapCount`: Number of laps to complete

**Returns:** RaceLobby struct with generated IDs

**Example:**
```csharp
var lobby = multiplayerManager.CreateLobby(
    "Monaco Speedrun",
    "Monaco",
    5
);
```

#### JoinLobby
```csharp
bool JoinLobby(string lobbyId, string carColor = "#0000FF")
```
Join an existing race lobby.

**Parameters:**
- `lobbyId`: Target lobby ID
- `carColor`: Vehicle color (hex code)

**Returns:** true if successful, false if lobby full/racing

**Example:**
```csharp
bool joined = multiplayerManager.JoinLobby(
    "lobby-uuid-12345",
    "#FF0000"
);
```

#### LeaveLobby
```csharp
void LeaveLobby()
```
Leave current lobby. Closes lobby if host.

#### SetPlayerReady
```csharp
void SetPlayerReady(bool ready)
```
Mark player as ready to start race.

**Example:**
```csharp
multiplayerManager.SetPlayerReady(true);
```

### Race Control

#### StartRace
```csharp
bool StartRace()
```
Host starts race (requires all players ready).

**Returns:** true if race started, false if not ready

**Example:**
```csharp
if (multiplayerManager.StartRace())
{
    Debug.Log("Race starting!");
}
```

#### FinishRace
```csharp
void FinishRace()
```
End current race and return to lobby.

### Matchmaking

#### GetAvailableLobbies
```csharp
List<RaceLobby> GetAvailableLobbies()
```
Get all available lobbies (not racing, not full).

**Returns:** List of joinable lobbies

**Example:**
```csharp
var lobbies = multiplayerManager.GetAvailableLobbies();
foreach (var lobby in lobbies)
{
    Debug.Log($"{lobby.LobbyName} - {lobby.Players.Count}/{lobby.MaxPlayers}");
}
```

#### GetLobbiesBySkillRange
```csharp
List<RaceLobby> GetLobbiesBySkillRange(int minRating, int maxRating)
```
Get lobbies matching skill rating range.

**Parameters:**
- `minRating`: Minimum skill rating
- `maxRating`: Maximum skill rating

**Example:**
```csharp
var balancedLobbies = multiplayerManager.GetLobbiesBySkillRange(1400, 1600);
```

### Social Features

#### AddFriend
```csharp
void AddFriend(string friendPlayerId)
```
Add player to friends list.

#### BlockPlayer
```csharp
void BlockPlayer(string blockedPlayerId)
```
Block player from invites and matchmaking.

### Race Data

#### GetNetworkPlayers
```csharp
Dictionary<string, NetworkPlayerState> GetNetworkPlayers()
```
Get current network state for all players.

**Returns:** Dictionary of player ID → network state

**Example:**
```csharp
var states = multiplayerManager.GetNetworkPlayers();
foreach (var (playerId, state) in states)
{
    Debug.Log($"{playerId}: Position {state.Position_Rank}, Speed {state.Speed}");
}
```

#### GetPlayerRanking
```csharp
int GetPlayerRanking(string playerId)
```
Get player's current race position.

**Returns:** Position (1=first) or -1 if not racing

### Skill System

#### UpdateSkillRating
```csharp
void UpdateSkillRating(int racePosition, int totalParticipants)
```
Update ELO rating after race completion.

**Calculation:**
- Base: (totalParticipants - position) × 25
- Win Bonus: +100
- Minimum Rating: 800

**Example:**
```csharp
// Finished 2nd of 4 players
multiplayerManager.UpdateSkillRating(2, 4);
// Rating: 800 + 50 = 850 (next race)
```

## Skill Rating System

### ELO Rating Tiers

| Rating | Tier | Players |
|--------|------|---------|
| 800-1200 | Bronze | 35% |
| 1200-1400 | Silver | 30% |
| 1400-1600 | Gold | 20% |
| 1600-1800 | Platinum | 10% |
| 1800+ | Diamond | 5% |

### Rating Changes Per Race

```
Base Change = (Total Players - Your Position) × 25

1st Place: +75 (3rd) or +100 (4th) + 100 bonus = 200
2nd Place: +50 (3rd) or +75 (4th) + 0 = 75
3rd Place: +25 (3rd) or +50 (4th) + 0 = 50
Last Place: +0 or +25 = 0-25
```

## Network Synchronization

### Update Frequency
- **Position/Rotation**: 20 Hz (50ms interval)
- **Lap Data**: 10 Hz (100ms interval)
- **Player Rankings**: 5 Hz (200ms interval)

### Latency Compensation
- **Prediction**: Linear extrapolation of position
- **Lag Compensation**: ~100ms client-side prediction
- **Reconciliation**: Server authoritative updates

### Bandwidth Usage
- **Per Update**: ~100 bytes per player
- **Per Player**: ~2 KB/s (worst case)
- **8 Players**: ~16 KB/s downstream

## Integration Example

### Complete Multiplayer Race Flow

```csharp
public class MultiplayerRaceController : MonoBehaviour
{
    private MultiplayerRaceManager multiplayerManager;
    private VehicleController vehicleController;

    void Start()
    {
        multiplayerManager = GetComponent<MultiplayerRaceManager>();
    }

    public void CreateAndJoinRace()
    {
        // Create new lobby
        var lobby = multiplayerManager.CreateLobby(
            "Practice Race",
            "Monza",
            3
        );

        Debug.Log($"Lobby created: {lobby.LobbyName}");
    }

    public void BrowseAndJoin()
    {
        // Get available lobbies in skill range
        int myRating = multiplayerManager.GetSkillRating;
        var lobbies = multiplayerManager.GetLobbiesBySkillRange(
            myRating - 200,
            myRating + 200
        );

        if (lobbies.Count > 0)
        {
            // Join first available
            multiplayerManager.JoinLobby(lobbies[0].LobbyId, "#0000FF");
        }
    }

    public void StartRaceWhenReady()
    {
        var lobby = multiplayerManager.GetCurrentLobby();

        // Check if all players ready
        if (lobby.Players.All(p => p.IsReady))
        {
            if (multiplayerManager.StartRace())
            {
                Debug.Log("Race started!");
            }
        }
    }

    void Update()
    {
        if (!multiplayerManager.IsRaceActive)
            return;

        // Get current network state
        var players = multiplayerManager.GetNetworkPlayers();

        // Update HUD with rankings
        foreach (var (playerId, state) in players)
        {
            Debug.Log($"#{state.Position_Rank}: {state.Speed:F1} m/s");
        }

        // Sync local vehicle position
        SyncLocalPlayerState();
    }

    void SyncLocalPlayerState()
    {
        // In production: serialize vehicle controller state
        // and send to network at configured update rate
    }

    public void FinishRace()
    {
        var players = multiplayerManager.GetNetworkPlayers();
        int finalPosition = multiplayerManager.GetPlayerRanking(
            multiplayerManager.GetCurrentLobby().HostId
        );

        // Update skill rating
        multiplayerManager.UpdateSkillRating(
            finalPosition,
            players.Count
        );

        multiplayerManager.FinishRace();
    }
}
```

## Network Architecture

### Connection Flow

```
1. Player creates/joins lobby
   └─ Lobby state synced to all players

2. All players ready
   └─ Host initiates race start

3. Race begins
   └─ Network updates every 50ms
   └─ Vehicle states synchronized
   └─ Rankings updated every 200ms

4. Race completes
   └─ Final results calculated
   └─ Skill ratings updated
   └─ Statistics saved
```

### State Synchronization

```
Local Player State
    ↓
Serialize (20 Hz)
    ↓
Send to Server
    ↓
Server Validates & Broadcasts
    ↓
Other Clients Receive
    ↓
Deserialize & Update Remote Players
    ↓
Interpolate Position for Smooth Display
```

## Disconnection Handling

### Player Disconnection
- **Timeout**: 5 second grace period
- **Rejoin**: Same session within 5 seconds
- **Replacement**: AI takes over after timeout
- **Cleanup**: Remove from rankings after 10 seconds

### Server Disconnection
- **Fallback**: Switch to peer-to-peer (P2P)
- **Recovery**: Reconnect on server restore
- **Validation**: Verify race integrity

## Matchmaking Algorithm

```
Input: Player skill rating, preferred track, playtime
Output: Recommended lobbies ranked by compatibility

Ranking Score =
  (100 - skill_distance) × 0.4 +
  track_match_score × 0.3 +
  player_count_balance × 0.2 +
  latency_score × 0.1

Return top 5 lobbies sorted by score
```

## Anti-Cheat Measures

- **Server Authority**: Vehicle positions validated server-side
- **Speed Validation**: Detect impossible speeds/accelerations
- **Lap Time Validation**: Cross-check with track data
- **Rate Limiting**: Prevent spam updates
- **Behavior Monitoring**: Track unusual patterns

## Future Enhancements

- [ ] Team racing (2v2, 4v4)
- [ ] Spectator mode
- [ ] Replays with multiplayer overlay
- [ ] Tournaments and seasons
- [ ] Leaderboards (global, friends, monthly)
- [ ] Custom lobbies (time limits, damage, fuel)
- [ ] Voice chat integration
- [ ] Cross-platform multiplayer
- [ ] Clan/team system
- [ ] Ranking seasons with rewards

## Performance Optimization

### Bandwidth Reduction
- Delta compression (send only changes)
- Quantization of float values (reduced precision)
- Interest-based culling (only sync nearby players)

### CPU Optimization
- Object pooling for network messages
- Efficient serialization format
- Batched updates when possible

### Memory Management
- Limit history size (100 frames max)
- Clean up disconnected players
- Compress old match data

---

**Status**: ✓ Complete and integrated
**Last Updated**: 2026-03-27
**Network Update Rate**: 20 Hz
**Max Players Per Lobby**: 8
