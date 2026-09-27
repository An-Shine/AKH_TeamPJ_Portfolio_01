using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimation : MonoBehaviour
{
    private enum Motion { Idle, Run, Dash, Attack, RunAttack }
    [SerializeField] private string _layerName = "Base Layer";
    private Animator _animator;
    private readonly int[,] _hashes = new int[5, 6];
    private readonly HashSet<int> _warnedMissing = new HashSet<int>();
    private int _lastHash;
    private int _lastShotSequence;
    private Motion _lastMotion;
    private bool _hasPlayed;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _animator.applyRootMotion = false;
        for (int m = 0; m < 5; m++)
            for (int d = 0; d < 6; d++)
                _hashes[m, d] = Animator.StringToHash(
                    $"{_layerName}.{(Motion)m}_{(FacingDirection)d}");
    }

    private void OnEnable() => _hasPlayed = false;

    public void Refresh(PlayerState state, FacingDirection facing,
                        bool isAttacking, int shotSequence)
    {
        Motion locomotion = state == PlayerState.Dash ? Motion.Dash
                          : state == PlayerState.Run ? Motion.Run : Motion.Idle;
        Motion motion = locomotion;
        if (isAttacking && state != PlayerState.Dash)
            motion = state == PlayerState.Run ? Motion.RunAttack : Motion.Attack;

        int hash = _hashes[(int)motion, (int)facing];
        // 공격 전용 상태가 없으면 현재 이동 애니메이션을 유지합니다.
        if (!_animator.HasState(0, hash) && motion != locomotion)
        {
            motion = locomotion;
            hash = _hashes[(int)motion, (int)facing];
        }
        if (!_animator.HasState(0, hash))
        {
            if (_warnedMissing.Add(hash))
                Debug.LogWarning($"Animator State가 없습니다: {_layerName}.{motion}_{facing}", this);
            return;
        }

        bool attackMotion = motion == Motion.Attack || motion == Motion.RunAttack;
        bool newShot = shotSequence != _lastShotSequence;
        _lastShotSequence = shotSequence;
        if (_hasPlayed && hash == _lastHash && !(attackMotion && newShot)) return;

        float normalizedTime = 0f;
        if (_hasPlayed && motion == _lastMotion && !(attackMotion && newShot))
        {
            AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(0);
            normalizedTime = info.loop ? Mathf.Repeat(info.normalizedTime, 1f)
                                       : Mathf.Clamp01(info.normalizedTime);
        }
        _animator.Play(hash, 0, normalizedTime);
        _lastHash = hash;
        _lastMotion = motion;
        _hasPlayed = true;
    }
}
