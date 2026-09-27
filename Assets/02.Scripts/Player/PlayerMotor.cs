using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMotor : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _moveSpeed = 5f;
    [SerializeField, Min(0f)] private float _dashSpeed = 15f;
    private Rigidbody2D _body;

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _body.gravityScale = 0f;
        _body.freezeRotation = true;
    }

    // PlayerController.FixedUpdate에서만 호출합니다.
    public void Move(Vector2 direction, bool isDashing)
    {
        _body.linearVelocity = direction * (isDashing ? _dashSpeed : _moveSpeed);
    }

    public void Stop()
    {
        if (_body != null) _body.linearVelocity = Vector2.zero;
    }
}
