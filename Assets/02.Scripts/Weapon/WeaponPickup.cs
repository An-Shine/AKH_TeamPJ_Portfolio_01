using System;
using UnityEngine;

[RequireComponent(typeof(OverlapSphere))]
public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private int _weaponId;
    public int WeaponId => _weaponId;

    [SerializeField] private Vector3 _tooltipOffset;
    private OverlapSphere _overlapSphere;
    private bool _tooltipFlag;

    private void Awake()
    {
        _overlapSphere = GetComponent<OverlapSphere>();
    }

    private void Start()
    {
        _tooltipFlag = false;
    }

    private void Update()
    {
        DisplayTooltip();
    }

    private void OnDisable()
    {
        if (_tooltipFlag)
        {
            ToolTipManager.Instance.CloseItemTooltip();
        }
    }

    public Sprite Interact(GameObject actor)
    {
        WeaponManager.Instance.GainWeapon(_weaponId);
        gameObject.SetActive(false);
        // Destroy(gameObject);
        return gameObject.GetComponent<SpriteRenderer>().sprite;
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
                manager.CloseItemTooltip();
            }

            return;
        }

        if (_tooltipFlag) return;

        string description = BuildDescription();
        manager.OpenItemTooltip(transform, _tooltipOffset, description);
        _tooltipFlag = true;
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
}