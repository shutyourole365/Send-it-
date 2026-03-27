using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace SendIt.Network
{
    /// <summary>
    /// Multiplayer racing system with lobby, matchmaking, and network synchronization.
    /// Manages competitive multiplayer races with real-time vehicle state sync.
    /// </summary>
    public class MultiplayerRaceManager : MonoBehaviour
    {
        /// <summary>
        /// Player lobby data.
        /// </summary>
        public struct LobbyPlayer
        {
            public string PlayerId;
            public string PlayerName;
            public int SkillRating;
            public string CarColor;
            public bool IsReady;
            public System.DateTime JoinTime;
        }

        /// <summary>
        /// Lobby room data.
        /// </summary>
        public struct RaceLobby
        {
            public string LobbyId;
            public string LobbyName;
            public string HostId;
            public List<LobbyPlayer> Players;
            public int MaxPlayers;
            public string TrackName;
            public int LapCount;
            public bool IsRacing;
            public System.DateTime CreatedTime;
        }

        /// <summary>
        /// Network player state.
        /// </summary>
        public struct NetworkPlayerState
        {
            public string PlayerId;
            public Vector3 Position;
            public Quaternion Rotation;
            public float Speed;
            public int CurrentLap;
            public float LapTime;
            public int Position_Rank;
            public System.DateTime LastUpdate;
        }

        [SerializeField] private string playerId;
        [SerializeField] private string playerName = "Player";
        [SerializeField] private int maxLobbies = 50;
        [SerializeField] private int maxPlayersPerLobby = 8;

        private List<RaceLobby> activeLobbies = new List<RaceLobby>();
        private RaceLobby currentLobby;
        private bool inLobby;
        private bool raceActive;

        // Network state
        private Dictionary<string, NetworkPlayerState> networkPlayers = new Dictionary<string, NetworkPlayerState>();
        private NetworkPlayerState localPlayerState;
        private float networkUpdateInterval = 0.05f; // 20 updates per second
        private float networkUpdateTimer;

        // Matchmaking
        private int skillRating = 1500; // ELO-style rating
        private List<string> friendsList = new List<string>();
        private List<string> blockedPlayers = new List<string>();

        private const string lobbyUpdateChannel = "lobby_updates";
        private const string raceDataChannel = "race_data";

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (string.IsNullOrEmpty(playerId))
                playerId = System.Guid.NewGuid().ToString();

            Debug.Log($"Multiplayer manager initialized for {playerName} ({playerId})");
        }

        private void Update()
        {
            if (raceActive)
            {
                networkUpdateTimer += Time.deltaTime;
                if (networkUpdateTimer >= networkUpdateInterval)
                {
                    UpdateNetworkPlayerState();
                    networkUpdateTimer = 0f;
                }
            }
        }

        /// <summary>
        /// Create a new race lobby.
        /// </summary>
        public RaceLobby CreateLobby(string lobbyName, string trackName, int lapCount)
        {
            if (activeLobbies.Count >= maxLobbies)
            {
                Debug.LogWarning("Maximum lobbies reached.");
                return new RaceLobby();
            }

            var lobby = new RaceLobby
            {
                LobbyId = System.Guid.NewGuid().ToString(),
                LobbyName = lobbyName,
                HostId = playerId,
                Players = new List<LobbyPlayer>(),
                MaxPlayers = maxPlayersPerLobby,
                TrackName = trackName,
                LapCount = lapCount,
                IsRacing = false,
                CreatedTime = System.DateTime.Now
            };

            // Add host as first player
            var hostPlayer = new LobbyPlayer
            {
                PlayerId = playerId,
                PlayerName = playerName,
                SkillRating = skillRating,
                CarColor = "#FF0000",
                IsReady = false,
                JoinTime = System.DateTime.Now
            };
            lobby.Players.Add(hostPlayer);

            activeLobbies.Add(lobby);
            currentLobby = lobby;
            inLobby = true;

            Debug.Log($"Lobby created: {lobbyName} on {trackName}");
            return lobby;
        }

        /// <summary>
        /// Join an existing lobby.
        /// </summary>
        public bool JoinLobby(string lobbyId, string carColor = "#0000FF")
        {
            var lobbyIndex = activeLobbies.FindIndex(l => l.LobbyId == lobbyId);
            if (lobbyIndex == -1)
            {
                Debug.LogWarning($"Lobby not found: {lobbyId}");
                return false;
            }

            var lobby = activeLobbies[lobbyIndex];

            if (lobby.Players.Count >= lobby.MaxPlayers)
            {
                Debug.LogWarning("Lobby is full.");
                return false;
            }

            if (lobby.IsRacing)
            {
                Debug.LogWarning("Race already in progress.");
                return false;
            }

            var newPlayer = new LobbyPlayer
            {
                PlayerId = playerId,
                PlayerName = playerName,
                SkillRating = skillRating,
                CarColor = carColor,
                IsReady = false,
                JoinTime = System.DateTime.Now
            };

            lobby.Players.Add(newPlayer);
            activeLobbies[lobbyIndex] = lobby;
            currentLobby = lobby;
            inLobby = true;

            Debug.Log($"Joined lobby: {lobby.LobbyName}");
            return true;
        }

        /// <summary>
        /// Leave current lobby.
        /// </summary>
        public void LeaveLobby()
        {
            if (!inLobby)
                return;

            var lobbyIndex = activeLobbies.FindIndex(l => l.LobbyId == currentLobby.LobbyId);
            if (lobbyIndex != -1)
            {
                var lobby = activeLobbies[lobbyIndex];

                // Remove player from lobby
                lobby.Players.RemoveAll(p => p.PlayerId == playerId);

                // If host left, close lobby
                if (lobby.HostId == playerId || lobby.Players.Count == 0)
                {
                    activeLobbies.RemoveAt(lobbyIndex);
                    Debug.Log($"Lobby closed: {lobby.LobbyName}");
                }
                else
                {
                    activeLobbies[lobbyIndex] = lobby;
                }
            }

            inLobby = false;
            currentLobby = new RaceLobby();
        }

        /// <summary>
        /// Mark player as ready in lobby.
        /// </summary>
        public void SetPlayerReady(bool ready)
        {
            if (!inLobby)
                return;

            var lobbyIndex = activeLobbies.FindIndex(l => l.LobbyId == currentLobby.LobbyId);
            if (lobbyIndex != -1)
            {
                var lobby = activeLobbies[lobbyIndex];
                var playerIndex = lobby.Players.FindIndex(p => p.PlayerId == playerId);

                if (playerIndex != -1)
                {
                    var player = lobby.Players[playerIndex];
                    player.IsReady = ready;
                    lobby.Players[playerIndex] = player;
                    activeLobbies[lobbyIndex] = lobby;
                    currentLobby = lobby;
                }
            }

            Debug.Log($"Player ready status: {ready}");
        }

        /// <summary>
        /// Start race if all players ready.
        /// </summary>
        public bool StartRace()
        {
            if (!inLobby || currentLobby.HostId != playerId)
            {
                Debug.LogWarning("Only host can start race.");
                return false;
            }

            // Check if all players ready
            if (!currentLobby.Players.All(p => p.IsReady))
            {
                Debug.LogWarning("Not all players are ready.");
                return false;
            }

            var lobbyIndex = activeLobbies.FindIndex(l => l.LobbyId == currentLobby.LobbyId);
            if (lobbyIndex != -1)
            {
                var lobby = activeLobbies[lobbyIndex];
                lobby.IsRacing = true;
                activeLobbies[lobbyIndex] = lobby;
                currentLobby = lobby;
            }

            raceActive = true;
            networkPlayers.Clear();

            // Initialize network states for all players
            foreach (var player in currentLobby.Players)
            {
                networkPlayers[player.PlayerId] = new NetworkPlayerState
                {
                    PlayerId = player.PlayerId,
                    Position = Vector3.zero,
                    Rotation = Quaternion.identity,
                    Speed = 0f,
                    CurrentLap = 0,
                    LapTime = 0f,
                    Position_Rank = 1
                };
            }

            Debug.Log($"Race started on {currentLobby.TrackName}");
            return true;
        }

        /// <summary>
        /// Finish race.
        /// </summary>
        public void FinishRace()
        {
            raceActive = false;
            var lobbyIndex = activeLobbies.FindIndex(l => l.LobbyId == currentLobby.LobbyId);
            if (lobbyIndex != -1)
            {
                var lobby = activeLobbies[lobbyIndex];
                lobby.IsRacing = false;
                activeLobbies[lobbyIndex] = lobby;
                currentLobby = lobby;
            }

            Debug.Log("Race finished");
        }

        /// <summary>
        /// Update local player network state.
        /// </summary>
        private void UpdateNetworkPlayerState()
        {
            localPlayerState.PlayerId = playerId;
            localPlayerState.LastUpdate = System.DateTime.Now;

            // In production, would sync with actual vehicle controller
            // vehicleController.GetPosition(), GetRotation(), GetSpeed(), etc.

            // Update ranking based on lap times
            UpdatePlayerRankings();
        }

        /// <summary>
        /// Update player rankings based on lap times.
        /// </summary>
        private void UpdatePlayerRankings()
        {
            var playersList = networkPlayers.Values.ToList();
            playersList.Sort((a, b) =>
            {
                int lapCompare = b.CurrentLap.CompareTo(a.CurrentLap);
                if (lapCompare != 0)
                    return lapCompare;

                return a.LapTime.CompareTo(b.LapTime);
            });

            for (int i = 0; i < playersList.Count; i++)
            {
                var state = playersList[i];
                state.Position_Rank = i + 1;
                networkPlayers[state.PlayerId] = state;
            }
        }

        /// <summary>
        /// Get available lobbies for matchmaking.
        /// </summary>
        public List<RaceLobby> GetAvailableLobbies()
        {
            return activeLobbies
                .Where(l => !l.IsRacing && l.Players.Count < l.MaxPlayers)
                .ToList();
        }

        /// <summary>
        /// Get lobbies by skill rating range.
        /// </summary>
        public List<RaceLobby> GetLobbiesBySkillRange(int minRating, int maxRating)
        {
            return GetAvailableLobbies()
                .Where(l => l.Players.Any(p => p.SkillRating >= minRating && p.SkillRating <= maxRating))
                .ToList();
        }

        /// <summary>
        /// Add friend to friends list.
        /// </summary>
        public void AddFriend(string friendPlayerId)
        {
            if (!friendsList.Contains(friendPlayerId))
                friendsList.Add(friendPlayerId);
        }

        /// <summary>
        /// Block player.
        /// </summary>
        public void BlockPlayer(string blockedPlayerId)
        {
            if (!blockedPlayers.Contains(blockedPlayerId))
                blockedPlayers.Add(blockedPlayerId);
        }

        /// <summary>
        /// Get current lobby.
        /// </summary>
        public RaceLobby GetCurrentLobby() => currentLobby;

        /// <summary>
        /// Get network player states.
        /// </summary>
        public Dictionary<string, NetworkPlayerState> GetNetworkPlayers() =>
            new Dictionary<string, NetworkPlayerState>(networkPlayers);

        /// <summary>
        /// Get player ranking in current race.
        /// </summary>
        public int GetPlayerRanking(string playerId)
        {
            return networkPlayers.ContainsKey(playerId) ?
                networkPlayers[playerId].Position_Rank : -1;
        }

        /// <summary>
        /// Update skill rating after race.
        /// </summary>
        public void UpdateSkillRating(int racePosition, int totalParticipants)
        {
            // ELO-style rating update
            int ratingChange = (totalParticipants - racePosition) * 25; // 25 points per position

            if (racePosition == 1)
                ratingChange += 100; // Win bonus

            skillRating = Mathf.Max(800, skillRating + ratingChange);
            Debug.Log($"Skill rating updated: {skillRating} ({ratingChange:+0;-#})");
        }

        /// <summary>
        /// Get multiplayer statistics.
        /// </summary>
        public string GetMultiplayerStats()
        {
            return $@"
=== MULTIPLAYER STATISTICS ===
Player ID: {playerId}
Player Name: {playerName}
Skill Rating: {skillRating}
Active Lobbies: {activeLobbies.Count}/{maxLobbies}
In Lobby: {inLobby}
Race Active: {raceActive}
Friends: {friendsList.Count}
Blocked: {blockedPlayers.Count}
";
        }

        public bool IsInLobby => inLobby;
        public bool IsRaceActive => raceActive;
        public int GetLobbyCount => activeLobbies.Count;
        public int GetSkillRating => skillRating;
    }
}
