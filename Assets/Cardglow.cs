using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class CardGlow : MonoBehaviour
{
    [Header("コンポーネント")]
    [Tooltip("発光を描画するImage（カードの背後に配置し、少しだけ大きくする）")]
    [SerializeField] private Image glowImage;

    [Header("発光設定")]
    [Tooltip("現在の発光カラー")]
    [SerializeField] private Color glowColor = new Color(0f, 0.82f, 1f, 1f);

    [Tooltip("明滅（パルス）の1サイクルの時間（秒）")]
    [SerializeField] private float pulseDuration = 1.5f;

    [Tooltip("発光の最小・最大不透明度（控えめに設定して上品さを出す）")]
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 0.75f;

    [Header("サイズ調整（タイト化）")]
    [Tooltip("カード本体（225x350）からどれくらい外側に光をはみ出させるか（ピクセル）")]
    [SerializeField] private float glowPadding = 10f;

    private Tween pulseTween;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (glowImage == null)
        {
            glowImage = GetComponent<Image>();
        }

        // プレハブの構造上、グロー用Imageのサイズをカード本体に追従させつつ、適度なパディングを持たせる
        SetupGlowRect();
    }

    void Start()
    {
        PlayGlow(glowColor);
    }

    /// <summary>
    /// グローのRectTransformをカード本体＋パディングのサイズに自動調整する
    /// </summary>
    private void SetupGlowRect()
    {
        if (glowImage != null)
        {
            RectTransform glowRect = glowImage.rectTransform;
            // アンカーを親（カードRoot）の全方位ストレッチに設定
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            // 上下左右に padding 分だけ広げる（マイナス指定ではなくプラスに広げるためサイズを調整）
            glowRect.sizeDelta = new Vector2(glowPadding * 2f, glowPadding * 2f);
            glowRect.anchoredPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// 指定した色でグローを発動・変更する
    /// </summary>
    public void PlayGlow(Color newColor)
    {
        glowColor = newColor;
        pulseTween?.Kill();

        if (glowImage != null)
        {
            Color startColor = glowColor;
            startColor.a = maxAlpha;
            glowImage.color = startColor;

            // 呼吸するような明滅ループ（不透明度のみを変化させ、サイズは変えないことで隣のカードへ干渉を防ぐ）
            pulseTween = glowImage.DOFade(minAlpha, pulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    /// <summary>
    /// スムーズなカラー変更
    /// </summary>
    public void ChangeColorSmoothly(Color targetColor, float duration = 0.5f)
    {
        glowColor = targetColor;

        if (glowImage != null)
        {
            pulseTween?.Kill();
            glowImage.DOColor(targetColor, duration).OnComplete(() => {
                PlayGlow(targetColor);
            });
        }
    }

    void OnDestroy()
    {
        pulseTween?.Kill();
    }
}