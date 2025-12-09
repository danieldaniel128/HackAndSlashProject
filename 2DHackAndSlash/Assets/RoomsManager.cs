using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomsManager : MonoBehaviour
{
    [SerializeField] private RoomGraphSO _graph;
    [SerializeField] private GameObject _player;

    private RoomDataSO _currentRoom;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (_player != null)
            DontDestroyOnLoad(_player);
    }

    private void Start()
    {
        if (_graph != null && _graph.StartingRoom != null)
        {
            LoadRoomAsync(_graph.StartingRoom);
        }
    }

    public void LoadRoomAsync(RoomDataSO room, string entryId = "Default")
    {
        if (room == null) return;
        _currentRoom = room;
        StartCoroutine(LoadRoomRoutine(room.SceneName, entryId));
    }

    private IEnumerator LoadRoomRoutine(string sceneName, string entryId)
    {
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        while (!op.isDone)
            yield return null;

        // find spawn point in new scene
        var spawns = FindObjectsOfType<RoomSpawnPoint>();
        var target = spawns.FirstOrDefault(s => s.EntryId == entryId) ?? spawns.FirstOrDefault();

        if (target != null && _player != null)
        {
            _player.transform.position = target.transform.position;

            if (_player.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    public void OnPlayerReachedExit(RoomDataSO nextRoom, string exitId = "Default")
    {
        if (nextRoom == null) return;
        LoadRoomAsync(nextRoom, exitId);
    }
}
