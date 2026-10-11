using UnityEngine;

public class ItemApplyEffect : MonoBehaviour
{
    protected virtual void DestroyEffect()
    {
        Destroy(gameObject);
    }
}