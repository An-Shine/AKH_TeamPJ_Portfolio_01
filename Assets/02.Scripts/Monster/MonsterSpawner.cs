using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("몬스터 프리팹")]
    [SerializeField] private NormalMonster[] _monsterPrefabs;

    [Header("스폰 위치")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("스폰 설정")]
    [SerializeField] private int _spawnCount = 3;

    private void Start()
    {
        SpawnMonsters();
    }

    private void SpawnMonsters()
    {
        if (_monsterPrefabs == null || _monsterPrefabs.Length == 0)
        {
            Debug.LogWarning("MonsterSpawner: 몬스터 프리팹이 설정되지 않았습니다.");
            return;
        }

        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogWarning("MonsterSpawner: 스폰 위치가 설정되지 않았습니다.");
            return;
        }

        for (int i = 0; i < _spawnCount; i++)
        {
            SpawnMonster(i);
        }
    }

    private void SpawnMonster(int index)
    {
        NormalMonster monsterPrefab =
            _monsterPrefabs[index % _monsterPrefabs.Length];

        Transform spawnPoint =
            _spawnPoints[index % _spawnPoints.Length];

        Instantiate(
            monsterPrefab,
            spawnPoint.position,
            Quaternion.identity
        );
    }
}