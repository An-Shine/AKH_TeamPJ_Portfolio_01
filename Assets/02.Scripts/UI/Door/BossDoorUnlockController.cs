using UnityEngine;

public class BossDoorUnlockController : MonoBehaviour
{
    [SerializeField] private DataCollectionUI _dataCollectionUI;
    [SerializeField] private DoorController _doorController;

    private bool _isUnlocked;

    private void Update()
    {
        if (_isUnlocked) return;

        if (_dataCollectionUI.CollectedCount < _dataCollectionUI.MaxCollectCount) return;

        _isUnlocked = true;
        _doorController.Open();
    }
}