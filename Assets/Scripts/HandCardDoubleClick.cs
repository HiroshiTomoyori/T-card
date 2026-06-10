using UnityEngine;
using UnityEngine.EventSystems;

public class HandCardDoubleClick : MonoBehaviour, IPointerClickHandler
{
    private static float lastClickTime = -1f;

    public float doubleClickTime = 0.6f;

    public void OnPointerClick(PointerEventData eventData)
    {
        float now = Time.time;

        if(now - lastClickTime <= doubleClickTime)
        {
            HandController handController =
                GetComponentInParent<HandController>();

            if(handController != null)
            {
                handController.Toggle();
            }

            lastClickTime = -1f;
            return;
        }

        lastClickTime = now;
    }
}