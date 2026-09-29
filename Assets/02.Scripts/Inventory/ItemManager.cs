using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private static ItemManager _instance;
    public static ItemManager Instance => _instance;

    [SerializeField] private List<ItemEntry> _itemEntryList;
    [SerializeField] private Transform _itemContainer;

    private Dictionary<string, Item> _unobtainedItemDictionary;
    private List<Item> _gainedItemList;

    public ItemPlayerStat PlayerStat { get; private set; }
    public ItemWeaponStat WeaponStat { get; private set; }
    public ItemProjectileStat ProjectileStat { get; private set; }

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
        PlayerStat = new ItemPlayerStat();
        WeaponStat = new ItemWeaponStat();
        ProjectileStat = new ItemProjectileStat();

        _unobtainedItemDictionary = new Dictionary<string, Item>();
        foreach (ItemEntry itemEntry in _itemEntryList)
        {
            _unobtainedItemDictionary[itemEntry.ItemId] = Instantiate(itemEntry.ItemPrefab, _itemContainer);
            _unobtainedItemDictionary[itemEntry.ItemId].Initialize(itemEntry.ItemId);
            _unobtainedItemDictionary[itemEntry.ItemId].gameObject.SetActive(false);
        }
    }

    public void GainItem(Item item)
    {
        _gainedItemList.Add(item);
        _unobtainedItemDictionary.Remove(item.Data.ItemId);

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
    }
}