using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(PlayerStateMachine), typeof(Rigidbody2D))]
public class PlayerRunDust : MonoBehaviour
{
    [SerializeField] private DustCloud _pool;
    [SerializeField] private Vector2 _groundOffset = new Vector2(0f, -0.6f);
    private Rigidbody2D _body;
    private PlayerStateMachine _state;
    private Vector2 _previousPosition;
    private float _distance;
    private float _nextSpacing;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _state = GetComponent<PlayerStateMachine>();
    }


    private void OnEnable()
    {
        _previousPosition = _body.position;
        _distance = 0f;
        _nextSpacing = Random.Range(1f, 2f);
    }

    private void FixedUpdate()
    {
        Vector2 current = _body.position;
        float moved = Vector2.Distance(current, _previousPosition);
        _previousPosition = current;
        if (_state.CurrentState != PlayerState.Run || PlayerManager.IsInputBlocked || moved > 1.5f)
        {
            _distance = 0f;
            return;
        }

        _distance += moved;
        if (_distance < _nextSpacing) return;
        _distance = 0f;
        _nextSpacing = Random.Range(1f, 2f);
        Vector2 foot = current + _groundOffset;
        _pool.Spawn(new Vector3(foot.x, foot.y, transform.position.z));
    }
}