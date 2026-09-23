using System;
using DG.Tweening;
using UnityEngine;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Dashes the card to the targeted card before destruction.
/// Only meaningful for TargettedUse; if target is null, falls back to a quick scale-down.
/// </summary>
[Serializable]
[AddTypeMenu("Dash", 101)]
public class DashUseAnimation : IUseAnimation
{
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private Ease easing = Ease.InQuart;

    public void Use(Card card, Transform target, Action onComplete)
    {
        if (card == null)
        {
            onComplete?.Invoke();
            return;
        }

        card.cardVisuals.transform.DOKill();

        if (target != null)
        {
            // Dash toward the targeted card
            DOTween.Sequence()
                .Append(card.cardVisuals.transform.DOMove(target.position, dashDuration)
                    .SetEase(easing))
                .AppendCallback(() => onComplete?.Invoke())
                .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
        }
        else
        {
            // No target: quick scale-down as fallback
            DOTween.Sequence()
                .Append(card.cardVisuals.transform.DOScale(Vector3.zero, dashDuration)
                    .SetEase(easing))
                .AppendCallback(() => onComplete?.Invoke())
                .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);
        }
    }
}

}
