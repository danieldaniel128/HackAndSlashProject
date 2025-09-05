using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameSpawner : RunnerCallbacksBase
{
    [SerializeField] private NetworkPrefabRef _playerPrefab;  // from Prefab Table
    [SerializeField] private Transform[] _spawnPoints;

    private bool _sceneReady = false;
    private readonly HashSet<PlayerRef> _pending = new HashSet<PlayerRef>();



    void OnEnable()
    {
        // Be robust: find the existing persistent runner from Lobby
        var runner = RunnerBootstrap.Runner;
        if (runner) runner.AddCallbacks(this);
        else Debug.LogWarning("[Spawner] No NetworkRunner found in scene.");
    }

    void OnDisable()
    {
        var runner = RunnerBootstrap.Runner;
        if (runner) runner.RemoveCallbacks(this);
    }
    public override void OnSceneLoadDone(NetworkRunner runner)
    {
        _sceneReady = true;
        if (!runner.IsServer) return;
        Debug.Log("scene load done");
        // Spawn for everyone already in the session
        foreach (var player in runner.ActivePlayers)
        {
            if (!runner.TryGetPlayerObject(player, out _))
            {
                SpawnFor(runner, player);
            }
        }

        // Also handle any who joined while the scene was loading
        foreach (var p in _pending)
        {
            if (!runner.TryGetPlayerObject(p, out _)) SpawnFor(runner, p);
        }
        _pending.Clear();
    }

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log("player joined my method");
        if (!runner.IsServer) return;

        if (_sceneReady)
        {
            if (!runner.TryGetPlayerObject(player, out _)) SpawnFor(runner, player);
        }
        else
        {
            _pending.Add(player); // will spawn in OnSceneLoadDone
        }
    }

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && runner.TryGetPlayerObject(player, out var obj))
            runner.Despawn(obj);
        _pending.Remove(player);
    }

    private void SpawnFor(NetworkRunner runner, PlayerRef player)
    {
        int idx = player.RawEncoded % Mathf.Max(1, _spawnPoints.Length);
        Vector3 pos = _spawnPoints.Length > 0 ? _spawnPoints[idx].position : Vector3.zero;
        var avatar = runner.Spawn(_playerPrefab, pos, Quaternion.identity, player);
        runner.SetPlayerObject(player, avatar); // map PlayerRef -> avatar to avoid duplicates
        Debug.Log($"[Spawner] Spawned player {player} at spawn {idx}");
    }
    public override void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new PlayerInputData();

        // Replace this with Unity InputSystem / keyboard checks
        if (Keyboard.current != null)
        {
            data.MoveX = (Keyboard.current.aKey.isPressed ? -1 :
                         Keyboard.current.dKey.isPressed ? 1 : 0);

            data.JumpPressed = Keyboard.current.spaceKey.isPressed;
            data.JumpHeld = Keyboard.current.spaceKey.isPressed;
            data.DashPressed = Keyboard.current.leftShiftKey.isPressed;
        }

        input.Set(data);
    }
}
