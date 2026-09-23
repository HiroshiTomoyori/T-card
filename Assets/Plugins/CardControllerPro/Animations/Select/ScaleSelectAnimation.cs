using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Scales the card up on select and back down on deselect.
/// </summary>
[Serializable]
[AddTypeMenu("Scale", 1)]
public class ScaleSelectAnimation : ISelectAnimation
{
    [SerializeField] private float scaleMultiplier = 1.15f;
    [SerializeField] private float duration = 0.2f;

    public void Select(Card card)
    {
        card.cardHoverer.transform.DOKill(complete: true);
        card.cardHoverer.transform.DOScale(card.originalVisualsScale * scaleMultiplier, duration)
            .SetEase(Ease.OutBack)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }

    public void Deselect(Card card)
    {
        card.cardHoverer.transform.DOKill(complete: true);
        card.cardHoverer.transform.DOScale(card.originalVisualsScale, duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }
}

}
