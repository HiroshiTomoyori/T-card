using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class StartButtonGlow : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public Image glowImage;

    Coroutine fadeRoutine;

    public void OnPointerEnter(PointerEventData eventData)
    {
        FadeTo(0.45f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        FadeTo(0f);
    }

    void FadeTo(float targetAlpha)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    IEnumerator FadeRoutine(float target)
    {
        Color c = glowImage.color;

        float start = c.a;
        float time = 0f;
        float duration = 0.25f;

        while (time < duration)
        {
            time += Time.deltaTime;

            c.a = Mathf.Lerp(start, target, time / duration);
            glowImage.color = c;

            yield return null;
        }

        c.a = target;
        glowImage.color = c;
    }
}