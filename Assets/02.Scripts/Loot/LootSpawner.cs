using System;
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
    private Dictionary<RoomId, LootSpawnInfo> _lootSpawnInfoDictionary;

    [SerializeField] private Vector2 _unitSize;
    private Dictionary<RoomId, List<GridPos>> _gridInfoDictionary;

    [Header("Item Display Stand")]
    [SerializeField] private GameObject _itemStandPrefab;
    [SerializeField] private Vector2 _itemStandSize;

    [Header("Weapon Display Stand")]
    [SerializeField] private GameObject _weaponStandPrefab;
    [SerializeField] private Vector2 _weaponStandSize;

    [Header("Position Modifier")]
    [SerializeField] private Vector3 _positionModifier;

    [Header("Evade Layer")]
    [SerializeField] private LayerMask _nonTargetLayerMask;

    private List<RoomId> _spawnDoneRoomIds;
    private Dictionary<RoomId, List<Item>> _spawnedItemDictionary;

    private RoomId _currentRoomId = RoomId.None;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Update()
    {
        if (_currentRoomId != RoomId.None && _lootSpawnInfoDictionary[_currentRoomId].DoNotNeedClear)
        {
            SpawnLoot();
        }
    }

    private void Start()
    {
        _weaponPickUpDictionary = new Dictionary<string, WeaponPickup>();
        foreach (WeaponPickup weaponPickup in _weaponPickups)
        {
            _weaponPickUpDictionary[IdPrefix.WeaponPrefix + weaponPickup.WeaponId] = weaponPickup;
        }

        _lootSpawnInfoDictionary = new Dictionary<RoomId, LootSpawnInfo>();
        _gridInfoDictionary = new Dictionary<RoomId, List<GridPos>>();
        foreach (LootSpawnInfo lootSpawnInfo in _lootSpawnInfos)
        {
            _lootSpawnInfoDictionary[lootSpawnInfo.RoomId] = lootSpawnInfo;
            _gridInfoDictionary[lootSpawnInfo.RoomId] = GridPos.CreateGridInfo(lootSpawnInfo);
        }

        _spawnDoneRoomIds = new List<RoomId>();
        _spawnedItemDictionary = new Dictionary<RoomId, List<Item>>();
    }

    public void SpawnLoot()
    {
        if (_currentRoomId == RoomId.None) return;
        if (_spawnDoneRoomIds.Contains(_currentRoomId)) return;

        int repeat;
        Vector3 spawnPosition;

        //item display stand
        spawnPosition = GetPosition(_currentRoomId, _itemStandSize);
        Instantiate(_itemStandPrefab, spawnPosition, Quaternion.identity);

        //item
        spawnPosition.x -= _itemStandSize.x / 3;
        for (repeat = 0; repeat < 3; repeat++)
        {
            SpawnItem(spawnPosition + _positionModifier, _currentRoomId);
            spawnPosition.x += _itemStandSize.x / 3;
        }

        //weapon
        for (repeat = 0; repeat < _lootSpawnInfoDictionary[_currentRoomId].WeaponSpawnCount; repeat++)
        {
            spawnPosition = GetPosition(_currentRoomId, _weaponStandSize);
            if (_weaponStandPrefab != null && SpawnWeapon(spawnPosition + _positionModifier))
            {
                Instantiate(_weaponStandPrefab, spawnPosition, Quaternion.identity);
            }
        }

        _spawnDoneRoomIds.Add(_currentRoomId);
    }

    private Vector3 GetPosition(RoomId roomId, Vector2 checksize)
    {
        int targetIndex;
        int count;
        Collider2D hit;

        List<GridPos> currentGridInfoList = _gridInfoDictionary[roomId].ToList();
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
                checksize,
                0,
                _nonTargetLayerMask);

            if (hit == null)
            {
                break;
            }

            Debug.LogWarning($"{targetPosition.x}, {targetPosition.y} : " + hit.gameObject.name);
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

    private void SpawnItem(Vector3 spawnPosition, RoomId roomId)
    {
        int count = ItemManager.Instance.UnobtainedItemDictionary.Count;
        if (count <= 0) return;

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
        }
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

    public void SetCurrentRoom(RoomId roomId)
    {
        _currentRoomId = roomId;
    }
}