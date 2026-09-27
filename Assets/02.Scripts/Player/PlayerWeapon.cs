using System;
using UnityEngine;
using UnityEngine.Events;

public class PlayerWeapon : MonoBehaviour
{
    [Serializable] public class FireEvent : UnityEvent<Vector2> { }

    [Header("현재 무기의 조준 표시")]
    [SerializeField] private AimDisplayMode _aimDisplayMode = AimDisplayMode.Crosshair;
    [Tooltip("현재 장착한 무기의 실제 총구 Transform")]
    [SerializeField] private Transform _muzzle;

    [Header("발사")]
    [SerializeField, Min(0.01f)] private float _fireInterval = 0.2f;
    [Tooltip("발사 후 Attack / RunAttack 애니메이션을 표시하는 시간")]
    [SerializeField, Min(0f)] private float _attackVisualDuration = 0.15f;
    [SerializeField] private bool _canFireDuringDash = false;
    [Tooltip("실제 총알 생성 함수(매개변수 Vector2 = 정규화된 조준 방향)를 연결")]
    [SerializeField] private FireEvent _onFire = new FireEvent();
    private float _nextFireTime;
    private float _attackVisualUntil;

    public bool IsAttacking => Time.time < _attackVisualUntil;
    public int ShotSequence { get; private set; }
    public AimDisplayMode AimMode => _aimDisplayMode;
    public Transform Muzzle => _muzzle;

    // 무기를 교체할 때 새 무기의 표시 방식과 총구를 함께 전달합니다.
    public void SetAimPresentation(AimDisplayMode mode, Transform muzzle)
    {
        _aimDisplayMode = mode;
        _muzzle = muzzle;
    }

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
        _onFire.Invoke(aimDirection);
    }

    public void CancelAttackVisual() => _attackVisualUntil = 0f;
}
