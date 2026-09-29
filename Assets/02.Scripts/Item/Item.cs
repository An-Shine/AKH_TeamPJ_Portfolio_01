using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemData Data { get; private set; }

    private List<ItemEffectData> _effects;
    public List<ItemEffectData> Effects => _effects;

    public void Initialize(string itemId)
    {
        Data = DataManager.Instance.ItemDatas.TryGetValue(itemId, out ItemData itemData) ? itemData : null;

        _effects = new List<ItemEffectData>();
        foreach (string effectId in Data.EffectIdList)
        {
            if (effectId.Length <= 0)
            {
                continue;
            }

            if (DataManager.Instance.ItemEffectDatas.TryGetValue(effectId, out ItemEffectData effectData))
            {
                _effects.Add(effectData);
            }
        }
    }
}