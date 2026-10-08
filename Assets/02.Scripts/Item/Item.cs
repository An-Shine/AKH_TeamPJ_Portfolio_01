using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject _gainEffectPrefab;
    public GameObject GainEffectPrefab => _gainEffectPrefab;
    [SerializeField] private SFXType _gainEffectSFX;
    public SFXType GainEffectSFX => _gainEffectSFX;

    public ItemData Data { get; private set; }

    private List<ItemEffectData> _effects;
    public List<ItemEffectData> Effects => _effects;

    public void Initialize(int itemId)
    {
        string id = IdPrefix.ItemPrefix + itemId;
        Data = DataManager.Instance.ItemDatas.TryGetValue(id, out ItemData itemData) ? itemData : null;

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

    public void Interact(GameObject actor)
    {
        ItemManager.Instance.GainItem(this);
    }
}