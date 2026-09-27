using System;
using UnityEngine;
using UnityEngine.Events;

public class PlayerWeapon : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float _fireInterval = 0.2f;

    [Tooltip("발사 후 Attack / RunAttack 애니메이션을 표시하는 시간")] [SerializeField, Min(0f)]
    private float _attackVisualDuration = 0.15f;

    [SerializeField] private bool _canFireDuringDash = false;

    private float _nextFireTime;
    private float _attackVisualUntil;

    public bool IsAttacking => Time.time < _attackVisualUntil;
    public int ShotSequence { get; private set; }

    public void Tick(bool fireHeld, bool isDashing, Vector2 aimDirection)
    {
        if (isDashing && !_canFireDuringDash)
        {
            CancelAttackVisual();
            return;
        }

        if (Time.timeScale <= 0f || !fireHeld || Time.time < _nextFireTime) return;

        _nextFireTime = Time.time + _fireInterval;
        _attackVisualUntil = Time.time + _attackVisualDuration;
        ShotSequence++;
        Fire(aimDirection);
    }

    public void CancelAttackVisual() => _attackVisualUntil = 0f;

    public void Fire(Vector2 aimDirection)
    {
    }
}