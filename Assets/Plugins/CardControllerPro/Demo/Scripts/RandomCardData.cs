using System;
using UnityEngine;

namespace CCP
{

[CreateAssetMenu(fileName = "NewRandomCardData", menuName = "CardControllerPro/Random Card Data")]
public class RandomCardData : CardData
{
    [Serializable]
    public struct CardVariant
    {
        public Sprite frontImage;
        public int value;
    }

    [Header("Random Variants")]
    public CardVariant[] variants;
    
    public override void InitializeCard(Card card) {
        if (variants != null && variants.Length > 0) {
            CardVariant picked = variants[UnityEngine.Random.Range(0, variants.Length)];

            frontArt = picked.frontImage;
            BalatroCardValue value = card.GetComponent<BalatroCardValue>();
            if (value != null)
                value.value = picked.value;;
        }

        base.InitializeCard(card);
    }
}

}
