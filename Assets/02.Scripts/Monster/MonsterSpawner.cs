using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("몬스터 프리팹")]
    [SerializeField] private NormalMonster[] _monsterPrefabs;

    [Header("스폰 위치")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("스폰 설정")]
    [SerializeField, Min(0)] private int _spawnCount = 3;
    [SerializeField, Min(0.1f)] private float _spawnInterval = 3f;
    [SerializeField] private bool _spawnOnStart = true;

    [Tooltip("이 스포너가 생성한 살아 있는 몬스터의 최대 수입니다. 0이면 제한하지 않습니다.")]
    [SerializeField, Min(0)] private int _maxAliveMonsters = 0;

    [Header("플레이어 근처 생성 차단")]
    [Tooltip("비워 두면 Player 태그로 실제 플레이어를 찾습니다.")]
    [SerializeField] private Transform _player;
    [SerializeField, Min(0f)] private float _minPlayerDistance = 5f;

    [Tooltip("켜면 스포너 본체 근처에서도 전체 생성을 멈춥니다. 각 Spawn Point의 거리 검사는 항상 적용합니다.")]
    [SerializeField] private bool _blockNearSpawnerRoot = true;

    private readonly List<NormalMonster> _aliveMonsters = new List<NormalMonster>();
    private readonly List<NormalMonster> _availablePrefabs = new List<NormalMonster>();
    private readonly List<Transform> _availableSpawnPoints = new List<Transform>();

    private float _nextSpawnTime;
    private float _nextPlayerSearchTime;
    private bool _warnedMissingPlayer;
    private bool _warnedMissingPrefabs;
    private bool _warnedMissingSpawnPoints;

    private void Start()
    {
        ClampSettings();

        if (_spawnOnStart && Time.timeScale > 0f)
        {
            SpawnMonsters();
        }

        _nextSpawnTime = Time.time + _spawnInterval;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || Time.time < _nextSpawnTime)
        {
            return;
        }

        // 안전한 위치가 없으면 다음 생성 주기에 다시 검사합니다.
        _nextSpawnTime = Time.time + Mathf.Max(0.1f, _spawnInterval);
        SpawnMonsters();
    }

    private void SpawnMonsters()
    {
        if (_spawnCount <= 0 || !ResolvePlayer())
        {
            return;
        }

        if (_blockNearSpawnerRoot && IsNearPlayer(transform.position))
        {
            return;
        }

        int aliveCount = CountAliveMonsters();
        int remainingSlots = _maxAliveMonsters > 0
            ? Mathf.Max(0, _maxAliveMonsters - aliveCount)
            : _spawnCount;

        if (remainingSlots == 0)
        {
            return;
        }

        if (!CollectAvailablePrefabs() || !CollectAvailableSpawnPoints())
        {
            return;
        }

        int count = Mathf.Min(_spawnCount, remainingSlots);
        count = Mathf.Min(count, _availableSpawnPoints.Count);

        for (int i = 0; i < count; i++)
        {
            SpawnMonster(_availableSpawnPoints);
        }
    }

    private bool CollectAvailablePrefabs()
    {
        _availablePrefabs.Clear();

        if (_monsterPrefabs != null)
        {
            foreach (NormalMonster prefab in _monsterPrefabs)
            {
                if (prefab != null)
                {
                    _availablePrefabs.Add(prefab);
                }
            }
        }

        if (_availablePrefabs.Count > 0)
        {
            _warnedMissingPrefabs = false;
            return true;
        }

        if (!_warnedMissingPrefabs)
        {
            Debug.LogWarning($"{name}: 몬스터 프리팹을 설정하세요.", this);
            _warnedMissingPrefabs = true;
        }

        return false;
    }

    private bool CollectAvailableSpawnPoints()
    {
        _availableSpawnPoints.Clear();
        bool hasValidPoint = false;

        if (_spawnPoints != null)
        {
            foreach (Transform point in _spawnPoints)
            {
                if (point == null)
                {
                    continue;
                }

                hasValidPoint = true;

                if (!point.gameObject.activeInHierarchy || IsNearPlayer(point.position))
                {
                    continue;
                }

                if (!_availableSpawnPoints.Contains(point))
                {
                    _availableSpawnPoints.Add(point);
                }
            }
        }

        if (!hasValidPoint)
        {
            if (!_warnedMissingSpawnPoints)
            {
                Debug.LogWarning($"{name}: Spawn Points를 설정하세요.", this);
                _warnedMissingSpawnPoints = true;
            }
        }
        else
        {
            _warnedMissingSpawnPoints = false;
        }

        return _availableSpawnPoints.Count > 0;
    }

    private void SpawnMonster(List<Transform> availableSpawnPoints)
    {
        int monsterIndex = Random.Range(0, _availablePrefabs.Count);
        int spawnPointIndex = Random.Range(0, availableSpawnPoints.Count);
        NormalMonster monsterPrefab = _availablePrefabs[monsterIndex];
        Transform spawnPoint = availableSpawnPoints[spawnPointIndex];

        NormalMonster monster = Instantiate(
            monsterPrefab,
            spawnPoint.position,
            Quaternion.identity
        );

        _aliveMonsters.Add(monster);
        availableSpawnPoints.RemoveAt(spawnPointIndex);
    }

    private int CountAliveMonsters()
    {
        for (int i = _aliveMonsters.Count - 1; i >= 0; i--)
        {
            if (_aliveMonsters[i] == null || _aliveMonsters[i].IsDead)
            {
                _aliveMonsters.RemoveAt(i);
            }
        }

        return _aliveMonsters.Count;
    }

    private bool ResolvePlayer()
    {
        if (_player != null && _player.gameObject.activeInHierarchy &&
            _player.GetComponentInParent<NormalMonster>() == null)
        {
            return true;
        }

        _player = null;

        if (Time.time < _nextPlayerSearchTime)
        {
            return false;
        }

        _nextPlayerSearchTime = Time.time + 1f;

        foreach (GameObject playerObject in GameObject.FindGameObjectsWithTag("Player"))
        {
            if (playerObject.GetComponentInParent<NormalMonster>() != null ||
                playerObject.transform == transform || playerObject.transform.IsChildOf(transform))
            {
                continue;
            }

            _player = playerObject.transform;
            _warnedMissingPlayer = false;
            return true;
        }

        if (!_warnedMissingPlayer)
        {
            Debug.LogWarning($"{name}: Player 태그가 붙은 실제 플레이어를 찾을 수 없습니다.", this);
            _warnedMissingPlayer = true;
        }

        return false;
    }

    private bool IsNearPlayer(Vector3 position)
    {
        if (_player == null)
        {
            return true;
        }

        Vector2 difference = (Vector2)position - (Vector2)_player.position;
        return difference.sqrMagnitude <= _minPlayerDistance * _minPlayerDistance;
    }

    private void OnValidate()
    {
        ClampSettings();
    }

    private void ClampSettings()
    {
        _spawnCount = Mathf.Max(0, _spawnCount);
        _spawnInterval = Mathf.Max(0.1f, _spawnInterval);
        _maxAliveMonsters = Mathf.Max(0, _maxAliveMonsters);
        _minPlayerDistance = Mathf.Max(0f, _minPlayerDistance);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.5f);

        if (_blockNearSpawnerRoot)
        {
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, _minPlayerDistance));
        }

        if (_spawnPoints == null)
        {
            return;
        }

        foreach (Transform point in _spawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, Mathf.Max(0f, _minPlayerDistance));
            }
        }
    }
}
