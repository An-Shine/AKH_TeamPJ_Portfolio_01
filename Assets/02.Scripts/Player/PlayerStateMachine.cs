using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    [SerializeField, Min(0.02f)] private float _dashDuration = 0.2f;
    [Tooltip("대시가 끝난 뒤 다음 대시까지의 대기 시간")]
    [SerializeField, Min(0f)] private float _dashCooldown = 0.5f;
    private float _dashRemaining;
    private float _cooldownRemaining;
    private Vector2 _dashDirection;

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
    public Vector2 MovementDirection { get; private set; }
    public bool IsDashing => CurrentState == PlayerState.Dash;
    public bool CanDash => !IsDashing && _cooldownRemaining <= 0f;

    public void Tick(Vector2 moveInput, Vector2 aimDirection,
                     bool dashRequested, float deltaTime)
    {
        _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);

        if (IsDashing)
        {
            _dashRemaining -= deltaTime;
            if (_dashRemaining > 0.00001f)
            {
                MovementDirection = _dashDirection;
                return;
            }
            ChangeState(moveInput.sqrMagnitude > 0f ? PlayerState.Run : PlayerState.Idle);
        }

        if (dashRequested && CanDash)
        {
            // 이동 중에는 이동 방향, 정지 중에는 마우스 방향으로 대시.
            _dashDirection = (moveInput.sqrMagnitude > 0f ? moveInput : aimDirection).normalized;
            _dashRemaining = _dashDuration;
            _cooldownRemaining = _dashDuration + _dashCooldown;
            ChangeState(PlayerState.Dash);
            MovementDirection = _dashDirection;
            return;
        }

        ChangeState(moveInput.sqrMagnitude > 0f ? PlayerState.Run : PlayerState.Idle);
        switch (CurrentState)
        {
            case PlayerState.Idle: MovementDirection = Vector2.zero; break;
            case PlayerState.Run: MovementDirection = moveInput; break;
        }
    }

    private void ChangeState(PlayerState next)
    {
        if (CurrentState == next) return;
        // 상태별 Exit / Enter 처리가 필요해지면 이곳에서 호출합니다.
        CurrentState = next;
    }

    public void ResetState()
    {
        CurrentState = PlayerState.Idle;
        MovementDirection = Vector2.zero;
        _dashRemaining = 0f;
        _cooldownRemaining = 0f;
    }
}
