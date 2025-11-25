using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class Room
{
    public Transform RoomSpawnPoint;
    public List<Room> ConnectedRooms;
    public RoomTypeEnum RoomType;
}
public enum RoomTypeEnum
{
    Tresure,Fight,Boss, Shop, Rest, DanielIsHereNotAIEasterEgg
}
