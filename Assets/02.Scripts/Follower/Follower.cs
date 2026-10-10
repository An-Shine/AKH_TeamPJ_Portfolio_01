using UnityEngine;

public abstract class Follower : MonoBehaviour
{
    [SerializeField] protected float _speedScalar;
    [SerializeField] private LayerMask _blockingLayer;
    [SerializeField] private float _teleportThreshold;

    protected Transform _playerTransform;

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
        _startAngle = startAngle;
        _radius = radius;

        SetInitialPosition();
    }

    protected bool CanMove(Vector3 direction, float distance)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction,
            distance,
            _blockingLayer);
        return hit.collider == null;
    }

    protected bool CheckTeleportDistance(float distance)
    {
        return distance >= _teleportThreshold;
    }

    protected abstract void Move();
    protected abstract void SetInitialPosition();
}