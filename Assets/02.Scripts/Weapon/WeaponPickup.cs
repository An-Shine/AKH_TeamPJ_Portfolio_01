using UnityEngine;

public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private int _weaponId;

    public void Interact(GameObject actor)
    {
        WeaponManager.Instance.GainWeapon(_weaponId);
        Destroy(gameObject);
    }
}