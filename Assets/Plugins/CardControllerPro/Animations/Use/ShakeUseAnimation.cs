using System;
using DG.Tweening;
using UnityEngine;
using MackySoft.SerializeReferenceExtensions;

namespace CCP
{

/// <summary>
/// Shakes the card before destruction. Works for both Use and TargettedUse.
/// </summary>
[Serializable]
[AddTypeMenu("Shake", 100)]
public class ShakeUseAnimation : IUseAnimation
{
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float strength = 15f;
    [SerializeField] private int vibrato = 10;
    [SerializeField] private float randomness = 45f;

    public void Use(Card card, Transform target, Action onComplete)
    {
        if (card == null)
        {
            onComplete?.Invoke();
            return;
        }

        card.cardVisuals.transform.DOKill();

        card.cardVisuals.transform
            .DOShakeRotation(duration, new Vector3(0, 0, strength), vibrato, randomness)
            .SetUpdate(CardControllerSettings.Instance.useUnscaledTime)
            .OnComplete(() => onComplete?.Invoke());
    }
}

}
