using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponData Data { get; private set; }
    public WeaponModifier Modifier { get; private set; }
    
    private FireBehaviour _fireBehaviour;
    
    private const float CoolTime = 10f;

    private bool _isReloading;
    private int _remainingAmmo;
    private float _timer;

    private float FireRate => (Data.FireRate + Modifier.FireRate.Flat)
                             * (1f + Modifier.FireRate.Rate);
    private float ReloadSpeed => (1 + Modifier.ReloadSpeed.Flat)
                                * (1f + Modifier.ReloadSpeed.Rate);

    private int MagazineSize => (Data.MagazineSize + Modifier.MagazineSize);
    private int ProjectileCount => (Data.ProjectileCount + Modifier.ProjectileCount);
    private float Angle => (Data.Angle + Modifier.Angle);

    private void Start()
    {
        _timer = CoolTime;
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
        //Fire Logic
    }
}