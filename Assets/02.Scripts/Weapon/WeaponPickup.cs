using System.Collections;
using UnityEngine;

[RequireComponent(typeof(OverlapSphere))]
public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private int _weaponId;
    public int WeaponId => _weaponId;

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

    private void Update()
    {
        DisplayTooltip();
    }

    private void OnDisable()
    {
        if (ToolTipManager.Instance != null)
        {
            ToolTipManager.Instance.CloseItemTooltip(transform);
        }

        DeactivateOutline();
    }

    public Sprite Interact(GameObject actor)
    {
        WeaponManager.Instance.GainWeapon(_weaponId);
        gameObject.SetActive(false);
        return gameObject.GetComponent<SpriteRenderer>().sprite;
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

        string description = BuildDescription();
        manager.OpenItemTooltip(transform, _tooltipOffset, description);
        ActivateOutline();
    }

    private string BuildDescription()
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager == null) return string.Empty;

        string description = "";
        description += dataManager.WeaponDatas[IdPrefix.WeaponPrefix + _weaponId].Name + " / ";
        description += dataManager.WeaponDatas[IdPrefix.WeaponPrefix + _weaponId].Description;

        return description;
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
}