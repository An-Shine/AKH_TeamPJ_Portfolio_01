using UnityEngine;

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
        float moveSpeed = _moveSpeed;
        if (PlayerManager.Instance != null && PlayerManager.Instance.PlayerStatusData != null)
        {
            moveSpeed = PlayerManager.Instance.PlayerStatusData.MoveSpeed;
        }
        else if (ItemManager.Instance != null && ItemManager.Instance.PlayerStat != null)
        {
            moveSpeed *= Mathf.Max(0f, 1f + ItemManager.Instance.PlayerStat.MoveSpeedMultiplier);
        }

        _body.linearVelocity = direction * (isDashing ? _dashSpeed : moveSpeed);
    }

    public void Stop()
    {
        if (_body != null)
        {
            _body.linearVelocity = Vector2.zero;
        }
    }
}
