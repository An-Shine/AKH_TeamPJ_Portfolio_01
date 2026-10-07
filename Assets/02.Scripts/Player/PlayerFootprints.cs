using UnityEngine;

// PlayerController가 이번 물리 프레임의 상태를 바꾸기 전에 이전 이동 결과를 읽습니다.
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(PlayerStateMachine), typeof(Rigidbody2D))]
public class PlayerFootprints : MonoBehaviour
{
    [SerializeField] private FootprintPool _pool;
    [SerializeField, Min(0.01f)] private float _stepDistance = 0.35f;
    [SerializeField, Min(0f)] private float _footSeparation = 0.1f;
    [SerializeField] private Vector2 _groundOffset = new Vector2(0f, -0.35f);
    [SerializeField] private float _spriteAngleOffset;
    [SerializeField, Min(0.01f)] private float _teleportThreshold = 1.5f;

    private PlayerStateMachine _state;
    private Rigidbody2D _body;
    private Vector3 _previousPosition;
    private float _distanceSinceStep;
    private bool _leftFoot = true;

    private void Awake()
    {
        _state = GetComponent<PlayerStateMachine>();
        _body = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _previousPosition = transform.position;
        _distanceSinceStep = 0f;
        _leftFoot = true;
    }

    private void Start()
    {
        if (_pool == null) _pool = FindFirstObjectByType<FootprintPool>();
    }

    private void FixedUpdate()
    {
        Vector3 position = new Vector3(_body.position.x, _body.position.y, transform.position.z);
        bool walking = _state.CurrentState == PlayerState.Run && !PlayerManager.IsInputBlocked;
        TrackPosition(position, walking);
    }

    private void TrackPosition(Vector3 position, bool walking)
    {
        Vector3 start = _previousPosition;
        _previousPosition = position;
        Vector2 delta = position - start;
        float distance = delta.magnitude;
        if (!walking || _pool == null || !_pool.isActiveAndEnabled || distance > _teleportThreshold)
        {
            _distanceSinceStep = 0f;
            return;
        }
        if (distance <= 0.00001f) return;

        Vector2 direction = delta / distance;
        Vector2 side = new Vector2(-direction.y, direction.x);
        float spacing = Mathf.Max(0.01f, _stepDistance);
        float nextStep = spacing - _distanceSinceStep;
        Quaternion rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + _spriteAngleOffset);

        // 한 프레임에 여러 걸음을 이동해도 실제 이동 구간 위에 일정 간격으로 배치합니다.
        while (nextStep <= distance)
        {
            Vector3 stamp = Vector3.Lerp(start, position, nextStep / distance);
            Vector2 offset = _groundOffset + side * (_leftFoot ? _footSeparation : -_footSeparation);
            stamp += new Vector3(offset.x, offset.y, 0f);
            if (_pool.Spawn(stamp, rotation, !_leftFoot)) _leftFoot = !_leftFoot;
            nextStep += spacing;
        }
        _distanceSinceStep = Mathf.Repeat(_distanceSinceStep + distance, spacing);
    }
}
