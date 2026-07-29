using UnityEngine;
using UnityEngine.EventSystems;

public class TableDrawer : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public RectTransform tablePanel;

    public float closedX = 420f;
    public float openedX = 0f;
    public float threshold = 120f;
    public float slideSpeed = 12f;

    Vector2 dragStartPos;
    float panelStartX;
    bool isOpen = false;
    bool isDragging = false;

    void Update()
    {
        if(tablePanel == null)
            return;

        if(isOpen)
        {
            tablePanel.SetAsLastSibling();
        }

        if(isDragging)
            return;

        float targetX =
            isOpen ? openedX : closedX;

        Vector2 pos =
            tablePanel.anchoredPosition;

        pos.x = Mathf.Lerp(
            pos.x,
            targetX,
            Time.deltaTime * slideSpeed
        );

        tablePanel.anchoredPosition = pos;
    }

    public void OnBeginDrag(
        PointerEventData eventData
    )
    {
        if(tablePanel == null)
            return;

        isDragging = true;

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                tablePanel.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out dragStartPos
            );

        panelStartX =
            tablePanel.anchoredPosition.x;

        tablePanel.SetAsLastSibling();
    }

    public void OnDrag(
        PointerEventData eventData
    )
    {
        if(!isDragging)
            return;

        if(tablePanel == null)
            return;

        Vector2 currentPos;

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                tablePanel.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out currentPos
            );

        float deltaX =
            currentPos.x - dragStartPos.x;

        Vector2 pos =
            tablePanel.anchoredPosition;

        pos.x = Mathf.Clamp(
            panelStartX + deltaX,
            openedX,
            closedX
        );

        tablePanel.anchoredPosition = pos;
    }

    public void OnEndDrag(
        PointerEventData eventData
    )
    {
        if(!isDragging)
            return;

        isDragging = false;

        if(tablePanel == null)
            return;

        float movedDistance =
            tablePanel.anchoredPosition.x -
            panelStartX;

        if(Mathf.Abs(movedDistance) >= threshold)
        {
            if(movedDistance < 0f)
            {
                isOpen = true;
            }
            else
            {
                isOpen = false;
            }
        }
        else
        {
            float centerX =
                (openedX + closedX) * 0.5f;

            isOpen =
                tablePanel.anchoredPosition.x <
                centerX;
        }
    }
}