using UnityEngine;

public class ItemRoomEntryTrigger : MonoBehaviour
{
    [SerializeField] private ItemRoomDoorController _itemRoomDoor;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        _itemRoomDoor.EnterRoom();
    }

    private bool IsPlayer(Collider2D other)
    {
        var body = other.attachedRigidbody;

        if (body != null && body.CompareTag("Player")) return true;
        return other.CompareTag("Player");
    }
}