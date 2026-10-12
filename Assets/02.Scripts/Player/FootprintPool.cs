using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[DefaultExecutionOrder(-200)]
public class FootprintPool : MonoBehaviour
{
    [SerializeField] private Footprint _prefab;
    [SerializeField, Min(1)] private int _initialSize = 32;
    [SerializeField, Min(1)] private int _maxSize = 128;

    private ObjectPool<Footprint> _pool;
    private readonly HashSet<Footprint> _active = new HashSet<Footprint>();
    public int ActiveCount => _active.Count;
    public int TotalCount => _pool != null ? _pool.CountAll : 0;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_pool != null) return;
        if (_prefab == null)
        {
            Debug.LogError("FootprintPool에 Footprint 프리팹을 연결하세요.", this);
            return;
        }

        // 사용자가 씬에 배치한 발자국도 최초 풀 항목으로 재사용합니다.
        var existing = new Queue<Footprint>(GetComponentsInChildren<Footprint>(true));
        _pool = new ObjectPool<Footprint>(
            () =>
            {
                Footprint footprint = Instantiate(_prefab, transform);
                footprint.Initialize(this);
                footprint.Hide();
                return footprint;
            },
            null,
            footprint => footprint.Hide(),
            footprint => Destroy(footprint.gameObject),
            true, Mathf.Min(_initialSize, _maxSize), _maxSize);

        int warmCount = Mathf.Clamp(Mathf.Max(_initialSize, existing.Count), 1, _maxSize);
        var warmed = new List<Footprint>(warmCount);
        for (int i = 0; i < warmCount; i++)
        {
            warmed.Add(_pool.Get());
        }

        foreach (Footprint footprint in warmed)
        {
            _pool.Release(footprint);
        }
    }

    public bool Spawn(Vector3 position, Quaternion rotation, bool flipY)
    {
        if (!isActiveAndEnabled)
        {
            return false;
        }

        Initialize();
        // 최대 개수에 도달하면 새 자국을 생략해 무한 생성을 방지합니다.
        if (_pool == null || (_pool.CountInactive == 0 && _pool.CountAll >= _maxSize))
        {
            return false;
        }

        Footprint footprint = _pool.Get();
        _active.Add(footprint);
        footprint.Show(position, rotation, flipY);
        return true;
    }

    public void Release(Footprint footprint)
    {
        if (_pool == null || !_active.Remove(footprint)) return;
        _pool.Release(footprint);
    }

    private void OnDisable()
    {
        if (_pool == null) return;
        foreach (Footprint footprint in _active)
        {
            if (footprint != null)
            {
                _pool.Release(footprint);
            }
        }

        _active.Clear();
    }
}