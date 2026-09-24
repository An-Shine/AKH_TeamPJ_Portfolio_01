using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponData Data { get; private set; }
    public WeaponModifier Modifier { get; private set; }
    
    //ToDo : factory를 알아봐서 활용하여 초기화
    private FireBehaviour _fireBehaviour;
    private IFirePattern _firePattern;
    
    private const float CoolTime = 10f;

    private bool _isReloading;
    private int _remainingAmmo;
    private float _timer;

    private float FireRate => (Data.FireRate + Modifier.FireRate.Flat)
                             * (1f + Modifier.FireRate.Rate);
    private float ReloadSpeed => (1 + Modifier.ReloadSpeed.Flat)
                                * (1f + Modifier.ReloadSpeed.Rate);

    private float ChargeSpeed => (1 + Modifier.ChargeSpeed.Flat)
                                 * (1f + Modifier.ChargeSpeed.Rate);

    private int MagazineSize => (Data.MagazineSize + Modifier.MagazineSize);
    private int ProjectileCount => (Data.ProjectileCount + Modifier.ProjectileCount);
    private float Angle => (Data.Angle + Modifier.Angle);
    private float Range => (Data.Range + Modifier.Range);

    private void Start()
    {
        _timer = CoolTime;
        
        //아래는 추후 생성 시 DataManager로부터 초기화
        Data = new WeaponData();
        Modifier = new WeaponModifier();
        _remainingAmmo = MagazineSize;
        //_fireBehaviour initialization
    }

    private void Update()
    {
        _timer += Time.deltaTime * FireRate;
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

        if (_remainingAmmo <= 0)
            return false;

        if (_timer < CoolTime)
            return false;

        Fire();

        _remainingAmmo--;

        return true;
    }

    private void Fire()
    {
        FireContext context = new FireContext(transform.position, transform.up, ProjectileCount, Angle);
        _firePattern.Fire(context);
    }
}