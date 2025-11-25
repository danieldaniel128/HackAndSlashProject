using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomsManager : MonoBehaviour
{
    [SerializeField] List<Room> rooms;
    [SerializeField] GameObject player; // Assign your player GameObject in the inspector

    private int currentRoomIndex = 0;

    void Start()
    {
        LoadRoomScene(currentRoomIndex);
    }

    public void LoadRoomScene(int roomIndex)
    {
        if (roomIndex < 0 || roomIndex >= rooms.Count) return;

        currentRoomIndex = roomIndex;
        // Load the scene asynchronously to allow post-load actions
        SceneManager.sceneLoaded += OnRoomSceneLoaded;
        SceneManager.LoadScene(rooms[roomIndex].RoomType.ToString());
    }

    private void OnRoomSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find the spawn point in the loaded scene
        Room currentRoom = rooms[currentRoomIndex];
        if (currentRoom.RoomSpawnPoint != null && player != null)
        {
            player.transform.position = currentRoom.RoomSpawnPoint.position;

            // Reset Rigidbody
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        SceneManager.sceneLoaded -= OnRoomSceneLoaded;
    }

    // Call this when the player reaches the exit
    public void OnPlayerReachedExit(Room nextRoom)
    {
        int nextRoomIndex = rooms.IndexOf(nextRoom);
        if (nextRoomIndex != -1)
        {
            LoadRoomScene(nextRoomIndex);
        }
    }
}