using UnityEngine;

public abstract class Follower : MonoBehaviour
{
    protected Transform _playerTransform;
    protected PlayerAim _playerAim;

    protected float _startAngle;
    protected float _radius;

    protected virtual void Update()
    {
        Move();
    }

    public virtual void Initiaize(Transform playerTransform, float startAngle, float radius)
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

        _startAngle = startAngle;
        _radius = radius;

        SetInitialPosition();
    }

    protected abstract void Move();
    protected abstract void SetInitialPosition();
}