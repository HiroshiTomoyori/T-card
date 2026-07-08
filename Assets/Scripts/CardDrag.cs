using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrag : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("ドラッグ中サイズ")]
    public float draggingScale = 3.0f;

    [Header("バトルエリアサイズ")]
    public float battleScale = 1.2f;

    Transform originalParent;
    Vector3 originalPosition;
    Quaternion originalRotation;
    Vector3 originalScale;

    bool droppedSuccessfully = false;
    bool canDrag = false;

    Canvas canvas;
    CanvasGroup canvasGroup;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        if(canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // バトルエリア上のカードはドラッグ禁止
        if(IsInBattleArea())
            return;

        canDrag = false;

        HandController hand =
            GetComponentInParent<HandController>();

        if(hand != null && hand.IsIdle)
            return;

        canDrag = true;
        droppedSuccessfully = false;

        originalParent = transform.parent;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        transform.localRotation = Quaternion.identity;

        ApplyDraggingScale();

        canvasGroup.blocksRaycasts = false;

        ResourcePhaseManager rpm =
            FindFirstObjectByType<ResourcePhaseManager>();

        if(rpm != null)
            rpm.ShowDropHighlight();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(!canDrag)
            return;

        transform.position = eventData.position;

        transform.localRotation = Quaternion.identity;
        ApplyDraggingScale();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(!canDrag)
            return;

        canDrag = false;

        ResourcePhaseManager rpm =
            FindFirstObjectByType<ResourcePhaseManager>();

        if(rpm != null)
            rpm.HideDropHighlight();

        canvasGroup.blocksRaycasts = true;

        if(droppedSuccessfully)
            return;

        transform.SetParent(originalParent, false);
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
        transform.localScale = originalScale;
    }

void ApplyDraggingScale()
{
    float canvasScale = 1f;

    if(canvas != null)
        canvasScale = canvas.transform.lossyScale.x;

    if(canvasScale <= 0f)
        canvasScale = 1f;

    // 1.15倍大きく表示
    transform.localScale =
        Vector3.one * ((draggingScale * 1.6f) / canvasScale);
}

    public void MarkDroppedSuccessfully()
    {
        droppedSuccessfully = true;
    }

    public void DropToBattleArea(Transform battleArea)
    {
        droppedSuccessfully = true;

        transform.SetParent(battleArea, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        transform.localScale = Vector3.one * battleScale;

        canvasGroup.blocksRaycasts = true;
    }

    bool IsInBattleArea()
    {
        Transform t = transform.parent;

        while(t != null)
        {
            if(t.name == "PlayerBattleArea" ||
            t.name == "EnemyBattleArea")
            {
                return true;
            }

            t = t.parent;
        }

        return false;
    }
}