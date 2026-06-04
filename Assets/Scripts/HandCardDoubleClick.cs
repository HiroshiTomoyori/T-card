using UnityEngine;
using UnityEngine.EventSystems;

public class HandCardDoubleClick : MonoBehaviour, IPointerClickHandler
{
    float lastClickTime = -1f;
    const float DOUBLE_CLICK_TIME = 0.5f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if(transform.parent == null)
            return;

        if(transform.parent.name != "HandArea")
            return;

        if(Time.time - lastClickTime < DOUBLE_CLICK_TIME)
        {
            if(HandExpandManager.I != null)
            {
                HandExpandManager.I.ToggleHand();
            }
        }

        lastClickTime = Time.time;
    }
}