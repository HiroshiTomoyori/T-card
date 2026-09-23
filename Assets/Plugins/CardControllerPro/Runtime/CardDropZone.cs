using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CCP
{

public class CardDropZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDropHandler {
    public bool activateWhenMovingCards = true;

    public CardGroup cardGroup;
    public bool allowDroppingCardsFromDestinationGroup = false;

    private void Start() {
        Card.OnCardMoved += CardMoved;
        gameObject.SetActive(false);
    }

    private void OnDestroy() {
        Card.OnCardMoved -= CardMoved;
    }

    public void CardMoved(bool moving) {
        gameObject.SetActive(moving);
    }
    
    public void OnPointerEnter(PointerEventData eventData) { }
    public void OnPointerExit(PointerEventData eventData) { }

    public void OnDrop(PointerEventData eventData) {
        GameObject droppedObject = eventData.pointerDrag;
        Card card = droppedObject.GetComponent<Card>();
        if (cardGroup == card.parentCardGroup && !allowDroppingCardsFromDestinationGroup) {
            return;
        }

        if (card.IsUseType) {
            card.PlayUseAnimation(cardGroup);
        }
        else {
            card.parentCardGroup.RemoveCard(card);
            cardGroup.AddBack(card);
            card.PlayPlaceAnimation();
        }
    }
}

}