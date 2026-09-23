using UnityEngine;

namespace CCP
{

[CreateAssetMenu(fileName = "NewFireballCardData", menuName = "CardControllerPro/Fireball Card Data")]
public class FireballCardData : CardData
{
    [Header("Fireball Settings")]
    public int damage = 5;

    public override void OnTargetedUse(Card card, Card target)
    {
        if (target == null) return;

        CardHealth health = target.GetComponent<CardHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }
    }
}

}
