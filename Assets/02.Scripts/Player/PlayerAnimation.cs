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
                default: return null;
            }
        }
    }

    private string _layerName = "Base Layer";
    [SerializeField] private SpriteRenderer _gunRenderer;
    [SerializeField] private List<GunSpriteSet> _gunSprites = new List<GunSpriteSet>();
    private Animator _animator;
    private readonly int[,] _hashes = new int[System.Enum.GetValues(typeof(Motion)).Length, 6];
    private int _lastHash;
    private int _lastShotSequence;
    private Motion _lastMotion;
    private bool _hasPlayed;
    private float _reloadUntil;
    private Weapon _lastGunWeapon;
    private FacingDirection _lastGunFacing;
    private bool _hasGunSelection;

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
            for (int d = 0; d < 6; d++)
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
        RefreshGunSprite(facing);

        Motion locomotion = state == PlayerState.Dash ? Motion.Dash
            : state == PlayerState.Run ? Motion.Run : Motion.Idle;
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
            motion = state == PlayerState.Run ? Motion.RunAttack : Motion.Attack;
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

        bool attackMotion = motion == Motion.Attack || motion == Motion.RunAttack;
        bool newShot = shotSequence != _lastShotSequence;
        _lastShotSequence = shotSequence;
        if (_hasPlayed && hash == _lastHash && !(attackMotion && newShot)) return;

        float normalizedTime = 0f;
        if (_hasPlayed && motion == _lastMotion && !(attackMotion && newShot))
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
