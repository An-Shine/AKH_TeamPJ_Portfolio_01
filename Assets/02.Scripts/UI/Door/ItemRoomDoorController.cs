using UnityEngine;
using System.Collections.Generic;

public class ItemRoomDoorController : MonoBehaviour
{
    private enum DoorState
    {
        WaitingForEntry,
        CombatLocked,
        Cleared
    }

    [SerializeField] private DoorController _doorController;
    [SerializeField] private MonsterSpawner _monsterSpawner;
    [SerializeField] private bool _startLocked;

    private static readonly List<ItemRoomDoorController> ActiveDoors = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActiveDoors()
    {
        ActiveDoors.Clear();
    }

    private void OnEnable()
    {
        if (!ActiveDoors.Contains(this)) ActiveDoors.Add(this);
    }

    private void OnDisable()
    {
        ActiveDoors.Remove(this);
    }

    private void Start()
    {
        if (_startLocked)
        {
            _state = DoorState.CombatLocked;
            _doorController.Close();
        }
    }

    private void Update()
    {
        if (_state == DoorState.Cleared) return;
        if (_monsterSpawner == null) return;

        if (!_monsterSpawner.IsComplete) return;
        if (_monsterSpawner.RemainingMonsterCount != 0) return;

        _state = DoorState.Cleared;
        _doorController.Open();
    }

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
        if (_state != DoorState.WaitingForEntry)
            return;

        foreach (var door in ActiveDoors)
        {
            if (door == this || (_monsterSpawner != null && door._monsterSpawner == _monsterSpawner))
            {
                door.LockForCombat();
            }
        }
    }

    private void LockForCombat()
    {
        if (_state == DoorState.Cleared)
            return;

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