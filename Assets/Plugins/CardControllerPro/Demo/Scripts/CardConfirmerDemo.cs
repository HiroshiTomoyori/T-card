using System.Collections;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

public class CardConfirmerDemo : MonoBehaviour
{
    public CardGroup cardGroup;
    public CardGroupInitializer cardInitializer;
    [SerializeReference, SubclassSelector] public IHideAnimation hideAnimation;

    public void ConfirmCards() {
        var selectedCards = cardGroup.GetSelectedCards();
        foreach (var card in selectedCards) {
            var balatroValue = card.GetComponent<BalatroCardValue>();
            if (balatroValue != null) {
                balatroValue.ShowValue();
            }
        }

        StartCoroutine(RemoveAllCardsOneByOne());
    }

    private IEnumerator RemoveAllCardsOneByOne() {
        yield return new WaitForSeconds(0.5f);
        while (cardGroup.cards.Count > 0) {
            bool animationDone = false;
            cardGroup.RemoveFront(hideAnimation, (card) => {
                animationDone = true;
                Destroy(card.gameObject);
            });
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(0.5f);
        cardInitializer.Initialize();
    }
}

}
