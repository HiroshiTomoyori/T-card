using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// HTML の .memo-item:hover を Unity uGUI で再現するスクリプト。
///
/// 再現する動き:
/// - 0.3 秒で 1.05 倍に拡大
/// - 少し上へ移動
/// - 初期の傾きを 0 度へ戻す
/// - スート記号の色・透明度を変更
/// - ドロップシャドウとグローを強くする
///
/// PC のマウスホバーと、スマホのタッチ押下の両方に対応します。
/// </summary>
[DisallowMultipleComponent]
public sealed class MemoButtonHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("References")]
    [Tooltip("通常は、このスクリプトを付けた Button 自身の RectTransform。空欄なら自動取得します。")]
    [SerializeField] private RectTransform target;

    [Tooltip("♠ ♥ ♣ ♦ を表示している Image または TextMeshProUGUI。不要なら空欄でOKです。")]
    [SerializeField] private Graphic suitGraphic;

    [Tooltip("白いぼかし画像を置いた子オブジェクトの CanvasGroup。不要なら空欄でOKです。")]
    [SerializeField] private CanvasGroup glowGroup;

    [Tooltip("ボタン本体に付けた Unity UI の Shadow。不要なら空欄でOKです。")]
    [SerializeField] private Shadow dropShadow;

    [Header("HTML hover settings")]
    [SerializeField, Min(0.01f)] private float duration = 0.3f;
    [SerializeField, Min(1f)] private float hoverScale = 1.05f;

    [Tooltip("HTML の translateY(-5px) に相当。Unity は上方向がプラスです。")]
    [SerializeField] private float hoverLift = 5f;

    [Tooltip("ホバー時のスート記号の色と透明度。スペード/クラブは黒、ハート/ダイヤは赤がおすすめです。")]
    [SerializeField] private Color hoverSuitColor = new Color(0f, 0f, 0f, 0.35f);

    [SerializeField, Range(0f, 1f)] private float hoverGlowAlpha = 0.9f;

    [Tooltip("HTML の 6px 18px の影に近い値。Unity では下方向がマイナスです。")]
    [SerializeField] private Vector2 hoverShadowDistance = new Vector2(6f, -18f);

    [SerializeField] private Color hoverShadowColor = new Color(0f, 0f, 0f, 0.3f);

    private Vector2 normalPosition;
    private Vector3 normalScale;
    private Quaternion normalRotation;
    private Color normalSuitColor;
    private float normalGlowAlpha;
    private Vector2 normalShadowDistance;
    private Color normalShadowColor;

    private Coroutine tweenCoroutine;
    private bool pointerInside;
    private bool pointerPressed;
    private bool initialized;

    private void Awake()
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }

        if (target == null)
        {
            Debug.LogError($"{nameof(MemoButtonHover)} requires a RectTransform.", this);
            enabled = false;
            return;
        }

        CacheNormalState();
        initialized = true;
    }

    private void CacheNormalState()
    {
        normalPosition = target.anchoredPosition;
        normalScale = target.localScale;
        normalRotation = target.localRotation;

        if (suitGraphic != null)
        {
            normalSuitColor = suitGraphic.color;
        }

        if (glowGroup != null)
        {
            normalGlowAlpha = glowGroup.alpha;
        }

        if (dropShadow != null)
        {
            normalShadowDistance = dropShadow.effectDistance;
            normalShadowColor = dropShadow.effectColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;

        if (!pointerPressed)
        {
            AnimateTo(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;

        if (!pointerPressed)
        {
            AnimateTo(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerPressed = true;
        AnimateTo(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerPressed = false;

        // 旧 EventSystem ではマウスの pointerId は負数、タッチは 0 以上です。
        // タッチ端末では指を離したら通常状態へ戻します。
        bool isMousePointer = eventData.pointerId < 0;

        if (!isMousePointer)
        {
            pointerInside = false;
        }

        AnimateTo(isMousePointer && pointerInside);
    }

    private void AnimateTo(bool hovered)
    {
        if (!isActiveAndEnabled || !initialized)
        {
            return;
        }

        if (tweenCoroutine != null)
        {
            StopCoroutine(tweenCoroutine);
        }

        tweenCoroutine = StartCoroutine(TweenVisuals(hovered));
    }

    private IEnumerator TweenVisuals(bool hovered)
    {
        Vector2 startPosition = target.anchoredPosition;
        Vector3 startScale = target.localScale;
        Quaternion startRotation = target.localRotation;

        Vector2 endPosition = hovered
            ? normalPosition + Vector2.up * hoverLift
            : normalPosition;

        Vector3 endScale = hovered
            ? normalScale * hoverScale
            : normalScale;

        Quaternion endRotation = hovered
            ? Quaternion.identity
            : normalRotation;

        Color startSuitColor = suitGraphic != null ? suitGraphic.color : default;
        Color endSuitColor = hovered ? hoverSuitColor : normalSuitColor;

        float startGlowAlpha = glowGroup != null ? glowGroup.alpha : 0f;
        float endGlowAlpha = hovered ? hoverGlowAlpha : normalGlowAlpha;

        Vector2 startShadowDistance = dropShadow != null
            ? dropShadow.effectDistance
            : Vector2.zero;

        Vector2 endShadowDistance = hovered
            ? hoverShadowDistance
            : normalShadowDistance;

        Color startShadowColor = dropShadow != null
            ? dropShadow.effectColor
            : default;

        Color endShadowColor = hovered
            ? hoverShadowColor
            : normalShadowColor;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            // CSS: cubic-bezier(0.25, 0.8, 0.25, 1)
            float eased = CssCubicBezier(progress, 0.25f, 0.8f, 0.25f, 1f);

            target.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, eased);
            target.localScale = Vector3.LerpUnclamped(startScale, endScale, eased);
            target.localRotation = Quaternion.SlerpUnclamped(startRotation, endRotation, eased);

            if (suitGraphic != null)
            {
                suitGraphic.color = Color.LerpUnclamped(startSuitColor, endSuitColor, eased);
            }

            if (glowGroup != null)
            {
                glowGroup.alpha = Mathf.LerpUnclamped(startGlowAlpha, endGlowAlpha, eased);
            }

            if (dropShadow != null)
            {
                dropShadow.effectDistance = Vector2.LerpUnclamped(
                    startShadowDistance,
                    endShadowDistance,
                    eased);

                dropShadow.effectColor = Color.LerpUnclamped(
                    startShadowColor,
                    endShadowColor,
                    eased);
            }

            yield return null;
        }

        target.anchoredPosition = endPosition;
        target.localScale = endScale;
        target.localRotation = endRotation;

        if (suitGraphic != null)
        {
            suitGraphic.color = endSuitColor;
        }

        if (glowGroup != null)
        {
            glowGroup.alpha = endGlowAlpha;
        }

        if (dropShadow != null)
        {
            dropShadow.effectDistance = endShadowDistance;
            dropShadow.effectColor = endShadowColor;
        }

        tweenCoroutine = null;
    }

    private void OnDisable()
    {
        if (!initialized)
        {
            return;
        }

        if (tweenCoroutine != null)
        {
            StopCoroutine(tweenCoroutine);
            tweenCoroutine = null;
        }

        pointerInside = false;
        pointerPressed = false;

        target.anchoredPosition = normalPosition;
        target.localScale = normalScale;
        target.localRotation = normalRotation;

        if (suitGraphic != null)
        {
            suitGraphic.color = normalSuitColor;
        }

        if (glowGroup != null)
        {
            glowGroup.alpha = normalGlowAlpha;
        }

        if (dropShadow != null)
        {
            dropShadow.effectDistance = normalShadowDistance;
            dropShadow.effectColor = normalShadowColor;
        }
    }

    /// <summary>
    /// CSS の cubic-bezier(x1, y1, x2, y2) を 0〜1 の進行度へ適用します。
    /// Newton 法で X に対応する曲線上の時刻を近似し、その Y を返します。
    /// </summary>
    private static float CssCubicBezier(
        float x,
        float x1,
        float y1,
        float x2,
        float y2)
    {
        x = Mathf.Clamp01(x);

        float cx = 3f * x1;
        float bx = 3f * (x2 - x1) - cx;
        float ax = 1f - cx - bx;

        float cy = 3f * y1;
        float by = 3f * (y2 - y1) - cy;
        float ay = 1f - cy - by;

        float t = x;

        for (int i = 0; i < 6; i++)
        {
            float estimatedX = ((ax * t + bx) * t + cx) * t;
            float slopeX = (3f * ax * t + 2f * bx) * t + cx;

            if (Mathf.Abs(slopeX) < 0.00001f)
            {
                break;
            }

            t -= (estimatedX - x) / slopeX;
            t = Mathf.Clamp01(t);
        }

        return ((ay * t + by) * t + cy) * t;
    }
}
