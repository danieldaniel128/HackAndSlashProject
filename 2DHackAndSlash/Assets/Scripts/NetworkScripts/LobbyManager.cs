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

    private NetworkRunner _runner;
    private List<SessionInfo> _sessions = new();

    void Awake()
    {
        // The NetworkRunner drives Fusion’s simulation loop.
        _hostBtn.onClick.AddListener(() => HostGame());
        _joinBtn.onClick.AddListener(() => JoinFirstAvailable());
    }

    // === Host a new session ===
    async void HostGame()
    {
        if (RunnerBootstrap.Runner.IsRunning)
        {
            Debug.LogWarning("Runner already running. Skipping StartGame.");
            return;
        }

        _status.text = "Hosting...";
        await RunnerBootstrap.Runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = "Room_" + Random.Range(0, 9999),
            SceneManager = RunnerBootstrap.Runner.GetComponent<NetworkSceneManagerDefault>(),
            Scene = SceneRef.FromIndex(1) // Game.unity index
        });
    }

    async void JoinFirstAvailable()
    {
        if (_sessions.Count > 0)
        {
            if (RunnerBootstrap.Runner.IsRunning)
            {
                Debug.LogWarning("Runner already running. Skipping StartGame.");
                return;
            }

            _status.text = "Joining " + _sessions[0].Name;
            await RunnerBootstrap.Runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = _sessions[0].Name,
                SceneManager = RunnerBootstrap.Runner.GetComponent<NetworkSceneManagerDefault>()//change to runnerBootstrap later.
                // No Scene here for joiners
            });
        }
        else
        {
            _status.text = "No sessions found, hosting instead";
            HostGame();
        }
    }

    // === Session list updates (when using Fusion’s lobby service) ===
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessions)
    {
        _sessions = sessions;
        _status.text = $"Sessions found: {sessions.Count}";
    }

    // === Player lifecycle ===
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Called when someone joins the session (before spawning).
        // Usually the server/host will Spawn() the player prefab here (but in Game scene, not in Lobby).
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Called when a player leaves (disconnect, quit).
        // Use this to clean up their spawned objects.
    }

    // === Connection lifecycle ===
    public void OnConnectedToServer(NetworkRunner runner)
    {
        // Called when this client successfully connects to Photon server.
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        // (older signature) Called when we disconnect from server.
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        // (newer signature) Same as above but with reason info.
        // You might show UI: "Disconnected: reason"
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress addr, NetConnectFailedReason reason)
    {
        // Called when connect attempt fails (wrong AppId, no internet, etc.).
    }

    // === Data channels ===
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, System.ArraySegment<byte> data)
    {
        // Legacy: a reliable payload arrived (RPC-like custom messages).
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data)
    {
        // New signature: includes a ReliableKey so you can track chunked transfers.
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        // Progress for large reliable messages (streaming a file or asset).
        // Range is 0.0 to 1.0. Useful for showing a loading bar.
    }


    // === Simulation & input ===
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Called every simulation tick if ProvideInput = true.
        // You fill a struct here (e.g., movement, buttons).
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        // Called when a client didn’t send input in time for a tick.
        // You can guess/predict input (e.g., repeat last) to keep simulation smooth.
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr msg)
    {
        // Custom low-level simulation messages (advanced use).
    }

    // === Scenes ===
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        // Called before Fusion starts loading a new scene.
        // You could show a loading UI here.
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // Called when the scene finished loading on this client.
        // Often you hook your spawner here.
    }

    // === AOI (Area of Interest) ===
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // Called when an object enters this player’s area of interest (streaming in).
        // Useful for large worlds where not everything is replicated to everyone.
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // Called when an object leaves AOI (streaming out).
        // You could disable visuals here.
    }

    // === Misc ===
    public void OnShutdown(NetworkRunner runner, ShutdownReason reason)
    {
        // Runner is shutting down (session ended, host left, etc.).
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        // Response from custom auth service (if using Photon custom auth).
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token)
    {
        // Called when the host leaves but Fusion promotes another peer to host.
        // You can migrate state using the token.
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token)
    {
        // Server side: a new client wants to join.
        // Call req.Accept() or req.Refuse().
    }
}
