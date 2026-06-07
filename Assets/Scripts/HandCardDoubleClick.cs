using UnityEngine;
using UnityEngine.EventSystems;

public class HandCardDoubleClick : MonoBehaviour, IPointerClickHandler
{
    private static float lastClickTime = 0f;
    private static bool isIdle = true;

    public float doubleClickTime = 0.35f;

    public void OnPointerClick(PointerEventData eventData)
    {
        float now = Time.time;

        if (now - lastClickTime <= doubleClickTime)
        {
            HandController handController =
                GetComponentInParent<HandController>();

            if (handController == null)
            {
                handController =
                    FindFirstObjectByType<HandController>();
            }

            if (handController == null)
                return;

            isIdle = !isIdle;
            handController.ChangeState(isIdle);
        }

        lastClickTime = now;
    }
}