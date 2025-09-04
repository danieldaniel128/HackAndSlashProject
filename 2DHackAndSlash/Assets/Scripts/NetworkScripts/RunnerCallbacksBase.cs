using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public abstract class RunnerCallbacksBase : MonoBehaviour, INetworkRunnerCallbacks
{
    // Player lifecycle
    public virtual void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public virtual void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    // Connection lifecycle
    public virtual void OnConnectedToServer(NetworkRunner runner) { }
    public virtual void OnDisconnectedFromServer(NetworkRunner runner) { }
    public virtual void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public virtual void OnConnectFailed(NetworkRunner runner, NetAddress address, NetConnectFailedReason reason) { }
    public virtual void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { request.Accept(); }

    // Sessions
    public virtual void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    // Simulation and input
    public virtual void OnInput(NetworkRunner runner, NetworkInput input) { }
    public virtual void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public virtual void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    // Reliable data channel
    public virtual void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public virtual void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public virtual void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    // Scenes
    public virtual void OnSceneLoadStart(NetworkRunner runner) { }
    public virtual void OnSceneLoadDone(NetworkRunner runner) { }

    // AOI streaming
    public virtual void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public virtual void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    // Shutdown and auth
    public virtual void OnShutdown(NetworkRunner runner, ShutdownReason reason) { }
    public virtual void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public virtual void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
}
