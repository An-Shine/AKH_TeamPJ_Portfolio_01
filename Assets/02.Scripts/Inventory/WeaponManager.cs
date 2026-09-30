using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    private static WeaponManager _instance;
    public static WeaponManager Instance => _instance;

    [SerializeField] private List<WeaponEntry> _weaponEntryList;
    [SerializeField] private Transform _weaponContainer;

    private Dictionary<string, Weapon> _unobtainedWeapons;
    private List<Weapon> _gainedWeaponList;

    public Weapon CurrentWeapon { get; private set; }
    public int CurrentWeaponIndex { get; private set; }

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        _unobtainedWeapons = new Dictionary<string, Weapon>();
        _gainedWeaponList = new List<Weapon>();
        foreach (var weaponSlot in _weaponEntryList)
        {
            _unobtainedWeapons[weaponSlot.WeaponId] = Instantiate(weaponSlot.WeaponPrefab, _weaponContainer);
            if (DataManager.Instance.WeaponDatas.TryGetValue(weaponSlot.WeaponId, out WeaponData weaponData))
            {
                _unobtainedWeapons[weaponSlot.WeaponId].Initialize(weaponData);
            }
            else
            {
                Destroy(_unobtainedWeapons[weaponSlot.WeaponId]);
                _unobtainedWeapons.Remove(weaponSlot.WeaponId);
                continue;
            }

            _unobtainedWeapons[weaponSlot.WeaponId].gameObject.SetActive(false);
        }
    }

    public void EquipByIndex(int targetIndex)
    {
        if (targetIndex >= _gainedWeaponList.Count)
        {
            return;
        }

        if (targetIndex < 0)
        {
            return;
        }

        if (CurrentWeapon != null)
            CurrentWeapon.gameObject.SetActive(false);

        CurrentWeaponIndex = targetIndex;
        CurrentWeapon = _gainedWeaponList[CurrentWeaponIndex];
        CurrentWeapon.gameObject.SetActive(true);
    }

    public void NextWeapon()
    {
        var index = CurrentWeaponIndex + 1;
        if (index >= _gainedWeaponList.Count)
        {
            index = 0;
            if (index == CurrentWeaponIndex) return;
        }

        if (CurrentWeapon != null)
            CurrentWeapon.gameObject.SetActive(false);

        CurrentWeaponIndex = index;
        CurrentWeapon = _gainedWeaponList[CurrentWeaponIndex];
        CurrentWeapon.gameObject.SetActive(true);
    }

    public void PreviousWeapon()
    {
        var index = CurrentWeaponIndex - 1;
        if (index < 0)
        {
            index = _gainedWeaponList.Count - 1;
            if (index == CurrentWeaponIndex) return;
        }

        if (CurrentWeapon != null)
            CurrentWeapon.gameObject.SetActive(false);

        CurrentWeaponIndex = index;
        CurrentWeapon = _gainedWeaponList[CurrentWeaponIndex];
        CurrentWeapon.gameObject.SetActive(true);
    }

    public bool GainWeapon(string weaponId)
    {
        if (_unobtainedWeapons.TryGetValue(weaponId, out var targetWeapon))
        {
            _gainedWeaponList.Add(targetWeapon);
            _unobtainedWeapons.Remove(weaponId);
            return true;
        }

        Debug.LogWarning($"{weaponId} is not found");
        return false;
    }
}