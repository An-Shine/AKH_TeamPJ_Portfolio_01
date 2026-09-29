using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private SFXType _fireSoundType;
    [SerializeField] private Transform _firePoint;
    public WeaponData Data { get; private set; }
    public WeaponModifier Modifier { get; private set; }

    private FireBehaviour _fireBehaviour;
    private IFirePattern _firePattern;

    private const float CoolTime = 10f;

    private bool _isReloading;
    private int _remainingAmmo;
    private float _timer;
    private float _reloadTimer;
    private float FireRate => (Data.FireRate + Modifier.FireRate.Flat) * (1f + Modifier.FireRate.Rate);
    private float ReloadSpeed => (1 + Modifier.ReloadSpeed.Flat) * (1f + Modifier.ReloadSpeed.Rate);
    public float ChargeSpeed => (1 + Modifier.ChargeSpeed.Flat) * (1f + Modifier.ChargeSpeed.Rate);
    private int MagazineSize => Data.MagazineSize + Modifier.MagazineSize;
    private int ProjectileCount => Data.ProjectileCount + Modifier.ProjectileCount;
    private float Angle => Data.Angle + Modifier.Angle;
    private float Range => Data.Range + Modifier.Range;

    private void Awake()
    {
        Modifier = new WeaponModifier();
    }

    private void Start()
    {
        _timer = CoolTime;
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
        var context = new FireContext(Data.ProjectileType, _firePoint.transform.position, transform.right, Range,
            ProjectileCount, Angle);
        _firePattern.Fire(context);
        AudioManager.Instance.PlaySFX(_fireSoundType);
    }

    public bool CheckRemainingAmmo()
    {
        if (_remainingAmmo <= 0) return false;
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
}