using UnityEngine;
using UnityEngine.UI;

// 씬에 미리 배치하는 UI 링. 완충 시 한 번 확장되고 자체적으로 사라집니다.
[RequireComponent(typeof(CanvasRenderer))]
public class CrosshairChargeRing : MaskableGraphic
{
    private float _duration = 0.16f;
    private float _startRadius = 13f;
    private float _endRadius = 108f;
    private float _thickness = 7f;
    private float _age;
    private bool _playing;

    public void Play()
    {
        _age = 0f;
        _playing = true;
        SetVerticesDirty();
    }

    public void Hide()
    {
        _playing = false;
        SetVerticesDirty();
    }

    private void Update()
    {
        if (!_playing) return;
        _age += Time.deltaTime;
        if (_age >= _duration) _playing = false;
        SetVerticesDirty();
    }

    protected override void OnDisable()
    {
        _playing = false;
        base.OnDisable();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (!_playing) return;
        float progress = Mathf.Clamp01(_age / Mathf.Max(0.01f, _duration));
        float radius = Mathf.Lerp(_startRadius, _endRadius, 1f - (1f - progress) * (1f - progress));
        float alpha = (1f - progress) * (1f - progress);
        AddRing(mesh, radius, _thickness * 3f, new Color(0.7f, 0.3f, 1f, alpha * 0.25f));
        AddRing(mesh, radius, _thickness, new Color(1f, 1f, 1f, alpha));
    }

    private static void AddRing(VertexHelper mesh, float radius, float thickness, Color tint)
    {
        const int segments = 64;
        int first = mesh.currentVertCount;
        float inner = Mathf.Max(0f, radius - thickness * 0.5f);
        float outer = radius + thickness * 0.5f;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            mesh.AddVert(direction * inner, tint, Vector2.zero);
            mesh.AddVert(direction * outer, tint, Vector2.zero);
            if (i == segments) continue;
            int index = first + i * 2;
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index + 1, index + 3, index + 2);
        }
    }
}