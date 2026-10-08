using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private CameraRoomArea[] _cameraRoomAreas;
    [SerializeField] private float _smoothTime = 0.15f;

    private Camera _camera;
    private float _fixedZ;
    private Vector2 _smoothVelocity;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _fixedZ = transform.position.z;
    }

    private void LateUpdate()
    {
        if (_player == null) return;

        Vector2 targetPosition = _player.position;

        var halfHeight = _camera.orthographicSize;
        var halfWidth = halfHeight * _camera.aspect;

        if (_cameraRoomAreas != null)
        {
            foreach (var area in _cameraRoomAreas)
            {
                if (area == null) continue;

                if (!area.ContainsPlayer(_player.position)) continue;

                targetPosition = area.ClampCameraPosition(targetPosition, halfWidth, halfHeight);

                break;
            }
        }

        var nextPosition = Vector2.SmoothDamp(transform.position, targetPosition, ref _smoothVelocity, _smoothTime);
        transform.position = new Vector3(nextPosition.x, nextPosition.y, _fixedZ);
    }
}