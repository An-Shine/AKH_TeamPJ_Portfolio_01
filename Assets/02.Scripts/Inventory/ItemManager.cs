using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private static ItemManager _instance;
    public static ItemManager Instance => _instance;

    [SerializeField] private List<ItemEntry> _lootItemEntryList;
    [SerializeField] private Transform _itemContainer;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private CommonItemPool _commonItemPool;

    private Dictionary<string, Item> _unobtainedItemDictionary;
    public IReadOnlyDictionary<string, Item> UnobtainedItemDictionary => _unobtainedItemDictionary;
    private List<Item> _gainedItemList;
    public IReadOnlyList<Item> GainedItemList => _gainedItemList;

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
        foreach (ItemEntry itemEntry in _lootItemEntryList)
        {
            id = IdPrefix.ItemPrefix + itemEntry.ItemId;
            _unobtainedItemDictionary[id] = Instantiate(itemEntry.ItemPrefab, _itemContainer);
            _unobtainedItemDictionary[id].Initialize(itemEntry.ItemId);
            _unobtainedItemDictionary[id].gameObject.SetActive(false);
        }

        _commonItemPool.CreatePool();
    }

    public void GainItem(Item item)
    {
        if (!item.IsCommon)
        {
            _gainedItemList.Add(item);
        }

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
                    ItemEffectApplier.ApplyImmediateEffect(effect, _playerTransform);
                    break;
                }
            }
        }

        if (item.GainEffectPrefab != null)
        {
            Instantiate(item.GainEffectPrefab, _playerTransform);
        }

        ItemVisualApplier.Visualize(item.Data, _playerTransform);

        AudioManager.Instance.PlaySFX(item.GainEffectSFX);
        item.gameObject.SetActive(false);
        LootSpawner.Instance.EraseOtherItemOption(item);
    }

    public void RemoveUnobtainItem(Item item)
    {
        _unobtainedItemDictionary.Remove(IdPrefix.ItemPrefix + item.Data.ItemId);
    }

    public void AddUnobtainItem(Item item)
    {
        string itemId = IdPrefix.ItemPrefix + item.Data.ItemId;
        if (_unobtainedItemDictionary.ContainsKey(itemId)) return;

        _unobtainedItemDictionary.Add(itemId, item);
    }

    public List<Item> GetRandomCommonItem(int min, int max)
    {
        int itemCount = UnityEngine.Random.Range(min, max + 1);
        List<Item> result = new List<Item>();
        Item item;
        for (int i = 0; i < itemCount; i++)
        {
            item = _commonItemPool.GetRandomCommonItem();
            if (item != null)
            {
                result.Add(item);
            }
        }

        return result;
    }
}