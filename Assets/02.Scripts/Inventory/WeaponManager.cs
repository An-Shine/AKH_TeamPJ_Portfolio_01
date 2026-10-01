using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    private static WeaponManager _instance;
    public static WeaponManager Instance => _instance;

    [SerializeField] private List<WeaponEntry> _weaponEntryList;
    [SerializeField] private Transform _weaponContainer;

    private Dictionary<string, Weapon> _unobtainedWeapons;
    public IReadOnlyDictionary<string, Weapon> UnobtainedWeapons => _unobtainedWeapons;
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
        string id = string.Empty;
        foreach (var weaponSlot in _weaponEntryList)
        {
            id = IdPrefix.WeaponPrefix + weaponSlot.WeaponId;
            _unobtainedWeapons[id] = Instantiate(weaponSlot.WeaponPrefab, _weaponContainer);
            if (DataManager.Instance.WeaponDatas.TryGetValue(id, out WeaponData weaponData))
            {
                _unobtainedWeapons[id].Initialize(weaponData);
            }
            else
            {
                Destroy(_unobtainedWeapons[id]);
                _unobtainedWeapons.Remove(id);
                continue;
            }

            _unobtainedWeapons[id].gameObject.SetActive(false);
        }

        _gainedWeaponList.Add(_unobtainedWeapons[id]);
        EquipByIndex(0);
        CurrentWeapon._firePoint = PlayerManager.Instance.Player._weapon.Muzzle;
        CurrentWeapon._originPoint = PlayerManager.Instance.Player._weapon.Origin;
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

    public bool GainWeapon(int weaponId)
    {
        string id = IdPrefix.WeaponPrefix + weaponId;
        if (_unobtainedWeapons.TryGetValue(id, out var targetWeapon))
        {
            _gainedWeaponList.Add(targetWeapon);
            _unobtainedWeapons.Remove(id);
            return true;
        }

        Debug.LogWarning($"{id} is not found");
        return false;
    }
}