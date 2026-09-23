using System;
using UnityEngine;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

[Serializable]
[AddTypeMenu("From Below", 200)]
public class FromBelowPlaceAnimation : IPlaceAnimation
{
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float shakeStrength = 15f;
    [SerializeField] private int shakeVibrato = 10;

    public void Place(Card card, Vector3 target, Action onComplete)
    {
        card.cardHoverer.transform.DOKill();
        card.transform.DOKill();
        card.isBeingPlaced = true;

        // Snap card to destination before scaling up
        card.transform.position = card.destinationPosition;
        card.transform.rotation = card.destinationRotation;
        card.Unhover();

        Vector3 originalScale = card.originalVisualsScale;
        card.cardHoverer.transform.localScale = Vector3.zero;

        // Scale from 0 to 1 with overshoot bounce
        card.cardHoverer.transform.DOScale(originalScale, duration)
            .SetEase(Ease.OutBack, 1.5f)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);

        card.cardHoverer.transform
            .DOPunchRotation(new Vector3(0,0,shakeStrength), duration, shakeVibrato, 1f)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime)
            .OnComplete(() =>
            {
                card.isBeingPlaced = false;
                onComplete?.Invoke();
            });
    }
}

}