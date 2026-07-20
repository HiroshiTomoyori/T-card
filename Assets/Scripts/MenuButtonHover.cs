using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Hover Shadows")]
    public GameObject shadow;
    public GameObject shadow2;
    public GameObject shadow3;

    [Header("Rotation")]
    public float normalRotationZ = -2f;
    public float hoverRotationZ = 0f;
    public float rotateSpeed = 8f;

    [Header("Button Scale")]
    [Tooltip("通常時のボタン全体の大きさ")]
    public float normalButtonScale = 1f;

    [Tooltip("カーソルON時のボタン全体の大きさ")]
    public float hoverButtonScale = 1.05f;

    [Tooltip("ボタンの拡大・縮小速度")]
    public float buttonScaleSpeed = 6f;

    [Header("Button Y Movement")]
    [Tooltip("カーソルON時にY軸方向へ移動する距離")]
    public float hoverMoveY = 10f;

    [Tooltip("ボタンが移動する速度")]
    public float buttonMoveSpeed = 6f;

    [Header("Text Object")]
    [Tooltip("PlayText、CustomText、OptionText、CreditsTextなどを入れる")]
    public RectTransform textObject;

    [Header("Text Scale")]
    [Tooltip("通常時の文字サイズ")]
    public float normalTextScale = 1f;

    [Tooltip("カーソルON時の文字サイズ")]
    public float hoverTextScale = 1.1f;

    [Tooltip("文字サイズが変化する速さ")]
    public float textScaleSpeed = 6f;

    [Header("Suit Image")]
    [Tooltip("スペード、ハート、クラブ、ダイヤのImageを入れる")]
    public Image suitImage;

    [Header("Suit Alpha")]
    [Range(0f, 1f)]
    [Tooltip("通常時のスートの濃さ")]
    public float normalSuitAlpha = 0.15f;

    [Range(0f, 1f)]
    [Tooltip("カーソルON時のスートの濃さ")]
    public float hoverSuitAlpha = 0.55f;

    [Tooltip("スートの濃さが変化する速さ")]
    public float suitFadeSpeed = 1.5f;

    RectTransform buttonRectTransform;

    Vector2 normalButtonPosition;
    Vector2 targetButtonPosition;

    float targetRotationZ;
    Vector3 targetButtonScale;
    Vector3 targetTextScale;
    float targetSuitAlpha;

    void Awake()
    {
        buttonRectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (buttonRectTransform != null)
        {
            // 最初の位置を通常位置として記録
            normalButtonPosition =
                buttonRectTransform.anchoredPosition;

            targetButtonPosition =
                normalButtonPosition;

            buttonRectTransform.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    normalRotationZ
                );

            buttonRectTransform.localScale =
                Vector3.one * normalButtonScale;
        }

        targetRotationZ = normalRotationZ;
        targetButtonScale =
            Vector3.one * normalButtonScale;

        targetTextScale =
            Vector3.one * normalTextScale;

        targetSuitAlpha =
            normalSuitAlpha;

        if (textObject != null)
        {
            textObject.localScale =
                Vector3.one * normalTextScale;
        }

        SetSuitAlpha(normalSuitAlpha);
        SetShadows(false);
    }

    void Update()
    {
        UpdateRotation();
        UpdateButtonScale();
        UpdateButtonPosition();
        UpdateTextScale();
        UpdateSuitAlpha();
    }

    void UpdateRotation()
    {
        if (buttonRectTransform == null)
            return;

        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                0f,
                targetRotationZ
            );

        buttonRectTransform.localRotation =
            Quaternion.Lerp(
                buttonRectTransform.localRotation,
                targetRotation,
                Time.deltaTime * rotateSpeed
            );
    }

    void UpdateButtonScale()
    {
        if (buttonRectTransform == null)
            return;

        buttonRectTransform.localScale =
            Vector3.Lerp(
                buttonRectTransform.localScale,
                targetButtonScale,
                Time.deltaTime * buttonScaleSpeed
            );
    }

    void UpdateButtonPosition()
    {
        if (buttonRectTransform == null)
            return;

        buttonRectTransform.anchoredPosition =
            Vector2.Lerp(
                buttonRectTransform.anchoredPosition,
                targetButtonPosition,
                Time.deltaTime * buttonMoveSpeed
            );
    }

    void UpdateTextScale()
    {
        if (textObject == null)
            return;

        textObject.localScale =
            Vector3.Lerp(
                textObject.localScale,
                targetTextScale,
                Time.deltaTime * textScaleSpeed
            );
    }

    void UpdateSuitAlpha()
    {
        if (suitImage == null)
            return;

        Color suitColor = suitImage.color;

        suitColor.a =
            Mathf.MoveTowards(
                suitColor.a,
                targetSuitAlpha,
                suitFadeSpeed * Time.deltaTime
            );

        suitImage.color = suitColor;
    }

    public void OnPointerEnter(
        PointerEventData eventData
    )
    {
        targetRotationZ = hoverRotationZ;

        targetButtonScale =
            Vector3.one * hoverButtonScale;

        targetButtonPosition =
            normalButtonPosition +
            new Vector2(0f, hoverMoveY);

        targetTextScale =
            Vector3.one * hoverTextScale;

        targetSuitAlpha =
            hoverSuitAlpha;

        SetShadows(true);
    }

    public void OnPointerExit(
        PointerEventData eventData
    )
    {
        targetRotationZ = normalRotationZ;

        targetButtonScale =
            Vector3.one * normalButtonScale;

        targetButtonPosition =
            normalButtonPosition;

        targetTextScale =
            Vector3.one * normalTextScale;

        targetSuitAlpha =
            normalSuitAlpha;

        SetShadows(false);
    }

    void SetSuitAlpha(float alpha)
    {
        if (suitImage == null)
            return;

        Color suitColor = suitImage.color;
        suitColor.a = alpha;
        suitImage.color = suitColor;
    }

    void SetShadows(bool isVisible)
    {
        if (shadow != null)
            shadow.SetActive(isVisible);

        if (shadow2 != null)
            shadow2.SetActive(isVisible);

        if (shadow3 != null)
            shadow3.SetActive(isVisible);
    }
}