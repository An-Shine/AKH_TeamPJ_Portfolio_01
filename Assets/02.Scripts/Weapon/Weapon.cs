using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private SFXType _fireSoundType;
    [SerializeField] private GameObject _fireEffectPrefab;
    [SerializeField] private Transform _originPoint;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Transform _effectPoint;
    public WeaponData Data { get; private set; }
    public WeaponModifier Modifier { get; private set; }

    private FireBehaviour _fireBehaviour;
    private IFirePattern _firePattern;

    private const float CoolTime = 10f;

    private bool _isReloading;
    private int _remainingAmmo;
    private float _timer;
    private float _reloadTimer;

    private int Damage => (int)(Modifier.Damage.Flat * (1f + Modifier.Damage.Rate));

    private float FireRate =>
        (Data.FireRate + Modifier.FireRate.Flat + ItemManager.Instance.WeaponStat.FireSpeedModifier)
        * (1f + Modifier.FireRate.Rate + ItemManager.Instance.WeaponStat.FireSpeedMultiplier);

    private float ReloadSpeed => (1 + Modifier.ReloadSpeed.Flat) * (1f + Modifier.ReloadSpeed.Rate);
    public float ChargeSpeed => (1 + Modifier.ChargeSpeed.Flat) * (1f + Modifier.ChargeSpeed.Rate);

    private int MagazineSize =>
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

    private Vector3 _targetDirection;
    private Vector3 _effectDirection;

    private Vector2 _fireOrigin;
    private Vector2 _fireDirection = Vector2.right;
    private bool _hasFirePose;

    private void Awake()
    {
        Modifier = new WeaponModifier();
    }

    private void Start()
    {
        _timer = CoolTime;
        RefreshDirection();
    }

    private void Update()
    {
        _timer += Time.deltaTime * FireRate;
        Reloading();
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
        _fireBehaviour.OnMouseHeld(this);
    }

    public void OnMouseUp()
    {
        _fireBehaviour.OnMouseUp(this);
    }

    public bool TryFire()
    {
        if (_isReloading)
            return false;

        if (!CheckRemainingAmmo())
            return false;

        if (_timer < CoolTime)
            return false;

        Fire();
        _timer = 0;

        _remainingAmmo--;

        return true;
    }

    private void Fire()
    {
        FireContext context = new FireContext(Data.ProjectileType, _firePoint.transform.position, _targetDirection,
            Range, ProjectileCount, Angle);
        _firePattern.Fire(context, Damage);
        AudioManager.Instance.PlaySFX(_fireSoundType);
        if (_fireEffectPrefab != null)
        {
            GameObject effectObject =
                Instantiate(_fireEffectPrefab, _effectPoint.transform.position, Quaternion.identity);
            effectObject.transform.eulerAngles = _effectDirection;
        }
    }

    public void RefreshDirection()
    {
        _targetDirection = _firePoint.position - _originPoint.position;
        float angle = Mathf.Atan2(_targetDirection.y, _targetDirection.x) * Mathf.Rad2Deg + 180f;
        _effectDirection = new Vector3(
            _fireEffectPrefab.transform.eulerAngles.x,
            _fireEffectPrefab.transform.eulerAngles.y,
            angle);
    }

    public bool CheckRemainingAmmo()
    {
        if (_remainingAmmo <= 0)
        {
            return false;
        }

        return true;
    }

    public void Reload()
    {
        _isReloading = true;
        _reloadTimer = 0;
    }

    private void Reloading()
    {
        if (!_isReloading) return;

        if (_isReloading && _reloadTimer < Data.ReloadTime)
        {
            _reloadTimer += Time.deltaTime * ReloadSpeed;
            _remainingAmmo = (int)(MagazineSize * (_reloadTimer / Data.ReloadTime));
            if (_remainingAmmo > MagazineSize) _remainingAmmo = MagazineSize;
        }

        if (_reloadTimer >= Data.ReloadTime) _isReloading = false;
    }

    public void SetFirePose(Vector2 origin, Vector2 direction)
    {
        _fireOrigin = origin;
        if (direction.sqrMagnitude > 0.000001f)
        {
            _fireDirection = direction.normalized;
        }

        _hasFirePose = true;
    }
}