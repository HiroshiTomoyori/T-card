using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Punches the card scale on pickup for a quick pop effect.
/// </summary>
[Serializable]
[AddTypeMenu("Punch Scale", 100)]
public class PunchScalePickupAnimation : IPickupAnimation
{
    [SerializeField] private Vector3 punchScale = new Vector3(0.1f, 0.1f, 0f);
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private int vibrato = 6;
    [SerializeField] private float elasticity = 0.5f;

    public void Pickup(Card card)
    {
        card.cardPickuper.transform.DOKill();
        card.cardPickuper.transform.localScale = card.originalPickuperScale;
        card.cardPickuper.transform.DOPunchScale(punchScale, duration, vibrato, elasticity)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }

    public void PutDown(Card card)
    {
        card.cardPickuper.transform.DOKill();
        card.cardPickuper.transform.DOScale(card.originalPickuperScale, 0.15f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }
}

}
