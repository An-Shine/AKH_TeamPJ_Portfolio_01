using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LootSpawner : MonoBehaviour
{
    private static LootSpawner _instance;
    public static LootSpawner Instance => _instance;

    [SerializeField] private List<WeaponPickup> _weaponPickups;
    private Dictionary<string, WeaponPickup> _weaponPickUpDictionary;

    [SerializeField] private List<LootSpawnInfo> _lootSpawnInfos;
    private Dictionary<RoomId, GridPos[]> _lootSpawnInfoDictionary;

    [SerializeField] private Vector2 _unitSize;
    private Dictionary<RoomId, List<GridPos>> _gridInfoDictionary;

    [Header("Spawn Count")]
    [SerializeField] private int _weaponSpawnCount;
    [SerializeField] private int _itemOptionCount;

    [Header("Evade Layer")]
    [SerializeField] private LayerMask _nonTargetLayerMask;

    private List<RoomId> _spawnDoneRoomIds;
    private Dictionary<RoomId, List<Item>> _spawnedItemDictionary;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        _weaponPickUpDictionary = new Dictionary<string, WeaponPickup>();
        foreach (WeaponPickup weaponPickup in _weaponPickups)
        {
            _weaponPickUpDictionary[IdPrefix.WeaponPrefix + weaponPickup.WeaponId] = weaponPickup;
        }

        _lootSpawnInfoDictionary = new Dictionary<RoomId, GridPos[]>();
        _gridInfoDictionary = new Dictionary<RoomId, List<GridPos>>();
        foreach (LootSpawnInfo lootSpawnInfo in _lootSpawnInfos)
        {
            _lootSpawnInfoDictionary[lootSpawnInfo.RoomId] = new GridPos[2]
            {
                new GridPos((int)lootSpawnInfo.SpawnPointLeftUp.position.x,
                    (int)lootSpawnInfo.SpawnPointLeftUp.position.y),
                new GridPos((int)lootSpawnInfo.SpawnPointRightDown.position.x,
                    (int)lootSpawnInfo.SpawnPointRightDown.position.y)
            };
            _gridInfoDictionary[lootSpawnInfo.RoomId] = CreateGridInfo(lootSpawnInfo.RoomId);
        }

        _spawnDoneRoomIds = new List<RoomId>();
        _spawnedItemDictionary = new Dictionary<RoomId, List<Item>>();
        //Invoke("SpawnTest", 1f);
    }

    public void SpawnTest()
    {
        SpawnLoot(RoomId.Room1);
        SpawnLoot(RoomId.Room2);
        SpawnLoot(RoomId.Room3);
    }

    private List<GridPos> CreateGridInfo(RoomId roomId)
    {
        List<GridPos> gridInfo = new List<GridPos>();

        int startOfX = _lootSpawnInfoDictionary[roomId][0].X;
        int startOfY = _lootSpawnInfoDictionary[roomId][0].Y;
        int endOfX = _lootSpawnInfoDictionary[roomId][1].X;
        int endOfY = _lootSpawnInfoDictionary[roomId][1].Y;

        int indexOfX = startOfX;
        int indexOfY = startOfY;

        GridPos grid;
        while (indexOfY >= endOfY)
        {
            while (indexOfX <= endOfX)
            {
                grid = new GridPos(indexOfX, indexOfY);
                gridInfo.Add(grid);
                indexOfX += 1;
            }

            indexOfX = startOfX;
            indexOfY -= 1;
        }

        return gridInfo;
    }

    public void SpawnLoot(RoomId roomId)
    {
        if (_spawnDoneRoomIds.Contains(roomId)) return;

        int repeat;
        Vector3 spawnPosition;

        int failedSpawnCount = 0;
        for (repeat = 0; repeat < _weaponSpawnCount; repeat++)
        {
            spawnPosition = GetPosition(roomId);
            if (!SpawnWeapon(spawnPosition))
            {
                failedSpawnCount++;
            }
        }

        for (repeat = 0; repeat < _itemOptionCount + failedSpawnCount; repeat++)
        {
            spawnPosition = GetPosition(roomId);
            SpawnItem(spawnPosition, roomId);
        }

        _spawnDoneRoomIds.Add(roomId);
    }

    private Vector3 GetPosition(RoomId roomId)
    {
        int targetIndex;
        int count;
        Collider2D hit;

        List<GridPos> currentGridInfoList = _gridInfoDictionary[roomId]; //.ToList(); //추후 사용할 일 없으니 굳이 복사X
        Vector3 targetPosition = new Vector3(0, 0, 0);
        int repeat = currentGridInfoList.Count;

        for (int index = 0; index < repeat; index++)
        {
            count = currentGridInfoList.Count;
            targetIndex = UnityEngine.Random.Range(0, count);
            targetPosition.x = currentGridInfoList[targetIndex].X;
            targetPosition.y = currentGridInfoList[targetIndex].Y;

            currentGridInfoList.RemoveAt(targetIndex);

            hit = Physics2D.OverlapBox(
                targetPosition,
                _unitSize,
                0,
                _nonTargetLayerMask);

            if (hit == null)
            {
                break;
            }
        }

        return targetPosition;
    }

    private bool SpawnWeapon(Vector3 spawnPosition)
    {
        int count = _weaponPickUpDictionary.Count; //WeaponManager.Instance.UnobtainedWeapons.Count;
        if (count <= 0) return false;

        int index = UnityEngine.Random.Range(0, count);
        string
            weaponId = _weaponPickUpDictionary.Keys
                .ElementAt(index); //WeaponManager.Instance.UnobtainedWeapons.Keys.ElementAt(index);
        if (_weaponPickUpDictionary.TryGetValue(weaponId, out WeaponPickup targetPickUp))
        {
            WeaponPickup weaponPickup = Instantiate(targetPickUp, spawnPosition, Quaternion.identity);
            _weaponPickUpDictionary.Remove(weaponId);
            return true;
        }

        return false;
    }

    private bool SpawnItem(Vector3 spawnPosition, RoomId roomId)
    {
        int count = ItemManager.Instance.UnobtainedItemDictionary.Count;
        if (count <= 0) return false;

        int index = UnityEngine.Random.Range(0, count);
        string itemId = ItemManager.Instance.UnobtainedItemDictionary.Keys.ElementAt(index);
        if (ItemManager.Instance.UnobtainedItemDictionary.TryGetValue(itemId, out Item targetItem))
        {
            targetItem.transform.position = spawnPosition;
            targetItem.gameObject.SetActive(true);
            ItemManager.Instance.RemoveUnobtainItem(targetItem);

            if (!_spawnedItemDictionary.ContainsKey(roomId))
            {
                _spawnedItemDictionary.Add(roomId, new List<Item>());
            }

            _spawnedItemDictionary[roomId].Add(targetItem);

            return true;
        }

        return false;
    }

    public void EraseOtherItemOption(Item targetItem)
    {
        bool findFlag = false;
        RoomId targetRoomId = RoomId.Room1;
        foreach (RoomId id in _spawnedItemDictionary.Keys)
        {
            if (_spawnedItemDictionary[id].Contains(targetItem))
            {
                targetRoomId = id;
                findFlag = true;
                break;
            }
        }

        if (!findFlag) return;

        foreach (Item item in _spawnedItemDictionary[targetRoomId])
        {
            if (targetItem.Data.ItemId == item.Data.ItemId)
            {
                continue;
            }

            ItemManager.Instance.AddUnobtainItem(item);
            item.gameObject.SetActive(false);
        }
    }
}