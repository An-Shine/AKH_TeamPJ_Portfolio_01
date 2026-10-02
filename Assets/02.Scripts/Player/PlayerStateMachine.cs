using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    // 순간이동을 사용한 뒤 다음 사용까지의 대기 시간
    [SerializeField, Min(0f)] private float _dashCooldown = 0.5f;

    private float _interactionRemaining;
    private float _cooldownRemaining;

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
    public Vector2 MovementDirection { get; private set; }
    public bool IsDashing => CurrentState == PlayerState.Dash;
    public bool IsInteracting => CurrentState == PlayerState.Interact;
    public bool CanInteract => !IsDashing && !IsInteracting;
    public bool CanDash => !IsDashing && !IsInteracting && _cooldownRemaining <= 0f;

    public bool TryStartInteraction(float duration)
    {
        if (!CanInteract || duration <= 0f || Time.timeScale <= 0f) return false;
        _interactionRemaining = duration;
        MovementDirection = Vector2.zero;
        ChangeState(PlayerState.Interact);
        return true;
    }

    public void Act(Vector2 moveInput, Vector2 aimDirection, bool dashRequested, float deltaTime)
    {
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

        if (dashRequested && CanDash)
        {
            // 순간이동은 한 물리 프레임만 처리하며 항상 조준 방향을 사용합니다.
            _cooldownRemaining = _dashCooldown;
            ChangeState(PlayerState.Dash);
            MovementDirection = aimDirection.normalized;
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
        CurrentState = PlayerState.Idle;
        MovementDirection = Vector2.zero;
        _interactionRemaining = 0f;
        _cooldownRemaining = 0f;
    }
}