using System;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;
using UnityEngine;

namespace CCP
{

/// <summary>
/// Slide animation implementation. Moves card linearly to target position
/// with configurable duration and easing.
/// </summary>
[Serializable]
[AddTypeMenu("LinearSlide", 102)]
public class LinearSlidePlaceAnimation : IPlaceAnimation
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease easing = Ease.Linear;

    public void Place(Card card, Vector3 target, Action onComplete)
    {
        // Kill existing tweens to prevent conflicts
        card.transform.DOKill();

        // Mark card as being moved manually so that CardGroup doesn't animate it
        card.isBeingMovedManually = true;

        // Linear motion to target with smooth easing
        card.transform.DORotateQuaternion(card.destinationRotation, duration)
            .SetEase(easing)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
        card.transform.DOMove(card.destinationPosition, duration)
            .SetEase(easing)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime)
            .SetLink(card.gameObject)
            .OnComplete(() => {
                card.isBeingMovedManually = false;
                onComplete?.Invoke();
            });
    }
}

}