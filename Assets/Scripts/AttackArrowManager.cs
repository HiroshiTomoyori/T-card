using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AttackArrowManager : MonoBehaviour
{
    [Header("Parts")]
    public RectTransform arrowRoot;
    public RectTransform arrowBody;
    public RectTransform arrowHead;

    public Image bodyImage;
    public Image headImage;

    [Header("Start Offset")]
    public float startXOffset = 0f;
    public float startYOffset = -40f;

    [Header("End Offset")]
    public float endXOffset = 0f;
    public float endYOffset = 40f;

    [Header("Length")]
    public float lengthOffset = 0f;

    [Header("Animation")]
    public float growTime = 0.12f;

    [Header("Head Overlap")]
    public float headOverlap = 65f;

    Coroutine growRoutine;

    readonly Color playerArrowColor =
        new Color(0.55f, 1f, 0.55f, 1f);

    Color currentPlayerArrowColor =
        new Color(0.55f, 1f, 0.55f, 1f);

    void Start()
    {
        Hide();
    }

    public void ShowArrow(RectTransform from, RectTransform to)
    {
        ShowArrow(from, to, Color.red);
    }

    public void ShowArrow(
        RectTransform from,
        RectTransform to,
        Color color
    )
    {
        // Retain the API for existing combat routines; arrow visuals are retired.
        Hide();
    }

    public void ShowEnemyArrow(
        RectTransform from,
        RectTransform to
    )
    {
        ShowArrow(from, to, Color.red);
    }

    public void ShowPlayerArrow(
        RectTransform from,
        RectTransform to
    )
    {
        ShowArrow(
            from,
            to,
            currentPlayerArrowColor
        );
    }

    IEnumerator GrowArrow(float targetLength)
    {
        float timer = 0f;

        while (timer < growTime)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / growTime
                );

            float currentLength =
                Mathf.Lerp(
                    0f,
                    targetLength,
                    t
                );

            ApplyLength(currentLength);

            yield return null;
        }

        ApplyLength(targetLength);
    }

    void ApplyLength(float distance)
    {
        float bodyLength =
            Mathf.Max(
                0f,
                distance + lengthOffset
            );

        arrowBody.sizeDelta =
            new Vector2(
                bodyLength,
                arrowBody.sizeDelta.y
            );

        arrowBody.anchoredPosition =
            Vector2.zero;

        arrowHead.anchoredPosition =
            new Vector2(
                bodyLength - headOverlap,
                0f
            );
    }

    public void Hide()
    {
        if (growRoutine != null)
        {
            StopCoroutine(growRoutine);
            growRoutine = null;
        }

        if (arrowRoot != null)
            arrowRoot.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    public void HideArrow()
    {
        Hide();
    }

    public void SetArrowColor(Color color)
    {
        if (bodyImage != null)
            bodyImage.color = color;

        if (headImage != null)
            headImage.color = color;
    }

    public void SetPlayerArrowColor(Color color)
    {
        currentPlayerArrowColor = color;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            gameObject.SetActive(true);

            if (arrowRoot != null)
                arrowRoot.gameObject.SetActive(true);

            if (arrowBody != null)
                arrowBody.gameObject.SetActive(true);

            if (arrowHead != null)
                arrowHead.gameObject.SetActive(true);

            transform.SetAsLastSibling();

            arrowRoot.anchoredPosition =
                Vector2.zero;

            arrowRoot.localRotation =
                Quaternion.identity;

            arrowBody.anchoredPosition =
                Vector2.zero;

            arrowBody.sizeDelta =
                new Vector2(300f, 20f);

            arrowHead.anchoredPosition =
                new Vector2(
                    300f - headOverlap,
                    0f
                );

            arrowHead.sizeDelta =
                new Vector2(40f, 40f);

            SetArrowColor(Color.red);

            Debug.Log("矢印テスト表示");
        }
    }
}