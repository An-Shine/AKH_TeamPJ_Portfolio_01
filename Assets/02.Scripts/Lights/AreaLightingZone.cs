using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AreaLightingZone : MonoBehaviour
{
    [Header("조명")]
    [SerializeField] private Light2D[] _lights;

    [Header("매니저")]
    [SerializeField] private AreaLightManager _areaLightManager;

    [SerializeField] private RoomId _roomId;
    [SerializeField] private bool _isItemRoom;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var rb = collision.attachedRigidbody;

        if (rb == null || !rb.CompareTag("Player")) return;

        _areaLightManager.EnterZone(this);
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