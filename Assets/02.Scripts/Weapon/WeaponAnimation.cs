using UnityEngine;

[RequireComponent(typeof(Animator))]
public class WeaponAnimation : MonoBehaviour
{
    [SerializeField] private string _layerName = "Base";
    private Animator _animator;
    private readonly int[] _hashes = new int[System.Enum.GetValues(typeof(FacingDirection)).Length];
    private int _lastHash;
    private bool _hasPlayed;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _animator.applyRootMotion = false;
        for (int direction = 0; direction < _hashes.Length; direction++)
        {
            _hashes[direction] = Animator.StringToHash($"{_layerName}.{(FacingDirection)direction}");
        }
    }

    private void OnEnable()
    {
        _hasPlayed = false;
    }

    public void Refresh(FacingDirection facing)
    {
        int hash = _hashes[(int)facing];
        if (!_animator.HasState(0, hash))
        {
            return;
        }

        if (_hasPlayed && hash == _lastHash) return;

        _animator.Play(hash, 0);
        _lastHash = hash;
        _hasPlayed = true;
    }
}