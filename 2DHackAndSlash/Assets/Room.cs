using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Rooms/Room Data", fileName = "Room_")]
public class RoomDataSO : ScriptableObject
{
    [field: SerializeField] public string Id { get; private set; }              // "Room_01", "Boss_A"
    [field: SerializeField] public string SceneName { get; private set; }       // e.g. "FightRoom_Variant1"
    [field: SerializeField] public RoomTypeEnum RoomType { get; private set; }
    [field: SerializeField] public List<RoomDataSO> ConnectedRooms { get; private set; } = new();
}

public enum RoomTypeEnum
{
    Tresure,Fight,Boss, Shop, Rest, DanielIsHereNotAIEasterEgg
}
