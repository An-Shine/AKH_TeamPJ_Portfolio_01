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
            motion = Motion.Reload;
        }
        else if (isAttacking && state != PlayerState.Dash)
        {
            motion = state == PlayerState.Run ? Motion.RunAttack : Motion.IdleAttack;
        }

        int hash = _hashes[(int)motion, (int)facing];
        if ((motion == Motion.Reload || motion == Motion.Interact) && !_animator.HasState(0, hash))
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
        // 정지 공격과 이동 공격 사이에서도 현재 재생 진행도를 유지합니다.
        if (_hasPlayed && (motion == _lastMotion || (attackMotion && wasAttackMotion)))
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
            // Animator의 Sprite 키/Write Defaults가 덮어써도 무기 선택 결과는 유지합니다.
            if (_gunRenderer.sprite != _lastGunSprite)
            {
                _gunRenderer.sprite = _lastGunSprite;
            }

            return;
        }

        _lastGunWeapon = weapon;
        _lastGunFacing = facing;

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