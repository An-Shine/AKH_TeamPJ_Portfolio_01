using UnityEngine;

public class ItemRoomDoorController : MonoBehaviour
{
    private enum DoorState
    {
        WaitingForEntry,
        CombatLocked,
        Cleared
    }

    [SerializeField] private DoorController _doorController;

    private DoorState _state = DoorState.WaitingForEntry;

    public void RequestOpen()
    {
        if (_state == DoorState.CombatLocked) return;
        _doorController.Open();
    }

    public void RequestClose()
    {
        if (_state == DoorState.Cleared) return;
        _doorController.Close();
    }

    public void EnterRoom()
    {
        if (_state != DoorState.WaitingForEntry) return;
        _state = DoorState.CombatLocked;
        _doorController.Close();
    }

    [ContextMenu("Test/Clear Room")]
    private void TestClearRoom()
    {
        if (!Application.isPlaying) return;
        _state = DoorState.Cleared;
        _doorController.Open();
    }
}