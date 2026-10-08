using UnityEngine;

public class MapPlayerMarker : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _mapBottomLeft;
    [SerializeField] private Transform _mapTopRight;

    [SerializeField] private RectTransform _mapContent;
    [SerializeField] private RectTransform _playerMarker;

    private MinimapController _minimapController;

    private void Awake()
    {
        if (_playerMarker != null) _playerMarker.SetAsLastSibling();
    }

    private void OnEnable()
    {
        Canvas.ForceUpdateCanvases();
        UpdateMarker();
    }

    private void LateUpdate()
    {
        UpdateMarker();
    }

    private void ResolveWorldReferences()
    {
        if (_player != null && _mapBottomLeft != null && _mapTopRight != null) return;

        if (_minimapController == null) _minimapController = FindFirstObjectByType<MinimapController>();

        if (_minimapController == null) return;

        if (_player == null) _player = _minimapController.Player;
        if (_mapBottomLeft == null) _mapBottomLeft = _minimapController.MapBottomLeft;
        if (_mapTopRight == null) _mapTopRight = _minimapController.MapTopRight;
    }

    private void UpdateMarker()
    {
        ResolveWorldReferences();

        if (_player == null || _mapBottomLeft == null || _mapTopRight == null || _mapContent == null || _playerMarker == null)
        {
            return;
        }

        Vector2 bottomLeft = _mapBottomLeft.position;
        Vector2 topRight = _mapTopRight.position;
        Vector2 playerPosition = _player.position;

        var worldWidth = topRight.x - bottomLeft.x;
        var worldHeight = topRight.y - bottomLeft.y;

        if (worldWidth <= 0f || worldHeight <= 0f) return;

        var normalizedX = (playerPosition.x - bottomLeft.x) / worldWidth;
        var normalizedY = (playerPosition.y - bottomLeft.y) / worldHeight;

        var mapRect = _mapContent.rect;

        var mapPosition = new Vector3(mapRect.xMin + normalizedX * mapRect.width,
            mapRect.yMin + normalizedY * mapRect.height, 0f);

        _playerMarker.position = _mapContent.TransformPoint(mapPosition);
    }
}