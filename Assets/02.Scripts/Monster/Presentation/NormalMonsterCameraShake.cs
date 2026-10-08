using UnityEngine;
using UnityEngine.Rendering;

namespace _02.Scripts.Monster.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class NormalMonsterCameraShake : MonoBehaviour
    {
        private Camera _camera;
        private Vector3 _renderPosition;
        private Vector2 _offset;
        private float _strength;
        private float _remaining;
        private float _duration;
        private bool _applied;
        internal static void Request(Vector3 point, float strength, float duration)
        {
            Camera camera = Camera.main;
            if (camera == null || strength <= 0f || Time.timeScale <= 0f)
            {
                return;
            }

            float distance = ((Vector2)camera.transform.position - (Vector2)point).magnitude;
            if (distance > 18f)
            {
                return;
            }

            NormalMonsterCameraShake shake = camera.GetComponent<NormalMonsterCameraShake>();
            if (shake == null)
            {
                shake = camera.gameObject.AddComponent<NormalMonsterCameraShake>();
            }

            shake._strength = Mathf.Max(shake._strength, strength / (1f + distance * 0.08f));
            shake._remaining = Mathf.Max(shake._remaining, duration);
            shake._duration = Mathf.Max(shake._duration, shake._remaining);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
            RenderPipelineManager.endCameraRendering += EndCameraRendering;
        }

        private void LateUpdate()
        {
            if (Time.timeScale <= 0f || _remaining <= 0f)
            {
                _offset = Vector2.zero;
                if (_remaining <= 0f)
                {
                    _strength = 0f;
                    _duration = 0f;
                }

                return;
            }

            _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
            float fade = _remaining / Mathf.Max(0.01f, _duration);
            float phase = Time.time * 63f;
            _offset = new Vector2(Mathf.Sin(phase), Mathf.Sin(phase * 1.31f + 1.8f)) * (_strength * fade * fade);
        }

        private void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
            {
                ApplyForRender();
            }
        }

        private void EndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
            {
                RestoreAfterRender();
            }
        }

        private void OnPreCull()
        {
            ApplyForRender();
        }

        private void OnPostRender()
        {
            RestoreAfterRender();
        }

        private void ApplyForRender()
        {
            if (_applied || _offset.sqrMagnitude <= 0f)
            {
                return;
            }

            _renderPosition = transform.position;
            transform.position = _renderPosition + new Vector3(_offset.x, _offset.y, 0f);
            _applied = true;
        }

        private void RestoreAfterRender()
        {
            if (!_applied)
            {
                return;
            }

            transform.position = _renderPosition;
            _applied = false;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= EndCameraRendering;
            RestoreAfterRender();
            _offset = Vector2.zero;
            _strength = 0f;
            _remaining = 0f;
            _duration = 0f;
        }
    }
}
