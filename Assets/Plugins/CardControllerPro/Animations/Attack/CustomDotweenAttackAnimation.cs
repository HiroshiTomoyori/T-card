using System;
using UnityEngine;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Custom DOTween attack animation - fully configurable via DOTween Ease types.
/// Position and scale use separate eases for both approach and return phases.
/// </summary>
[Serializable]
[AddTypeMenu("Custom DOTween", 106)]
public class CustomDotweenAttackAnimation : IAttackAnimation
{
    [Header("Approach (to target)")]
    [SerializeField] private float approachDuration = 0.1f;
    [SerializeField] private Ease approachPositionEase = Ease.InQuad;
    [SerializeField] private float approachScaleMultiplier = 1f;
    [SerializeField] private Ease approachScaleEase = Ease.InQuad;

    [Header("Return (to origin)")]
    [SerializeField] private float returnDuration = 0.3f;
    [SerializeField] private Ease returnPositionEase = Ease.InOutCubic;
    [SerializeField] private float returnScaleMultiplier = 1f;
    [SerializeField] private Ease returnScaleEase = Ease.InOutCubic;

    public void Attack(Card attacker, Transform target, Action onAttack, Action onComplete)
    {
        if (attacker == null || target == null) return;

        attacker.cardVisuals.transform.DOKill();
        attacker.SetAttackingState(true);

        Vector3 originalLocalPosition = attacker.cardVisuals.transform.localPosition;
        Vector3 originalScale = attacker.originalVisualsScale;
        Vector3 targetPosition = target.position;

        Sequence sequence = DOTween.Sequence();

        // Approach phase - move to target
        sequence.Append(
            attacker.cardVisuals.transform.DOMove(targetPosition, approachDuration)
                .SetEase(approachPositionEase)
        );
        sequence.Join(
            attacker.cardVisuals.transform.DOScale(originalScale * approachScaleMultiplier, approachDuration)
                .SetEase(approachScaleEase)
        );

        // Fire onAttack at the moment of impact
        sequence.AppendCallback(() => onAttack?.Invoke());

        // Return phase - move back to origin
        sequence.Append(
            attacker.cardVisuals.transform.DOLocalMove(originalLocalPosition, returnDuration)
                .SetEase(returnPositionEase)
        );
        sequence.Join(
            attacker.cardVisuals.transform.DOScale(originalScale * returnScaleMultiplier, returnDuration)
                .SetEase(returnScaleEase)
        );

        // Clean up
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
