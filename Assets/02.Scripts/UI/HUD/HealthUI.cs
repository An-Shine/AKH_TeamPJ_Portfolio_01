using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private TMP_Text _healthText;

    private float _lastCurrentHP = -1f;
    private float _lastMaxHP = -1f;

    public void SetHealthData(float currentHP, float maxHP)
    {
        if (Mathf.Approximately(_lastCurrentHP, currentHP) && Mathf.Approximately(_lastMaxHP, maxHP)) return;

        _lastCurrentHP = currentHP;
        _lastMaxHP = maxHP;

        _healthSlider.maxValue = maxHP;
        _healthSlider.value = currentHP;

        _healthText.text = $"{Mathf.RoundToInt(currentHP)} / {Mathf.RoundToInt(maxHP)}";
    }
}