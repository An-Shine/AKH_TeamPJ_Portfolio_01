using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(OverlapSphere))]
public class Item : MonoBehaviour, IInteractable
{
    [SerializeField] private bool _isCommon = false;
    public bool IsCommon => _isCommon;

    [SerializeField] private GameObject _gainEffectPrefab;
    public GameObject GainEffectPrefab => _gainEffectPrefab;
    [SerializeField] private SFXType _gainEffectSFX;
    public SFXType GainEffectSFX => _gainEffectSFX;

    public ItemData Data { get; private set; }

    private List<ItemEffectData> _effects;
    public List<ItemEffectData> Effects => _effects;

    [SerializeField] private Vector3 _tooltipOffset;
    private OverlapSphere _overlapSphere;

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

    private void OnDisable()
    {
        if (ToolTipManager.Instance != null)
        {
            ToolTipManager.Instance.CloseItemTooltip(transform);
        }

        DeactivateOutline();
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
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
        if (Data == null)
        {
            Debug.LogWarning($"Item data not found: {id}", this);
            if (ToolTipManager.Instance != null)
                ToolTipManager.Instance.CloseItemTooltip(transform);
            return;
        }

        foreach (string effectId in Data.EffectIdList)
        {
            if (string.IsNullOrEmpty(effectId))
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
            manager.CloseItemTooltip(transform);
            DeactivateOutline();
            return;
        }

        manager.OpenItemTooltip(transform, _tooltipOffset, Data.Description);
        ActivateOutline();
    }

    private void ActivateOutline()
    {
        _runtimeMaterial.SetFloat(OutlineShaderKey, 1f);
    }

    private void DeactivateOutline()
    {
        _runtimeMaterial.SetFloat(OutlineShaderKey, 0f);
    }

    public void StartAppearance()
    {
        StartCoroutine(Appearance());
    }

    private IEnumerator Appearance()
    {
        float fadeValue = 0f;
        float t = 0f;
        float duration = 3f;
        _runtimeMaterial.SetFloat("_FullGlowDissolveFade", fadeValue);

        while (t < 3f)
        {
            t += Time.deltaTime;
            fadeValue = Mathf.SmoothStep(0f, 6f, t / duration);
            _runtimeMaterial.SetFloat("_FullGlowDissolveFade", fadeValue);

            yield return null;
        }
    }

    public Sprite Interact(GameObject actor)
    {
        ItemManager.Instance.GainItem(this);
        return gameObject.GetComponent<SpriteRenderer>().sprite;
    }
}