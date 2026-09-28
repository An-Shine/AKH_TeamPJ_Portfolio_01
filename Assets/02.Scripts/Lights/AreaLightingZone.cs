using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AreaLightingZone : MonoBehaviour
{
    [Header("조명")]
    [SerializeField] private Light2D[] lights;
    
    [Header("매니저")]
    [SerializeField] private AreaLightManager areaLightManager;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Rigidbody2D rb = collision.attachedRigidbody;

        if (rb == null || !rb.CompareTag("Player"))
        {
            return;
        }

        areaLightManager.EnterZone(this);
    }

    public void ActivateLighting()
    {
        foreach (Light2D lightObject in lights)
        {
            if (lightObject == null)
            {
                continue;
            }
            lightObject.enabled = true;
        }
    }

    public void DeactivateLighting()
    {
        foreach (Light2D lightObject in lights)
        {
            if (lightObject == null)
            {
                continue;
            }
            
            lightObject.enabled = false;
        }
    }
}
