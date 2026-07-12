using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// HTML の以下のホバー動作を Unity UI で再現します。
///
/// transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1);
/// transform: scale(1.05) rotate(0deg) translateY(-5px);
/// z-index: 10;
///
/// Unity UI では Y の正方向が上なので、translateY(-5px) は +5 として扱います。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HtmlLikeButtonHover :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("HTML と同じ基本設定")]
    [SerializeField, Min(0.01f)]
    private float duration = 0.3f;

    [SerializeField, Min(0.01f)]
    private float hoverScale = 1.05f;

    [Tooltip("HTML の translateY(-5px) に相当。Unity UIでは上方向がプラスです。")]
    [SerializeField]
    private float hoverLift = 5f;

    [Header("入力")]
    [Tooltip("スマホでも押している間だけホバー状態にします。")]
    [SerializeField]
    private bool useTouchPressAsHover = true;

    [Tooltip("Time.timeScale が 0 でもアニメーションさせます。")]
    [SerializeField]
    private bool useUnscaledTime = true;

    private RectTransform rectTransform;

    private Vector2 normalPosition;
    private Vector3 normalScale;
    private Quaternion normalRotation;
    private int normalSiblingIndex;

    private Coroutine animationRoutine;
    private bool pointerInside;
    private bool pointerPressed;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        normalPosition = rectTransform.anchoredPosition;
        normalScale = rectTransform.localScale;
        normalRotation = rectTransform.localRotation;
        normalSiblingIndex = rectTransform.GetSiblingIndex();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        UpdateHoverState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        UpdateHoverState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!useTouchPressAsHover)
            return;

        pointerPressed = true;
        UpdateHoverState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!useTouchPressAsHover)
            return;

        pointerPressed = false;
        UpdateHoverState();
    }

    private void UpdateHoverState()
    {
        bool shouldHover = pointerInside || (useTouchPressAsHover && pointerPressed);
        AnimateTo(shouldHover);
    }

    private void AnimateTo(bool hover)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        if (hover)
        {
            // CSS の z-index: 10 に近い動き。
            rectTransform.SetAsLastSibling();
        }

        Vector2 targetPosition = hover
            ? normalPosition + new Vector2(0f, hoverLift)
            : normalPosition;

        Vector3 targetScale = hover
            ? normalScale * hoverScale
            : normalScale;

        Quaternion targetRotation = hover
            ? Quaternion.identity
            : normalRotation;

        animationRoutine = StartCoroutine(
            AnimateRoutine(
                targetPosition,
                targetScale,
                targetRotation,
                restoreSiblingIndexAfterAnimation: !hover
            )
        );
    }

    private IEnumerator AnimateRoutine(
        Vector2 targetPosition,
        Vector3 targetScale,
        Quaternion targetRotation,
        bool restoreSiblingIndexAfterAnimation)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector3 startScale = rectTransform.localScale;
        Quaternion startRotation = rectTransform.localRotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float linearTime = Mathf.Clamp01(elapsed / duration);

            // CSS: cubic-bezier(0.25, 0.8, 0.25, 1)
            float easedTime = EvaluateCubicBezier(
                linearTime,
                0.25f,
                0.80f,
                0.25f,
                1.00f
            );

            rectTransform.anchoredPosition =
                Vector2.LerpUnclamped(startPosition, targetPosition, easedTime);

            rectTransform.localScale =
                Vector3.LerpUnclamped(startScale, targetScale, easedTime);

            rectTransform.localRotation =
                Quaternion.SlerpUnclamped(startRotation, targetRotation, easedTime);

            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        rectTransform.localScale = targetScale;
        rectTransform.localRotation = targetRotation;

        if (restoreSiblingIndexAfterAnimation)
        {
            int maximumIndex = rectTransform.parent != null
                ? rectTransform.parent.childCount - 1
                : 0;

            rectTransform.SetSiblingIndex(
                Mathf.Clamp(normalSiblingIndex, 0, maximumIndex)
            );
        }

        animationRoutine = null;
    }

    /// <summary>
    /// CSS の cubic-bezier(x1, y1, x2, y2) を評価します。
    /// 入力 progress は x 軸の進行度で、戻り値は y 軸の進行度です。
    /// </summary>
    private static float EvaluateCubicBezier(
        float progress,
        float x1,
        float y1,
        float x2,
        float y2)
    {
        progress = Mathf.Clamp01(progress);

        // x(t) = progress となる t を二分探索します。
        float lower = 0f;
        float upper = 1f;
        float t = progress;

        for (int i = 0; i < 16; i++)
        {
            t = (lower + upper) * 0.5f;
            float x = CubicBezierCoordinate(t, x1, x2);

            if (x < progress)
                lower = t;
            else
                upper = t;
        }

        return CubicBezierCoordinate(t, y1, y2);
    }

    private static float CubicBezierCoordinate(float t, float control1, float control2)
    {
        float inverse = 1f - t;

        return
            3f * inverse * inverse * t * control1 +
            3f * inverse * t * t * control2 +
            t * t * t;
    }

    private void OnDisable()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (rectTransform == null)
            return;

        rectTransform.anchoredPosition = normalPosition;
        rectTransform.localScale = normalScale;
        rectTransform.localRotation = normalRotation;

        if (rectTransform.parent != null)
        {
            rectTransform.SetSiblingIndex(
                Mathf.Clamp(
                    normalSiblingIndex,
                    0,
                    rectTransform.parent.childCount - 1
                )
            );
        }

        pointerInside = false;
        pointerPressed = false;
    }
}
