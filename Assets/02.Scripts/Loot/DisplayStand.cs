using System.Collections;
using UnityEngine;

public class DisplayStand : MonoBehaviour
{
    [SerializeField] private Material _shaderMaterial;
    private Material _runtimeMaterial;

    private void Awake()
    {
        _runtimeMaterial = Instantiate(_shaderMaterial);
        gameObject.GetComponent<SpriteRenderer>().material = _runtimeMaterial;
    }

    public void StartAppearance()
    {
        StartCoroutine(Appearance());
    }

    private IEnumerator Appearance()
    {
        float fadeValue = 0f;
        float t = 0f;
        float duration = 3f;
        _runtimeMaterial.SetFloat("_FullGlowDissolveFade", fadeValue);

        while (t < 3f)
        {
            t += Time.deltaTime;
            fadeValue = Mathf.SmoothStep(0f, 6f, t / duration);
            _runtimeMaterial.SetFloat("_FullGlowDissolveFade", fadeValue);

            yield return null;
        }
    }
}