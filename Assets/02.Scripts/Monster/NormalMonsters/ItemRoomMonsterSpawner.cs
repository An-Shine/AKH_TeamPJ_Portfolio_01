using System.Collections.Generic;
using UnityEngine;

namespace _02.Scripts.Monster.NormalMonsters
{
    [DisallowMultipleComponent]
    public sealed class ItemRoomMonsterSpawner : MonoBehaviour
    {

        [Header("아이템 방")]
        [SerializeField] private MonsterRoomBounds _room;

        [Header("일반 몬스터 3종: 각각 한 마리")]
        [SerializeField] private MeleeMonster _meleePrefab;
        [SerializeField] private RangeMonster _rangePrefab;
        [SerializeField] private KamikazeMonster _kamikazePrefab;

        [Header("각 몬스터의 생성 위치")]
        [SerializeField] private Transform _meleeSpawnPoint;
        [SerializeField] private Transform _rangeSpawnPoint;
        [SerializeField] private Transform _kamikazeSpawnPoint;

        [Header("추가 생성 위치")]
        [Tooltip("추가 위치를 연결하면 기본 세 위치와 이 목록에서 서로 다른 위치를 랜덤 선택합니다. 생성량은 각 종류 한 마리씩입니다.")]
        [SerializeField] private Transform[] _additionalSpawnPoints = new Transform[0];

        [Header("아이템 방 전용 플레이어 감지")]
        [Tooltip("이 스포너가 생성한 몬스터에만 같은 방과 거리 조건을 적용합니다. 프리팹 원본은 변경하지 않습니다.")]
        [SerializeField] private bool _overridePlayerDetection = true;
        [SerializeField, Min(0.1f)] private float _detectionRange = 2f;
        [SerializeField, Min(0.1f)] private float _loseTargetRange = 7f;

        [Header("생성 설정")]
        [SerializeField] private bool _spawnOnStart = true;
        [SerializeField, Min(0.1f)] private float _retryInterval = 0.5f;
        private bool _meleeSpawned;
        private bool _rangeSpawned;
        private bool _kamikazeSpawned;
        private bool _warnedConfiguration;
        private bool _warnedSpawnPosition;
        private float _nextSpawnTime;
        private readonly List<Transform> _spawnCandidates = new List<Transform>();
        private readonly HashSet<Transform> _usedSpawnPoints = new HashSet<Transform>();
        public int TotalSpawned => (_meleeSpawned ? 1 : 0) + (_rangeSpawned ? 1 : 0) + (_kamikazeSpawned ? 1 : 0);
        public bool IsComplete => _meleeSpawned && _rangeSpawned && _kamikazeSpawned;
        private void Awake()
        {
            ResolveRoom();
        }

        private void ResolveRoom()
        {
            if (_room == null)
            {
                _room = GetComponentInParent<MonsterRoomBounds>();
            }

            if (_room != null)
            {
                _room.EnablePlayerEntryDetection();
            }
        }

        private void Start()
        {
            if (_spawnOnStart && Time.timeScale > 0f)
            {
                SpawnMonsters();
            }

            _nextSpawnTime = Time.time + Mathf.Max(0.1f, _retryInterval);
        }

        private void Update()
        {
            if (IsComplete || Time.timeScale <= 0f || Time.time < _nextSpawnTime)
            {
                return;
            }

            _nextSpawnTime = Time.time + Mathf.Max(0.1f, _retryInterval);
            SpawnMonsters();
        }

        public void SpawnMonsters()
        {
            if (IsComplete || Time.timeScale <= 0f || !ValidateConfiguration())
            {
                return;
            }

            if (!_meleeSpawned)
            {
                _meleeSpawned = TrySpawn(_meleePrefab, _meleeSpawnPoint);
            }

            if (!_rangeSpawned)
            {
                _rangeSpawned = TrySpawn(_rangePrefab, _rangeSpawnPoint);
            }

            if (!_kamikazeSpawned)
            {
                _kamikazeSpawned = TrySpawn(_kamikazePrefab, _kamikazeSpawnPoint);
            }
        }

        private bool ValidateConfiguration()
        {
            ResolveRoom();
            CollectSpawnCandidates();
            bool validSpawnPoints = HasAdditionalSpawnPoints() ? _spawnCandidates.Count >= 3 : _meleeSpawnPoint != null && _rangeSpawnPoint != null && _kamikazeSpawnPoint != null && _meleeSpawnPoint != _rangeSpawnPoint && _meleeSpawnPoint != _kamikazeSpawnPoint && _rangeSpawnPoint != _kamikazeSpawnPoint;
            bool isValid = _room != null && _room.IsConfigured && _meleePrefab != null && _rangePrefab != null && _kamikazePrefab != null && validSpawnPoints;
            if (!isValid && !_warnedConfiguration)
            {
                Debug.LogWarning($"{name}: Room, 세 종류의 Prefab, 서로 다른 Spawn Point를 최소 세 개 설정하세요.", this);
                _warnedConfiguration = true;
            }

            if (isValid)
            {
                _warnedConfiguration = false;
            }

            return isValid;
        }

        private bool TrySpawn(NormalMonster prefab, Transform spawnPoint)
        {
            if (!_room.isActiveAndEnabled)
            {
                return false;
            }

            if (!HasAdditionalSpawnPoints())
            {
                if (TrySpawnAt(prefab, spawnPoint))
                {
                    return true;
                }

                WarnSpawnPosition();
                return false;
            }

            int startIndex = Random.Range(0, _spawnCandidates.Count);
            for (int i = 0; i < _spawnCandidates.Count; i++)
            {
                Transform candidate = _spawnCandidates[(startIndex + i) % _spawnCandidates.Count];
                if (TrySpawnAt(prefab, candidate))
                {
                    return true;
                }
            }

            WarnSpawnPosition();
            return false;
        }

        private bool TrySpawnAt(NormalMonster prefab, Transform spawnPoint)
        {
            if (spawnPoint == null || _usedSpawnPoints.Contains(spawnPoint) || !spawnPoint.gameObject.activeInHierarchy || !prefab.IsSpawnPositionClear(spawnPoint.position, _room))
            {
                return false;
            }

            NormalMonster monster = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            if (!monster.TryAssignHomeRoom(_room))
            {
                monster.gameObject.SetActive(false);
                Destroy(monster.gameObject);
                return false;
            }

            if (_overridePlayerDetection)
            {
                monster.ConfigureRoomDetection(_detectionRange, _loseTargetRange);
            }

            _usedSpawnPoints.Add(spawnPoint);
            _warnedSpawnPosition = false;
            return true;
        }

        private bool HasAdditionalSpawnPoints()
        {
            if (_additionalSpawnPoints == null)
            {
                return false;
            }

            foreach (Transform point in _additionalSpawnPoints)
            {
                if (point != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void CollectSpawnCandidates()
        {
            _spawnCandidates.Clear();
            AddSpawnCandidate(_meleeSpawnPoint);
            AddSpawnCandidate(_rangeSpawnPoint);
            AddSpawnCandidate(_kamikazeSpawnPoint);
            if (_additionalSpawnPoints == null)
            {
                return;
            }

            foreach (Transform point in _additionalSpawnPoints)
            {
                AddSpawnCandidate(point);
            }
        }

        private void AddSpawnCandidate(Transform spawnPoint)
        {
            if (spawnPoint != null && !_spawnCandidates.Contains(spawnPoint))
            {
                _spawnCandidates.Add(spawnPoint);
            }
        }

        private void WarnSpawnPosition()
        {
            if (_warnedSpawnPosition)
            {
                return;
            }

            Debug.LogWarning($"{name}: 아이템 방의 Spawn Point를 방 안쪽의 벽과 겹치지 않는 위치로 옮기세요.", this);
            _warnedSpawnPosition = true;
        }

        private void OnValidate()
        {
            _retryInterval = Mathf.Max(0.1f, _retryInterval);
            _detectionRange = Mathf.Max(0.1f, _detectionRange);
            _loseTargetRange = Mathf.Max(_detectionRange, _loseTargetRange);
        }
    }
}
