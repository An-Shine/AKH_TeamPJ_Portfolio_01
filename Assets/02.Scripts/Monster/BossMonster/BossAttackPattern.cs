using UnityEngine;

public enum BossAttackType
{
    Melee = 1,
    Projectile = 2,
    Explosion = 3
}

public enum BossPatternKind
{
    FallingProjectiles = 1,
    SummonRangeMonsters = 2,
    RandomEdgeBarrage = 3,
    OrbitBarrage = 4,
    ExpandingShockwave = 5,
    ScatteredBombs = 6
}

[CreateAssetMenu(fileName = "BossAttackPattern", menuName = "Monsters/Boss Attack Pattern")]
public sealed class BossAttackPattern : ScriptableObject
{
    [Header("패턴 선택")]
    [SerializeField] private BossPatternKind _patternKind = BossPatternKind.FallingProjectiles;
    [SerializeField, Min(0.01f)] private float _selectionWeight = 1f;
    [SerializeField, Min(0f)] private float _cooldown = 8f;
    [SerializeField, Min(0f)] private float _recoveryTime = 1.5f;

    [Header("애니메이션")]
    [SerializeField, Min(0.1f)] private float _animationSpeed = 1f;
    [Tooltip("실제 내려찍기, 총구 점등, 폭발 시작 프레임의 클립 진행률입니다.")]
    [SerializeField, Range(0f, 0.95f)] private float _releaseNormalizedTime = 0.53f;

    [Header("발생 횟수와 시간")]
    [SerializeField, Min(0.1f)] private float _duration = 5f;
    [SerializeField, Min(0.03f)] private float _emitInterval = 0.65f;
    [SerializeField, Min(1)] private int _waveCount = 3;
    [SerializeField, Min(1)] private int _countPerWave = 4;
    [SerializeField, Min(0.2f)] private float _warningTime = 1f;

    [Header("빨간 원과 범위 피해")]
    [SerializeField, Min(0.1f)] private float _areaRadius = 0.65f;
    [SerializeField, Min(0f)] private float _areaDamage = 12f;
    [SerializeField] private Color _warningColor = new Color(1f, 0.12f, 0.12f, 1f);

    [Header("탄과 폭발탄 이미지")]
    [Tooltip("비워 두면 기본 픽셀 탄을 사용합니다. 낙하탄과 폭발탄에도 사용합니다.")]
    [SerializeField] private Sprite _projectileSprite;
    [SerializeField] private Color _projectileColor = new Color(0.3f, 0.85f, 1f, 1f);
    [SerializeField, Min(0.05f)] private float _projectileSize = 0.28f;
    [SerializeField, Min(0.01f)] private float _projectileCollisionRadius = 0.1f;
    [SerializeField, Min(0f)] private float _projectileDamage = 10f;
    [SerializeField, Min(0.1f)] private float _projectileSpeed = 4.5f;
    [SerializeField, Min(0.2f)] private float _projectileLifetime = 6f;

    [Header("1번 낙하")]
    [SerializeField, Min(0.1f)] private float _fallHeight = 5f;
    [SerializeField, Min(0.1f)] private float _fallDuration = 0.4f;

    [Header("2번 소환")]
    [SerializeField, Min(1)] private int _summonCount = 2;

    [Header("4번 공전")]
    [SerializeField, Min(0.2f)] private float _orbitRadius = 1.8f;
    [SerializeField, Min(0.1f)] private float _orbitDuration = 1.2f;
    [SerializeField] private float _orbitAngularSpeed = 180f;

    [Header("5번 확장 충격파")]
    [SerializeField, Min(0.1f)] private float _shockwaveInitialRadius = 0.5f;
    [SerializeField, Min(0.2f)] private float _shockwaveMaximumRadius = 5.5f;
    [SerializeField, Min(0.2f)] private float _shockwaveExpandTime = 2.5f;
    [SerializeField, Min(0.05f)] private float _shockwaveThickness = 0.25f;

    [Header("6번 폭발탄")]
    [SerializeField, Min(0.2f)] private float _bombFuse = 0.8f;
    [SerializeField, Min(0.2f)] private float _bossCueRadius = 1.3f;

    public BossPatternKind PatternKind => _patternKind;
    public BossAttackType AttackType => (BossAttackType)(((int)_patternKind - 1) / 2 + 1);
    public float SelectionWeight => Mathf.Max(0.01f, _selectionWeight);
    public float Cooldown => Mathf.Max(0f, _cooldown);
    public float RecoveryTime => Mathf.Max(0f, _recoveryTime);
    public float AnimationSpeed => Mathf.Max(0.1f, _animationSpeed);
    public float ReleaseNormalizedTime => Mathf.Clamp(_releaseNormalizedTime, 0f, 0.95f);
    public float Duration => Mathf.Max(0.1f, _duration);
    public float EmitInterval => Mathf.Max(0.03f, _emitInterval);
    public int WaveCount => Mathf.Clamp(_waveCount, 1, 30);
    public int CountPerWave => Mathf.Clamp(_countPerWave, 1, 30);
    public float WarningTime => Mathf.Max(0.2f, _warningTime);
    public float AreaRadius => Mathf.Max(0.1f, _areaRadius);
    public float AreaDamage => Mathf.Max(0f, _areaDamage);
    public Color WarningColor => _warningColor;
    public Sprite ProjectileSprite => _projectileSprite;
    public Color ProjectileColor => _projectileColor;
    public float ProjectileSize => Mathf.Max(0.05f, _projectileSize);
    public float ProjectileCollisionRadius => Mathf.Max(0.01f, _projectileCollisionRadius);
    public float ProjectileDamage => Mathf.Max(0f, _projectileDamage);
    public float ProjectileSpeed => Mathf.Max(0.1f, _projectileSpeed);
    public float ProjectileLifetime => Mathf.Max(0.2f, _projectileLifetime);
    public float FallHeight => Mathf.Max(0.1f, _fallHeight);
    public float FallDuration => Mathf.Max(0.1f, _fallDuration);
    public int SummonCount => Mathf.Clamp(_summonCount, 1, 20);
    public float OrbitRadius => Mathf.Max(0.2f, _orbitRadius);
    public float OrbitDuration => Mathf.Max(0.1f, _orbitDuration);
    public float OrbitAngularSpeed => _orbitAngularSpeed;
    public float ShockwaveInitialRadius => Mathf.Max(0.1f, _shockwaveInitialRadius);
    public float ShockwaveMaximumRadius => Mathf.Max(ShockwaveInitialRadius, _shockwaveMaximumRadius);
    public float ShockwaveExpandTime => Mathf.Max(0.2f, _shockwaveExpandTime);
    public float ShockwaveThickness => Mathf.Max(0.05f, _shockwaveThickness);
    public float BombFuse => Mathf.Max(0.2f, _bombFuse);
    public float BossCueRadius => Mathf.Max(0.2f, _bossCueRadius);

#if UNITY_EDITOR
    public void ConfigureDefaults(BossPatternKind kind)
    {
        _patternKind = kind;
        switch (kind)
        {
            case BossPatternKind.SummonRangeMonsters:
                _cooldown = 12f;
                _duration = 2f;
                break;
            case BossPatternKind.RandomEdgeBarrage:
                _emitInterval = 0.1f;
                _countPerWave = 3;
                _projectileDamage = 8f;
                _projectileSpeed = 4.3f;
                _recoveryTime = 2f;
                break;
            case BossPatternKind.OrbitBarrage:
                _duration = 4f;
                _emitInterval = 0.1f;
                _countPerWave = 1;
                _projectileSpeed = 3.8f;
                _projectileLifetime = 7f;
                _recoveryTime = 2f;
                break;
            case BossPatternKind.ExpandingShockwave:
                _areaDamage = 20f;
                _cooldown = 10f;
                _recoveryTime = 2f;
                break;
            case BossPatternKind.ScatteredBombs:
                _waveCount = 1;
                _countPerWave = 8;
                _areaRadius = 0.85f;
                _areaDamage = 16f;
                _projectileColor = new Color(1f, 0.45f, 0.15f, 1f);
                break;
        }
    }
#endif
}
