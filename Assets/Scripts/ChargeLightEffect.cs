using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChargeLightEffect : MonoBehaviour
{
    public Image lightImage;

    public float duration = 0.6f;
    public float maxHeight = 420f;
    public float maxWidth = 120f;

    RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();

        if (lightImage == null)
            lightImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        StartCoroutine(PlayEffect());
    }

    IEnumerator PlayEffect()
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float rate = Mathf.Clamp01(t / duration);

            float height = Mathf.Lerp(20f, maxHeight, rate);
            float width = Mathf.Lerp(20f, maxWidth, rate);

            rt.sizeDelta = new Vector2(width, height);

            if (lightImage != null)
            {
                Color c = lightImage.color;
                c.a = Mathf.Lerp(0.9f, 0f, rate);
                lightImage.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}