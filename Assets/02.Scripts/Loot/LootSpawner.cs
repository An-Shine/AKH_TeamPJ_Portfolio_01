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

    [SerializeField] private GameObject _spawnEffectPrefab;

    [Header("Item Display Stand")]
    [SerializeField] private DisplayStand _itemStandPrefab;
    [SerializeField] private Vector2 _itemStandSize;
    [SerializeField] private Vector2 _checkItemStandSize;

    [Header("Weapon Display Stand")]
    [SerializeField] private DisplayStand _weaponStandPrefab;
    [SerializeField] private Vector2 _weaponStandSize;

    [Header("Position Modifier")]
    [SerializeField] private Vector3 _positionModifier;

    [Header("Evade Layer")]
    [SerializeField] private LayerMask _nonTargetLayerMask;

    [Header("Common Item Min Max")]
    [SerializeField] private Vector2Int _commonItemMinMax;

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
        if (_lootSpawnInfoDictionary[_currentRoomId].IsOnlyCommon) return;

        int repeat;
        Vector3 spawnPosition;

        //item display stand
        spawnPosition =
            GridPos.GetPosition(_gridInfoDictionary[_currentRoomId], _checkItemStandSize, _nonTargetLayerMask);
        DisplayStand itemStand = Instantiate(_itemStandPrefab, spawnPosition, Quaternion.identity);
        itemStand.StartAppearance();

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
            spawnPosition =
                GridPos.GetPosition(_gridInfoDictionary[_currentRoomId], _weaponStandSize, _nonTargetLayerMask);
            if (_weaponStandPrefab != null && SpawnWeapon(spawnPosition + _positionModifier))
            {
                DisplayStand weaponStand = Instantiate(_weaponStandPrefab, spawnPosition, Quaternion.identity);
                weaponStand.StartAppearance();
            }
        }

        _spawnDoneRoomIds.Add(_currentRoomId);
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
            weaponPickup.StartAppearance();
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
            targetItem.StartAppearance();
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
        if (_currentRoomId == RoomId.None) return;
        if (_lootSpawnInfoDictionary[_currentRoomId].IsOnlyCommon) return;

        bool findFlag = false;
        RoomId targetRoomId = RoomId.None;
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

    public void SpawnCommonItem()
    {
        if (_currentRoomId == RoomId.None) return;
        if (_spawnDoneRoomIds.Contains(_currentRoomId)) return;

        ItemManager manager = ItemManager.Instance;
        if (manager == null) return;

        Vector3 spawnPosition;
        List<Item> spawnList = manager.GetRandomCommonItem(_commonItemMinMax[0], _commonItemMinMax[1]);
        foreach (Item targetItem in spawnList)
        {
            spawnPosition = GridPos.GetPosition(_gridInfoDictionary[_currentRoomId], _unitSize, _nonTargetLayerMask);
            targetItem.transform.position = spawnPosition;
            targetItem.gameObject.SetActive(true);
            targetItem.StartAppearance();
        }

        _spawnDoneRoomIds.Add(_currentRoomId);
    }
}