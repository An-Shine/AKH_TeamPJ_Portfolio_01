using System.Collections.Generic;
using UnityEngine;

namespace _02.Scripts.Monster.Presentation
{
    [DefaultExecutionOrder(1100)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Monster/Normal Monster Game Feel")]
    public sealed class NormalMonsterGameFeel : MonoBehaviour
    {
        private static readonly int UvBoundsId = Shader.PropertyToID("_UvBounds");
        private static readonly int HitFlashAmountId = Shader.PropertyToID("_HitFlashAmount");
        [SerializeField]
        private NormalMonsterFeelProfile _profile;
        [SerializeField]
        private SpriteRenderer _bodyRenderer;
        private NormalMonsterFeelEffects _effects;
        private MonsterNeonVfx _neon;
        private SpriteRenderer _outline;
        private SpriteRenderer _chargeGlow;
        private SpriteRenderer _chargeCore;
        private MonsterAttackCue _attackCue;
        private SpriteRenderer _hitRenderer;
        private Material _bodyMaterial;
        private MaterialPropertyBlock _hitProperties;
        private Vector2 _hitDirection;
        private float _hitAge;
        private bool _hitActive;
        private bool _isNormalMonster;
        private readonly Dictionary<AudioClip[], int> _previousSoundIndices = new Dictionary<AudioClip[], int>();
        private int _previousHitSound = -1;
        private bool _deathSoundPlayed;
        private bool _explosionSoundPlayed;
        private MaterialPropertyBlock _outlineProperties;
        private Sprite _outlinedSprite;
        private Vector2 _chargeDirection;
        private Vector3 _chargeOffset;
        private float _aimCueLength;
        private float _chargeStartedAt;
        private float _referenceWidth;
        private bool _charging;
        private bool _rangedCharge;
        private bool _ready;
        private bool _isMelee;
        private bool _isKamikaze;
        private bool _fusing;
        private float _fuseRadius;
        private float _fuseProgress;
        private float _nextChargeParticles;
        private float _nextFuseTick;
        private int _previousExplosion = -1;
        private int _previousMeleeSlash = -1;
        private int _previousRangeShot = -1;
        public bool IsConfigured => enabled && _profile != null;
        public float TrailInterval => _profile != null ? _profile.TrailInterval : 0.035f;

        private void Awake()
        {
            _outlineProperties = new MaterialPropertyBlock();
            _hitProperties = new MaterialPropertyBlock();
            _isMelee = GetComponent<MeleeMonster>() != null;
            _isKamikaze = GetComponent<KamikazeMonster>() != null;
            _isNormalMonster = _isMelee || _isKamikaze || GetComponent<RangeMonster>() != null;
            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponent<SpriteRenderer>();
                if (_bodyRenderer == null)
                    _bodyRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            ConfigureBodyMaterial();
            KeepBodyOpaque();
        }

        private void ConfigureBodyMaterial()
        {
            if (!_isNormalMonster || _bodyRenderer == null)
                return;
            _bodyMaterial = Resources.Load<Material>("Monster/NormalGameFeel/NormalMonsterOpaque");
            if (_bodyMaterial == null || _bodyMaterial.shader == null || !_bodyMaterial.shader.isSupported)
                return;
            // Unity의 2D Unlit 재질을 공유합니다. 효과 재질로 본체를 대체하지 않습니다.
            _bodyRenderer.sharedMaterial = _bodyMaterial;
        }

        private void KeepBodyOpaque()
        {
            if (!_isNormalMonster || _bodyRenderer == null)
                return;
            Color color = _bodyRenderer.color;
            color.a = 1f;
            _bodyRenderer.color = color;
            _bodyRenderer.forceRenderingOff = false;
        }

        private void OnEnable()
        {
            if (_isNormalMonster && _bodyRenderer != null && _bodyMaterial != null)
                _bodyRenderer.sharedMaterial = _bodyMaterial;
            KeepBodyOpaque();
        }

        private void Start()
        {
            EnsureReady();
        }

        private bool EnsureReady()
        {
            if (_ready)
            {
                return IsConfigured && _bodyRenderer != null && _effects != null;
            }

            if (!IsConfigured || _bodyRenderer == null || _bodyRenderer.sprite == null)
            {
                return false;
            }

            _effects = NormalMonsterFeelEffects.ForScene(gameObject.scene);
            _neon = MonsterNeonVfx.ForScene(gameObject.scene);
            _outline = _effects.CreatePersistentRenderer(name + "_PixelRim", true);
            _chargeGlow = _effects.CreatePersistentRenderer(name + "_ChargeGlow");
            _chargeCore = _effects.CreatePersistentRenderer(name + "_ChargeCore");
            _attackCue = new MonsterAttackCue(transform, _bodyRenderer);
            _chargeGlow.sprite = _effects.Shape(NormalFeelShape.Glow);
            _chargeCore.sprite = _effects.Shape(NormalFeelShape.Star);
            _referenceWidth = Mathf.Max(0.3f, _bodyRenderer.bounds.size.x);
            _ready = true;
            return true;
        }

        // TakeDamage에서 실제로 피해가 적용된 경우에만 호출합니다. 마지막 타격도 포함합니다.
        public bool ReceiveHit(Vector2 direction, bool lethal)
        {
            if (!_isNormalMonster || Time.timeScale <= 0f || !EnsureReady())
            {
                return false;
            }

            if (_hitRenderer == null && _effects.HasHitMaterial)
            {
                _hitRenderer = _effects.CreateHitRenderer(name + "_HitVisual");
            }

            _hitDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
            _hitAge = 0f;
            _hitActive = _hitRenderer != null;
            // 피격 복사본이 준비되지 않아도 실제 몬스터 본체는 계속 표시합니다.
            KeepBodyOpaque();
            RefreshHitVisual();

            float width = Mathf.Clamp(_referenceWidth, 0.45f, 1.8f);
            Vector3 point = _bodyRenderer.bounds.center;
            Color accent = _isKamikaze ? _profile.KamikazeColor : _isMelee ? _profile.MeleeColor : _profile.RangeColor;
            int sortingLayer = _bodyRenderer.sortingLayerID;
            int order = _bodyRenderer.sortingOrder;
            _effects.MechanicalHit(point, _hitDirection, width, accent, _profile, gameObject.layer, sortingLayer, order + 4, lethal);
            Emit(_effects.Shape(NormalFeelShape.Star), point, Random.Range(0f, 90f), new Vector2(width * 0.32f, width * 0.2f), Vector2.one * width * 0.04f, _hitDirection * 0.12f, new Color(1f, 0.9f, 0.72f, 0.85f), 0.055f, 6, false, false, true);
            Ring(point, new Color(1f, 0.4f, 0.18f, 0.65f), width * 0.15f, width * 0.72f, 0.11f, true);
            AudioClip[] sounds = _profile.HitSounds;
            if (sounds == null || sounds.Length == 0)
            {
                sounds = _profile.MeleeImpactSounds;
            }

            int index = Choose(sounds, _previousHitSound);
            _previousHitSound = index;
            PlayIndex(sounds, index, point, _profile.HitVolume * (lethal ? 1f : 0.85f));
            NormalMonsterCameraShake.Request(point, _profile.HitCameraShake * (lethal ? 1.3f : 1f), 0.075f);
            // 복사본 재질이 없는 경우 기존 NormalMonster의 붉은 피격 표시를 허용합니다.
            return _hitActive;
        }

        public void PlayDeathSound()
        {
            if (_deathSoundPlayed || _explosionSoundPlayed || !_isNormalMonster || !IsConfigured || Time.timeScale <= 0f)
                return;
            _deathSoundPlayed = true;
            if (_effects == null)
            {
                _effects = NormalMonsterFeelEffects.ForScene(gameObject.scene);
            }

            string cue = _isKamikaze ? "KamikazeDeath" : _isMelee ? "MeleeDeath" : "RangeDeath";
            AudioClip[] clips = MonsterCombatAudio.Normal(cue, _profile.HitSounds);
            Play(clips, transform.position, _profile.ImpactVolume);
        }

        private void RefreshHitVisual()
        {
            if (!_hitActive)
            {
                return;
            }

            float duration = Mathf.Max(_profile.HitPopDuration, _profile.HitFlashHold + _profile.HitFlashFade);
            if (_bodyRenderer == null || _hitRenderer == null || _hitAge >= duration || !_bodyRenderer.enabled || !_bodyRenderer.gameObject.activeInHierarchy || _bodyRenderer.sprite == null)
            {
                EndHitVisual();
                return;
            }

            _hitRenderer.sprite = _bodyRenderer.sprite;
            _hitRenderer.flipX = _bodyRenderer.flipX;
            _hitRenderer.flipY = _bodyRenderer.flipY;
            Color hitColor = _bodyRenderer.color;
            hitColor.a = 1f;
            _hitRenderer.color = hitColor;
            _hitRenderer.gameObject.layer = _bodyRenderer.gameObject.layer;
            _hitRenderer.sortingLayerID = _bodyRenderer.sortingLayerID;
            _hitRenderer.sortingOrder = _bodyRenderer.sortingOrder + 1;
            _hitRenderer.spriteSortPoint = _bodyRenderer.spriteSortPoint;
            float scale = NormalMonsterHitMath.Scale(_hitAge, _profile.HitPopDuration, _profile.HitPopAmount);
            float recoil = NormalMonsterHitMath.Recoil(_hitAge, _profile.HitPopDuration);
            float width = Mathf.Clamp(_referenceWidth, 0.45f, 1.8f);
            Vector2 sideways = new Vector2(-_hitDirection.y, _hitDirection.x);
            Vector2 offset = (_hitDirection * _profile.HitRecoil + sideways * Mathf.Sin(_hitAge * 120f) * _profile.HitRecoil * 0.2f) * width * recoil;
            _hitRenderer.transform.position = _bodyRenderer.transform.position + new Vector3(offset.x, offset.y, 0f);
            _hitRenderer.transform.rotation = _bodyRenderer.transform.rotation;
            Vector3 originalScale = _bodyRenderer.transform.lossyScale;
            _hitRenderer.transform.localScale = new Vector3(originalScale.x * scale, originalScale.y * scale, originalScale.z);
            _hitProperties.SetFloat(HitFlashAmountId, NormalMonsterHitMath.Flash(_hitAge, _profile.HitFlashHold, _profile.HitFlashFade));
            _hitRenderer.SetPropertyBlock(_hitProperties);
            _hitRenderer.enabled = true;
        }

        private void EndHitVisual()
        {
            _hitActive = false;
            if (_hitRenderer != null)
            {
                _hitRenderer.enabled = false;
            }
        }

        public void BeginMeleeAttack(Vector2 direction, float range)
        {
            if (!EnsureReady())
            {
                return;
            }

            BeginCharge(direction, transform.position + new Vector3(direction.x, direction.y, 0f) * range * 0.35f, false);
            _aimCueLength = Mathf.Max(0.3f, range);
            Play(_profile.MeleeChargeSounds, transform.position, _profile.AttackVolume * 0.35f);
        }

        public void BeginRangeAttack(Vector2 direction, Vector3 muzzle)
        {
            if (!EnsureReady())
            {
                return;
            }

            BeginCharge(direction, muzzle, true);
            _aimCueLength = 0.85f;
            Play(_profile.RangeChargeSounds, muzzle, _profile.AttackVolume * 0.45f);
        }

        private void BeginCharge(Vector2 direction, Vector3 position, bool ranged)
        {
            _charging = true;
            _rangedCharge = ranged;
            _chargeDirection = direction.normalized;
            _chargeOffset = position - transform.position;
            _chargeStartedAt = Time.time;
            _nextChargeParticles = 0f;
        }

        public void BeginKamikazeFuse(float damageRadius)
        {
            if (!EnsureReady())
                return;
            BeginCharge(Vector2.up, transform.position, false);
            _fusing = true;
            _fuseProgress = 0f;
            _fuseRadius = Mathf.Max(0.1f, damageRadius);
            _nextFuseTick = Time.time + 0.28f;
            Play(_profile.KamikazeChargeSounds, transform.position, _profile.AttackVolume * 0.55f);
        }

        public void UpdateKamikazeFuse(float progress)
        {
            if (_fusing)
                _fuseProgress = Mathf.Clamp01(progress);
        }

        public void KamikazeExplosion(Vector3 point, float radius)
        {
            if (!EnsureReady())
                return;
            CancelAttackVisual();
            Color color = _profile.KamikazeColor;
            if (_neon != null)
                _neon.Burst(point, Mathf.Max(0.2f, radius * 0.7f), color, _bodyRenderer.sortingLayerID, _bodyRenderer.sortingOrder + 3, 38, true);
            Ring(point, color, radius * 0.25f, radius * 2.1f, 0.35f);
            Glow(point, Color.Lerp(color, Color.white, 0.4f), radius * 0.7f, 0.1f);
            _explosionSoundPlayed = true;
            int index = Choose(_profile.KamikazeExplosionSounds, _previousExplosion);
            _previousExplosion = index;
            PlayIndex(_profile.KamikazeExplosionSounds, index, point, Mathf.Min(1f, _profile.ImpactVolume * 1.1f));
            NormalMonsterCameraShake.Request(point, _profile.CameraShake * 1.65f, 0.24f);
        }

        public void CancelAttackVisual()
        {
            _charging = false;
            _fusing = false;
            if (_attackCue != null)
            {
                _attackCue.Hide();
            }
            if (_chargeGlow != null)
            {
                _chargeGlow.enabled = false;
            }

            if (_chargeCore != null)
            {
                _chargeCore.enabled = false;
            }
        }

        public void MeleeRelease(Vector2 direction, float range, float attackAngle)
        {
            if (!EnsureReady())
            {
                return;
            }

            CancelAttackVisual();
            Color color = _profile.MeleeColor;
            float rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float width = Mathf.Max(0.2f, range) * 2f / 0.95f;
            Sprite slash = _effects.Slash(attackAngle);
            Emit(slash, transform.position, rotation, new Vector2(width * 0.92f, width * 0.92f), Vector2.one * width, Vector2.zero, color, 0.17f, 4);
            Emit(slash, transform.position, rotation, Vector2.one * width * 0.92f, Vector2.one * width * 0.97f, Vector2.zero, Color.white, 0.045f, 5);
            Color trail = color;
            trail.a = 0.2f;
            Emit(slash, transform.position, rotation - 7f, Vector2.one * width * 0.92f, Vector2.one * width, -direction * 0.4f, trail, 0.16f, 3);
            Vector3 strike = transform.position + new Vector3(direction.x, direction.y, 0f) * range * 0.73f;
            if (_neon != null)
                _neon.Slash(transform.position, range * 0.95f, rotation, color, _bodyRenderer.sortingLayerID, _bodyRenderer.sortingOrder + 3);
            Ring(strike, color, 0.16f, 0.65f * _profile.DecorationScale, 0.16f);
            Burst(strike, direction, color, _profile.MeleeSparks, 0.85f, false);
            Afterimages(direction, color, 3);
            int index = Choose(_profile.MeleeSlashSounds, _previousMeleeSlash);
            _previousMeleeSlash = index;
            PlayIndex(_profile.MeleeSlashSounds, index, strike, _profile.AttackVolume);
            NormalMonsterCameraShake.Request(strike, _profile.CameraShake * 0.3f, 0.09f);
        }

        public void MeleeImpact(Vector3 point, Vector2 direction)
        {
            if (!EnsureReady())
            {
                return;
            }

            Impact(point, direction, _profile.MeleeColor, 1f);
            Play(_profile.MeleeImpactSounds, point, _profile.ImpactVolume);
        }

        public void BeginRangePatternAttack(Vector3 muzzle)
        {
            if (!EnsureReady())
                return;
            CancelAttackVisual();
            Play(_profile.RangeChargeSounds, muzzle, _profile.AttackVolume * 0.35f);
        }

        public void RangePatternRelease(Vector3 muzzle, Vector2 direction, bool firstWave)
        {
            if (!EnsureReady())
                return;
            CancelAttackVisual();
            int index = Choose(_profile.RangeShotSounds, _previousRangeShot);
            _previousRangeShot = index;
            PlayIndex(_profile.RangeShotSounds, index, muzzle, _profile.AttackVolume * 0.72f);
            if (firstWave)
            {
                NormalMonsterCameraShake.Request(muzzle, _profile.CameraShake * 0.15f, 0.06f);
            }
        }

        public void RangePatternImpact(Vector3 point, bool fullImpact)
        {
            if (!EnsureReady())
                return;
            Play(_profile.RangeImpactSounds, point, _profile.ImpactVolume * (fullImpact ? 0.7f : 0.25f));
        }

        public void RangeRelease(Vector3 muzzle, Vector2 direction)
        {
            if (!EnsureReady())
            {
                return;
            }

            CancelAttackVisual();
            Color color = _profile.RangeColor;
            if (_neon != null)
                _neon.PlasmaShot(muzzle, direction, 0.45f * _profile.DecorationScale, color, _bodyRenderer.sortingLayerID, _bodyRenderer.sortingOrder + 3);
            Glow(muzzle, color, 0.8f * _profile.DecorationScale, 0.085f);
            Emit(_effects.Shape(NormalFeelShape.Star), muzzle, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, new Vector2(0.6f, 0.35f), new Vector2(0.9f, 0.12f), direction * 1.4f, Color.white, 0.085f, 6);
            Ring(muzzle, color, 0.16f, 0.8f * _profile.DecorationScale, 0.18f);
            Burst(muzzle, direction, color, 14, 0.6f, false);
            int index = Choose(_profile.RangeShotSounds, _previousRangeShot);
            _previousRangeShot = index;
            PlayIndex(_profile.RangeShotSounds, index, muzzle, _profile.AttackVolume);
            NormalMonsterCameraShake.Request(muzzle, _profile.CameraShake * 0.2f, 0.08f);
        }

        public void ProjectileTrail(SpriteRenderer source, Vector3 position, Vector2 direction)
        {
            if (!EnsureReady())
            {
                return;
            }

            Color color = _profile.RangeColor;
            color.a = 0.28f;
            Vector2 size = new Vector2(0.2f, 0.12f);
            if (source != null && source.sprite != null)
            {
                source.sortingLayerID = _bodyRenderer.sortingLayerID;
                source.sortingOrder = Mathf.Max(source.sortingOrder,
                    _bodyRenderer.sortingOrder + MonsterVfxReadability.ProjectileBodyOrder);
                Vector3 bounds = source.sprite.bounds.size;
                Vector3 scale = source.transform.lossyScale;
                size = new Vector2(bounds.x * Mathf.Abs(scale.x), bounds.y * Mathf.Abs(scale.y));
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (_neon != null)
                _neon.Trail(position, direction, Mathf.Max(0.18f, size.x * 1.3f), _profile.RangeColor, _bodyRenderer.sortingLayerID, _bodyRenderer.sortingOrder + 3);
            if (source != null && source.sprite != null)
            {
                Emit(source.sprite, position - (Vector3)(direction * size.x * 0.35f), source.transform.eulerAngles.z,
                    size, size * 0.25f, -direction * 0.2f, color, 0.095f, 2, source.flipX, source.flipY);
            }

            Emit(_effects.Shape(NormalFeelShape.Star), position - (Vector3)(direction * size.x * 0.4f), angle,
                new Vector2(Mathf.Max(0.12f, size.x * 0.7f), 0.045f), new Vector2(0.06f, 0.02f),
                -direction * 0.6f, color, 0.065f, 3);
            Glow(position, color, Mathf.Max(0.2f, size.x * 1.5f), 0.07f);
        }

        public void ProjectileImpact(Vector3 point, Vector2 direction, bool fullImpact)
        {
            if (!EnsureReady())
            {
                return;
            }

            float intensity = fullImpact ? 1f : 0.35f;
            Impact(point, direction, _profile.RangeColor, intensity);
            Play(_profile.RangeImpactSounds, point, _profile.ImpactVolume * intensity);
        }

        private void Impact(Vector3 point, Vector2 direction, Color color, float intensity)
        {
            float scale = _profile.DecorationScale * intensity;
            Glow(point, color, 1.35f * scale, 0.12f);
            Ring(point, color, 0.2f * scale, 1.5f * scale, 0.23f);
            Color accent = _profile.AccentColor;
            accent.a = 0.7f;
            Ring(point, accent, 0.15f * scale, 0.9f * scale, 0.13f);
            Emit(_effects.Shape(NormalFeelShape.Star), point, Random.Range(0f, 90f), Vector2.one * 0.68f * scale, Vector2.one * 0.1f, Vector2.zero, Color.white, 0.08f, 7);
            Burst(point, direction, color, Mathf.Max(4, Mathf.RoundToInt(_profile.ImpactSparks * intensity)), scale, true);
            NormalMonsterCameraShake.Request(point, _profile.CameraShake * intensity, 0.14f);
        }

        private void Burst(Vector3 position, Vector2 direction, Color color, int count, float scale, bool fullCircle)
        {
            if (_neon != null)
            {
                _neon.Burst(position, Mathf.Max(0.1f, scale * (fullCircle ? 0.4f : 0.24f)), color, _bodyRenderer.sortingLayerID, _bodyRenderer.sortingOrder + 3, count);
                return;
            }

            float heading = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            for (int i = 0; i < count; i++)
            {
                float angle = fullCircle ? Random.Range(0f, 360f) : heading + Random.Range(-75f, 75f);
                float radians = angle * Mathf.Deg2Rad;
                Vector2 velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * Random.Range(2.5f, 6.5f) * scale;
                Color spark = i % 4 == 0 ? Color.white : (i % 5 == 0 ? _profile.AccentColor : color);
                float length = Random.Range(0.08f, 0.2f) * Mathf.Max(0.4f, scale);
                Emit(_effects.Shape(NormalFeelShape.Pixel), position, angle, new Vector2(length, 0.025f * Mathf.Max(0.5f, scale)), new Vector2(length * 0.15f, 0.01f), velocity, spark, Random.Range(0.12f, 0.28f), 5);
            }
        }

        private void Afterimages(Vector2 direction, Color color, int count)
        {
            if (_bodyRenderer == null || _bodyRenderer.sprite == null)
            {
                return;
            }

            Vector2 size = _bodyRenderer.bounds.size;
            for (int i = 0; i < count; i++)
            {
                color.a = 0.23f / (i + 1f);
                Vector3 position = _bodyRenderer.transform.position - new Vector3(direction.x, direction.y, 0f) * (i + 1) * 0.06f;
                Emit(_bodyRenderer.sprite, position, _bodyRenderer.transform.eulerAngles.z, size, size * (1f + 0.025f * (i + 1)), -direction * 0.18f, color, 0.18f + i * 0.045f, -1, _bodyRenderer.flipX, _bodyRenderer.flipY);
            }
        }

        private void Ring(Vector3 point, Color color, float start, float end, float lifetime, bool hitGlow = false)
        {
            Emit(_effects.Shape(NormalFeelShape.Ring), point, 0f, Vector2.one * start, Vector2.one * end, Vector2.zero, color, lifetime, 3, false, false, hitGlow);
        }

        private void Glow(Vector3 point, Color color, float size, float lifetime)
        {
            color.a = Mathf.Min(color.a * 0.4f, 0.22f);
            Emit(_effects.Shape(NormalFeelShape.Glow), point, 0f, Vector2.one * size, Vector2.one * size * 1.1f, Vector2.zero, color, lifetime, 1);
        }

        private void Emit(Sprite sprite, Vector3 position, float rotation, Vector2 startSize, Vector2 endSize, Vector2 velocity, Color color, float lifetime, int order, bool flipX = false, bool flipY = false, bool hitGlow = false)
        {
            _effects.Emit(sprite, position, rotation, startSize, endSize, velocity, color, lifetime, gameObject.layer, _bodyRenderer.sortingLayerID, _bodyRenderer.sortingOrder + order, flipX, flipY, false, hitGlow);
        }

        private void Play(AudioClip[] sounds, Vector3 position, float volume)
        {
            int previous = sounds != null && _previousSoundIndices.TryGetValue(sounds, out int last) ? last : -1;
            int index = Choose(sounds, previous);
            if (sounds != null && index >= 0)
            {
                _previousSoundIndices[sounds] = index;
            }

            PlayIndex(sounds, index, position, volume);
        }

        private void PlayIndex(AudioClip[] sounds, int index, Vector3 position, float volume)
        {
            if (sounds != null && index >= 0 && index < sounds.Length)
            {
                _effects.PlaySound(sounds[index], position, volume, _profile);
            }
        }

        private static int Choose(AudioClip[] sounds, int previous)
        {
            if (sounds == null || sounds.Length == 0)
            {
                return -1;
            }

            int start = Random.Range(0, sounds.Length);
            if (sounds.Length > 1 && start == previous)
            {
                start = (start + 1) % sounds.Length;
            }

            for (int i = 0; i < sounds.Length; i++)
            {
                int index = (start + i) % sounds.Length;
                if (sounds[index] != null)
                {
                    return index;
                }
            }

            return -1;
        }

        private void LateUpdate()
        {
            KeepBodyOpaque();
            if (!EnsureReady())
            {
                EndHitVisual();
                CancelAttackVisual();
                return;
            }

            if (_hitActive)
            {
                _hitAge += Time.deltaTime;
                RefreshHitVisual();
            }

            RefreshOutline();
            if (!_bodyRenderer.enabled || !_bodyRenderer.gameObject.activeInHierarchy)
            {
                CancelAttackVisual();
            }
            if (!_charging || Time.timeScale <= 0f)
            {
                return;
            }

            Vector3 position = transform.position + _chargeOffset;
            Color color = _fusing ? _profile.KamikazeColor : _rangedCharge ? _profile.RangeColor : _profile.MeleeColor;
            float pulse = 0.5f + 0.5f * Mathf.Sin((Time.time - _chargeStartedAt) * (_fusing ? 8f + _fuseProgress * 12f : 10f));
            float width = Mathf.Clamp(_referenceWidth * 0.45f, 0.3f, 0.8f) * (0.9f + pulse * 0.15f);
            UpdateChargeRenderer(_chargeGlow, position, MonsterVfxReadability.WithOpacity(color, 0.2f), width, 2);
            UpdateChargeRenderer(_chargeCore, position, Color.Lerp(color, Color.white, 0.35f), width * 0.22f, 3);
            if (_neon != null && Time.time >= _nextChargeParticles)
            {
                _nextChargeParticles = Time.time + 0.1f;
                _neon.Charge(position, width * 0.6f, _fusing ? _fuseProgress : 0.5f, color, _bodyRenderer.sortingLayerID, _bodyRenderer.sortingOrder + 2);
            }

            if (_fusing)
            {
                _attackCue.Circle(transform.position, _fuseRadius, _fuseProgress);
                if (Time.time >= _nextFuseTick)
                {
                    _nextFuseTick = Time.time + Mathf.Lerp(0.38f, 0.14f, _fuseProgress);
                    Play(_profile.KamikazeTickSounds, transform.position, _profile.AttackVolume * 0.4f);
                }
            }
            else
            {
                _attackCue.Direction(_rangedCharge ? (Vector2)position : (Vector2)transform.position,
                    _chargeDirection, _aimCueLength, color);
            }
        }

        private void UpdateChargeRenderer(SpriteRenderer renderer, Vector3 position, Color color, float width, int order)
        {
            renderer.gameObject.layer = gameObject.layer;
            renderer.sortingLayerID = _bodyRenderer.sortingLayerID;
            renderer.sortingOrder = _bodyRenderer.sortingOrder + order;
            renderer.transform.position = position;
            renderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_chargeDirection.y, _chargeDirection.x) * Mathf.Rad2Deg);
            renderer.color = color;
            NormalMonsterFeelEffects.ApplySize(renderer, Vector2.one * width);
            renderer.enabled = _bodyRenderer.enabled;
        }

        private void RefreshOutline()
        {
            bool visible = !_hitActive && !_bodyRenderer.forceRenderingOff && _effects.HasOutlineMaterial && _profile.OutlineOpacity > 0f && _bodyRenderer.enabled && _bodyRenderer.gameObject.activeInHierarchy && _bodyRenderer.sprite != null;
            _outline.enabled = visible;
            if (!visible)
            {
                return;
            }

            _outline.gameObject.layer = gameObject.layer;
            _outline.sprite = _bodyRenderer.sprite;
            _outline.flipX = _bodyRenderer.flipX;
            _outline.flipY = _bodyRenderer.flipY;
            _outline.sortingLayerID = _bodyRenderer.sortingLayerID;
            _outline.sortingOrder = _bodyRenderer.sortingOrder + 1;
            _outline.transform.position = _bodyRenderer.transform.position;
            _outline.transform.rotation = _bodyRenderer.transform.rotation;
            _outline.transform.localScale = _bodyRenderer.transform.lossyScale;
            Color color = _isKamikaze ? _profile.KamikazeColor : _isMelee ? _profile.MeleeColor : _profile.RangeColor;
            color.a = _profile.OutlineOpacity;
            _outline.color = color;
            if (_outlinedSprite == _bodyRenderer.sprite)
            {
                return;
            }

            _outlinedSprite = _bodyRenderer.sprite;
            Vector2 minimum = Vector2.one;
            Vector2 maximum = Vector2.zero;
            foreach (Vector2 uv in _outlinedSprite.uv)
            {
                minimum = Vector2.Min(minimum, uv);
                maximum = Vector2.Max(maximum, uv);
            }

            _outlineProperties.SetVector(UvBoundsId, new Vector4(minimum.x, minimum.y, maximum.x, maximum.y));
            _outline.SetPropertyBlock(_outlineProperties);
        }

        private void OnDisable()
        {
            EndHitVisual();
            CancelAttackVisual();
            KeepBodyOpaque();
            if (_outline != null)
            {
                _outline.enabled = false;
            }
        }

        private void OnDestroy()
        {
            EndHitVisual();
            DestroyOwnedRenderer(_hitRenderer);
            DestroyOwnedRenderer(_outline);
            DestroyOwnedRenderer(_chargeGlow);
            DestroyOwnedRenderer(_chargeCore);
            if (_attackCue != null)
            {
                _attackCue.Dispose();
            }
        }

        private static void DestroyOwnedRenderer(SpriteRenderer renderer)
        {
            if (renderer != null)
            {
                Destroy(renderer.gameObject);
            }
        }
    }
}
