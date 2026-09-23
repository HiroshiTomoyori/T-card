using System;
using UnityEngine;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Custom DOTween place animation - fully configurable via DOTween Ease types.
/// Position and scale use separate eases during placement.
/// </summary>
[Serializable]
[AddTypeMenu("Custom DOTween", 109)]
public class CustomDotweenPlaceAnimation : IPlaceAnimation
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Ease positionEase = Ease.InOutCubic;
    [SerializeField] private float scaleMultiplier = 1f;
    [SerializeField] private Ease scaleEase = Ease.InOutCubic;

    public void Place(Card card, Vector3 target, Action onComplete)
    {
        card.transform.DOKill();
        card.isBeingMovedManually = true;

        Vector3 originalScale = card.transform.localScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            card.transform.DOMove(card.destinationPosition, duration)
                .SetEase(positionEase)
        );
        sequence.Join(
            card.transform.DORotateQuaternion(card.destinationRotation, duration)
                .SetEase(positionEase)
        );
        sequence.Join(
            card.transform.DOScale(originalScale * scaleMultiplier, duration)
                .SetEase(scaleEase)
        );

        sequence.AppendCallback(() =>
        {
            card.transform.localScale = originalScale;
            card.isBeingMovedManually = false;
            onComplete?.Invoke();
        });

        sequence.SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
        sequence.SetLink(card.gameObject);
    }
}

}