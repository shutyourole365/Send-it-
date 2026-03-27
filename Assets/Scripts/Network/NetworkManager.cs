using UnityEngine;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SendIt.Network
{
    /// <summary>
    /// Network manager for multiplayer racing.
    /// Handles peer-to-peer and client-server communication.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        [SerializeField] private int port = 7777;
        [SerializeField] private string serverAddress = "localhost";
        [SerializeField] private bool isServer = false;
        [SerializeField] private bool autoConnect = true;

        private UdpClient udpClient;
        private TcpClient tcpClient;
        private NetworkStream networkStream;

        private Dictionary<string, RemotePlayer> remotePlayers = new Dictionary<string, RemotePlayer>();
        private string localPlayerId;
        private bool isConnected;

        // Network state
        private struct RemotePlayer
        {
            public string PlayerId;
            public Vector3 Position;
            public Quaternion Rotation;
            public float Speed;
            public int CurrentLap;
            public float LapTime;
        }

        private struct NetworkMessage
        {
            public string Type;      // "state", "input", "lap", "race_end"
            public string PlayerId;
            public Vector3 Position;
            public Quaternion Rotation;
            public float Speed;
            public int CurrentLap;
            public float LapTime;
            public float Timestamp;
        }

        private static NetworkManager instance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void Start()
        {
            if (autoConnect)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        /// <summary>
        /// Initialize network connection.
        /// </summary>
        public void Initialize()
        {
            localPlayerId = System.Guid.NewGuid().ToString();

            if (isServer)
            {
                StartServer();
            }
            else
            {
                ConnectToServer();
            }
        }

        /// <summary>
        /// Start as server.
        /// </summary>
        private void StartServer()
        {
            try
            {
                udpClient = new UdpClient(port);
                isConnected = true;

                Debug.Log($"Network Server started on port {port}");

                // Start listening for incoming messages
                _ = ListenForIncomingMessages();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to start server: {e.Message}");
            }
        }

        /// <summary>
        /// Connect to server.
        /// </summary>
        private void ConnectToServer()
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.ConnectAsync(serverAddress, port).Wait(5000);

                networkStream = tcpClient.GetStream();
                isConnected = true;

                Debug.Log($"Connected to server at {serverAddress}:{port}");

                // Send initial hello message
                SendHelloMessage();

                // Start listening for updates
                _ = ListenForServerMessages();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to connect to server: {e.Message}");
                isConnected = false;
            }
        }

        /// <summary>
        /// Send hello message to server.
        /// </summary>
        private void SendHelloMessage()
        {
            var message = new NetworkMessage
            {
                Type = "hello",
                PlayerId = localPlayerId,
                Timestamp = Time.time
            };

            SendMessage(message);
        }

        /// <summary>
        /// Listen for incoming messages (server).
        /// </summary>
        private async Task ListenForIncomingMessages()
        {
            while (isConnected && udpClient != null)
            {
                try
                {
                    IPEndPoint remoteEP = null;
                    byte[] data = udpClient.Receive(ref remoteEP);

                    string json = Encoding.UTF8.GetString(data);
                    var message = JsonUtility.FromJson<NetworkMessage>(json);

                    ProcessMessage(message);
                }
                catch (Exception e)
                {
                    if (isConnected)
                        Debug.LogError($"Error receiving message: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Listen for server messages (client).
        /// </summary>
        private async Task ListenForServerMessages()
        {
            byte[] buffer = new byte[1024];

            while (isConnected && networkStream != null)
            {
                try
                {
                    int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        Debug.LogWarning("Connection closed by server");
                        Disconnect();
                        return;
                    }

                    string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var message = JsonUtility.FromJson<NetworkMessage>(json);

                    ProcessMessage(message);
                }
                catch (Exception e)
                {
                    if (isConnected)
                        Debug.LogError($"Error receiving message: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Process incoming network message.
        /// </summary>
        private void ProcessMessage(NetworkMessage message)
        {
            switch (message.Type)
            {
                case "state":
                    UpdateRemotePlayerState(message);
                    break;

                case "lap":
                    ProcessLapUpdate(message);
                    break;

                case "race_end":
                    ProcessRaceEnd(message);
                    break;

                case "hello":
                    // Player joined, acknowledge
                    Debug.Log($"Player joined: {message.PlayerId}");
                    break;
            }
        }

        /// <summary>
        /// Update remote player state.
        /// </summary>
        private void UpdateRemotePlayerState(NetworkMessage message)
        {
            if (!remotePlayers.ContainsKey(message.PlayerId))
            {
                remotePlayers[message.PlayerId] = new RemotePlayer();
            }

            var player = remotePlayers[message.PlayerId];
            player.PlayerId = message.PlayerId;
            player.Position = message.Position;
            player.Rotation = message.Rotation;
            player.Speed = message.Speed;
            player.CurrentLap = message.CurrentLap;
            player.LapTime = message.LapTime;

            remotePlayers[message.PlayerId] = player;
        }

        /// <summary>
        /// Process lap update.
        /// </summary>
        private void ProcessLapUpdate(NetworkMessage message)
        {
            Debug.Log($"Player {message.PlayerId} completed lap {message.CurrentLap} in {message.LapTime:F2}s");
            UpdateRemotePlayerState(message);
        }

        /// <summary>
        /// Process race end notification.
        /// </summary>
        private void ProcessRaceEnd(NetworkMessage message)
        {
            Debug.Log($"Race ended for player {message.PlayerId}");
        }

        /// <summary>
        /// Send player state update.
        /// </summary>
        public void SendPlayerStateUpdate(Vector3 position, Quaternion rotation, float speed, int lap, float lapTime)
        {
            var message = new NetworkMessage
            {
                Type = "state",
                PlayerId = localPlayerId,
                Position = position,
                Rotation = rotation,
                Speed = speed,
                CurrentLap = lap,
                LapTime = lapTime,
                Timestamp = Time.time
            };

            SendMessage(message);
        }

        /// <summary>
        /// Send lap completion.
        /// </summary>
        public void SendLapComplete(int lapNumber, float lapTime)
        {
            var message = new NetworkMessage
            {
                Type = "lap",
                PlayerId = localPlayerId,
                CurrentLap = lapNumber,
                LapTime = lapTime,
                Timestamp = Time.time
            };

            SendMessage(message);
        }

        /// <summary>
        /// Send race end.
        /// </summary>
        public void SendRaceEnd(int finalPosition)
        {
            var message = new NetworkMessage
            {
                Type = "race_end",
                PlayerId = localPlayerId,
                Timestamp = Time.time
            };

            SendMessage(message);
        }

        /// <summary>
        /// Send message to network.
        /// </summary>
        private void SendMessage(NetworkMessage message)
        {
            if (!isConnected)
            {
                Debug.LogWarning("Not connected to network");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(message);
                byte[] data = Encoding.UTF8.GetBytes(json);

                if (isServer && udpClient != null)
                {
                    // Broadcast to all clients (simplified)
                }
                else if (networkStream != null)
                {
                    networkStream.Write(data, 0, data.Length);
                    networkStream.Flush();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to send message: {e.Message}");
            }
        }

        /// <summary>
        /// Get remote players.
        /// </summary>
        public Dictionary<string, RemotePlayer> GetRemotePlayers() =>
            new Dictionary<string, RemotePlayer>(remotePlayers);

        /// <summary>
        /// Disconnect from network.
        /// </summary>
        public void Disconnect()
        {
            isConnected = false;

            if (networkStream != null)
            {
                networkStream.Close();
                networkStream = null;
            }

            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient = null;
            }

            if (udpClient != null)
            {
                udpClient.Close();
                udpClient = null;
            }

            remotePlayers.Clear();

            Debug.Log("Disconnected from network");
        }

        /// <summary>
        /// Check if connected.
        /// </summary>
        public bool IsConnected => isConnected;

        /// <summary>
        /// Get local player ID.
        /// </summary>
        public string GetLocalPlayerId() => localPlayerId;

        /// <summary>
        /// Get instance.
        /// </summary>
        public static NetworkManager Instance => instance;
    }
}
