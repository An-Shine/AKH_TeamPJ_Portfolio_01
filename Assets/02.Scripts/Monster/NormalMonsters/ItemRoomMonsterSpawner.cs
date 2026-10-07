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

        [Header("생성 설정")]
        [SerializeField] private bool _spawnOnStart = true;
        [SerializeField, Min(0.1f)] private float _retryInterval = 0.5f;
        private bool _meleeSpawned;
        private bool _rangeSpawned;
        private bool _kamikazeSpawned;
        private bool _warnedConfiguration;
        private bool _warnedSpawnPosition;
        private float _nextSpawnTime;
        public int TotalSpawned => (_meleeSpawned ? 1 : 0) + (_rangeSpawned ? 1 : 0) + (_kamikazeSpawned ? 1 : 0);
        public bool IsComplete => _meleeSpawned && _rangeSpawned && _kamikazeSpawned;
        private void Awake()
        {
            ResolveRoom();
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
            bool isValid = _room != null && _room.IsConfigured &&
                _meleePrefab != null && _rangePrefab != null && _kamikazePrefab != null &&
                _meleeSpawnPoint != null && _rangeSpawnPoint != null && _kamikazeSpawnPoint != null &&
                _meleeSpawnPoint != _rangeSpawnPoint && _meleeSpawnPoint != _kamikazeSpawnPoint &&
                _rangeSpawnPoint != _kamikazeSpawnPoint;
            if (!isValid && !_warnedConfiguration)
            {
                Debug.LogWarning($"{name}: Room, 세 종류의 Prefab, 서로 다른 세 Spawn Point를 설정하세요.", this);
                _warnedConfiguration = true;
            }

            if (isValid)
            {
                _warnedConfiguration = false;
            }

            return isValid;
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

        private bool TrySpawn(NormalMonster prefab, Transform spawnPoint)
        {
            if (!_room.gameObject.activeInHierarchy || !spawnPoint.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!prefab.IsSpawnPositionClear(spawnPoint.position, _room))
            {
                WarnSpawnPosition();
                return false;
            }

            NormalMonster monster = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
            if (!monster.TryAssignHomeRoom(_room))
            {
                monster.gameObject.SetActive(false);
                Destroy(monster.gameObject);
                WarnSpawnPosition();
                return false;
            }

            _warnedSpawnPosition = false;
            return true;
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
        }
    }
}
