using UnityEngine;

[RequireComponent(typeof(PlayerHitFlash))]
public class PlayerDamageReceiver : MonoBehaviour
{
    private PlayerManager _playerManager;
    private PlayerHitFlash _hitFlash;

    private void Awake()
    {
        _hitFlash = GetComponent<PlayerHitFlash>();
    }

    private void Start()
    {
        _playerManager = PlayerManager.Instance;
    }

    public bool TakeDamage(float damage)
    {
        if (!isActiveAndEnabled || !_playerManager.TryTakeDamage(damage))
        {
            return false;
        }

        _hitFlash.Play();
        return true;
    }
}