using System;
using UnityEngine;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Impact Strike attack animation - card tilts forward and punches scale without moving position.
/// Creates a quick strike/impact effect for melee-focused cards.
/// </summary>
[Serializable]
[AddTypeMenu("Impact Strike", 103)]
public class ImpactStrikeAttackAnimation : IAttackAnimation
{
    [SerializeField] private float tiltAngle = 25f;
    [SerializeField] private float scalePunch = 1.3f;
    [SerializeField] private float duration = 0.3f;

    public void Attack(Card attacker, Transform target, Action onAttack, Action onComplete)
    {
        if (attacker == null || target == null) return;

        // Kill existing tweens to prevent conflicts
        attacker.cardVisuals.transform.DOKill();
        attacker.SetAttackingState(true);

        // Store original rotation and scale
        Vector3 originalRotation = attacker.cardVisuals.transform.localEulerAngles;
        Vector3 originalScale = attacker.originalVisualsScale;

        // Calculate direction toward target for tilt
        Vector3 directionToTarget = (target.position - attacker.transform.position).normalized;

        // Calculate tilt rotation based on direction (tilt X forward, rotate Y to face target)
        float targetYRotation = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
        Vector3 strikeRotation = new Vector3(-tiltAngle, targetYRotation, 0f);

        // Create impact sequence
        Sequence impactSequence = DOTween.Sequence();

        // Strike forward with tilt and scale punch (50% of duration)
        impactSequence.Append(
            attacker.cardVisuals.transform.DOLocalRotate(strikeRotation, duration * 0.5f)
                .SetEase(Ease.OutQuad)
        );
        impactSequence.Join(
            attacker.cardVisuals.transform.DOScale(originalScale * scalePunch, duration * 0.5f)
                .SetEase(Ease.OutQuad)
        );

        // "Hit" moment at peak of strike
        impactSequence.AppendCallback(() => onAttack?.Invoke());

        // Return to original rotation and scale (50% of duration)
        impactSequence.Append(
            attacker.cardVisuals.transform.DOLocalRotate(originalRotation, duration * 0.5f)
                .SetEase(Ease.InQuad)
        );
        impactSequence.Join(
            attacker.cardVisuals.transform.DOScale(originalScale, duration * 0.5f)
                .SetEase(Ease.InQuad)
        );

        // Invoke callback when complete
        impactSequence.AppendCallback(() => {
            attacker.SetAttackingState(false);
            onComplete?.Invoke();
        });

        impactSequence.SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }
}

}
