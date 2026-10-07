using System.Collections.Generic;
using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _blinkDistance = 3f;
    [SerializeField] private GameObject _dashEffectPrefab;
    [SerializeField] private float _dashEffectLifetime = 0.75f;
    private Rigidbody2D _body;
    private PlayerHitFlash _hitFlash;
    private PlayerDashAfterimage _afterimage;
    private readonly List<RaycastHit2D> _blinkHits = new List<RaycastHit2D>();
    private readonly List<Collider2D> _landingOverlaps = new List<Collider2D>();

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _hitFlash = GetComponent<PlayerHitFlash>();
        _afterimage = GetComponent<PlayerDashAfterimage>();
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
            moveSpeed = PlayerManager.Instance.PlayerStatusData.MoveSpeed;
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
        Vector2 destination = start + direction * _blinkDistance;
        if (CanLandAt(destination, filter))
        {
            // 도착 지점에 몸 전체가 들어가면 경로의 얇은 장애물을 넘습니다.
            SpawnDashEffect(start, destination);
            _body.position = destination;
            return;
        }

        // 도착 지점도 막혀 있으면 처음 만나는 장애물 앞에서 멈춥니다.
        float distance = _blinkDistance;
        _body.Cast(direction, filter, _blinkHits, distance);
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
        if (_body != null)
        {
            _body.linearVelocity = Vector2.zero;
        }
    }
}
