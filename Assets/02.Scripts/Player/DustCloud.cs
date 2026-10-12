using UnityEngine;

public class DustCloud : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    private readonly string _indexString = "index";

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void Spawn(Vector3 position)
    {
        gameObject.SetActive(true);
        _animator.SetInteger(_indexString, UnityEngine.Random.Range(0, 3));
        transform.position = position;
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }
}