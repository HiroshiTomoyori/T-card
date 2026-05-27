using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WallSlashEffect : MonoBehaviour
{
    [Header("Effect")]
    public Image slashImage;
    public float duration = 0.25f;
    public float maxScale = 1.4f;

    CanvasGroup slashGroup;

    void Awake()
    {
        if (slashImage != null)
        {
            slashGroup = slashImage.GetComponent<CanvasGroup>();
            if (slashGroup == null)
                slashGroup = slashImage.gameObject.AddComponent<CanvasGroup>();

            slashGroup.alpha = 0f;
            slashImage.transform.localScale = Vector3.zero;
        }
    }

    public IEnumerator Play()
    {
        if (slashImage == null)
            yield break;

        slashGroup.alpha = 1f;
        slashImage.transform.localScale = Vector3.zero;

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            slashImage.transform.localScale =
                Vector3.Lerp(Vector3.zero, Vector3.one * maxScale, t);

            slashGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        slashGroup.alpha = 0f;
    }
}