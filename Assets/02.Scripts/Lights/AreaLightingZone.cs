using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AreaLightingZone : MonoBehaviour
{
    [Header("조명")]
    [SerializeField] private Light2D[] _lights;

    [SerializeField] private RoomId _roomId;
    [SerializeField] private bool _isItemRoom;

    private Collider2D _zoneCollider;

    private void Awake()
    {
        _zoneCollider = GetComponent<Collider2D>();
    }

    public bool ContainsPoint(Vector2 position)
    {
        return isActiveAndEnabled && _zoneCollider != null &&
               _zoneCollider.enabled && _zoneCollider.gameObject.activeInHierarchy &&
               _zoneCollider.OverlapPoint(position);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var rb = collision.attachedRigidbody;

        if (rb == null || !rb.CompareTag("Player")) return;

        EnterRoom();
    }

    public void ActivateLighting()
    {
        foreach (var lightObject in _lights)
        {
            if (lightObject == null) continue;
            lightObject.enabled = true;
        }
    }

    public void DeactivateLighting()
    {
        foreach (var lightObject in _lights)
        {
            if (lightObject == null) continue;

            lightObject.enabled = false;
        }
    }

    private void EnterRoom()
    {
        if (!_isItemRoom) return;

        LootSpawner spawner = LootSpawner.Instance;
        if (spawner == null) return;

        spawner.SetCurrentRoom(_roomId);
    }
}
