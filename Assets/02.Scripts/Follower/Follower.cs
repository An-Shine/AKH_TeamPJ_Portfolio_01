using UnityEngine;

public abstract class Follower : MonoBehaviour
{
    protected Transform _playerTransform;
    protected PlayerAim _playerAim;

    protected virtual void Update()
    {
        Move();
    }

    private void Initialize(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("Follower: player is null!");
            return;
        }

        _playerTransform = playerTransform;
        if (!playerTransform.gameObject.TryGetComponent(out _playerAim))
        {
            Debug.LogWarning("Follower: can not find player aim!");
        }
    }

    protected abstract void Move();
}