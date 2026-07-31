using UnityEngine;
using UnityEngine.EventSystems;

public class HandCardDoubleClick :
    MonoBehaviour,
    IPointerClickHandler
{
    public void OnPointerClick(
        PointerEventData eventData
    )
    {
        if(eventData.button !=
           PointerEventData.InputButton.Left)
        {
            return;
        }

        HandController handController =
            GetComponentInParent<HandController>();

        if(handController == null)
            return;

        handController.RegisterHandCardClick();

        eventData.Use();
    }
}