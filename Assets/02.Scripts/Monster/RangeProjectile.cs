using UnityEngine;

public class RangeProjectile : MonoBehaviour
{
    [Header("투사체 설정")]
    [SerializeField] private float _moveSpeed = 8f;
    [SerializeField] private float _lifeTime = 3f;
    [SerializeField] private float _damage = 10f;

    private Vector2 _direction;

    public void SetDirection(Vector2 direction)
    {
        _direction = direction.normalized;

        float angle =
            Mathf.Atan2(
                _direction.y,
                _direction.x
            ) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }

    private void Start()
    {
        Destroy(gameObject, _lifeTime);
    }

    private void Update()
    {
        transform.position +=
            (Vector3)(
                _direction *
                _moveSpeed *
                Time.deltaTime
            );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log(
                $"RangeMonster 투사체가 플레이어에게 적중! 데미지: {_damage}"
            );

            Destroy(gameObject);

            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}