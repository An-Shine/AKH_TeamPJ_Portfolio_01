using UnityEngine;

public class RangeProjectile : MonoBehaviour
{
    [Header("투사체 설정")]
    [SerializeField] private float _moveSpeed = 8f;
    [SerializeField] private float _lifeTime = 3f;

    private Vector2 _direction;

    public void SetDirection(Vector2 direction)
    {
        _direction = direction.normalized;
    }

    private void Start()
    {
        Destroy(gameObject, _lifeTime);
    }

    private void Update()
    {
        transform.position +=
            (Vector3)(_direction * _moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("RangeMonster 투사체가 플레이어에게 적중!");

            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}