using System;
using UnityEngine;

[Serializable]
public class WeaponEntry
{
    [SerializeField] private string _weaponId;
    [SerializeField] private Weapon _weaponPrefab;

    public string WeaponId => _weaponId;
    public Weapon WeaponPrefab => _weaponPrefab;
}