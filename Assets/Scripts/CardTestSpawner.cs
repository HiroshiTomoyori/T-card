using UnityEngine;

public class CardTestSpawner : MonoBehaviour
{
    public GameObject cardPrefab;
    public CardData testCard;

    void Start()
    {
        GameObject obj =
            Instantiate(
                cardPrefab,
                transform
            );

        CardController card =
            obj.GetComponent<CardController>();

        if(card != null)
        {
            card.SetData(testCard);
        }
    }
}