using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
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
    private readonly int[,] _hashes = new int[System.Enum.GetValues(typeof(Motion)).Length, System.Enum.GetValues(typeof(FacingDirection)).Length];
    private int _lastHash;
    private Motion _lastMotion;
    private bool _hasPlayed;
    private float _reloadUntil;
    private Weapon _lastGunWeapon;
    private FacingDirection _lastGunFacing;
    private bool _hasGunSelection;
    private bool _isDead;

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
        _reloadUntil = 0f;
        _hasPlayed = false;
    }

    public void PlayReload(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        _reloadUntil = Time.time + duration;
        _hasPlayed = false;
    }

    public void Refresh(PlayerState state, FacingDirection facing, bool isAttacking, int shotSequence)
    {
        if (state == PlayerState.Die) PlayDie();
        if (_isDead) return;
        RefreshGunSprite(facing);

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
        if ((motion == Motion.Reload || motion == Motion.RunReload || motion == Motion.Interact) && !_animator.HasState(0, hash))
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
        if (_isDead) return;
        _isDead = true;
        _reloadUntil = 0f;
        if (_gunRenderer != null) _gunRenderer.enabled = false;
        _animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        _animator.speed = 1f;
        _animator.Play(Animator.StringToHash($"{_layerName}.Die"), 0, 0f);
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
            return;
        }

        _lastGunWeapon = weapon;
        _lastGunFacing = facing;
        _hasGunSelection = true;

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

        _gunRenderer.sprite = sprite;
    }
}
