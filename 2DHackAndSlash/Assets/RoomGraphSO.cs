using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rooms/Room Graph", fileName = "RoomGraph")]
public class RoomGraphSO : ScriptableObject
{
    [field: SerializeField] public RoomDataSO StartingRoom { get; private set; }
    [field: SerializeField] public List<RoomDataSO> AllRooms { get; private set; } = new();
}