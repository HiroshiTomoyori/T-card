using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardDrag : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    RectTransform rt;
    Canvas canvas;
    CanvasGroup canvasGroup;
    LayoutElement layoutElement;

    Vector2 startPos;
    Transform startParent;
    int startSiblingIndex;

    bool droppedSuccessfully = false;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();

        if(turnManager != null &&
        turnManager.IsBattlePhase)
        {
            Debug.Log("バトルフェイズ中は召喚不可");
            return;
        }

        droppedSuccessfully = false;

        startPos = rt.anchoredPosition;
        startParent = transform.parent;
        startSiblingIndex = transform.GetSiblingIndex();

        layoutElement.ignoreLayout = true;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rt.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

public void OnEndDrag(PointerEventData eventData)
{
    ResourceDropZone dropZone =
    eventData.pointerEnter != null
    ? eventData.pointerEnter.GetComponentInParent<ResourceDropZone>()
    : null;

    if(dropZone != null)
    {
        dropZone.OnDrop(eventData);
    }
    canvasGroup.blocksRaycasts = true;
    canvasGroup.interactable = true;

    if (droppedSuccessfully)
    {
        layoutElement.ignoreLayout = false;
        rt.localScale = Vector3.one;

        RectTransform parentRect =
            transform.parent as RectTransform;

        if(parentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }

        return;
    }

    transform.SetParent(startParent, false);
    transform.SetSiblingIndex(startSiblingIndex);

    layoutElement.ignoreLayout = false;

    rt.localScale = Vector3.one;
    rt.anchoredPosition = startPos;

    RectTransform startParentRect =
        startParent as RectTransform;

    if(startParentRect != null)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(startParentRect);
    }
}

    public void MarkDroppedSuccessfully()
    {
        droppedSuccessfully = true;
    }

    public void DropToBattleArea(Transform battleArea)
    {
        droppedSuccessfully = true;

        transform.SetParent(battleArea, false);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        layoutElement.ignoreLayout = false;

        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
        canvasGroup.alpha = 1f;

        BattleAreaLayout layout = battleArea.GetComponent<BattleAreaLayout>();
        if (layout != null)
        {
            layout.Refresh();
        }
    }
}