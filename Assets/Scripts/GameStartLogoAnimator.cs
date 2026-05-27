using System.Collections;
using UnityEngine;

public class GameStartLogoAnimator : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    [Header("SE")]
    public AudioSource seSource;
    public AudioClip gameStartSE;

    public float showTime = 1.5f;
    public float fadeTime = 0.8f;

    void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        gameObject.SetActive(false);
    }

    public void Play()
    {
        gameObject.SetActive(true);

        StopAllCoroutines();
        StartCoroutine(PlayRoutine());
    }

    IEnumerator PlayRoutine()
    {
        // 完全表示
        canvasGroup.alpha = 1f;

            // SE再生
        if(seSource != null && gameStartSE != null)
        {
            seSource.PlayOneShot(gameStartSE);
        }

        // 中央で停止
        yield return new WaitForSeconds(showTime);

        // フェードアウト
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;

            float rate =
                Mathf.Clamp01(
                    t / fadeTime
                );

            canvasGroup.alpha =
                1f - rate;

            yield return null;
        }

        canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
    }
}