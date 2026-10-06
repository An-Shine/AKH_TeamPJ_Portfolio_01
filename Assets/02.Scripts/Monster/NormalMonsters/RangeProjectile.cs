using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class RangeProjectile : MonoBehaviour
{
    [Header("투사체 설정")]
    [SerializeField, Min(0f)] private float _moveSpeed = 8f;
    [SerializeField, Min(0.1f)] private float _lifeTime = 3f;
    [SerializeField, Min(0f)] private float _damage = 10f;

    [Header("충돌 판정")]
    [Tooltip("이동 경로 전체를 검사하는 월드 단위 반지름입니다.")]
    [SerializeField, Min(0.01f)] private float _sweepRadius = 0.08f;
    [SerializeField] private LayerMask _hitLayers = ~0;

    [Header("적중 효과")]
    [Tooltip("비워 두면 기본 픽셀 적중 효과를 표시합니다.")]
    [SerializeField] private GameObject _impactEffectPrefab;
    [SerializeField, Min(0.1f)] private float _impactEffectLifetime = 0.5f;
    [SerializeField] private Color _impactColor = new Color(0.25f, 0.85f, 1f, 1f);

    [Header("사운드")]
    [Tooltip("투사체가 처음 발사될 때 재생합니다.")]
    [SerializeField] private AudioClip _fireSound;
    [Tooltip("플레이어에게 피해가 실제로 적용됐을 때 재생합니다.")]
    [SerializeField] private AudioClip _playerHitSound;
    [Tooltip("벽이나 장애물에 충돌했을 때 재생합니다.")]
    [SerializeField] private AudioClip _obstacleHitSound;
    [SerializeField, Range(0f, 1f)] private float _soundVolume = 0.75f;
    [Tooltip("SFX 믹서 그룹이 있으면 연결합니다. 비워 둬도 재생됩니다.")]
    [SerializeField] private AudioMixerGroup _soundMixerGroup;

    // 보스가 소환한 몬스터의 탄만 골라 정리할 수 있게 소유자를 제공합니다.
    public Transform Owner => _owner;

    private readonly List<RaycastHit2D> _castHits = new List<RaycastHit2D>(16);
    private Vector2 _direction;
    private Transform _owner;
    private Transform _player;
    private Rigidbody2D _body;
    private Collider2D[] _colliders;
    private Collider2D[] _playerColliders;
    private ContactFilter2D _filter;
    private bool _hasImpacted;
    private bool _hasPlayedFireSound;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _colliders = GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D bodyCollider in _colliders)
        {
            bodyCollider.isTrigger = true;
        }

        if (_body != null)
        {
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            _body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        _filter = new ContactFilter2D();
        _filter.SetLayerMask(_hitLayers);
        _filter.useTriggers = true;
    }

    public void Initialize(Vector2 direction, Transform owner, Transform player)
    {
        _owner = owner;
        _player = player;
        CachePlayerColliders();
        SetDirection(direction);

        if (_owner == null)
        {
            return;
        }

        foreach (Collider2D ownerCollider in _owner.GetComponentsInChildren<Collider2D>(true))
        {
            foreach (Collider2D projectileCollider in _colliders)
            {
                if (ownerCollider.enabled && projectileCollider.enabled &&
                    ownerCollider.gameObject.activeInHierarchy && projectileCollider.gameObject.activeInHierarchy)
                {
                    Physics2D.IgnoreCollision(projectileCollider, ownerCollider);
                }
            }
        }
    }

    // 기존 발사 코드에서 사용하던 API도 유지합니다.
    public void SetDirection(Vector2 direction)
    {
        _direction = direction.normalized;

        if (_direction.sqrMagnitude < 0.000001f)
        {
            return;
        }

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (_body != null)
        {
            _body.rotation = angle;
        }

        if (!_hasPlayedFireSound && !_hasImpacted)
        {
            _hasPlayedFireSound = true;
            PlaySound(_fireSound, transform.position);
        }
    }

    private void Start()
    {
        if (_player == null)
        {
            _player = MonsterCombat.FindPlayer(transform);
        }

        CachePlayerColliders();
        Destroy(gameObject, Mathf.Max(0.1f, _lifeTime));
    }

    private void CachePlayerColliders()
    {
        _playerColliders = _player != null ? _player.GetComponentsInChildren<Collider2D>(true) : null;
    }

    private void Update()
    {
        if (_body == null)
        {
            MoveProjectile(Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (_body != null && _body.simulated)
        {
            MoveProjectile(Time.fixedDeltaTime);
        }
    }

    private void MoveProjectile(float deltaTime)
    {
        if (_hasImpacted || deltaTime <= 0f || _direction.sqrMagnitude < 0.000001f)
        {
            return;
        }

        Vector2 origin = _body != null ? _body.position : (Vector2)transform.position;
        float distance = Mathf.Max(0f, _moveSpeed) * deltaTime;
        int count = Physics2D.CircleCast(origin, Mathf.Max(0.01f, _sweepRadius),
            _direction, _filter, _castHits, distance);

        bool found = false;
        RaycastHit2D nearest = default(RaycastHit2D);
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = _castHits[i];
            if (!CanImpact(hit.collider) || hit.distance >= nearestDistance)
            {
                continue;
            }

            found = true;
            nearest = hit;
            nearestDistance = hit.distance;
        }

        if (found)
        {
            SetPosition(nearest.centroid);
            Impact(nearest.collider, nearest.point);
            return;
        }

        SetPosition(origin + _direction * distance);
    }

    private void SetPosition(Vector2 position)
    {
        if (_body != null)
        {
            _body.position = position;
        }
        else
        {
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }
    }

    private bool CanImpact(Collider2D other)
    {
        if (other == null || !other.enabled || !other.gameObject.activeInHierarchy)
        {
            return false;
        }

        if ((_hitLayers.value & (1 << other.gameObject.layer)) == 0)
        {
            return false;
        }

        if (other.transform == transform || other.transform.IsChildOf(transform))
        {
            return false;
        }

        if (_owner != null && (other.transform == _owner || other.transform.IsChildOf(_owner)))
        {
            return false;
        }

        if (other.GetComponentInParent<NormalMonster>() != null)
        {
            return false;
        }

        if (MonsterCombat.ResolvePlayerCollider(other, _player) != null)
        {
            // 몸의 콜라이더가 있다면 넓은 감지용 Trigger에서는 적중시키지 않습니다.
            return !other.isTrigger || !MonsterCombat.HasEnabledBodyCollider(_playerColliders);
        }

        return !other.isTrigger;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_hasImpacted && Time.timeScale > 0f && CanImpact(other))
        {
            Impact(other, other.ClosestPoint(transform.position));
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_hasImpacted || Time.timeScale <= 0f || !CanImpact(collision.collider))
        {
            return;
        }

        Vector2 point = collision.contactCount > 0
            ? collision.GetContact(0).point : collision.collider.ClosestPoint(transform.position);
        Impact(collision.collider, point);
    }

    private void Impact(Collider2D other, Vector2 point)
    {
        if (_hasImpacted)
        {
            return;
        }

        // 파괴가 프레임 끝에 실행되어도 추가 콜라이더로 피해를 중복 적용하지 않습니다.
        _hasImpacted = true;

        foreach (Collider2D bodyCollider in _colliders)
        {
            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }
        }

        if (_body != null)
        {
            _body.simulated = false;
        }

        Vector3 hitPosition = new Vector3(point.x, point.y, transform.position.z);
        Transform player = MonsterCombat.ResolvePlayerCollider(other, _player);

        if (player != null)
        {
            bool damaged = MonsterCombat.TryTakePlayerDamage(player, _damage, hitPosition,
                _direction, _impactColor, _impactEffectPrefab == null);

            if (damaged)
            {
                PlaySound(_playerHitSound, hitPosition);

                if (_impactEffectPrefab != null)
                {
                    SpawnCustomImpact(hitPosition);
                }
            }
        }
        else
        {
            PlaySound(_obstacleHitSound, hitPosition);

            if (_impactEffectPrefab != null)
            {
                SpawnCustomImpact(hitPosition);
            }
            else
            {
                MonsterCombatEffect.SpawnImpact(hitPosition, _direction, _impactColor, transform);
            }
        }

        Destroy(gameObject);
    }

    private void SpawnCustomImpact(Vector3 position)
    {
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        GameObject effect = Instantiate(_impactEffectPrefab, position, Quaternion.Euler(0f, 0f, angle));
        Destroy(effect, Mathf.Max(0.1f, _impactEffectLifetime));
    }

    private void PlaySound(AudioClip clip, Vector3 position)
    {
        if (clip == null || _soundVolume <= 0f)
        {
            return;
        }

        // 투사체가 파괴돼도 재생 중인 사운드는 끝까지 유지합니다.
        GameObject soundObject = new GameObject("RangeProjectileSound");
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = Mathf.Clamp01(_soundVolume);
        source.outputAudioMixerGroup = _soundMixerGroup;
        source.clip = clip;
        source.Play();

        Destroy(soundObject, clip.length + 0.1f);
    }
}