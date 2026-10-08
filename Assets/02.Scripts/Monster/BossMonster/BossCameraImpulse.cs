using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace _02.Scripts.Monster.BossMonster
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class BossCameraImpulse : MonoBehaviour
    {
        private sealed class Pulse
        {
            public float Age;
            public float Duration;
            public float Strength;
        }

        private readonly List<Pulse> _pulses = new List<Pulse>(12);
        private Camera _camera;
        private Vector3 _offset;
        private Vector3 _appliedOffset;
        private float _sustain;
        private float _targetSustain;
        private float _clock;
        private bool _offsetApplied;

        public static BossCameraImpulse GetOrCreate(Camera camera)
        {
            if (camera == null)
            {
                return null;
            }
            BossCameraImpulse impulse = camera.GetComponent<BossCameraImpulse>();
            return impulse != null ? impulse : camera.gameObject.AddComponent<BossCameraImpulse>();
        }

        public void Hit(float strength, float duration = 0.3f)
        {
            if (strength <= 0f)
            {
                return;
            }
            if (_pulses.Count >= 12)
            {
                _pulses.RemoveAt(0);
            }
            _pulses.Add(new Pulse
            {
                Strength = Mathf.Clamp(strength, 0f, 0.6f),
                Duration = Mathf.Max(0.05f, duration)
            });
        }

        public void SetSustain(float strength)
        {
            _targetSustain = Mathf.Clamp(strength, 0f, 0.2f);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeginRendering;
            RenderPipelineManager.endCameraRendering += EndRendering;
            Camera.onPreCull += ApplyOffset;
            Camera.onPostRender += RestoreOffset;
        }

        private void Update()
        {
            // 카메라 추적 코드가 다음 위치를 계산하기 전에 이전 렌더의 오프셋을 제거합니다.
            RestoreOffset(_camera);
            if (Time.deltaTime <= 0f)
            {
                _offset = Vector3.zero;
                return;
            }
            _clock += Time.deltaTime;
            _sustain = Mathf.MoveTowards(_sustain, _targetSustain, Time.deltaTime * 0.6f);
            float strength = _sustain;
            for (int i = _pulses.Count - 1; i >= 0; i--)
            {
                Pulse pulse = _pulses[i];
                pulse.Age += Time.deltaTime;
                if (pulse.Age >= pulse.Duration)
                {
                    _pulses.RemoveAt(i);
                    continue;
                }
                float remaining = 1f - pulse.Age / pulse.Duration;
                strength += pulse.Strength * remaining * remaining;
            }
            strength = Mathf.Min(0.45f, strength);
            float x = Mathf.Sin(_clock * 137f) * 0.65f + Mathf.Sin(_clock * 83f) * 0.35f;
            float y = Mathf.Cos(_clock * 119f) * 0.65f + Mathf.Sin(_clock * 71f) * 0.35f;
            _offset = new Vector3(x, y, 0f) * strength;
        }

        private void BeginRendering(ScriptableRenderContext context, Camera camera)
        {
            ApplyOffset(camera);
        }

        private void EndRendering(ScriptableRenderContext context, Camera camera)
        {
            RestoreOffset(camera);
        }

        private void ApplyOffset(Camera camera)
        {
            if (camera != _camera || _offsetApplied || Time.timeScale <= 0f)
            {
                return;
            }
            _appliedOffset = _offset;
            camera.transform.position += _appliedOffset;
            _offsetApplied = true;
        }

        private void RestoreOffset(Camera camera)
        {
            if (camera != _camera || !_offsetApplied || camera == null)
            {
                return;
            }
            camera.transform.position -= _appliedOffset;
            _offsetApplied = false;
            _appliedOffset = Vector3.zero;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginRendering;
            RenderPipelineManager.endCameraRendering -= EndRendering;
            Camera.onPreCull -= ApplyOffset;
            Camera.onPostRender -= RestoreOffset;
            RestoreOffset(_camera);
            _pulses.Clear();
            _sustain = 0f;
            _targetSustain = 0f;
        }
    }
}
