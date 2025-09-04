using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : RunnerCallbacksBase
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
            SceneManager = RunnerBootstrap.SceneManager,
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
    public override void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessions)
    {
        _sessions = sessions;
        _status.text = $"Sessions found: {sessions.Count}";
        Debug.Log($"[Lobby] Found {sessions.Count} sessions");
    }
    
    public override void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token)
    {
        req.Accept();
    }
}
