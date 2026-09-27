using UnityEngine;

public class PlayerAim : MonoBehaviour
{
    [SerializeField] private Camera _worldCamera;

    [Tooltip("비워 두면 플레이어 Transform을 조준 기준점으로 사용")] [SerializeField]
    private Transform _aimOrigin;

    [SerializeField, Range(0f, 15f)] private float _directionHysteresis = 5f;
    [SerializeField, Min(0f)] private float _mouseDeadZone = 0.05f;
    private bool _hasFacing;

    public Vector2 AimDirection { get; private set; } = Vector2.down;
    public Camera WorldCamera => _worldCamera;
    public FacingDirection Facing { get; private set; } = FacingDirection.Down;

    private void Awake()
    {
        if (_aimOrigin == null)
        {
            _aimOrigin = transform;
        }

        if (_worldCamera == null)
        {
            _worldCamera = Camera.main;
        }

        if (_worldCamera == null)
        {
            Debug.LogError("PlayerAim의 World Camera에 게임 카메라를 연결하세요.", this);
        }
    }

    public void Refresh()
    {
        if (!TryGetMouseWorldPosition(out Vector3 mouseWorldPosition)) return;
        Vector3 origin = _aimOrigin.position;

        Vector2 offset = mouseWorldPosition - origin;
        if (offset.sqrMagnitude <= _mouseDeadZone * _mouseDeadZone) return;

        AimDirection = offset.normalized;
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;

        // 현재 방향의 범위를 여유 각도만큼 더 벗어나야 방향을 변경합니다.
        if (_hasFacing && Mathf.Abs(Mathf.DeltaAngle(CenterAngle(Facing), angle))
            <= 30f + _directionHysteresis) return;

        if (angle >= 60f && angle < 120f)
        {
            Facing = FacingDirection.Up;
        }
        else if (angle >= 120f && angle < 180f)
        {
            Facing = FacingDirection.LeftUp;
        }
        else if (angle <= -120f || angle >= 180f)
        {
            Facing = FacingDirection.LeftDown;
        }
        else if (angle <= -60f)
        {
            Facing = FacingDirection.Down;
        }
        else if (angle <= 0f)
        {
            Facing = FacingDirection.RightDown;
        }
        else
        {
            Facing = FacingDirection.RightUp;
        }

        _hasFacing = true;
    }

    // 조준선도 같은 XY 게임 평면에서 마우스 좌표를 얻습니다.
    public bool TryGetMouseWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = default;
        if (_worldCamera == null || _aimOrigin == null)
        {
            return false;
        }

        Plane plane = new Plane(Vector3.forward, _aimOrigin.position);
        Ray ray = _worldCamera.ScreenPointToRay(Input.mousePosition);
        if (!plane.Raycast(ray, out float distance))
        {
            return false;
        }

        worldPosition = ray.GetPoint(distance);
        return true;
    }

    // 몸의 바라보는 방향과 별개로, 총알은 총구에서 마우스를 향합니다.
    public Vector2 GetDirectionFrom(Vector3 worldPosition)
    {
        if (!TryGetMouseWorldPosition(out Vector3 target))
        {
            return AimDirection;
        }

        Vector2 offset = target - worldPosition;
        return offset.sqrMagnitude > 0.000001f ? offset.normalized : AimDirection;
    }

    private static float CenterAngle(FacingDirection direction)
    {
        switch (direction)
        {
            case FacingDirection.Up: return 90f;
            case FacingDirection.Down: return -90f;
            case FacingDirection.RightUp: return 30f;
            case FacingDirection.RightDown: return -30f;
            case FacingDirection.LeftUp: return 150f;
            default: return -150f;
        }
    }
}