using UnityEngine;

public class ItemRoomEntryTrigger : MonoBehaviour
{
    [SerializeField] private ItemRoomDoorController _itemRoomDoor;

    private Collider2D _entryArea;
    private PlayerController _player;
    private Collider2D _movementCollider;
    private bool _hasEntered;

    private void Awake()
    {
        _entryArea = GetComponent<Collider2D>();
    }

    private void FixedUpdate()
    {
        if (_hasEntered || _itemRoomDoor == null || _entryArea == null || !_entryArea.enabled) return;

        var player = PlayerManager.Instance?.PlayerControllerObj;
        if (player == null || !player.isActiveAndEnabled) return;

        if (_player != player)
        {
            _player = player;
            // 루트의 이동 콜라이더만 사용하고 자식의 몸통 피격 콜라이더는 제외합니다.
            _movementCollider = player.GetComponent<Collider2D>();
        }

        if (_movementCollider == null || !_movementCollider.enabled || _movementCollider.isTrigger) return;

        Vector2 footPosition = _movementCollider.bounds.center;
        if (!_entryArea.OverlapPoint(footPosition)) return;

        _hasEntered = true;
        _itemRoomDoor.EnterRoom();
    }
}
