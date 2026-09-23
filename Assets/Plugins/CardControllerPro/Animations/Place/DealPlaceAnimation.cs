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
[AddTypeMenu("Deal", 102)]
public class DealPlaceAnimation : IPlaceAnimation
{
    [SerializeField] private float duration = 0.6f;
    [SerializeField] private Ease easing = Ease.Linear;

    public void Place(Card card, Vector3 target, Action onComplete)
    {
        // Kill existing tweens on cardVisuals transform to prevent conflicts
        card.cardHoverer.transform.DOKill();

        // Set isBeingPlaced flag to disable hover tilt during animation
        card.isBeingPlaced = true;
        
        // Rotation sequence (360-degree spin)
        DOTween.Sequence()
            .Append(card.cardHoverer.transform.DOBlendableLocalRotateBy(
                new Vector3(0, 0, 360f), duration, RotateMode.FastBeyond360))
            .SetEase(Ease.OutQuad)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }
}

}
