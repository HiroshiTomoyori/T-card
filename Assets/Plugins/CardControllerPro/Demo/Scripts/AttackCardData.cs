using UnityEngine;

namespace CCP
{

[CreateAssetMenu(fileName = "NewAttackCardData", menuName = "CardControllerPro/Attack Card Data")]
public class AttackCardData : CardData
{
    [Header("Attack Settings")]
    public int damage = 3;

    private void Reset()
    {
        cardType = CardType.Attack;
    }

    public override void OnAttackHit(Card attacker, Card target)
    {
        if (target == null) return;

        CardHealth health = target.GetComponent<CardHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }
    }

    public override void OnPlaced(Card card) {
        card.GetComponent<CardHealth>().StartDisplayingHealth();
    }
}

}
