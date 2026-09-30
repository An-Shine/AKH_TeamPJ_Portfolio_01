using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerAnimation _animation;
    private PlayerMotor _motor;
    private PlayerStateMachine _fsm;
    private PlayerAim _aim;
    private PlayerWeapon _weapon;
    private Vector2 _moveInput;
    private bool _dashQueued;

    private void Awake()
    {
        if (FindFirstObjectByType<PlayerManager>() == null)
            gameObject.AddComponent<PlayerManager>();

        _motor = GetComponent<PlayerMotor>();
        _fsm = GetComponent<PlayerStateMachine>();
        _aim = GetComponent<PlayerAim>();
        _weapon = GetComponent<PlayerWeapon>();
        if (_animation == null)
        {
            _animation = GetComponentInChildren<PlayerAnimation>();
        }
        if (_animation == null)
        {
            Debug.LogError("PlayerAnimation을 Visual 자식에 추가하고 연결하세요.", this);
        }
    }

    private void Update()
    {
        _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _dashQueued = true;
        }
        _aim.Refresh();

        // FixedUpdate 전에 들어온 대시 입력도 즉시 사격 제한에 반영합니다.
        bool dashStarting = _dashQueued && _fsm.CanDash;
        Vector2 fireDirection = _weapon.Muzzle != null
            ? _aim.GetDirectionFrom(_weapon.Muzzle.position)
            : _aim.AimDirection;
        _weapon.Tick(Input.GetMouseButton(0), _fsm.IsDashing || dashStarting, fireDirection);
        if (Input.GetKeyDown(KeyCode.R))
        {
            float reloadDuration = _weapon.Reload();
            if (_animation != null)
            {
                _animation.PlayReload(reloadDuration);
            }
        }
    }

    private void FixedUpdate()
    {
        _fsm.Act(_moveInput, _aim.AimDirection, _dashQueued, Time.fixedDeltaTime);
        _dashQueued = false;
        _motor.Move(_fsm.MovementDirection, _fsm.IsDashing);
    }

    private void LateUpdate()
    {
        if (_animation != null)
        {
            _animation.Refresh(_fsm.CurrentState, _aim.Facing, _weapon.IsAttacking, _weapon.ShotSequence);
        }
    }

    private void OnDisable()
    {
        _moveInput = Vector2.zero;
        _dashQueued = false;
        if (_motor != null)
        {
            _motor.Stop();
        }

        if (_fsm != null)
        {
            _fsm.ResetState();
        }

        if (_weapon != null)
        {
            _weapon.CancelAttackVisual();
        }
    }
}
