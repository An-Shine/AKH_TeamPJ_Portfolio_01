using System.Collections.Generic;
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
        if (_monsterPrefabs == null ||
            _monsterPrefabs.Length == 0)
        {
            Debug.LogWarning(
                "MonsterSpawner: 몬스터 프리팹이 설정되지 않았습니다."
            );

            return;
        }

        if (_spawnPoints == null ||
            _spawnPoints.Length == 0)
        {
            Debug.LogWarning(
                "MonsterSpawner: 스폰 위치가 설정되지 않았습니다."
            );

            return;
        }

        List<Transform> availableSpawnPoints =
            new List<Transform>(_spawnPoints);

        for (int i = 0; i < _spawnCount; i++)
        {
            SpawnMonster(availableSpawnPoints);
        }
    }

    private void SpawnMonster(
        List<Transform> availableSpawnPoints
    )
    {
        if (availableSpawnPoints.Count == 0)
        {
            Debug.LogWarning(
                "MonsterSpawner: 사용할 수 있는 스폰 위치가 없습니다."
            );

            return;
        }

        int monsterIndex =
            Random.Range(
                0,
                _monsterPrefabs.Length
            );

        int spawnPointIndex =
            Random.Range(
                0,
                availableSpawnPoints.Count
            );

        NormalMonster monsterPrefab =
            _monsterPrefabs[monsterIndex];

        Transform spawnPoint =
            availableSpawnPoints[spawnPointIndex];

        Instantiate(
            monsterPrefab,
            spawnPoint.position,
            Quaternion.identity
        );

        availableSpawnPoints.RemoveAt(
            spawnPointIndex
        );
    }
}