using System;
using UnityEngine;
using DG.Tweening;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Shrink hide animation. Scales the card down to zero with a punch rotation.
/// Reverse of FromBelowPlaceAnimation.
/// </summary>
[Serializable]
[AddTypeMenu("Shrink", 200)]
public class ShrinkHideAnimation : IHideAnimation
{
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float shakeStrength = 15f;
    [SerializeField] private int shakeVibrato = 10;

    public void Hide(Card card, Action onComplete)
    {
        card.cardHoverer.transform.DOKill();
        card.transform.DOKill();
        card.Unhover();

        card.cardHoverer.transform
            .DOPunchRotation(new Vector3(0, 0, shakeStrength), duration, shakeVibrato, 1f)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime);

        card.cardHoverer.transform.DOScale(Vector3.zero, duration)
            .SetEase(Ease.InBack, 1.5f)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime)
            .OnComplete(() => onComplete?.Invoke());
    }
}

}
