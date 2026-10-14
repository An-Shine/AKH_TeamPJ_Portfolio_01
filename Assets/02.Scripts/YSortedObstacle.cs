using UnityEngine;

public class YSortedObstacle : MonoBehaviour
{
    [SerializeField] private Transform _sortPoint;
    [SerializeField] private SpriteRenderer[] _renderers;
    [SerializeField] private string _sortingLayerName = "Player";
    [SerializeField] private int _behindPlayerOrder = -10;
    [SerializeField] private int _inFrontOfPlayerOrder = 10;

    private PlayerController _player;
    private Collider2D _movementCollider;
    private int _sortingLayerId;
    private int _currentOrder;

    private void Awake()
    {
        _sortingLayerId = SortingLayer.NameToID(_sortingLayerName);
        ApplyOrder(_inFrontOfPlayerOrder);
    }

    private void LateUpdate()
    {
        var player = PlayerManager.Instance?.PlayerControllerObj;
        if (player == null) return;

        if (_player != player)
        {
            _player = player;
            _movementCollider = player.GetComponent<Collider2D>();
        }

        float playerY = _movementCollider != null && _movementCollider.enabled
            ? _movementCollider.bounds.center.y
            : player.transform.position.y;
        float wallY = _sortPoint != null ? _sortPoint.position.y : transform.position.y;
        int order = playerY > wallY ? _inFrontOfPlayerOrder : _behindPlayerOrder;
        if (order != _currentOrder) ApplyOrder(order);
    }

    private void ApplyOrder(int order)
    {
        _currentOrder = order;
        if (_renderers == null) return;
        foreach (var spriteRenderer in _renderers)
        {
            if (spriteRenderer == null) continue;
            spriteRenderer.sortingLayerID = _sortingLayerId;
            spriteRenderer.sortingOrder = order;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 point = _sortPoint != null ? _sortPoint.position : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(point + Vector3.left * 1.5f, point + Vector3.right * 1.5f);
    }
}
