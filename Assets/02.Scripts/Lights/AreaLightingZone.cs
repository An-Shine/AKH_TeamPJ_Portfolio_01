using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AreaLightingZone : MonoBehaviour
{
    [Header("조명")]
    [SerializeField] private Light2D[] _lights;

    [Header("매니저")]
    [SerializeField] private AreaLightManager _areaLightManager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var rb = collision.attachedRigidbody;

        if (rb == null || !rb.CompareTag("Player")) return;

        _areaLightManager.EnterZone(this);
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
}