using System;
using UnityEngine;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Custom Curve attack animation - fully configurable via AnimationCurves.
/// Position and scale are driven by separate curves for both approach and return phases.
/// </summary>
[Serializable]
[AddTypeMenu("Custom Curve", 105)]
public class CustomCurveAttackAnimation : IAttackAnimation
{
    [Header("Approach (to target)")]
    [SerializeField] private float approachDuration = 0.3f;
    [SerializeField] private AnimationCurve approachPositionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve approachScaleCurve = AnimationCurve.Constant(0, 1, 1);

    [Header("Return (to origin)")]
    [SerializeField] private float returnDuration = 0.3f;
    [SerializeField] private AnimationCurve returnPositionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve returnScaleCurve = AnimationCurve.Constant(0, 1, 1);

    public void Attack(Card attacker, Transform target, Action onAttack, Action onComplete)
    {
        if (attacker == null || target == null) return;

        attacker.cardVisuals.transform.DOKill();
        attacker.SetAttackingState(true);

        Vector3 originalLocalPosition = attacker.cardVisuals.transform.localPosition;
        Vector3 originalScale = attacker.originalVisualsScale;
        Vector3 originWorld = attacker.cardVisuals.transform.position;
        Vector3 targetPosition = target.position;

        Sequence sequence = DOTween.Sequence();

        // Approach phase - animate from origin to target
        sequence.Append(
            DOVirtual.Float(0f, 1f, approachDuration, t =>
            {
                float posT = approachPositionCurve.Evaluate(t);
                attacker.cardVisuals.transform.position = Vector3.LerpUnclamped(originWorld, targetPosition, posT);
                attacker.cardVisuals.transform.localScale = originalScale * approachScaleCurve.Evaluate(t);
            })
        );

        // Fire onAttack at the moment of impact
        sequence.AppendCallback(() => onAttack?.Invoke());

        // Return phase - animate from target back to origin
        sequence.Append(
            DOVirtual.Float(0f, 1f, returnDuration, t =>
            {
                float posT = returnPositionCurve.Evaluate(t);
                attacker.cardVisuals.transform.position = Vector3.LerpUnclamped(targetPosition, originWorld, posT);
                attacker.cardVisuals.transform.localScale = originalScale * returnScaleCurve.Evaluate(t);
            })
        );

        // Reset to exact local position and clean up
        sequence.AppendCallback(() =>
        {
            attacker.cardVisuals.transform.localPosition = originalLocalPosition;
            attacker.cardVisuals.transform.localScale = originalScale;
            attacker.SetAttackingState(false);
            onComplete?.Invoke();
        });

        sequence.SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }
}

}