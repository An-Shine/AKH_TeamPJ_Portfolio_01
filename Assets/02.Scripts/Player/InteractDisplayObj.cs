using System;
using UnityEngine;

public class InteractDisplayObj : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _objectSprite;
    [SerializeField] private Animator _animator;
    private readonly string _triggerString = "Show";

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void Display(Sprite sprite, Vector3 position)
    {
        gameObject.SetActive(true);
        _objectSprite.sprite = sprite;
        gameObject.transform.position = new Vector3(position.x, position.y + 1, 0);
        _animator.SetTrigger(_triggerString);
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }
}