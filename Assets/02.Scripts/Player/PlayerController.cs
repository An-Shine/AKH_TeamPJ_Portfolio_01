using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerAnimation _animation;
    [SerializeField, Min(0f)] private float _interactionRadius = 1.5f;
    [SerializeField, Min(0.01f)] private float _interactionDuration = 0.5f;
    private PlayerMotor _motor;
    private PlayerStateMachine _fsm;
    private PlayerAim _aim;
    public PlayerWeapon _weapon;
    private Vector2 _moveInput;
    private bool _dashQueued;

    private void Awake()
    {
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
        if (Input.GetKeyDown(KeyCode.Space) && !_fsm.IsInteracting && !_weapon.IsReloading)
        {
            _dashQueued = true;
        }
        _aim.Refresh();
        if (Input.GetKeyDown(KeyCode.G))
        {
            TryInteract();
        }

        // FixedUpdate 전에 들어온 대시 입력도 즉시 사격 제한에 반영합니다.
        bool dashStarting = _dashQueued && _fsm.CanDash;
        Vector2 fireDirection = _weapon.Muzzle != null
            ? _aim.GetDirectionFrom(_weapon.Muzzle.position)
            : _aim.AimDirection;
        _weapon.Tick(Input.GetMouseButton(0), _fsm.IsDashing || dashStarting || _fsm.IsInteracting, fireDirection);
        if (Input.GetKeyDown(KeyCode.R) && !_fsm.IsInteracting)
        {
            float reloadDuration = _weapon.Reload();
            if (_weapon.IsReloading)
            {
                _dashQueued = false;
                _motor.Stop();
            }
            if (_animation != null)
            {
                _animation.PlayReload(reloadDuration);
            }
        }
    }

    public bool TryInteract()
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || !_fsm.CanInteract ||
            (_dashQueued && _fsm.CanDash))
        {
            return false;
        }

        ItemManager items = ItemManager.Instance;
        if (items == null || items.PlayerStat == null) return false;

        Item nearest = null;
        float nearestDistance = _interactionRadius * _interactionRadius;
        // G 입력 때만 탐색합니다. 아이템에 Collider가 없어도 획득할 수 있습니다.
        foreach (Item item in FindObjectsByType<Item>(FindObjectsSortMode.None))
        {
            if (!item.isActiveAndEnabled || item.Data == null || item.Effects == null) continue;
            float distance = ((Vector2)(item.transform.position - transform.position)).sqrMagnitude;
            if (distance > nearestDistance) continue;
            nearestDistance = distance;
            nearest = item;
        }

        if (nearest == null || !_fsm.TryStartInteraction(_interactionDuration)) return false;
        _dashQueued = false;
        _motor.Stop();
        _weapon.CancelAttackVisual();
        if (_animation != null)
        {
            _animation.BeginInteraction();
        }
        items.GainItem(nearest);
        PlayerManager.Instance.GetItem();
        return true;
    }

    private void FixedUpdate()
    {
        bool isReloading = _weapon.IsReloading;
        _fsm.Act(isReloading ? Vector2.zero : _moveInput, _aim.AimDirection,
            !isReloading && _dashQueued, Time.fixedDeltaTime);
        _dashQueued = false;
        if (_fsm.IsDashing && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.BeginDashInvulnerability();
        }
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
