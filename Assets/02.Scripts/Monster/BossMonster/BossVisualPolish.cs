using System.Collections.Generic;
using UnityEngine;
using _02.Scripts.Monster.Presentation;

namespace _02.Scripts.Monster.BossMonster
{
    [DefaultExecutionOrder(1250)]
    [DisallowMultipleComponent]
    public sealed class BossVisualPolish : MonoBehaviour
    {
        [SerializeField]
        private SpriteRenderer _body;
        private global::BossMonster _owner;
        private Material _bodyMaterial;
        private Material _rimMaterial;
        private SpriteRenderer _rim;
        private MaterialPropertyBlock _properties;
        private MaterialPropertyBlock _rimProperties;
        private Sprite _current;
        private Sprite _previous;
        private Vector3 _restPosition;
        private Vector3 _restScale;
        private Vector2 _impulse;
        private Color _castColor = new Color(0.1f, 0.88f, 1f);
        private float _blendAge;
        private float _blendDuration = 0.026f;
        private bool _blendPending;
        private bool _blending;
        private float _blendPendingUntil;
        private struct FrameData
        {
            public Vector4 Uv;
            public Vector4 Local;
        }

        private readonly Dictionary<Sprite, FrameData> _frames = new Dictionary<Sprite, FrameData>();
        private float _charge;
        private float _punch;
        private float _nextAmbient;
        private bool _casting;
        private MonsterNeonVfx _effects;
        private void Start()
        {
            _owner = GetComponent<global::BossMonster>();
            if (_body == null)
                _body = GetComponentInChildren<SpriteRenderer>();
            if (_body == null)
            {
                enabled = false;
                return;
            }

            _restPosition = _body.transform.localPosition;
            _restScale = _body.transform.localScale;
            _properties = new MaterialPropertyBlock();
            _rimProperties = new MaterialPropertyBlock();
            Shader body = Resources.Load<Shader>("Monster/NeonVfx/BossNeonBody");
            if (body != null)
            {
                _bodyMaterial = new Material(body)
                {
                    hideFlags = HideFlags.DontSave
                };
                _body.sharedMaterial = _bodyMaterial;
            }

            Shader rim = Resources.Load<Shader>("Monster/NormalGameFeel/NormalFeelPixelRim");
            if (rim != null)
            {
                _rimMaterial = new Material(rim)
                {
                    hideFlags = HideFlags.DontSave
                };
                _rim = CreateRenderer("BossNeonRim", _rimMaterial);
            }

            _effects = MonsterNeonVfx.ForScene(gameObject.scene);
            _current = _body.sprite;
            _previous = _current;
        }

        private SpriteRenderer CreateRenderer(string objectName, Material material)
        {
            GameObject child = new GameObject(objectName, typeof(SpriteRenderer));
            child.transform.SetParent(transform, false);
            child.layer = gameObject.layer;
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            return renderer;
        }

        public void Cast(Color color, float charge)
        {
            _casting = true;
            _castColor = color;
            _charge = Mathf.Clamp01(charge);
        }

        public void EndCast()
        {
            _casting = false;
            _charge = 0f;
        }

        public void Pulse(Vector2 direction, float strength = 1f)
        {
            _punch = Mathf.Max(_punch, Mathf.Clamp01(strength));
            _impulse = -direction.normalized * 0.045f * Mathf.Clamp01(strength);
        }

        public void BlendMotion(float seconds)
        {
            if (_body == null || _body.sprite == null)
                return;
            _previous = _body.sprite;
            _blendDuration = Mathf.Clamp(seconds, 0.02f, 0.1f);
            _blendAge = 0f;
            _blendPending = true;
            _blending = false;
            _blendPendingUntil = Time.time + 0.22f;
        }

        private void LateUpdate()
        {
            if (_body == null || _body.sprite == null)
                return;
            bool alive = _owner != null && _owner.CurrentHealth > 0f;
            float dt = Time.deltaTime;
            _punch *= Mathf.Exp(-dt * 14f);
            _impulse *= Mathf.Exp(-dt * 16f);
            float breath = alive ? Mathf.Sin(Time.time * (_casting ? 4f : 2.4f)) : 0f;
            // Only the visual child moves. The collider, room bounds and damage stay stable.
            if (_body.transform != transform)
            {
                _body.transform.localPosition = _restPosition + new Vector3(_impulse.x, _impulse.y + breath * 0.018f, 0f);
                _body.transform.localScale = Vector3.Scale(_restScale, new Vector3(1f + breath * 0.008f + _punch * 0.025f, 1f - breath * 0.007f - _punch * 0.022f, 1f));
                _body.transform.localRotation = Quaternion.Euler(0f, 0f, breath * (_casting ? 0.8f : 0.35f));
            }

            if (_current != _body.sprite)
            {
                _current = _body.sprite;
                if (_blendPending)
                {
                    _blendPending = false;
                    _blending = true;
                    _blendAge = 0f;
                }
            }

            if (_blendPending && Time.time >= _blendPendingUntil)
                _blendPending = false;
            if (_blending)
            {
                _blendAge += dt;
                if (_blendAge >= _blendDuration)
                    _blending = false;
            }

            if (!_blending && !_blendPending)
                _previous = _current;
            if (_bodyMaterial != null)
            {
                FrameData current = Frame(_current);
                FrameData previous = Frame(_previous != null ? _previous : _current);
                _properties.SetTexture("_PreviousTex", _previous != null ? _previous.texture : _current.texture);
                _properties.SetVector("_CurrentUv", current.Uv);
                _properties.SetVector("_PreviousUv", previous.Uv);
                _properties.SetVector("_CurrentLocal", current.Local);
                _properties.SetVector("_PreviousLocal", previous.Local);
                _properties.SetFloat("_FrameBlend", _blending ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_blendAge / _blendDuration)) : 1f);
                _properties.SetFloat("_NeonStrength", alive ? 0.28f + _charge * 0.3f + _punch * 0.2f : 0.08f);
                _properties.SetColor("_NeonColor", _casting ? _castColor : new Color(0.1f, 0.88f, 1f));
                _body.SetPropertyBlock(_properties);
            }

            if (_rim != null)
            {
                _rim.enabled = alive && _body.enabled;
                _rim.sprite = _body.sprite;
                _rim.sortingLayerID = _body.sortingLayerID;
                _rim.sortingOrder = _body.sortingOrder + 1;
                _rim.transform.position = _body.transform.position;
                _rim.transform.rotation = _body.transform.rotation;
                Vector3 parentScale = transform.lossyScale;
                Vector3 bodyScale = _body.transform.lossyScale;
                _rim.transform.localScale = new Vector3(bodyScale.x / Mathf.Max(0.001f, parentScale.x), bodyScale.y / Mathf.Max(0.001f, parentScale.y), 1f);
                Color rim = _casting ? _castColor : new Color(0.12f, 0.85f, 1f);
                rim.a = 0.6f + _charge * 0.18f;
                _rim.color = rim;
                _rimProperties.SetVector("_UvBounds", Frame(_current).Uv);
                _rim.SetPropertyBlock(_rimProperties);
            }

            if (alive && _owner.CanFight && Time.time >= _nextAmbient && _body.enabled)
            {
                _nextAmbient = Time.time + (_casting ? 0.12f : 0.35f);
                Vector3 point = transform.position + new Vector3(Random.Range(-0.55f, 0.55f), Random.Range(-0.2f, 0.55f), 0f);
                _effects.Trail(point, Vector2.down, 0.15f, _casting ? _castColor : new Color(0.15f, 0.9f, 1f), _body.sortingLayerID, _body.sortingOrder - 1);
            }
        }

        private FrameData Frame(Sprite sprite)
        {
            if (_frames.TryGetValue(sprite, out FrameData data))
                return data;
            Rect rect = sprite.textureRect;
            float ppu = Mathf.Max(1f, sprite.pixelsPerUnit);
            data.Uv = new Vector4(rect.xMin / sprite.texture.width, rect.yMin / sprite.texture.height, rect.xMax / sprite.texture.width, rect.yMax / sprite.texture.height);
            // Align differently sized canvases by their pivot and pixels per unit.
            data.Local = new Vector4(-sprite.pivot.x / ppu, -sprite.pivot.y / ppu, sprite.rect.width / ppu, sprite.rect.height / ppu);
            _frames.Add(sprite, data);
            return data;
        }

        private void OnDisable()
        {
            if (_rim != null)
                _rim.enabled = false;
        }

        private void OnDestroy()
        {
            if (_bodyMaterial != null)
                Destroy(_bodyMaterial);
            if (_rimMaterial != null)
                Destroy(_rimMaterial);
        }
    }
}
