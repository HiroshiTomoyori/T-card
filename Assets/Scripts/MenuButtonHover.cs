using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuButtonHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Hover Shadows")]
    public GameObject shadow;
    public GameObject shadow2;
    public GameObject shadow3;

    [Header("Suit")]
    public Image suitImage;

    [Header("Suit Alpha")]
    [Range(0f,1f)]
    public float normalAlpha = 0.18f;

    [Range(0f,1f)]
    public float hoverAlpha = 0.55f;

    [Header("Rotation")]
    public float normalRotationZ = -2f;
    public float hoverRotationZ = 0f;

    [Header("Animation")]
    public float rotateSpeed = 5f;

    [Header("Suit Animation")]
    public float alphaSpeed = 2f;

    RectTransform rectTransform;

    float targetRotation;
    float targetAlpha;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        targetRotation = normalRotationZ;
        targetAlpha = normalAlpha;

        rectTransform.localRotation =
            Quaternion.Euler(
                0,
                0,
                normalRotationZ
            );

        SetShadowVisible(false);
        SetSuitAlpha(normalAlpha);
    }

    void Update()
    {
        Quaternion target =
            Quaternion.Euler(
                0,
                0,
                targetRotation
            );

        rectTransform.localRotation =
            Quaternion.Lerp(
                rectTransform.localRotation,
                target,
                Time.deltaTime * rotateSpeed
            );

        if(
            Quaternion.Angle(
                rectTransform.localRotation,
                target
            ) < 0.05f
        )
        {
            rectTransform.localRotation =
                target;
        }

        if(suitImage != null)
        {
            Color c = suitImage.color;

            c.a = Mathf.Lerp(
                c.a,
                targetAlpha,
                Time.deltaTime * alphaSpeed
            );

            suitImage.color = c;
        }
    }

    public void OnPointerEnter(
        PointerEventData eventData
    )
    {
        targetRotation = hoverRotationZ;
        targetAlpha = hoverAlpha;

        SetShadowVisible(true);
    }

    public void OnPointerExit(
        PointerEventData eventData
    )
    {
        targetRotation = normalRotationZ;
        targetAlpha = normalAlpha;

        SetShadowVisible(false);
    }

    void SetShadowVisible(bool visible)
    {
        if(shadow != null)
            shadow.SetActive(visible);

        if(shadow2 != null)
            shadow2.SetActive(visible);

        if(shadow3 != null)
            shadow3.SetActive(visible);
    }

    void SetSuitAlpha(float alpha)
    {
        if(suitImage == null)
            return;

        Color c = suitImage.color;
        c.a = alpha;
        suitImage.color = c;
    }
}