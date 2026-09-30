using UnityEngine;

public class PressAnyKeyController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode _startKey = KeyCode.Space;

    [Header("Scene")]
    [SerializeField] private string _mainMeunuSceneName = "03.MainMenu";

    private bool _hasRequestedTransition;

    private void Update()
    {
        if (_hasRequestedTransition) return;

        if (!Input.GetKeyDown(_startKey)) return;
        _hasRequestedTransition = true;
        SceneFlowManager.Instance.LoadScene(_mainMeunuSceneName);
    }
}