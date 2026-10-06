using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnchantPanelController : MonoBehaviour
{
    private static readonly int WindowInHash = Animator.StringToHash("Base Layer.Window In");

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        _animator.Play(WindowInHash, 0, 1f);
    }
}