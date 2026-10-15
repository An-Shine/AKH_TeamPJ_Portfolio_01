using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Animator _doorAnimator;
    [SerializeField] private Collider2D _doorBlocker;
    [SerializeField] private AudioSource _doorOpenAudioSource;

    private bool _isOpen;

    private static readonly int ClosedHash = Animator.StringToHash("Base Layer.Door_H_Closed");
    private static readonly int OpenHash = Animator.StringToHash("Base Layer.Door_H_Open");
    private static readonly int CloseHash = Animator.StringToHash("Base Layer.Door_H_Close");

    private void Awake()
    {
        _isOpen = false;
        _doorBlocker.enabled = true;
        _doorAnimator.Play(ClosedHash, 0, 0f);
    }

    [ContextMenu("Test/Open Door")]
    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        _doorAnimator.Play(OpenHash, 0, 0f);
        _doorBlocker.enabled = false;
        if (_doorOpenAudioSource != null) _doorOpenAudioSource.Play();
    }

    [ContextMenu("Test/Close Door")]
    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;
        _doorBlocker.enabled = true;
        _doorAnimator.Play(CloseHash, 0, 0f);
    }
}
