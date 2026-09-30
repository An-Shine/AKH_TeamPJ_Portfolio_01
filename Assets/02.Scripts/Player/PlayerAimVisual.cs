using UnityEngine;
using UnityEngine.UI;

// 일반적인 카메라 추적/무기 위치 갱신 뒤에 선을 그립니다.
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(PlayerAim), typeof(PlayerWeapon))]
public class PlayerAimVisual : MonoBehaviour
{
    [SerializeField] private Image _crosshairImage;
    [SerializeField] private LineRenderer _aimLine;

    private PlayerAim _aim;
    private PlayerWeapon _weapon;
    private readonly Vector3[] _linePoints = new Vector3[2];
    private readonly Plane[] _frustumPlanes = new Plane[6];
    private bool _previousCrosshairVisible;
    private bool _missingMuzzleWarned;

    private void Awake()
    {
        _aim = GetComponent<PlayerAim>();
        _weapon = GetComponent<PlayerWeapon>();

        if (_crosshairImage == null)
        {
            Debug.LogWarning("PlayerAimVisual에 Crosshair Image를 연결하세요.", this);
        }
        else
        {
            _crosshairImage.raycastTarget = false;
        }

        if (_aimLine == null)
        {
            Debug.LogError("PlayerAimVisual에 Aim Line을 연결하세요.", this);
            return;
        }

        _aimLine.useWorldSpace = true;
        _aimLine.positionCount = 2;
        _aimLine.loop = false;
        _aimLine.alignment = LineAlignment.View;
        _aimLine.enabled = false;
    }

    private void OnEnable()
    {
        if (_crosshairImage != null)
        {
            _previousCrosshairVisible = _crosshairImage.enabled;
        }

        RefreshVisual();
    }

    private void LateUpdate()
    {
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (_weapon == null || _aim == null) return;
        // bool showLine = _weapon.AimMode == AimDisplayMode.AimLine;
        bool showLine = true;
        // GameObject나 CrosshairFollowMouse를 끄지 않습니다.
        // 원래 스크립트의 위치 갱신과 기본 커서 숨김 동작을 유지합니다.
        if (_crosshairImage != null)
        {
            _crosshairImage.enabled = !showLine;
        }

        if (_aimLine == null) return;
        if (!showLine)
        {
            _aimLine.enabled = false;
            return;
        }

        Transform muzzle = _weapon.Muzzle;
        if (muzzle == null)
        {
            _aimLine.enabled = false;
            if (!_missingMuzzleWarned)
            {
                Debug.LogWarning("조준선 모드에는 PlayerWeapon의 Muzzle이 필요합니다.", this);
                _missingMuzzleWarned = true;
            }

            return;
        }

        _missingMuzzleWarned = false;

        if (!_aim.TryGetMouseWorldPosition(out Vector3 target))
        {
            _aimLine.enabled = false;
            return;
        }

        Vector3 start = muzzle.position;
        target.z = start.z; // 2D 선은 총구와 같은 깊이에 그립니다.
        if (!TryGetScreenEdge(start, target - start, out Vector3 end))
        {
            _aimLine.enabled = false;
            return;
        }

        _linePoints[0] = start;
        _linePoints[1] = end;
        _aimLine.SetPositions(_linePoints);
        _aimLine.enabled = true;
    }

    private bool TryGetScreenEdge(Vector3 start, Vector3 direction, out Vector3 end)
    {
        end = start;
        Camera camera = _aim.WorldCamera;
        if (camera == null || direction.sqrMagnitude <= 0.000001f) return false;

        direction.Normalize();
        GeometryUtility.CalculateFrustumPlanes(camera, _frustumPlanes);
        float enterDistance = 0f;
        float exitDistance = float.PositiveInfinity;

        // 카메라 표시 영역과 총구에서 앞으로 뻗는 반직선의 교차 구간입니다.
        // 법선이 안쪽을 향하므로 GetDistanceToPoint >= 0인 쪽이 화면 내부입니다.
        foreach (Plane plane in _frustumPlanes)
        {
            float distance = plane.GetDistanceToPoint(start);
            float slope = Vector3.Dot(plane.normal, direction);
            if (Mathf.Abs(slope) < 0.000001f)
            {
                if (distance < 0f) return false;
                continue;
            }

            float hitDistance = -distance / slope;
            if (slope > 0f)
            {
                enterDistance = Mathf.Max(enterDistance, hitDistance);
            }
            else
            {
                exitDistance = Mathf.Min(exitDistance, hitDistance);
            }

            if (exitDistance < enterDistance) return false;
        }

        if (float.IsInfinity(exitDistance) || exitDistance <= 0.00001f) return false;
        end = start + direction * exitDistance;
        return true;
    }

    private void OnDisable()
    {
        if (_aimLine != null)
        {
            _aimLine.enabled = false;
        }

        if (_crosshairImage != null)
        {
            _crosshairImage.enabled = _previousCrosshairVisible;
        }
    }
}