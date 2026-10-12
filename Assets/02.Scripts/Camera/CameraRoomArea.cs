using UnityEngine;

public class CameraRoomArea : MonoBehaviour
{
    [Header("카메라 제한 방향")]
    [SerializeField] private bool _limitLeft = true;
    [SerializeField] private bool _limitRight = true;
    [SerializeField] private bool _limitBottom = true;
    [SerializeField] private bool _limitTop = true;

    [SerializeField] private Collider2D _playerArea;
    [SerializeField] private Transform _viewBottomLeft;
    [SerializeField] private Transform _viewTopRight;

    [SerializeField] private bool _isSmallRoom;

    public bool ContainsPlayer(Vector2 playerPosition)
    {
        if (_playerArea == null) return false;

        return _playerArea.OverlapPoint(playerPosition);
    }

    public Vector2 ClampCameraPosition(Vector2 targetPosition, float halfWidth, float halfHeight)
    {
        Vector2 bottomLeft = _viewBottomLeft.position;
        Vector2 topRight = _viewTopRight.position;

        var minX = _limitLeft ? bottomLeft.x + halfWidth : float.NegativeInfinity;
        var maxX = _limitRight ? topRight.x - halfWidth : float.PositiveInfinity;
        var minY = _limitBottom ? bottomLeft.y + halfHeight : float.NegativeInfinity;
        var maxY = _limitTop ? topRight.y - halfHeight : float.PositiveInfinity;

        var x = targetPosition.x;
        var y = targetPosition.y;

        if (minX <= maxX)
            x = Mathf.Clamp(x, minX, maxX);
        else if (_isSmallRoom) // 방의 x축 폭이 카메라 뷰보다 작으면
            x = (bottomLeft.x + topRight.x) * 0.5f;

        if (minY <= maxY) y = Mathf.Clamp(y, minY, maxY);

        return new Vector2(x, y);
    }
}