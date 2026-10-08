using UnityEngine;

public class MinimapController : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _mapBottomLeft;
    [SerializeField] private Transform _mapTopRight;
    [SerializeField] private RectTransform _miniMapContent;

    public Transform Player => _player;
    public Transform MapBottomLeft => _mapBottomLeft;
    public Transform MapTopRight => _mapTopRight;

    private void LateUpdate()
    {
        if (_player == null || _mapBottomLeft == null || _mapTopRight == null || _miniMapContent == null) return;

        Vector2 bottomLeft = _mapBottomLeft.position;
        Vector2 topRight = _mapTopRight.position;
        Vector2 playerPosition = _player.position;

        var worldWidth = topRight.x - bottomLeft.x;
        var worldHeight = topRight.y - bottomLeft.y;

        if (worldWidth <= 0f || worldHeight <= 0f) return;

        var normalizedX = (playerPosition.x - bottomLeft.x) / worldWidth;

        var normalizedY = (playerPosition.y - bottomLeft.y) / worldHeight;

        var mapRect = _miniMapContent.rect;

        var playerMapPosition = new Vector2(mapRect.xMin + normalizedX * mapRect.width,
            mapRect.yMin + normalizedY * mapRect.height);

        _miniMapContent.anchoredPosition = -playerMapPosition;
    }
}