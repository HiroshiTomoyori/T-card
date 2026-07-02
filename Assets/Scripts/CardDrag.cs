using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrag : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
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
        canDrag = false;

        HandController hand =
            GetComponentInParent<HandController>();

        if(hand != null && hand.IsIdle)
        {
            return;
        }

        canDrag = true;
        droppedSuccessfully = false;

        originalParent = transform.parent;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;

        // 展開中の斜め・縮小状態をドラッグ開始時に戻す
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

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
        transform.localScale = Vector3.one;

        canvasGroup.blocksRaycasts = true;
    }
}