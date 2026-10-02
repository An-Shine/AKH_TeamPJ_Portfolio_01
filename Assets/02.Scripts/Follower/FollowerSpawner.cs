using System.Collections.Generic;
using UnityEngine;

public class FollowerSpawner : MonoBehaviour
{
    private static FollowerSpawner _instance;
    public static FollowerSpawner Instance => _instance;

    [SerializeField] private List<FollowerEntry> _followerEntries;

    private Dictionary<FollowerType, List<Follower>> _followerDictionary;
    public IReadOnlyDictionary<FollowerType, List<Follower>> FollowerDictionary => _followerDictionary;

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        _followerDictionary = new Dictionary<FollowerType, List<Follower>>();
        int repeat;
        Follower follower;
        foreach (FollowerEntry followerEntry in _followerEntries)
        {
            _followerDictionary[followerEntry.Type] = new List<Follower>();
            for (repeat = 0; repeat < followerEntry.Count; repeat++)
            {
                follower = Instantiate(followerEntry.FollowerPrefab, transform);
                follower.gameObject.SetActive(false);
                _followerDictionary[followerEntry.Type].Add(follower);
            }
        }
    }

    // itemManager가 playerTransform을 가지고 있음
    public void Spawn(int spawnCount, FollowerType followerType, Transform playerTransform,
        float startAngle, float radius)
    {
        float differenceAngle = 360f / spawnCount;
        int spawnedCount = 0;
        if (_followerDictionary.TryGetValue(followerType, out List<Follower> followerList))
        {
            for (int count = 0; count < spawnCount; count++)
            {
                if (!followerList[count].isActiveAndEnabled)
                {
                    followerList[count].Initiaize(
                        playerTransform,
                        startAngle + differenceAngle * spawnedCount,
                        radius);
                    followerList[count].gameObject.SetActive(true);
                    spawnedCount += 1;
                }
            }
        }
    }
}