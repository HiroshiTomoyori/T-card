using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Lifts the card upward on select and lowers it back on deselect.
/// </summary>
[Serializable]
[AddTypeMenu("Lift", 2)]
public class LiftSelectAnimation : ISelectAnimation
{
    [SerializeField] private float liftAmount = 50f;
    [SerializeField] private float duration = 0.2f;

    public void Select(Card card)
    {
        card.cardHoverer.transform.DOKill(complete: true);
        card.cardHoverer.transform.DOLocalMoveY(liftAmount, duration)
            .SetEase(Ease.OutBack)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }

    public void Deselect(Card card)
    {
        card.cardHoverer.transform.DOKill(complete: true);
        card.cardHoverer.transform.DOLocalMoveY(0f, duration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }
}

}
