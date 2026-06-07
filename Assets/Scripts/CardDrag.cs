using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrag : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    Transform originalParent;
    Vector3 originalPosition;
    bool droppedSuccessfully = false;

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
        droppedSuccessfully = false;

        originalParent = transform.parent;
        originalPosition = transform.localPosition;

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;

        ResourcePhaseManager rpm =
            FindFirstObjectByType<ResourcePhaseManager>();

        if(rpm != null)
        {
            rpm.ShowDropHighlight();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ResourcePhaseManager rpm =
            FindFirstObjectByType<ResourcePhaseManager>();

        if(rpm != null)
        {
            rpm.HideDropHighlight();
        }

        canvasGroup.blocksRaycasts = true;

        if(droppedSuccessfully)
            return;

        transform.SetParent(originalParent, false);
        transform.localPosition = originalPosition;
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
        transform.localScale = Vector3.one;

        canvasGroup.blocksRaycasts = true;
    }
}