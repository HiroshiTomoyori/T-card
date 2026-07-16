using UnityEngine;
using UnityEngine.EventSystems;

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

    [Header("Text Object")]
    [Tooltip("PlayText、CustomText、OptionText、CreditsTextなどを入れる")]
    public RectTransform textObject;

    [Header("Text Scale")]
    [Tooltip("通常時の文字サイズ")]
    public float normalTextScale = 1f;

    [Tooltip("カーソルを乗せた時の文字サイズ")]
    public float hoverTextScale = 1.1f;

    [Tooltip("文字サイズが変化する速さ")]
    public float textScaleSpeed = 6f;

    RectTransform buttonRectTransform;

    float targetRotationZ;
    Vector3 targetTextScale;

    void Awake()
    {
        buttonRectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        targetRotationZ = normalRotationZ;
        targetTextScale = Vector3.one * normalTextScale;

        if (buttonRectTransform != null)
        {
            buttonRectTransform.localRotation =
                Quaternion.Euler(0f, 0f, normalRotationZ);
        }

        if (textObject != null)
        {
            textObject.localScale =
                Vector3.one * normalTextScale;
        }

        SetShadows(false);
    }

    void Update()
    {
        UpdateRotation();
        UpdateTextScale();
    }

    void UpdateRotation()
    {
        if (buttonRectTransform == null)
            return;

        Quaternion targetRotation =
            Quaternion.Euler(0f, 0f, targetRotationZ);

        buttonRectTransform.localRotation =
            Quaternion.Lerp(
                buttonRectTransform.localRotation,
                targetRotation,
                Time.deltaTime * rotateSpeed
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetRotationZ = hoverRotationZ;
        targetTextScale = Vector3.one * hoverTextScale;

        SetShadows(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetRotationZ = normalRotationZ;
        targetTextScale = Vector3.one * normalTextScale;

        SetShadows(false);
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