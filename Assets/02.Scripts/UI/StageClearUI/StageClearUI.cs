using TMPro;
using UnityEngine;

public class StageClearUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _playTimeText;
    [SerializeField] private GameObject _stageClearUI;
    [SerializeField] private Animator _stageClearAnimator;

    private float _elapsedSeconds;
    private bool _isTimeRecorded;
    private static readonly int WindowInHash = Animator.StringToHash("Base Layer.WindowIn");

    public bool IsBossDie { get; set; }
    public float PlayTimeSeconds => _elapsedSeconds;

    private void Update()
    {
        if (_isTimeRecorded) return;

        if (IsBossDie)
        {
            RecordPlayTime();
            _stageClearUI.SetActive(true);
            _stageClearAnimator.Play(WindowInHash, 0, 0f);
            return;
        }

        _elapsedSeconds += Time.deltaTime;
    }

    public void RecordPlayTime()
    {
        if (_isTimeRecorded) return;

        IsBossDie = true;
        _isTimeRecorded = true;

        var totalSeconds = Mathf.FloorToInt(_elapsedSeconds);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
    }
}