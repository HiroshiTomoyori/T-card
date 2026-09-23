using System.Collections.Generic;
using UnityEngine;

namespace CCP
{

public class CardTester : MonoBehaviour {
    [SerializeField] private List<CardGroup> cardGroups;
    [SerializeField] private List<int> numOfCards;
    [SerializeField] private GameObject cardPrefab;
    
    [SerializeField] private CardGroup deck;
    
    void Start()
    {
        for (int i = 0; i < cardGroups.Count; i++) {
            var cardGroup = cardGroups[i];
            int num = numOfCards.Count > i ? numOfCards[i] : 6;
            for (int j = 0; j < num; j++) {
                var card = Instantiate(cardPrefab, transform);
                cardGroup.AddBack(card.GetComponent<Card>());
            }
        }
        
        for (int i = 0; i < 6; i++) {
            var card = Instantiate(cardPrefab, transform);
            deck.AddBack(card.GetComponent<Card>());
            card.GetComponent<Card>().FlipCardNoAnimation();
        }
    }

}

}
