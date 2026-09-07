using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class CardGlow : MonoBehaviour
{
    [Header("Glow")]
    public Image glowImage;

    public Color glowColor =
        new Color(0f, 0.82f, 1f, 1f);

    [Header("Pulse")]
    public float pulseDuration = 1.5f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 0.75f;

    [Header("Size")]
    public float glowPadding = 10f;

    private Tween pulseTween;
    private RectTransform rectTransform;


    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if(glowImage == null)
        {
            glowImage = GetComponent<Image>();
        }

        SetupGlowRect();

        // 最初は必ずOFF
        StopGlow();
    }


    void Start()
    {
        // 自動では光らせない
    }


    void SetupGlowRect()
    {
        if(glowImage == null)
            return;

        RectTransform glowRt =
            glowImage.GetComponent<RectTransform>();

        if(glowRt == null)
            return;

        glowRt.anchorMin = Vector2.zero;
        glowRt.anchorMax = Vector2.one;
        glowRt.pivot = new Vector2(0.5f, 0.5f);

        glowRt.sizeDelta =
            new Vector2(
                glowPadding * 2f,
                glowPadding * 2f
            );

        glowRt.anchoredPosition = Vector2.zero;
    }


    // ========================================
    // Glow ON
    // ========================================

    public void PlayGlow()
    {
        if(glowImage == null)
            return;

        pulseTween?.Kill();

        glowImage.color = glowColor;

        glowImage.canvasRenderer.SetAlpha(maxAlpha);

        pulseTween =
            glowImage
                .DOFade(
                    minAlpha,
                    pulseDuration
                )
                .SetLoops(
                    -1,
                    LoopType.Yoyo
                )
                .SetEase(
                    Ease.InOutSine
                );
    }


    // ========================================
    // Glow OFF
    // ========================================

    public void StopGlow()
    {
        pulseTween?.Kill();
        pulseTween = null;

        if(glowImage == null)
            return;

        glowImage.canvasRenderer.SetAlpha(0f);
    }


    // ========================================
    // Change Color
    // ========================================

    public void ChangeColorSmoothly(Color newColor)
    {
        if(glowImage == null)
            return;

        pulseTween?.Kill();

        glowImage
            .DOColor(
                newColor,
                0.2f
            )
            .OnComplete(() =>
            {
                glowColor = newColor;
                PlayGlow();
            });
    }


    // ========================================
    // Disable
    // ========================================

    void OnDisable()
    {
        pulseTween?.Kill();
        pulseTween = null;

        if(glowImage != null)
        {
            glowImage.canvasRenderer.SetAlpha(0f);
        }
    }


    // ========================================
    // Destroy
    // ========================================

    void OnDestroy()
    {
        pulseTween?.Kill();
    }
}
