using System;
using UnityEngine;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Swirl placement animation extracted from Card.PlaceWithSwirlAnimation.
/// Performs 360-degree rotation, scale pulse, and position bounce.
/// </summary>
[Serializable]
[AddTypeMenu("Swirl", 100)]
public class SwirlPlaceAnimation : IPlaceAnimation
{
    [SerializeField] private float duration = 0.4f;

    public void Place(Card card, Vector3 target, Action onComplete)
    {
        // Kill existing tweens on cardVisuals transform to prevent conflicts
        card.cardHoverer.transform.DOKill();

        // Set isBeingPlaced flag to disable hover tilt during animation
        card.isBeingPlaced = true;

        Vector3 originalScale = card.originalVisualsScale;

        // Rotation sequence (360-degree spin)
        DOTween.Sequence()
            .Append(card.cardHoverer.transform.DOBlendableLocalRotateBy(
                new Vector3(0, 360, 0), duration, RotateMode.FastBeyond360))
            .SetEase(Ease.OutQuad)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);

        // Scale sequence (grow then shrink)
        DOTween.Sequence()
            .Append(card.cardHoverer.transform.DOScale(originalScale * 2.25f, 0.3f)
                .SetEase(Ease.OutExpo))
            .Append(card.cardHoverer.transform.DOScale(originalScale, 0.15f)
                .SetEase(Ease.InQuad))
            .Append(card.cardHoverer.transform.DOPunchScale(-Vector3.one * 0.15f, 0.5f, 6, 1f))
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);

        // Position sequence with callback
        DOTween.Sequence()
            .Append(card.cardHoverer.transform.DOLocalMove(
                card.transform.position + new Vector3(0f, 2f, 0f), 0.3f)
                .SetEase(Ease.OutSine))
            .Append(card.cardHoverer.transform.DOLocalMove(
                new Vector3(0f, 1.2f, 0f), 0.2f)
                .SetEase(Ease.InSine))
            .AppendCallback(() => {
                card.isBeingPlaced = false;
                onComplete?.Invoke();
            })
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }
}

}
