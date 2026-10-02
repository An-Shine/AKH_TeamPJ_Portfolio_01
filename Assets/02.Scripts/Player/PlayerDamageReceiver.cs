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

#if UNITY_EDITOR
    [SerializeField, Min(0f)] private float _testDamage = 10f;

    [ContextMenu("Take Damage (Test)")]
    private void TestTakeDamage()
    {
        if (!Application.isPlaying || _playerManager == null || _hitFlash == null)
        {
            Debug.LogWarning("플레이 모드에서 플레이어 초기화가 완료된 후 실행하세요.", this);
            return;
        }

        TakeDamage(_testDamage);
    }
#endif

    public bool TakeDamage(float damage)
    {
        if (!_playerManager.TryTakeDamage(damage))
        {
            return false;
        }

        _hitFlash.Play();
        return true;
    }
}