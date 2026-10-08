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

    [SerializeField] private Vector3 _tooltipOffset;
    private OverlapSphere _overlapSphere;
    private bool _tooltipFlag;

    [SerializeField] private Material _shaderMaterial;
    private Material _runtimeMaterial;

    private const string OutlineShaderKey = "_OuterOutlineFade";

    private void Awake()
    {
        _overlapSphere = GetComponent<OverlapSphere>();
        _runtimeMaterial = Instantiate(_shaderMaterial);
        gameObject.GetComponent<SpriteRenderer>().material = _runtimeMaterial;
        DeactivateOutline();
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
        ToolTipManager manager = ToolTipManager.Instance;
        if (manager == null) return;

        if (!_overlapSphere.ExistTarget())
        {
            if (_tooltipFlag)
            {
                _tooltipFlag = false;
                DeactivateOutline();
                manager.CloseItemTooltip();
            }

            return;
        }

        if (_tooltipFlag) return;

        ActivateOutline();
        manager.OpenItemTooltip(transform, _tooltipOffset, Data.Description);
        _tooltipFlag = true;
    }

    private void ActivateOutline()
    {
        _runtimeMaterial.SetFloat(OutlineShaderKey, 1f);
    }

    private void DeactivateOutline()
    {
        _runtimeMaterial.SetFloat(OutlineShaderKey, 0f);
    }

    public Sprite Interact(GameObject actor)
    {
        ItemManager.Instance.GainItem(this);
        return gameObject.GetComponent<SpriteRenderer>().sprite;
    }
}