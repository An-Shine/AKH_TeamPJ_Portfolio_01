using UnityEngine;

public class OverlapSphere : MonoBehaviour
{
    [SerializeField] private float _radius = 1f;
    [SerializeField] private LayerMask _targetMask;
    [SerializeField] private float _findInterval = 0.1f;
    private Collider2D[] _targets;
    private float _timer;

    private void Start()
    {
        _timer = 0f;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _findInterval)
        {
            _timer = 0f;
            _targets = Physics2D.OverlapCircleAll(transform.position, _radius, _targetMask);
            Debug.Log($"target count : {_targets.Length}");
        }
    }

    public Collider2D GetNearestTarget()
    {
        Collider2D result = null;

        if (_targets == null || _targets.Length <= 0) return null;

        float shortestDistance = Vector3.Distance(transform.position, _targets[0].transform.position);
        float tempDistance;
        foreach (Collider2D target in _targets)
        {
            tempDistance = Vector3.Distance(transform.position, target.transform.position);
            if (shortestDistance > tempDistance)
            {
                shortestDistance = tempDistance;
                result = target;
            }
        }

        return result;
    }

    public bool ExistTarget()
    {
        if (_targets == null || _targets.Length <= 0) return false;
        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}