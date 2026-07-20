using UnityEngine;
using UnityEngine.EventSystems;

public class HandCardDoubleClick : MonoBehaviour, IPointerClickHandler
{
    [Header("Double Click")]
    public float doubleClickTime = 0.6f;

    // カードごとにクリック時刻を持たせる
    float lastClickTime = -1f;

    public void OnPointerClick(PointerEventData eventData)
    {
        // 左クリック以外は無視
        if(eventData.button != PointerEventData.InputButton.Left)
            return;

        HandController handController =
            GetComponentInParent<HandController>();

        // 手札にないカードでは処理しない
        if(handController == null)
            return;

        float now = Time.unscaledTime;

        if(lastClickTime >= 0f &&
           now - lastClickTime <= doubleClickTime)
        {
            handController.Toggle();

            lastClickTime = -1f;

            // このクリックが他のクリック処理へ流れるのを抑える
            eventData.Use();
            return;
        }

        lastClickTime = now;
    }

    void OnDisable()
    {
        lastClickTime = -1f;
    }
}