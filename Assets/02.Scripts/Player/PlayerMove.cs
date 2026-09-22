using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(LineRenderer))]
public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;

    private Rigidbody2D _rigidBody;
    private Vector2 _moveInput;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        MoveInput();
        RotateToMouse();
    }

    private void FixedUpdate()
    {
        _rigidBody.linearVelocity = _moveInput * _moveSpeed;
    }

    private void MoveInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        _moveInput = new Vector2(x, y).normalized;
    }

    private void RotateToMouse()
    {
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector2 direction = mouseWorldPos - transform.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 스프라이트가 기본적으로 오른쪽을 바라보면 그대로 사용
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 만약 스프라이트 기본 방향이 "위쪽"이면 아래처럼 사용
        // transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;
        return mouseWorldPos;
    }
}