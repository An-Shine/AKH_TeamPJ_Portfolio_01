using UnityEngine;

public class LaserAnimationEventReceiver : MonoBehaviour
{
    private LaserProjectile _laser;

    private void Awake()
    {
        _laser = GetComponentInParent<LaserProjectile>();
    }

    public void OnEndAnimationFinished()
    {
        if (_laser == null) return;
        _laser.OnEndAnimationFinished();
    }
}