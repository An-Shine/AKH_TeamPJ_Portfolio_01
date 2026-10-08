using UnityEngine;

public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private int _weaponId;
    public int WeaponId => _weaponId;

    public Sprite Interact(GameObject actor)
    {
        WeaponManager.Instance.GainWeapon(_weaponId);
        gameObject.SetActive(false);
        // Destroy(gameObject);
        return gameObject.GetComponent<SpriteRenderer>().sprite;
    }
}