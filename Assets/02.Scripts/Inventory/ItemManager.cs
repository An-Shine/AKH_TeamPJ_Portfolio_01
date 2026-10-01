using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private static ItemManager _instance;
    public static ItemManager Instance => _instance;

    [SerializeField] private List<ItemEntry> _itemEntryList;
    [SerializeField] private Transform _itemContainer;
    [SerializeField] private Transform _playerTransform;

    private Dictionary<string, Item> _unobtainedItemDictionary;
    private List<Item> _gainedItemList;
    public List<Item> GainedItemList => _gainedItemList;

    private ItemPlayerStat _playerStat = new ItemPlayerStat();
    public ItemPlayerStat PlayerStat => _playerStat;
    private ItemWeaponStat _weaponStat = new ItemWeaponStat();
    public ItemWeaponStat WeaponStat => _weaponStat;
    private ItemProjectileStat _projectileStat = new ItemProjectileStat();
    public ItemProjectileStat ProjectileStat => _projectileStat;

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
        _gainedItemList = new List<Item>();
        _unobtainedItemDictionary = new Dictionary<string, Item>();
        string id = string.Empty;
        foreach (ItemEntry itemEntry in _itemEntryList)
        {
            id = IdPrefix.ItemPrefix + itemEntry.ItemId;
            _unobtainedItemDictionary[id] = Instantiate(itemEntry.ItemPrefab, _itemContainer);
            _unobtainedItemDictionary[id].Initialize(itemEntry.ItemId);
            _unobtainedItemDictionary[id].gameObject.SetActive(false);
        }
    }

    public void GainItem(Item item)
    {
        _gainedItemList.Add(item);
        _unobtainedItemDictionary.Remove(IdPrefix.ItemPrefix + item.Data.ItemId);

        foreach (ItemEffectData effect in item.Effects)
        {
            switch (effect.ApplyType)
            {
                case ItemEffectApplyType.Stat:
                {
                    ItemEffectApplier.ApplyStatEffect(PlayerStat, WeaponStat, ProjectileStat, effect);
                    break;
                }
                case ItemEffectApplyType.Immediate:
                {
                    ItemEffectApplier.ApplyImmediateEffect(effect);
                    break;
                }
            }
        }

        Instantiate(item.GainEffectPrefab, _playerTransform);
        AudioManager.Instance.PlaySFX(item.GainEffectSFX);
        item.gameObject.SetActive(false);
    }
}