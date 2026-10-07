using System.Collections.Generic;
using UnityEngine;

public class ItemRoomDoorProximityTrigger : MonoBehaviour
{
    [SerializeField] private ItemRoomDoorController _itemRoomDoor;

    private readonly HashSet<Collider2D> _playerColliders = new();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        _playerColliders.Add(other);
        _itemRoomDoor.RequestOpen();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_playerColliders.Remove(other)) return;

        if (_playerColliders.Count == 0) _itemRoomDoor.RequestClose();
    }

    private bool IsPlayer(Collider2D other)
    {
        var body = other.attachedRigidbody;

        if (body != null && body.CompareTag("Player")) return true;

        return other.CompareTag("Player");
    }
}