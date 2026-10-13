using UnityEngine;

[System.Serializable]
public class LootSpawnInfo
{
    public RoomId RoomId;

    public Vector2Int SpawnPointLeftUp;
    public Vector2Int SpawnPointRightDown;

    public int WeaponSpawnCount;
}