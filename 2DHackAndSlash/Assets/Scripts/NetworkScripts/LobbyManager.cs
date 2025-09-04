using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{

    [Header("UI")]
    [SerializeField] private Button _hostBtn;
    [SerializeField] private Button _joinBtn;
    [SerializeField] private TextMeshProUGUI _status;

    private List<SessionInfo> _sessions = new();

    async void Awake()
    {
        var runner = RunnerBootstrap.Runner;
        if (runner == null)
        {
            Debug.LogError("No RunnerBootstrap in scene. Make sure it exists in Lobby scene.");
            return;
        }

        runner.AddCallbacks(this);

        // Explicitly join the lobby so OnSessionListUpdated starts firing
        await runner.JoinSessionLobby(SessionLobby.ClientServer);

        _hostBtn.onClick.AddListener(HostGame);
        _joinBtn.onClick.AddListener(JoinFirstAvailable);
    }

    // === Host a new session ===
    private async void HostGame()
    {
        if (RunnerBootstrap.Runner.IsRunning)
        {
            Debug.LogWarning("Runner already running. Skipping StartGame.");
            return;
        }

        _status.text = "Hosting...";
        await RunnerBootstrap.Runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = "Room_" + Random.Range(0, 9999),
            SceneManager = RunnerBootstrap.Runner.GetComponent<NetworkSceneManagerDefault>(),
            Scene = SceneRef.FromIndex(1) // your Game scene index
        });
    }

    private async void JoinFirstAvailable()
    {
        if (_sessions.Count > 0)
        {
            if (RunnerBootstrap.Runner.IsRunning)
            {
                Debug.LogWarning("Runner already running. Skipping StartGame.");
                return;
            }

            var chosen = _sessions[0];
            _status.text = $"Joining {chosen.Name}";

            await RunnerBootstrap.Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = chosen.Name,
                SceneManager = RunnerBootstrap.Runner.GetComponent<NetworkSceneManagerDefault>()
                // no Scene for clients
            });
        }
        else
        {
            _status.text = "No sessions found, hosting instead";
            HostGame();
        }
    }

    // === Session list updates ===
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessions)
    {
        _sessions = sessions;
        _status.text = $"Sessions found: {sessions.Count}";
        Debug.Log($"[Lobby] Found {sessions.Count} sessions");
    }

    // === Player lifecycle ===
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    // === Connection lifecycle ===
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress addr, NetConnectFailedReason reason) { }

    // === Data channels ===
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, System.ArraySegment<byte> data) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    // === Simulation & input ===
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr msg) { }

    // === Scenes ===
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }

    // === AOI ===
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    // === Misc ===
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token)
    {
        req.Accept();
    }
}
