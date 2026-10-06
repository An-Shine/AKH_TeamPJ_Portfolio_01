using UnityEngine;
using System.Collections.Generic;

public class DoorProximityTrigger : MonoBehaviour
{
    [SerializeField] private DoorController _doorController;

    private readonly HashSet<Collider2D> _playerColliders = new();

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.GetComponentInParent<PlayerController>() == null) return;
        _playerColliders.Add(collider);
        _doorController.Open();
    }

    private void OnTriggerExit2D(Collider2D collider)
    {
        if (!_playerColliders.Remove(collider)) return;

        if (_playerColliders.Count == 0) _doorController.Close();
    }
}