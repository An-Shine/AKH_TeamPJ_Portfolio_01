using UnityEngine;

public class AreaLightManager : MonoBehaviour
{
    [Header("구역 리스트")]
    [SerializeField] private AreaLightingZone[] _zones;
    [Header("초기 구역")]
    [SerializeField] private AreaLightingZone _initialZone;

    public AreaLightingZone _currentZone { get; private set; }

    private PlayerController _player;
    private Collider2D _playerMovementCollider;

    private void Awake()
    {
        if (_zones != null)
        {
            foreach (AreaLightingZone zone in _zones)
            {
                if (zone == null) continue;
                zone.DeactivateLighting();
            }
        }

        _currentZone = null;

        if (_initialZone != null)
        {
            _currentZone = _initialZone;
            _currentZone.ActivateLighting();
        }
    }

    private void LateUpdate()
    {
        var player = PlayerManager.Instance?.PlayerControllerObj;
        if (player == null) return;

        if (_player != player)
            SetPlayer(player);

        if (_player == null || !_player.isActiveAndEnabled || _zones == null) return;

        // 플레이어 루트의 이동 콜라이더 중심은 발밑에 위치합니다.
        // 자식의 몸통 피격 콜라이더는 구역 판정에 사용하지 않습니다.
        Vector2 footPosition = _playerMovementCollider != null && _playerMovementCollider.enabled
            ? (Vector2)_playerMovementCollider.bounds.center
            : (Vector2)_player.transform.position;

        if (_currentZone != null && _currentZone.ContainsPoint(footPosition)) return;

        foreach (var zone in _zones)
        {
            if (zone == null || !zone.ContainsPoint(footPosition)) continue;
            EnterZone(zone);
            return;
        }

        // 구역 사이의 작은 빈틈에서는 마지막 조명을 유지합니다.
    }

    private void SetPlayer(PlayerController player)
    {
        _player = player;
        _playerMovementCollider = player != null ? player.GetComponent<Collider2D>() : null;
    }

    private void EnterZone(AreaLightingZone newZone)
    {
        if (newZone == null || newZone == _currentZone)
            return;

        if (_currentZone != null) _currentZone.DeactivateLighting();
        _currentZone = newZone;
        _currentZone.ActivateLighting();
    }
}
