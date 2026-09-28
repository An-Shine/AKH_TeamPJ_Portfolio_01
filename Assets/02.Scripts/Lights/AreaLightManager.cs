using UnityEngine;

public class AreaLightManager : MonoBehaviour
{
    [Header("구역 리스트")]
    [SerializeField] private AreaLightingZone[] zones;
    [Header("초기 구역")]
    [SerializeField] private AreaLightingZone initialZone;

    public AreaLightingZone CurrentZone { get; private set; }

    private void Awake()
    {
        foreach (AreaLightingZone zone in zones)
        {
            if (zone == null)
            {
                continue;
            }
            zone.DeactivateLighting();
        }

        CurrentZone = null;

        if (initialZone != null)
        {
            CurrentZone = initialZone;
            CurrentZone.ActivateLighting();
        }
    }
    public void EnterZone(AreaLightingZone newZone)
    {
        if (newZone == null || newZone == CurrentZone)
            return;

        if (CurrentZone != null)
        {
            CurrentZone.DeactivateLighting();
        }
        CurrentZone = newZone;
        CurrentZone.ActivateLighting();
    }
}
