using System.Collections.Generic;
using UnityEngine;

public class DataCollectionArea : MonoBehaviour
{
    [SerializeField] private DataCollectionUI _dataCollectionUI;
    [SerializeField] private MapRoomIconUI _mapRoomIconUI;
    [SerializeField, InspectorName("Hacking Effect")] private GameObject _gKeyIcon;
    [SerializeField] private Vector3 _tooltipOffset = new(0f, 1.5f, 0f);

    private readonly HashSet<Collider2D> _playerColliders = new();
    private bool _isCollected;
    private bool _isTooltipOpen;

    private void OnEnable()
    {
        if (_gKeyIcon != null) _gKeyIcon.SetActive(!_isCollected);
    }

    private void Update()
    {
        if (_isCollected || _playerColliders.Count == 0) return;

        if (Time.timeScale <= 0f || PlayerManager.IsInputBlocked) return;

        if (Input.GetKeyDown(KeyCode.G))
        {
            _isCollected = true;
            _dataCollectionUI.CollectData();
            if (_gKeyIcon != null) _gKeyIcon.SetActive(false);
            if (_mapRoomIconUI != null) _mapRoomIconUI.MarkCleared();
            HidePrompt();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isCollected) return;

        if (other.GetComponentInParent<PlayerController>() == null) return;

        _playerColliders.Add(other);
        ShowPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!_playerColliders.Remove(other)) return;

        if (_playerColliders.Count == 0) HidePrompt();
    }

    private void OnDisable()
    {
        _playerColliders.Clear();

        HidePrompt();
    }

    private void ShowPrompt()
    {
        if (_isTooltipOpen || ToolTipManager.Instance == null) return;

        ToolTipManager.Instance.OpenItemTooltip(transform, _tooltipOffset, " 데이터 추출을 진행합니다.");
        _isTooltipOpen = true;
    }

    private void HidePrompt()
    {
        if (_isTooltipOpen)
        {
            if (ToolTipManager.Instance != null) ToolTipManager.Instance.CloseItemTooltip();

            _isTooltipOpen = false;
        }
    }
}
