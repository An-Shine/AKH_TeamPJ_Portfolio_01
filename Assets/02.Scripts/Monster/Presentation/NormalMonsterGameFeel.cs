using UnityEngine;

namespace _02.Scripts.Monster.Presentation
{
    [DefaultExecutionOrder(1100)]
    [DisallowMultipleComponent]
    [AddComponentMenu("Monster/Normal Monster Game Feel")]
    public sealed class NormalMonsterGameFeel : MonoBehaviour
    {
        private static readonly int UvBoundsId = Shader.PropertyToID("_UvBounds");
        [SerializeField] private NormalMonsterFeelProfile _profile;
        [SerializeField] private SpriteRenderer _bodyRenderer;
        private NormalMonsterFeelEffects _effects;
        private SpriteRenderer _outline;
        private SpriteRenderer _chargeGlow;
        private SpriteRenderer _chargeCore;
        private MaterialPropertyBlock _outlineProperties;
        private Sprite _outlinedSprite;
        private Vector2 _chargeDirection;
        private Vector3 _chargeOffset;
        private float _chargeStartedAt;
        private float _referenceWidth;
        private bool _charging;
        private bool _rangedCharge;
        private bool _ready;
        private bool _isMelee;
        private int _previousMeleeSlash = -1;
        private int _previousRangeShot = -1;
        public bool IsConfigured => enabled && _profile != null;
        public float TrailInterval => _profile != null ? _profile.TrailInterval : 0.035f;
        private void Awake()
        {
            _outlineProperties = new MaterialPropertyBlock();
            _isMelee = GetComponent<MeleeMonster>() != null;
            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
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
            _outline = _effects.CreatePersistentRenderer(name + "_PixelRim", true);
            _chargeGlow = _effects.CreatePersistentRenderer(name + "_ChargeGlow");
            _chargeCore = _effects.CreatePersistentRenderer(name + "_ChargeCore");
            _chargeGlow.sprite = _effects.Shape(NormalFeelShape.Glow);
            _chargeCore.sprite = _effects.Shape(NormalFeelShape.Star);
            _referenceWidth = Mathf.Max(0.3f, _bodyRenderer.bounds.size.x);
            _ready = true;
            return true;
        }

        public void BeginMeleeAttack(Vector2 direction, float range)
        {
            if (!EnsureReady())
            {
                return;
            }

            BeginCharge(direction, transform.position + new Vector3(direction.x, direction.y, 0f) * range * 0.35f, false);
            Play(_profile.MeleeChargeSounds, transform.position, _profile.AttackVolume * 0.35f);
        }

        public void BeginRangeAttack(Vector2 direction, Vector3 muzzle)
        {
            if (!EnsureReady())
            {
                return;
            }

            BeginCharge(direction, muzzle, true);
            Play(_profile.RangeChargeSounds, muzzle, _profile.AttackVolume * 0.45f);
        }

        private void BeginCharge(Vector2 direction, Vector3 position, bool ranged)
        {
            _charging = true;
            _rangedCharge = ranged;
            _chargeDirection = direction.normalized;
            _chargeOffset = position - transform.position;
            _chargeStartedAt = Time.time;
        }

        public void CancelAttackVisual()
        {
            _charging = false;
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
            Emit(slash, transform.position, rotation, Vector2.one * width * 0.92f, Vector2.one * width * 0.97f, Vector2.zero, Color.white, 0.065f, 5);
            Color trail = color;
            trail.a = 0.32f;
            Emit(slash, transform.position, rotation - 7f, Vector2.one * width * 0.92f, Vector2.one * width, -direction * 0.4f, trail, 0.24f, 3);
            Vector3 strike = transform.position + new Vector3(direction.x, direction.y, 0f) * range * 0.73f;
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

        public void RangeRelease(Vector3 muzzle, Vector2 direction)
        {
            if (!EnsureReady())
            {
                return;
            }

            CancelAttackVisual();
            Color color = _profile.RangeColor;
            Glow(muzzle, color, 1.1f * _profile.DecorationScale, 0.12f);
            Emit(_effects.Shape(NormalFeelShape.Star), muzzle, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, new Vector2(0.6f, 0.35f), new Vector2(0.9f, 0.12f), direction * 1.4f, Color.white, 0.085f, 6);
            Ring(muzzle, color, 0.16f, 0.8f * _profile.DecorationScale, 0.18f);
            Burst(muzzle, direction, color, 14, 0.6f, false);
            Afterimages(direction, color, 2);
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
            color.a = 0.55f;
            Vector2 size = new Vector2(0.2f, 0.12f);
            if (source != null && source.sprite != null)
            {
                Vector3 bounds = source.sprite.bounds.size;
                Vector3 scale = source.transform.lossyScale;
                size = new Vector2(bounds.x * Mathf.Abs(scale.x), bounds.y * Mathf.Abs(scale.y));
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (source != null && source.sprite != null)
            {
                Emit(source.sprite, position, source.transform.eulerAngles.z, size * 1.25f, size * 0.35f, -direction * 0.2f, color, 0.14f, 2, source.flipX, source.flipY);
            }

            Emit(_effects.Shape(NormalFeelShape.Star), position, angle, new Vector2(Mathf.Max(0.15f, size.x), 0.065f), new Vector2(0.08f, 0.02f), -direction * 0.6f, Color.white, 0.08f, 3);
            Glow(position, color, Mathf.Max(0.24f, size.x * 2.3f), 0.09f);
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
            Glow(point, color, 1.8f * scale, 0.21f);
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

        private void Ring(Vector3 point, Color color, float start, float end, float lifetime)
        {
            Emit(_effects.Shape(NormalFeelShape.Ring), point, 0f, Vector2.one * start, Vector2.one * end, Vector2.zero, color, lifetime, 3);
        }

        private void Glow(Vector3 point, Color color, float size, float lifetime)
        {
            color.a *= 0.55f;
            Emit(_effects.Shape(NormalFeelShape.Glow), point, 0f, Vector2.one * size, Vector2.one * size * 1.1f, Vector2.zero, color, lifetime, 1);
        }

        private void Emit(Sprite sprite, Vector3 position, float rotation, Vector2 startSize, Vector2 endSize, Vector2 velocity, Color color, float lifetime, int order, bool flipX = false, bool flipY = false)
        {
            _effects.Emit(sprite, position, rotation, startSize, endSize, velocity, color, lifetime, gameObject.layer, _bodyRenderer.sortingLayerID, _bodyRenderer.sortingOrder + order, flipX, flipY);
        }

        private void Play(AudioClip[] sounds, Vector3 position, float volume)
        {
            PlayIndex(sounds, Choose(sounds, -1), position, volume);
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
            if (!EnsureReady())
            {
                return;
            }

            RefreshOutline();
            if (!_charging || Time.timeScale <= 0f)
            {
                return;
            }

            Vector3 position = transform.position + _chargeOffset;
            Color color = _rangedCharge ? _profile.RangeColor : _profile.MeleeColor;
            float pulse = 0.5f + 0.5f * Mathf.Sin((Time.time - _chargeStartedAt) * 47f);
            float width = Mathf.Clamp(_referenceWidth * 0.5f, 0.3f, 0.9f) * (0.8f + pulse * 0.4f);
            UpdateChargeRenderer(_chargeGlow, position, color, width, 2);
            UpdateChargeRenderer(_chargeCore, position, Color.Lerp(color, Color.white, 0.65f), width * 0.28f, 3);
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
            bool visible = _effects.HasOutlineMaterial && _profile.OutlineOpacity > 0f && _bodyRenderer.enabled && _bodyRenderer.gameObject.activeInHierarchy && _bodyRenderer.sprite != null;
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
            Color color = _isMelee ? _profile.MeleeColor : _profile.RangeColor;
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
            CancelAttackVisual();
            if (_outline != null)
            {
                _outline.enabled = false;
            }
        }

        private void OnDestroy()
        {
            DestroyOwnedRenderer(_outline);
            DestroyOwnedRenderer(_chargeGlow);
            DestroyOwnedRenderer(_chargeCore);
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
