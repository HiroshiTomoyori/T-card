using System;
using UnityEngine;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Quick Dash attack animation - fast linear movement to target and return.
/// Aggressive and snappy feel for instant attack feedback.
/// </summary>
[Serializable]
[AddTypeMenu("Quick Dash", 101)]
public class QuickDashAttackAnimation : IAttackAnimation
{
    [SerializeField] private float dashDuration = 1f;
    [SerializeField] private float holdDuration = 0.1f;
    [SerializeField] private float returnDuration = 1f;
    [SerializeField] private Ease easing = Ease.InOutQuad;

    public void Attack(Card attacker, Transform target, Action onAttack, Action onComplete)
    {
        if (attacker == null || target == null) return;

        // Kill existing tweens to prevent conflicts
        attacker.cardVisuals.transform.DOKill();
        attacker.SetAttackingState(true);

        // Store original position
        Vector3 originalLocalPosition = attacker.cardVisuals.transform.localPosition;
        Vector3 targetPosition = target.position;

        // Create dash sequence
        Sequence dashSequence = DOTween.Sequence();

        // Fast dash to target
        dashSequence.Append(
            attacker.cardVisuals.transform.DOMove(targetPosition, 0.25f)
                .SetEase(Ease.InQuart)
        );

        // "Hit" moment at target
        dashSequence.AppendCallback(() => onAttack?.Invoke());

        // Return to original position
        dashSequence.Append(
            attacker.cardVisuals.transform.DOLocalMove(originalLocalPosition, 0.25f)
                .SetEase(Ease.OutQuad)
        );

        // Invoke callback when complete
        dashSequence.AppendCallback(() => {
            attacker.SetAttackingState(false);
            onComplete?.Invoke();
        });

        dashSequence.SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }
}

}
