using UnityEngine;

public class KamikazeMonster : NormalMonster
{
    [Header("자폭 설정")]
    [SerializeField] private float _explosionRange = 1.5f;
    [SerializeField] private float _explosionDelay = 1f;
    [SerializeField] private float _explosionDamage = 50f;

    private bool _isExploding;
    private float _explosionTimer;

    protected override void Update()
    {
        base.Update();

        if (_player == null)
        {
            return;
        }

        if (_isExploding)
        {
            UpdateExplosion();
            return;
        }

        MoveToPlayer();

        float distanceToPlayer = Vector2.Distance(
            transform.position,
            _player.position
        );

        if (distanceToPlayer <= _explosionRange)
        {
            StartExplosion();
        }
    }

    private void MoveToPlayer()
    {
        Vector2 direction =
            (_player.position - transform.position).normalized;

        transform.position +=
            (Vector3)(direction * _moveSpeed * Time.deltaTime);
    }

    private void StartExplosion()
    {
        _isExploding = true;
        _explosionTimer = _explosionDelay;

        Debug.Log($"{gameObject.name}: 자폭 준비!");
    }

    private void UpdateExplosion()
    {
        _explosionTimer -= Time.deltaTime;

        if (_explosionTimer > 0f)
        {
            return;
        }

        Explode();
    }

    private void Explode()
    {
        Debug.Log(
            $"{gameObject.name}: 자폭! 폭발 데미지 {_explosionDamage}"
        );

        Destroy(gameObject);
    }
}