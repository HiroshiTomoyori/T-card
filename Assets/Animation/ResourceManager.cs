using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class BridgeResourceNode : MonoBehaviour
{
    [Header("UI 構造")]
    [Tooltip("背後にうっすら表示する最後にチャージしたカード")]
    [SerializeField] private Image ghostCardImage;

    [Tooltip("横長ブリッジサイズのフレーム")]
    [SerializeField] private Image baseFrameImage;

    [Tooltip("内部の水面・液体エフェクト")]
    [SerializeField] private Image liquidWaterImage;


    [Header("タイポグラフィ")]
    [Tooltip("現在のリソース数")]
    [SerializeField] private TextMeshProUGUI currentNumberText;

    [Tooltip("最大値表示 例：/ 10")]
    [SerializeField] private TextMeshProUGUI maxInfoText;


    [Header("カラー設定")]
    [Tooltip("フレーム・グローの基本色")]
    [ColorUsage(false, true)]
    [SerializeField]
    private Color uiThemeColor =
        new Color(0.9f, 0.82f, 0.6f, 1f);

    [Tooltip("数字が増減した時のグローカラー")]
    [ColorUsage(false, true)]
    [SerializeField]
    private Color numberGlowColor =
        new Color(1f, 0.95f, 0.8f, 1f);


    [Header("横長ブリッジサイズ")]
    [Tooltip("Bridge Cardの横:縦比率。57×89mm → 約1.56")]
    [SerializeField] private float bridgeAspectRatio = 1.56f;


    [Header("アンビエントアニメーション")]
    [Tooltip("フレームが呼吸する時間")]
    [SerializeField] private float pulseDuration = 2f;

    [Tooltip("水面が上下する量")]
    [SerializeField] private float waterMoveAmount = 3f;


    [Header("リソース変化アニメーション")]
    [Tooltip("数字が跳ねる最大倍率")]
    [SerializeField] private float numberPunchScale = 1.12f;

    [Tooltip("リップルの強さ")]
    [SerializeField] private float waterRippleScale = 1.06f;

    [Tooltip("最大値増加時のフラッシュ時間")]
    [SerializeField] private float maxFlashDuration = 0.12f;


    private int currentResource = 2;
    private int maxResource = 10;

    private Sequence backgroundAnimSeq;
    private Sequence rippleAnimSeq;

    private Vector3 originalNumberScale;
    private Vector3 originalWaterScale;
    private Vector2 originalWaterPosition;


    private void Awake()
    {
        SetupInitialStyles();

        if (currentNumberText != null)
            originalNumberScale = currentNumberText.transform.localScale;

        if (liquidWaterImage != null)
        {
            originalWaterScale =
                liquidWaterImage.rectTransform.localScale;

            originalWaterPosition =
                liquidWaterImage.rectTransform.anchoredPosition;
        }

        SetupBridgeAspectRatio();
    }


    private void Start()
    {
        UpdateDisplay(
            currentResource,
            maxResource
        );

        StartAmbientAnimation();
    }


    // =========================================================
    // 初期設定
    // =========================================================

    private void SetupInitialStyles()
    {
        if (baseFrameImage != null)
        {
            baseFrameImage.color = uiThemeColor;
        }


        if (liquidWaterImage != null)
        {
            Color waterColor = uiThemeColor;
            waterColor.a = 0.4f;

            liquidWaterImage.color = waterColor;
        }


        if (currentNumberText != null)
        {
            currentNumberText.color = Color.white;

            // LoraなどのフォントマテリアルにGlowを適用
            currentNumberText.fontSharedMaterial.EnableKeyword(
                "GLOW_ON"
            );
        }


        if (maxInfoText != null)
        {
            maxInfoText.color =
                new Color(1f, 1f, 1f, 0.85f);
        }
    }


    // =========================================================
    // 横長ブリッジ比率
    // =========================================================

    private void SetupBridgeAspectRatio()
    {
        RectTransform rt = GetComponent<RectTransform>();

        if (rt == null)
            return;

        /*
         * 横長Bridge
         *
         * 57 × 89
         * ↓
         * 横向き
         *
         * 89 / 57 = 約1.56
         */

        float height = rt.rect.height;

        if (height > 0f)
        {
            rt.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                height * bridgeAspectRatio
            );
        }
    }


    // =========================================================
    // アンビエントアニメーション
    // =========================================================

    private void StartAmbientAnimation()
    {
        backgroundAnimSeq?.Kill();

        backgroundAnimSeq = DOTween.Sequence();


        // -------------------------
        // 明るくなる
        // -------------------------

        backgroundAnimSeq.Append(
            baseFrameImage
                .DOFade(0.95f, pulseDuration)
                .SetEase(Ease.InOutSine)
        );


        // 水面が少し上昇
        if (liquidWaterImage != null)
        {
            Vector2 targetPosition =
                originalWaterPosition +
                Vector2.up * waterMoveAmount;

            backgroundAnimSeq.Join(
                liquidWaterImage.rectTransform
                    .DOAnchorPos(
                        targetPosition,
                        pulseDuration
                    )
                    .SetEase(Ease.InOutSine)
            );
        }


        // -------------------------
        // 暗くなる
        // -------------------------

        backgroundAnimSeq.Append(
            baseFrameImage
                .DOFade(0.75f, pulseDuration)
                .SetEase(Ease.InOutSine)
        );


        // 水面が少し下降
        if (liquidWaterImage != null)
        {
            Vector2 targetPosition =
                originalWaterPosition -
                Vector2.up * waterMoveAmount;

            backgroundAnimSeq.Join(
                liquidWaterImage.rectTransform
                    .DOAnchorPos(
                        targetPosition,
                        pulseDuration
                    )
                    .SetEase(Ease.InOutSine)
            );
        }


        backgroundAnimSeq.SetLoops(
            -1,
            LoopType.Restart
        );
    }


    // =========================================================
    // リソース変更
    // =========================================================

    public void SetResource(
        int newCurrent,
        int newMax,
        Sprite lastCardSprite = null
    )
    {
        bool isMaxIncreased =
            newMax > maxResource;


        currentResource =
            Mathf.Clamp(
                newCurrent,
                0,
                newMax
            );

        maxResource = newMax;


        // -------------------------
        // 背後のカード
        // -------------------------

        if (
            lastCardSprite != null &&
            ghostCardImage != null
        )
        {
            ghostCardImage.sprite =
                lastCardSprite;

            ghostCardImage.gameObject.SetActive(
                true
            );

            ghostCardImage.color =
                new Color(1f, 1f, 1f, 0f);


            ghostCardImage
                .DOFade(0.2f, 0.4f)
                .SetEase(Ease.OutQuad);
        }


        // -------------------------
        // 数字更新
        // -------------------------

        UpdateDisplay(
            currentResource,
            maxResource
        );


        // -------------------------
        // アニメーション
        // -------------------------

        PlayResourceChangeAnimation(
            isMaxIncreased
        );
    }


    // =========================================================
    // 表示更新
    // =========================================================

    private void UpdateDisplay(
        int current,
        int max
    )
    {
        if (currentNumberText != null)
        {
            currentNumberText.text =
                current.ToString();
        }


        if (maxInfoText != null)
        {
            maxInfoText.text =
                $"/ {max}";
        }
    }


    // =========================================================
    // リソース変化演出
    // =========================================================

    private void PlayResourceChangeAnimation(
        bool isMaxIncreased
    )
    {
        rippleAnimSeq?.Kill();

        // 元の状態へ戻す
        if (currentNumberText != null)
        {
            currentNumberText.transform.localScale =
                originalNumberScale;
        }

        if (liquidWaterImage != null)
        {
            liquidWaterImage.rectTransform.localScale =
                originalWaterScale;
        }


        rippleAnimSeq =
            DOTween.Sequence();


        // =====================================================
        // 数字が「ポンッ」と出る
        // =====================================================

        if (currentNumberText != null)
        {
            rippleAnimSeq.Append(
                currentNumberText.transform
                    .DOScale(
                        originalNumberScale *
                        numberPunchScale,
                        0.12f
                    )
                    .SetEase(Ease.OutQuad)
            );


            rippleAnimSeq.Join(
                currentNumberText
                    .DOColor(
                        numberGlowColor,
                        0.12f
                    )
                    .SetEase(Ease.OutQuad)
            );
        }


        // =====================================================
        // 最大値が増えた場合
        // =====================================================

        if (isMaxIncreased)
        {
            PlayMaxIncreaseFlash();
        }


        // =====================================================
        // 数字が元に戻る
        // =====================================================

        if (currentNumberText != null)
        {
            rippleAnimSeq.Append(
                currentNumberText.transform
                    .DOScale(
                        originalNumberScale,
                        0.25f
                    )
                    .SetEase(Ease.OutBack)
            );


            rippleAnimSeq.Join(
                currentNumberText
                    .DOColor(
                        Color.white,
                        0.25f
                    )
            );
        }


        // =====================================================
        // 水面リップル
        // =====================================================

        if (liquidWaterImage != null)
        {
            PlayWaterRipple();
        }
    }


    // =========================================================
    // 水面リップル
    // =========================================================

    private void PlayWaterRipple()
    {
        RectTransform water =
            liquidWaterImage.rectTransform;


        water.DOKill();


        water.localScale =
            originalWaterScale;


        Sequence waterSeq =
            DOTween.Sequence();


        // 横方向に広がる
        waterSeq.Append(
            water.DOScaleX(
                originalWaterScale.x *
                waterRippleScale,
                0.15f
            )
            .SetEase(Ease.OutQuad)
        );


        // 縦方向にも少し膨らむ
        waterSeq.Join(
            water.DOScaleY(
                originalWaterScale.y * 1.03f,
                0.15f
            )
            .SetEase(Ease.OutQuad)
        );


        // 元に戻る
        waterSeq.Append(
            water.DOScale(
                originalWaterScale,
                0.3f
            )
            .SetEase(Ease.OutSine)
        );
    }


    // =========================================================
    // 最大値増加フラッシュ
    // =========================================================

    private void PlayMaxIncreaseFlash()
    {
        if (baseFrameImage == null)
            return;


        Color originalColor =
            uiThemeColor;


        // 一瞬白く光る
        rippleAnimSeq.Append(
            baseFrameImage
                .DOColor(
                    Color.white,
                    maxFlashDuration
                )
                .SetEase(Ease.OutQuad)
        );


        // 元の色へ戻る
        rippleAnimSeq.Append(
            baseFrameImage
                .DOColor(
                    originalColor,
                    0.3f
                )
                .SetEase(Ease.OutQuad)
        );
    }


    // =========================================================
    // 外部から現在値を取得
    // =========================================================

    public int GetCurrentResource()
    {
        return currentResource;
    }


    public int GetMaxResource()
    {
        return maxResource;
    }


    // =========================================================
    // 終了処理
    // =========================================================

    private void OnDestroy()
    {
        backgroundAnimSeq?.Kill();
        rippleAnimSeq?.Kill();

        if (baseFrameImage != null)
            baseFrameImage.DOKill();

        if (liquidWaterImage != null)
            liquidWaterImage.DOKill();

        if (currentNumberText != null)
            currentNumberText.DOKill();

        if (ghostCardImage != null)
            ghostCardImage.DOKill();
    }
}