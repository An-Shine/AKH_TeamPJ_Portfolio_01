using UnityEngine;

public class MapUIController : MonoBehaviour
{
    [SerializeField] private GameObject _fullMapPanel;
    [SerializeField] private GameObject _miniMapUI;

    private void Start()
    {
        _fullMapPanel.SetActive(false);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;

        SetFullMapOpen(!_fullMapPanel.activeSelf);
    }

    private void SetFullMapOpen(bool isOpen)
    {
        _fullMapPanel.SetActive(isOpen);
        _miniMapUI.SetActive(!isOpen);
    }
}