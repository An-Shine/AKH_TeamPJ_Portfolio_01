using UnityEngine;

public class PlayerDashAfterimage : MonoBehaviour
{
    [SerializeField] private Afterimage[] _images;
    [SerializeField, Min(0.01f)] private float _duration = 0.45f;
    [SerializeField] private Color _tint = new Color(0.25f, 0.85f, 1f, 0.55f);

    public void Play(Vector2 start, Vector2 destination)
    {
        if (!isActiveAndEnabled || (destination - start).sqrMagnitude < 0.0001f) return;

        for (int i = 0; i < _images.Length; i++)
        {
            // 도착 지점은 플레이어가 차지하므로 잔상은 그 직전까지만 배치합니다.
            float progress = i / (float)_images.Length;
            Vector2 position = Vector2.Lerp(start, destination, progress);
            Color color = _tint;
            color.a *= Mathf.Lerp(0.5f, 1f, progress);
            float lifetime = _duration * Mathf.Lerp(0.55f, 1f, progress);
            _images[i].Spawn(position, color, lifetime);
        }
    }
}