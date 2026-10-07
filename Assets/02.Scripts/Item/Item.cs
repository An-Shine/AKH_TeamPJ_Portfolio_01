using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OverlapSphere))]
public class Item : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject _gainEffectPrefab;
    public GameObject GainEffectPrefab => _gainEffectPrefab;
    [SerializeField] private SFXType _gainEffectSFX;
    public SFXType GainEffectSFX => _gainEffectSFX;

    public ItemData Data { get; private set; }

    private List<ItemEffectData> _effects;
    public List<ItemEffectData> Effects => _effects;

    private OverlapSphere _overlapSphere;
    private bool _tooltipFlag;

    [SerializeField] private Vector3 _tooltipOffset;

    private void Awake()
    {
        _overlapSphere = GetComponent<OverlapSphere>();
    }

    private void Start()
    {
        _tooltipFlag = false;
    }

    private void OnDisable()
    {
        if (_tooltipFlag)
        {
            ToolTipManager.Instance.CloseItemTooltip();
        }
    }

    private void Update()
    {
        DisplayTooltip();
    }

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

    private void DisplayTooltip()
    {
        if (!_overlapSphere.ExistTarget())
        {
            if (_tooltipFlag)
            {
                _tooltipFlag = false;
                ToolTipManager.Instance.CloseItemTooltip();
            }

            return;
        }

        if (_tooltipFlag) return;

        ToolTipManager.Instance.OpenItemTooltip(transform, _tooltipOffset, Data.Description);
        _tooltipFlag = true;
    }

    public void Interact(GameObject actor)
    {
        ItemManager.Instance.GainItem(this);
    }
}