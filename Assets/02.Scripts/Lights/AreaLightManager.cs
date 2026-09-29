using UnityEngine;

public class AreaLightManager : MonoBehaviour
{
    [Header("구역 리스트")]
    [SerializeField] private AreaLightingZone[] _zones;
    [Header("초기 구역")]
    [SerializeField] private AreaLightingZone _initialZone;

    public AreaLightingZone _currentZone { get; private set; }

    private void Awake()
    {
        foreach (AreaLightingZone zone in _zones)
        {
            if (zone == null) continue;
            zone.DeactivateLighting();
        }

        _currentZone = null;

        if (_initialZone != null)
        {
            _currentZone = _initialZone;
            _currentZone.ActivateLighting();
        }
    }

    public void EnterZone(AreaLightingZone newZone)
    {
        if (newZone == null || newZone == _currentZone)
            return;

        if (_currentZone != null) _currentZone.DeactivateLighting();
        _currentZone = newZone;
        _currentZone.ActivateLighting();
    }
}