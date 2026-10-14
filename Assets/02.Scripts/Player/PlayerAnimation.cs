using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[DefaultExecutionOrder(-100)]
public class PlayerAnimation : MonoBehaviour
{
    [System.Serializable]
    private class GunSpriteSet
    {
        public int WeaponId;
        public Sprite Up;
        public Sprite Down;
        public Sprite RightUp;
        public Sprite RightDown;
        public Sprite LeftUp;
        public Sprite LeftDown;
        public Sprite Right;
        public Sprite Left;

        public Sprite Get(FacingDirection facing)
        {
            switch (facing)
            {
                case FacingDirection.Up: return Up;
                case FacingDirection.Down: return Down;
                case FacingDirection.RightUp: return RightUp;
                case FacingDirection.RightDown: return RightDown;
                case FacingDirection.LeftUp: return LeftUp;
                case FacingDirection.LeftDown: return LeftDown;
                case FacingDirection.Right: return Right;
                case FacingDirection.Left: return Left;
                default: return null;
            }
        }
    }

    private string _layerName = "Base Layer";
    [SerializeField] private SpriteRenderer _gunRenderer;
    [SerializeField] private List<GunSpriteSet> _gunSprites = new List<GunSpriteSet>();
    private Animator _animator;

    private readonly int[,] _hashes = new int[System.Enum.GetValues(typeof(Motion)).Length,
        System.Enum.GetValues(typeof(FacingDirection)).Length];

    private int _lastHash;
    private Motion _lastMotion;
    private bool _hasPlayed;
    private float _reloadUntil;
    private Weapon _lastGunWeapon;
    private FacingDirection _lastGunFacing;
    private bool _hasGunSelection;
    private Sprite _lastGunSprite;
    private bool _isDead;
    [SerializeField] private float _recoilDistance = 0.045f;
    [SerializeField] private float _recoilAngle = 16f;
    [SerializeField] private float _recoilDuration = 0.08f;
    private Weapon _recoilWeapon;
    private int _recoilShotSequence;
    private float _recoilStartedAt = float.NegativeInfinity;
    private Vector3 _gunBasePosition;
    private Quaternion _gunBaseRotation;
    private bool _recoilApplied;

    private void Update()
    {
        // 입력/발사 계산과 Animator 갱신 전에 이전 프레임의 시각 반동을 제거합니다.
        RestoreGunPose();
    }

    private void LateUpdate()
    {
        Weapon weapon = WeaponManager.Instance != null ? WeaponManager.Instance.CurrentWeapon : null;
        if (weapon != _recoilWeapon)
        {
            ResetRecoil();
            _recoilWeapon = weapon;
            _recoilShotSequence = weapon != null ? weapon.ShotSequence : 0;
            if (weapon != null && weapon.LastShotFrame == Time.frameCount)
            {
                _recoilStartedAt = Time.time;
            }
        }

        if (weapon == null || _gunRenderer == null) return;
        if (weapon.ShotSequence != _recoilShotSequence)
        {
            _recoilShotSequence = weapon.ShotSequence;
            // 연사 중에도 누적하지 않고 같은 최대 반동에서 복귀합니다.
            _recoilStartedAt = Time.time;
        }

        if (_isDead || weapon.IsReloading || PlayerManager.IsInputBlocked || !_gunRenderer.enabled)
        {
            ResetRecoil();
            return;
        }

        float progress = (Time.time - _recoilStartedAt) / Mathf.Max(0.01f, _recoilDuration);
        if (progress >= 1f || weapon.OriginPoint == null || weapon.FirePoint == null) return;

        Vector2 direction = ((Vector2)(weapon.FirePoint.position - weapon.OriginPoint.position)).normalized;
        float strength = (1f - Mathf.Clamp01(progress));
        strength *= strength;

        Transform gun = _gunRenderer.transform;
        _gunBasePosition = gun.localPosition;
        _gunBaseRotation = gun.localRotation;

        Vector3 offset = -(Vector3)direction * (_recoilDistance * strength);
        gun.localPosition += gun.parent != null ? gun.parent.InverseTransformVector(offset) : offset;

        float sign = direction.x < 0f ? -1f : 1f;
        gun.localRotation = _gunBaseRotation * Quaternion.Euler(0f, 0f, sign * _recoilAngle * strength);
        _recoilApplied = true;
    }

    private void RestoreGunPose()
    {
        if (!_recoilApplied || _gunRenderer == null) return;
        _gunRenderer.transform.localPosition = _gunBasePosition;
        _gunRenderer.transform.localRotation = _gunBaseRotation;
        _recoilApplied = false;
    }

    private void ResetRecoil()
    {
        RestoreGunPose();
        _recoilStartedAt = float.NegativeInfinity;
    }

    private void OnDisable()
    {
        ResetRecoil();
        _recoilWeapon = null;
    }

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _animator.applyRootMotion = false;
        if (_gunRenderer == null)
        {
            Transform gun = transform.Find("Gun (43)");
            if (gun != null)
            {
                _gunRenderer = gun.GetComponent<SpriteRenderer>();
            }
        }

        for (int m = 0; m < _hashes.GetLength(0); m++)
        {
            for (int d = 0; d < _hashes.GetLength(1); d++)
            {
                _hashes[m, d] = Animator.StringToHash($"{_layerName}.{(Motion)m}_{(FacingDirection)d}");
            }
        }
    }

    private void OnEnable()
    {
        _hasPlayed = false;
        _reloadUntil = 0f;
        _hasGunSelection = false;
    }

    public void BeginInteraction()
    {
        ResetRecoil();
        _reloadUntil = 0f;
        _hasPlayed = false;
    }

    public void PlayReload(float duration)
    {
        ResetRecoil();
        if (duration <= 0f)
        {
            return;
        }

        _reloadUntil = Time.time + duration;
        _hasPlayed = false;
    }

    public void Refresh(PlayerState state, FacingDirection facing, bool isAttacking, int shotSequence)
    {
        if (state == PlayerState.Dash || state == PlayerState.Interact) ResetRecoil();
        if (state == PlayerState.Die) PlayDie();
        if (_isDead) return;
        // 상호작용 클립의 무기 숨김은 유지하고, 종료 후에는 선택한 스프라이트를 복원합니다.
        if (state != PlayerState.Interact) RefreshGunSprite(facing);

        Motion locomotion = state == PlayerState.Run ? Motion.Run : Motion.Idle;
        Motion motion = locomotion;
        if (state == PlayerState.Interact)
        {
            motion = Motion.Interact;
        }
        else if (state != PlayerState.Dash && Time.time < _reloadUntil)
        {
            motion = state == PlayerState.Run ? Motion.RunReload : Motion.Reload;
        }
        else if (isAttacking && state != PlayerState.Dash)
        {
            motion = state == PlayerState.Run ? Motion.RunAttack : Motion.IdleAttack;
        }

        int hash = _hashes[(int)motion, (int)facing];
        if ((motion == Motion.Reload || motion == Motion.RunReload || motion == Motion.Interact) &&
            !_animator.HasState(0, hash))
        {
            hash = _hashes[(int)motion, (int)FacingDirection.Down];
        }

        // 해당 상태가 없으면 현재 이동 애니메이션을 유지합니다.
        if (!_animator.HasState(0, hash) && motion != locomotion)
        {
            motion = locomotion;
            hash = _hashes[(int)motion, (int)facing];
        }

        if (!_animator.HasState(0, hash))
        {
            return;
        }

        // 같은 Run/Idle 클립을 재생 중이어도 아이템 배율 변경을 즉시 반영합니다.
        ItemPlayerStat playerStat = ItemManager.Instance != null ? ItemManager.Instance.PlayerStat : null;
        _animator.speed = (motion == Motion.Run || motion == Motion.Idle) && playerStat != null
            ? Mathf.Max(0f, 1f + playerStat.MoveSpeedMultiplier)
            : 1f;

        bool attackMotion = motion == Motion.IdleAttack || motion == Motion.RunAttack;
        // 추가 발사는 공격 유지 시간만 연장하고 현재 애니메이션은 계속 재생합니다.
        if (_hasPlayed && hash == _lastHash) return;

        float normalizedTime = 0f;
        bool wasAttackMotion = _lastMotion == Motion.IdleAttack || _lastMotion == Motion.RunAttack;
        bool reloadMotion = motion == Motion.Reload || motion == Motion.RunReload;
        bool wasReloadMotion = _lastMotion == Motion.Reload || _lastMotion == Motion.RunReload;
        // 공격/재장전 중 이동 여부가 바뀌어도 현재 재생 진행도를 유지합니다.
        if (_hasPlayed && (motion == _lastMotion || (attackMotion && wasAttackMotion) ||
                           (reloadMotion && wasReloadMotion)))
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            normalizedTime = info.loop
                ? Mathf.Repeat(info.normalizedTime, 1f)
                : Mathf.Clamp01(info.normalizedTime);
        }

        _animator.Play(hash, 0, normalizedTime);
        _lastHash = hash;
        _lastMotion = motion;
        _hasPlayed = true;
    }

    public void PlayDie()
    {
        ResetRecoil();
        if (_isDead) return;
        _isDead = true;
        _reloadUntil = 0f;
        if (_gunRenderer != null) _gunRenderer.enabled = false;
        _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        _animator.speed = 1f;
        _animator.Play(Animator.StringToHash($"{_layerName}.Die"), 0, 0f);
    }

    public void Revive()
    {
        _isDead = false;
        _reloadUntil = 0f;
        _hasPlayed = false;
        _hasGunSelection = false;
        ResetRecoil();
        _animator.updateMode = AnimatorUpdateMode.Normal;
        _animator.speed = 1f;
        if (_gunRenderer != null)
        {
            _gunRenderer.enabled = true;
        }

        Refresh(PlayerState.Idle, FacingDirection.Down, false, 0);
    }

    private void RefreshGunSprite(FacingDirection facing)
    {
        if (_gunRenderer == null)
        {
            return;
        }

        WeaponManager weaponManager = WeaponManager.Instance;
        Weapon weapon = weaponManager != null ? weaponManager.CurrentWeapon : null;
        if (_hasGunSelection && _lastGunWeapon == weapon && _lastGunFacing == facing)
        {
            // Animator의 Sprite 키/Write Defaults가 덮어써도 무기 선택 결과는 유지합니다.
            if (_gunRenderer.sprite != _lastGunSprite)
            {
                _gunRenderer.sprite = _lastGunSprite;
            }

            return;
        }

        _lastGunWeapon = weapon;
        _lastGunFacing = facing;
        // _hasGunSelection = true;

        Sprite sprite = null;
        DataManager dataManager = DataManager.Instance;
        if (weapon != null && dataManager != null && dataManager.WeaponDatas != null)
        {
            foreach (GunSpriteSet sprites in _gunSprites)
            {
                if (sprites == null || !dataManager.WeaponDatas.TryGetValue(
                        IdPrefix.WeaponPrefix + sprites.WeaponId, out WeaponData data) ||
                    !ReferenceEquals(weapon.Data, data))
                {
                    continue;
                }

                sprite = sprites.Get(facing);
                break;
            }
        }

        _lastGunSprite = sprite;
        // 데이터 초기화 전의 null을 캐시하면 같은 무기/방향에서 다시 찾지 못합니다.
        _hasGunSelection = weapon == null || sprite != null;
        _gunRenderer.sprite = sprite;
    }
}
