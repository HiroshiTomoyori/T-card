using System;
using UnityEngine;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Custom Curve place animation - fully configurable via AnimationCurves.
/// Position and scale are driven by separate curves during placement.
/// </summary>
[Serializable]
[AddTypeMenu("Custom Curve", 108)]
public class CustomCurvePlaceAnimation : IPlaceAnimation
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private AnimationCurve positionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Constant(0, 1, 1);

    public void Place(Card card, Vector3 target, Action onComplete)
    {
        card.transform.DOKill();
        card.isBeingMovedManually = true;

        Vector3 originPosition = card.transform.position;
        Quaternion originRotation = card.transform.rotation;
        Vector3 originalScale = card.transform.localScale;

        Sequence sequence = DOTween.Sequence();

        sequence.Append(
            DOVirtual.Float(0f, 1f, duration, t =>
            {
                float posT = positionCurve.Evaluate(t);
                card.transform.position = Vector3.LerpUnclamped(originPosition, card.destinationPosition, posT);
                card.transform.rotation = Quaternion.LerpUnclamped(originRotation, card.destinationRotation, posT);
                card.transform.localScale = originalScale * scaleCurve.Evaluate(t);
            })
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