using UnityEngine;

public class DustCloudPool : MonoBehaviour
{
    [SerializeField] private Transform[] _clouds;
    private float[] _hideAt;

    private void Awake()
    {
        _hideAt = new float[_clouds.Length];
    }

    public void Spawn(Vector3 position)
    {
        if (!isActiveAndEnabled || _clouds.Length == 0) return;
        int index = Random.Range(0, _clouds.Length);

        _clouds[index].position = position;
        _hideAt[index] = Time.time + 0.3f;
        _clouds[index].gameObject.SetActive(true);
        return;
    }

    private void Update()
    {
        for (int i = 0; i < _clouds.Length; i++)
        {
            if (_clouds[i].gameObject.activeSelf && Time.time >= _hideAt[i])
            {
                _clouds[i].gameObject.SetActive(false);
            }
        }
    }

    // private void OnDisable()
    // {
    //     if (_clouds == null) return;
    //     foreach (Transform cloud in _clouds)
    //     {
    //         if (cloud != null) cloud.gameObject.SetActive(false);
    //     }
    // }
}