using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    [System.Serializable]
    public class ItemIconEntry
    {
        public string ItemName;
        public Sprite Icon;
    }

    [SerializeField] private List<ItemIconEntry> _itemIcons = new();
    [SerializeField] private Image _itemIconPrefab;

    private int _displayedItemCount;

    private Sprite FindItemIcon(string itemName)
    {
        if (_itemIcons == null) return null;

        foreach (var entry in _itemIcons)
        {
            if (entry != null && entry.ItemName == itemName)
            {
                return entry.Icon;
            }
        }
        return null;
    }

    public void SetItemData(IReadOnlyList<Item> gainedItems)
    {
        if (gainedItems == null || _itemIconPrefab == null) return;

        for (var i = _displayedItemCount; i < gainedItems.Count; i++)
        {
            var item = gainedItems[i];
            if (item == null || item.Data == null) continue;

            var itemIcon = FindItemIcon(item.Data.Name);

            if (itemIcon == null)
            {
                Debug.LogWarning($"아이템 아이콘 연결 확인하세요", this);

                continue;
            }

            var icon = Instantiate(_itemIconPrefab, transform);
            icon.sprite = itemIcon;
            icon.enabled = true;
        }

        _displayedItemCount = gainedItems.Count;
    }
}