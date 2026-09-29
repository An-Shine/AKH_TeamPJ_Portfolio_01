using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private List<Item> _gaineItemList;

    public ItemPlayerStat PlayerStat { get; private set; }
    public ItemWeaponStat WeaponStat { get; private set; }
    public ItemProjectileStat ProjectileStat { get; private set; }

    public void GainItem(Item item)
    {
        _gaineItemList.Add(item);
        foreach (ItemEffectData effect in item.effects)
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