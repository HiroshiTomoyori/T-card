using UnityEngine;

namespace CCP
{

public class DemoCardDealer : MonoBehaviour {
    public CardGroup sourceCardGroup;
    public CardGroup destinationCardGroup;

    public void Deal() {
        if (sourceCardGroup.cards.Count == 0) return;
        var card = sourceCardGroup.Back();
        card.MoveToGroup(destinationCardGroup);
        card.FlipCard();
    }
}

}