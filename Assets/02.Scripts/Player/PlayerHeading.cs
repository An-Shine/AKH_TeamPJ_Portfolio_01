using UnityEngine;

public class PlayerHeading : MonoBehaviour
{
    [Header("Laser")]
    [SerializeField] private float _laserStartOffset = 0f; // 총구 위치를 조금 앞으로 밀고 싶을 때 사용
    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();

        _lineRenderer.positionCount = 2;
        _lineRenderer.useWorldSpace = true;
    }

    private void Update()
    {
        DrawLaser();
    }

    private void DrawLaser()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();

        Vector3 startPos = transform.position;
        Vector3 dir = (mouseWorldPos - transform.position).normalized;

        // 레이저 시작점을 플레이어 중심보다 조금 앞에 두고 싶다면
        if (_laserStartOffset != 0f)
        {
            startPos += dir * _laserStartOffset;
        }

        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, mouseWorldPos + dir * 30);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;
        return mouseWorldPos;
    }
}