using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Scales the card up on pickup and back to original on putdown.
/// </summary>
[Serializable]
[AddTypeMenu("Scale Up", 103)]
public class ScaleUpPickupAnimation : IPickupAnimation
{
    [SerializeField] private float scaleMultiplier = 1.15f;
    [SerializeField] private float duration = 0.2f;

    public void Pickup(Card card)
    {
        card.cardPickuper.transform.DOKill(complete: true);
        card.cardPickuper.transform.DOScale(card.originalPickuperScale * scaleMultiplier, duration)
            .SetEase(Ease.OutBack)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }

    public void PutDown(Card card)
    {
        card.cardPickuper.transform.DOKill(complete: true);
        card.cardPickuper.transform.DOScale(card.originalPickuperScale, duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }
}

}
