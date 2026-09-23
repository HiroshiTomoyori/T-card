using UnityEngine;

namespace CCP
{

[CreateAssetMenu(fileName = "NewBanditCardData", menuName = "CardControllerPro/Bandit Card Data")]
public class BanditCardData : CardData
{
    public override void InitializeCard(Card card) {
        base.InitializeCard(card);
        card.GetComponent<CardHealth>().StartDisplayingHealth();
    }
}

}
