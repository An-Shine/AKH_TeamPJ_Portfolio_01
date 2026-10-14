using System;
using System.Collections.Generic;
using UnityEngine;

public class CommonItemPool : MonoBehaviour
{
    [SerializeField] private List<ItemEntry> _commonItemEntryList;
    [SerializeField] private Transform _commonItemContainer;
    [SerializeField] private int _commonItemPoolSize;

    private Dictionary<string, List<Item>> _commonItemDictionary;
    private List<string> _keyList;

    public void CreatePool()
    {
        Item itemObj;
        string id;
        int currentSize = 0;
        _commonItemDictionary = new Dictionary<string, List<Item>>();
        foreach (ItemEntry commonEntry in _commonItemEntryList)
        {
            id = IdPrefix.ItemPrefix + commonEntry.ItemId;
            _commonItemDictionary[id] = new List<Item>();
            for (currentSize = 0; currentSize < _commonItemPoolSize; currentSize++)
            {
                itemObj = Instantiate(commonEntry.ItemPrefab, _commonItemContainer);
                itemObj.gameObject.SetActive(false);
                itemObj.Initialize(commonEntry.ItemId);
                _commonItemDictionary[id].Add(itemObj);
            }
        }

        _keyList = new List<string>(_commonItemDictionary.Keys);
    }

    public Item GetRandomCommonItem()
    {
        int randomIndex = UnityEngine.Random.Range(0, _keyList.Count);
        string randomKey = _keyList[randomIndex];
        foreach (Item item in _commonItemDictionary[randomKey])
        {
            if (!item.gameObject.activeSelf)
            {
                return item;
            }
        }

        return null;
    }
}