using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public ItemData Data { get; private set; }

    private List<ItemEffectData> _effects;
    public List<ItemEffectData> Effects => _effects;

    public void Initialize(string itemId)
    {
        Data = DataManager.Instance.ItemDatas[itemId];

        _effects = new List<ItemEffectData>();
        foreach (string effectId in Data.EffectIdList)
        {
            if (effectId.Length <= 0)
            {
                continue;
            }
            _effects.Add(DataManager.Instance.ItemEffectDatas[effectId]);
        }
    }
}