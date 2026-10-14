using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private SFXType _fireSoundType;
    [SerializeField] private GameObject _fireEffectPrefab;
    public Transform OriginPoint { get; set; }
    public Transform FirePoint { get; set; }
    [SerializeField] private Sprite _hudIcon;

    public Sprite HudIcon => _hudIcon;
    public WeaponData Data { get; private set; }
    public WeaponModifier Modifier { get; private set; }

    private FireBehaviour _fireBehaviour;
    public FireBehaviour FireBehaviour => _fireBehaviour;
    private IFirePattern _firePattern;
    private Projectile _currentProjectile;

    private const float CoolTime = 10f;

    private bool _isReloading;
    public bool IsReloading => _isReloading;
    private int _remainingAmmo;
    public int RemainingAmmo => _remainingAmmo;
    public int ShotSequence { get; private set; }
    public int LastShotFrame { get; private set; } = -1;
    private float _timer;
    private float _fireTimer;
    private float _reloadTimer;

    private int Damage => (int)(Modifier.Damage.Flat * (1f + Modifier.Damage.Rate));

    private float FireRate =>
        (Data.FireRate + Modifier.FireRate.Flat + ItemManager.Instance.WeaponStat.FireSpeedModifier)
        * (1f + Modifier.FireRate.Rate + ItemManager.Instance.WeaponStat.FireSpeedMultiplier);

    private float ReloadSpeed => (1 + Modifier.ReloadSpeed.Flat) * (1f + Modifier.ReloadSpeed.Rate);

    public float ChargeSpeed => (1 + Modifier.ChargeSpeed.Flat)
                                * (1f + Modifier.ChargeSpeed.Rate +
                                   ItemManager.Instance.WeaponStat.ChargeSpeedMultiplier);

    public int MagazineSize =>
        (int)((Data.MagazineSize + Modifier.MagazineSize)
              * (1f + ItemManager.Instance.WeaponStat.MagazineSizeMultiplier));

    private int ProjectileCount =>
        Data.ProjectileCount + Modifier.ProjectileCount + ItemManager.Instance.WeaponStat.ProjectileCountModifier;

    private float Angle =>
        (Data.Angle + Modifier.Angle + ItemManager.Instance.WeaponStat.SpreadAngleModifier)
        * (1f + ItemManager.Instance.WeaponStat.SpreadAngleMultiplier);

    private float Range =>
        (Data.Range + Modifier.Range + ItemManager.Instance.WeaponStat.RangeModifier)
        * (1f + ItemManager.Instance.WeaponStat.RangeMultiplier);

    private int _currentAmmo;
    private float _reloadAmmoRate;

    private Vector3 _targetDirection;
    private Vector3 _effectDirection;

    private Vector2 _fireOrigin;
    private Vector2 _fireDirection = Vector2.right;
    //private bool _hasFirePose;

    private void Awake()
    {
        Modifier = new WeaponModifier();
    }

    private void Start()
    {
        _fireTimer = CoolTime;
        _timer = 0f;
    }

    private void Update()
    {
        _fireTimer += Time.deltaTime * FireRate;
        Reloading();
    }

    private void OnEnable()
    {
        transform.localScale = Vector3.one * Modifier.Size;
    }

    public void Initialize(WeaponData data)
    {
        Data = data;
        _isReloading = false;
        _remainingAmmo = MagazineSize;

        _fireBehaviour = FireFactory.CreateBehaviour(Data.BehaviourType);
        _firePattern = FireFactory.CreatePattern(Data.PatternType);
    }

    public void OnMouseDown()
    {
        _fireBehaviour.OnMouseDown(this);
    }

    public void OnMouseHeld()
    {
        _timer += Time.deltaTime;
        ContinousDecreaseAmmo();
        _fireBehaviour.OnMouseHeld(this);
    }

    public void OnMouseUp()
    {
        _fireBehaviour.OnMouseUp(this);
    }

    public bool CanFire()
    {
        if (_isReloading)
            return false;

        if (!CheckRemainingAmmo())
            return false;

        if (_fireTimer < CoolTime && Data.BehaviourType != FireBehaviourType.Continuous)
            return false;

        return true;
    }

    public bool TryFire()
    {
        if (!CanFire())
        {
            if (!CheckRemainingAmmo())
            {
                AudioManager.Instance.PlaySFX(SFXType.FailFire);
            }

            return false;
        }

        RefreshDirection();
        _currentProjectile = Fire();
        _fireTimer = 0;

        _remainingAmmo--;

        ShotSequence++;
        LastShotFrame = Time.frameCount;

        return true;
    }

    private Projectile Fire()
    {
        FireContext context = new FireContext(Data.ProjectileType, OriginPoint.transform.position,
            _targetDirection.normalized, Range, ProjectileCount, Angle);

        AudioManager.Instance.PlaySFX(_fireSoundType);
        Projectile projectile = _firePattern.Fire(context, Damage);
        LastBullet(projectile);

        if (_fireEffectPrefab != null)
        {
            GameObject effectObject =
                Instantiate(_fireEffectPrefab, OriginPoint.transform.position, Quaternion.identity);
            effectObject.transform.eulerAngles = _effectDirection;
        }

        return projectile;
    }

    private void LastBullet(Projectile projectile)
    {
        if (projectile == null) return;

        ItemManager manager = ItemManager.Instance;
        if (manager != null)
        {
            if (manager.WeaponStat.HasLastBulletEffect && _remainingAmmo <= 1)
            {
                projectile.ApplyLastBulletEffect(manager.WeaponStat.LastBulletMultiplier);
            }
        }
    }

    public void RefreshDirection()
    {
        _targetDirection = FirePoint.position - OriginPoint.position;
        float angle = Mathf.Atan2(_targetDirection.y, _targetDirection.x) * Mathf.Rad2Deg + 180f;

        if (_currentProjectile != null && _currentProjectile is LaserProjectile laser)
        {
            laser.UpdateLaserShape(OriginPoint.position, _targetDirection);
        }

        if (_fireEffectPrefab == null) return;
        _effectDirection = new Vector3(
            _fireEffectPrefab.transform.eulerAngles.x,
            _fireEffectPrefab.transform.eulerAngles.y,
            angle);
    }

    public bool CheckRemainingAmmo()
    {
        if (_remainingAmmo <= 0)
            return false;

        return true;
    }

    public void Reload()
    {
        _isReloading = true;
        _reloadTimer = 0;
        _currentAmmo = _remainingAmmo;
        _reloadAmmoRate = (float)(MagazineSize - _remainingAmmo) / MagazineSize;
    }

    private void Reloading()
    {
        if (!_isReloading) return;

        if (_isReloading && _reloadTimer < Data.ReloadTime * _reloadAmmoRate)
        {
            _reloadTimer += Time.deltaTime * ReloadSpeed;
            _remainingAmmo = _currentAmmo +
                             (int)((MagazineSize * _reloadAmmoRate) *
                                   (_reloadTimer / (Data.ReloadTime * _reloadAmmoRate)));
            if (_remainingAmmo > MagazineSize) _remainingAmmo = MagazineSize;
        }

        if (_reloadTimer >= Data.ReloadTime * _reloadAmmoRate) _isReloading = false;
    }

    private void ContinousDecreaseAmmo()
    {
        if (_fireBehaviour is ContinuousFireBehaviour fireBehaviour)
        {
            if (fireBehaviour.IsFiring && _timer >= 1f)
            {
                _remainingAmmo -= 1;
                _timer = 0f;
            }
        }
    }

    public void SetFirePose(Vector2 origin, Vector2 direction)
    {
        _fireOrigin = origin;
        if (direction.sqrMagnitude > 0.000001f) _fireDirection = direction.normalized;

        //_hasFirePose = true;
    }

    public void CancelActions()
    {
        // 차지 무기는 OnMouseUp 호출 시 발사되므로 입력 해제 대신 동작 자체를 취소합니다.
        StopFire();
        if (AudioManager.Instance != null) AudioManager.Instance.StopLoopSFX();
        _isReloading = false;
        _reloadTimer = 0f;
        _timer = 0f;
        if (Data != null) _fireBehaviour = FireFactory.CreateBehaviour(Data.BehaviourType);
    }

    public void StopFire()
    {
        if (_currentProjectile is LaserProjectile laser)
        {
            //ProjectilePoolManager.Instance.Release(laser, Data.ProjectileType);
            laser.SetEnd();
        }
    }
}