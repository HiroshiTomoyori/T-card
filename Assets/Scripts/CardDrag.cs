using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrag : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("ドラッグ中サイズ（ピクセル）")]
    [Tooltip("ドラッグ中のカード幅")]
    public float dragWidth = 190f;

    [Tooltip("ドラッグ中のカード高さ")]
    public float dragHeight = 285f;

    [Header("バトルエリアサイズ")]
    public float battleScale = 1.2f;

    Transform originalParent;

    Vector3 originalPosition;
    Quaternion originalRotation;
    Vector3 originalScale;

    Vector2 originalSizeDelta;

    bool droppedSuccessfully = false;
    bool canDrag = false;

    Canvas canvas;
    CanvasGroup canvasGroup;
    RectTransform rectTransform;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        if(canvasGroup == null)
        {
            canvasGroup =
                gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(
        PointerEventData eventData
    )
    {
        // バトルエリア上のカードはドラッグ禁止
        if(IsInBattleArea())
            return;

        canDrag = false;

        HandController hand =
            GetComponentInParent<HandController>();

        // 収納中の手札はドラッグ禁止
        if(hand != null && hand.IsIdle)
            return;

        if(canvas == null)
        {
            canvas =
                GetComponentInParent<Canvas>();

            if(canvas == null)
                return;
        }

        if(rectTransform == null)
        {
            rectTransform =
                GetComponent<RectTransform>();

            if(rectTransform == null)
                return;
        }

        canDrag = true;
        droppedSuccessfully = false;

        originalParent = transform.parent;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;
        originalSizeDelta = rectTransform.sizeDelta;

        // Canvas直下へ移動
        transform.SetParent(
            canvas.transform,
            true
        );

        transform.SetAsLastSibling();

        // ドラッグ中は縦向き
        transform.localRotation =
            Quaternion.identity;

        ApplyDraggingSize();

        canvasGroup.blocksRaycasts = false;

        ResourcePhaseManager rpm =
            FindFirstObjectByType<ResourcePhaseManager>();

        if(rpm != null)
        {
            rpm.ShowDropHighlight();
            rpm.SetResourceAreaRaycast(true);
        }
    }

    public void OnDrag(
        PointerEventData eventData
    )
    {
        if(!canDrag)
            return;

        transform.position = eventData.position;

        transform.localRotation =
            Quaternion.identity;

        ApplyDraggingSize();
    }

    public void OnEndDrag(
        PointerEventData eventData
    )
    {
        if(!canDrag)
            return;

        canDrag = false;

        ResourcePhaseManager rpm =
            FindFirstObjectByType<ResourcePhaseManager>();

        if(rpm != null)
        {
            rpm.HideDropHighlight();
            rpm.SetResourceAreaRaycast(false);
        }

        canvasGroup.blocksRaycasts = true;

        if(droppedSuccessfully)
            return;

        // ドロップ失敗時は元の手札へ戻す
        transform.SetParent(
            originalParent,
            false
        );

        transform.localPosition =
            originalPosition;

        transform.localRotation =
            originalRotation;

        transform.localScale =
            originalScale;

        if(rectTransform != null)
        {
            rectTransform.sizeDelta =
                originalSizeDelta;
        }
    }

    void ApplyDraggingSize()
    {
        if(rectTransform == null)
            return;

        // Scaleは1に固定し、
        // 幅・高さを直接指定
        transform.localScale =
            Vector3.one;

        rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            dragWidth
        );

        rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            dragHeight
        );
    }

    public void MarkDroppedSuccessfully()
    {
        droppedSuccessfully = true;
    }

    public void DropToBattleArea(
        Transform battleArea
    )
    {
        if(battleArea == null)
            return;

        droppedSuccessfully = true;

        transform.SetParent(
            battleArea,
            false
        );

        transform.localPosition =
            Vector3.zero;

        transform.localRotation =
            Quaternion.identity;

        transform.localScale =
            Vector3.one * battleScale;

        // バトルエリアでは元のカードサイズへ戻す
        if(rectTransform != null)
        {
            rectTransform.sizeDelta =
                originalSizeDelta;
        }

        canvasGroup.blocksRaycasts = true;

        ResourcePhaseManager rpm =
            FindFirstObjectByType<ResourcePhaseManager>();

        if(rpm != null)
        {
            rpm.HideDropHighlight();
            rpm.SetResourceAreaRaycast(false);
        }
    }

    bool IsInBattleArea()
    {
        Transform current =
            transform.parent;

        while(current != null)
        {
            if(current.name == "PlayerBattleArea" ||
               current.name == "EnemyBattleArea")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    void OnDisable()
    {
        if(canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        ResourcePhaseManager rpm =
            FindFirstObjectByType<ResourcePhaseManager>();

        if(rpm != null)
        {
            rpm.HideDropHighlight();
            rpm.SetResourceAreaRaycast(false);
        }
    }
}