using System;
using UnityEngine;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Arc attack animation - card moves in a curved path to target and returns.
/// Simulates physical card game attack with smooth arc motion.
/// </summary>
[Serializable]
[AddTypeMenu("Arc", 100)]
public class ArcAttackAnimation : IAttackAnimation
{
    [SerializeField, Range(0f, 1f)] private float retreatPoint = 0.6f;
    [SerializeField] private float durationWindup = 0.6f;
    [SerializeField] private float durationStrike = 0.15f;
    [SerializeField] private float durationRetreat = 0.35f;
    [SerializeField] private float durationPause = 0.15f;
    [SerializeField] private float durationReturn = 0.5f;

    public void Attack(Card attacker, Transform target, Action onAttack, Action onComplete)
    {
        if (attacker == null || target == null) return;

        // Kill existing tweens to prevent conflicts
        attacker.cardVisuals.transform.DOKill();
        attacker.SetAttackingState(true);

        // Store original position and scale
        Vector3 originalPosition = attacker.cardVisuals.transform.position;

        Vector3 originalScale = attacker.originalVisualsScale;

        // Calculate arc path using bezier curve
        Vector3 targetPosition = target.position;
        Vector3 midPoint = (originalPosition + targetPosition) * 0.4f;
        Vector3 retreatPosition = (originalPosition + targetPosition) * retreatPoint;

        targetPosition = targetPosition.WithZ(originalPosition.z);
        midPoint = midPoint.WithZ(originalPosition.z);
        retreatPosition = retreatPosition.WithZ(originalPosition.z);

        Vector3 targetScale = originalScale * 1.3f;

        Ease easeFirst = Ease.InOutCubic;
        Ease easeSecond1 = Ease.InSine;
        Ease easeSecond2 = Ease.OutSine;

        // Create attack sequence
        Sequence attackSequence = DOTween.Sequence()
            .Append(attacker.cardVisuals.transform.DOMove(midPoint, durationWindup).SetEase(easeFirst))
            .Join(attacker.cardVisuals.transform.DOScale(targetScale, durationWindup).SetEase(easeFirst))

            .Append(attacker.cardVisuals.transform.DOMove(targetPosition, durationStrike).SetEase(easeSecond1))
            .Join(attacker.cardVisuals.transform.DOScale(originalScale, durationStrike).SetEase(easeSecond1))

            // "Hit" moment at target - brief pause for impact
            .AppendCallback(() => onAttack?.Invoke())

            .Append(attacker.cardVisuals.transform.DOMove(retreatPosition, durationRetreat).SetEase(easeSecond2))
            .Join(attacker.cardVisuals.transform.DOScale(targetScale, durationRetreat).SetEase(easeSecond2))
            .AppendInterval(durationPause)

            .Append(attacker.cardVisuals.transform.DOLocalMove(Vector3.zero, durationReturn).SetEase(easeFirst))
            .Join(attacker.cardVisuals.transform.DOScale(originalScale, durationReturn).SetEase(easeFirst))

            .AppendCallback(() => {
                attacker.SetAttackingState(false);
                onComplete?.Invoke();
            });
        
        attackSequence.SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
    }
}

}
