using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MapRoomIconUI : MonoBehaviour
{
    [SerializeField] private Color _clearColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    private Image _iconImage;
    public bool IsCleared { get; private set; }

    private void OnEnable()
    {
        if (IsCleared) ApplyClearedColor();
    }

    public void MarkCleared()
    {
        if (IsCleared) return;

        IsCleared = true;
        ApplyClearedColor();
    }

    private void ApplyClearedColor()
    {
        if (_iconImage == null)
        {
            _iconImage = GetComponent<Image>();
        }
        _iconImage.color = _clearColor;
    }
}
