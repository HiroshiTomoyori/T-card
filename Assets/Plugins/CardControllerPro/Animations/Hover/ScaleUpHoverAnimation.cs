using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Scales the card up on hover and back down on unhover.
/// Matches the original ScaleCardHoverer behavior.
/// </summary>
[Serializable]
[AddTypeMenu("Scale Up", 102)]
public class ScaleUpHoverAnimation : IHoverAnimation
{
    [SerializeField] private float scaleMultiplier = 1.15f;
    [SerializeField] private float duration = 0.2f;

    public void Hover(Card card)
    {
        card.cardHoverer.transform.DOKill(complete: true);
        card.cardHoverer.transform.DOScale(card.originalVisualsScale * scaleMultiplier, duration)
            .SetEase(Ease.OutBack)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }

    public void Unhover(Card card)
    {
        card.cardHoverer.transform.DOKill(complete: true);
        card.cardHoverer.transform.DOScale(card.originalVisualsScale, duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }
}

}
