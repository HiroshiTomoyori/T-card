using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class DifficultyGlow : MonoBehaviour,
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

    void FadeTo(float target)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(target));
    }

    IEnumerator FadeRoutine(float target)
    {
        Color c = glowImage.color;

        float start = c.a;
        float t = 0f;

        while (t < 0.25f)
        {
            t += Time.deltaTime;

            c.a = Mathf.Lerp(start,target,t/0.25f);
            glowImage.color = c;

            yield return null;
        }

        c.a = target;
        glowImage.color = c;
    }
}