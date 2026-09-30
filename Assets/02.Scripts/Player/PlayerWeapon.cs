using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class PlayerWeapon : MonoBehaviour
{
    private AimDisplayMode _aimDisplayMode = AimDisplayMode.Crosshair;

    // 총구 transform
    [SerializeField] private Transform _muzzle;

    private float _fireInterval = 0.2f;

    // 발사 후 Attack / RunAttack 애니메이션을 표시하는 시간
    [SerializeField, Min(0f)] private float _attackVisualDuration = 0.15f;

    private float _nextFireTime;
    private float _attackVisualUntil;

    public bool IsAttacking => Time.time < _attackVisualUntil;
    public int ShotSequence { get; private set; }
    public AimDisplayMode AimMode => _aimDisplayMode;
    public Transform Muzzle => _muzzle;

    public void Tick(bool fireHeld, bool isDashing, Vector2 aimDirection)
    {
        if (isDashing)
        {
            CancelAttackVisual();
            return;
        }

        if (Time.timeScale <= 0f || !fireHeld || Time.time < _nextFireTime) return;

        _nextFireTime = Time.time + _fireInterval;
        _attackVisualUntil = Time.time + _attackVisualDuration;
        ShotSequence++;
        PlayerFire(aimDirection);
    }

    public void CancelAttackVisual() => _attackVisualUntil = 0f;

    private void PlayerFire(Vector2 aimDirection)
    {
        Debug.LogError("Fire");
    }

    public void SetAimPresentation(AimDisplayMode mode, Transform muzzle)
    {
        _aimDisplayMode = mode;
        _muzzle = muzzle;
    }
}