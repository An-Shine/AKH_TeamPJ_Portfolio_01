using System.Collections.Generic;
using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _blinkDistance;
    [SerializeField] private GameObject _dashEffectPrefab;
    private float _dashEffectLifetime = 0.2f;
    private Rigidbody2D _body;
    private PlayerHitFlash _hitFlash;
    [SerializeField] private PlayerDashAfterimage _afterimage;
    private PlayerStateMachine _state;
    private Vector2 _lastSoundPosition;
    private bool _blinkSinceLastFrame;
    private float _lastSoundFixedTime = -1f;
    private readonly List<RaycastHit2D> _blinkHits = new List<RaycastHit2D>();
    private readonly List<Collider2D> _landingOverlaps = new List<Collider2D>();

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _hitFlash = GetComponent<PlayerHitFlash>();
        if (_afterimage == null) _afterimage = GetComponent<PlayerDashAfterimage>();
        _state = GetComponent<PlayerStateMachine>();
        _lastSoundPosition = _body.position;
        _body.gravityScale = 0f;
        _body.freezeRotation = true;
    }

    // PlayerController.FixedUpdate에서만 호출합니다.
    public void Move(Vector2 direction, bool isDashing)
    {
        if (isDashing)
        {
            Blink(direction);
            return;
        }

        float moveSpeed = _moveSpeed;
        if (PlayerManager.Instance != null && PlayerManager.Instance.PlayerStatusData != null)
        {
            PlayerStatus status = PlayerManager.Instance.PlayerStatusData;
            moveSpeed = Input.GetMouseButton(0) ? status.ShootingMoveSpeed : status.MoveSpeed;
        }
        else if (ItemManager.Instance != null && ItemManager.Instance.PlayerStat != null)
        {
            moveSpeed *= Mathf.Max(0f, 1f + ItemManager.Instance.PlayerStat.MoveSpeedMultiplier);
        }

        _body.linearVelocity = direction * moveSpeed;
    }

    private void Blink(Vector2 direction)
    {
        Stop();
        if (direction.sqrMagnitude <= 0f || _blinkDistance <= 0f) return;

        direction.Normalize();
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = false;

        Vector2 start = _body.position;
        float distance = _blinkDistance;
        _body.Cast(direction, filter, _blinkHits, distance);
        foreach (RaycastHit2D hit in _blinkHits)
        {
            // 문과 문틀은 도착 지점이 비어 있어도 뛰어넘지 않습니다.
            // 열린 문의 비활성 충돌체와 감지용 트리거는 Cast 대상에서 제외됩니다.
            if (hit.collider != null && hit.collider.GetComponentInParent<DoorController>() != null)
            {
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.02f));
            }
        }

        Vector2 destination = start + direction * distance;
        if (CanLandAt(destination, filter))
        {
            // 도착 지점에 몸 전체가 들어가면 경로의 얇은 장애물을 넘습니다.
            SpawnDashEffect(start, destination);
            _body.position = destination;
            return;
        }

        // 도착 지점도 막혀 있으면 처음 만나는 장애물 앞에서 멈춥니다.
        foreach (RaycastHit2D hit in _blinkHits)
        {
            distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.02f));
        }

        // 속도로 이동하지 않고 충돌 가능한 지점 앞까지 즉시 위치를 변경합니다.
        SpawnDashEffect(start, start + direction * distance);
        _body.position += direction * distance;
    }

    private void SpawnDashEffect(Vector2 position, Vector2 destination)
    {
        _blinkSinceLastFrame = true;
        if ((destination - position).sqrMagnitude > 0.0001f && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.PlayerDash);
        }
        if (_afterimage != null) _afterimage.Play(position, destination);
        if (_hitFlash != null) _hitFlash.PlayDashGlow();
        if (_dashEffectPrefab == null) return;

        Vector3 worldPosition = new Vector3(position.x, position.y, transform.position.z);
        GameObject effect = Instantiate(_dashEffectPrefab, worldPosition, Quaternion.identity);
        Destroy(effect, _dashEffectLifetime);
    }

    private bool CanLandAt(Vector2 position, ContactFilter2D filter)
    {
        _body.Overlap(position, _body.rotation, filter, _landingOverlaps);
        foreach (Collider2D overlap in _landingOverlaps)
        {
            if (overlap.attachedRigidbody != _body)
            {
                return false;
            }
        }

        return true;
    }

    public void Stop()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetPlayerWalking(false);
        if (_body != null)
        {
            _body.linearVelocity = Vector2.zero;
        }
    }

    private void LateUpdate()
    {
        // 렌더 프레임 사이에 물리가 갱신되지 않았다고 걷기 루프를 끊지 않습니다.
        if (_lastSoundFixedTime == Time.fixedTime) return;
        _lastSoundFixedTime = Time.fixedTime;
        Vector2 position = _body.position;
        bool moved = (position - _lastSoundPosition).sqrMagnitude > 0.000001f;
        bool walking = moved && !_blinkSinceLastFrame && _state != null &&
                       _state.CurrentState == PlayerState.Run && _body.linearVelocity.sqrMagnitude > 0.0001f;
        if (AudioManager.Instance != null) AudioManager.Instance.SetPlayerWalking(walking);
        _lastSoundPosition = position;
        _blinkSinceLastFrame = false;
    }

    private void OnDisable()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetPlayerWalking(false);
    }
}
