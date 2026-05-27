using UnityEngine;
using UnityEngine.EventSystems;

public class ResourceDropZone : MonoBehaviour, IDropHandler
{
    public ResourcePhaseManager resourcePhaseManager;

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("ResourceArea にドロップされた");

        if (resourcePhaseManager == null)
        {
            Debug.LogError("ResourceDropZone の ResourcePhaseManager が未設定");
            return;
        }

        if (eventData.pointerDrag == null)
        {
            Debug.LogError("pointerDrag が null");
            return;
        }

        CardDrag card =
            eventData.pointerDrag.GetComponent<CardDrag>();

        if (card == null)
        {
            Debug.LogError("CardDrag が見つからない");
            return;
        }

        resourcePhaseManager.TryChargeResource(card);
    }
}