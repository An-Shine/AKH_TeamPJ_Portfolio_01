using System.Collections.Generic;
using UnityEngine;

public class FollowerSpawner : MonoBehaviour
{
    private static FollowerSpawner _instance;
    public static FollowerSpawner Instance => _instance;

    [SerializeField] private List<FollowerEntry> _followerEntries;

    private Dictionary<FollowerType, Follower> _followerDictionary;
    public IReadOnlyDictionary<FollowerType, Follower> FollowerDictionary => _followerDictionary;

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
        _followerDictionary = new Dictionary<FollowerType, Follower>();
        foreach (FollowerEntry follower in _followerEntries)
        {
            _followerDictionary[follower.Type] = Instantiate(follower.FollowerPrefab, transform);
            _followerDictionary[follower.Type].gameObject.SetActive(false);
        }
    }

    public void Spawn(FollowerType followerType, Transform playerTransform) // itemManager가 playerTransform을 가지고 있음
    {
        _followerDictionary[followerType].Initiaize(playerTransform);
        _followerDictionary[followerType].gameObject.SetActive(true);
    }
}