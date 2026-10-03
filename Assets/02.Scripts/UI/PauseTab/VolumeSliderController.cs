using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSliderController : MonoBehaviour
{
    [SerializeField] private Slider _volumeSlider;

    private void OnEnable()
    {
        if (AudioManager.Instance == null) return;
        _volumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
        // 슬라이더의 On Value Changed 이벤트 불필요하게 실행되는것을 막기위함
        _volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
    }

    private void OnDisable()
    {
        _volumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
    }

    private void HandleVolumeChanged(float volume)
    {
        if (AudioManager.Instance == null) return;

        AudioManager.Instance.SetMasterVolume(volume);
    }
}