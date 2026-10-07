using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    // 순간이동을 사용한 뒤 다음 사용까지의 대기 시간
    public float DashCooldown = 1f;

    private float _interactionRemaining;
    private float _cooldownRemaining;
    private Vector2 _lastMoveDirection = Vector2.right;

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
    public Vector2 MovementDirection { get; private set; }
    public bool IsDashing => CurrentState == PlayerState.Dash;
    public bool IsInteracting => CurrentState == PlayerState.Interact;
    public bool IsDead => CurrentState == PlayerState.Die;
    public bool CanInteract => !IsDead && !IsDashing && !IsInteracting;
    public bool CanDash => !IsDead && !IsDashing && !IsInteracting && _cooldownRemaining <= 0f;

    public bool TryStartInteraction(float duration)
    {
        if (!CanInteract || duration <= 0f || Time.timeScale <= 0f) return false;
        _interactionRemaining = duration;
        MovementDirection = Vector2.zero;
        ChangeState(PlayerState.Interact);
        return true;
    }

    public void Act(Vector2 moveInput, bool dashRequested, float deltaTime)
    {
        if (IsDead) return;
        _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - deltaTime);

        if (IsInteracting)
        {
            _interactionRemaining = Mathf.Max(0f, _interactionRemaining - deltaTime);
            MovementDirection = Vector2.zero;
            if (_interactionRemaining > 0f) return;
            ChangeState(moveInput.sqrMagnitude > 0f ? PlayerState.Run : PlayerState.Idle);
        }

        if (IsDashing)
        {
            ChangeState(moveInput.sqrMagnitude > 0f ? PlayerState.Run : PlayerState.Idle);
        }

        if (moveInput.sqrMagnitude > 0f)
        {
            _lastMoveDirection = moveInput.normalized;
        }

        if (dashRequested && CanDash)
        {
            // 정지 중에도 마지막 이동 방향을 유지해 순간이동합니다.
            _cooldownRemaining = DashCooldown;
            ChangeState(PlayerState.Dash);
            MovementDirection = _lastMoveDirection;
            return;
        }

        ChangeState(moveInput.sqrMagnitude > 0f ? PlayerState.Run : PlayerState.Idle);
        switch (CurrentState)
        {
            case PlayerState.Idle:
            {
                MovementDirection = Vector2.zero;
                break;
            }
            case PlayerState.Run:
            {
                MovementDirection = moveInput;
                break;
            }
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
        if (IsDead) return;
        CurrentState = PlayerState.Idle;
        MovementDirection = Vector2.zero;
        _interactionRemaining = 0f;
        _cooldownRemaining = 0f;
    }

    public void Die()
    {
        CurrentState = PlayerState.Die;
        MovementDirection = Vector2.zero;
        _interactionRemaining = 0f;
        _cooldownRemaining = 0f;
    }
}